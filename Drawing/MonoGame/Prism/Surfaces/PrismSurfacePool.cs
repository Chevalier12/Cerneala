using Cerneala.Drawing.Prism.Graph;
using Microsoft.Xna.Framework.Graphics;

namespace Cerneala.Drawing.MonoGame.Prism.Surfaces;

internal sealed class PrismSurfacePool : IDisposable
{
    private readonly GraphicsDevice graphicsDevice;
    private readonly PrismSurfaceMemoryAccountant accountant;
    private readonly List<SurfaceEntry> available = [];
    private PrismRetainedSurfaceCache? retainedCache;
    private SurfaceEntry?[] frameEntries = [];
    private PrismGraphExecutionPlan? framePlan;
    private long frameGeneration;
    private int frameStep = -1;
    private int retentionLimit;
    private bool disposed;

    public PrismSurfacePool(GraphicsDevice graphicsDevice)
        : this(
            graphicsDevice,
            PrismSurfaceBudget.Unbounded)
    {
    }

    internal PrismSurfacePool(
        GraphicsDevice graphicsDevice,
        PrismSurfaceBudget budget)
    {
        ArgumentNullException.ThrowIfNull(graphicsDevice);
        if (graphicsDevice.IsDisposed)
        {
            throw new ObjectDisposedException(nameof(graphicsDevice));
        }

        this.graphicsDevice = graphicsDevice;
        accountant =
            new PrismSurfaceMemoryAccountant(budget);
        graphicsDevice.DeviceResetting += OnDeviceResetting;
    }

    public int ActiveLeaseCount { get; private set; }

    public int AvailableSurfaceCount => available.Count;

    public int OwnedSurfaceCount =>
        ActiveLeaseCount + AvailableSurfaceCount;

    public int PeakActiveLeaseCount { get; private set; }

    public long TransientByteCount =>
        accountant.TransientByteCount;

    public long TotalByteCount =>
        accountant.TotalByteCount;

    public long PeakTotalByteCount =>
        accountant.PeakTotalByteCount;

    public long CreatedSurfaceCount { get; private set; }

    public long ReusedSurfaceCount { get; private set; }

    public long DisposedSurfaceCount { get; private set; }

    public long PromotedSurfaceCount { get; private set; }

    internal PrismSurfaceMemoryAccountant MemoryAccountant =>
        accountant;

    public PrismSurfaceFrame BeginFrame(PrismGraphExecutionPlan plan)
    {
        accountant.VerifyAccess();
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(plan);
        if (framePlan is not null)
        {
            throw new InvalidOperationException(
                "A Prism surface frame is already active.");
        }
        if (ActiveLeaseCount != 0)
        {
            throw new InvalidOperationException(
                "A Prism surface pool cannot begin with active leases.");
        }

        retentionLimit = plan.PeakLiveSurfaces;
        available.EnsureCapacity(retentionLimit);

        if (frameEntries.Length < plan.ExecutionOrder.Length)
        {
            Array.Resize(ref frameEntries, plan.ExecutionOrder.Length);
        }
        else
        {
            Array.Clear(frameEntries);
        }

        framePlan = plan;
        frameStep = -1;
        frameGeneration = unchecked(frameGeneration + 1);
        return new PrismSurfaceFrame(this, frameGeneration);
    }

    public void Reset()
    {
        accountant.VerifyAccess();
        ThrowIfDisposed();
        ResetCore(poolDisposing: false);
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        accountant.VerifyAccess();
        graphicsDevice.DeviceResetting -= OnDeviceResetting;
        ResetCore(poolDisposing: true);
        disposed = true;
    }

    internal void AttachRetainedCache(
        PrismRetainedSurfaceCache cache)
    {
        accountant.VerifyAccess();
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(cache);
        if (retainedCache is not null)
        {
            throw new InvalidOperationException(
                "A Prism surface pool already has a retained cache.");
        }

        retainedCache = cache;
    }

    internal void DetachRetainedCache(
        PrismRetainedSurfaceCache cache)
    {
        accountant.VerifyAccess();
        if (ReferenceEquals(retainedCache, cache))
        {
            retainedCache = null;
        }
    }

