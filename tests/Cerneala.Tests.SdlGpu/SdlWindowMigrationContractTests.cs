using Cerneala.Platforms.Sdl3;
using Cerneala.Backends.SdlGpu;
using Cerneala.UI;
using Cerneala.UI.Controls;
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
}
