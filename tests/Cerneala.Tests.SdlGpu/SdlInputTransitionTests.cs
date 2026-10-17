using Cerneala.Platforms.Sdl3;
using Cerneala.UI.Controls;
using Cerneala.UI.Hosting.Windowing;
using Cerneala.UI.Input;

namespace Cerneala.Tests.SdlGpu;

public sealed class SdlInputTransitionTests
{
    private const int BackspaceScancode = 42;
    private const int EndScancode = 77;
    private const int XScancode = 27;

    [Fact]
    public void KeyPressedAndReleasedInOnePumpIsReportedAsPressThenRelease()
    {
        FakeSdlApi api = new();
        using SdlWindowPlatform platform = new(api, new RecordingGraphicsFactory());
        SdlPlatformWindow window = Assert.IsType<SdlPlatformWindow>(
            platform.CreateWindow(new Window(), new RecordingWindowCallbacks()));
        api.Enqueue(
            new SdlEvent(SdlEventKind.KeyDown, window.WindowId, Scancode: BackspaceScancode),
            new SdlEvent(SdlEventKind.KeyUp, window.WindowId, Scancode: BackspaceScancode));

        platform.PumpEvents();

        InputFrame pressed = window.InputSource.GetFrame();
        Assert.True(pressed.Keyboard.IsPressed(InputKey.Back));
        Assert.False(pressed.Keyboard.IsReleased(InputKey.Back));
        InputFrame released = window.InputSource.GetFrame();
        Assert.True(released.Keyboard.IsReleased(InputKey.Back));
        Assert.False(released.Keyboard.IsPressed(InputKey.Back));
        Assert.False(window.InputSource.GetFrame().Keyboard.IsReleased(InputKey.Back));
    }

    [Fact]
    public void KeyDownUpDownInOnePumpIsReportedAsTwoPresses()
    {
        FakeSdlApi api = new();
        using SdlWindowPlatform platform = new(api, new RecordingGraphicsFactory());
        SdlPlatformWindow window = Assert.IsType<SdlPlatformWindow>(
            platform.CreateWindow(new Window(), new RecordingWindowCallbacks()));
        api.Enqueue(
            new SdlEvent(SdlEventKind.KeyDown, window.WindowId, Scancode: BackspaceScancode),
            new SdlEvent(SdlEventKind.KeyUp, window.WindowId, Scancode: BackspaceScancode),
            new SdlEvent(SdlEventKind.KeyDown, window.WindowId, Scancode: BackspaceScancode));

        platform.PumpEvents();

        int presses = 0;
        int releases = 0;
        for (int frame = 0; frame < 5; frame++)
        {
            InputFrame input = window.InputSource.GetFrame();
            presses += input.Keyboard.IsPressed(InputKey.Back) ? 1 : 0;
            releases += input.Keyboard.IsReleased(InputKey.Back) ? 1 : 0;
        }

        Assert.Equal(2, presses);
        Assert.Equal(1, releases);
        Assert.True(window.InputSource.GetFrame().Keyboard.IsDown(InputKey.Back));
    }

    [Fact]
    public void MouseButtonPressedAndReleasedInOnePumpIsReportedAtItsOwnPosition()
    {
        FakeSdlApi api = new() { WindowPixelDensity = 1, WindowDisplayScale = 1 };
        using SdlWindowPlatform platform = new(api, new RecordingGraphicsFactory());
        SdlPlatformWindow window = Assert.IsType<SdlPlatformWindow>(
            platform.CreateWindow(new Window(), new RecordingWindowCallbacks()));
        api.Enqueue(
            new SdlEvent(SdlEventKind.MouseButtonDown, window.WindowId, X: 10, Y: 20, MouseButton: 1),
            new SdlEvent(SdlEventKind.MouseMotion, window.WindowId, X: 30, Y: 40),
            new SdlEvent(SdlEventKind.MouseButtonUp, window.WindowId, X: 30, Y: 40, MouseButton: 1));

        platform.PumpEvents();

        InputFrame pressed = window.InputSource.GetFrame();
        Assert.True(pressed.Pointer.IsPressed(InputMouseButton.Left));
        Assert.Equal(10, pressed.Pointer.X);
        Assert.Equal(20, pressed.Pointer.Y);
        InputFrame released = window.InputSource.GetFrame();
        Assert.True(released.Pointer.IsReleased(InputMouseButton.Left));
        Assert.Equal(30, released.Pointer.X);
        Assert.Equal(40, released.Pointer.Y);
    }

