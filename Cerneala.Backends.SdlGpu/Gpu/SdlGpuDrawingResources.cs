using System.Runtime.InteropServices;
using Cerneala.Drawing;
using Cerneala.Drawing.Text;
using Cerneala.Platforms.Sdl3;

namespace Cerneala.Backends.SdlGpu;

internal sealed class SdlGpuDrawingResources : IDisposable
{
    private const uint InitialGeometryCapacity = 64 * 1024;
    private const int TextAtlasDimension = 1024;
    private const int MaximumTextAtlasPages = 8;
    private readonly ISdlApi api;
    private readonly nint device;
    private readonly SdlGpuShaderFormats supportedShaderFormats;
    private readonly Dictionary<SdlGpuPipelineKey, nint> pipelines = [];
    private readonly Dictionary<SdlGpuSamplerKey, nint> samplers = [];
    private readonly Dictionary<object, SdlGpuTextureResource> textures = [];
    private readonly Dictionary<SdlGpuTextLayerTextureKey, SdlGpuTextAtlasEntry> textAtlasEntries = [];
    private readonly List<SdlGpuTextAtlasPage> textAtlasPages = [];
    private readonly Stack<SdlGpuTextAtlasPage> spareTextAtlasPages = [];
    private readonly HashSet<SdlGpuTextAtlasPage> dirtyTextAtlasPages = [];
    private readonly Dictionary<SdlGpuLayerTargetKey, SdlGpuRenderTarget> layerTargets = [];
    private readonly HashSet<nint> ownedTextures = [];
    private readonly List<nint> retiredTextures = [];
    private readonly List<nint> retiredBuffers = [];
    private readonly List<nint> retiredTransferBuffers = [];
    private nint vertexShader;
    private nint fragmentShader;
    private nint vertexBuffer;
    private nint indexBuffer;
    private nint uploadTransferBuffer;
    private uint vertexCapacity;
    private uint indexCapacity;
    private uint transferCapacity;
    private long nextTextAtlasFrameToken;
    private long textAtlasUsageSequence;
    private int compactedTextAtlasPageCount = 1;
    private SdlGpuPrismDeviceResources? prismResources;
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
        textures.Count + textAtlasPages.Count + spareTextAtlasPages.Count;

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
                RetainedCacheSoftByteLimit = 64L * 1024 * 1024
            });

    public nint GetPipeline(
        SdlGpuTextureFormat colorFormat,
        SdlGpuSampleCount sampleCount,
        DrawPrimitiveTopology topology,
        DrawBlendMode blendMode,
        SdlGpuStencilMode stencilMode,
        SdlGpuColorWriteMask colorWriteMask = SdlGpuColorWriteMask.All)
    {
        ThrowIfDisposed();
        EnsureShaders();
        SdlGpuPipelineKey key = new(
            colorFormat,
            sampleCount,
            topology,
            blendMode,
            stencilMode,
            colorWriteMask);
        if (pipelines.TryGetValue(key, out nint cached))
        {
            return cached;
        }

        SdlGpuGraphicsPipelineCreateInfo createInfo = new(
            vertexShader,
            fragmentShader,
            colorFormat,
            SdlGpuTextureFormat.D24UnormS8Uint,
            sampleCount,
            topology == DrawPrimitiveTopology.TriangleStrip
                ? SdlGpuPrimitiveType.TriangleStrip
                : SdlGpuPrimitiveType.TriangleList,
            ToBlendState(blendMode),
            stencilMode,
            colorWriteMask);
        nint pipeline = RequireHandle(
            api.CreateGpuGraphicsPipeline(device, createInfo),
            $"SDL GPU drawing pipeline creation ({key})");
        pipelines.Add(key, pipeline);
        return pipeline;
    }

    public nint GetSampler(DrawSamplingMode sampling, DrawAddressMode addressMode)
    {
        ThrowIfDisposed();
        SdlGpuSamplerKey key = new(sampling, addressMode);
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
                : SdlGpuSamplerMipmapMode.Linear);
        nint sampler = RequireHandle(
            api.CreateGpuSampler(device, createInfo),
            $"SDL GPU drawing sampler creation ({key})");
        samplers.Add(key, sampler);
        return sampler;
    }

    public SdlGpuTextureResource GetOrCreateTexture(
        SdlGpuWindowGraphicsSession session,
        object key,
        int width,
        int height,
        ReadOnlySpan<byte> rgbaPixels)
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

        if (textures.TryGetValue(key, out SdlGpuTextureResource? cached))
        {
            return cached;
        }

        return CreateSampledTexture(
            session,
            key,
            width,
            height,
            SdlGpuTextureFormat.R8G8B8A8Unorm,
            rgbaPixels);
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
        if (textures.TryGetValue(key, out SdlGpuTextureResource? cached))
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
        ReadOnlySpan<byte> pixels)
    {
        if (textures.TryGetValue(key, out SdlGpuTextureResource? cached))
        {
            return cached;
        }

        SdlGpuTextureCreateInfo createInfo = new(
            format,
            SdlGpuTextureUsage.Sampler,
            checked((uint)width),
            checked((uint)height));
        nint texture = RequireHandle(
            api.CreateGpuTexture(device, createInfo),
            "SDL GPU sampled-texture creation");
        ownedTextures.Add(texture);
        try
        {
            UploadTexture(session, texture, width, height, pixels);
            SdlGpuTextureResource created = new(texture, width, height);
            textures.Add(key, created);
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
        return checked(++nextTextAtlasFrameToken);
    }

    public void EndTextAtlasFrame(long frameToken)
    {
        if (frameToken == 0)
        {
            return;
        }

        bool shouldCompact =
            textAtlasPages.Count > compactedTextAtlasPageCount ||
            ShouldCompactTextAtlas(frameToken);
        foreach (SdlGpuTextAtlasPage page in textAtlasPages)
        {
            page.EndFrame(frameToken);
        }
        if (shouldCompact)
        {
            CompactTextAtlas(frameToken);
        }
    }

    private bool ShouldCompactTextAtlas(long frameToken)
    {
        foreach (SdlGpuTextAtlasPage page in textAtlasPages)
        {
            int activeEntryCount = 0;
            int maximumActiveWidth = 0;
            int maximumActiveHeight = 0;
            foreach (SdlGpuTextAtlasEntry entry in textAtlasEntries.Values)
            {
                if (!ReferenceEquals(entry.Page, page) ||
                    entry.LastUsedFrameToken != frameToken)
                {
                    continue;
                }

                activeEntryCount++;
                maximumActiveWidth = Math.Max(maximumActiveWidth, entry.Width);
                maximumActiveHeight = Math.Max(maximumActiveHeight, entry.Height);
            }

            if (activeEntryCount != 0 &&
                page.Keys.Count > activeEntryCount &&
                !page.CanAllocate(maximumActiveWidth, maximumActiveHeight, 3))
            {
                return true;
            }
        }

        return false;
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

        long usage = checked(++textAtlasUsageSequence);
        red.MarkUsed(frameToken);
        green.MarkUsed(frameToken);
        blue.MarkUsed(frameToken);
        red.Page.MarkUsed(frameToken, usage);
        green.Page.MarkUsed(frameToken, usage);
        blue.Page.MarkUsed(frameToken, usage);
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
        if (TryGetTextAtlasEntries(
                redKey,
                greenKey,
                blueKey,
                frameToken,
                out SdlGpuTextAtlasEntries cached))
        {
            return cached;
        }

        if (!TryGetOrCreateTextAtlasEntry(redKey, layers[0], frameToken, out SdlGpuTextAtlasEntry red) ||
            !TryGetOrCreateTextAtlasEntry(greenKey, layers[1], frameToken, out SdlGpuTextAtlasEntry green) ||
            !TryGetOrCreateTextAtlasEntry(blueKey, layers[2], frameToken, out SdlGpuTextAtlasEntry blue))
        {
            return null;
        }

        return new SdlGpuTextAtlasEntries(red, green, blue);
    }

    private bool TryGetOrCreateTextAtlasEntry(
        SdlGpuTextLayerTextureKey key,
        RasterizedText layer,
        long frameToken,
        out SdlGpuTextAtlasEntry entry)
    {
        if (textAtlasEntries.TryGetValue(key, out SdlGpuTextAtlasEntry? existing))
        {
            existing.MarkUsed(frameToken);
            existing.Page.MarkUsed(
                frameToken,
                checked(++textAtlasUsageSequence));
            entry = existing;
            return true;
        }
        if (!TryAddTextAtlasEntry(key, layer, frameToken, out entry))
        {
            return false;
        }
        dirtyTextAtlasPages.Add(entry.Page);
        return true;
    }

    public void FlushTextAtlasUploads(SdlGpuWindowGraphicsSession session)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(session);
        if (dirtyTextAtlasPages.Count == 0)
        {
            return;
        }

        foreach (SdlGpuTextAtlasPage page in dirtyTextAtlasPages)
        {
            if (!page.TryGetDirtyRegion(out SdlRect dirtyRegion))
            {
                continue;
            }

            UploadTextureRegion(
                session,
                page.Texture.Handle,
                TextAtlasDimension,
                TextAtlasDimension,
                page.Pixels,
                dirtyRegion,
                bytesPerPixel: 4);
            page.MarkUploaded();
        }
        dirtyTextAtlasPages.Clear();
    }

    public void InvalidateTexture(object key)
    {
        if (textures.Remove(key, out SdlGpuTextureResource? texture))
        {
            RetireTexture(texture.Handle);
        }
    }

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

    public SdlGpuGeometryBinding UploadGeometry(
        SdlGpuWindowGraphicsSession session,
        ReadOnlySpan<SdlGpuVertex> vertices,
        ReadOnlySpan<int> indices)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(session);
        if (vertices.IsEmpty || indices.IsEmpty)
        {
            throw new ArgumentException("GPU geometry cannot be empty.");
        }

        ReadOnlySpan<byte> vertexBytes = MemoryMarshal.AsBytes(vertices);
        ReadOnlySpan<byte> indexBytes = MemoryMarshal.AsBytes(indices);
        uint vertexByteCount = checked((uint)vertexBytes.Length);
        uint indexByteCount = checked((uint)indexBytes.Length);
        EnsureGeometryCapacity(
            vertexByteCount,
            indexByteCount);
        uint totalBytes = checked((uint)(vertexBytes.Length + indexBytes.Length));
        EnsureTransferCapacity(totalBytes);
        nint mapped = RequireHandle(
            api.MapGpuTransferBuffer(device, uploadTransferBuffer, cycle: true),
            "SDL GPU drawing upload-buffer mapping");
        try
        {
            CopyToUnmanaged(vertexBytes, mapped);
            CopyToUnmanaged(indexBytes, mapped + vertexBytes.Length);
        }
        finally
        {
            api.UnmapGpuTransferBuffer(device, uploadTransferBuffer);
        }

        session.RunCopyPass(copyPass =>
        {
            api.UploadToGpuBuffer(
                copyPass,
                uploadTransferBuffer,
                0,
                vertexBuffer,
                0,
                vertexByteCount,
                cycle: true);
            api.UploadToGpuBuffer(
                copyPass,
                uploadTransferBuffer,
                vertexByteCount,
                indexBuffer,
                0,
                indexByteCount,
                cycle: true);
        });
        return new SdlGpuGeometryBinding(vertexBuffer, indexBuffer);
    }

    public void FlushRetired()
    {
        foreach (nint texture in retiredTextures)
        {
            if (ownedTextures.Remove(texture))
            {
                api.ReleaseGpuTexture(device, texture);
            }
        }
        retiredTextures.Clear();

        foreach (nint buffer in retiredBuffers)
        {
            api.ReleaseGpuBuffer(device, buffer);
        }
        retiredBuffers.Clear();

        foreach (nint transfer in retiredTransferBuffers)
        {
            api.ReleaseGpuTransferBuffer(device, transfer);
        }
        retiredTransferBuffers.Clear();
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }
        disposed = true;

        prismResources?.Dispose();
        prismResources = null;
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
        textAtlasEntries.Clear();
        textAtlasPages.Clear();
        spareTextAtlasPages.Clear();
        dirtyTextAtlasPages.Clear();
        layerTargets.Clear();
        if (vertexBuffer != 0)
        {
            api.ReleaseGpuBuffer(device, vertexBuffer);
            vertexBuffer = 0;
        }
        if (indexBuffer != 0)
        {
            api.ReleaseGpuBuffer(device, indexBuffer);
            indexBuffer = 0;
        }
        if (uploadTransferBuffer != 0)
        {
            api.ReleaseGpuTransferBuffer(device, uploadTransferBuffer);
            uploadTransferBuffer = 0;
        }
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
        EnsureTransferCapacity(size);
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
            page = textAtlasPages
                .Where(static candidate => candidate.ActiveFrameCount == 0)
                .OrderBy(static candidate => candidate.LastUsedSequence)
                .FirstOrDefault();
            if (page is not null)
            {
                foreach (SdlGpuTextLayerTextureKey staleKey in page.Keys)
                {
                    textAtlasEntries.Remove(staleKey);
                }
                page.Reset();
            }
            else if (spareTextAtlasPages.Count != 0 ||
                textAtlasPages.Count + spareTextAtlasPages.Count < MaximumTextAtlasPages)
            {
                page = RentTextAtlasPage();
                textAtlasPages.Add(page);
            }
            else
            {
                return false;
            }

            if (!page.TryAllocate(layer.Width, layer.Height, out x, out y))
            {
                throw new InvalidOperationException(
                    "A fresh SDL_GPU text atlas page rejected a fitting raster layer.");
            }
        }

        page.CopyPixels(x, y, layer);
        page.MarkUsed(frameToken, checked(++textAtlasUsageSequence));
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
            x,
            y,
            frameToken);
        page.Keys.Add(key);
        textAtlasEntries.Add(key, entry);
        return true;
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

    private SdlGpuTextAtlasPage RentTextAtlasPage() =>
        spareTextAtlasPages.TryPop(out SdlGpuTextAtlasPage? page)
            ? page
            : CreateTextAtlasPage();

    private void CompactTextAtlas(long frameToken)
    {
        SdlGpuTextAtlasSnapshot[] retained = textAtlasEntries
            .Where(pair => pair.Value.LastUsedFrameToken == frameToken)
            .Select(pair => new SdlGpuTextAtlasSnapshot(
                pair.Key,
                pair.Value.Width,
                pair.Value.Height,
                pair.Value.OriginOffset,
                pair.Value.Page.CopyPixels(
                    pair.Value.PixelX,
                    pair.Value.PixelY,
                    pair.Value.Width,
                    pair.Value.Height)))
            .OrderByDescending(static snapshot => snapshot.Height)
            .ThenByDescending(static snapshot => snapshot.Width)
            .ToArray();

        foreach (SdlGpuTextAtlasPage page in textAtlasPages)
        {
            page.Reset();
        }
        textAtlasEntries.Clear();
        dirtyTextAtlasPages.Clear();

        int usedPageCount = 0;
        foreach (SdlGpuTextAtlasSnapshot snapshot in retained)
        {
            SdlGpuTextAtlasPage? page = null;
            int x = 0;
            int y = 0;
            for (int pageIndex = 0; pageIndex < usedPageCount; pageIndex++)
            {
                SdlGpuTextAtlasPage candidate = textAtlasPages[pageIndex];
                if (candidate.TryAllocate(snapshot.Width, snapshot.Height, out x, out y))
                {
                    page = candidate;
                    break;
                }
            }

            if (page is null)
            {
                page = usedPageCount < textAtlasPages.Count
                    ? textAtlasPages[usedPageCount]
                    : RentTextAtlasPage();
                if (usedPageCount == textAtlasPages.Count)
                {
                    textAtlasPages.Add(page);
                }
                usedPageCount++;
                if (!page.TryAllocate(snapshot.Width, snapshot.Height, out x, out y))
                {
                    throw new InvalidOperationException(
                        "A fresh SDL_GPU text atlas page rejected a retained fitting raster layer.");
                }
            }

            page.CopyPixels(x, y, snapshot.Width, snapshot.Height, snapshot.Pixels);
            page.MarkUsed(0, checked(++textAtlasUsageSequence));
            DrawRect textureCoordinates = new(
                x / (float)TextAtlasDimension,
                y / (float)TextAtlasDimension,
                snapshot.Width / (float)TextAtlasDimension,
                snapshot.Height / (float)TextAtlasDimension);
            SdlGpuTextAtlasEntry entry = new(
                page.Texture,
                textureCoordinates,
                snapshot.Width,
                snapshot.Height,
                snapshot.OriginOffset,
                page,
                x,
                y,
                frameToken);
            page.Keys.Add(snapshot.Key);
            textAtlasEntries.Add(snapshot.Key, entry);
            dirtyTextAtlasPages.Add(page);
        }

        for (int pageIndex = textAtlasPages.Count - 1;
             pageIndex >= usedPageCount;
             pageIndex--)
        {
            SdlGpuTextAtlasPage unused = textAtlasPages[pageIndex];
            textAtlasPages.RemoveAt(pageIndex);
            dirtyTextAtlasPages.Remove(unused);
            spareTextAtlasPages.Push(unused);
        }
        compactedTextAtlasPageCount = Math.Max(1, usedPageCount);
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

    private void EnsureGeometryCapacity(uint requiredVertexBytes, uint requiredIndexBytes)
    {
        if (requiredVertexBytes > vertexCapacity)
        {
            uint next = GrowCapacity(vertexCapacity, requiredVertexBytes);
            nint created = RequireHandle(
                api.CreateGpuBuffer(
                    device,
                    new SdlGpuBufferCreateInfo(SdlGpuBufferUsage.Vertex, next)),
                "SDL GPU vertex-buffer creation");
            if (vertexBuffer != 0)
            {
                retiredBuffers.Add(vertexBuffer);
            }
            vertexBuffer = created;
            vertexCapacity = next;
        }

        if (requiredIndexBytes > indexCapacity)
        {
            uint next = GrowCapacity(indexCapacity, requiredIndexBytes);
            nint created = RequireHandle(
                api.CreateGpuBuffer(
                    device,
                    new SdlGpuBufferCreateInfo(SdlGpuBufferUsage.Index, next)),
                "SDL GPU index-buffer creation");
            if (indexBuffer != 0)
            {
                retiredBuffers.Add(indexBuffer);
            }
            indexBuffer = created;
            indexCapacity = next;
        }
    }

    private void EnsureTransferCapacity(uint requiredBytes)
    {
        if (requiredBytes <= transferCapacity)
        {
            return;
        }

        uint next = GrowCapacity(transferCapacity, requiredBytes);
        nint created = RequireHandle(
            api.CreateGpuTransferBuffer(
                device,
                new SdlGpuTransferBufferCreateInfo(
                    SdlGpuTransferBufferUsage.Upload,
                    next)),
            "SDL GPU drawing transfer-buffer creation");
        if (uploadTransferBuffer != 0)
        {
            retiredTransferBuffers.Add(uploadTransferBuffer);
        }
        uploadTransferBuffer = created;
        transferCapacity = next;
    }

    private void RetireTexture(nint texture)
    {
        if (texture != 0 && ownedTextures.Contains(texture) &&
            !retiredTextures.Contains(texture))
        {
            retiredTextures.Add(texture);
        }
    }

    private static uint GrowCapacity(uint current, uint required)
    {
        uint next = Math.Max(current, InitialGeometryCapacity);
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
}

[StructLayout(LayoutKind.Sequential)]
internal readonly record struct SdlGpuVertex(
    System.Numerics.Vector2 Position,
    System.Numerics.Vector2 TextureCoordinate,
    System.Numerics.Vector4 Color);

internal sealed record SdlGpuTextureResource(nint Handle, int Width, int Height);

internal sealed class SdlGpuTextAtlasEntry(
    SdlGpuTextureResource texture,
    DrawRect textureCoordinates,
    int width,
    int height,
    DrawPoint originOffset,
    SdlGpuTextAtlasPage page,
    int pixelX,
    int pixelY,
    long lastUsedFrameToken)
{
    public SdlGpuTextureResource Texture { get; } = texture;

    public DrawRect TextureCoordinates { get; } = textureCoordinates;

    public int Width { get; } = width;

    public int Height { get; } = height;

    public DrawPoint OriginOffset { get; } = originOffset;

    public SdlGpuTextAtlasPage Page { get; } = page;

    public int PixelX { get; } = pixelX;

    public int PixelY { get; } = pixelY;

    public long LastUsedFrameToken { get; private set; } = lastUsedFrameToken;

    public void MarkUsed(long frameToken) => LastUsedFrameToken = frameToken;
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

internal readonly record struct SdlGpuTextAtlasSnapshot(
    SdlGpuTextLayerTextureKey Key,
    int Width,
    int Height,
    DrawPoint OriginOffset,
    byte[] Pixels);

internal sealed class SdlGpuTextAtlasPage(
    SdlGpuTextureResource texture,
    int dimension)
{
    private const int Padding = 1;
    private readonly HashSet<long> activeFrames = [];
    private int nextX;
    private int nextY;
    private int rowHeight;
    private int dirtyLeft = int.MaxValue;
    private int dirtyTop = int.MaxValue;
    private int dirtyRight;
    private int dirtyBottom;

    public SdlGpuTextureResource Texture { get; } = texture;

    public byte[] Pixels { get; } = new byte[checked(dimension * dimension * 4)];

    public HashSet<SdlGpuTextLayerTextureKey> Keys { get; } = [];

    public int ActiveFrameCount => activeFrames.Count;

    public long LastUsedSequence { get; private set; }

    public bool TryAllocate(int width, int height, out int x, out int y)
    {
        int paddedWidth = checked(width + (Padding * 2));
        int paddedHeight = checked(height + (Padding * 2));
        if (paddedWidth > dimension || paddedHeight > dimension)
        {
            x = 0;
            y = 0;
            return false;
        }

        if (nextX + paddedWidth > dimension)
        {
            nextX = 0;
            nextY += rowHeight;
            rowHeight = 0;
        }
        if (nextY + paddedHeight > dimension)
        {
            x = 0;
            y = 0;
            return false;
        }

        x = nextX + Padding;
        y = nextY + Padding;
        nextX += paddedWidth;
        rowHeight = Math.Max(rowHeight, paddedHeight);
        return true;
    }

    public bool CanAllocate(int width, int height, int count)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(count);
        int paddedWidth = checked(width + (Padding * 2));
        int paddedHeight = checked(height + (Padding * 2));
        if (paddedWidth > dimension || paddedHeight > dimension)
        {
            return false;
        }

        int candidateX = nextX;
        int candidateY = nextY;
        int candidateRowHeight = rowHeight;
        for (int index = 0; index < count; index++)
        {
            if (candidateX + paddedWidth > dimension)
            {
                candidateX = 0;
                candidateY += candidateRowHeight;
                candidateRowHeight = 0;
            }
            if (candidateY + paddedHeight > dimension)
            {
                return false;
            }

            candidateX += paddedWidth;
            candidateRowHeight = Math.Max(candidateRowHeight, paddedHeight);
        }

        return true;
    }

    public void CopyPixels(int x, int y, RasterizedText layer)
    {
        CopyPixels(x, y, layer.Width, layer.Height, layer.PixelSpan);
    }

    public void CopyPixels(
        int x,
        int y,
        int width,
        int height,
        ReadOnlySpan<byte> pixels)
    {
        int sourceStride = checked(width * 4);
        int destinationStride = checked(dimension * 4);
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
    }

    public byte[] CopyPixels(int x, int y, int width, int height)
    {
        int rowByteCount = checked(width * 4);
        int sourceStride = checked(dimension * 4);
        byte[] copied = new byte[checked(rowByteCount * height)];
        for (int row = 0; row < height; row++)
        {
            Pixels.AsSpan(
                checked(((y + row) * sourceStride) + (x * 4)),
                rowByteCount).CopyTo(copied.AsSpan(row * rowByteCount, rowByteCount));
        }
        return copied;
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
        dirtyLeft = int.MaxValue;
        dirtyTop = int.MaxValue;
        dirtyRight = 0;
        dirtyBottom = 0;
    }

    public void MarkUsed(long frameToken, long usageSequence)
    {
        if (frameToken != 0)
        {
            activeFrames.Add(frameToken);
        }
        LastUsedSequence = usageSequence;
    }

    public void EndFrame(long frameToken) => activeFrames.Remove(frameToken);

    public void Reset()
    {
        Array.Clear(Pixels);
        Keys.Clear();
        activeFrames.Clear();
        nextX = 0;
        nextY = 0;
        rowHeight = 0;
        LastUsedSequence = 0;
        MarkUploaded();
    }
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

internal readonly record struct SdlGpuGeometryBinding(
    nint VertexBuffer,
    nint IndexBuffer);

internal readonly record struct SdlGpuPipelineKey(
    SdlGpuTextureFormat ColorFormat,
    SdlGpuSampleCount SampleCount,
    DrawPrimitiveTopology Topology,
    DrawBlendMode BlendMode,
    SdlGpuStencilMode StencilMode,
    SdlGpuColorWriteMask ColorWriteMask);

internal readonly record struct SdlGpuSamplerKey(
    DrawSamplingMode Sampling,
    DrawAddressMode AddressMode);

internal readonly record struct SdlGpuLayerTargetKey(
    int Depth,
    int PixelWidth,
    int PixelHeight,
    SdlGpuTextureFormat ColorFormat,
    SdlGpuSampleCount SampleCount);