    internal void AdvanceToStep(
        long generation,
        int step,
        ReadOnlySpan<PrismSurfaceKey> surfaceKeys)
    {
        AdvanceToStep(
            generation,
            step,
            surfaceKeys,
            requiredSurfaces: default);
    }

    internal void AdvanceToStep(
        long generation,
        int step,
        ReadOnlySpan<PrismSurfaceKey> surfaceKeys,
        ReadOnlySpan<bool> requiredSurfaces)
    {
        accountant.VerifyAccess();
        PrismGraphExecutionPlan plan = GetActivePlan(generation);
        if (surfaceKeys.Length != plan.ExecutionOrder.Length)
        {
            throw new ArgumentException(
                "Surface keys must align with the optimized execution order.",
                nameof(surfaceKeys));
        }
        if (!requiredSurfaces.IsEmpty &&
            requiredSurfaces.Length != plan.ExecutionOrder.Length)
        {
            throw new ArgumentException(
                "Required-surface flags must align with the optimized execution order.",
                nameof(requiredSurfaces));
        }
        if (step != frameStep + 1 ||
            step < 0 ||
            step >= plan.ExecutionOrder.Length)
        {
            throw new ArgumentOutOfRangeException(
                nameof(step),
                step,
                "Prism surface steps must advance once in execution order.");
        }

        frameStep = step;

        for (int index = 0; index < plan.SurfaceLifetimes.Length; index++)
        {
            if (frameEntries[index] is not null &&
                plan.SurfaceLifetimes[index].LastStep < step)
            {
                ReleaseFrameEntry(index);
            }
        }

        for (int index = 0; index < plan.SurfaceLifetimes.Length; index++)
        {
            if (plan.SurfaceLifetimes[index].FirstStep != step)
            {
                continue;
            }
            if (!requiredSurfaces.IsEmpty &&
                !requiredSurfaces[index])
            {
                continue;
            }
            if (frameEntries[index] is not null)
            {
                throw new InvalidOperationException(
                    "A Prism execution surface was acquired more than once.");
            }
            if (ActiveLeaseCount >= retentionLimit)
            {
                throw new InvalidOperationException(
                    "The backend-neutral lifetime plan exceeded its declared peak surface count.");
            }

            frameEntries[index] = RentEntry(surfaceKeys[index]);
            ActiveLeaseCount++;
            PeakActiveLeaseCount = Math.Max(
                PeakActiveLeaseCount,
                ActiveLeaseCount);
        }
    }

    internal RenderTarget2D GetSurface(
        long generation,
        int executionIndex)
    {
        accountant.VerifyAccess();
        PrismGraphExecutionPlan plan = GetActivePlan(generation);
        ValidateExecutionIndex(plan, executionIndex);

        SurfaceEntry? entry = frameEntries[executionIndex];
        if (entry is null)
        {
            throw new InvalidOperationException(
                "The requested Prism execution surface is not live.");
        }
        if (entry.Surface.IsDisposed)
        {
            throw new ObjectDisposedException(
                nameof(RenderTarget2D),
                "The requested Prism execution surface has been disposed.");
        }

        return entry.Surface;
    }

    internal PrismRetainedSurface PromoteToRetainedOwner(
        long generation,
        int executionIndex)
    {
        accountant.VerifyAccess();
        SurfaceEntry entry = GetPromotableEntry(
            generation,
            executionIndex);
        accountant.TransferTransientToRetained(
            entry.ByteCount);
        PrismRetainedSurface retained;
        try
        {
            retained = new PrismRetainedSurface(
                entry.Key,
                entry.Surface,
                accountant,
                entry.ByteCount);
        }
        catch
        {
            accountant.TransferRetainedToTransient(
                entry.ByteCount);
            throw;
        }
        frameEntries[executionIndex] = null;
        ActiveLeaseCount--;
        PromotedSurfaceCount++;
        return retained;
    }

