using Cerneala.Drawing;
using Cerneala.Drawing.Prism;
using Cerneala.Drawing.Prism.Catalog;
using Cerneala.Drawing.Prism.Filters;
using Cerneala.Drawing.Prism.Graph;
using Cerneala.Drawing.Prism.Styles;
using Cerneala.Drawing.Prism.Surfaces;
using Cerneala.Platforms.Sdl3;
using Cerneala.UI.Prism.Definitions;

namespace Cerneala.Backends.SdlGpu;

internal sealed class SdlGpuPrismDeviceResources :
    IDisposable,
    ISdlGpuCommandBufferParticipant
{
    private static readonly object WhiteTextureKey = new();
    private static readonly object SpatterPointTextureKey = new();
    private static readonly object GradientDitherTextureKey = new();
    private static readonly byte[] WhiteTexturePixels = [255, 255, 255, 255];
    private static readonly byte[] GradientDitherPixels = CreateGradientDitherPixels();
    private const int MaximumWaveNoiseEntryCount = 32;
    private readonly int ownerThreadId = Environment.CurrentManagedThreadId;
    private readonly ISdlApi api;
    private readonly nint device;
    private readonly SdlGpuShaderFormats shaderFormats;
    private readonly SdlGpuDrawingResources drawingResources;
    private readonly PrismRendererOptions options;
    private readonly Dictionary<(SdlGpuTextureFormat, SdlGpuSampleCount), nint> pipelines = [];
    private readonly Dictionary<SurfaceKey, SurfaceBucket> surfaceBuckets = [];
    private readonly Dictionary<PrismRetainedCacheKey, RetainedEntry> retained = [];
    private readonly Dictionary<PendingRetainedKey, RetainedEntry> pendingRetained = [];
    private readonly List<PendingRetainedKey> pendingRetainedKeysToInvalidate = [];
    private readonly Dictionary<SdlGpuCommandBufferToken, HashSet<RetainedEntry>> commandBufferPins = [];
    private readonly Stack<HashSet<RetainedEntry>> reusableCommandBufferPinSets = [];
    private readonly Dictionary<long, LeaseState> activeLeases = [];
    private readonly List<PrismRetainedCacheKey> retainedKeysToRemove = [];
    private readonly Dictionary<SdlGpuRenderTarget, LinkedListNode<FreeSurfaceEntry>> allSurfaces = [];
    private readonly Dictionary<int, List<WaveNoiseEntry>> waveNoiseEntries = [];
    private readonly Dictionary<GradientOverlayKey, GradientOverlayEntry> gradientOverlays = [];
    private readonly Dictionary<PrismResourceId, CurvesEntry> curves = [];
    private nint vertexShader;
    private nint fragmentShader;
    private long freeBytes;
    private long totalBytes;
    private long peakBytes;
    private long createdSurfaceCount;
    private long reusedSurfaceCount;
    private long useSequence;
    private long leaseSequence;
    private bool disposed;

    public SdlGpuPrismDeviceResources(
        ISdlApi api,
        nint device,
        SdlGpuShaderFormats shaderFormats,
        SdlGpuDrawingResources drawingResources,
        PrismRendererOptions options)
    {
        this.api = api;
        this.device = device;
        this.shaderFormats = shaderFormats;
        this.drawingResources = drawingResources;
        this.options = options;
        options.Validate();
    }

    internal long TotalBytes => totalBytes;
    internal long PeakBytes => peakBytes;
    internal long FreeBytes => freeBytes;
    internal long CreatedSurfaceCount => createdSurfaceCount;
    internal long ReusedSurfaceCount => reusedSurfaceCount;
    internal int FreeSurfaceCount => surfaceBuckets.Values.Sum(
        static bucket => bucket.Free.Count);
    internal int RetainedCount => retained.Count + pendingRetained.Count;

    public SdlGpuPrismSurfaceLease RentSurface(
        long windowId,
        int width,
        int height,
        SdlGpuTextureFormat format,
        bool mipmapped)
    {
        VerifyAccess();
        ObjectDisposedException.ThrowIf(disposed, this);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(width);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(height);
        uint mipLevelCount = mipmapped
            ? CalculateMipLevelCount(width, height)
            : 1;
        SurfaceKey key = new(width, height, format, mipLevelCount);
        if (surfaceBuckets.TryGetValue(key, out SurfaceBucket? bucket) &&
            bucket.Free.Last is LinkedListNode<FreeSurfaceEntry> last)
        {
            FreeSurfaceEntry entry = last.Value;
            bucket.Free.RemoveLast();
            SdlGpuRenderTarget target = entry.Target;
            freeBytes -= EstimateBytes(
                target.PixelWidth,
                target.PixelHeight,
                target.ColorFormat,
                target.MipLevelCount);
            reusedSurfaceCount++;
            return CreateLease(target, windowId);
        }

        // Reusable buckets already passed admission. Preserve the distinction
        // between unknown formats and defined formats unsupported by EstimateBytes.
        if (format is not (SdlGpuTextureFormat.Invalid or SdlGpuTextureFormat.R8G8B8A8Unorm or
            SdlGpuTextureFormat.B8G8R8A8Unorm or SdlGpuTextureFormat.R16G16B16A16Float or
            SdlGpuTextureFormat.R32Float or SdlGpuTextureFormat.R32G32B32A32Float or
            SdlGpuTextureFormat.R8G8B8A8UnormSrgb or SdlGpuTextureFormat.B8G8R8A8UnormSrgb or
            SdlGpuTextureFormat.D24UnormS8Uint))
        {
            throw new ArgumentOutOfRangeException(nameof(format), format, "Unknown SDL_GPU surface format.");
        }
        long byteCount = EstimateBytes(width, height, format, mipLevelCount);
        EnsureBudget(byteCount);
        try
        {
            SdlGpuRenderTarget created = drawingResources.CreateRenderTarget(
                width,
                height,
                format,
                SdlGpuSampleCount.One,
                mipLevelCount,
                useDepthStencil: false);
            // Budget enforcement may have retired the last surface in this bucket.
            if (!surfaceBuckets.TryGetValue(key, out bucket))
            {
                bucket = new SurfaceBucket();
                surfaceBuckets.Add(key, bucket);
            }
            allSurfaces.Add(created, new LinkedListNode<FreeSurfaceEntry>(new(created, 0)));
            bucket.SurfaceCount++;
            totalBytes = checked(totalBytes + byteCount);
            peakBytes = Math.Max(peakBytes, totalBytes);
            createdSurfaceCount++;
            return CreateLease(created, windowId);
        }
        catch (Exception exception)
        {
            throw new PrismSurfaceAllocationException(
                key.ToString(),
                byteCount,
                totalBytes,
                options.SurfaceHardByteLimit,
                exception);
        }
    }

    public bool TryAcquireRetained(
        in PrismRetainedCacheKey key,
        long windowId,
        out SdlGpuPrismSurfaceLease lease)
    {
        VerifyAccess();
        ObjectDisposedException.ThrowIf(disposed, this);
        if (!retained.TryGetValue(key, out RetainedEntry? entry) || entry.Invalidated)
        {
            lease = default;
            return false;
        }
        lease = CreateLease(entry.Target, windowId, key);
        entry.PinCount++;
        entry.LastUse = ++useSequence;
        return true;
    }

    internal bool TryAcquireRetained(
        SdlGpuWindowGraphicsSession session,
        in PrismRetainedCacheKey key,
        long windowId,
        out SdlGpuPrismSurfaceLease lease)
    {
        ArgumentNullException.ThrowIfNull(session);
        VerifyAccess();
        ObjectDisposedException.ThrowIf(disposed, this);
        SdlGpuCommandBufferToken token = session.ActiveCommandBufferToken;
        PendingRetainedKey pendingKey = new(key, token);
        if (pendingRetained.TryGetValue(pendingKey, out RetainedEntry? pending) &&
            !pending.Invalidated)
        {
            lease = CreateLease(pending.Target, windowId, pending);
            pending.PinCount++;
            pending.LastUse = ++useSequence;
            PinForCommandBuffer(session, token, pending);
            return true;
        }
        if (!retained.TryGetValue(key, out RetainedEntry? submitted) || submitted.Invalidated)
        {
            lease = default;
            return false;
        }
        lease = CreateLease(submitted.Target, windowId, submitted);
        submitted.PinCount++;
        submitted.LastUse = ++useSequence;
        PinForCommandBuffer(session, token, submitted);
        return true;
    }

    public void Promote(
        in PrismRetainedCacheKey key,
        SdlGpuPrismSurfaceLease lease)
    {
        VerifyAccess();
        ObjectDisposedException.ThrowIf(disposed, this);
        RequireActiveLease(lease);
        long byteCount = EstimateBytes(
            lease.Target.PixelWidth,
            lease.Target.PixelHeight,
            lease.Target.ColorFormat,
            lease.Target.MipLevelCount);
        if (options.RetainedCacheEntryLimit == 0 || byteCount > options.RetainedCacheSoftByteLimit)
        {
            return;
        }
        if (retained.TryGetValue(key, out RetainedEntry? existing))
        {
            if (!ReferenceEquals(existing.Target, lease.Target) && !IsPinned(existing))
            {
                retained.Remove(key);
                existing.Released = true;
                ReturnSurface(existing.Target);
            }
            else
            {
                return;
            }
        }

        while (retained.Count >= options.RetainedCacheEntryLimit ||
            RetainedBytes() > options.RetainedCacheSoftByteLimit - byteCount)
        {
            if (!EvictOneRetained())
            {
                return;
            }
        }
        RetainedEntry entry = new(key, lease.Target, ++useSequence) { PinCount = 1 };
        activeLeases[lease.Id] = new LeaseState(lease.Target, entry);
        retained.Add(key, entry);
    }

    internal void Promote(
        SdlGpuWindowGraphicsSession session,
        in PrismRetainedCacheKey key,
        SdlGpuPrismSurfaceLease lease)
    {
        ArgumentNullException.ThrowIfNull(session);
        VerifyAccess();
        ObjectDisposedException.ThrowIf(disposed, this);
        RequireActiveLease(lease);
        SdlGpuCommandBufferToken token = session.ActiveCommandBufferToken;
        PendingRetainedKey pendingKey = new(key, token);
        if (pendingRetained.TryGetValue(pendingKey, out RetainedEntry? existingPending))
        {
            if (ReferenceEquals(existingPending.Target, lease.Target))
            {
                return;
            }

            return;
        }

        long byteCount = EstimateBytes(
            lease.Target.PixelWidth,
            lease.Target.PixelHeight,
            lease.Target.ColorFormat,
            lease.Target.MipLevelCount);
        if (options.RetainedCacheEntryLimit == 0 || byteCount > options.RetainedCacheSoftByteLimit)
        {
            return;
        }

        while (retained.Count + pendingRetained.Count >= options.RetainedCacheEntryLimit ||
            RetainedBytes() > options.RetainedCacheSoftByteLimit - byteCount)
        {
            if (!EvictOneRetained())
            {
                return;
            }
        }

        RetainedEntry entry = new(key, lease.Target, ++useSequence) { PinCount = 1 };
        activeLeases[lease.Id] = new LeaseState(lease.Target, entry);
        pendingRetained.Add(pendingKey, entry);
        PinForCommandBuffer(session, token, entry);
    }

    public void Invalidate(PrismCacheInvalidation invalidation)
    {
        VerifyAccess();
        ObjectDisposedException.ThrowIf(disposed, this);
        foreach (PrismRetainedCacheKey key in retained.Keys
            .Where(candidate => invalidation.Kind == PrismCacheInvalidationKind.All ||
                candidate.StableNodeId.ScopeOwnerToken == invalidation.OwnerToken)
            .ToArray())
        {
            RetainedEntry entry = retained[key];
            entry.Invalidated = true;
            ReleaseInvalidatedEntryIfUnpinned(entry);
        }
        foreach (PendingRetainedKey key in pendingRetained.Keys
            .Where(candidate => invalidation.Kind == PrismCacheInvalidationKind.All ||
                candidate.Key.StableNodeId.ScopeOwnerToken == invalidation.OwnerToken)
            .ToArray())
        {
            RetainedEntry entry = pendingRetained[key];
            entry.Invalidated = true;
        }
    }

    public void InvalidateStaleOwnerEntries(
        PrismCacheOwnerToken ownerToken,
        IReadOnlySet<PrismRetainedCacheKey> currentKeys)
    {
        VerifyAccess();
        ObjectDisposedException.ThrowIf(disposed, this);
        retainedKeysToRemove.Clear();
        foreach (PrismRetainedCacheKey key in retained.Keys)
        {
            if (key.StableNodeId.ScopeOwnerToken == ownerToken &&
                !currentKeys.Contains(key))
            {
                retainedKeysToRemove.Add(key);
            }
        }

        foreach (PrismRetainedCacheKey key in retainedKeysToRemove)
        {
            RetainedEntry entry = retained[key];
            entry.Invalidated = true;
            ReleaseInvalidatedEntryIfUnpinned(entry);
        }
        pendingRetainedKeysToInvalidate.Clear();
        foreach (PendingRetainedKey key in pendingRetained.Keys)
        {
            if (key.Key.StableNodeId.ScopeOwnerToken == ownerToken &&
                !currentKeys.Contains(key.Key))
            {
                pendingRetainedKeysToInvalidate.Add(key);
            }
        }
        foreach (PendingRetainedKey key in pendingRetainedKeysToInvalidate)
        {
            RetainedEntry entry = pendingRetained[key];
            entry.Invalidated = true;
        }
    }

    public nint GetPipeline(SdlGpuTextureFormat format)
    {
        VerifyAccess();
        ObjectDisposedException.ThrowIf(disposed, this);
        var key = (format, SdlGpuSampleCount.One);
        if (pipelines.TryGetValue(key, out nint pipeline))
        {
            return pipeline;
        }
        EnsureShaders();
        pipeline = RequireHandle(
            api.CreateGpuGraphicsPipeline(
                device,
                new SdlGpuGraphicsPipelineCreateInfo(
                    vertexShader,
                    fragmentShader,
                    format,
                    SdlGpuTextureFormat.Invalid,
                    SdlGpuSampleCount.One,
                    SdlGpuPrimitiveType.TriangleList,
                    SdlGpuBlendState.Opaque,
                    SdlGpuStencilMode.Disabled,
                    SdlGpuVertexInputDescription.Empty,
                    SdlGpuDepthState.Disabled,
                    SdlGpuColorWriteMask.All)),
            "SDL GPU Prism pipeline creation");
        pipelines.Add(key, pipeline);
        return pipeline;
    }

    public nint GetWhiteTexture(SdlGpuWindowGraphicsSession session) =>
        drawingResources.GetOrCreateTexture(
            session,
            WhiteTextureKey,
            1,
            1,
            WhiteTexturePixels).Handle;

    public nint GetWaveNoiseTexture(
        SdlGpuWindowGraphicsSession session,
        PrismWaveNoiseTable table)
    {
        VerifyAccess();
        ObjectDisposedException.ThrowIf(disposed, this);
        if (table.PackedSamples.Length != PrismWaveNoise.PackedTableSampleCount)
        {
            throw new InvalidOperationException(
                "Wave Noise texture creation requires a complete table.");
        }

        int hash = ContentHash(table);
        if (waveNoiseEntries.TryGetValue(hash, out List<WaveNoiseEntry>? bucket))
        {
            WaveNoiseEntry? existing = bucket.FirstOrDefault(entry => entry.Table == table);
            if (existing is not null)
            {
                return drawingResources.GetOrCreateHalfVector4Texture(
                    session,
                    existing.Key,
                    PrismWaveNoise.PackedTableSampleCount,
                    1,
                    table.PackedSamples.AsSpan()).Handle;
            }
        }

        if (waveNoiseEntries.Values.Sum(static entries => entries.Count) >=
            MaximumWaveNoiseEntryCount)
        {
            foreach (WaveNoiseEntry entry in waveNoiseEntries.Values.SelectMany(static entries => entries))
            {
                drawingResources.InvalidateTexture(entry.Key);
            }
            waveNoiseEntries.Clear();
            bucket = null;
        }

        object key = new();
        bucket ??= [];
        if (!waveNoiseEntries.ContainsKey(hash))
        {
            waveNoiseEntries.Add(hash, bucket);
        }
        bucket.Add(new WaveNoiseEntry(table, key));
        return drawingResources.GetOrCreateHalfVector4Texture(
            session,
            key,
            PrismWaveNoise.PackedTableSampleCount,
            1,
            table.PackedSamples.AsSpan()).Handle;
    }

    public nint GetSpatterPointTexture(SdlGpuWindowGraphicsSession session)
    {
        VerifyAccess();
        ObjectDisposedException.ThrowIf(disposed, this);
        PrismSpatterPointField field = PrismRecursiveWangBlueNoise.PointField;
        return drawingResources.GetOrCreateHalfVector4Texture(
            session,
            SpatterPointTextureKey,
            field.TextureWidth,
            field.GridSize,
            field.PackedPoints).Handle;
    }

    public nint GetGradientOverlayTexture(
        SdlGpuWindowGraphicsSession session,
        PrismResourceId id,
        PrismGradientMapResource resource,
        long identity,
        long version,
        PrismGradientInterpolation interpolation,
        PrismColorProfile workingProfile)
    {
        VerifyAccess();
        ObjectDisposedException.ThrowIf(disposed, this);
        GradientOverlayKey key = new(id, interpolation, workingProfile);
        if (gradientOverlays.TryGetValue(key, out GradientOverlayEntry? existing) &&
            ReferenceEquals(existing.Resource, resource) &&
            existing.Identity == identity &&
            existing.Version == version)
        {
            return drawingResources.GetOrCreateHalfVector4Texture(
                session,
                existing.TextureKey,
                PrismCssGradientLut.SampleCount,
                1,
                existing.Lut.Values).Handle;
        }

        if (existing is not null)
        {
            drawingResources.InvalidateTexture(existing.TextureKey);
        }
        PrismCssGradientLut lut = PrismCssGradientLut.Create(
            resource,
            interpolation,
            workingProfile);
        object textureKey = new();
        gradientOverlays[key] = new GradientOverlayEntry(
            resource,
            identity,
            version,
            textureKey,
            lut);
        return drawingResources.GetOrCreateHalfVector4Texture(
            session,
            textureKey,
            PrismCssGradientLut.SampleCount,
            1,
            lut.Values).Handle;
    }

    public nint GetGradientDitherTexture(SdlGpuWindowGraphicsSession session)
    {
        VerifyAccess();
        ObjectDisposedException.ThrowIf(disposed, this);
        return drawingResources.GetOrCreateTexture(
            session,
            GradientDitherTextureKey,
            16,
            16,
            GradientDitherPixels).Handle;
    }

    public nint GetCurvesTexture(
        SdlGpuWindowGraphicsSession session,
        PrismResourceId id,
        PrismCurvesResource resource,
        long identity,
        long version)
    {
        VerifyAccess();
        ObjectDisposedException.ThrowIf(disposed, this);
        if (!curves.TryGetValue(id, out CurvesEntry? entry) ||
            !ReferenceEquals(entry.Resource, resource) ||
            entry.Identity != identity || entry.Version != version)
        {
            if (entry is not null)
            {
                drawingResources.InvalidateTexture(entry.TextureKey);
            }
            entry = new CurvesEntry(resource, identity, version, new object(), PrismCurveLut.Create(resource));
            curves[id] = entry;
        }
        return drawingResources.GetOrCreateHalfVector4Texture(
            session, entry.TextureKey, PrismCurveLut.SampleCount, 1, entry.Lut.Values).Handle;
    }

    private static byte[] CreateGradientDitherPixels()
    {
        const int size = 16;
        byte[] pixels = new byte[size * size * 4];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                byte rank = (byte)PrismIncrementalVoronoiSet.Rank(x, y, 0);
                int offset = ((y * size) + x) * 4;
                pixels[offset] = rank;
                pixels[offset + 1] = rank;
                pixels[offset + 2] = rank;
                pixels[offset + 3] = byte.MaxValue;
            }
        }
        return pixels;
    }

    public void Dispose()
    {
        VerifyAccess();
        if (disposed)
        {
            return;
        }
        disposed = true;
        foreach (RetainedEntry entry in retained.Values
            .Concat(pendingRetained.Values)
            .Distinct()
            .ToArray())
        {
            entry.Invalidated = true;
            entry.RetireOnRelease = true;
            ReleaseInvalidatedEntryIfUnpinned(entry);
        }
        retained.Clear();
        pendingRetained.Clear();
        foreach (FreeSurfaceEntry entry in surfaceBuckets.Values.SelectMany(static bucket => bucket.Free).ToArray())
        {
            RetireSurface(entry.Target);
        }
        retainedKeysToRemove.Clear();
        waveNoiseEntries.Clear();
        gradientOverlays.Clear();
        curves.Clear();
        freeBytes = 0;
        foreach (nint pipeline in pipelines.Values)
        {
            api.ReleaseGpuGraphicsPipeline(device, pipeline);
        }
        pipelines.Clear();
        if (fragmentShader != 0)
        {
            api.ReleaseGpuShader(device, fragmentShader);
            fragmentShader = 0;
        }
        if (vertexShader != 0)
        {
            api.ReleaseGpuShader(device, vertexShader);
            vertexShader = 0;
        }
    }

    internal void VerifyAccess()
    {
        if (Environment.CurrentManagedThreadId != ownerThreadId)
        {
            throw new InvalidOperationException("SDL_GPU Prism resources must be accessed on their owning thread.");
        }
    }

    internal void Release(SdlGpuPrismSurfaceLease lease)
    {
        VerifyAccess();
        if (!activeLeases.Remove(lease.Id, out LeaseState state))
        {
            return;
        }
        if (state.Entry is RetainedEntry entry)
        {
            entry.PinCount--;
            if (entry.PinCount < 0)
            {
                throw new InvalidOperationException("SDL_GPU Prism retained surface pin count is unbalanced.");
            }
            ReleaseInvalidatedEntryIfUnpinned(entry);
            return;
        }
        ReturnSurface(state.Target);
    }

    internal PrismRetainedCacheKey? GetRetainedKey(long leaseId)
    {
        VerifyAccess();
        return activeLeases.TryGetValue(leaseId, out LeaseState state) ? state.Entry?.Key : null;
    }

    private SdlGpuPrismSurfaceLease CreateLease(
        SdlGpuRenderTarget target,
        long windowId,
        PrismRetainedCacheKey? retainedKey = null)
    {
        RetainedEntry? entry = retainedKey is PrismRetainedCacheKey key
            ? retained.GetValueOrDefault(key)
            : null;
        return CreateLease(target, windowId, entry);
    }

    private SdlGpuPrismSurfaceLease CreateLease(
        SdlGpuRenderTarget target,
        long windowId,
        RetainedEntry? entry)
    {
        // Never recycle an acquisition identity: old value copies must stay inert.
        long id = checked(++leaseSequence);
        activeLeases.Add(id, new LeaseState(target, entry));
        return new SdlGpuPrismSurfaceLease(this, id, target, windowId);
    }

    private void RequireActiveLease(SdlGpuPrismSurfaceLease lease)
    {
        if (!ReferenceEquals(lease.Owner, this))
        {
            throw new ArgumentException("The surface lease belongs to another SDL_GPU Prism resource owner.", nameof(lease));
        }
        ObjectDisposedException.ThrowIf(!activeLeases.ContainsKey(lease.Id), typeof(SdlGpuPrismSurfaceLease));
    }

    private void PinForCommandBuffer(
        SdlGpuWindowGraphicsSession session,
        SdlGpuCommandBufferToken token,
        RetainedEntry entry)
    {
        if (!commandBufferPins.TryGetValue(token, out HashSet<RetainedEntry>? entries))
        {
            entries = reusableCommandBufferPinSets.Count > 0
                ? reusableCommandBufferPinSets.Pop()
                : [];
            commandBufferPins.Add(token, entries);
        }
        if (entries.Add(entry))
        {
            entry.CommandBufferPinCount++;
        }
        session.RegisterCommandBufferParticipant(this);
    }

    private void ReleaseCommandBufferPins(SdlGpuCommandBufferToken token)
    {
        if (!commandBufferPins.Remove(token, out HashSet<RetainedEntry>? entries))
        {
            return;
        }
        foreach (RetainedEntry entry in entries)
        {
            entry.CommandBufferPinCount--;
            if (entry.CommandBufferPinCount < 0)
            {
                throw new InvalidOperationException(
                    "SDL_GPU Prism command-buffer pin count is unbalanced.");
            }
            ReleaseInvalidatedEntryIfUnpinned(entry);
        }
        entries.Clear();
        reusableCommandBufferPinSets.Push(entries);
    }

    private static bool IsPinned(RetainedEntry entry) =>
        entry.PinCount != 0 || entry.CommandBufferPinCount != 0;

    private void ReleaseInvalidatedEntryIfUnpinned(RetainedEntry entry)
    {
        if (!entry.Invalidated || entry.Released || IsPinned(entry))
        {
            return;
        }
        if (retained.TryGetValue(entry.Key, out RetainedEntry? submitted) &&
            ReferenceEquals(submitted, entry))
        {
            retained.Remove(entry.Key);
        }
        PendingRetainedKey? pendingKeyToRemove = null;
        foreach ((PendingRetainedKey pendingKey, RetainedEntry pendingEntry) in pendingRetained)
        {
            if (ReferenceEquals(pendingEntry, entry))
            {
                pendingKeyToRemove = pendingKey;
                break;
            }
        }
        if (pendingKeyToRemove is PendingRetainedKey matchedPendingKey)
        {
            pendingRetained.Remove(matchedPendingKey);
        }
        entry.Released = true;
        if (entry.RetireOnRelease || disposed)
        {
            RetireSurface(entry.Target);
        }
        else
        {
            ReturnSurface(entry.Target);
        }
    }

    private void ReturnSurface(SdlGpuRenderTarget target)
    {
        if (disposed)
        {
            RetireSurface(target);
            return;
        }
        SurfaceKey key = new(
            target.PixelWidth,
            target.PixelHeight,
            target.ColorFormat,
            target.MipLevelCount);
        LinkedListNode<FreeSurfaceEntry> node = allSurfaces[target];
        node.Value = new FreeSurfaceEntry(target, ++useSequence);
        surfaceBuckets[key].Free.AddLast(node);
        freeBytes += EstimateBytes(
            target.PixelWidth,
            target.PixelHeight,
            target.ColorFormat,
            target.MipLevelCount);
        while (freeBytes > options.RetainedCacheSoftByteLimit && TryDestroyFreeSurface())
        {
        }
    }

    private void EnsureBudget(long requestedBytes)
    {
        if (requestedBytes > options.SurfaceHardByteLimit)
        {
            throw new PrismSurfaceAllocationException(
                "SDL_GPU Prism surface",
                requestedBytes,
                totalBytes,
                options.SurfaceHardByteLimit,
                new InvalidOperationException("The requested surface exceeds the hard GPU budget."));
        }
        while (requestedBytes > options.SurfaceHardByteLimit - totalBytes)
        {
            if (!TryDestroyFreeSurface() && !EvictOneRetained())
            {
                break;
            }
        }
        if (requestedBytes > options.SurfaceHardByteLimit - totalBytes)
        {
            throw new PrismSurfaceAllocationException(
                "SDL_GPU Prism surface",
                requestedBytes,
                totalBytes,
                options.SurfaceHardByteLimit,
                new InvalidOperationException("The device Prism surface budget is exhausted."));
        }
    }

    private bool TryDestroyFreeSurface()
    {
        SurfaceKey candidateKey = default;
        FreeSurfaceEntry candidate = default;
        bool found = false;
        foreach ((SurfaceKey key, SurfaceBucket bucket) in surfaceBuckets)
        {
            if (bucket.Free.First is not LinkedListNode<FreeSurfaceEntry> first ||
                (found && first.Value.LastUse >= candidate.LastUse))
            {
                continue;
            }
            FreeSurfaceEntry entry = first.Value;
            candidateKey = key;
            candidate = entry;
            found = true;
        }
        if (!found)
        {
            return false;
        }

        surfaceBuckets[candidateKey].Free.RemoveFirst();
        SdlGpuRenderTarget target = candidate.Target;
        long byteCount = EstimateBytes(
            target.PixelWidth,
            target.PixelHeight,
            target.ColorFormat,
            target.MipLevelCount);
        freeBytes -= byteCount;
        RetireSurface(target);
        return true;
    }

    private void RetireSurface(SdlGpuRenderTarget target)
    {
        if (allSurfaces.Remove(target, out LinkedListNode<FreeSurfaceEntry>? node))
        {
            node.List?.Remove(node);
            SurfaceKey key = new(target.PixelWidth, target.PixelHeight, target.ColorFormat, target.MipLevelCount);
            SurfaceBucket bucket = surfaceBuckets[key];
            if (--bucket.SurfaceCount == 0)
            {
                surfaceBuckets.Remove(key);
            }
            totalBytes -= EstimateBytes(target.PixelWidth, target.PixelHeight,
                target.ColorFormat, target.MipLevelCount);
            drawingResources.RetireRenderTarget(target);
        }
    }

    private bool EvictOneRetained()
    {
        KeyValuePair<PrismRetainedCacheKey, RetainedEntry>? candidate = retained
            .Where(static pair => !IsPinned(pair.Value))
            .OrderBy(static pair => pair.Value.LastUse)
            .Cast<KeyValuePair<PrismRetainedCacheKey, RetainedEntry>?>()
            .FirstOrDefault();
        if (candidate is null)
        {
            return false;
        }
        retained.Remove(candidate.Value.Key);
        candidate.Value.Value.Released = true;
        ReturnSurface(candidate.Value.Value.Target);
        return true;
    }

    private long RetainedBytes() => retained.Values
        .Concat(pendingRetained.Values)
        .Select(static entry => EstimateBytes(
            entry.Target.PixelWidth,
            entry.Target.PixelHeight,
            entry.Target.ColorFormat,
            entry.Target.MipLevelCount))
        .Sum();

    private void EnsureShaders()
    {
        if (vertexShader != 0)
        {
            return;
        }
        vertexShader = SdlGpuShaderArtifacts.CreateShader(
            api,
            device,
            shaderFormats,
            SdlGpuShaderArtifacts.PrismVertex);
        try
        {
            fragmentShader = SdlGpuShaderArtifacts.CreateShader(
                api,
                device,
                shaderFormats,
                SdlGpuShaderArtifacts.PrismCatalogFragment);
        }
        catch
        {
            api.ReleaseGpuShader(device, vertexShader);
            vertexShader = 0;
            throw;
        }
    }

    private nint RequireHandle(nint handle, string operation) =>
        handle != 0 ? handle : throw SdlApiError.Create(api, operation);

    private static int ContentHash(PrismWaveNoiseTable table)
    {
        HashCode hash = new();
        hash.Add(table.Normalization);
        foreach (System.Numerics.Vector4 sample in table.PackedSamples)
        {
            hash.Add(sample);
        }
        return hash.ToHashCode();
    }

    private static uint CalculateMipLevelCount(int width, int height)
    {
        uint levels = 1;
        int dimension = Math.Max(width, height);
        while (dimension > 1)
        {
            dimension /= 2;
            levels++;
        }
        return levels;
    }

    private static long EstimateBytes(
        int width,
        int height,
        SdlGpuTextureFormat format,
        uint mipLevelCount)
    {
        long colorPixels = 0;
        int mipWidth = width;
        int mipHeight = height;
        for (uint level = 0; level < mipLevelCount; level++)
        {
            colorPixels = checked(colorPixels + ((long)mipWidth * mipHeight));
            mipWidth = Math.Max(1, mipWidth / 2);
            mipHeight = Math.Max(1, mipHeight / 2);
        }

        int colorBytesPerPixel = format switch
        {
            SdlGpuTextureFormat.R16G16B16A16Float => 8,
            SdlGpuTextureFormat.R32G32B32A32Float => 16,
            SdlGpuTextureFormat.R32Float => 4,
            SdlGpuTextureFormat.R8G8B8A8Unorm or
            SdlGpuTextureFormat.B8G8R8A8Unorm or
            SdlGpuTextureFormat.R8G8B8A8UnormSrgb or
            SdlGpuTextureFormat.B8G8R8A8UnormSrgb => 4,
            _ => throw new NotSupportedException(
                $"SDL_GPU Prism byte accounting does not support '{format}'.")
        };
        long colorBytes = checked(colorPixels * colorBytesPerPixel);
        return checked(colorPixels * colorBytesPerPixel);
    }

    private readonly record struct SurfaceKey(
        int Width,
        int Height,
        SdlGpuTextureFormat Format,
        uint MipLevelCount);

    private readonly record struct FreeSurfaceEntry(
        SdlGpuRenderTarget Target,
        long LastUse);

    private sealed class SurfaceBucket
    {
        public LinkedList<FreeSurfaceEntry> Free { get; } = new();
        public int SurfaceCount { get; set; }
    }

    public void OnCommandBufferSubmitted(SdlGpuCommandBufferToken token)
    {
        VerifyAccess();
        foreach (PendingRetainedKey pendingKey in pendingRetained.Keys
            .Where(candidate => candidate.Token == token)
            .ToArray())
        {
            RetainedEntry entry = pendingRetained[pendingKey];
            pendingRetained.Remove(pendingKey);
            if (entry.Invalidated)
            {
                continue;
            }

            if (retained.Remove(entry.Key, out RetainedEntry? replaced))
            {
                replaced.Invalidated = true;
                ReleaseInvalidatedEntryIfUnpinned(replaced);
            }
            retained.Add(entry.Key, entry);
        }
        ReleaseCommandBufferPins(token);
    }

    public void OnCommandBufferAbandoned(SdlGpuCommandBufferToken token)
    {
        VerifyAccess();
        foreach (PendingRetainedKey pendingKey in pendingRetained.Keys
            .Where(candidate => candidate.Token == token)
            .ToArray())
        {
            RetainedEntry entry = pendingRetained[pendingKey];
            pendingRetained.Remove(pendingKey);
            entry.Invalidated = true;
        }
        ReleaseCommandBufferPins(token);
    }

    private readonly record struct LeaseState(
        SdlGpuRenderTarget Target,
        RetainedEntry? Entry);

    private readonly record struct PendingRetainedKey(
        PrismRetainedCacheKey Key,
        SdlGpuCommandBufferToken Token);

    private sealed class RetainedEntry(
        PrismRetainedCacheKey key,
        SdlGpuRenderTarget target,
        long lastUse)
    {
        public PrismRetainedCacheKey Key { get; } = key;
        public SdlGpuRenderTarget Target { get; } = target;
        public long LastUse { get; set; } = lastUse;
        public int PinCount { get; set; }
        public int CommandBufferPinCount { get; set; }
        public bool Invalidated { get; set; }
        public bool RetireOnRelease { get; set; }
        public bool Released { get; set; }
    }

    private sealed record WaveNoiseEntry(
        PrismWaveNoiseTable Table,
        object Key);

    private readonly record struct GradientOverlayKey(
        PrismResourceId Id,
        PrismGradientInterpolation Interpolation,
        PrismColorProfile WorkingProfile);

    private sealed record GradientOverlayEntry(
        PrismGradientMapResource Resource,
        long Identity,
        long Version,
        object TextureKey,
        PrismCssGradientLut Lut);

    private sealed record CurvesEntry(
        PrismCurvesResource Resource,
        long Identity,
        long Version,
        object TextureKey,
        PrismCurveLut Lut);
}

internal readonly struct SdlGpuPrismSurfaceLease : IDisposable, IEquatable<SdlGpuPrismSurfaceLease>
{
    internal SdlGpuPrismSurfaceLease(
        SdlGpuPrismDeviceResources owner,
        long id,
        SdlGpuRenderTarget target,
        long windowId)
    {
        Owner = owner;
        Id = id;
        Target = target;
        WindowId = windowId;
    }

    internal SdlGpuPrismDeviceResources? Owner { get; }
    internal long Id { get; }
    public SdlGpuRenderTarget Target { get; }
    public long WindowId { get; }
    public bool IsRetained => RetainedKey.HasValue;
    public PrismRetainedCacheKey? RetainedKey => Owner?.GetRetainedKey(Id);

    public void Dispose() => Owner?.Release(this);

    public bool Equals(SdlGpuPrismSurfaceLease other) =>
        ReferenceEquals(Owner, other.Owner) && Id == other.Id;

    public override bool Equals(object? obj) => obj is SdlGpuPrismSurfaceLease other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(Owner, Id);
}
