using Cerneala.Drawing;
using Cerneala.UI.Controls;
using Cerneala.UI.Hosting;
using Cerneala.UI.Input;
using Cerneala.UI.Resources;

namespace Cerneala.UI.Hosting.Windows;

internal interface IWindowPlatform : IDisposable
{
    IImageLoader? ImageLoader { get; }

    ImageResourceCache? ImageResourceCache { get; }

    IPlatformWindow CreateWindow(Window window, IWindowPlatformCallbacks callbacks);

    void PumpEvents();
}

internal interface IPlatformWindow : IDisposable
{
    nint Handle { get; }

    UiViewport Viewport { get; }

    IInputSource InputSource { get; }

    IDrawingBackend DrawingBackend { get; }

    void ApplyProperties(Window window);

    void SetOwner(IPlatformWindow? owner);

    void SetEnabled(bool enabled);

    void Show();

    void Hide();

    void Activate();

    void Destroy();

    void Present();
}

internal interface IWindowPlatformCallbacks
{
    void RequestClose();

    void ActivationChanged(bool active);

    void BoundsChanged(UiViewport viewport, float left, float top, WindowState state);

    void RenderRequested();
}
