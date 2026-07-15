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
    private readonly RasterizerState rasterizerState;
    private readonly MonoGameDrawingBackend drawingBackend;
    private readonly ImageResourceCache imageResourceCache;
    private PresentationParameters presentationParameters;
    private float coordinateScale;
    private bool frameBegun;
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
        RasterizerState? createdRasterizerState = null;
        MonoGameDrawingBackend? createdBackend = null;
        ImageResourceCache? createdImageCache = null;
        try
        {
            presentationParameters = CreatePresentationParameters(windowHandle, pixelWidth, pixelHeight);
            createdDevice = CreateGraphicsDevice(presentationParameters);
            createdSpriteBatch = new SpriteBatch(createdDevice);
            createdWhitePixel = new Texture2D(createdDevice, 1, 1);
            createdWhitePixel.SetData([XnaColor.White]);
            createdRasterizerState = MonoGameDrawingBackend.ScissorRasterizerState;
            createdBackend = new MonoGameDrawingBackend(createdSpriteBatch, createdWhitePixel, new SkiaTextRasterizer())
            {
                CoordinateScale = coordinateScale
            };
            ImageLoader = new MonoGameImageLoader(createdDevice);
            createdImageCache = new ImageResourceCache(ImageLoader);

            graphicsDevice = createdDevice;
            spriteBatch = createdSpriteBatch;
            whitePixel = createdWhitePixel;
            rasterizerState = createdRasterizerState;
            drawingBackend = createdBackend;
            imageResourceCache = createdImageCache;
        }
        catch (Exception exception)
        {
            createdImageCache?.Dispose();
            createdBackend?.Dispose();
            createdWhitePixel?.Dispose();
            createdRasterizerState?.Dispose();
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

        if (frameBegun)
        {
            try
            {
                spriteBatch.End();
            }
            finally
            {
                frameBegun = false;
            }
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
        if (frameBegun)
        {
            try
            {
                spriteBatch.End();
            }
            finally
            {
                frameBegun = false;
            }
        }

        graphicsDevice.Clear(new XnaColor(clearColor.R, clearColor.G, clearColor.B, clearColor.A));
        drawingBackend.CoordinateScale = coordinateScale;
        spriteBatch.Begin(
            sortMode: SpriteSortMode.Immediate,
            blendState: BlendState.AlphaBlend,
            samplerState: SamplerState.LinearClamp,
            depthStencilState: DepthStencilState.None,
            rasterizerState: rasterizerState);
        frameBegun = true;
    }

    public void Present()
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        try
        {
            if (frameBegun)
            {
                try
                {
                    spriteBatch.End();
                }
                finally
                {
                    frameBegun = false;
                }
            }

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

    public void RenderPng(Stream output, CernealaColor clearColor, Action<IDrawingBackend> draw)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        ArgumentNullException.ThrowIfNull(output);
        ArgumentNullException.ThrowIfNull(draw);
        if (!output.CanWrite)
        {
            throw new ArgumentException("The screenshot stream must be writable.", nameof(output));
        }

        if (frameBegun)
        {
            throw new InvalidOperationException("A screenshot cannot be rendered while an on-screen frame is active.");
        }

        int width = presentationParameters.BackBufferWidth;
        int height = presentationParameters.BackBufferHeight;
        RenderTargetBinding[] previousTargets = graphicsDevice.GetRenderTargets();
        using RenderTarget2D target = new(
            graphicsDevice,
            width,
            height,
            false,
            SurfaceFormat.Color,
            DepthFormat.None,
            0,
            RenderTargetUsage.DiscardContents);
        using SpriteBatch captureSpriteBatch = new(graphicsDevice);
        using MonoGameDrawingBackend captureBackend = new(captureSpriteBatch, whitePixel, new SkiaTextRasterizer())
        {
            CoordinateScale = coordinateScale
        };
        bool targetsRestored = false;

        try
        {
            graphicsDevice.SetRenderTarget(target);
            graphicsDevice.Clear(new XnaColor(clearColor.R, clearColor.G, clearColor.B, clearColor.A));
            captureSpriteBatch.Begin(
                sortMode: SpriteSortMode.Immediate,
                blendState: BlendState.AlphaBlend,
                samplerState: SamplerState.LinearClamp,
                depthStencilState: DepthStencilState.None,
                rasterizerState: rasterizerState);
            try
            {
                draw(captureBackend);
            }
            finally
            {
                captureSpriteBatch.End();
            }

            RestoreRenderTargets();
            targetsRestored = true;
            target.SaveAsPng(output, width, height);
        }
        finally
        {
            if (!targetsRestored)
            {
                RestoreRenderTargets();
            }
        }

        void RestoreRenderTargets()
        {
            if (previousTargets.Length == 0)
            {
                graphicsDevice.SetRenderTarget(null);
                return;
            }

            graphicsDevice.SetRenderTargets(previousTargets);
        }
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        Exception? failure = null;
        if (frameBegun)
        {
            try
            {
                spriteBatch.End();
            }
            catch (Exception exception)
            {
                failure = exception;
            }

            frameBegun = false;
        }

        DisposeResource(imageResourceCache, ref failure);
        DisposeResource(drawingBackend, ref failure);
        DisposeResource(rasterizerState, ref failure);
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
