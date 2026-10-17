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
    private const int AScancode = 4;
    private const int LeftCtrlScancode = 224;

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

    [Fact]
    public void AlternatingMoveAndWheelDoNotLagBehindTheInput()
    {
        SdlInputSource source = new();
        int pair = 0;
        // 60 frames; before each, 3 or 4 (move, wheel notch) pairs arrive: 200 pairs in 1 s.
        for (int frame = 0; frame < 60; frame++)
        {
            int target = (frame + 1) * 200 / 60;
            for (; pair < target; pair++)
            {
                source.MovePointer(pair, 10);
                source.AddWheel(1, flipped: false);
            }

            _ = source.GetFrame();
        }

        InputFrame next = source.GetFrame();
        Assert.Equal(200 * 120, next.Pointer.WheelValue);
        Assert.Equal(199, next.Pointer.X);
        Assert.Equal(0, source.GetFrame().Pointer.WheelDelta);
    }

    [Fact]
    public void HeldKeyRepeatsDoNotLagBehindTheInput()
    {
        SdlInputSource source = new();
        source.SetKey(AScancode, true);
        source.AddText("a");
        _ = source.GetFrame();
        // 15 frames (1 s at 15 fps); before each, 2 auto-repeats with their text (30 repeats/s).
        for (int frame = 0; frame < 15; frame++)
        {
            for (int i = 0; i < 2; i++)
            {
                source.SetKey(AScancode, true, repeat: true);
                source.AddText("a");
            }

            InputFrame input = source.GetFrame();
            Assert.Equal(2, input.Keyboard.GetRepeatCount(InputKey.A));
            Assert.Equal(2, input.TextInputEvents.Count);
        }

        source.SetKey(AScancode, false);
        Assert.True(source.GetFrame().Keyboard.IsReleased(InputKey.A));
    }

    [Fact]
    public void HeldKeyPressAndItsRepeatsInOnePumpNeedOnlyOneMoreFrame()
    {
        SdlInputSource source = new();
        source.SetKey(AScancode, true);
        source.AddText("a");
        for (int i = 0; i < 5; i++)
        {
            source.SetKey(AScancode, true, repeat: true);
            source.AddText("a");
        }

        InputFrame pressed = source.GetFrame();
        Assert.True(pressed.Keyboard.IsPressed(InputKey.A));
        Assert.Equal(["a"], pressed.TextInputEvents.Select(text => text.Text));
        InputFrame repeated = source.GetFrame();
        Assert.Equal(5, repeated.Keyboard.GetRepeatCount(InputKey.A));
        Assert.Equal(5, repeated.TextInputEvents.Count);
        Assert.Empty(source.GetFrame().TextInputEvents);
    }

    [Fact]
    public void HeldModifierRepeatsAndWheelDoNotLagBehindTheInput()
    {
        SdlInputSource source = new();
        source.MovePointer(50, 50);
        source.SetKey(LeftCtrlScancode, true);
        _ = source.GetFrame();
        // Ctrl is held for zooming; its auto-repeats alternate with wheel notches, 4 pairs before each frame.
        for (int frame = 0; frame < 30; frame++)
        {
            for (int i = 0; i < 4; i++)
            {
                source.SetKey(LeftCtrlScancode, true, repeat: true);
                source.AddWheel(-1, flipped: false);
            }

            InputFrame input = source.GetFrame();
            Assert.Equal(-4 * 120, input.Pointer.WheelDelta);
            Assert.Equal(4, input.Keyboard.GetRepeatCount(InputKey.LeftCtrl));
        }

        source.SetKey(LeftCtrlScancode, false);
        InputFrame released = source.GetFrame();
        Assert.True(released.Keyboard.IsReleased(InputKey.LeftCtrl));
        Assert.Equal(-30 * 4 * 120, released.Pointer.WheelValue);
    }

    [Fact]
    public void RepeatOfAnotherKeyAfterTextWaitsForTheNextFrame()
    {
        SdlInputSource source = new();
        source.SetKey(BackspaceScancode, true);
        _ = source.GetFrame();
        // X types "x" while Backspace is held; the next Backspace repeat must delete it.
        source.SetKey(XScancode, true);
        source.AddText("x");
        source.SetKey(BackspaceScancode, true, repeat: true);

        InputFrame typed = source.GetFrame();
        Assert.True(typed.Keyboard.IsPressed(InputKey.X));
        Assert.Equal(["x"], typed.TextInputEvents.Select(text => text.Text));
        Assert.Equal(0, typed.Keyboard.GetRepeatCount(InputKey.Back));
        Assert.Equal(1, source.GetFrame().Keyboard.GetRepeatCount(InputKey.Back));
    }

    [Fact]
    public void MoveAfterWheelAndButtonPressWaitsForTheNextFrame()
    {
        SdlInputSource source = new();
        source.MovePointer(10, 20);
        _ = source.GetFrame();
        source.AddWheel(1, flipped: false);
        source.SetButton(1, true);
        source.MovePointer(30, 40);

        InputFrame pressed = source.GetFrame();
        Assert.True(pressed.Pointer.IsPressed(InputMouseButton.Left));
        Assert.Equal(10, pressed.Pointer.X);
        Assert.Equal(120, pressed.Pointer.WheelDelta);
        Assert.Equal(30, source.GetFrame().Pointer.X);
    }

    private static void Pump(WindowApplicationRuntime runtime, FakeSdlApi api, params SdlEvent[] events)
    {
        api.Enqueue(events);
        runtime.PumpOnce(TimeSpan.FromMilliseconds(16));
    }
}
