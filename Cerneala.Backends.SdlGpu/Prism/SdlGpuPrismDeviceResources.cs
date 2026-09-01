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

internal sealed class SdlGpuPrismDeviceResources : IDisposable
{
    private static readonly object WhiteTextureKey = new();
    private static readonly object SpatterPointTextureKey = new();
    private static readonly object GradientDitherTextureKey = new();
    private static readonly byte[] WhiteTexturePixels = [255, 255, 255, 255];
    private static readonly byte[] GradientDitherPixels = CreateGradientDitherPixels();
    private const int MaximumWaveNoiseEntryCount = 32;
    private readonly ISdlApi api;
    private readonly nint device;
    private readonly SdlGpuShaderFormats shaderFormats;
    private readonly SdlGpuDrawingResources drawingResources;
    private readonly PrismRendererOptions options;
    private readonly Dictionary<(SdlGpuTextureFormat, SdlGpuSampleCount), nint> pipelines = [];
    private readonly Dictionary<SurfaceKey, LinkedList<FreeSurfaceEntry>> freeSurfaces = [];
    private readonly Dictionary<PrismRetainedCacheKey, RetainedEntry> retained = [];
    private readonly List<PrismRetainedCacheKey> retainedKeysToRemove = [];
    private readonly HashSet<SdlGpuRenderTarget> allSurfaces = [];
    private readonly Dictionary<int, List<WaveNoiseEntry>> waveNoiseEntries = [];
    private readonly Dictionary<GradientOverlayKey, GradientOverlayEntry> gradientOverlays = [];
    private nint vertexShader;
    private nint fragmentShader;
    private long freeBytes;
    private long totalBytes;
    private long peakBytes;
    private long createdSurfaceCount;
    private long reusedSurfaceCount;
    private long useSequence;
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
    internal int FreeSurfaceCount => freeSurfaces.Values.Sum(
        static entries => entries.Count);
    internal int RetainedCount => retained.Count;

    public SdlGpuPrismSurfaceLease RentSurface(
        long windowId,
        int width,
        int height,
        SdlGpuTextureFormat format,
        bool mipmapped)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        uint mipLevelCount = mipmapped
            ? CalculateMipLevelCount(width, height)
            : 1;
        SurfaceKey key = new(width, height, format, mipLevelCount);
        if (freeSurfaces.TryGetValue(key, out LinkedList<FreeSurfaceEntry>? free) &&
            free.Last is LinkedListNode<FreeSurfaceEntry> last)
        {
            FreeSurfaceEntry entry = last.Value;
            free.RemoveLast();
            if (free.Count == 0)
            {
                freeSurfaces.Remove(key);
            }
            SdlGpuRenderTarget target = entry.Target;
            freeBytes -= EstimateBytes(
                target.PixelWidth,
                target.PixelHeight,
                target.ColorFormat,
                target.MipLevelCount);
            reusedSurfaceCount++;
            return new SdlGpuPrismSurfaceLease(this, target, windowId, retained: false);
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
            allSurfaces.Add(created);
            totalBytes = checked(totalBytes + byteCount);
            peakBytes = Math.Max(peakBytes, totalBytes);
            createdSurfaceCount++;
            return new SdlGpuPrismSurfaceLease(this, created, windowId, retained: false);
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
        ObjectDisposedException.ThrowIf(disposed, this);
        if (!retained.TryGetValue(key, out RetainedEntry? entry))
        {
            lease = null!;
            return false;
        }
        entry.PinCount++;
        entry.LastUse = ++useSequence;
        lease = new SdlGpuPrismSurfaceLease(this, entry.Target, windowId, retained: true, key);
        return true;
    }

    public void Promote(
        in PrismRetainedCacheKey key,
        SdlGpuPrismSurfaceLease lease)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        ArgumentNullException.ThrowIfNull(lease);
        if (retained.TryGetValue(key, out RetainedEntry? existing))
        {
            if (!ReferenceEquals(existing.Target, lease.Target) && existing.PinCount == 0)
            {
                retained.Remove(key);
                ReturnSurface(existing.Target);
            }
            else
            {
                return;
            }
        }

