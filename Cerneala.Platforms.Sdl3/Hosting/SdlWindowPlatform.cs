using Cerneala.UI.Controls;
using Cerneala.UI.Hosting;
using Cerneala.UI.Hosting.Windowing;
using Cerneala.UI.Input;
using Cerneala.UI.Platform;
using System.Runtime.ExceptionServices;

namespace Cerneala.Platforms.Sdl3;

internal sealed class SdlWindowPlatformFactory
{
    private readonly IWindowGraphicsSessionFactory graphicsSessionFactory;

    public SdlWindowPlatformFactory(IWindowGraphicsSessionFactory graphicsSessionFactory)
    {
        this.graphicsSessionFactory = graphicsSessionFactory ??
            throw new ArgumentNullException(nameof(graphicsSessionFactory));
    }

    public SdlWindowPlatform Create(float? coordinateScaleOverride = null) =>
        new(new NativeSdlApi(), graphicsSessionFactory, coordinateScaleOverride);
}

internal sealed class SdlWindowPlatform : IWindowPlatform
{
    private readonly object sync = new();
    private readonly int ownerThreadId = Environment.CurrentManagedThreadId;
    private readonly ISdlApi api;
    private readonly IWindowGraphicsSessionFactory graphicsSessionFactory;
    private readonly float? coordinateScaleOverride;
    private readonly SdlPlatformLifetime lifetime;
    private readonly SdlCursorService cursorService;
    private readonly PlatformServices platformServices;
    private readonly Dictionary<uint, SdlPlatformWindow> windows = [];
    private readonly SdlEventWatch eventWatch;
    private ExceptionDispatchInfo? pendingEventWatchFailure;
    private bool disposed;

    public SdlWindowPlatform(
        ISdlApi api,
        IWindowGraphicsSessionFactory graphicsSessionFactory,
        float? coordinateScaleOverride = null)
    {
        this.api = api ?? throw new ArgumentNullException(nameof(api));
        this.graphicsSessionFactory = graphicsSessionFactory ??
            throw new ArgumentNullException(nameof(graphicsSessionFactory));
        if (coordinateScaleOverride is float scale)
        {
            UiCoordinateMapper.ValidateScale(scale);
        }

        this.coordinateScaleOverride = coordinateScaleOverride;
        lifetime = new SdlPlatformLifetime(api);
        cursorService = new SdlCursorService(api);
        platformServices = new PlatformServices(
            Cursor: cursorService,
            TextInput: new SdlTextInputPlatform());
        eventWatch = ProcessWatchedEvent;
        try
        {
            if (!api.AddEventWatch(eventWatch))
            {
                throw SdlApiError.Create(api, "SDL live-resize event watch registration");
            }
        }
        catch
        {
            try
            {
                cursorService.Dispose();
            }
            finally
            {
                lifetime.Dispose();
            }

            throw;
        }
    }

    public IPlatformServices PlatformServices => platformServices;

    public IPlatformWindow CreateWindow(Window window, IWindowPlatformCallbacks callbacks)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        lifetime.VerifyUiThread();
        ArgumentNullException.ThrowIfNull(window);
        ArgumentNullException.ThrowIfNull(callbacks);

        SdlPlatformWindow created = new(
            api,
            window,
            callbacks,
            graphicsSessionFactory,
            coordinateScaleOverride,
            RemoveWindow);
        lock (sync)
        {
            if (!windows.TryAdd(created.WindowId, created))
            {
                created.Dispose();
                throw new InvalidOperationException(
                    $"SDL window ID {created.WindowId} is already registered.");
            }
        }

