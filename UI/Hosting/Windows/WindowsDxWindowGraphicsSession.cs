using Cerneala.Drawing;
using Cerneala.Drawing.MonoGame;
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

internal sealed class WindowsDxWindowGraphicsSession : IWindowGraphicsSession, IWindowScreenshotSource
{
    private readonly nint windowHandle;
    private readonly GraphicsDevice graphicsDevice;
    private readonly SpriteBatch spriteBatch;
    private readonly Texture2D whitePixel;
    private readonly MonoGameDrawingBackend drawingBackend;
    private readonly ImageResourceCache imageResourceCache;
    private PresentationParameters presentationParameters;
    private float coordinateScale;
    private bool frameActive;
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

            graphicsDevice = createdDevice;
            spriteBatch = createdSpriteBatch;
            whitePixel = createdWhitePixel;
            drawingBackend = createdBackend;
            imageResourceCache = createdImageCache;
        }
        catch (Exception exception)
        {
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

    public void Resize(int pixelWidth, int pixelHeight, float coordinateScale)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        ValidateSize(pixelWidth, pixelHeight);
        UiCoordinateMapper.ValidateScale(coordinateScale);

        bool sizeChanged = presentationParameters.BackBufferWidth != pixelWidth ||
            presentationParameters.BackBufferHeight != pixelHeight;
        this.coordinateScale = coordinateScale;
        drawingBackend.CoordinateScale = coordinateScale;
        if (!sizeChanged)
        {
            return;
        }

        frameActive = false;

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
        graphicsDevice.Clear(new XnaColor(clearColor.R, clearColor.G, clearColor.B, clearColor.A));
        drawingBackend.CoordinateScale = coordinateScale;
        frameActive = true;
    }

    public void Present()
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        try
        {
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
        finally
        {
            frameActive = false;
        }
    }

    public void RenderPng(Stream output, CernealaColor clearColor, Action<IDrawingBackend> draw)
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
            throw new InvalidOperationException("A screenshot cannot be rendered while an on-screen frame is active.");
        }

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
        using MonoGameDrawingBackend captureBackend = new(captureSpriteBatch, whitePixel, new SkiaTextRasterizer())
        {
            CoordinateScale = coordinateScale
        };

        try
        {
            graphicsDevice.SetRenderTarget(target);
            graphicsDevice.Clear(new XnaColor(clearColor.R, clearColor.G, clearColor.B, clearColor.A));
            draw(captureBackend);
            stateSnapshot.Restore(graphicsDevice);
            target.SaveAsPng(output, width, height);
        }
        finally
        {
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
        frameActive = false;
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
}
