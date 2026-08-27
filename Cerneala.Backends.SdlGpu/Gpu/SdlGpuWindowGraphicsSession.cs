using System.Numerics;
using System.Runtime.InteropServices;
using Cerneala.Drawing;
using Cerneala.Drawing.Prism;
using Cerneala.Drawing.Prism.Catalog;
using Cerneala.Platforms.Sdl3;
using Cerneala.UI.Hosting;
using Cerneala.UI.Hosting.Windowing;
using Cerneala.UI.Resources;
using SkiaSharp;

namespace Cerneala.Backends.SdlGpu;

internal sealed class SdlGpuWindowGraphicsSessionFactory :
    IWindowGraphicsSessionFactory,
    IDisposable
{
    private readonly object sync = new();
    private readonly ISdlApi api;
    private readonly SdlGpuPresentationOptions options;
    private SdlGpuDeviceOwner? deviceOwner;
    private bool disposed;

    public SdlGpuWindowGraphicsSessionFactory(
        ISdlApi api,
        bool useMultisampling = true)
        : this(api, SdlGpuPresentationOptions.CreateDefault(useMultisampling))
    {
    }

    internal SdlGpuWindowGraphicsSessionFactory(
        ISdlApi api,
        SdlGpuPresentationOptions options)
    {
        this.api = api ?? throw new ArgumentNullException(nameof(api));
        this.options = options;
    }

    public IWindowGraphicsSession Create(
        IWindowSurface windowSurface,
        int pixelWidth,
        int pixelHeight,
        float coordinateScale)
    {
        ArgumentNullException.ThrowIfNull(windowSurface);
        if (windowSurface is not SdlWindowSurface sdlSurface)
        {
            throw new ArgumentException(
                $"SDL_GPU requires a '{typeof(SdlWindowSurface).FullName}' window surface, " +
                $"but received '{windowSurface.GetType().FullName}'.",
                nameof(windowSurface));
        }

        SdlGpuDeviceLease lease;
        SdlGpuDebugLabels labels;
        lock (sync)
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            deviceOwner ??= new SdlGpuDeviceOwner(api);
            lease = deviceOwner.AcquireSession();
            labels = deviceOwner.DebugLabels;
        }

        try
        {
            return new SdlGpuWindowGraphicsSession(
                api,
                lease,
                sdlSurface,
                pixelWidth,
                pixelHeight,
                coordinateScale,
                options,
                labels);
        }
        catch
        {
            lease.Dispose();
            throw;
        }
    }

    public void Dispose()
    {
        lock (sync)
        {
            if (disposed)
            {
                return;
            }

            disposed = true;
            deviceOwner?.Dispose();
            deviceOwner = null;
        }
    }
}