        return created;
    }

    public void PumpEvents()
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        lifetime.VerifyUiThread();
        ThrowPendingEventWatchFailure();
        while (api.PollEvent(out SdlEvent @event))
        {
            ThrowPendingEventWatchFailure();
            if (@event.Kind == SdlEventKind.Quit)
            {
                foreach (SdlPlatformWindow window in SnapshotWindows())
                {
                    window.RequestApplicationClose();
                }

                continue;
            }

            SdlPlatformWindow? target;
            lock (sync)
            {
                windows.TryGetValue(@event.WindowId, out target);
            }

            target?.ProcessEvent(@event);
        }

        ThrowPendingEventWatchFailure();
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        lifetime.VerifyUiThread();
        disposed = true;
        api.RemoveEventWatch(eventWatch);
        foreach (SdlPlatformWindow window in SnapshotWindows())
        {
            window.Dispose();
        }

        if (graphicsSessionFactory is IDisposable disposableGraphicsFactory)
        {
            disposableGraphicsFactory.Dispose();
        }

        cursorService.Dispose();
        lifetime.Dispose();
    }

    private SdlPlatformWindow[] SnapshotWindows()
    {
        lock (sync)
        {
            return windows.Values.ToArray();
        }
    }

    private void RemoveWindow(uint windowId)
    {
        lock (sync)
        {
            windows.Remove(windowId);
        }
    }

    private void ProcessWatchedEvent(SdlEvent @event)
    {
        if (@event.Kind != SdlEventKind.WindowExposed || @event.Data1 != 1 ||
            Environment.CurrentManagedThreadId != ownerThreadId || disposed)
        {
            return;
        }

        try
        {
            SdlPlatformWindow? target;
            lock (sync)
            {
                windows.TryGetValue(@event.WindowId, out target);
            }

            target?.ProcessLiveResizeExpose();
        }
        catch (Exception exception)
        {
            lock (sync)
            {
                pendingEventWatchFailure ??= ExceptionDispatchInfo.Capture(exception);
            }
        }
    }

    private void ThrowPendingEventWatchFailure()
    {
        ExceptionDispatchInfo? failure;
        lock (sync)
        {
            failure = pendingEventWatchFailure;
            pendingEventWatchFailure = null;
        }

        failure?.Throw();
    }

    private sealed class SdlTextInputPlatform : ITextInputPlatform
    {
        public bool SupportsIme => true;
    }
}

internal sealed class SdlPlatformWindow : IPlatformWindow
{
    private readonly ISdlApi api;
    private readonly Window window;
    private readonly IWindowPlatformCallbacks callbacks;
    private readonly Action<uint> removeWindow;
    private readonly float? coordinateScaleOverride;
    private readonly SdlInputSource inputSource = new();
    private readonly IWindowGraphicsSession graphicsSession;
    private bool destroyed;
    private bool enabled = true;
    private bool? lastActive;
    private (UiViewport Viewport, float Left, float Top, WindowState State)? lastBounds;
    private WindowState desiredState;
    private int graphicsPixelWidth;
    private int graphicsPixelHeight;
    private float graphicsScale;
    private float nativeCoordinateScale = 1;
    private SdlPlatformWindow? owner;

    public SdlPlatformWindow(
        ISdlApi api,
        Window window,
        IWindowPlatformCallbacks callbacks,
        IWindowGraphicsSessionFactory graphicsSessionFactory,
        float? coordinateScaleOverride,
        Action<uint> removeWindow)
    {
        this.api = api;
        this.window = window;
        this.callbacks = callbacks;
        this.coordinateScaleOverride = coordinateScaleOverride;
        this.removeWindow = removeWindow;
        desiredState = window.WindowState;
        SdlWindowOptions options = SdlWindowOptions.Hidden | SdlWindowOptions.HighPixelDensity;
        if (window.ResizeMode is ResizeMode.CanResize or ResizeMode.CanResizeWithGrip)
        {
            options |= SdlWindowOptions.Resizable;
        }

        if (window.Topmost)
        {
            options |= SdlWindowOptions.AlwaysOnTop;
        }

        if (!window.ShowInTaskbar)
        {
            options |= SdlWindowOptions.Utility;
        }

        Handle = api.CreateWindow(
            window.Title,
            Math.Max(1, (int)MathF.Ceiling(window.Width)),
            Math.Max(1, (int)MathF.Ceiling(window.Height)),
            options);
        if (Handle == 0)
        {
            throw SdlApiError.Create(api, "SDL window creation");
        }

        WindowId = api.GetWindowId(Handle);
        if (WindowId == 0)
        {
            api.DestroyWindow(Handle);
            throw SdlApiError.Create(api, "SDL window ID lookup");
        }

        try
        {
            UpdateCoordinateScales();
            ApplyLogicalSize(window);
            Surface = new SdlWindowSurface(Handle, WindowId);
            (int pixelWidth, int pixelHeight, float scale) = ReadPixelGeometry();
            Viewport = UiViewport.FromPhysicalPixels(pixelWidth, pixelHeight, scale);
            graphicsSession = graphicsSessionFactory.Create(Surface, pixelWidth, pixelHeight, scale);
            graphicsPixelWidth = pixelWidth;
            graphicsPixelHeight = pixelHeight;
            graphicsScale = scale;
            ApplyProperties(window);
        }
        catch
        {
            api.DestroyWindow(Handle);
            throw;
        }
    }

