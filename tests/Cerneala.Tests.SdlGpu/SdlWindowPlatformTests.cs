using Cerneala.Platforms.Sdl3;
using Cerneala.UI.Controls;
using Cerneala.UI.Input;
using Cerneala.UI.Platform;

namespace Cerneala.Tests.SdlGpu;

public sealed class SdlWindowPlatformTests
{
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
