using System.Runtime.InteropServices;
using Cerneala.Drawing;
using Cerneala.Drawing.Text;
using Cerneala.Platforms.Sdl3;

namespace Cerneala.Backends.SdlGpu;

internal sealed class SdlGpuDrawingResources :
    IDisposable,
    ISdlGpuCommandBufferParticipant
{
    private const uint InitialTransferCapacity = 64 * 1024;
    private const int TextAtlasDimension = 1024;
    private const int MaximumTextAtlasPages = 8;
    private const int MaximumIdleSampledTextures = 16;
    private const long MaximumIdleSampledTextureBytes = 1024 * 1024;
    private readonly ISdlApi api;
    private readonly nint device;
    private readonly SdlGpuShaderFormats supportedShaderFormats;
    private readonly Dictionary<SdlGpuPipelineKey, nint> pipelines = [];
    private readonly Dictionary<SdlGpuSamplerKey, nint> samplers = [];
    private readonly Dictionary<object, SdlGpuTextureResource> textures = [];
    private readonly Dictionary<PendingTextureKey, SdlGpuTextureResource> pendingTextures = [];
    private readonly int ownerThreadId = Environment.CurrentManagedThreadId;
    private readonly object imageInvalidationGate = new();
    private readonly Queue<SdlGpuImage> pendingImageInvalidations = new();
    private readonly HashSet<SdlGpuImage> subscribedImages = new(ReferenceEqualityComparer.Instance);
    private readonly Dictionary<object, int> textureReferenceCounts = [];
    private readonly List<SdlGpuTextureResource> idleSampledTextures = [];
    private long idleSampledTextureBytes;
    private readonly Dictionary<SdlGpuTextLayerTextureKey, SdlGpuTextAtlasEntry> textAtlasEntries = [];
    private readonly LinkedList<SdlGpuTextAtlasEntry> textAtlasRecency = new();
    private readonly Dictionary<long, HashSet<SdlGpuTextAtlasEntry>> activeTextAtlasFrames = [];
    private readonly Stack<HashSet<SdlGpuTextAtlasEntry>> unusedTextAtlasFrames = new();
    private readonly Dictionary<SdlGpuCommandBufferToken, HashSet<SdlGpuTextAtlasEntry>> pendingTextAtlasPins = [];
    private readonly Stack<HashSet<SdlGpuTextAtlasEntry>> unusedPendingTextAtlasPins = new();
    private readonly List<SdlRect> requiredTextAtlasUploadRegions = [];
    private readonly Dictionary<SdlGpuCommandBufferToken, HashSet<nint>> pendingSampledTexturePins = [];
    private readonly Stack<HashSet<nint>> unusedPendingSampledTexturePins = new();
    private readonly Dictionary<nint, int> sampledTexturePinCounts = [];
    private readonly Dictionary<nint, SdlGpuTextureResource> deferredIdleSampledTextures = [];
    private readonly List<SdlGpuTextAtlasPage> textAtlasPages = [];
    private readonly HashSet<SdlGpuTextAtlasPage> dirtyTextAtlasPages = [];
    private readonly Dictionary<SdlGpuLayerTargetKey, SdlGpuRenderTarget> layerTargets = [];
    private readonly HashSet<nint> ownedTextures = [];
    private readonly List<nint> retiredTextures = [];
    private readonly Dictionary<uint, nint> uploadTransferBuffers = [];
    private nint vertexShader;
    private nint fragmentShader;
    private nint prismPresentationShader;
    private nint imageDomainVertexShader;
    private nint imageDomainFragmentShader;
    private long nextTextAtlasFrameToken;
    private SdlGpuPrismDeviceResources? prismResources;
    private SdlGpuRenderSurface3DDeviceResources? surface3DResources;
    private bool disposed;

    public SdlGpuDrawingResources(
        ISdlApi api,
        nint device,
        SdlGpuShaderFormats supportedShaderFormats)
    {
        this.api = api ?? throw new ArgumentNullException(nameof(api));
        this.device = device != 0
            ? device
            : throw new ArgumentOutOfRangeException(nameof(device));
        this.supportedShaderFormats = supportedShaderFormats;
    }

    internal int PipelineCount => pipelines.Count;

    internal int SamplerCount => samplers.Count;

    internal int CachedTextureCount =>
        textures.Count + pendingTextures.Count + textAtlasPages.Count;

    internal int TextAtlasPageCount => textAtlasPages.Count;

    internal int TextAtlasEntryCount => textAtlasEntries.Count;

    internal bool HasPendingTextAtlasUploads => dirtyTextAtlasPages.Count != 0;

    internal int LayerTargetCount => layerTargets.Count;

    internal SdlGpuPrismDeviceResources PrismResources =>
        prismResources ??= new SdlGpuPrismDeviceResources(
            api,
            device,
            supportedShaderFormats,
            this,
            new Drawing.Prism.PrismRendererOptions
            {
                RetainedCacheSoftByteLimit = 32L * 1024 * 1024
            });

    internal SdlGpuRenderSurface3DDeviceResources Surface3DResources =>
        surface3DResources ??= new(api, device, supportedShaderFormats);

    public nint GetPipeline(
        SdlGpuTextureFormat colorFormat,
        SdlGpuSampleCount sampleCount,
        DrawPrimitiveTopology topology,
        DrawBlendMode blendMode,
        SdlGpuStencilMode stencilMode,
        SdlGpuColorWriteMask colorWriteMask = SdlGpuColorWriteMask.All,
        bool alphaMask = false,
        bool prismPresentation = false,
        bool pointClampImageDomain = false)
    {
        ThrowIfDisposed();
        if (pointClampImageDomain && prismPresentation)
        {
            throw new ArgumentException(
                "Prism presentation cannot use the image-domain drawing pipeline.",
                nameof(pointClampImageDomain));
        }
        if (pointClampImageDomain)
        {
            EnsureImageDomainShaders();
        }
        else
        {
            EnsureShaders();
        }
        SdlGpuPipelineKey key = new(
            colorFormat,
            sampleCount,
            topology,
            blendMode,
            stencilMode,
            colorWriteMask,
            alphaMask,
            prismPresentation,
            pointClampImageDomain);
        if (pipelines.TryGetValue(key, out nint cached))
        {
            return cached;
        }

        if (prismPresentation && prismPresentationShader == 0)
        {
            prismPresentationShader = SdlGpuShaderArtifacts.CreateShader(
                api, device, supportedShaderFormats,
                SdlGpuShaderArtifacts.PrismPresentationFragment);
        }
        SdlGpuGraphicsPipelineCreateInfo createInfo = new(
            pointClampImageDomain ? imageDomainVertexShader : vertexShader,
            pointClampImageDomain
                ? imageDomainFragmentShader
                : prismPresentation ? prismPresentationShader : fragmentShader,
            colorFormat,
            SdlGpuTextureFormat.D24UnormS8Uint,
            sampleCount,
            topology == DrawPrimitiveTopology.TriangleStrip
                ? SdlGpuPrimitiveType.TriangleStrip
                : SdlGpuPrimitiveType.TriangleList,
            alphaMask
                ? new SdlGpuBlendState(
                    SdlGpuBlendFactor.Zero, SdlGpuBlendFactor.SourceAlpha, SdlGpuBlendOperation.Add,
                    SdlGpuBlendFactor.Zero, SdlGpuBlendFactor.SourceAlpha, SdlGpuBlendOperation.Add)
                : ToBlendState(blendMode),
            stencilMode,
            pointClampImageDomain
                ? SdlGpuVertexInputDescription.Drawing2DImageDomain
                : SdlGpuVertexInputDescription.Drawing2D,
            SdlGpuDepthState.Disabled,
            colorWriteMask);
        nint pipeline = RequireHandle(
            api.CreateGpuGraphicsPipeline(device, createInfo),
            $"SDL GPU drawing pipeline creation ({key})");
        pipelines.Add(key, pipeline);
        return pipeline;
    }

    public nint GetSampler(DrawSamplingMode sampling, DrawAddressMode addressMode, bool anisotropic = false)
    {
        ThrowIfDisposed();
        SdlGpuSamplerKey key = new(sampling, addressMode, anisotropic);
        if (samplers.TryGetValue(key, out nint cached))
        {
            return cached;
        }

        SdlGpuSamplerCreateInfo createInfo = new(
            sampling == DrawSamplingMode.Point
                ? SdlGpuFilter.Nearest
                : SdlGpuFilter.Linear,
            addressMode == DrawAddressMode.Wrap
                ? SdlGpuSamplerAddressMode.Repeat
                : SdlGpuSamplerAddressMode.ClampToEdge,
            sampling == DrawSamplingMode.Point
                ? SdlGpuSamplerMipmapMode.Nearest
                : SdlGpuSamplerMipmapMode.Linear,
            EnableAnisotropy: anisotropic,
            MaxAnisotropy: anisotropic ? 4 : 1);
        nint sampler = RequireHandle(
            api.CreateGpuSampler(device, createInfo),
            $"SDL GPU drawing sampler creation ({key})");
        samplers.Add(key, sampler);
        return sampler;
    }

    public SdlGpuTextureResource? FindTexture(object key)
    {
        ThrowIfDisposed();
        if (textures.TryGetValue(key, out SdlGpuTextureResource? submitted))
        {
            return submitted;
        }

        return pendingTextures
            .FirstOrDefault(pair => Equals(pair.Key.Key, key))
            .Value;
    }

    internal SdlGpuTextureResource? FindTexture(
        SdlGpuWindowGraphicsSession session,
        object key)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(session);
        if (!session.TryGetActiveCommandBufferToken(out SdlGpuCommandBufferToken token))
        {
            return textures.GetValueOrDefault(key);
        }
        SdlGpuTextureResource? texture =
            pendingTextures.GetValueOrDefault(new PendingTextureKey(key, token)) ??
            textures.GetValueOrDefault(key);
        if (texture is not null)
        {
            PinSampledTexture(session, token, texture);
        }
        return texture;
    }

    public SdlGpuTextureResource GetOrCreateTexture(
        SdlGpuWindowGraphicsSession session,
        object key,
        int width,
        int height,
        ReadOnlySpan<byte> rgbaPixels,
        DrawPoint originOffset = default,
        bool recycleStorage = false)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(key);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(width);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(height);
        if (rgbaPixels.Length != checked(width * height * 4))
        {
            throw new ArgumentException(
                "Texture upload data must contain exactly width * height * 4 bytes.",
                nameof(rgbaPixels));
        }

        if (FindTexture(session, key) is SdlGpuTextureResource cached)
        {
            return cached;
        }

        return CreateSampledTexture(
            session,
            key,
            width,
            height,
            SdlGpuTextureFormat.R8G8B8A8Unorm,
            rgbaPixels,
            originOffset,
            recycleStorage);
    }

    public SdlGpuTextureResource GetOrCreateHalfVector4Texture(
        SdlGpuWindowGraphicsSession session,
        object key,
        int width,
        int height,
        ReadOnlySpan<System.Numerics.Vector4> pixels)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(key);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(width);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(height);
        if (pixels.Length != checked(width * height))
        {
            throw new ArgumentException(
                "Half-vector texture upload data must contain exactly width * height values.",
                nameof(pixels));
        }
        if (FindTexture(session, key) is SdlGpuTextureResource cached)
        {
            return cached;
        }

        Half[] components = new Half[checked(pixels.Length * 4)];
        for (int index = 0; index < pixels.Length; index++)
        {
            System.Numerics.Vector4 pixel = pixels[index];
            int component = index * 4;
            components[component] = (Half)pixel.X;
            components[component + 1] = (Half)pixel.Y;
            components[component + 2] = (Half)pixel.Z;
            components[component + 3] = (Half)pixel.W;
        }

        return CreateSampledTexture(
            session,
            key,
            width,
            height,
            SdlGpuTextureFormat.R16G16B16A16Float,
            MemoryMarshal.AsBytes(components.AsSpan()));
    }

    private SdlGpuTextureResource CreateSampledTexture(
        SdlGpuWindowGraphicsSession session,
        object key,
        int width,
        int height,
        SdlGpuTextureFormat format,
        ReadOnlySpan<byte> pixels,
        DrawPoint originOffset = default,
        bool recycleStorage = false)
    {
        if (FindTexture(session, key) is SdlGpuTextureResource cached)
        {
            return cached;
        }

        SdlGpuTextureCreateInfo createInfo = new(
            format,
            SdlGpuTextureUsage.Sampler,
            checked((uint)width),
            checked((uint)height));
        nint texture = recycleStorage ? TakeIdleSampledTexture(width, height) : 0;
        if (texture == 0)
        {
            texture = RequireHandle(
                api.CreateGpuTexture(device, createInfo),
                "SDL GPU sampled-texture creation");
        }
        ownedTextures.Add(texture);
        try
        {
            UploadTexture(session, texture, width, height, pixels);
            SdlGpuTextureResource created = new(texture, width, height, originOffset)
            {
                CanRecycleStorage = recycleStorage
            };
            PendingTextureKey pendingKey = new(key, session.ActiveCommandBufferToken);
            pendingTextures.Add(pendingKey, created);
            session.RegisterCommandBufferParticipant(this);
            PinSampledTexture(session, pendingKey.Token, created);
            // The device, not the uploading window, owns this texture. This
            // covers ordinary drawing and direct resource uploads (e.g. Prism).
            if (key is SdlGpuImage image && subscribedImages.Add(image))
            {
                image.ContentChanged += OnImageContentChanged;
            }
            return created;
        }
        catch
        {
            ownedTextures.Remove(texture);
            api.ReleaseGpuTexture(device, texture);
            throw;
        }
    }

    public long BeginTextAtlasFrame()
    {
        ThrowIfDisposed();
        long token = checked(++nextTextAtlasFrameToken);
        activeTextAtlasFrames.Add(token, unusedTextAtlasFrames.TryPop(out var entries) ? entries : []);
        return token;
    }

    public void EndTextAtlasFrame(long frameToken)
    {
        if (frameToken == 0)
        {
            return;
        }

        // Ending one frame releases only its pins. Another window may still
        // hold queued UVs into the same entry, not just the same page.
        if (activeTextAtlasFrames.Remove(frameToken, out var entries))
        {
            foreach (SdlGpuTextAtlasEntry entry in entries)
            {
                entry.ActiveFrameCount--;
            }
            entries.Clear();
            unusedTextAtlasFrames.Push(entries);
        }
    }

    public bool TryGetTextAtlasEntries(
        SdlGpuTextLayerTextureKey redKey,
        SdlGpuTextLayerTextureKey greenKey,
        SdlGpuTextLayerTextureKey blueKey,
        long frameToken,
        out SdlGpuTextAtlasEntries entries)
    {
        ThrowIfDisposed();
        if (!textAtlasEntries.TryGetValue(redKey, out SdlGpuTextAtlasEntry? red) ||
            !textAtlasEntries.TryGetValue(greenKey, out SdlGpuTextAtlasEntry? green) ||
            !textAtlasEntries.TryGetValue(blueKey, out SdlGpuTextAtlasEntry? blue))
        {
            entries = default;
            return false;
        }

        MarkTextAtlasEntryUsed(red, frameToken);
        MarkTextAtlasEntryUsed(green, frameToken);
        MarkTextAtlasEntryUsed(blue, frameToken);
        entries = new SdlGpuTextAtlasEntries(red, green, blue);
        return true;
    }

    public SdlGpuTextAtlasEntries? GetOrCreateTextAtlasEntries(
        SdlGpuWindowGraphicsSession session,
        SdlGpuTextLayerTextureKey redKey,
        SdlGpuTextLayerTextureKey greenKey,
        SdlGpuTextLayerTextureKey blueKey,
        RasterizedText[] layers,
        long frameToken)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(layers);
        if (layers.Length != 3)
        {
            throw new ArgumentException(
                "Subpixel text atlases require exactly three raster layers.",
                nameof(layers));
        }
        bool hasCommandBuffer = session.TryGetActiveCommandBufferToken(
            out SdlGpuCommandBufferToken token);
        bool found = hasCommandBuffer
            ? TryGetTextAtlasEntries(
                session,
                redKey,
                greenKey,
                blueKey,
                frameToken,
                out SdlGpuTextAtlasEntries cached)
            : TryGetTextAtlasEntries(
                redKey,
                greenKey,
                blueKey,
                frameToken,
                out cached);
        if (found)
        {
            return cached;
        }

        if (!TryGetOrCreateTextAtlasEntry(redKey, layers[0], frameToken, token, out SdlGpuTextAtlasEntry red) ||
            !TryGetOrCreateTextAtlasEntry(greenKey, layers[1], frameToken, token, out SdlGpuTextAtlasEntry green) ||
            !TryGetOrCreateTextAtlasEntry(blueKey, layers[2], frameToken, token, out SdlGpuTextAtlasEntry blue))
        {
            return null;
        }

        SdlGpuTextAtlasEntries entries = new(red, green, blue);
        if (token.Session is not null)
        {
            PinTextAtlasEntries(session, token, entries);
        }
        return entries;
    }

    private bool TryGetOrCreateTextAtlasEntry(
        SdlGpuTextLayerTextureKey key,
        RasterizedText layer,
        long frameToken,
        SdlGpuCommandBufferToken token,
        out SdlGpuTextAtlasEntry entry)
    {
        if (textAtlasEntries.TryGetValue(key, out SdlGpuTextAtlasEntry? existing))
        {
            if (token.Session is not null)
            {
                existing.Page.RequireUpload(token, existing.CreatedRevision);
            }
            MarkTextAtlasEntryUsed(existing, frameToken);
            entry = existing;
            return true;
        }
        if (!TryAddTextAtlasEntry(key, layer, frameToken, token, out entry))
        {
            return false;
        }
        dirtyTextAtlasPages.Add(entry.Page);
        return true;
    }

    private void MarkTextAtlasEntryUsed(SdlGpuTextAtlasEntry entry, long frameToken)
    {
        if (frameToken != 0 && activeTextAtlasFrames[frameToken].Add(entry))
        {
            entry.ActiveFrameCount++;
        }
        textAtlasRecency.Remove(entry.RecencyNode);
        textAtlasRecency.AddLast(entry.RecencyNode);
    }

    public void FlushTextAtlasUploads(SdlGpuWindowGraphicsSession session)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(session);
        SdlGpuCommandBufferToken token = session.ActiveCommandBufferToken;
        bool recordedUpload = false;
        foreach (SdlGpuTextAtlasPage page in textAtlasPages)
        {
            if (!page.CollectRequiredUploadRegions(
                    token,
                    requiredTextAtlasUploadRegions,
                    out long revision))
            {
                continue;
            }

            PinTextAtlasEntriesCoveredByUploads(
                session,
                token,
                page,
                requiredTextAtlasUploadRegions);
            foreach (SdlRect dirtyRegion in requiredTextAtlasUploadRegions)
            {
                UploadTextureRegion(
                    session,
                    page.Texture.Handle,
                    TextAtlasDimension,
                    TextAtlasDimension,
                    page.Pixels,
                    dirtyRegion,
                    bytesPerPixel: 4);
            }
            page.MarkUploadRecorded(token, revision);
            recordedUpload = true;
        }
        if (recordedUpload)
        {
            session.RegisterCommandBufferParticipant(this);
        }
    }

    public void InvalidateTexture(object key)
    {
        if (key is SdlGpuImage image && subscribedImages.Remove(image))
        {
            image.ContentChanged -= OnImageContentChanged;
        }
        if (textures.Remove(key, out SdlGpuTextureResource? texture))
        {
            RetireTexture(texture.Handle);
        }
        foreach (PendingTextureKey pendingKey in pendingTextures.Keys
            .Where(candidate => Equals(candidate.Key, key))
            .ToArray())
        {
            RetireTexture(pendingTextures[pendingKey].Handle);
            pendingTextures.Remove(pendingKey);
        }
    }

    internal void ProcessImageInvalidations()
    {
        while (true)
        {
            SdlGpuImage? image;
            lock (imageInvalidationGate)
            {
                if (!pendingImageInvalidations.TryDequeue(out image)) { return; }
            }
            InvalidateTexture(image);
        }
    }

    private void OnImageContentChanged(object? sender, EventArgs args)
    {
        if (sender is not SdlGpuImage image) { return; }
        lock (imageInvalidationGate)
        {
            if (disposed) { return; }
            if (Environment.CurrentManagedThreadId != ownerThreadId)
            {
                pendingImageInvalidations.Enqueue(image);
                return;
            }
        }
        InvalidateTexture(image);
    }

    public void RetainTexture(object key)
    {
        ThrowIfDisposed();
        if (!textures.ContainsKey(key) &&
            !pendingTextures.Keys.Any(candidate => Equals(candidate.Key, key)))
        {
            throw new InvalidOperationException("Cannot retain an uncached SDL_GPU texture.");
        }
        textureReferenceCounts.TryGetValue(key, out int count);
        textureReferenceCounts[key] = checked(count + 1);
    }

    internal void RetainTexture(SdlGpuWindowGraphicsSession session, object key)
    {
        if (FindTexture(session, key) is null)
        {
            throw new InvalidOperationException("Cannot retain an uncached SDL_GPU texture.");
        }
        textureReferenceCounts.TryGetValue(key, out int count);
        textureReferenceCounts[key] = checked(count + 1);
    }

    public void ReleaseTexture(object key)
    {
        ThrowIfDisposed();
        if (!textureReferenceCounts.TryGetValue(key, out int count))
        {
            throw new InvalidOperationException("Cannot release an unretained SDL_GPU texture.");
        }
        if (count > 1)
        {
            textureReferenceCounts[key] = count - 1;
            return;
        }
        textureReferenceCounts.Remove(key);
        if (textures.Remove(key, out SdlGpuTextureResource? texture))
        {
            if (textureReferenceCounts.Count == 0 || !texture.CanRecycleStorage)
            {
                RetireTexture(texture.Handle);
            }
            else
            {
                ReturnIdleSampledTexture(texture);
            }
        }
        foreach (PendingTextureKey pendingKey in pendingTextures.Keys
            .Where(candidate => Equals(candidate.Key, key))
            .ToArray())
        {
            RetireTexture(pendingTextures[pendingKey].Handle);
            pendingTextures.Remove(pendingKey);
        }
        if (textureReferenceCounts.Count == 0)
        {
            foreach (SdlGpuTextureResource idle in idleSampledTextures)
            {
                RetireTexture(idle.Handle);
            }
            idleSampledTextures.Clear();
            idleSampledTextureBytes = 0;
        }
    }

    private nint TakeIdleSampledTexture(int width, int height)
    {
        for (int index = idleSampledTextures.Count - 1; index >= 0; index--)
        {
            SdlGpuTextureResource candidate = idleSampledTextures[index];
            if (candidate.Width != width || candidate.Height != height)
            {
                continue;
            }
            idleSampledTextures.RemoveAt(index);
            idleSampledTextureBytes -= SampledTextureBytes(candidate);
            return candidate.Handle;
        }
        return 0;
    }

    private void ReturnIdleSampledTexture(SdlGpuTextureResource texture)
    {
        if (sampledTexturePinCounts.ContainsKey(texture.Handle))
        {
            deferredIdleSampledTextures[texture.Handle] = texture;
            return;
        }
        // Only explicitly recyclable RGBA storage reaches this path after its
        // last retained brush lease ends. Other textures keep their own lifetime
        // policies, including the bounded histories of text masks and captures.
        // The releasing backend has recorded all draws. Reuse rewrites the whole texture with
        // SDL cycling, preserving data bound by pending/in-flight GPU commands.
        // General image invalidation must not enter this pool: it can occur
        // while a backend still has unrecorded batches referencing the handle.
        long bytes = SampledTextureBytes(texture);
        if (bytes > MaximumIdleSampledTextureBytes)
        {
            RetireTexture(texture.Handle);
            return;
        }
        while (idleSampledTextures.Count >= MaximumIdleSampledTextures ||
            idleSampledTextureBytes + bytes > MaximumIdleSampledTextureBytes)
        {
            SdlGpuTextureResource oldest = idleSampledTextures[0];
            idleSampledTextures.RemoveAt(0);
            idleSampledTextureBytes -= SampledTextureBytes(oldest);
            RetireTexture(oldest.Handle);
        }
        idleSampledTextures.Add(texture);
        idleSampledTextureBytes += bytes;
    }

    private static long SampledTextureBytes(SdlGpuTextureResource texture) =>
        (long)texture.Width * texture.Height * 4;

    public SdlGpuRenderTarget GetLayerTarget(
        int depth,
        int pixelWidth,
        int pixelHeight,
        SdlGpuTextureFormat colorFormat,
        SdlGpuSampleCount sampleCount)
    {
        ThrowIfDisposed();
        SdlGpuLayerTargetKey key = new(
            depth,
            pixelWidth,
            pixelHeight,
            colorFormat,
            sampleCount);
        if (layerTargets.TryGetValue(key, out SdlGpuRenderTarget? cached))
        {
            return cached;
        }

        foreach (SdlGpuLayerTargetKey stale in layerTargets.Keys
            .Where(candidate => candidate.Depth == depth)
            .ToArray())
        {
            SdlGpuRenderTarget target = layerTargets[stale];
            layerTargets.Remove(stale);
            RetireTexture(target.ColorTexture);
            RetireTexture(target.DepthStencilTexture);
            RetireTexture(target.ResolveTexture);
        }

        SdlGpuRenderTarget created = CreateRenderTarget(
            pixelWidth,
            pixelHeight,
            colorFormat,
            sampleCount);
        layerTargets.Add(key, created);
        return created;
    }

    public SdlGpuRenderTarget CreateRenderTarget(
        int pixelWidth,
        int pixelHeight,
        SdlGpuTextureFormat colorFormat,
        SdlGpuSampleCount sampleCount,
        uint mipLevelCount = 1,
        bool useDepthStencil = true)
    {
        ThrowIfDisposed();
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(pixelWidth);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(pixelHeight);
        ArgumentOutOfRangeException.ThrowIfZero(mipLevelCount);
        nint color = 0;
        nint depth = 0;
        nint resolve = 0;
        try
        {
            bool multisampled = sampleCount != SdlGpuSampleCount.One;
            color = RequireHandle(
                api.CreateGpuTexture(
                    device,
                    new SdlGpuTextureCreateInfo(
                        colorFormat,
                        multisampled
                            ? SdlGpuTextureUsage.ColorTarget
                            : SdlGpuTextureUsage.ColorTarget | SdlGpuTextureUsage.Sampler,
                        checked((uint)pixelWidth),
                        checked((uint)pixelHeight),
                        sampleCount,
                        multisampled ? 1u : mipLevelCount)),
                "SDL GPU offscreen color-texture creation");
            ownedTextures.Add(color);
            if (multisampled)
            {
                resolve = RequireHandle(
                    api.CreateGpuTexture(
                        device,
                        new SdlGpuTextureCreateInfo(
                            colorFormat,
                            SdlGpuTextureUsage.ColorTarget | SdlGpuTextureUsage.Sampler,
                            checked((uint)pixelWidth),
                            checked((uint)pixelHeight),
                            SdlGpuSampleCount.One,
                            mipLevelCount)),
                    "SDL GPU offscreen resolve-texture creation");
                ownedTextures.Add(resolve);
            }
            if (useDepthStencil)
            {
                depth = RequireHandle(
                    api.CreateGpuTexture(
                        device,
                        new SdlGpuTextureCreateInfo(
                            SdlGpuTextureFormat.D24UnormS8Uint,
                            SdlGpuTextureUsage.DepthStencilTarget,
                            checked((uint)pixelWidth),
                            checked((uint)pixelHeight),
                            sampleCount)),
                    "SDL GPU offscreen depth/stencil-texture creation");
                ownedTextures.Add(depth);
            }
            return new SdlGpuRenderTarget(
                color,
                depth,
                pixelWidth,
                pixelHeight,
                colorFormat,
                sampleCount,
                resolve,
                mipLevelCount);
        }
        catch
        {
            if (depth != 0)
            {
                ownedTextures.Remove(depth);
                api.ReleaseGpuTexture(device, depth);
            }
            if (resolve != 0)
            {
                ownedTextures.Remove(resolve);
                api.ReleaseGpuTexture(device, resolve);
            }
            if (color != 0)
            {
                ownedTextures.Remove(color);
                api.ReleaseGpuTexture(device, color);
            }
            throw;
        }
    }

    public void RetireRenderTarget(SdlGpuRenderTarget target)
    {
        ArgumentNullException.ThrowIfNull(target);
        RetireTexture(target.ColorTexture);
        RetireTexture(target.DepthStencilTexture);
        RetireTexture(target.ResolveTexture);
    }

    internal void PinRenderTarget(SdlGpuWindowGraphicsSession session,
        SdlGpuCommandBufferToken token, SdlGpuRenderTarget target)
    {
        PinTexture(session, token, target.ColorTexture);
        PinTexture(session, token, target.DepthStencilTexture);
        PinTexture(session, token, target.ResolveTexture);
    }

    public void FlushRetired()
    {
        ProcessImageInvalidations();
        for (int index = retiredTextures.Count - 1; index >= 0; index--)
        {
            nint texture = retiredTextures[index];
            if (sampledTexturePinCounts.ContainsKey(texture))
            {
                continue;
            }
            if (ownedTextures.Remove(texture))
            {
                api.ReleaseGpuTexture(device, texture);
            }
            retiredTextures.RemoveAt(index);
        }

    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }
        lock (imageInvalidationGate)
        {
            disposed = true;
            pendingImageInvalidations.Clear();
        }
        foreach (SdlGpuImage image in subscribedImages)
        {
            image.ContentChanged -= OnImageContentChanged;
        }
        subscribedImages.Clear();

        prismResources?.Dispose();
        prismResources = null;
        surface3DResources?.Dispose();
        surface3DResources = null;
        FlushRetired();
        foreach (nint pipeline in pipelines.Values)
        {
            api.ReleaseGpuGraphicsPipeline(device, pipeline);
        }
        pipelines.Clear();
        foreach (nint sampler in samplers.Values)
        {
            api.ReleaseGpuSampler(device, sampler);
        }
        samplers.Clear();
        foreach (nint texture in ownedTextures)
        {
            api.ReleaseGpuTexture(device, texture);
        }
        ownedTextures.Clear();
        textures.Clear();
        pendingTextures.Clear();
        textureReferenceCounts.Clear();
        idleSampledTextures.Clear();
        idleSampledTextureBytes = 0;
        textAtlasEntries.Clear();
        textAtlasRecency.Clear();
        activeTextAtlasFrames.Clear();
        unusedTextAtlasFrames.Clear();
        pendingTextAtlasPins.Clear();
        unusedPendingTextAtlasPins.Clear();
        pendingSampledTexturePins.Clear();
        unusedPendingSampledTexturePins.Clear();
        sampledTexturePinCounts.Clear();
        deferredIdleSampledTextures.Clear();
        textAtlasPages.Clear();
        dirtyTextAtlasPages.Clear();
        layerTargets.Clear();
        foreach (nint transfer in uploadTransferBuffers.Values)
        {
            api.ReleaseGpuTransferBuffer(device, transfer);
        }
        uploadTransferBuffers.Clear();
        if (fragmentShader != 0)
        {
            api.ReleaseGpuShader(device, fragmentShader);
            fragmentShader = 0;
        }
        if (imageDomainFragmentShader != 0)
        {
            api.ReleaseGpuShader(device, imageDomainFragmentShader);
            imageDomainFragmentShader = 0;
        }
        if (prismPresentationShader != 0)
        {
            api.ReleaseGpuShader(device, prismPresentationShader);
            prismPresentationShader = 0;
        }
        if (vertexShader != 0)
        {
            api.ReleaseGpuShader(device, vertexShader);
            vertexShader = 0;
        }
        if (imageDomainVertexShader != 0)
        {
            api.ReleaseGpuShader(device, imageDomainVertexShader);
            imageDomainVertexShader = 0;
        }
    }

    private void UploadTexture(
        SdlGpuWindowGraphicsSession session,
        nint texture,
        int width,
        int height,
        ReadOnlySpan<byte> pixels)
    {
        int pixelCount = checked(width * height);
        if (pixels.Length % pixelCount != 0)
        {
            throw new ArgumentException(
                "Texture upload data must contain a whole number of bytes per pixel.",
                nameof(pixels));
        }
        UploadTextureRegion(
            session,
            texture,
            width,
            height,
            pixels,
            new SdlRect(0, 0, width, height),
            pixels.Length / pixelCount);
    }

    private void UploadTextureRegion(
        SdlGpuWindowGraphicsSession session,
        nint texture,
        int textureWidth,
        int textureHeight,
        ReadOnlySpan<byte> pixels,
        SdlRect region,
        int bytesPerPixel)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(bytesPerPixel);
        if (pixels.Length != checked(textureWidth * textureHeight * bytesPerPixel))
        {
            throw new ArgumentException(
                "Texture upload data does not match its dimensions and pixel size.",
                nameof(pixels));
        }
        if (region.X < 0 || region.Y < 0 ||
            region.Width <= 0 || region.Height <= 0 ||
            region.X + region.Width > textureWidth ||
            region.Y + region.Height > textureHeight)
        {
            throw new ArgumentOutOfRangeException(
                nameof(region),
                "The texture upload region must be non-empty and contained by the texture.");
        }

        int rowByteCount = checked(region.Width * bytesPerPixel);
        uint size = checked((uint)(rowByteCount * region.Height));
        uint transferCapacity = GrowTransferCapacity(0, size);
        nint uploadTransferBuffer = GetUploadTransferBuffer(transferCapacity);
        nint mapped = RequireHandle(
            api.MapGpuTransferBuffer(device, uploadTransferBuffer, cycle: true),
            "SDL GPU texture upload-buffer mapping");
        try
        {
            int sourceStride = checked(textureWidth * bytesPerPixel);
            int sourceOffset = checked(
                (region.Y * sourceStride) + (region.X * bytesPerPixel));
            for (int row = 0; row < region.Height; row++)
            {
                CopyToUnmanaged(
                    pixels.Slice(
                        checked(sourceOffset + (row * sourceStride)),
                        rowByteCount),
                    mapped + checked(row * rowByteCount));
            }
        }
        finally
        {
            api.UnmapGpuTransferBuffer(device, uploadTransferBuffer);
        }

        session.RunCopyPass(copyPass =>
        {
            bool uploadsWholeTexture = region.X == 0 && region.Y == 0 &&
                region.Width == textureWidth && region.Height == textureHeight;
            SdlGpuTextureTransferInfo source = new(
                uploadTransferBuffer,
                Offset: 0,
                PixelsPerRow: checked((uint)region.Width),
                RowsPerLayer: checked((uint)region.Height));
            SdlGpuTextureRegion destination = new(
                texture,
                checked((uint)region.Width),
                checked((uint)region.Height),
                checked((uint)region.X),
                checked((uint)region.Y));
            api.UploadToGpuTexture(
                copyPass,
                source,
                destination,
                cycle: uploadsWholeTexture);
        });
    }

    private bool TryAddTextAtlasEntry(
        SdlGpuTextLayerTextureKey key,
        RasterizedText layer,
        long frameToken,
        SdlGpuCommandBufferToken token,
        out SdlGpuTextAtlasEntry entry)
    {
        entry = null!;
        if (layer.Width + 2 > TextAtlasDimension ||
            layer.Height + 2 > TextAtlasDimension)
        {
            return false;
        }

        SdlGpuTextAtlasPage? page = null;
        int x = 0;
        int y = 0;
        foreach (SdlGpuTextAtlasPage candidate in textAtlasPages)
        {
            if (candidate.TryAllocate(layer.Width, layer.Height, out x, out y))
            {
                page = candidate;
                break;
            }
        }

        if (page is null)
        {
            if (textAtlasPages.Count < MaximumTextAtlasPages)
            {
                page = CreateTextAtlasPage();
                textAtlasPages.Add(page);
                if (!page.TryAllocate(layer.Width, layer.Height, out x, out y))
                {
                    throw new InvalidOperationException(
                        "A fresh SDL_GPU text atlas page rejected a fitting raster layer.");
                }
            }
            else
            {
                LinkedListNode<SdlGpuTextAtlasEntry>? node = textAtlasRecency.First;
                while (node is not null)
                {
                    LinkedListNode<SdlGpuTextAtlasEntry>? next = node.Next;
                    SdlGpuTextAtlasEntry stale = node.Value;
                    if (stale.ActiveFrameCount == 0)
                    {
                        SdlGpuTextAtlasPage candidate = stale.Page;
                        candidate.Free(stale);
                        if (!candidate.HasUnsubmittedChanges)
                        {
                            dirtyTextAtlasPages.Remove(candidate);
                        }
                        textAtlasEntries.Remove(stale.Key);
                        textAtlasRecency.Remove(node);
                        if (candidate.TryAllocate(layer.Width, layer.Height, out x, out y))
                        {
                            page = candidate;
                            break;
                        }
                    }
                    node = next;
                }
                if (page is null)
                {
                    return false;
                }
            }
        }

        long createdRevision = page.CopyPixels(x, y, layer);
        DrawRect textureCoordinates = new(
            x / (float)TextAtlasDimension,
            y / (float)TextAtlasDimension,
            layer.Width / (float)TextAtlasDimension,
            layer.Height / (float)TextAtlasDimension);
        entry = new SdlGpuTextAtlasEntry(
            page.Texture,
            textureCoordinates,
            layer.Width,
            layer.Height,
            layer.OriginOffset,
            page,
            key,
            createdRevision);
        textAtlasEntries.Add(key, entry);
        textAtlasRecency.AddLast(entry.RecencyNode);
        MarkTextAtlasEntryUsed(entry, frameToken);
        if (token.Session is not null)
        {
            page.RequireUpload(token, createdRevision);
        }
        return true;
    }

    internal bool TryGetTextAtlasEntries(
        SdlGpuWindowGraphicsSession session,
        SdlGpuTextLayerTextureKey redKey,
        SdlGpuTextLayerTextureKey greenKey,
        SdlGpuTextLayerTextureKey blueKey,
        long frameToken,
        out SdlGpuTextAtlasEntries entries)
    {
        if (!TryGetTextAtlasEntries(redKey, greenKey, blueKey, frameToken, out entries))
        {
            return false;
        }

        SdlGpuCommandBufferToken token = session.ActiveCommandBufferToken;
        for (int index = 0; index < 3; index++)
        {
            SdlGpuTextAtlasEntry entry = entries[index];
            entry.Page.RequireUpload(token, entry.CreatedRevision);
        }
        PinTextAtlasEntries(session, token, entries);
        return true;
    }

    private void PinTextAtlasEntries(
        SdlGpuWindowGraphicsSession session,
        SdlGpuCommandBufferToken token,
        SdlGpuTextAtlasEntries entries)
    {
        for (int index = 0; index < 3; index++)
        {
            PinTextAtlasEntry(session, token, entries[index]);
        }
    }

    private void PinTextAtlasEntriesCoveredByUploads(
        SdlGpuWindowGraphicsSession session,
        SdlGpuCommandBufferToken token,
        SdlGpuTextAtlasPage page,
        IReadOnlyList<SdlRect> uploadRegions)
    {
        foreach (SdlGpuTextAtlasEntry entry in textAtlasEntries.Values)
        {
            if (!ReferenceEquals(entry.Page, page) ||
                !OverlapsAnyTextAtlasUpload(entry, uploadRegions))
            {
                continue;
            }
            PinTextAtlasEntry(session, token, entry);
        }
    }

    private static bool OverlapsAnyTextAtlasUpload(
        SdlGpuTextAtlasEntry entry,
        IReadOnlyList<SdlRect> uploadRegions)
    {
        const int padding = 1;
        int left = (int)(entry.TextureCoordinates.X * TextAtlasDimension) - padding;
        int top = (int)(entry.TextureCoordinates.Y * TextAtlasDimension) - padding;
        int right = left + entry.Width + (padding * 2);
        int bottom = top + entry.Height + (padding * 2);
        foreach (SdlRect region in uploadRegions)
        {
            if (left < region.X + region.Width &&
                region.X < right &&
                top < region.Y + region.Height &&
                region.Y < bottom)
            {
                return true;
            }
        }
        return false;
    }

    private void PinTextAtlasEntry(
        SdlGpuWindowGraphicsSession session,
        SdlGpuCommandBufferToken token,
        SdlGpuTextAtlasEntry entry)
    {
        if (!pendingTextAtlasPins.TryGetValue(token, out HashSet<SdlGpuTextAtlasEntry>? pins))
        {
            pins = unusedPendingTextAtlasPins.TryPop(out HashSet<SdlGpuTextAtlasEntry>? unused)
                ? unused
                : [];
            pendingTextAtlasPins.Add(token, pins);
            session.RegisterCommandBufferParticipant(this);
        }
        if (pins.Add(entry))
        {
            entry.ActiveFrameCount++;
        }
    }

    private void ReleaseTextAtlasPins(SdlGpuCommandBufferToken token)
    {
        if (!pendingTextAtlasPins.Remove(token, out HashSet<SdlGpuTextAtlasEntry>? pins))
        {
            return;
        }
        foreach (SdlGpuTextAtlasEntry entry in pins)
        {
            entry.ActiveFrameCount--;
        }
        pins.Clear();
        unusedPendingTextAtlasPins.Push(pins);
    }

    private void PinSampledTexture(
        SdlGpuWindowGraphicsSession session,
        SdlGpuCommandBufferToken token,
        SdlGpuTextureResource texture)
    {
        PinTexture(session, token, texture.Handle);
    }

    private void PinTexture(
        SdlGpuWindowGraphicsSession session,
        SdlGpuCommandBufferToken token,
        nint handle)
    {
        if (handle == 0) return;
        if (!pendingSampledTexturePins.TryGetValue(token, out HashSet<nint>? pins))
        {
            pins = unusedPendingSampledTexturePins.TryPop(out HashSet<nint>? unused)
                ? unused
                : [];
            pendingSampledTexturePins.Add(token, pins);
            session.RegisterCommandBufferParticipant(this);
        }
        if (pins.Add(handle))
        {
            sampledTexturePinCounts.TryGetValue(handle, out int count);
            sampledTexturePinCounts[handle] = checked(count + 1);
        }
    }

    private void ReleaseSampledTexturePins(SdlGpuCommandBufferToken token)
    {
        if (!pendingSampledTexturePins.Remove(token, out HashSet<nint>? pins))
        {
            return;
        }
        foreach (nint handle in pins)
        {
            int count = sampledTexturePinCounts[handle] - 1;
            if (count == 0)
            {
                sampledTexturePinCounts.Remove(handle);
                if (deferredIdleSampledTextures.Remove(
                    handle,
                    out SdlGpuTextureResource? deferred))
                {
                    ReturnIdleSampledTexture(deferred);
                }
            }
            else
            {
                sampledTexturePinCounts[handle] = count;
            }
        }
        pins.Clear();
        unusedPendingSampledTexturePins.Push(pins);
    }

    public void OnCommandBufferSubmitted(SdlGpuCommandBufferToken token)
    {
        foreach (PendingTextureKey pendingKey in pendingTextures.Keys
            .Where(candidate => candidate.Token == token)
            .ToArray())
        {
            SdlGpuTextureResource pending = pendingTextures[pendingKey];
            pendingTextures.Remove(pendingKey);
            if (textures.TryGetValue(pendingKey.Key, out SdlGpuTextureResource? existing))
            {
                RetireTexture(pending.Handle);
            }
            else
            {
                textures.Add(pendingKey.Key, pending);
            }
        }

        foreach (SdlGpuTextAtlasPage page in textAtlasPages)
        {
            page.OnCommandBufferSubmitted(token);
            if (!page.HasUnsubmittedChanges)
            {
                dirtyTextAtlasPages.Remove(page);
            }
        }
        ReleaseTextAtlasPins(token);
        ReleaseSampledTexturePins(token);
    }

    public void OnCommandBufferAbandoned(SdlGpuCommandBufferToken token)
    {
        foreach (PendingTextureKey pendingKey in pendingTextures.Keys
            .Where(candidate => candidate.Token == token)
            .ToArray())
        {
            SdlGpuTextureResource pending = pendingTextures[pendingKey];
            pendingTextures.Remove(pendingKey);
            RetireTexture(pending.Handle);
            if (pendingKey.Key is SdlGpuImage image &&
                !textures.ContainsKey(pendingKey.Key) &&
                !pendingTextures.Keys.Any(candidate => Equals(candidate.Key, pendingKey.Key)) &&
                subscribedImages.Remove(image))
            {
                image.ContentChanged -= OnImageContentChanged;
            }
        }

        foreach (SdlGpuTextAtlasPage page in textAtlasPages)
        {
            page.OnCommandBufferAbandoned(token);
        }
        ReleaseTextAtlasPins(token);
        ReleaseSampledTexturePins(token);
    }

    private SdlGpuTextAtlasPage CreateTextAtlasPage()
    {
        SdlGpuTextureCreateInfo createInfo = new(
            SdlGpuTextureFormat.R8G8B8A8Unorm,
            SdlGpuTextureUsage.Sampler,
            TextAtlasDimension,
            TextAtlasDimension);
        nint texture = RequireHandle(
            api.CreateGpuTexture(device, createInfo),
            "SDL GPU text-atlas texture creation");
        ownedTextures.Add(texture);
        return new SdlGpuTextAtlasPage(
            new SdlGpuTextureResource(
                texture,
                TextAtlasDimension,
                TextAtlasDimension),
            TextAtlasDimension);
    }

    private void EnsureShaders()
    {
        if (vertexShader != 0)
        {
            return;
        }

        vertexShader = SdlGpuShaderArtifacts.CreateShader(
            api,
            device,
            supportedShaderFormats,
            SdlGpuShaderArtifacts.DrawingVertex);
        try
        {
            fragmentShader = SdlGpuShaderArtifacts.CreateShader(
                api,
                device,
                supportedShaderFormats,
                SdlGpuShaderArtifacts.DrawingFragment);
        }
        catch
        {
            api.ReleaseGpuShader(device, vertexShader);
            vertexShader = 0;
            throw;
        }
    }

    private void EnsureImageDomainShaders()
    {
        if (imageDomainVertexShader != 0)
        {
            return;
        }

        imageDomainVertexShader = SdlGpuShaderArtifacts.CreateShader(
            api,
            device,
            supportedShaderFormats,
            SdlGpuShaderArtifacts.DrawingImageDomainVertex);
        try
        {
            imageDomainFragmentShader = SdlGpuShaderArtifacts.CreateShader(
                api,
                device,
                supportedShaderFormats,
                SdlGpuShaderArtifacts.DrawingImageDomainFragment);
        }
        catch
        {
            api.ReleaseGpuShader(device, imageDomainVertexShader);
            imageDomainVertexShader = 0;
            throw;
        }
    }

    private void RetireTexture(nint texture)
    {
        deferredIdleSampledTextures.Remove(texture);
        if (texture != 0 && ownedTextures.Contains(texture) &&
            !retiredTextures.Contains(texture))
        {
            retiredTextures.Add(texture);
        }
    }

    private nint GetUploadTransferBuffer(uint capacity)
    {
        if (uploadTransferBuffers.TryGetValue(capacity, out nint cached))
        {
            return cached;
        }

        // Cycling preserves queued uploads, but cycles the entire allocation.
        // A large atlas upload must not make every later tiny texture cycle
        // that large buffer. Power-of-two capacities retain less than twice
        // the largest capacity while reusing storage for each upload size.
        nint created = RequireHandle(
            api.CreateGpuTransferBuffer(
                device,
                new SdlGpuTransferBufferCreateInfo(
                    SdlGpuTransferBufferUsage.Upload,
                    capacity)),
            "SDL GPU drawing transfer-buffer creation");
        uploadTransferBuffers.Add(capacity, created);
        return created;
    }

    private static uint GrowTransferCapacity(uint current, uint required)
    {
        uint next = Math.Max(current, InitialTransferCapacity);
        while (next < required)
        {
            next = checked(next * 2);
        }
        return next;
    }

    private static unsafe void CopyToUnmanaged(
        ReadOnlySpan<byte> source,
        nint destination)
    {
        source.CopyTo(new Span<byte>(destination.ToPointer(), source.Length));
    }

    private static SdlGpuBlendState ToBlendState(DrawBlendMode mode) => mode switch
    {
        DrawBlendMode.Opaque => SdlGpuBlendState.Opaque,
        DrawBlendMode.Additive => new SdlGpuBlendState(
            SdlGpuBlendFactor.One,
            SdlGpuBlendFactor.One,
            SdlGpuBlendOperation.Add,
            SdlGpuBlendFactor.One,
            SdlGpuBlendFactor.One,
            SdlGpuBlendOperation.Add),
        DrawBlendMode.Multiply => new SdlGpuBlendState(
            SdlGpuBlendFactor.DestinationColor,
            SdlGpuBlendFactor.OneMinusSourceAlpha,
            SdlGpuBlendOperation.Add,
            SdlGpuBlendFactor.One,
            SdlGpuBlendFactor.OneMinusSourceAlpha,
            SdlGpuBlendOperation.Add),
        DrawBlendMode.Screen => new SdlGpuBlendState(
            SdlGpuBlendFactor.One,
            SdlGpuBlendFactor.OneMinusSourceColor,
            SdlGpuBlendOperation.Add,
            SdlGpuBlendFactor.One,
            SdlGpuBlendFactor.OneMinusSourceAlpha,
            SdlGpuBlendOperation.Add),
        _ => new SdlGpuBlendState(
            SdlGpuBlendFactor.One,
            SdlGpuBlendFactor.OneMinusSourceAlpha,
            SdlGpuBlendOperation.Add,
            SdlGpuBlendFactor.One,
            SdlGpuBlendFactor.OneMinusSourceAlpha,
            SdlGpuBlendOperation.Add)
    };

    private nint RequireHandle(nint handle, string operation) =>
        handle != 0 ? handle : throw SdlApiError.Create(api, operation);

    private void ThrowIfDisposed() =>
        ObjectDisposedException.ThrowIf(disposed, this);

    private readonly record struct PendingTextureKey(
        object Key,
        SdlGpuCommandBufferToken Token);
}