internal sealed class SdlGpuWindowGraphicsSession :
    IWindowGraphicsSession,
    IWindowScreenshotSource,
    IWindowPresentedFrameSource,
    IBackdropFrameSource
{
    private readonly ISdlApi api;
    private readonly SdlGpuDeviceLease deviceLease;
    private readonly SdlWindowSurface windowSurface;
    private readonly SdlGpuPresentationOptions requestedOptions;
    private readonly SdlGpuDebugLabels debugLabels;
    private readonly SdlGpuDrawingBackend drawingBackend;
    private nint frameTexture;
    private nint multisampleTexture;
    private nint depthStencilTexture;
    private nint activeCommandBuffer;
    private nint activeRenderPass;
    private SdlGpuRenderTarget? activeTarget;
    private IDisposable? activeDebugGroup;
    private int pixelWidth;
    private int pixelHeight;
    private float coordinateScale;
    private BackdropFrameMetadata activeBackdropMetadata;
    private long contentVersion;
    private int activeBackdropLeaseCount;
    private bool windowClaimed;
    private bool suspended;
    private bool frameActive;
    private bool disposed;

    public SdlGpuWindowGraphicsSession(
        ISdlApi api,
        SdlGpuDeviceLease deviceLease,
        SdlWindowSurface windowSurface,
        int pixelWidth,
        int pixelHeight,
        float coordinateScale,
        SdlGpuPresentationOptions requestedOptions,
        SdlGpuDebugLabels debugLabels)
    {
        this.api = api ?? throw new ArgumentNullException(nameof(api));
        this.deviceLease = deviceLease ?? throw new ArgumentNullException(nameof(deviceLease));
        this.windowSurface = windowSurface ?? throw new ArgumentNullException(nameof(windowSurface));
        this.requestedOptions = requestedOptions;
        this.debugLabels = debugLabels ?? throw new ArgumentNullException(nameof(debugLabels));
        ValidateSize(pixelWidth, pixelHeight);
        UiCoordinateMapper.ValidateScale(coordinateScale);
        this.pixelWidth = pixelWidth;
        this.pixelHeight = pixelHeight;
        this.coordinateScale = coordinateScale;
        drawingBackend = new SdlGpuDrawingBackend(this);

        try
        {
            if (!api.ClaimWindowForGpuDevice(deviceLease.Device, windowSurface.WindowHandle))
            {
                throw SdlApiError.Create(api, "SDL GPU window claim");
            }

            windowClaimed = true;
            Diagnostics = ConfigurePresentation();
            suspended = pixelWidth == 0 || pixelHeight == 0;
            if (!suspended)
            {
                CreateSizeResources();
            }
        }
        catch
        {
            ReleaseSizeResources();
            if (windowClaimed)
            {
                api.ReleaseWindowFromGpuDevice(deviceLease.Device, windowSurface.WindowHandle);
                windowClaimed = false;
            }

            throw;
        }
    }

    public IDrawingBackend DrawingBackend => drawingBackend;

    public IImageLoader? ImageLoader => deviceLease.ImageLoader;

    public ImageResourceCache? ImageResourceCache => deviceLease.ImageResourceCache;

    PrismExecutionDiagnostics? IWindowGraphicsSession.PrismExecutionDiagnostics =>
        drawingBackend.PrismDiagnostics;

    int IWindowGraphicsSession.ActiveBackdropLeaseCount => activeBackdropLeaseCount;

    internal SdlGpuPresentationDiagnostics Diagnostics { get; private set; }

    internal nint FrameTexture => frameTexture;

    internal nint MultisampleTexture => multisampleTexture;

    internal nint DepthStencilTexture => depthStencilTexture;

    internal bool IsSuspended => suspended;

    internal bool IsFrameActive => frameActive;

    internal float CoordinateScale => coordinateScale;

    internal int PixelWidth => pixelWidth;

    internal int PixelHeight => pixelHeight;

    internal long WindowIdentity => windowSurface.WindowId;

    internal nint Device => deviceLease.Device;

    internal ISdlApi Api => api;

    internal nint ActiveCommandBuffer => activeCommandBuffer;

    internal nint ActiveRenderPass => activeRenderPass;

    internal SdlGpuDrawingResources DrawingResources => deviceLease.DrawingResources;

    internal SdlGpuRenderTarget WindowRenderTarget => new(
        multisampleTexture != 0 ? multisampleTexture : frameTexture,
        depthStencilTexture,
        pixelWidth,
        pixelHeight,
        Diagnostics.TextureFormat,
        Diagnostics.SampleCount,
        multisampleTexture != 0 ? frameTexture : 0);

    public bool IsCompatibleWith(IDrawingBackend backend)
    {
        ArgumentNullException.ThrowIfNull(backend);
        return !disposed && ReferenceEquals(backend, drawingBackend);
    }

    public IBackdropFrameLease AcquireFrame(in BackdropFrameRequest request)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        if (!frameActive || suspended || frameTexture == 0)
        {
            throw new InvalidOperationException(
                "A backdrop frame can be acquired only while a drawable SDL_GPU frame is active.");
        }
        if (request.PixelWidth != activeBackdropMetadata.PixelWidth ||
            request.PixelHeight != activeBackdropMetadata.PixelHeight ||
            request.PixelScale != activeBackdropMetadata.PixelScale)
        {
            throw new InvalidOperationException(
                $"Backdrop request {request.PixelWidth}x{request.PixelHeight} at scale " +
                $"{request.PixelScale} does not match the active SDL_GPU frame " +
                $"{activeBackdropMetadata.PixelWidth}x{activeBackdropMetadata.PixelHeight} " +
                $"at scale {activeBackdropMetadata.PixelScale}.");
        }

        activeBackdropLeaseCount = checked(activeBackdropLeaseCount + 1);
        return new BackdropFrameLease(this, frameTexture, activeBackdropMetadata);
    }

    public void Resize(int pixelWidth, int pixelHeight, float coordinateScale)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        ValidateSize(pixelWidth, pixelHeight);
        UiCoordinateMapper.ValidateScale(coordinateScale);
        if (frameActive)
        {
            throw new InvalidOperationException(
                "The SDL_GPU graphics session cannot be resized while a frame is active.");
        }

        bool sizeChanged = pixelWidth != this.pixelWidth || pixelHeight != this.pixelHeight;
        this.pixelWidth = pixelWidth;
        this.pixelHeight = pixelHeight;
        this.coordinateScale = coordinateScale;
        drawingBackend.CoordinateScale = coordinateScale;
        bool nextSuspended = pixelWidth == 0 || pixelHeight == 0;
        if (!sizeChanged && suspended == nextSuspended)
        {
            return;
        }

        ReleaseSizeResources();
        suspended = nextSuspended;
        if (!suspended)
        {
            CreateSizeResources();
        }
    }

    public void BeginFrame(Color clearColor)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        if (frameActive)
        {
            throw new InvalidOperationException("The SDL_GPU graphics session already has an active frame.");
        }

        EnsureNoActiveBackdropLeases("begin another frame");

        frameActive = true;
        drawingBackend.BeginFrame();
        if (suspended)
        {
            activeBackdropMetadata = default;
            return;
        }

        try
        {
            activeCommandBuffer = RequireHandle(
                api.AcquireGpuCommandBuffer(deviceLease.Device),
                "SDL GPU command-buffer acquisition");
            activeDebugGroup = debugLabels.Push(activeCommandBuffer, $"Cerneala window {windowSurface.WindowId} frame");
            contentVersion = checked(contentVersion + 1);
            activeBackdropMetadata = new BackdropFrameMetadata(
                pixelWidth,
                pixelHeight,
                coordinateScale,
                PrismColorProfile.Srgb,
                ToBackdropPixelFormat(Diagnostics.TextureFormat),
                BackdropAlphaMode.Premultiplied,
                Matrix3x2.CreateScale(coordinateScale),
                contentVersion);
            BeginRenderTarget(WindowRenderTarget, clearColor, SdlGpuLoadOp.Clear);
        }
        catch
        {
            CancelActiveCommandBuffer();
            EndFrameState();
            throw;
        }
    }

    public void Present() => CompleteFrame(present: true);

    public void CompleteFrame(bool present)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        if (!frameActive)
        {
            throw new InvalidOperationException("The SDL_GPU graphics session has no active frame.");
        }

        if (suspended)
        {
            EndFrameState();
            return;
        }

        bool commandSubmitted = false;
        bool swapchainAcquired = false;
        try
        {
            EnsureNoActiveBackdropLeases(present ? "present" : "complete a frame");
            EndActiveRenderPass();
            activeDebugGroup?.Dispose();
            activeDebugGroup = null;

            if (present)
            {
                PresentFrame(ref commandSubmitted, ref swapchainAcquired);
            }

            SubmitActiveCommandBuffer(ref commandSubmitted);
            deviceLease.DrawingResources.FlushRetired();
        }
        finally
        {
            if (!commandSubmitted && !swapchainAcquired)
            {
                CancelActiveCommandBuffer();
            }

            EndFrameState();
        }
    }

    public WindowPreviewFrame CapturePresentedFrame(byte[]? reusablePixels = null)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        if (frameActive)
        {
            throw new InvalidOperationException(
                "The SDL_GPU presented frame cannot be captured while another frame is active.");
        }

        if (suspended || frameTexture == 0)
        {
            throw new InvalidOperationException(
                "The SDL_GPU presented frame is unavailable while the window has zero pixel size.");
        }

        int pixelsPerRow = checked(Align(pixelWidth, 64));
        int sourceStride = checked(pixelsPerRow * 4);
        int transferSize = checked(sourceStride * pixelHeight);
        nint transferBuffer = 0;
        nint commandBuffer = 0;
        nint copyPass = 0;
        nint fence = 0;
        nint mapped = 0;
        bool submitted = false;
        try
        {
            SdlGpuTransferBufferCreateInfo transferInfo = new(
                SdlGpuTransferBufferUsage.Download,
                checked((uint)transferSize));
            transferBuffer = RequireHandle(
                api.CreateGpuTransferBuffer(deviceLease.Device, transferInfo),
                "SDL GPU readback transfer-buffer creation");
            commandBuffer = RequireHandle(
                api.AcquireGpuCommandBuffer(deviceLease.Device),
                "SDL GPU readback command-buffer acquisition");
            copyPass = RequireHandle(
                api.BeginGpuCopyPass(commandBuffer),
                "SDL GPU readback copy-pass creation");
            SdlGpuTextureRegion source = new(
                frameTexture,
                checked((uint)pixelWidth),
                checked((uint)pixelHeight));
            SdlGpuTextureTransferInfo destination = new(
                transferBuffer,
                Offset: 0,
                PixelsPerRow: checked((uint)pixelsPerRow),
                RowsPerLayer: checked((uint)pixelHeight));
            api.DownloadFromGpuTexture(copyPass, source, destination);
            api.EndGpuCopyPass(copyPass);
            copyPass = 0;
            fence = RequireHandle(
                api.SubmitGpuCommandBufferAndAcquireFence(commandBuffer),
                "SDL GPU readback submission");
            submitted = true;
            commandBuffer = 0;
            if (!api.WaitForGpuFence(deviceLease.Device, fence))
            {
                throw SdlApiError.Create(api, "SDL GPU readback fence wait");
            }

            mapped = RequireHandle(
                api.MapGpuTransferBuffer(deviceLease.Device, transferBuffer, cycle: false),
                "SDL GPU readback transfer-buffer mapping");
            byte[] sourcePixels = new byte[transferSize];
            Marshal.Copy(mapped, sourcePixels, 0, transferSize);
            int destinationStride = checked(pixelWidth * 4);
            int destinationLength = checked(destinationStride * pixelHeight);
            byte[] pixels = reusablePixels is { Length: var length } && length == destinationLength
                ? reusablePixels
                : new byte[destinationLength];
            NormalizeRgba(
                sourcePixels,
                sourceStride,
                pixels,
                destinationStride,
                pixelWidth,
                pixelHeight,
                Diagnostics.TextureFormat);
            return new WindowPreviewFrame(pixels, pixelWidth, pixelHeight, destinationStride);
        }
        finally
        {
            if (mapped != 0)
            {
                api.UnmapGpuTransferBuffer(deviceLease.Device, transferBuffer);
            }

            if (copyPass != 0)
            {
                api.EndGpuCopyPass(copyPass);
            }

            if (!submitted && commandBuffer != 0)
            {
                api.CancelGpuCommandBuffer(commandBuffer);
            }

            if (fence != 0)
            {
                api.ReleaseGpuFence(deviceLease.Device, fence);
            }

            if (transferBuffer != 0)
            {
                api.ReleaseGpuTransferBuffer(deviceLease.Device, transferBuffer);
            }
        }
    }

    public void RenderPng(
        Stream output,
        Color clearColor,
        Action<IDrawingBackend> draw)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        ArgumentNullException.ThrowIfNull(output);
        ArgumentNullException.ThrowIfNull(draw);
        if (!output.CanWrite)
        {
            throw new ArgumentException("The screenshot stream must be writable.", nameof(output));
        }

        if (frameActive)
        {
            throw new InvalidOperationException(
                "An SDL_GPU screenshot cannot be rendered while an on-screen frame is active.");
        }

        BeginFrame(clearColor);
        try
        {
            draw(drawingBackend);
        }
        finally
        {
            CompleteFrame(present: false);
        }

        WindowPreviewFrame frame = CapturePresentedFrame();
        using SKBitmap bitmap = new(new SKImageInfo(
            frame.PixelWidth,
            frame.PixelHeight,
            SKColorType.Rgba8888,
            SKAlphaType.Unpremul));
        Marshal.Copy(frame.Pixels, 0, bitmap.GetPixels(), frame.Pixels.Length);
        using SKImage image = SKImage.FromBitmap(bitmap);
        using SKData data = image.Encode(SKEncodedImageFormat.Png, quality: 100) ??
            throw new InvalidOperationException("Skia could not encode the SDL_GPU screenshot as PNG.");
        data.SaveTo(output);
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        Exception? failure = null;
        if (frameActive)
        {
            try
            {
                if (activeRenderPass != 0)
                {
                    EndActiveRenderPass();
                }

                activeDebugGroup?.Dispose();
                activeDebugGroup = null;
                CancelActiveCommandBuffer();
            }
            catch (Exception exception)
            {
                failure = exception;
            }

            EndFrameState();
        }

        try
        {
            drawingBackend.Dispose();
            ReleaseSizeResources();
        }
        catch (Exception exception)
        {
            failure ??= exception;
        }

        if (windowClaimed)
        {
            try
            {
                api.ReleaseWindowFromGpuDevice(deviceLease.Device, windowSurface.WindowHandle);
                windowClaimed = false;
            }
            catch (Exception exception)
            {
                failure ??= exception;
            }
        }

        try
        {
            deviceLease.Dispose();
        }
        catch (Exception exception)
        {
            failure ??= exception;
        }

        if (failure is not null)
        {
            throw new InvalidOperationException(
                $"Could not dispose the SDL_GPU graphics session for window {windowSurface.WindowId}.",
                failure);
        }
    }

    internal static void NormalizeRgba(
        ReadOnlySpan<byte> source,
        int sourceStride,
        Span<byte> destination,
        int destinationStride,
        int width,
        int height,
        SdlGpuTextureFormat format)
    {
        if (width < 0 || height < 0 || sourceStride < checked(width * 4) ||
            destinationStride < checked(width * 4))
        {
            throw new ArgumentOutOfRangeException(nameof(width), "Invalid pixel dimensions or row pitch.");
        }

        int requiredSource = checked(sourceStride * height);
        int requiredDestination = checked(destinationStride * height);
        if (source.Length < requiredSource || destination.Length < requiredDestination)
        {
            throw new ArgumentException("The pixel buffers are smaller than their declared row layout.");
        }

        bool bgra = format is SdlGpuTextureFormat.B8G8R8A8Unorm or
            SdlGpuTextureFormat.B8G8R8A8UnormSrgb;
        bool rgba = format is SdlGpuTextureFormat.R8G8B8A8Unorm or
            SdlGpuTextureFormat.R8G8B8A8UnormSrgb;
        if (!bgra && !rgba)
        {
            throw new NotSupportedException(
                $"SDL_GPU readback does not support texture format '{format}'.");
        }

        for (int y = 0; y < height; y++)
        {
            ReadOnlySpan<byte> sourceRow = source.Slice(y * sourceStride, width * 4);
            Span<byte> destinationRow = destination.Slice(y * destinationStride, width * 4);
            if (!bgra)
            {
                sourceRow.CopyTo(destinationRow);
                continue;
            }

            for (int x = 0; x < width; x++)
            {
                int offset = x * 4;
                destinationRow[offset] = sourceRow[offset + 2];
                destinationRow[offset + 1] = sourceRow[offset + 1];
                destinationRow[offset + 2] = sourceRow[offset];
                destinationRow[offset + 3] = sourceRow[offset + 3];
            }
        }
    }

    private SdlGpuPresentationDiagnostics ConfigurePresentation()
    {
        List<string> fallbacks = [];
        SdlGpuSwapchainComposition composition = requestedOptions.Composition;
        if (!api.WindowSupportsGpuSwapchainComposition(
            deviceLease.Device,
            windowSurface.WindowHandle,
            composition))
        {
            fallbacks.Add(
                $"Swapchain composition '{composition}' is unavailable; using '{SdlGpuSwapchainComposition.Sdr}'.");
            composition = SdlGpuSwapchainComposition.Sdr;
        }

        SdlGpuPresentMode presentMode = requestedOptions.PresentMode;
        if (!api.WindowSupportsGpuPresentMode(
            deviceLease.Device,
            windowSurface.WindowHandle,
            presentMode))
        {
            fallbacks.Add(
                $"Present mode '{presentMode}' is unavailable; using '{SdlGpuPresentMode.VSync}'.");
            presentMode = SdlGpuPresentMode.VSync;
        }

        if (!api.SetGpuSwapchainParameters(
            deviceLease.Device,
            windowSurface.WindowHandle,
            composition,
            presentMode))
        {
            throw SdlApiError.Create(api, "SDL GPU swapchain configuration");
        }

        SdlGpuTextureFormat swapchainFormat = api.GetGpuSwapchainTextureFormat(
            deviceLease.Device,
            windowSurface.WindowHandle);
        SdlGpuTextureFormat textureFormat = SelectTextureFormat(swapchainFormat, fallbacks);
        SdlGpuSampleCount sampleCount = SelectSampleCount(textureFormat, fallbacks);
        return new SdlGpuPresentationDiagnostics(
            composition,
            presentMode,
            textureFormat,
            sampleCount,
            fallbacks.ToArray());
    }

    private SdlGpuTextureFormat SelectTextureFormat(
        SdlGpuTextureFormat swapchainFormat,
        List<string> fallbacks)
    {
        SdlGpuTextureUsage usage = SdlGpuTextureUsage.ColorTarget | SdlGpuTextureUsage.Sampler;
        SdlGpuTextureFormat[] candidates =
        [
            swapchainFormat,
            SdlGpuTextureFormat.R8G8B8A8Unorm,
            SdlGpuTextureFormat.B8G8R8A8Unorm
        ];
        foreach (SdlGpuTextureFormat candidate in candidates.Distinct())
        {
            if (candidate != SdlGpuTextureFormat.Invalid &&
                api.GpuTextureSupportsFormat(deviceLease.Device, candidate, usage))
            {
                if (candidate != swapchainFormat)
                {
                    fallbacks.Add(
                        $"Swapchain texture format '{swapchainFormat}' cannot be used for the offscreen frame; using '{candidate}'.");
                }

                return candidate;
            }
        }

        throw new InvalidOperationException(
            "SDL GPU supports no RGBA/BGRA color-target and sampler format for the window frame.");
    }

    private SdlGpuSampleCount SelectSampleCount(
        SdlGpuTextureFormat format,
        List<string> fallbacks)
    {
        SdlGpuSampleCount[] candidates = requestedOptions.SampleCount switch
        {
            SdlGpuSampleCount.Eight =>
                [SdlGpuSampleCount.Eight, SdlGpuSampleCount.Four, SdlGpuSampleCount.Two, SdlGpuSampleCount.One],
            SdlGpuSampleCount.Four =>
                [SdlGpuSampleCount.Four, SdlGpuSampleCount.Two, SdlGpuSampleCount.One],
            SdlGpuSampleCount.Two =>
                [SdlGpuSampleCount.Two, SdlGpuSampleCount.One],
            _ => [SdlGpuSampleCount.One]
        };
        foreach (SdlGpuSampleCount candidate in candidates)
        {
            if (candidate == SdlGpuSampleCount.One ||
                api.GpuTextureSupportsSampleCount(deviceLease.Device, format, candidate))
            {
                if (candidate != requestedOptions.SampleCount)
                {
                    fallbacks.Add(
                        $"MSAA '{requestedOptions.SampleCount}' is unavailable for '{format}'; using '{candidate}'.");
                }

                return candidate;
            }
        }

        return SdlGpuSampleCount.One;
    }

    private void CreateSizeResources()
    {
        SdlGpuTextureCreateInfo frameInfo = new(
            Diagnostics.TextureFormat,
            SdlGpuTextureUsage.ColorTarget | SdlGpuTextureUsage.Sampler,
            checked((uint)pixelWidth),
            checked((uint)pixelHeight));
        frameTexture = RequireHandle(
            api.CreateGpuTexture(deviceLease.Device, frameInfo),
            "SDL GPU frame-texture creation");
        try
        {
            SdlGpuTextureCreateInfo depthInfo = new(
                SdlGpuTextureFormat.D24UnormS8Uint,
                SdlGpuTextureUsage.DepthStencilTarget,
                checked((uint)pixelWidth),
                checked((uint)pixelHeight),
                Diagnostics.SampleCount);
            depthStencilTexture = RequireHandle(
                api.CreateGpuTexture(deviceLease.Device, depthInfo),
                "SDL GPU depth/stencil-texture creation");

            if (Diagnostics.SampleCount != SdlGpuSampleCount.One)
            {
                SdlGpuTextureCreateInfo multisampleInfo = new(
                    Diagnostics.TextureFormat,
                    SdlGpuTextureUsage.ColorTarget,
                    checked((uint)pixelWidth),
                    checked((uint)pixelHeight),
                    Diagnostics.SampleCount);
                multisampleTexture = RequireHandle(
                    api.CreateGpuTexture(deviceLease.Device, multisampleInfo),
                    "SDL GPU multisample-texture creation");
            }
        }
        catch
        {
            if (depthStencilTexture != 0)
            {
                api.ReleaseGpuTexture(deviceLease.Device, depthStencilTexture);
                depthStencilTexture = 0;
            }
            api.ReleaseGpuTexture(deviceLease.Device, frameTexture);
            frameTexture = 0;
            throw;
        }
    }

    private void ReleaseSizeResources()
    {
        if (depthStencilTexture != 0)
        {
            api.ReleaseGpuTexture(deviceLease.Device, depthStencilTexture);
            depthStencilTexture = 0;
        }

        if (multisampleTexture != 0)
        {
            api.ReleaseGpuTexture(deviceLease.Device, multisampleTexture);
            multisampleTexture = 0;
        }

        if (frameTexture != 0)
        {
            api.ReleaseGpuTexture(deviceLease.Device, frameTexture);
            frameTexture = 0;
        }
    }

    private void PresentFrame(
        ref bool commandSubmitted,
        ref bool swapchainAcquired)
    {
        if (!api.WaitAndAcquireGpuSwapchainTexture(
            activeCommandBuffer,
            windowSurface.WindowHandle,
            out nint swapchainTexture,
            out uint swapchainWidth,
            out uint swapchainHeight))
        {
            SubmitActiveCommandBuffer(ref commandSubmitted);
            Diagnostics = ConfigurePresentation();
            activeCommandBuffer = RequireHandle(
                api.AcquireGpuCommandBuffer(deviceLease.Device),
                "SDL GPU recovery command-buffer acquisition");
            commandSubmitted = false;
            if (!api.WaitAndAcquireGpuSwapchainTexture(
                activeCommandBuffer,
                windowSurface.WindowHandle,
                out swapchainTexture,
                out swapchainWidth,
                out swapchainHeight))
            {
                throw SdlApiError.Create(api, "SDL GPU swapchain reacquisition");
            }
        }

        swapchainAcquired = true;
        if (swapchainTexture == 0)
        {
            return;
        }

        SdlGpuBlitInfo blit = new(
            frameTexture,
            checked((uint)pixelWidth),
            checked((uint)pixelHeight),
            swapchainTexture,
            swapchainWidth,
            swapchainHeight,
            LinearFilter: pixelWidth != swapchainWidth || pixelHeight != swapchainHeight);
        api.BlitGpuTexture(activeCommandBuffer, blit);
    }

    private void CancelActiveCommandBuffer()
    {
        activeDebugGroup?.Dispose();
        activeDebugGroup = null;
        if (activeCommandBuffer != 0)
        {
            api.CancelGpuCommandBuffer(activeCommandBuffer);
            activeCommandBuffer = 0;
        }

        activeRenderPass = 0;
        activeTarget = null;
    }

    private void EndFrameState()
    {
        activeRenderPass = 0;
        activeTarget = null;
        activeCommandBuffer = 0;
        activeDebugGroup?.Dispose();
        activeDebugGroup = null;
        drawingBackend.EndFrame();
        activeBackdropMetadata = default;
        frameActive = false;
    }

    private void EnsureNoActiveBackdropLeases(string operation)
    {
        if (activeBackdropLeaseCount != 0)
        {
            throw new InvalidOperationException(
                $"The SDL_GPU graphics session cannot {operation} while " +
                $"{activeBackdropLeaseCount} backdrop lease(s) are still active.");
        }
    }

    private void ValidateBackdropLease(nint texture, long version)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        if (!frameActive || frameTexture != texture ||
            activeBackdropMetadata.ContentVersion != version)
        {
            throw new InvalidOperationException(
                "The backdrop lease is no longer valid for the active SDL_GPU frame.");
        }
    }

    private void ReleaseBackdropLease()
    {
        if (activeBackdropLeaseCount <= 0)
        {
            throw new InvalidOperationException(
                "The SDL_GPU backdrop lease count is already zero.");
        }
        activeBackdropLeaseCount--;
    }

    internal void RunCopyPass(Action<nint> copy)
    {
        ArgumentNullException.ThrowIfNull(copy);
        if (!frameActive || activeCommandBuffer == 0)
        {
            throw new InvalidOperationException(
                "SDL GPU uploads require an active command buffer.");
        }

        SdlGpuRenderTarget target = activeTarget ??
            throw new InvalidOperationException("SDL GPU uploads require an active render target.");
        EndActiveRenderPass();
        nint copyPass = RequireHandle(
            api.BeginGpuCopyPass(activeCommandBuffer),
            "SDL GPU drawing copy-pass creation");
        try
        {
            copy(copyPass);
        }
        finally
        {
            api.EndGpuCopyPass(copyPass);
        }
        BeginRenderTarget(target, Color.Transparent, SdlGpuLoadOp.Load);
    }

    internal void BeginRenderTarget(
        SdlGpuRenderTarget target,
        Color clearColor,
        SdlGpuLoadOp loadOp)
    {
        ArgumentNullException.ThrowIfNull(target);
        if (activeRenderPass != 0)
        {
            EndActiveRenderPass();
        }

        SdlGpuColorTargetInfo colorTarget = new(
            target.ColorTexture,
            ToGpuColor(clearColor),
            loadOp,
            target.ResolveTexture != 0
                ? SdlGpuStoreOp.ResolveAndStore
                : SdlGpuStoreOp.Store,
            target.ResolveTexture,
            Cycle: loadOp != SdlGpuLoadOp.Load,
            CycleResolveTexture:
                target.ResolveTexture != 0 && loadOp != SdlGpuLoadOp.Load);
        activeRenderPass = RequireHandle(
            target.DepthStencilTexture == 0
                ? api.BeginGpuRenderPass(activeCommandBuffer, colorTarget)
                : api.BeginGpuRenderPass(
                    activeCommandBuffer,
                    colorTarget,
                    new SdlGpuDepthStencilTargetInfo(
                        target.DepthStencilTexture,
                        loadOp,
                        SdlGpuStoreOp.Store,
                        loadOp,
                        SdlGpuStoreOp.Store,
                        Cycle: loadOp != SdlGpuLoadOp.Load)),
            "SDL GPU render-pass creation");
        SdlGpuViewport viewport = new(0, 0, target.PixelWidth, target.PixelHeight);
        api.SetGpuViewport(activeRenderPass, viewport);
        activeTarget = target;
    }

    internal void EndActiveRenderPass()
    {
        if (activeRenderPass == 0)
        {
            return;
        }

        api.EndGpuRenderPass(activeRenderPass);
        activeRenderPass = 0;
    }

    internal void GenerateMipmaps(SdlGpuRenderTarget target)
    {
        ArgumentNullException.ThrowIfNull(target);
        if (target.MipLevelCount <= 1)
        {
            return;
        }
        if (!frameActive || activeCommandBuffer == 0)
        {
            throw new InvalidOperationException(
                "SDL GPU mipmap generation requires an active command buffer.");
        }

        EndActiveRenderPass();
        api.GenerateGpuMipmaps(activeCommandBuffer, target.SampleTexture);
    }

    private void SubmitActiveCommandBuffer(ref bool commandSubmitted)
    {
        if (!api.SubmitGpuCommandBuffer(activeCommandBuffer))
        {
            throw SdlApiError.Create(api, "SDL GPU command-buffer submission");
        }

        commandSubmitted = true;
        activeCommandBuffer = 0;
    }

    private nint RequireHandle(nint handle, string operation) =>
        handle != 0 ? handle : throw SdlApiError.Create(api, operation);

    private static SdlGpuColor ToGpuColor(Color color) =>
        new(
            color.R / 255f,
            color.G / 255f,
            color.B / 255f,
            color.A / 255f);

    private static int Align(int value, int alignment) =>
        checked(((value + alignment - 1) / alignment) * alignment);

    private static void ValidateSize(int pixelWidth, int pixelHeight)
    {
        if (pixelWidth < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(pixelWidth));
        }

        if (pixelHeight < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(pixelHeight));
        }
    }

    private static BackdropPixelFormat ToBackdropPixelFormat(SdlGpuTextureFormat format) =>
        format switch
        {
            SdlGpuTextureFormat.B8G8R8A8Unorm or SdlGpuTextureFormat.B8G8R8A8UnormSrgb =>
                BackdropPixelFormat.Bgra8Unorm,
            SdlGpuTextureFormat.R8G8B8A8Unorm or SdlGpuTextureFormat.R8G8B8A8UnormSrgb =>
                BackdropPixelFormat.Rgba8Unorm,
            _ => throw new NotSupportedException(
                $"SDL_GPU cannot expose texture format '{format}' as a backdrop.")
        };

    private sealed class BackdropFrameLease : ISdlGpuBackdropFrameLease
    {
        private SdlGpuWindowGraphicsSession? owner;
        private readonly nint texture;
        private readonly BackdropFrameMetadata metadata;

        public BackdropFrameLease(
            SdlGpuWindowGraphicsSession owner,
            nint texture,
            BackdropFrameMetadata metadata)
        {
            this.owner = owner;
            this.texture = texture;
            this.metadata = metadata;
        }

        public BackdropFrameMetadata Metadata
        {
            get
            {
                RequireOwner().ValidateBackdropLease(texture, metadata.ContentVersion);
                return metadata;
            }
        }

        public nint Texture
        {
            get
            {
                RequireOwner().ValidateBackdropLease(texture, metadata.ContentVersion);
                return texture;
            }
        }

        public void Dispose() =>
            Interlocked.Exchange(ref owner, null)?.ReleaseBackdropLease();

        private SdlGpuWindowGraphicsSession RequireOwner() =>
            owner ?? throw new ObjectDisposedException(nameof(BackdropFrameLease));
    }
}
