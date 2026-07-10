using Cerneala.Drawing;
using Cerneala.UI.Controls;
using Cerneala.UI.Hosting;
using Cerneala.UI.Input;
using Cerneala.UI.Resources;

namespace Cerneala.UI.Hosting.Windows;

internal interface IWindowPlatform : IDisposable
{
    IPlatformWindow CreateWindow(Window window, IWindowPlatformCallbacks callbacks);

    void PumpEvents();
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

    void BeginFrame(DrawColor clearColor);

    void Present();
}

internal interface IWindowScreenshotSource
{
    void SavePng(Stream output);
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
}
