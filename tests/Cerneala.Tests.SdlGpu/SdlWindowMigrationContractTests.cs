using Cerneala.Platforms.Sdl3;
using Cerneala.Backends.SdlGpu;
using Cerneala.UI;
using Cerneala.UI.Controls;
using Cerneala.UI.Elements;
using Cerneala.UI.Hosting.Windowing;
using Cerneala.UI.Input;
using Cerneala.UI.Platform;

namespace Cerneala.Tests.SdlGpu;

public sealed class SdlWindowMigrationContractTests
{
    [Fact]
    public void GraphicsFactoryRejectsAForeignWindowSurfaceBeforeCreatingADevice()
    {
        FakeSdlApi api = new();
        using SdlGpuWindowGraphicsSessionFactory factory = new(api);

        ArgumentException exception = Assert.Throws<ArgumentException>(() =>
            factory.Create(new ForeignWindowSurface(), 32, 24, 1));

        Assert.Equal("windowSurface", exception.ParamName);
        Assert.Contains(nameof(SdlWindowSurface), exception.Message, StringComparison.Ordinal);
        Assert.Equal(0, api.CreateDeviceCount);
    }

    private sealed class ForeignWindowSurface : IWindowSurface { }

    [Theory]
    [InlineData(CursorShape.Default, (int)SdlSystemCursor.Default)]
    [InlineData(CursorShape.Arrow, (int)SdlSystemCursor.Default)]
    [InlineData(CursorShape.Hand, (int)SdlSystemCursor.Pointer)]
    [InlineData(CursorShape.IBeam, (int)SdlSystemCursor.Text)]
    [InlineData(CursorShape.Crosshair, (int)SdlSystemCursor.Crosshair)]
    [InlineData(CursorShape.ResizeHorizontal, (int)SdlSystemCursor.ResizeHorizontal)]
    [InlineData(CursorShape.ResizeVertical, (int)SdlSystemCursor.ResizeVertical)]
    public void CursorServiceMapsPlatformShapes(CursorShape shape, int nativeShape)
    {
        FakeSdlApi api = new();
        using SdlCursorService cursor = new(api);

        cursor.SetCursor(shape);

        Assert.Equal((SdlSystemCursor)nativeShape, api.CreatedCursors[api.SelectedCursor]);
        Assert.Equal(shape, cursor.Current);
    }

    [Fact]
    public void HiddenCursorPublishesZeroWithoutCreatingACursor()
    {
        FakeSdlApi api = new();
        using SdlCursorService cursor = new(api);

        cursor.SetCursor(CursorShape.Hidden);

        Assert.Empty(api.CreatedCursors);
        Assert.Equal(0, api.SelectedCursor);
        Assert.Equal(CursorShape.Hidden, cursor.Current);
    }

    [Fact]
    public void RuntimePublishesHoveredElementCursorThroughInputRouting()
    {
        FakeSdlApi api = new() { WindowPixelDensity = 1, WindowDisplayScale = 1 };
        RecordingGraphicsFactory graphics = new();
        using WindowApplicationRuntime runtime = new(new SdlWindowPlatform(api, graphics));
        Button button = new() { Content = "Hover", Cursor = Cursor.Crosshair };
        runtime.StartMainWindow(new Window { Content = button, Width = 320, Height = 200 });
        SdlWindowSurface surface = Assert.IsType<SdlWindowSurface>(Assert.Single(graphics.Surfaces));
        var bounds = button.ArrangedBounds;
        api.Enqueue(new SdlEvent(SdlEventKind.MouseMotion, surface.WindowId,
            X: bounds.X + bounds.Width / 2, Y: bounds.Y + bounds.Height / 2));

        runtime.PumpOnce(TimeSpan.FromMilliseconds(16));

        Assert.Equal(SdlSystemCursor.Crosshair, api.CreatedCursors[api.SelectedCursor]);
    }