[StructLayout(LayoutKind.Sequential)]
internal readonly record struct SdlGpuVertex(
    System.Numerics.Vector2 Position,
    System.Numerics.Vector2 TextureCoordinate,
    System.Numerics.Vector4 Color);

internal sealed record SdlGpuTextureResource(
    nint Handle, int Width, int Height, DrawPoint OriginOffset = default)
{
    internal bool CanRecycleStorage { get; init; }
}

internal sealed class SdlGpuTextAtlasEntry
{
    public SdlGpuTextAtlasEntry(
        SdlGpuTextureResource texture,
        DrawRect textureCoordinates,
        int width,
        int height,
        DrawPoint originOffset,
        SdlGpuTextAtlasPage page,
        SdlGpuTextLayerTextureKey key,
        long createdRevision)
    {
        Texture = texture;
        TextureCoordinates = textureCoordinates;
        Width = width;
        Height = height;
        OriginOffset = originOffset;
        Page = page;
        Key = key;
        CreatedRevision = createdRevision;
        RecencyNode = new(this);
    }

    public SdlGpuTextLayerTextureKey Key { get; }

    public LinkedListNode<SdlGpuTextAtlasEntry> RecencyNode { get; }

    public int ActiveFrameCount { get; set; }

    public SdlGpuTextureResource Texture { get; }

