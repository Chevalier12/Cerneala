using Cerneala.Drawing;
using Cerneala.Drawing.MonoGame.Prism.Execution;
using Cerneala.UI.Controls;
using Cerneala.UI.Hosting;
using Cerneala.UI.Input;
using Cerneala.UI.Platform;
using Cerneala.UI.Resources;

namespace Cerneala.UI.Hosting.Windows;

internal interface IWindowPlatform : IDisposable
{
    IPlatformServices? PlatformServices => null;

    IPlatformWindow CreateWindow(Window window, IWindowPlatformCallbacks callbacks);

    void PumpEvents();

    void WaitForPresentedFrames();
}

internal interface IPlatformWindow : IDisposable
{
    nint Handle { get; }

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

internal interface IWindowGraphicsSession : IDisposable
{
    IDrawingBackend DrawingBackend { get; }

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

internal interface IWindowPrismScreenshotDiagnosticsSource
{
    PrismExecutionDiagnostics? LastPrismScreenshotDiagnostics { get; }

    int ActiveBackdropLeaseCount { get; }
}

internal interface IWindowGraphicsSessionFactory
{
    IWindowGraphicsSession Create(nint windowHandle, int pixelWidth, int pixelHeight, float coordinateScale);
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
