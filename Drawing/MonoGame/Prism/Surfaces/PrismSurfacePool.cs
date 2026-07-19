using Cerneala.Drawing.Prism.Graph;
using Microsoft.Xna.Framework.Graphics;

namespace Cerneala.Drawing.MonoGame.Prism.Surfaces;

internal sealed class PrismSurfacePool : IDisposable
{
    private readonly GraphicsDevice graphicsDevice;
    private readonly List<SurfaceEntry> available = [];
    private SurfaceEntry?[] frameEntries = [];
    private PrismGraphExecutionPlan? framePlan;
    private long frameGeneration;
    private int frameStep = -1;
    private int retentionLimit;
    private bool disposed;

    public PrismSurfacePool(GraphicsDevice graphicsDevice)
    {
        ArgumentNullException.ThrowIfNull(graphicsDevice);
        if (graphicsDevice.IsDisposed)
        {
            throw new ObjectDisposedException(nameof(graphicsDevice));
        }

        this.graphicsDevice = graphicsDevice;
        graphicsDevice.DeviceResetting += OnDeviceResetting;
    }

    public int ActiveLeaseCount { get; private set; }

    public int AvailableSurfaceCount => available.Count;

    public int OwnedSurfaceCount =>
        ActiveLeaseCount + AvailableSurfaceCount;

    public int PeakActiveLeaseCount { get; private set; }

    public long CreatedSurfaceCount { get; private set; }

    public long ReusedSurfaceCount { get; private set; }

    public long DisposedSurfaceCount { get; private set; }

    public long PromotedSurfaceCount { get; private set; }

    public PrismSurfaceFrame BeginFrame(PrismGraphExecutionPlan plan)
    {
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
        TrimAvailableToLimit();
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
        ThrowIfDisposed();
        ResetCore();
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        graphicsDevice.DeviceResetting -= OnDeviceResetting;
        ResetCore();
        disposed = true;
    }

    internal void AdvanceToStep(
        long generation,
        int step,
        ReadOnlySpan<PrismSurfaceKey> surfaceKeys)
    {
        PrismGraphExecutionPlan plan = GetActivePlan(generation);
        if (surfaceKeys.Length != plan.ExecutionOrder.Length)
        {
            throw new ArgumentException(
                "Surface keys must align with the optimized execution order.",
                nameof(surfaceKeys));
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
        PrismGraphExecutionPlan plan = GetActivePlan(generation);
        ValidateExecutionIndex(plan, executionIndex);

        SurfaceEntry? entry = frameEntries[executionIndex];
        if (entry is null)
        {
            throw new InvalidOperationException(
                "Only a live Prism execution surface can be promoted.");
        }
        if (frameStep < plan.SurfaceLifetimes[executionIndex].LastStep)
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

        frameEntries[executionIndex] = null;
        ActiveLeaseCount--;
        PromotedSurfaceCount++;
        return new PrismRetainedSurface(entry.Key, entry.Surface);
    }

    internal void EndFrame(long generation)
    {
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

        RenderTarget2D surface;
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
        }
        catch (Exception exception) when (
            exception is InvalidOperationException or
                ArgumentException or
                OutOfMemoryException)
        {
            throw new PrismSurfaceAllocationException(key, exception);
        }
        CreatedSurfaceCount++;
        return new SurfaceEntry(key, surface);
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

    private void ResetCore()
    {
        frameGeneration = unchecked(frameGeneration + 1);

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
        entry.Surface.Dispose();
        DisposedSurfaceCount++;
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
            ResetCore();
        }
    }

    private sealed class SurfaceEntry
    {
        public SurfaceEntry(
            PrismSurfaceKey key,
            RenderTarget2D surface)
        {
            Key = key;
            Surface = surface;
        }

        public PrismSurfaceKey Key { get; }

        public RenderTarget2D Surface { get; }
    }
}
