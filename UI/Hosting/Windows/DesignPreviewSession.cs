using Cerneala.UI.Automation;
using Cerneala.UI.Controls;
using Cerneala.UI.Elements;
using Cerneala.UI.Input;
using Microsoft.Extensions.DependencyInjection;

namespace Cerneala.UI.Hosting.Windows;

internal sealed class DesignPreviewSession : IDisposable
{
    private readonly WindowApplicationRuntime runtime;
    private readonly Window window;
    private bool disposed;

    private DesignPreviewSession(WindowApplicationRuntime runtime, Window window)
    {
        this.runtime = runtime;
        this.window = window;
    }

    public static DesignPreviewSession Create(
        Application application,
        Func<UIElement> createContent,
        int width,
        int height,
        float renderScale = 1)
    {
        ArgumentNullException.ThrowIfNull(application);
        ArgumentNullException.ThrowIfNull(createContent);
        ArgumentOutOfRangeException.ThrowIfLessThan(width, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(height, 1);
        UiCoordinateMapper.ValidateScale(renderScale);

        WindowApplicationRuntime runtime = new(new Win32WindowPlatform(
            new WindowsDxWindowGraphicsSessionFactory(useMultisampling: false),
            coordinateScaleOverride: renderScale));
        application.Install(runtime);
        ServiceCollection services = new();
        application.ConfigureAndPublishServices(services);

        UIElement content = createContent();
        Window window = content as Window ?? new Window
        {
            Content = content,
            Title = "Cerneala Live Preview",
            ShowInTaskbar = false,
            WindowStartupLocation = WindowStartupLocation.Manual,
            Left = -32_000,
            Top = -32_000
        };
        window.Width = width;
        window.Height = height;
        window.ShowInTaskbar = false;
        runtime.StartPreviewWindow(window);
        return new DesignPreviewSession(runtime, window);
    }

    public void Pump(TimeSpan elapsedTime)
    {
        ThrowIfDisposed();
        runtime.PumpOnce(elapsedTime);
    }

    public void SaveScreenshot(string path)
    {
        ThrowIfDisposed();
        window.SaveScreenshot(path);
    }

    public WindowPreviewFrame CaptureFrame(byte[]? reusablePixels = null)
    {
        ThrowIfDisposed();
        return runtime.CapturePreviewFrame(window, reusablePixels);
    }

    public void Click(float x, float y)
    {
        ThrowIfDisposed();
        runtime.ClickPreview(window, x, y);
    }

    public void MovePointer(float x, float y)
    {
        ThrowIfDisposed();
        runtime.MovePreviewPointer(window, x, y);
    }

    public void SetPointerButton(float x, float y, InputMouseButton button, bool isDown)
    {
        ThrowIfDisposed();
        runtime.SetPreviewPointerButton(window, x, y, button, isDown);
    }

    public void ScrollPointer(float x, float y, int wheelDelta)
    {
        ThrowIfDisposed();
        runtime.ScrollPreviewPointer(window, x, y, wheelDelta);
    }

    public void LeavePointer()
    {
        ThrowIfDisposed();
        runtime.LeavePreviewPointer(window);
    }

    public void SendText(string text)
    {
        ThrowIfDisposed();
        runtime.SendPreviewText(window, text);
    }

    public void PressKey(InputKey key, AutomationModifiers modifiers)
    {
        ThrowIfDisposed();
        runtime.PressPreviewKey(window, key, modifiers);
    }

    public void SetKeyState(InputKey key, bool isDown)
    {
        ThrowIfDisposed();
        runtime.SetPreviewKeyState(window, key, isDown);
    }

    public void ResetInput()
    {
        ThrowIfDisposed();
        runtime.ResetPreviewInput(window);
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        runtime.Dispose();
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(disposed, this);
    }
}