    // #870: before any mouse event the pointer is not over the window, as after the pointer leaves it.
    [Fact]
    public void WindowTheMouseNeverEnteredHasNoHoveredElementAndNoElementCursor()
    {
        FakeSdlApi api = new() { WindowPixelDensity = 1, WindowDisplayScale = 1 };
        RecordingGraphicsFactory graphics = new();
        using WindowApplicationRuntime runtime = new(new SdlWindowPlatform(api, graphics));
        Button button = new() { Content = "Hover", Cursor = Cursor.Hand };
        int mouseEnters = 0;
        button.MouseEnter += (_, _) => mouseEnters++;
        runtime.StartMainWindow(new Window { Content = button, Width = 320, Height = 200 });

        runtime.PumpOnce(TimeSpan.FromMilliseconds(16));
        runtime.PumpOnce(TimeSpan.FromMilliseconds(16));

        Assert.True(
            !button.IsPointerOver,
            $"The button is hovered although no mouse event reached the window (button bounds {button.ArrangedBounds}).");
        Assert.Equal(0, mouseEnters);
        Assert.NotEqual(SdlSystemCursor.Pointer, SelectedCursor(api));
    }

    [Fact]
    public void MouseMotionInsideTheButtonHoversItAndSelectsTheHandCursor()
    {
        // Control for the never-entered test: a real MouseMotion inside the button.
        FakeSdlApi api = new() { WindowPixelDensity = 1, WindowDisplayScale = 1 };
        RecordingGraphicsFactory graphics = new();
        using WindowApplicationRuntime runtime = new(new SdlWindowPlatform(api, graphics));
        Button button = new() { Content = "Hover", Cursor = Cursor.Hand };
        runtime.StartMainWindow(new Window { Content = button, Width = 320, Height = 200 });
        SdlWindowSurface surface = Assert.IsType<SdlWindowSurface>(Assert.Single(graphics.Surfaces));
        var bounds = button.ArrangedBounds;
        api.Enqueue(new SdlEvent(SdlEventKind.MouseMotion, surface.WindowId,
            X: bounds.X + bounds.Width / 2, Y: bounds.Y + bounds.Height / 2));

        runtime.PumpOnce(TimeSpan.FromMilliseconds(16));

        Assert.True(button.IsPointerOver);
        Assert.Equal(SdlSystemCursor.Pointer, SelectedCursor(api));
    }

    private static SdlSystemCursor? SelectedCursor(FakeSdlApi api) =>
        api.CreatedCursors.TryGetValue(api.SelectedCursor, out SdlSystemCursor cursor) ? cursor : null;

    [Fact]
    public void SecondWindowRenderingDoesNotOverwriteTheCursorOfTheHoveredWindow()
    {
        TextBlock textB = new() { Text = "B" };
        (FakeSdlApi api, WindowApplicationRuntime runtime, _, uint windowB, RecordingGraphicsFactory graphics) =
            CreateTwoWindowsHoveringTheFirst(textB);
        using (runtime)
        {
            int presentsBefore = graphics.Sessions[1].PresentCount;
            textB.Text = "B changed";
            api.Enqueue(new SdlEvent(SdlEventKind.WindowExposed, windowB, Data1: 1));
            runtime.PumpOnce(TimeSpan.FromMilliseconds(16));
            Assert.True(graphics.Sessions[1].PresentCount > presentsBefore, "Window B did not present a frame.");

            Assert.True(
                SelectedCursor(api) == SdlSystemCursor.Pointer,
                $"After window B rendered, the selected cursor is {SelectedCursor(api)} instead of Pointer.");
        }
    }

    [Fact]
    public void HoveredWindowRenderingAgainKeepsItsHandCursor()
    {
        // Control: the hovered window itself renders again.
        (FakeSdlApi api, WindowApplicationRuntime runtime, uint windowA, _, _) =
            CreateTwoWindowsHoveringTheFirst(new TextBlock { Text = "B" });
        using (runtime)
        {
            api.Enqueue(new SdlEvent(SdlEventKind.WindowExposed, windowA, Data1: 1));
            runtime.PumpOnce(TimeSpan.FromMilliseconds(16));

            Assert.Equal(SdlSystemCursor.Pointer, SelectedCursor(api));
        }
    }