    public nint Handle { get; }

    public uint WindowId { get; }

    public SdlWindowSurface Surface { get; }

    IWindowSurface IPlatformWindow.Surface => Surface;

    public UiViewport Viewport { get; private set; }

    public IInputSource InputSource => inputSource;

    public IWindowGraphicsSession GraphicsSession => graphicsSession;

    public void ApplyProperties(Window window)
    {
        if (destroyed)
        {
            return;
        }

        Require(api.SetWindowTitle(Handle, window.Title), "SDL window title update");
        ApplyLogicalSize(window);
        Require(api.SetWindowAlwaysOnTop(Handle, window.Topmost), "SDL topmost update");
        Require(api.SetWindowBordered(Handle, window.ResizeMode != ResizeMode.NoResize), "SDL border update");
        Require(api.SetWindowResizable(
            Handle,
            window.ResizeMode is ResizeMode.CanResize or ResizeMode.CanResizeWithGrip),
            "SDL resize mode update");
        ApplyPosition(window);
        ApplyState(window.WindowState);
    }

    public void SetOwner(IPlatformWindow? value)
    {
        owner = value switch
        {
            null => null,
            SdlPlatformWindow sdl => sdl,
            _ => throw new InvalidOperationException(
                $"An SDL window cannot be owned by a '{value.Surface.GetType().FullName}' surface.")
        };
        Require(api.SetWindowParent(Handle, owner?.Handle ?? 0), "SDL window owner update");
        if (window.WindowStartupLocation == WindowStartupLocation.CenterOwner && owner is not null)
        {
            CenterOverOwner(owner);
        }
    }

    public void SetEnabled(bool value) => enabled = value;

    public void Show() => Require(api.ShowWindow(Handle), "SDL window show");

    public void Hide() => Require(api.HideWindow(Handle), "SDL window hide");

    public void Activate() => Require(api.RaiseWindow(Handle), "SDL window activation");

    public void Destroy()
    {
        if (destroyed)
        {
            return;
        }

        destroyed = true;
        removeWindow(WindowId);
        Exception? failure = null;
        try
        {
            graphicsSession.Dispose();
        }
        catch (Exception exception)
        {
            failure = exception;
        }

        try
        {
            api.DestroyWindow(Handle);
        }
        catch (Exception exception)
        {
            failure ??= exception;
        }

        if (failure is not null)
        {
            throw failure;
        }
    }

    public void Dispose() => Destroy();

    public void RequestApplicationClose()
    {
        if (!destroyed)
        {
            callbacks.RequestClose();
        }
    }

