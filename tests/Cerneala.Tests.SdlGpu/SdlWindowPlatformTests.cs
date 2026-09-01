using Cerneala.Platforms.Sdl3;
using Cerneala.UI.Controls;
using Cerneala.UI.Input;
using Cerneala.UI.Platform;

namespace Cerneala.Tests.SdlGpu;

public sealed class SdlWindowPlatformTests
{
    [Fact]
    public void DisplayScaleControlsLogicalWindowSizeRenderingAndPointerCoordinates()
    {
        FakeSdlApi api = new()
        {
            WindowPixelDensity = 1,
            WindowDisplayScale = 1.25f
        };
        RecordingGraphicsFactory graphics = new();
        using SdlWindowPlatform platform = new(api, graphics);
        Window model = new()
        {
            Width = 640,
            Height = 480,
            MinWidth = 200,
            MinHeight = 100
        };

        SdlPlatformWindow window = Assert.IsType<SdlPlatformWindow>(
            platform.CreateWindow(model, new RecordingWindowCallbacks()));
        api.Enqueue(new SdlEvent(
            SdlEventKind.MouseMotion,
            window.WindowId,
            X: 400,
            Y: 300));
        platform.PumpEvents();

        Assert.Equal(800, api.Windows[window.Handle].Width);
        Assert.Equal(600, api.Windows[window.Handle].Height);
        Assert.Equal(250, api.Windows[window.Handle].MinimumWidth);
        Assert.Equal(125, api.Windows[window.Handle].MinimumHeight);
        Assert.Equal(800, graphics.Sessions[0].PixelWidth);
        Assert.Equal(600, graphics.Sessions[0].PixelHeight);
        Assert.Equal(1.25f, graphics.Sessions[0].CoordinateScale);
        Assert.Equal(640, window.Viewport.Width);
        Assert.Equal(480, window.Viewport.Height);
        Assert.Equal(320, window.InputSource.GetFrame().Pointer.X);
        Assert.Equal(240, window.InputSource.GetFrame().Pointer.Y);
    }

    [Fact]
    public void TwoWindowsHaveIndependentPropertiesOwnershipAndLifetime()
    {
        FakeSdlApi api = new();
        RecordingGraphicsFactory graphics = new();
        using SdlWindowPlatform platform = new(api, graphics);
        Window ownerModel = new()
        {
            Title = "Owner",
            Width = 640,
            Height = 480,
            MinWidth = 200,
            MinHeight = 100,
            MaxWidth = 900,
            MaxHeight = 700,
            Topmost = true,
            ResizeMode = ResizeMode.CanResize,
            WindowStartupLocation = WindowStartupLocation.CenterScreen
        };
        Window childModel = new()
        {
            Title = "Child",
            Width = 300,
            Height = 200,
            ShowInTaskbar = false,
            ResizeMode = ResizeMode.NoResize,
            WindowStartupLocation = WindowStartupLocation.CenterOwner
        };

        SdlPlatformWindow owner = Assert.IsType<SdlPlatformWindow>(
            platform.CreateWindow(ownerModel, new RecordingWindowCallbacks()));
        SdlPlatformWindow child = Assert.IsType<SdlPlatformWindow>(
            platform.CreateWindow(childModel, new RecordingWindowCallbacks()));
        child.SetOwner(owner);
        child.SetEnabled(false);
        owner.Show();
        child.Show();
        child.Hide();
        owner.Activate();

        Assert.NotEqual(owner.WindowId, child.WindowId);
        Assert.Equal(owner.Handle, api.Windows[child.Handle].Parent);
        Assert.True(api.Windows[owner.Handle].AlwaysOnTop);
        Assert.True(api.Windows[owner.Handle].Resizable);
        Assert.False(api.Windows[child.Handle].Resizable);
        Assert.True(api.Windows[child.Handle].Options.HasFlag(SdlWindowOptions.Utility));
        Assert.Equal(2, graphics.Sessions.Count);
        Assert.Equal(2, graphics.Sessions[0].CoordinateScale);

        child.Destroy();
        Assert.Contains(child.Handle, api.DestroyedWindows);
        Assert.DoesNotContain(owner.Handle, api.DestroyedWindows);
        owner.Destroy();
        owner.Destroy();
        Assert.Equal(2, api.DestroyedWindows.Count);
    }