    [Fact]
    public void CursorFollowsThePointerIntoAnotherWindow()
    {
        Button buttonB = new() { Content = "B", Cursor = Cursor.Crosshair };
        (FakeSdlApi api, WindowApplicationRuntime runtime, uint windowA, uint windowB, _) =
            CreateTwoWindowsHoveringTheFirst(buttonB);
        using (runtime)
        {
            var bounds = buttonB.ArrangedBounds;
            // SDL moves the mouse focus: the old window gets the leave before the new window's motion.
            api.Enqueue(
                new SdlEvent(SdlEventKind.WindowMouseLeave, windowA),
                new SdlEvent(SdlEventKind.WindowMouseEnter, windowB),
                new SdlEvent(SdlEventKind.MouseMotion, windowB,
                    X: bounds.X + bounds.Width / 2, Y: bounds.Y + bounds.Height / 2));
            runtime.PumpOnce(TimeSpan.FromMilliseconds(16));
            Assert.Equal(SdlSystemCursor.Crosshair, SelectedCursor(api));

            // Window A renders after the pointer left it; the cursor stays the one of B's button.
            api.Enqueue(new SdlEvent(SdlEventKind.WindowExposed, windowA, Data1: 1));
            runtime.PumpOnce(TimeSpan.FromMilliseconds(16));
            Assert.Equal(SdlSystemCursor.Crosshair, SelectedCursor(api));
        }
    }

    // Window A shows a Hand button under the pointer; window B shows contentB and has no pointer.
    private static (FakeSdlApi Api, WindowApplicationRuntime Runtime, uint WindowA, uint WindowB,
        RecordingGraphicsFactory Graphics) CreateTwoWindowsHoveringTheFirst(UIElement contentB)
    {
        FakeSdlApi api = new() { WindowPixelDensity = 1, WindowDisplayScale = 1 };
        RecordingGraphicsFactory graphics = new();
        WindowApplicationRuntime runtime = new(new SdlWindowPlatform(api, graphics));
        Button buttonA = new() { Content = "A", Cursor = Cursor.Hand };
        runtime.Show(new Window { Content = buttonA, Width = 320, Height = 200 }, modal: false);
        runtime.Show(new Window { Content = contentB, Width = 320, Height = 200 }, modal: false);
        uint windowA = Assert.IsType<SdlWindowSurface>(graphics.Surfaces[0]).WindowId;
        uint windowB = Assert.IsType<SdlWindowSurface>(graphics.Surfaces[1]).WindowId;
        runtime.PumpOnce(TimeSpan.FromMilliseconds(16));
        var bounds = buttonA.ArrangedBounds;
        api.Enqueue(new SdlEvent(SdlEventKind.MouseMotion, windowA,
            X: bounds.X + bounds.Width / 2, Y: bounds.Y + bounds.Height / 2));
        runtime.PumpOnce(TimeSpan.FromMilliseconds(16));
        Assert.Equal(SdlSystemCursor.Pointer, SelectedCursor(api));
        Assert.NotEqual(windowA, windowB);
        return (api, runtime, windowA, windowB, graphics);
    }

    private static SdlSystemCursor? SelectedCursor(FakeSdlApi api) =>
        api.CreatedCursors.TryGetValue(api.SelectedCursor, out SdlSystemCursor cursor) ? cursor : null;