    public void ProcessEvent(SdlEvent @event)
    {
        if (destroyed)
        {
            return;
        }

        switch (@event.Kind)
        {
            case SdlEventKind.WindowCloseRequested:
                callbacks.RequestClose();
                break;
            case SdlEventKind.WindowFocusGained:
                ReportActivation(true);
                break;
            case SdlEventKind.WindowFocusLost:
                ReportActivation(false);
                break;
            case SdlEventKind.WindowMoved:
            case SdlEventKind.WindowResized:
            case SdlEventKind.WindowPixelSizeChanged:
            case SdlEventKind.WindowDisplayChanged:
            case SdlEventKind.WindowDisplayScaleChanged:
                RefreshGeometry(resizeGraphics: true);
                break;
            case SdlEventKind.WindowMinimized:
                desiredState = WindowState.Minimized;
                RefreshGeometry(resizeGraphics: false);
                break;
            case SdlEventKind.WindowMaximized:
                desiredState = WindowState.Maximized;
                RefreshGeometry(resizeGraphics: true);
                break;
            case SdlEventKind.WindowRestored:
                desiredState = WindowState.Normal;
                RefreshGeometry(resizeGraphics: true);
                break;
            case SdlEventKind.WindowExposed:
                callbacks.RenderRequested();
                break;
            case SdlEventKind.WindowMouseLeave when enabled:
                inputSource.LeavePointer();
                callbacks.RenderRequested();
                break;
            case SdlEventKind.MouseMotion when enabled:
                inputSource.MovePointer(@event.X, @event.Y);
                callbacks.RenderRequested();
                break;
            case SdlEventKind.MouseButtonDown when enabled:
            case SdlEventKind.MouseButtonUp when enabled:
                inputSource.MovePointer(@event.X, @event.Y);
                inputSource.SetButton(@event.MouseButton, @event.Kind == SdlEventKind.MouseButtonDown);
                callbacks.RenderRequested();
                break;
            case SdlEventKind.MouseWheel when enabled:
                inputSource.MovePointer(@event.X, @event.Y);
                inputSource.AddWheel(@event.Data2, @event.WheelFlipped);
                callbacks.RenderRequested();
                break;
            case SdlEventKind.KeyDown when enabled:
            case SdlEventKind.KeyUp when enabled:
                inputSource.SetKey(@event.Scancode, @event.Kind == SdlEventKind.KeyDown);
                callbacks.RenderRequested();
                break;
            case SdlEventKind.TextInput when enabled:
                inputSource.AddText(@event.Text);
                callbacks.RenderRequested();
                break;
            case SdlEventKind.TextEditing when enabled:
                callbacks.RenderRequested();
                break;
        }
    }

    internal void ProcessLiveResizeExpose()
    {
        if (destroyed)
        {
            return;
        }

        RefreshGeometry(resizeGraphics: true);
        callbacks.RenderImmediately();
    }

    private void ApplyPosition(Window value)
    {
        if (value.WindowStartupLocation == WindowStartupLocation.CenterOwner && owner is not null)
        {
            CenterOverOwner(owner);
            return;
        }

        if (value.WindowStartupLocation == WindowStartupLocation.CenterScreen)
        {
            uint display = api.GetPrimaryDisplay();
            if (display != 0 &&
                api.GetDisplayUsableBounds(display, out SdlRect bounds) &&
                api.GetWindowSize(Handle, out int width, out int height))
            {
                int x = bounds.X + Math.Max(0, (bounds.Width - width) / 2);
                int y = bounds.Y + Math.Max(0, (bounds.Height - height) / 2);
                Require(api.SetWindowPosition(Handle, x, y), "SDL centered window position update");
            }

            return;
        }

        if (float.IsFinite(value.Left) && float.IsFinite(value.Top))
        {
            Require(api.SetWindowPosition(
                Handle,
                (int)MathF.Round(value.Left * nativeCoordinateScale),
                (int)MathF.Round(value.Top * nativeCoordinateScale)),
                "SDL manual window position update");
        }
    }

    private void CenterOverOwner(SdlPlatformWindow parent)
    {
        if (!api.GetWindowPosition(parent.Handle, out int ownerX, out int ownerY) ||
            !api.GetWindowSize(parent.Handle, out int ownerWidth, out int ownerHeight) ||
            !api.GetWindowSize(Handle, out int width, out int height))
        {
            throw SdlApiError.Create(api, "SDL owner-relative window position lookup");
        }

        Require(api.SetWindowPosition(
            Handle,
            ownerX + (ownerWidth - width) / 2,
            ownerY + (ownerHeight - height) / 2), "SDL owner-relative window position update");
    }