    [Fact]
    public void EventPumpRoutesInterleavedInputFocusDpiBoundsAndCloseByWindowId()
    {
        FakeSdlApi api = new();
        RecordingGraphicsFactory graphics = new();
        using SdlWindowPlatform platform = new(api, graphics);
        RecordingWindowCallbacks firstCallbacks = new();
        RecordingWindowCallbacks secondCallbacks = new();
        SdlPlatformWindow first = Assert.IsType<SdlPlatformWindow>(
            platform.CreateWindow(new Window { Title = "First" }, firstCallbacks));
        SdlPlatformWindow second = Assert.IsType<SdlPlatformWindow>(
            platform.CreateWindow(new Window { Title = "Second" }, secondCallbacks));
        api.WindowPixelDensity = 1.5f;
        api.WindowDisplayScale = 1.5f;
        api.Enqueue(
            new SdlEvent(SdlEventKind.MouseMotion, first.WindowId, X: 12, Y: 34),
            new SdlEvent(SdlEventKind.KeyDown, first.WindowId, Scancode: 4),
            new SdlEvent(SdlEventKind.TextInput, first.WindowId, Text: "ă"),
            new SdlEvent(SdlEventKind.WindowFocusGained, first.WindowId),
            new SdlEvent(SdlEventKind.WindowFocusGained, first.WindowId),
            new SdlEvent(SdlEventKind.MouseMotion, second.WindowId, X: 56, Y: 78),
            new SdlEvent(SdlEventKind.MouseButtonDown, second.WindowId, X: 56, Y: 78, MouseButton: 1),
            new SdlEvent(SdlEventKind.MouseWheel, second.WindowId, Data2: 2, X: 56, Y: 78),
            new SdlEvent(SdlEventKind.WindowMoved, second.WindowId),
            new SdlEvent(SdlEventKind.WindowDisplayScaleChanged, second.WindowId),
            new SdlEvent(SdlEventKind.WindowCloseRequested, second.WindowId));

        platform.PumpEvents();

        InputFrame firstFrame = first.InputSource.GetFrame();
        InputFrame secondFrame = second.InputSource.GetFrame();
        Assert.Equal(12, firstFrame.Pointer.X);
        Assert.Equal(34, firstFrame.Pointer.Y);
        Assert.True(firstFrame.Keyboard.IsPressed(InputKey.A));
        Assert.Equal("ă", Assert.Single(firstFrame.TextInputEvents).Text);
        Assert.Equal([true], firstCallbacks.Activations);
        Assert.Equal(56, secondFrame.Pointer.X);
        Assert.True(secondFrame.Pointer.IsPressed(InputMouseButton.Left));
        Assert.Equal(240, secondFrame.Pointer.WheelDelta);
        Assert.NotEmpty(secondCallbacks.Bounds);
        Assert.Equal(1, secondCallbacks.CloseRequests);
        Assert.Equal(1, graphics.Sessions[1].ResizeCount);
        Assert.Equal(1.5f, graphics.Sessions[1].CoordinateScale);
        Assert.Equal(0, firstCallbacks.CloseRequests);
    }

    [Fact]
    public void DisabledAndDestroyedWindowsDoNotReceiveInputOrLateEvents()
    {
        FakeSdlApi api = new();
        using SdlWindowPlatform platform = new(api, new RecordingGraphicsFactory());
        RecordingWindowCallbacks callbacks = new();
        SdlPlatformWindow window = Assert.IsType<SdlPlatformWindow>(
            platform.CreateWindow(new Window(), callbacks));
        window.SetEnabled(false);
        api.Enqueue(
            new SdlEvent(SdlEventKind.KeyDown, window.WindowId, Scancode: 4),
            new SdlEvent(SdlEventKind.MouseButtonDown, window.WindowId, MouseButton: 1));
        platform.PumpEvents();

        InputFrame frame = window.InputSource.GetFrame();
        Assert.False(frame.Keyboard.IsDown(InputKey.A));
        Assert.False(frame.Pointer.IsDown(InputMouseButton.Left));
        window.Destroy();
        api.Enqueue(
            new SdlEvent(SdlEventKind.WindowCloseRequested, window.WindowId),
            new SdlEvent(SdlEventKind.WindowFocusGained, window.WindowId));
        platform.PumpEvents();
        Assert.Equal(0, callbacks.CloseRequests);
        Assert.Empty(callbacks.Activations);
    }