    [Fact]
    public void GraphicsFactoryReceivesSurfaceAndResizeEventsAreCoalesced()
    {
        FakeSdlApi api = new() { WindowPixelDensity = 1, WindowDisplayScale = 1 };
        RecordingGraphicsFactory graphics = new();
        using SdlWindowPlatform platform = new(api, graphics);
        Window model = new() { Width = 320, Height = 200 };
        using SdlPlatformWindow window = Assert.IsType<SdlPlatformWindow>(
            platform.CreateWindow(model, new RecordingWindowCallbacks()));
        RecordingGraphicsSession session = Assert.Single(graphics.Sessions);
        Assert.Same(window.Surface, Assert.Single(graphics.Surfaces));
        Assert.Equal(window.Viewport.Scale, session.CoordinateScale);

        model.Width = 420;
        model.Height = 260;
        window.ApplyProperties(model);
        api.Enqueue(new SdlEvent(SdlEventKind.WindowResized, window.WindowId),
            new SdlEvent(SdlEventKind.WindowPixelSizeChanged, window.WindowId));
        platform.PumpEvents();

        Assert.Equal(1, session.ResizeCount);
        Assert.Equal(420, session.PixelWidth);
        Assert.Equal(260, session.PixelHeight);
        window.Hide();
        Assert.Equal(0, session.DisposeCount);
        window.Dispose();
        Assert.Equal(1, session.DisposeCount);
    }

    [Fact]
    public void DuplicateMouseMoveDoesNotRequestAnotherFrame()
    {
        FakeSdlApi api = new();
        using SdlWindowPlatform platform = new(api, new RecordingGraphicsFactory());
        RecordingWindowCallbacks callbacks = new();
        SdlPlatformWindow window = Assert.IsType<SdlPlatformWindow>(
            platform.CreateWindow(new Window(), callbacks));
        api.Enqueue(new SdlEvent(SdlEventKind.MouseMotion, window.WindowId, X: 0, Y: 0),
            new SdlEvent(SdlEventKind.MouseMotion, window.WindowId, X: 0, Y: 0));

        platform.PumpEvents();

        Assert.Equal(1, callbacks.RenderRequests);
    }

    [Fact]
    public void PointerReentryAndCoordinateScaleChangeAreNotCoalesced()
    {
        SdlInputSource input = new();
        Assert.True(input.MovePointer(0, 0));
        Assert.False(input.MovePointer(0, 0));
        input.LeavePointer();
        Assert.True(input.MovePointer(0, 0));
        Assert.True(input.MovePointer(20, 10));
        Assert.False(input.MovePointer(20, 10));
        input.CoordinateScale = 2;
        Assert.True(input.MovePointer(20, 10));
        Assert.Equal(10, input.GetFrame().Pointer.X);
    }

    [Theory]
    [InlineData(WindowStartupLocation.CenterScreen)]
    [InlineData(WindowStartupLocation.CenterOwner)]
    public void ExplicitPositionOverridesAutomaticStartupPlacement(WindowStartupLocation startup)
    {
        FakeSdlApi api = new() { WindowPixelDensity = 1, WindowDisplayScale = 1 };
        using SdlWindowPlatform platform = new(api, new RecordingGraphicsFactory());
        Window model = new() { Left = 80, Top = 60, WindowStartupLocation = startup };
        using SdlPlatformWindow window = Assert.IsType<SdlPlatformWindow>(
            platform.CreateWindow(model, new RecordingWindowCallbacks()));

        if (startup == WindowStartupLocation.CenterOwner)
        {
            using var owner = platform.CreateWindow(new Window(), new RecordingWindowCallbacks());
            window.SetOwner(owner);
        }

        Assert.Equal(80, api.Windows[window.Handle].X);
        Assert.Equal(60, api.Windows[window.Handle].Y);
    }

    [Theory]
    [InlineData((int)SdlEventKind.WindowMaximized)]
    [InlineData((int)SdlEventKind.WindowMinimized)]
    public void NonNormalStateKeepsTheModelsRestorePosition(int eventKind)
    {
        FakeSdlApi api = new() { WindowPixelDensity = 1, WindowDisplayScale = 1 };
        using SdlWindowPlatform platform = new(api, new RecordingGraphicsFactory());
        Window model = new() { Left = 80, Top = 60, WindowStartupLocation = WindowStartupLocation.Manual };
        RecordingWindowCallbacks callbacks = new();
        using SdlPlatformWindow window = Assert.IsType<SdlPlatformWindow>(platform.CreateWindow(model, callbacks));
        api.Windows[window.Handle].X = 0;
        api.Windows[window.Handle].Y = 0;
        api.Enqueue(new SdlEvent((SdlEventKind)eventKind, window.WindowId));

        platform.PumpEvents();

        var bounds = Assert.Single(callbacks.Bounds);
        Assert.Equal(80, bounds.Left);
        Assert.Equal(60, bounds.Top);
    }