    private void ApplyState(WindowState state)
    {
        desiredState = state;
        bool result = state switch
        {
            WindowState.Minimized => api.MinimizeWindow(Handle),
            WindowState.Maximized => api.MaximizeWindow(Handle),
            _ => api.RestoreWindow(Handle)
        };
        Require(result, "SDL window state update");
    }

    private void RefreshGeometry(bool resizeGraphics)
    {
        (int pixelWidth, int pixelHeight, float scale) = ReadPixelGeometry();
        Viewport = UiViewport.FromPhysicalPixels(pixelWidth, pixelHeight, scale);
        if (resizeGraphics && pixelWidth > 0 && pixelHeight > 0 &&
            (pixelWidth != graphicsPixelWidth || pixelHeight != graphicsPixelHeight || scale != graphicsScale))
        {
            graphicsSession.Resize(pixelWidth, pixelHeight, scale);
            graphicsPixelWidth = pixelWidth;
            graphicsPixelHeight = pixelHeight;
            graphicsScale = scale;
        }

        if (!api.GetWindowPosition(Handle, out int x, out int y))
        {
            throw SdlApiError.Create(api, "SDL window position lookup");
        }

        float logicalX = x / nativeCoordinateScale;
        float logicalY = y / nativeCoordinateScale;
        var current = (Viewport, logicalX, logicalY, desiredState);
        if (lastBounds != current)
        {
            lastBounds = current;
            callbacks.BoundsChanged(
                Viewport,
                logicalX,
                logicalY,
                desiredState);
        }

        callbacks.RenderRequested();
    }

    private (int PixelWidth, int PixelHeight, float Scale) ReadPixelGeometry()
    {
        if (!api.GetWindowSizeInPixels(Handle, out int pixelWidth, out int pixelHeight))
        {
            throw SdlApiError.Create(api, "SDL window pixel size lookup");
        }

        float scale = UpdateCoordinateScales();

        return (Math.Max(1, pixelWidth), Math.Max(1, pixelHeight), scale);
    }

    private void ApplyLogicalSize(Window value)
    {
        Require(api.SetWindowSize(
            Handle,
            ToNativeDimension(value.Width, minimum: 1),
            ToNativeDimension(value.Height, minimum: 1)),
            "SDL window size update");
        Require(api.SetWindowMinimumSize(
            Handle,
            ToNativeDimension(value.MinWidth, minimum: 0),
            ToNativeDimension(value.MinHeight, minimum: 0)),
            "SDL window minimum size update");
        Require(api.SetWindowMaximumSize(
            Handle,
            float.IsFinite(value.MaxWidth)
                ? ToNativeDimension(value.MaxWidth, minimum: 1)
                : 0,
            float.IsFinite(value.MaxHeight)
                ? ToNativeDimension(value.MaxHeight, minimum: 1)
                : 0),
            "SDL window maximum size update");
    }

    private float UpdateCoordinateScales()
    {
        float pixelDensity = api.GetWindowPixelDensity(Handle);
        if (!float.IsFinite(pixelDensity) || pixelDensity <= 0)
        {
            pixelDensity = 1;
        }

        float displayScale = coordinateScaleOverride ??
            api.GetWindowDisplayScale(Handle);
        if (!float.IsFinite(displayScale) || displayScale <= 0)
        {
            displayScale = pixelDensity;
        }

        nativeCoordinateScale = coordinateScaleOverride.HasValue
            ? 1
            : displayScale / pixelDensity;
        if (!float.IsFinite(nativeCoordinateScale) ||
            nativeCoordinateScale <= 0)
        {
            nativeCoordinateScale = 1;
        }
        inputSource.CoordinateScale = nativeCoordinateScale;
        return displayScale;
    }

    private int ToNativeDimension(float logical, int minimum) =>
        Math.Max(
            minimum,
            (int)MathF.Ceiling(logical * nativeCoordinateScale));

    private void ReportActivation(bool active)
    {
        if (lastActive == active)
        {
            return;
        }

        lastActive = active;
        callbacks.ActivationChanged(active);
    }

    private void Require(bool result, string operation)
    {
        if (!result)
        {
            throw SdlApiError.Create(api, operation);
        }
    }
}