        lease.MarkRetained(key);
        retained.Add(key, new RetainedEntry(lease.Target, ++useSequence) { PinCount = 1 });
        TrimRetained();
    }

    public void Invalidate(PrismCacheInvalidation invalidation)
    {
        foreach (PrismRetainedCacheKey key in retained.Keys
            .Where(candidate => invalidation.Kind == PrismCacheInvalidationKind.All ||
                candidate.StableNodeId.ScopeOwnerToken == invalidation.OwnerToken)
            .ToArray())
        {
            RetainedEntry entry = retained[key];
            if (entry.PinCount != 0)
            {
                entry.Invalidated = true;
                continue;
            }
            retained.Remove(key);
            ReturnSurface(entry.Target);
        }
    }

    public void InvalidateStaleOwnerEntries(
        PrismCacheOwnerToken ownerToken,
        IReadOnlySet<PrismRetainedCacheKey> currentKeys)
    {
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
            if (entry.PinCount != 0)
            {
                entry.Invalidated = true;
                continue;
            }
            retained.Remove(key);
            ReturnSurface(entry.Target);
        }
    }

    public nint GetPipeline(SdlGpuTextureFormat format)
    {
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
                    SdlGpuColorWriteMask.All,
                    UsesVertexInput: false)),
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
                PrismCssGradientLut.Create(resource, interpolation, workingProfile).Values).Handle;
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
            textureKey);
        return drawingResources.GetOrCreateHalfVector4Texture(
            session,
            textureKey,
            PrismCssGradientLut.SampleCount,
            1,
            lut.Values).Handle;
    }

    public nint GetGradientDitherTexture(SdlGpuWindowGraphicsSession session)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        return drawingResources.GetOrCreateTexture(
            session,
            GradientDitherTextureKey,
            16,
            16,
            GradientDitherPixels).Handle;
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
        if (disposed)
        {
            return;
        }
        disposed = true;
        retained.Clear();
        retainedKeysToRemove.Clear();
        freeSurfaces.Clear();
        waveNoiseEntries.Clear();
        gradientOverlays.Clear();
        foreach (SdlGpuRenderTarget surface in allSurfaces)
        {
            drawingResources.RetireRenderTarget(surface);
        }
        allSurfaces.Clear();
        freeBytes = 0;
        totalBytes = 0;
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

    internal void Release(SdlGpuPrismSurfaceLease lease)
    {
        if (lease.IsRetained && lease.RetainedKey is PrismRetainedCacheKey key &&
            retained.TryGetValue(key, out RetainedEntry? entry) &&
            ReferenceEquals(entry.Target, lease.Target))
        {
            entry.PinCount--;
            if (entry.PinCount < 0)
            {
                throw new InvalidOperationException("SDL_GPU Prism retained surface pin count is unbalanced.");
            }
            if (entry.PinCount == 0 && entry.Invalidated)
            {
                retained.Remove(key);
                ReturnSurface(entry.Target);
            }
            return;
        }
        ReturnSurface(lease.Target);
    }

    private void ReturnSurface(SdlGpuRenderTarget target)
    {
        SurfaceKey key = new(
            target.PixelWidth,
            target.PixelHeight,
            target.ColorFormat,
            target.MipLevelCount);
        if (!freeSurfaces.TryGetValue(key, out LinkedList<FreeSurfaceEntry>? free))
        {
            free = new LinkedList<FreeSurfaceEntry>();
            freeSurfaces.Add(key, free);
        }
        free.AddLast(new FreeSurfaceEntry(target, ++useSequence));
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
        while (totalBytes + requestedBytes > options.SurfaceHardByteLimit && TryDestroyFreeSurface())
        {
        }
        if (totalBytes + requestedBytes > options.SurfaceHardByteLimit)
        {
            EvictOneRetained();
        }
        if (totalBytes + requestedBytes > options.SurfaceHardByteLimit)
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
        foreach ((SurfaceKey key, LinkedList<FreeSurfaceEntry> free) in freeSurfaces)
        {
            if (free.First is not LinkedListNode<FreeSurfaceEntry> first ||
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

        LinkedList<FreeSurfaceEntry> candidateList = freeSurfaces[candidateKey];
        candidateList.RemoveFirst();
        if (candidateList.Count == 0)
        {
            freeSurfaces.Remove(candidateKey);
        }
        SdlGpuRenderTarget target = candidate.Target;
        allSurfaces.Remove(target);
        long byteCount = EstimateBytes(
            target.PixelWidth,
            target.PixelHeight,
            target.ColorFormat,
            target.MipLevelCount);
        freeBytes -= byteCount;
        totalBytes -= byteCount;
        drawingResources.RetireRenderTarget(target);
        return true;
    }

    private void TrimRetained()
    {
        while (retained.Count > options.RetainedCacheEntryLimit ||
            RetainedBytes() > options.RetainedCacheSoftByteLimit)
        {
            if (!EvictOneRetained())
            {
                break;
            }
        }
    }

    private bool EvictOneRetained()
    {
        KeyValuePair<PrismRetainedCacheKey, RetainedEntry>? candidate = retained
            .Where(static pair => pair.Value.PinCount == 0)
            .OrderBy(static pair => pair.Value.LastUse)
            .Cast<KeyValuePair<PrismRetainedCacheKey, RetainedEntry>?>()
            .FirstOrDefault();
        if (candidate is null)
        {
            return false;
        }
        retained.Remove(candidate.Value.Key);
        ReturnSurface(candidate.Value.Value.Target);
        return true;
    }

    private long RetainedBytes() => retained.Values
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
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(width);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(height);
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

    private sealed class RetainedEntry(SdlGpuRenderTarget target, long lastUse)
    {
        public SdlGpuRenderTarget Target { get; } = target;
        public long LastUse { get; set; } = lastUse;
        public int PinCount { get; set; }
        public bool Invalidated { get; set; }
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
        object TextureKey);
}

internal sealed class SdlGpuPrismSurfaceLease : IDisposable
{
    private SdlGpuPrismDeviceResources? owner;

    internal SdlGpuPrismSurfaceLease(
        SdlGpuPrismDeviceResources owner,
        SdlGpuRenderTarget target,
        long windowId,
        bool retained,
        PrismRetainedCacheKey? retainedKey = null)
    {
        this.owner = owner;
        Target = target;
        WindowId = windowId;
        IsRetained = retained;
        RetainedKey = retainedKey;
    }

    public SdlGpuRenderTarget Target { get; }
    public long WindowId { get; }
    public bool IsRetained { get; private set; }
    public PrismRetainedCacheKey? RetainedKey { get; private set; }

    internal void MarkRetained(in PrismRetainedCacheKey key)
    {
        IsRetained = true;
        RetainedKey = key;
    }

    public void Dispose() =>
        Interlocked.Exchange(ref owner, null)?.Release(this);
}
