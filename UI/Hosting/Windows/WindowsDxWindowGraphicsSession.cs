using System.Numerics;
using Cerneala.Drawing;
using Cerneala.Drawing.MonoGame;
using Cerneala.Drawing.MonoGame.Prism;
using Cerneala.Drawing.Prism;
using Cerneala.Drawing.Prism.Catalog;
using Cerneala.Drawing.Text;
using Cerneala.UI.Hosting;
using Cerneala.UI.Resources;
using Cerneala.UI.Resources.MonoGame;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using CernealaColor = Cerneala.Drawing.Color;
using XnaColor = Microsoft.Xna.Framework.Color;

namespace Cerneala.UI.Hosting.Windows;

internal sealed class WindowsDxWindowGraphicsSessionFactory : IWindowGraphicsSessionFactory
{
    public IWindowGraphicsSession Create(nint windowHandle, int pixelWidth, int pixelHeight, float coordinateScale)
    {
        return new WindowsDxWindowGraphicsSession(windowHandle, pixelWidth, pixelHeight, coordinateScale);
    }
}

internal sealed class WindowsDxWindowGraphicsSession :
    IWindowGraphicsSession,
    IWindowScreenshotSource,
    IBackdropFrameSource
{
    private readonly nint windowHandle;
    private readonly GraphicsDevice graphicsDevice;
    private readonly SpriteBatch spriteBatch;
    private readonly Texture2D whitePixel;
    private readonly MonoGameDrawingBackend drawingBackend;
    private readonly ImageResourceCache imageResourceCache;
    private PresentationParameters presentationParameters;
    private RenderTarget2D? frameTarget;
    private RenderTarget2D? activeBackdropTarget;
    private BackdropFrameMetadata activeBackdropMetadata;
    private float coordinateScale;
    private FrameKind activeFrameKind;
    private int activeBackdropLeaseCount;
    private long contentVersion;
    private bool disposed;

    public WindowsDxWindowGraphicsSession(nint windowHandle, int pixelWidth, int pixelHeight, float coordinateScale)
    {
        if (windowHandle == 0)
        {
            throw new ArgumentException("A native window handle is required.", nameof(windowHandle));
        }

        ValidateSize(pixelWidth, pixelHeight);
        UiCoordinateMapper.ValidateScale(coordinateScale);
        this.windowHandle = windowHandle;
        this.coordinateScale = coordinateScale;

        GraphicsDevice? createdDevice = null;
        SpriteBatch? createdSpriteBatch = null;
        Texture2D? createdWhitePixel = null;
        MonoGameDrawingBackend? createdBackend = null;
        ImageResourceCache? createdImageCache = null;
        RenderTarget2D? createdFrameTarget = null;
        try
        {
            presentationParameters = CreatePresentationParameters(windowHandle, pixelWidth, pixelHeight);
            createdDevice = CreateGraphicsDevice(presentationParameters);
            createdSpriteBatch = new SpriteBatch(createdDevice);
            createdWhitePixel = new Texture2D(createdDevice, 1, 1);
            createdWhitePixel.SetData([XnaColor.White]);
            createdBackend = new MonoGameDrawingBackend(createdSpriteBatch, createdWhitePixel, new SkiaTextRasterizer())
            {
                CoordinateScale = coordinateScale
            };
            ImageLoader = new MonoGameImageLoader(createdDevice);
            createdImageCache = new ImageResourceCache(ImageLoader);
            createdFrameTarget = CreateFrameTarget(
                createdDevice,
                presentationParameters);

            graphicsDevice = createdDevice;
            spriteBatch = createdSpriteBatch;
            whitePixel = createdWhitePixel;
            drawingBackend = createdBackend;
            imageResourceCache = createdImageCache;
            frameTarget = createdFrameTarget;
            graphicsDevice.DeviceResetting += OnDeviceResetting;
            graphicsDevice.DeviceReset += OnDeviceReset;
        }
        catch (Exception exception)
        {
            createdFrameTarget?.Dispose();
            createdImageCache?.Dispose();
            createdBackend?.Dispose();
            createdWhitePixel?.Dispose();
            createdSpriteBatch?.Dispose();
            createdDevice?.Dispose();
            throw CreateGraphicsException("create", windowHandle, pixelWidth, pixelHeight, exception);
        }
    }

    public IDrawingBackend DrawingBackend => drawingBackend;

    public IImageLoader ImageLoader { get; }

    public ImageResourceCache ImageResourceCache => imageResourceCache;

    internal GraphicsDevice GraphicsDevice => graphicsDevice;

    internal nint WindowHandle => windowHandle;

    internal RenderTarget2D? FrameTarget => frameTarget;

    internal int ActiveBackdropLeaseCount =>
        activeBackdropLeaseCount;

    internal long BackdropContentVersion => contentVersion;

    internal bool IsFrameActive =>
        activeFrameKind != FrameKind.None;

    public bool IsCompatibleWith(IDrawingBackend drawingBackend)
    {
        ArgumentNullException.ThrowIfNull(drawingBackend);
        return !disposed &&
            drawingBackend is MonoGameDrawingBackend monoGameBackend &&
            monoGameBackend.UsesGraphicsDevice(graphicsDevice);
    }

    public IBackdropFrameLease AcquireFrame(
        in BackdropFrameRequest request)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        RenderTarget2D target =
            activeBackdropTarget ??
            throw new InvalidOperationException(
                "A backdrop frame can be acquired only while a WindowsDX frame is active.");
        if (request.PixelWidth != activeBackdropMetadata.PixelWidth ||
            request.PixelHeight != activeBackdropMetadata.PixelHeight ||
            request.PixelScale != activeBackdropMetadata.PixelScale)
        {
            throw new InvalidOperationException(
                $"Backdrop request {request.PixelWidth}x{request.PixelHeight} at scale " +
                $"{request.PixelScale} does not match the active WindowsDX frame " +
                $"{activeBackdropMetadata.PixelWidth}x{activeBackdropMetadata.PixelHeight} " +
                $"at scale {activeBackdropMetadata.PixelScale}.");
        }

        activeBackdropLeaseCount = checked(
            activeBackdropLeaseCount + 1);
        return new BackdropFrameLease(
            this,
            target,
            activeBackdropMetadata);
    }

    public void Resize(int pixelWidth, int pixelHeight, float coordinateScale)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        ValidateSize(pixelWidth, pixelHeight);
        UiCoordinateMapper.ValidateScale(coordinateScale);
        if (activeFrameKind != FrameKind.None)
        {
            throw new InvalidOperationException(
                "The WindowsDX graphics session cannot be resized while a frame is active.");
        }

        bool sizeChanged = presentationParameters.BackBufferWidth != pixelWidth ||
            presentationParameters.BackBufferHeight != pixelHeight;
        this.coordinateScale = coordinateScale;
        drawingBackend.CoordinateScale = coordinateScale;
        if (!sizeChanged)
        {
            return;
        }

        presentationParameters.BackBufferWidth = pixelWidth;
        presentationParameters.BackBufferHeight = pixelHeight;
        try
        {
            graphicsDevice.Reset(presentationParameters);
        }
        catch (Exception exception)
        {
            throw CreateGraphicsException("resize", windowHandle, pixelWidth, pixelHeight, exception);
        }
    }

    public void BeginFrame(CernealaColor clearColor)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        drawingBackend.CoordinateScale = coordinateScale;
        BeginBackdropFrame(
            FrameKind.OnScreen,
            RequireFrameTarget(),
            clearColor);
    }

    public void Present()
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        if (activeFrameKind != FrameKind.OnScreen ||
            activeBackdropTarget is not RenderTarget2D target)
        {
            throw new InvalidOperationException(
                "The WindowsDX graphics session has no on-screen frame to present.");
        }
        bool batchBegun = false;
        try
        {
            EnsureNoActiveBackdropLeases("present");
            try
            {
                graphicsDevice.SetRenderTarget(null);
                spriteBatch.Begin(
                    SpriteSortMode.Deferred,
                    BlendState.Opaque,
                    SamplerState.LinearClamp,
                    DepthStencilState.None,
                    RasterizerState.CullNone);
                batchBegun = true;
                spriteBatch.Draw(
                    target,
                    new Rectangle(
                        0,
                        0,
                        presentationParameters.BackBufferWidth,
                        presentationParameters.BackBufferHeight),
                    XnaColor.White);
                spriteBatch.End();
                batchBegun = false;
                graphicsDevice.Present();
            }
            catch (Exception exception)
            {
                throw CreateGraphicsException(
                    "present",
                    windowHandle,
                    presentationParameters.BackBufferWidth,
                    presentationParameters.BackBufferHeight,
                    exception);
            }
        }
        finally
        {
            if (batchBegun)
            {
                try
                {
                    spriteBatch.End();
                }
                catch
                {
                    // Preserve the original present failure.
                }
            }
            EndBackdropFrame();
        }
    }

    public void RenderPng(
        Stream output,
        CernealaColor clearColor,
        Action<IDrawingBackend> draw) =>
        RenderPng(
            output,
            clearColor,
            retainedCacheEnabled: true,
            draw);

    internal void RenderPng(
        Stream output,
        CernealaColor clearColor,
        bool retainedCacheEnabled,
        Action<IDrawingBackend> draw)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        ArgumentNullException.ThrowIfNull(output);
        ArgumentNullException.ThrowIfNull(draw);
        if (!output.CanWrite)
        {
            throw new ArgumentException("The screenshot stream must be writable.", nameof(output));
        }

        if (activeFrameKind != FrameKind.None)
        {
            throw new InvalidOperationException("A screenshot cannot be rendered while an on-screen frame is active.");
        }
        EnsureNoActiveBackdropLeases("render a screenshot");

        int width = presentationParameters.BackBufferWidth;
        int height = presentationParameters.BackBufferHeight;
        MonoGameGraphicsDeviceStateSnapshot stateSnapshot = new();
        stateSnapshot.Capture(graphicsDevice);
        using RenderTarget2D target = new(
            graphicsDevice,
            width,
            height,
            false,
            SurfaceFormat.Color,
            DepthFormat.None,
            0,
            RenderTargetUsage.PreserveContents);
        using SpriteBatch captureSpriteBatch = new(graphicsDevice);
        using MonoGameDrawingBackend captureBackend = new(
            captureSpriteBatch,
            whitePixel,
            new SkiaTextRasterizer(),
            new PrismRendererOptions(),
            retainedCacheEnabled)
        {
            CoordinateScale = coordinateScale
        };

        try
        {
            BeginBackdropFrame(
                FrameKind.Capture,
                target,
                clearColor);
            draw(captureBackend);
            EnsureNoActiveBackdropLeases(
                "complete a screenshot");
            EndBackdropFrame();
            stateSnapshot.Restore(graphicsDevice);
            target.SaveAsPng(output, width, height);
        }
        finally
        {
            EndBackdropFrame();
            stateSnapshot.Restore(graphicsDevice);
        }
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        Exception? failure = null;
        graphicsDevice.DeviceResetting -= OnDeviceResetting;
        graphicsDevice.DeviceReset -= OnDeviceReset;
        EndBackdropFrame();
        if (frameTarget is not null)
        {
            DisposeResource(frameTarget, ref failure);
            frameTarget = null;
        }
        DisposeResource(imageResourceCache, ref failure);
        DisposeResource(drawingBackend, ref failure);
        DisposeResource(whitePixel, ref failure);
        DisposeResource(spriteBatch, ref failure);
        DisposeResource(graphicsDevice, ref failure);
        disposed = true;
        if (failure is not null)
        {
            throw CreateGraphicsException(
                "dispose",
                windowHandle,
                presentationParameters.BackBufferWidth,
                presentationParameters.BackBufferHeight,
                failure);
        }
    }

    private static PresentationParameters CreatePresentationParameters(nint handle, int width, int height)
    {
        return new PresentationParameters
        {
            DeviceWindowHandle = handle,
            BackBufferWidth = width,
            BackBufferHeight = height,
            BackBufferFormat = SurfaceFormat.Color,
            DepthStencilFormat = DepthFormat.None,
            MultiSampleCount = 8,
            IsFullScreen = false,
            PresentationInterval = PresentInterval.One,
            RenderTargetUsage = RenderTargetUsage.PreserveContents
        };
    }

    private void BeginBackdropFrame(
        FrameKind frameKind,
        RenderTarget2D target,
        CernealaColor clearColor)
    {
        if (activeFrameKind != FrameKind.None)
        {
            throw new InvalidOperationException(
                "The WindowsDX graphics session already has an active frame.");
        }
        EnsureNoActiveBackdropLeases("begin another frame");

        contentVersion = checked(contentVersion + 1);
        if (!MonoGameBackdropFrameValidation.TryMapPixelFormat(
            target.Format,
            out BackdropPixelFormat pixelFormat))
        {
            throw new InvalidOperationException(
                $"WindowsDX cannot expose surface format '{target.Format}' as a backdrop.");
        }

        activeBackdropMetadata = new BackdropFrameMetadata(
            target.Width,
            target.Height,
            coordinateScale,
            PrismColorProfile.Srgb,
            pixelFormat,
            BackdropAlphaMode.Premultiplied,
            Matrix3x2.CreateScale(coordinateScale),
            contentVersion);
        activeBackdropTarget = target;
        activeFrameKind = frameKind;
        graphicsDevice.SetRenderTarget(target);
        graphicsDevice.Clear(
            new XnaColor(
                clearColor.R,
                clearColor.G,
                clearColor.B,
                clearColor.A));
    }

    private void EndBackdropFrame()
    {
        activeBackdropTarget = null;
        activeBackdropMetadata = default;
        activeFrameKind = FrameKind.None;
    }

    private void EnsureNoActiveBackdropLeases(
        string operation)
    {
        if (activeBackdropLeaseCount != 0)
        {
            throw new InvalidOperationException(
                $"The WindowsDX graphics session cannot {operation} while " +
                $"{activeBackdropLeaseCount} backdrop lease(s) are still active.");
        }
    }

    private void ValidateLease(
        RenderTarget2D target,
        long version)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        if (activeFrameKind == FrameKind.None ||
            !ReferenceEquals(activeBackdropTarget, target) ||
            activeBackdropMetadata.ContentVersion != version)
        {
            throw new InvalidOperationException(
                "The backdrop lease is no longer valid for the active WindowsDX frame.");
        }
    }

    private void ReleaseBackdropLease()
    {
        if (activeBackdropLeaseCount <= 0)
        {
            throw new InvalidOperationException(
                "The WindowsDX backdrop lease count is already zero.");
        }

        activeBackdropLeaseCount--;
    }

    private RenderTarget2D RequireFrameTarget()
    {
        return frameTarget ??
            throw new InvalidOperationException(
                "The WindowsDX frame target is unavailable after a device reset.");
    }

    private void OnDeviceResetting(
        object? sender,
        EventArgs eventArgs)
    {
        EndBackdropFrame();
        frameTarget?.Dispose();
        frameTarget = null;
    }

    private void OnDeviceReset(
        object? sender,
        EventArgs eventArgs)
    {
        if (!disposed)
        {
            frameTarget = CreateFrameTarget(
                graphicsDevice,
                presentationParameters);
        }
    }

    private static RenderTarget2D CreateFrameTarget(
        GraphicsDevice graphicsDevice,
        PresentationParameters parameters)
    {
        try
        {
            return new RenderTarget2D(
                graphicsDevice,
                parameters.BackBufferWidth,
                parameters.BackBufferHeight,
                false,
                parameters.BackBufferFormat,
                DepthFormat.None,
                parameters.MultiSampleCount,
                RenderTargetUsage.PreserveContents);
        }
        catch when (parameters.MultiSampleCount > 0)
        {
            return new RenderTarget2D(
                graphicsDevice,
                parameters.BackBufferWidth,
                parameters.BackBufferHeight,
                false,
                parameters.BackBufferFormat,
                DepthFormat.None,
                0,
                RenderTargetUsage.PreserveContents);
        }
    }

    private static GraphicsDevice CreateGraphicsDevice(PresentationParameters parameters)
    {
        foreach (int sampleCount in new[] { 8, 4, 2, 0 })
        {
            parameters.MultiSampleCount = sampleCount;
            try
            {
                return new GraphicsDevice(
                    GraphicsAdapter.DefaultAdapter,
                    GraphicsProfile.HiDef,
                    parameters);
            }
            catch when (sampleCount > 0)
            {
                // Fall back to the next supported MSAA level.
            }
        }

        throw new InvalidOperationException("Could not create a MonoGame graphics device.");
    }

    private static InvalidOperationException CreateGraphicsException(
        string operation,
        nint handle,
        int width,
        int height,
        Exception innerException)
    {
        string adapter;
        try
        {
            adapter = GraphicsAdapter.DefaultAdapter.Description;
        }
        catch
        {
            adapter = "unavailable";
        }

        return new InvalidOperationException(
            $"Could not {operation} the WindowsDX graphics session for HWND 0x{handle.ToInt64():X}. " +
            $"Adapter='{adapter}', profile={GraphicsProfile.HiDef}, backbuffer={width}x{height}.",
            innerException);
    }

    private static void ValidateSize(int pixelWidth, int pixelHeight)
    {
        if (pixelWidth <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(pixelWidth), "Backbuffer width must be greater than zero.");
        }

        if (pixelHeight <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(pixelHeight), "Backbuffer height must be greater than zero.");
        }
    }

    private static void DisposeResource(IDisposable resource, ref Exception? failure)
    {
        try
        {
            resource.Dispose();
        }
        catch (Exception exception)
        {
            failure ??= exception;
        }
    }

    private enum FrameKind
    {
        None,
        OnScreen,
        Capture
    }

    private sealed class BackdropFrameLease :
        IMonoGameBackdropFrameLease
    {
        private WindowsDxWindowGraphicsSession? owner;
        private readonly RenderTarget2D texture;
        private readonly BackdropFrameMetadata metadata;

        public BackdropFrameLease(
            WindowsDxWindowGraphicsSession owner,
            RenderTarget2D texture,
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
                RequireOwner().ValidateLease(
                    texture,
                    metadata.ContentVersion);
                return metadata;
            }
        }

        public Texture2D Texture
        {
            get
            {
                RequireOwner().ValidateLease(
                    texture,
                    metadata.ContentVersion);
                return texture;
            }
        }

        public void Dispose()
        {
            WindowsDxWindowGraphicsSession? currentOwner =
                Interlocked.Exchange(ref owner, null);
            currentOwner?.ReleaseBackdropLease();
        }

        private WindowsDxWindowGraphicsSession RequireOwner()
        {
            return owner ??
                throw new ObjectDisposedException(
                    nameof(BackdropFrameLease));
        }
    }
}