    [Fact]
    public void DeferredTransitionRequestsAnotherFrame()
    {
        FakeSdlApi api = new();
        using SdlWindowPlatform platform = new(api, new RecordingGraphicsFactory());
        RecordingWindowCallbacks callbacks = new();
        SdlPlatformWindow window = Assert.IsType<SdlPlatformWindow>(
            platform.CreateWindow(new Window(), callbacks));
        api.Enqueue(
            new SdlEvent(SdlEventKind.KeyDown, window.WindowId, Scancode: BackspaceScancode),
            new SdlEvent(SdlEventKind.KeyUp, window.WindowId, Scancode: BackspaceScancode));
        platform.PumpEvents();
        int requestsAfterPump = callbacks.RenderRequests;

        _ = window.InputSource.GetFrame();
        Assert.True(callbacks.RenderRequests > requestsAfterPump);
        int requestsAfterPress = callbacks.RenderRequests;

        _ = window.InputSource.GetFrame();
        Assert.Equal(requestsAfterPress, callbacks.RenderRequests);
    }

    [Fact]
    public void FocusedTextBoxDeletesOneCharacterForBackspaceTapInOnePump()
    {
        FakeSdlApi api = new() { WindowPixelDensity = 1, WindowDisplayScale = 1 };
        RecordingGraphicsFactory graphics = new();
        using WindowApplicationRuntime runtime = new(new SdlWindowPlatform(api, graphics));
        TextBox box = new() { Text = "GUVINTNYEG" };
        Window window = new() { Content = box, Width = 320, Height = 200 };
        int backDowns = 0;
        int backUps = 0;
        window.PreviewKeyDown += (_, args) => backDowns += ((KeyEventArgs)args).Key == InputKey.Back ? 1 : 0;
        window.PreviewKeyUp += (_, args) => backUps += ((KeyEventArgs)args).Key == InputKey.Back ? 1 : 0;
        runtime.StartMainWindow(window);
        uint windowId = Assert.IsType<SdlWindowSurface>(Assert.Single(graphics.Surfaces)).WindowId;
        var bounds = box.ArrangedBounds;
        float x = bounds.X + bounds.Width / 2;
        float y = bounds.Y + bounds.Height / 2;
        Pump(runtime, api, new SdlEvent(SdlEventKind.MouseButtonDown, windowId, X: x, Y: y, MouseButton: 1));
        Pump(runtime, api, new SdlEvent(SdlEventKind.MouseButtonUp, windowId, X: x, Y: y, MouseButton: 1));
        Pump(runtime, api, new SdlEvent(SdlEventKind.KeyDown, windowId, Scancode: EndScancode));
        Pump(runtime, api, new SdlEvent(SdlEventKind.KeyUp, windowId, Scancode: EndScancode));
        Assert.True(box.IsKeyboardFocused);

        Pump(runtime, api,
            new SdlEvent(SdlEventKind.KeyDown, windowId, Scancode: BackspaceScancode),
            new SdlEvent(SdlEventKind.KeyUp, windowId, Scancode: BackspaceScancode));
        for (int frame = 0; frame < 4; frame++)
        {
            runtime.PumpOnce(TimeSpan.FromMilliseconds(16));
        }

        Assert.Equal("GUVINTNYE", box.Text);
        Assert.Equal(1, backDowns);
        Assert.Equal(1, backUps);
    }