    internal PrismSurfaceKey ValidatePromotion(
        long generation,
        int executionIndex)
    {
        accountant.VerifyAccess();
        return GetPromotableEntry(
            generation,
            executionIndex).Key;
    }

    internal bool TryReclaim(
        PrismRetainedSurface retained)
    {
        accountant.VerifyAccess();
        ArgumentNullException.ThrowIfNull(retained);
        if (disposed ||
            retentionLimit <= 0 ||
            !retained.IsOwnedBy(accountant) ||
            retained.Surface.IsDisposed)
        {
            return false;
        }

        try
        {
            available.EnsureCapacity(available.Count + 1);
            SurfaceEntry entry = new(
                retained.Key,
                retained.Surface,
                retained.ByteCount);
            RenderTarget2D surface =
                retained.DetachToTransientOwner();
            if (!ReferenceEquals(surface, entry.Surface))
            {
                throw new InvalidOperationException(
                    "Prism retained ownership transfer changed its GPU surface.");
            }
            available.Add(entry);
            return true;
        }
        catch (OutOfMemoryException)
        {
            return false;
        }
    }

    internal void EndFrame(long generation)
    {
        accountant.VerifyAccess();
        if (framePlan is null || generation != frameGeneration)
        {
            return;
        }

        int surfaceCount = framePlan.SurfaceLifetimes.Length;
        for (int index = 0; index < surfaceCount; index++)
        {
            ReleaseFrameEntry(index);
        }

        framePlan = null;
        frameStep = -1;
        if (ActiveLeaseCount != 0)
        {
            throw new InvalidOperationException(
                "The Prism surface frame ended with active leases.");
        }
        TrimAvailableToLimit();
    }

    private SurfaceEntry RentEntry(PrismSurfaceKey key)
    {
        key.Validate();

        for (int index = available.Count - 1; index >= 0; index--)
        {
            SurfaceEntry entry = available[index];
            if (entry.Surface.IsDisposed)
            {
                EvictAvailableAt(index);
                continue;
            }
            if (entry.Key != key)
            {
                continue;
            }

            available.RemoveAt(index);
            ReusedSurfaceCount++;
            return entry;
        }

        while (OwnedSurfaceCount >= retentionLimit)
        {
            if (available.Count == 0)
            {
                throw new InvalidOperationException(
                    "The Prism lifetime plan requires more surfaces than its declared peak.");
            }

            EvictAvailableAt(0);
        }

        long byteCount = key.CalculateByteSize();
        while (!accountant.CanReserveTransient(
                byteCount) &&
            available.Count > 0)
        {
            EvictAvailableAt(0);
        }
        if (!accountant.CanReserveTransient(byteCount))
        {
            _ = retainedCache?.EvictForTransient(byteCount);
        }
        if (!accountant.TryReserveTransient(byteCount))
        {
            throw new PrismSurfaceAllocationException(
                key,
                new OutOfMemoryException(
                    "The Prism surface hard byte budget is exhausted."));
        }

        SurfaceEntry pendingEntry;
        try
        {
            pendingEntry = new SurfaceEntry(
                key,
                byteCount);
        }
        catch (OutOfMemoryException exception)
        {
            accountant.ReleaseTransient(byteCount);
            throw new PrismSurfaceAllocationException(
                key,
                exception);
        }

        RenderTarget2D? surface = null;
        try
        {
            surface = new RenderTarget2D(
                graphicsDevice,
                key.Width,
                key.Height,
                mipMap: false,
                key.Format,
                DepthFormat.None,
                key.MultiSampleCount,
                RenderTargetUsage.DiscardContents);
            pendingEntry.Attach(surface);
        }
        catch (Exception exception)
        {
            accountant.ReleaseTransient(byteCount);
            surface?.Dispose();
            throw new PrismSurfaceAllocationException(key, exception);
        }
        CreatedSurfaceCount++;
        return pendingEntry;
    }

    private void ReleaseFrameEntry(int executionIndex)
    {
        SurfaceEntry? entry = frameEntries[executionIndex];
        if (entry is null)
        {
            return;
        }

        frameEntries[executionIndex] = null;
        ActiveLeaseCount--;
        if (entry.Surface.IsDisposed)
        {
            accountant.ReleaseTransient(entry.ByteCount);
            DisposedSurfaceCount++;
            return;
        }

        available.Add(entry);
    }

