using Cerneala.Drawing;
using Cerneala.Drawing.Prism;
using Cerneala.UI.Controls;
using Cerneala.UI.Hosting;
using Cerneala.UI.Input;
using Cerneala.UI.Platform;
using Cerneala.UI.Resources;

namespace Cerneala.UI.Hosting.Windowing;

internal interface IWindowPlatform : IDisposable
{
    IPlatformServices? PlatformServices => null;

    IPlatformWindow CreateWindow(Window window, IWindowPlatformCallbacks callbacks);

    void PumpEvents();
}

internal interface IPlatformWindow : IDisposable
{
    IWindowSurface Surface { get; }

    UiViewport Viewport { get; }

    IInputSource InputSource { get; }

    IWindowGraphicsSession GraphicsSession { get; }

    void ApplyProperties(Window window);

    void SetOwner(IPlatformWindow? owner);

    void SetEnabled(bool enabled);

    void Show();

    void Hide();

    void Activate();

    void Destroy();
}

internal interface IWindowSurface
{
}

internal interface IWindowGraphicsSession : IDisposable
{
    IDrawingBackend DrawingBackend { get; }

    PrismExecutionDiagnostics? PrismExecutionDiagnostics => null;

    int ActiveBackdropLeaseCount => 0;

    IImageLoader? ImageLoader { get; }

    ImageResourceCache? ImageResourceCache { get; }

    void Resize(int pixelWidth, int pixelHeight, float coordinateScale);

    void BeginFrame(Color clearColor);

    void Present();

    void CompleteFrame(bool present)
    {
        if (present)
        {
            Present();
        }
    }
}

internal interface IWindowScreenshotSource
{
    void RenderPng(Stream output, Color clearColor, Action<IDrawingBackend> draw);
}

internal readonly record struct WindowPreviewFrame(
    byte[] Pixels,
    int PixelWidth,
    int PixelHeight,
    int Stride);

internal interface IWindowPresentedFrameSource
{
    WindowPreviewFrame CapturePresentedFrame(byte[]? reusablePixels = null);
}

internal interface IWindowGraphicsSessionFactory
{
    IWindowGraphicsSession Create(
        IWindowSurface windowSurface,
        int pixelWidth,
        int pixelHeight,
        float coordinateScale);
}

internal interface IWindowPlatformCallbacks
{
    void RequestClose();

    void ActivationChanged(bool active);

    void BoundsChanged(UiViewport viewport, float left, float top, WindowState state);

    void RenderRequested();

    void RenderImmediately()
    {
        RenderRequested();
    }
}