    public DrawRect TextureCoordinates { get; }

    public int Width { get; }

    public int Height { get; }

    public DrawPoint OriginOffset { get; }

    public SdlGpuTextAtlasPage Page { get; }

    public long CreatedRevision { get; }
}

internal readonly record struct SdlGpuTextAtlasEntries(
    SdlGpuTextAtlasEntry Red,
    SdlGpuTextAtlasEntry Green,
    SdlGpuTextAtlasEntry Blue)
{
    public SdlGpuTextAtlasEntry this[int index] => index switch
    {
        0 => Red,
        1 => Green,
        2 => Blue,
        _ => throw new ArgumentOutOfRangeException(nameof(index))
    };
}

internal readonly record struct SdlGpuTextRasterKey(
    object FontIdentity,
    string Text,
    float Size,
    float CoordinateScale,
    DrawPoint PixelPhase);

internal readonly record struct SdlGpuTextLayerTextureKey(
    SdlGpuTextRasterKey Raster,
    SdlGpuColorWriteMask Channel);

internal sealed class SdlGpuTextAtlasPage(
    SdlGpuTextureResource texture,
    int dimension)
{
    private const int Padding = 1;
    private readonly SdlGpuTextAtlasAllocator allocator = new(dimension);
    private int dirtyLeft = int.MaxValue;
    private int dirtyTop = int.MaxValue;
    private int dirtyRight;
    private int dirtyBottom;
    private long revision;
    private long submittedRevision;
    private readonly List<DirtyChange> dirtyChanges = [];
    private readonly Dictionary<SdlGpuCommandBufferToken, long> requiredRevisions = [];
    private readonly Dictionary<SdlGpuCommandBufferToken, long> uploadedRevisions = [];

    public SdlGpuTextureResource Texture { get; } = texture;

    public byte[] Pixels { get; } = new byte[checked(dimension * dimension * 4)];

    public bool TryAllocate(int width, int height, out int x, out int y)
    {
        int paddedWidth = checked(width + (Padding * 2));
        int paddedHeight = checked(height + (Padding * 2));
        if (!allocator.TryAllocate(paddedWidth, paddedHeight, out SdlRect allocation))
        {
            x = 0;
            y = 0;
            return false;
        }

        x = allocation.X + Padding;
        y = allocation.Y + Padding;
        return true;
    }

    public void Free(SdlGpuTextAtlasEntry entry)
    {
        allocator.Free(new SdlRect(
            (int)(entry.TextureCoordinates.X * dimension) - Padding,
            (int)(entry.TextureCoordinates.Y * dimension) - Padding,
            entry.Width + Padding * 2,
            entry.Height + Padding * 2));
        if (dirtyChanges.RemoveAll(change =>
            change.Revision == entry.CreatedRevision) != 0)
        {
            RebuildDirtyBounds();
        }
    }

    public long CopyPixels(int x, int y, RasterizedText layer)
    {
        return CopyPixels(x, y, layer.Width, layer.Height, layer.PixelSpan);
    }

    public long CopyPixels(
        int x,
        int y,
        int width,
        int height,
        ReadOnlySpan<byte> pixels)
    {
        int sourceStride = checked(width * 4);
        int destinationStride = checked(dimension * 4);
        // A reclaimed slot can contain a larger old raster. Clear the complete
        // new gutter before copying so bilinear sampling cannot see old pixels.
        for (int row = -Padding; row < height + Padding; row++)
        {
            Pixels.AsSpan(((y + row) * dimension + x - Padding) * 4,
                (width + Padding * 2) * 4).Clear();
        }
        for (int row = 0; row < height; row++)
        {
            pixels.Slice(row * sourceStride, sourceStride).CopyTo(
                Pixels.AsSpan(
                    checked(((y + row) * destinationStride) + (x * 4)),
                    sourceStride));
        }

        dirtyLeft = Math.Min(dirtyLeft, x - Padding);
        dirtyTop = Math.Min(dirtyTop, y - Padding);
        dirtyRight = Math.Max(dirtyRight, checked(x + width + Padding));
        dirtyBottom = Math.Max(dirtyBottom, checked(y + height + Padding));
        long nextRevision = checked(++revision);
        dirtyChanges.Add(new DirtyChange(
            nextRevision,
            new SdlRect(
                x - Padding,
                y - Padding,
                width + Padding * 2,
                height + Padding * 2)));
        return nextRevision;
    }

    public bool TryGetDirtyRegion(out SdlRect region)
    {
        if (dirtyRight <= dirtyLeft || dirtyBottom <= dirtyTop)
        {
            region = default;
            return false;
        }

        region = new SdlRect(
            dirtyLeft,
            dirtyTop,
            dirtyRight - dirtyLeft,
            dirtyBottom - dirtyTop);
        return true;
    }

    public void MarkUploaded()
    {
        submittedRevision = revision;
        dirtyChanges.Clear();
        dirtyLeft = int.MaxValue;
        dirtyTop = int.MaxValue;
        dirtyRight = 0;
        dirtyBottom = 0;
    }

    public bool HasUnsubmittedChanges => dirtyChanges.Count != 0;

    public void RequireUpload(SdlGpuCommandBufferToken token, long requiredRevision)
    {
        if (requiredRevision <= submittedRevision)
        {
            return;
        }
        if (!requiredRevisions.TryGetValue(token, out long current) || current < requiredRevision)
        {
            requiredRevisions[token] = requiredRevision;
        }
    }

    public bool CollectRequiredUploadRegions(
        SdlGpuCommandBufferToken token,
        List<SdlRect> regions,
        out long uploadRevision)
    {
        ArgumentNullException.ThrowIfNull(regions);
        regions.Clear();
        if (!requiredRevisions.TryGetValue(token, out long required) ||
            required <= submittedRevision)
        {
            uploadRevision = 0;
            return false;
        }

        uploadedRevisions.TryGetValue(token, out long alreadyUploaded);
        long afterRevision = Math.Max(submittedRevision, alreadyUploaded);
        uploadRevision = required;
        foreach (DirtyChange change in dirtyChanges)
        {
            if (change.Revision <= afterRevision || change.Revision > uploadRevision)
            {
                continue;
            }
            // Record exact changed rectangles. A bounding rectangle may include
            // an unrelated free slot which another command buffer can reuse;
            // submitting the older snapshot last would then restore stale bytes.
            AddUploadRegion(regions, change.Region);
        }

        return regions.Count != 0;
    }

    private static void AddUploadRegion(List<SdlRect> regions, SdlRect region)
    {
        for (int index = regions.Count - 1; index >= 0; index--)
        {
            SdlRect candidate = regions[index];
            if (candidate.Y != region.Y ||
                candidate.Height != region.Height ||
                candidate.X + candidate.Width < region.X ||
                region.X + region.Width < candidate.X)
            {
                continue;
            }

            int left = Math.Min(candidate.X, region.X);
            int right = Math.Max(
                candidate.X + candidate.Width,
                region.X + region.Width);
            region = new SdlRect(left, region.Y, right - left, region.Height);
            regions.RemoveAt(index);
        }
        regions.Add(region);
    }

    public void MarkUploadRecorded(SdlGpuCommandBufferToken token, long uploadRevision)
    {
        uploadedRevisions[token] = Math.Max(
            uploadedRevisions.GetValueOrDefault(token),
            uploadRevision);
    }

    public void OnCommandBufferSubmitted(SdlGpuCommandBufferToken token)
    {
        requiredRevisions.Remove(token);
        if (!uploadedRevisions.Remove(token, out long uploadedRevision) ||
            uploadedRevision <= submittedRevision)
        {
            return;
        }

        submittedRevision = uploadedRevision;
        dirtyChanges.RemoveAll(change => change.Revision <= submittedRevision);
        RebuildDirtyBounds();
    }

    public void OnCommandBufferAbandoned(SdlGpuCommandBufferToken token)
    {
        requiredRevisions.Remove(token);
        uploadedRevisions.Remove(token);
    }

    private void RebuildDirtyBounds()
    {
        dirtyLeft = int.MaxValue;
        dirtyTop = int.MaxValue;
        dirtyRight = 0;
        dirtyBottom = 0;
        foreach (DirtyChange change in dirtyChanges)
        {
            dirtyLeft = Math.Min(dirtyLeft, change.Region.X);
            dirtyTop = Math.Min(dirtyTop, change.Region.Y);
            dirtyRight = Math.Max(dirtyRight, checked(change.Region.X + change.Region.Width));
            dirtyBottom = Math.Max(dirtyBottom, checked(change.Region.Y + change.Region.Height));
        }
    }

    private readonly record struct DirtyChange(long Revision, SdlRect Region);

}