    [Fact]
    public void TextTypedBeforeBackspaceInOnePumpIsInsertedBeforeTheBackspace()
    {
        FakeSdlApi api = new() { WindowPixelDensity = 1, WindowDisplayScale = 1 };
        RecordingGraphicsFactory graphics = new();
        using WindowApplicationRuntime runtime = new(new SdlWindowPlatform(api, graphics));
        TextBox box = new() { Text = "ab" };
        runtime.StartMainWindow(new Window { Content = box, Width = 320, Height = 200 });
        uint windowId = Assert.IsType<SdlWindowSurface>(Assert.Single(graphics.Surfaces)).WindowId;
        var bounds = box.ArrangedBounds;
        float x = bounds.X + bounds.Width / 2;
        float y = bounds.Y + bounds.Height / 2;
        Pump(runtime, api, new SdlEvent(SdlEventKind.MouseButtonDown, windowId, X: x, Y: y, MouseButton: 1));
        Pump(runtime, api, new SdlEvent(SdlEventKind.MouseButtonUp, windowId, X: x, Y: y, MouseButton: 1));
        Pump(runtime, api, new SdlEvent(SdlEventKind.KeyDown, windowId, Scancode: EndScancode));
        Pump(runtime, api, new SdlEvent(SdlEventKind.KeyUp, windowId, Scancode: EndScancode));

        // X goes down and types "x"; Backspace goes down before X is released.
        Pump(runtime, api,
            new SdlEvent(SdlEventKind.KeyDown, windowId, Scancode: XScancode),
            new SdlEvent(SdlEventKind.TextInput, windowId, Text: "x"),
            new SdlEvent(SdlEventKind.KeyDown, windowId, Scancode: BackspaceScancode),
            new SdlEvent(SdlEventKind.KeyUp, windowId, Scancode: XScancode),
            new SdlEvent(SdlEventKind.KeyUp, windowId, Scancode: BackspaceScancode));
        for (int frame = 0; frame < 4; frame++)
        {
            runtime.PumpOnce(TimeSpan.FromMilliseconds(16));
        }

        Assert.Equal("ab", box.Text);
    }

    [Fact]
    public void ButtonClickedWithinOnePumpRaisesClickOnce()
    {
        FakeSdlApi api = new() { WindowPixelDensity = 1, WindowDisplayScale = 1 };
        RecordingGraphicsFactory graphics = new();
        using WindowApplicationRuntime runtime = new(new SdlWindowPlatform(api, graphics));
        Button button = new() { Content = "Click" };
        int clicks = 0;
        button.Click += (_, _) => clicks++;
        runtime.StartMainWindow(new Window { Content = button, Width = 320, Height = 200 });
        uint windowId = Assert.IsType<SdlWindowSurface>(Assert.Single(graphics.Surfaces)).WindowId;
        var bounds = button.ArrangedBounds;
        float x = bounds.X + bounds.Width / 2;
        float y = bounds.Y + bounds.Height / 2;

        Pump(runtime, api,
            new SdlEvent(SdlEventKind.MouseMotion, windowId, X: x, Y: y),
            new SdlEvent(SdlEventKind.MouseButtonDown, windowId, X: x, Y: y, MouseButton: 1),
            new SdlEvent(SdlEventKind.MouseButtonUp, windowId, X: x, Y: y, MouseButton: 1));
        for (int frame = 0; frame < 4; frame++)
        {
            runtime.PumpOnce(TimeSpan.FromMilliseconds(16));
        }

        Assert.Equal(1, clicks);
    }

    private static void Pump(WindowApplicationRuntime runtime, FakeSdlApi api, params SdlEvent[] events)
    {
        api.Enqueue(events);
        runtime.PumpOnce(TimeSpan.FromMilliseconds(16));
    }
}