    [Fact]
    public void LiveResizeExposeRefreshesGeometryAndRendersImmediatelyFromEventWatch()
    {
        FakeSdlApi api = new()
        {
            WindowPixelDensity = 1,
            WindowDisplayScale = 1
        };
        RecordingGraphicsFactory graphics = new();
        RecordingWindowCallbacks callbacks = new();
        SdlWindowPlatform platform = new(api, graphics);
        SdlPlatformWindow window = Assert.IsType<SdlPlatformWindow>(
            platform.CreateWindow(new Window { Width = 640, Height = 480 }, callbacks));
        api.Windows[window.Handle].Width = 900;
        api.Windows[window.Handle].Height = 700;

        Assert.Equal(1, api.RegisteredEventWatchCount);
        api.RaiseWatchedEvent(
            new SdlEvent(SdlEventKind.WindowExposed, window.WindowId, Data1: 1));

        Assert.Equal(1, callbacks.ImmediateRenderRequests);
        Assert.Equal(900, window.Viewport.Width);
        Assert.Equal(700, window.Viewport.Height);
        Assert.Equal(900, graphics.Sessions[0].PixelWidth);
        Assert.Equal(700, graphics.Sessions[0].PixelHeight);

        api.Enqueue(new SdlEvent(SdlEventKind.WindowExposed, window.WindowId, Data1: 1));
        platform.PumpEvents();
        Assert.Equal(1, callbacks.ImmediateRenderRequests);

        platform.Dispose();
        Assert.Equal(0, api.RegisteredEventWatchCount);
        Assert.Equal(1, api.RemoveEventWatchCount);
    }

    [Fact]
    public void LiveResizeEventWatchDoesNotRenderFromANonOwnerThread()
    {
        FakeSdlApi api = new();
        RecordingWindowCallbacks callbacks = new();
        using SdlWindowPlatform platform = new(api, new RecordingGraphicsFactory());
        SdlPlatformWindow window = Assert.IsType<SdlPlatformWindow>(
            platform.CreateWindow(new Window(), callbacks));

        Exception? workerFailure = null;
        Thread worker = new(() =>
        {
            try
            {
                api.RaiseWatchedEvent(
                    new SdlEvent(SdlEventKind.WindowExposed, window.WindowId, Data1: 1));
            }
            catch (Exception exception)
            {
                workerFailure = exception;
            }
        });
        worker.Start();
        Assert.True(worker.Join(TimeSpan.FromSeconds(5)));
        Assert.Null(workerFailure);

        Assert.Equal(0, callbacks.ImmediateRenderRequests);
        api.Enqueue(new SdlEvent(SdlEventKind.WindowExposed, window.WindowId, Data1: 1));
        platform.PumpEvents();
        Assert.Equal(1, callbacks.RenderRequests);
    }

    [Fact]
    public void LiveResizeEventWatchFailureIsRethrownByTheEventPump()
    {
        FakeSdlApi api = new();
        using SdlWindowPlatform platform = new(api, new RecordingGraphicsFactory());
        SdlPlatformWindow window = Assert.IsType<SdlPlatformWindow>(
            platform.CreateWindow(new Window(), new RecordingWindowCallbacks()));
        api.GetWindowSizeInPixelsResult = false;

        Exception? callbackFailure = Record.Exception(() => api.RaiseWatchedEvent(
            new SdlEvent(SdlEventKind.WindowExposed, window.WindowId, Data1: 1)));

        Assert.Null(callbackFailure);
        Assert.Throws<InvalidOperationException>(() => platform.PumpEvents());
    }

    [Fact]
    public void EventWatchRegistrationFailureReleasesTheSdlLifetime()
    {
        FakeSdlApi api = new() { AddEventWatchResult = false };

        Assert.Throws<InvalidOperationException>(() =>
            new SdlWindowPlatform(api, new RecordingGraphicsFactory()));

        Assert.Equal(1, api.AddEventWatchCount);
        Assert.Equal(0, api.RegisteredEventWatchCount);
        Assert.Equal(1, api.InitializeCount);
        Assert.Equal(1, api.QuitCount);
    }

    [Fact]
    public void CursorServiceCachesAndDisposesNativeCursors()
    {
        FakeSdlApi api = new();
        SdlWindowPlatform platform = new(api, new RecordingGraphicsFactory());
        ICursorService cursor = Assert.IsAssignableFrom<ICursorService>(platform.PlatformServices.Cursor);

        cursor.SetCursor(CursorShape.Hand);
        cursor.SetCursor(CursorShape.Hand);
        cursor.SetCursor(CursorShape.IBeam);
        platform.Dispose();

        Assert.Equal(2, api.DestroyedCursors.Count);
    }
}