internal sealed record SdlGpuRenderTarget(
    nint ColorTexture,
    nint DepthStencilTexture,
    int PixelWidth,
    int PixelHeight,
    SdlGpuTextureFormat ColorFormat,
    SdlGpuSampleCount SampleCount,
    nint ResolveTexture = 0,
    uint MipLevelCount = 1)
{
    public nint SampleTexture => ResolveTexture != 0
        ? ResolveTexture
        : ColorTexture;
}

internal readonly record struct SdlGpuPipelineKey(
    SdlGpuTextureFormat ColorFormat,
    SdlGpuSampleCount SampleCount,
    DrawPrimitiveTopology Topology,
    DrawBlendMode BlendMode,
    SdlGpuStencilMode StencilMode,
    SdlGpuColorWriteMask ColorWriteMask,
    bool AlphaMask = false,
    bool PrismPresentation = false,
    bool PointClampImageDomain = false);

internal readonly record struct SdlGpuSamplerKey(
    DrawSamplingMode Sampling,
    DrawAddressMode AddressMode,
    bool Anisotropic = false);

internal readonly record struct SdlGpuLayerTargetKey(
    int Depth,
    int PixelWidth,
    int PixelHeight,
    SdlGpuTextureFormat ColorFormat,
    SdlGpuSampleCount SampleCount);