    private void TrimAvailableToLimit()
    {
        while (OwnedSurfaceCount > retentionLimit)
        {
            EvictAvailableAt(0);
        }
    }

    private void EvictAvailableAt(int index)
    {
        SurfaceEntry entry = available[index];
        available.RemoveAt(index);
        DisposeEntry(entry);
    }

    private void ResetCore(bool poolDisposing)
    {
        frameGeneration = unchecked(frameGeneration + 1);
        retainedCache?.ResetFromPool(poolDisposing);
        if (poolDisposing)
        {
            retainedCache = null;
        }

        for (int index = 0; index < frameEntries.Length; index++)
        {
            SurfaceEntry? entry = frameEntries[index];
            if (entry is null)
            {
                continue;
            }

            frameEntries[index] = null;
            DisposeEntry(entry);
        }

        foreach (SurfaceEntry entry in available)
        {
            DisposeEntry(entry);
        }
        available.Clear();

        ActiveLeaseCount = 0;
        framePlan = null;
        frameStep = -1;
        retentionLimit = 0;
    }

    private void DisposeEntry(SurfaceEntry entry)
    {
        accountant.ReleaseTransient(entry.ByteCount);
        try
        {
            entry.Surface.Dispose();
        }
        finally
        {
            DisposedSurfaceCount++;
        }
    }

    private SurfaceEntry GetPromotableEntry(
        long generation,
        int executionIndex)
    {
        PrismGraphExecutionPlan plan =
            GetActivePlan(generation);
        ValidateExecutionIndex(plan, executionIndex);

        SurfaceEntry? entry = frameEntries[executionIndex];
        if (entry is null)
        {
            throw new InvalidOperationException(
                "Only a live Prism execution surface can be promoted.");
        }
        if (frameStep <
            plan.SurfaceLifetimes[executionIndex].LastStep)
        {
            throw new InvalidOperationException(
                "A Prism surface can be promoted only after its final planned use.");
        }
        if (entry.Surface.IsDisposed)
        {
            throw new ObjectDisposedException(
                nameof(RenderTarget2D),
                "A disposed Prism surface cannot be promoted.");
        }

        return entry;
    }

    private PrismGraphExecutionPlan GetActivePlan(long generation)
    {
        ThrowIfDisposed();
        if (framePlan is null || generation != frameGeneration)
        {
            throw new InvalidOperationException(
                "The Prism surface frame token is no longer active.");
        }

        return framePlan;
    }

    private static void ValidateExecutionIndex(
        PrismGraphExecutionPlan plan,
        int executionIndex)
    {
        if ((uint)executionIndex >= (uint)plan.ExecutionOrder.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(executionIndex));
        }
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(disposed, this);
    }

    private void OnDeviceResetting(object? sender, EventArgs eventArgs)
    {
        if (!disposed)
        {
            ResetCore(poolDisposing: false);
        }
    }

    private sealed class SurfaceEntry
    {
        private RenderTarget2D? surface;

        public SurfaceEntry(
            PrismSurfaceKey key,
            long byteCount)
        {
            Key = key;
            ByteCount = byteCount;
        }

        public SurfaceEntry(
            PrismSurfaceKey key,
            RenderTarget2D surface,
            long byteCount)
            : this(key, byteCount)
        {
            Attach(surface);
        }

        public PrismSurfaceKey Key { get; }

        public RenderTarget2D Surface =>
            surface ??
            throw new InvalidOperationException(
                "A Prism transient surface entry has no surface owner.");

        public long ByteCount { get; }

        public void Attach(RenderTarget2D ownedSurface)
        {
            ArgumentNullException.ThrowIfNull(ownedSurface);
            if (surface is not null)
            {
                throw new InvalidOperationException(
                    "A Prism transient surface entry already owns a surface.");
            }

            surface = ownedSurface;
        }
    }
}