    [Theory]
    [InlineData(true, 100f, 50f)]
    [InlineData(false, 560f, 240f)]
    public void MaximizingKeepsTheRestorePositionWhetherOrNotLeftAndTopWereSet(
        bool explicitPosition, float restoreLeft, float restoreTop)
    {
        // Without Left/Top the default 800x600 window is centered on the fake 1920x1080 display: (560, 240).
        FakeSdlApi api = new() { WindowPixelDensity = 1, WindowDisplayScale = 1 };
        using SdlWindowPlatform platform = new(api, new RecordingGraphicsFactory());
        Window model = explicitPosition
            ? new Window { Left = restoreLeft, Top = restoreTop, WindowStartupLocation = WindowStartupLocation.Manual }
            : new Window { WindowStartupLocation = WindowStartupLocation.CenterScreen };
        RecordingWindowCallbacks callbacks = new();
        using SdlPlatformWindow window = Assert.IsType<SdlPlatformWindow>(platform.CreateWindow(model, callbacks));
        int boundsBeforeMaximize = callbacks.Bounds.Count;
        api.Windows[window.Handle].X = 0;
        api.Windows[window.Handle].Y = 0;
        api.Enqueue(new SdlEvent(SdlEventKind.WindowMaximized, window.WindowId));

        platform.PumpEvents();

        var bounds = callbacks.Bounds[^1];
        Assert.True(callbacks.Bounds.Count > boundsBeforeMaximize, "No bounds were reported for the maximize.");
        Assert.True(
            (float.IsNaN(bounds.Left) && float.IsNaN(bounds.Top)) ||
            (bounds.Left == restoreLeft && bounds.Top == restoreTop),
            $"After maximizing, the reported position is ({bounds.Left}, {bounds.Top}); the restore position is ({restoreLeft}, {restoreTop}).");
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void RestoringAMaximizedWindowDoesNotMoveItToTheMaximizedPosition(bool explicitPosition)
    {
        // The window sits at (800, 440): centered 320x200 on the fake 1920x1080 display, or explicit; true is the control.
        FakeSdlApi api = new() { WindowPixelDensity = 1, WindowDisplayScale = 1 };
        RecordingGraphicsFactory graphics = new();
        using WindowApplicationRuntime runtime = new(new SdlWindowPlatform(api, graphics));
        Window model = explicitPosition
            ? new Window { Left = 800, Top = 440, Width = 320, Height = 200, WindowStartupLocation = WindowStartupLocation.Manual }
            : new Window { Width = 320, Height = 200, WindowStartupLocation = WindowStartupLocation.CenterScreen };
        runtime.StartMainWindow(model);
        FakeSdlApi.FakeWindow native = Assert.Single(api.Windows.Values);
        (int X, int Y) before = (native.X, native.Y);
        Assert.Equal((800, 440), before);

        native.X = 0;
        native.Y = 0;
        api.Enqueue(new SdlEvent(SdlEventKind.WindowMaximized, native.Id));
        runtime.PumpOnce(TimeSpan.FromMilliseconds(16));
        (float Left, float Top) modelAfterMaximize = (model.Left, model.Top);
        model.WindowState = WindowState.Normal;
        runtime.PumpOnce(TimeSpan.FromMilliseconds(16));

        Assert.True(
            (native.X, native.Y) == before,
            $"explicitPosition={explicitPosition}: model Left/Top after the maximize are ({modelAfterMaximize.Left}, {modelAfterMaximize.Top}); " +
            $"after WindowState = Normal the native window is at ({native.X}, {native.Y}), it was at ({before.X}, {before.Y}).");
    }

    [Fact]
    public void ComingBackMaximizedFromMinimizedKeepsTheRestorePositionAndReportsNoNormalState()
    {
        // SDL3 on Windows queues WindowRestored, then WindowMaximized, when a minimized window comes back maximized
        // (SDL_windowsevents.c, WM_WINDOWPOSCHANGED); it marks the window maximized before either event is read.
        FakeSdlApi api = new() { WindowPixelDensity = 1, WindowDisplayScale = 1 };
        using WindowApplicationRuntime runtime = new(new SdlWindowPlatform(api, new RecordingGraphicsFactory()));
        Window model = new() { Left = 80, Top = 60, Width = 640, Height = 480, WindowStartupLocation = WindowStartupLocation.Manual };
        runtime.StartMainWindow(model);
        FakeSdlApi.FakeWindow native = Assert.Single(api.Windows.Values);
        (native.State, native.X, native.Y) = (SdlEventKind.WindowMaximized, 0, 29);
        api.Enqueue(new SdlEvent(SdlEventKind.WindowMaximized, native.Id));
        runtime.PumpOnce(TimeSpan.FromMilliseconds(16));
        (native.State, native.X, native.Y) = (SdlEventKind.WindowMinimized, -32000, -32000);
        api.Enqueue(new SdlEvent(SdlEventKind.WindowMinimized, native.Id));
        runtime.PumpOnce(TimeSpan.FromMilliseconds(16));
        List<WindowState> states = [];
        model.StateChanged += (_, _) => states.Add(model.WindowState);

        (native.State, native.X, native.Y) = (SdlEventKind.WindowMaximized, 0, 29);
        api.Enqueue(
            new SdlEvent(SdlEventKind.WindowRestored, native.Id),
            new SdlEvent(SdlEventKind.WindowMaximized, native.Id));
        runtime.PumpOnce(TimeSpan.FromMilliseconds(16));
        (float Left, float Top) whileMaximized = (model.Left, model.Top);
        WindowState[] restoreStates = [.. states];
        model.WindowState = WindowState.Normal;
        runtime.PumpOnce(TimeSpan.FromMilliseconds(16));

        Assert.True(
            whileMaximized == (80, 60) && restoreStates.SequenceEqual([WindowState.Maximized]) && (native.X, native.Y) == (80, 60),
            $"While maximized after the restore from minimized, Left/Top are {whileMaximized} (expected (80, 60)); " +
            $"StateChanged reported [{string.Join(", ", restoreStates)}] (expected [Maximized]); " +
            $"after WindowState = Normal the native window is at ({native.X}, {native.Y}) (expected (80, 60)).");
    }

    [Fact]
    public void AMaximizedWindowWithoutAKnownRestorePositionReportsUnchangedBoundsOnce()
    {
        // Manual startup without Left/Top: no normal position is known, so the maximized window reports NaN.
        FakeSdlApi api = new() { WindowPixelDensity = 1, WindowDisplayScale = 1 };
        using SdlWindowPlatform platform = new(api, new RecordingGraphicsFactory());
        Window model = new() { WindowStartupLocation = WindowStartupLocation.Manual };
        RecordingWindowCallbacks callbacks = new();
        using SdlPlatformWindow window = Assert.IsType<SdlPlatformWindow>(platform.CreateWindow(model, callbacks));
        api.Enqueue(new SdlEvent(SdlEventKind.WindowMaximized, window.WindowId));
        platform.PumpEvents();
        int boundsAfterMaximize = callbacks.Bounds.Count;

        api.Enqueue(new SdlEvent(SdlEventKind.WindowMoved, window.WindowId));
        api.Enqueue(new SdlEvent(SdlEventKind.WindowResized, window.WindowId));
        platform.PumpEvents();

        var bounds = callbacks.Bounds[^1];
        Assert.True(float.IsNaN(bounds.Left) && float.IsNaN(bounds.Top), $"The reported position is ({bounds.Left}, {bounds.Top}).");
        Assert.Equal(boundsAfterMaximize, callbacks.Bounds.Count);
    }
}
