using Cerneala.Drawing;
using Cerneala.Platforms.Sdl3;
using Cerneala.UI.Controls;
using Cerneala.UI.Elements;
using Cerneala.UI.Hosting.Windowing;
using Cerneala.UI.Input;
using Cerneala.UI.Layout;
using Cerneala.UI.Media;

namespace Cerneala.Tests.SdlGpu;

public sealed class SdlInputTransitionTests
{
    private const int BackspaceScancode = 42;
    private const int EndScancode = 77;
    private const int EnterScancode = 40;
    private const int XScancode = 27;
    private const int AScancode = 4;
    private const int LeftCtrlScancode = 224;
    private const int DeleteScancode = 76;
    private const int LeftScancode = 80;
    private const int PeriodScancode = 55;
    private const int Keypad5Scancode = 93;
    private const int NonUsHashScancode = 50;
    private const int KeypadEqualsScancode = 103;

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
    public void AlternatingMoveAndWheelRaiseEachNotchWhereItTurned()
    {
        SdlInputSource source = new();
        // One pump, in the order SdlWindowPlatform makes the calls for three wheel events.
        for (int x = 0; x < 3; x++)
        {
            source.MovePointer(x, 10);
            source.AddWheel(1, flipped: false);
        }

        for (int x = 0; x < 3; x++)
        {
            InputFrame frame = source.GetFrame();
            Assert.Equal(x, frame.Pointer.X);
            Assert.Equal(120, frame.Pointer.WheelDelta);
        }

        Assert.Equal(0, source.GetFrame().Pointer.WheelDelta);
    }

    [Fact]
    public void WheelNotchesAtOnePositionDoNotLagBehindTheInput()
    {
        SdlInputSource source = new();
        source.MovePointer(50, 10);
        _ = source.GetFrame();
        // 60 frames; before each, 3 or 4 wheel events at one position arrive: 200 notches in 1 s.
        int notch = 0;
        for (int frame = 0; frame < 60; frame++)
        {
            int target = (frame + 1) * 200 / 60;
            for (; notch < target; notch++)
            {
                source.MovePointer(50, 10);
                source.AddWheel(1, flipped: false);
            }

            _ = source.GetFrame();
        }

        Assert.Equal(200 * 120, source.GetFrame().Pointer.WheelValue);
    }

    [Fact]
    public void MoveAfterWheelAndKeyRepeatWaitsForTheNextFrame()
    {
        SdlInputSource source = new();
        source.MovePointer(10, 20);
        source.SetKey(LeftCtrlScancode, true);
        _ = source.GetFrame();
        source.AddWheel(1, flipped: false);
        source.SetKey(LeftCtrlScancode, true, repeat: true);
        source.MovePointer(30, 40);

        InputFrame zoomed = source.GetFrame();
        Assert.Equal(10, zoomed.Pointer.X);
        Assert.Equal(120, zoomed.Pointer.WheelDelta);
        Assert.Equal(1, zoomed.Keyboard.GetRepeatCount(InputKey.LeftCtrl));
        Assert.Equal(30, source.GetFrame().Pointer.X);
    }

    [Theory]
    [InlineData(AScancode, InputKey.A, "a")]
    [InlineData(Keypad5Scancode, InputKey.NumPad5, "5")]
    [InlineData(PeriodScancode, InputKey.OemPeriod, ".")]
    public void HeldKeyRepeatsDoNotLagBehindTheInput(int scancode, InputKey key, string text)
    {
        SdlInputSource source = new();
        source.SetKey(scancode, true, numLock: true);
        source.AddText(text);
        _ = source.GetFrame();
        // 15 frames (1 s at 15 fps); before each, 2 auto-repeats with their text (30 repeats/s).
        for (int frame = 0; frame < 15; frame++)
        {
            for (int i = 0; i < 2; i++)
            {
                source.SetKey(scancode, true, repeat: true, numLock: true);
                source.AddText(text);
            }

            InputFrame input = source.GetFrame();
            Assert.Equal(2, input.Keyboard.GetRepeatCount(key));
            Assert.Equal(2, input.TextInputEvents.Count);
        }

        source.SetKey(scancode, false, numLock: true);
        Assert.True(source.GetFrame().Keyboard.IsReleased(key));
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
            Assert.Equal([-120, -120, -120, -120], input.Pointer.WheelDeltas);
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

    [Theory]
    [InlineData(BackspaceScancode, InputKey.Back, KeypadEqualsScancode, "=")]
    [InlineData(BackspaceScancode, InputKey.Back, NonUsHashScancode, "#")]
    [InlineData(DeleteScancode, InputKey.Delete, KeypadEqualsScancode, "=")]
    [InlineData(LeftScancode, InputKey.Left, KeypadEqualsScancode, "=")]
    public void RepeatAfterTextOfAnUnmappedKeyWaitsForTheNextFrame(int heldScancode, InputKey heldKey, int unmappedScancode, string text)
    {
        SdlInputSource source = new();
        source.SetKey(heldScancode, true);
        _ = source.GetFrame();
        // The unmapped key cannot be reported, but it, not the held key, typed the text.
        source.SetKey(heldScancode, true, repeat: true);
        source.SetKey(unmappedScancode, true);
        source.AddText(text);
        source.SetKey(unmappedScancode, false);
        source.SetKey(heldScancode, true, repeat: true);

        InputFrame typed = source.GetFrame();
        Assert.Equal(1, typed.Keyboard.GetRepeatCount(heldKey));
        Assert.Equal([text], typed.TextInputEvents.Select(textInput => textInput.Text));
        InputFrame repeated = source.GetFrame();
        Assert.Equal(1, repeated.Keyboard.GetRepeatCount(heldKey));
        Assert.Empty(repeated.TextInputEvents);
    }

    [Theory]
    [InlineData(BackspaceScancode, InputKey.Back)]
    [InlineData(DeleteScancode, InputKey.Delete)]
    [InlineData(LeftScancode, InputKey.Left)]
    [InlineData(EnterScancode, InputKey.Enter)]
    public void RepeatAfterTextWithNoKeyDownWaitsForTheNextFrame(int heldScancode, InputKey heldKey)
    {
        SdlInputSource source = new();
        source.SetKey(heldScancode, true);
        _ = source.GetFrame();
        // An IME commit or the emoji panel inserts "x" with no key-down; the held key types no text.
        source.SetKey(heldScancode, true, repeat: true);
        source.AddText("x");
        source.SetKey(heldScancode, true, repeat: true);

        InputFrame typed = source.GetFrame();
        Assert.Equal(1, typed.Keyboard.GetRepeatCount(heldKey));
        Assert.Equal(["x"], typed.TextInputEvents.Select(textInput => textInput.Text));
        InputFrame repeated = source.GetFrame();
        Assert.Equal(1, repeated.Keyboard.GetRepeatCount(heldKey));
        Assert.Empty(repeated.TextInputEvents);
    }

    [Fact]
    public void FocusedTextBoxDeletesTextWithNoKeyDownInsertedWhileBackspaceIsHeld()
    {
        FakeSdlApi api = new() { WindowPixelDensity = 1, WindowDisplayScale = 1 };
        RecordingGraphicsFactory graphics = new();
        using WindowApplicationRuntime runtime = new(new SdlWindowPlatform(api, graphics));
        TextBox box = new() { Text = "abcd" };
        runtime.StartMainWindow(new Window { Content = box, Width = 320, Height = 200 });
        uint windowId = Assert.IsType<SdlWindowSurface>(Assert.Single(graphics.Surfaces)).WindowId;
        var bounds = box.ArrangedBounds;
        float x = bounds.X + bounds.Width / 2;
        float y = bounds.Y + bounds.Height / 2;
        Pump(runtime, api, new SdlEvent(SdlEventKind.MouseButtonDown, windowId, X: x, Y: y, MouseButton: 1));
        Pump(runtime, api, new SdlEvent(SdlEventKind.MouseButtonUp, windowId, X: x, Y: y, MouseButton: 1));
        Pump(runtime, api, new SdlEvent(SdlEventKind.KeyDown, windowId, Scancode: EndScancode));
        Pump(runtime, api, new SdlEvent(SdlEventKind.KeyUp, windowId, Scancode: EndScancode));
        Pump(runtime, api, new SdlEvent(SdlEventKind.KeyDown, windowId, Scancode: BackspaceScancode));
        Assert.Equal("abc", box.Text);

        // In arrival order: "ab", "abx", "ab". No key-down types the "x".
        Pump(runtime, api,
            new SdlEvent(SdlEventKind.KeyDown, windowId, Scancode: BackspaceScancode, Repeat: true),
            new SdlEvent(SdlEventKind.TextInput, windowId, Text: "x"),
            new SdlEvent(SdlEventKind.KeyDown, windowId, Scancode: BackspaceScancode, Repeat: true));
        for (int frame = 0; frame < 4; frame++)
        {
            runtime.PumpOnce(TimeSpan.FromMilliseconds(16));
        }

        Assert.Equal("ab", box.Text);
    }

    [Fact]
    public void FocusedTextBoxDeletesKeypadDigitTypedWhileBackspaceIsHeld()
    {
        FakeSdlApi api = new() { WindowPixelDensity = 1, WindowDisplayScale = 1 };
        RecordingGraphicsFactory graphics = new();
        using WindowApplicationRuntime runtime = new(new SdlWindowPlatform(api, graphics));
        TextBox box = new() { Text = "abcd" };
        runtime.StartMainWindow(new Window { Content = box, Width = 320, Height = 200 });
        uint windowId = Assert.IsType<SdlWindowSurface>(Assert.Single(graphics.Surfaces)).WindowId;
        var bounds = box.ArrangedBounds;
        float x = bounds.X + bounds.Width / 2;
        float y = bounds.Y + bounds.Height / 2;
        Pump(runtime, api, new SdlEvent(SdlEventKind.MouseButtonDown, windowId, X: x, Y: y, MouseButton: 1));
        Pump(runtime, api, new SdlEvent(SdlEventKind.MouseButtonUp, windowId, X: x, Y: y, MouseButton: 1));
        Pump(runtime, api, new SdlEvent(SdlEventKind.KeyDown, windowId, Scancode: EndScancode));
        Pump(runtime, api, new SdlEvent(SdlEventKind.KeyUp, windowId, Scancode: EndScancode));
        Pump(runtime, api, new SdlEvent(SdlEventKind.KeyDown, windowId, Scancode: BackspaceScancode));
        Assert.Equal("abc", box.Text);

        // In arrival order: "ab", "ab5", "ab".
        Pump(runtime, api,
            new SdlEvent(SdlEventKind.KeyDown, windowId, Scancode: BackspaceScancode, Repeat: true),
            new SdlEvent(SdlEventKind.KeyDown, windowId, Scancode: Keypad5Scancode, NumLock: true),
            new SdlEvent(SdlEventKind.TextInput, windowId, Text: "5"),
            new SdlEvent(SdlEventKind.KeyUp, windowId, Scancode: Keypad5Scancode, NumLock: true),
            new SdlEvent(SdlEventKind.KeyDown, windowId, Scancode: BackspaceScancode, Repeat: true));
        for (int frame = 0; frame < 4; frame++)
        {
            runtime.PumpOnce(TimeSpan.FromMilliseconds(16));
        }

        Assert.Equal("ab", box.Text);
    }

    [Fact]
    public void FocusedTextBoxRaisesKeyEventsForKeypadAndPunctuationKeys()
    {
        FakeSdlApi api = new() { WindowPixelDensity = 1, WindowDisplayScale = 1 };
        RecordingGraphicsFactory graphics = new();
        using WindowApplicationRuntime runtime = new(new SdlWindowPlatform(api, graphics));
        TextBox box = new();
        List<string> log = [];
        box.AddHandler(UIElement.KeyDownEvent, (_, args) => log.Add("down " + ((KeyEventArgs)args).Key), handledEventsToo: true);
        box.AddHandler(UIElement.KeyUpEvent, (_, args) => log.Add("up " + ((KeyEventArgs)args).Key), handledEventsToo: true);
        runtime.StartMainWindow(new Window { Content = box, Width = 320, Height = 200 });
        uint windowId = Assert.IsType<SdlWindowSurface>(Assert.Single(graphics.Surfaces)).WindowId;
        var bounds = box.ArrangedBounds;
        float x = bounds.X + bounds.Width / 2;
        float y = bounds.Y + bounds.Height / 2;
        Pump(runtime, api, new SdlEvent(SdlEventKind.MouseButtonDown, windowId, X: x, Y: y, MouseButton: 1));
        Pump(runtime, api, new SdlEvent(SdlEventKind.MouseButtonUp, windowId, X: x, Y: y, MouseButton: 1));

        Pump(runtime, api,
            new SdlEvent(SdlEventKind.KeyDown, windowId, Scancode: Keypad5Scancode, NumLock: true),
            new SdlEvent(SdlEventKind.TextInput, windowId, Text: "5"));
        Pump(runtime, api, new SdlEvent(SdlEventKind.KeyUp, windowId, Scancode: Keypad5Scancode, NumLock: true));
        Pump(runtime, api,
            new SdlEvent(SdlEventKind.KeyDown, windowId, Scancode: PeriodScancode),
            new SdlEvent(SdlEventKind.TextInput, windowId, Text: "."));
        Pump(runtime, api, new SdlEvent(SdlEventKind.KeyUp, windowId, Scancode: PeriodScancode));

        Assert.Equal(["down NumPad5", "up NumPad5", "down OemPeriod", "up OemPeriod"], log);
        Assert.Equal("5.", box.Text);
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

        InputFrame wheeled = source.GetFrame();
        Assert.Equal(120, wheeled.Pointer.WheelDelta);
        Assert.False(wheeled.Pointer.IsPressed(InputMouseButton.Left));
        InputFrame pressed = source.GetFrame();
        Assert.True(pressed.Pointer.IsPressed(InputMouseButton.Left));
        Assert.Equal(10, pressed.Pointer.X);
        Assert.Equal(0, pressed.Pointer.WheelDelta);
        Assert.Equal(30, source.GetFrame().Pointer.X);
    }

    [Fact]
    public void ReleaseAfterWheelNotchWaitsForTheNextFrame()
    {
        SdlInputSource source = new();
        source.MovePointer(10, 20);
        source.SetButton(1, true);
        _ = source.GetFrame();
        source.AddWheel(-1, flipped: false);
        source.SetButton(1, false);

        InputFrame wheeled = source.GetFrame();
        Assert.Equal(-120, wheeled.Pointer.WheelDelta);
        Assert.False(wheeled.Pointer.IsReleased(InputMouseButton.Left));
        InputFrame released = source.GetFrame();
        Assert.True(released.Pointer.IsReleased(InputMouseButton.Left));
        Assert.Equal(0, released.Pointer.WheelDelta);
    }

    [Fact]
    public void ButtonsAndWheelInOnePumpAreRaisedInArrivalOrder()
    {
        FakeSdlApi api = new() { WindowPixelDensity = 1, WindowDisplayScale = 1 };
        RecordingGraphicsFactory graphics = new();
        using WindowApplicationRuntime runtime = new(new SdlWindowPlatform(api, graphics));
        Border target = new() { Width = 200, Height = 100, Background = new SolidColorBrush(new Color(1, 2, 3)) };
        Window window = new() { Content = target, Width = 320, Height = 200 };
        List<string> log = [];
        window.AddHandler(UIElement.PreviewMouseDownEvent, (_, args) => log.Add("down " + ((MouseButtonEventArgs)args).ChangedButton), handledEventsToo: true);
        window.AddHandler(UIElement.PreviewMouseWheelEvent, (_, _) => log.Add("wheel"), handledEventsToo: true);
        runtime.StartMainWindow(window);
        uint windowId = Assert.IsType<SdlWindowSurface>(Assert.Single(graphics.Surfaces)).WindowId;
        var bounds = target.ArrangedBounds;
        float x = bounds.X + bounds.Width / 2;
        float y = bounds.Y + bounds.Height / 2;
        Pump(runtime, api, new SdlEvent(SdlEventKind.MouseMotion, windowId, X: x, Y: y));

        // Right goes down, then Left, then the wheel turns one notch.
        Pump(runtime, api,
            new SdlEvent(SdlEventKind.MouseButtonDown, windowId, X: x, Y: y, MouseButton: 3),
            new SdlEvent(SdlEventKind.MouseButtonDown, windowId, X: x, Y: y, MouseButton: 1),
            new SdlEvent(SdlEventKind.MouseWheel, windowId, X: x, Y: y, Data2: 1));
        for (int frame = 0; frame < 5; frame++)
        {
            runtime.PumpOnce(TimeSpan.FromMilliseconds(16));
        }

        Assert.Equal(["down Right", "down Left", "wheel"], log);
    }

    [Theory]
    [InlineData("nothing")]
    [InlineData("a Ctrl auto-repeat")]
    [InlineData("a pointer move")]
    public void EveryWheelNotchOfOnePumpScrollsAScrollViewerOneStep(string beforeEachNotch)
    {
        FakeSdlApi api = new() { WindowPixelDensity = 1, WindowDisplayScale = 1 };
        RecordingGraphicsFactory graphics = new();
        using WindowApplicationRuntime runtime = new(new SdlWindowPlatform(api, graphics));
        ScrollViewer viewer = new() { Content = new Border { Height = 1000 } };
        Window window = new() { Content = viewer, Width = 320, Height = 200 };
        List<int> deltas = [];
        window.AddHandler(InputEvents.MouseWheelEvent, (_, args) => deltas.Add(((MouseWheelEventArgs)args).Delta), handledEventsToo: true);
        runtime.StartMainWindow(window);
        uint windowId = Assert.IsType<SdlWindowSurface>(Assert.Single(graphics.Surfaces)).WindowId;
        var bounds = viewer.ArrangedBounds;
        float x = bounds.X + bounds.Width / 2;
        float y = bounds.Y + bounds.Height / 2;
        Pump(runtime, api, new SdlEvent(SdlEventKind.MouseMotion, windowId, X: x, Y: y));
        if (beforeEachNotch == "a Ctrl auto-repeat")
        {
            Pump(runtime, api, new SdlEvent(SdlEventKind.KeyDown, windowId, Scancode: LeftCtrlScancode));
        }

        List<SdlEvent> pump = [];
        for (int notch = 0; notch < 3; notch++)
        {
            if (beforeEachNotch == "a Ctrl auto-repeat")
            {
                pump.Add(new SdlEvent(SdlEventKind.KeyDown, windowId, Scancode: LeftCtrlScancode, Repeat: true));
            }
            else if (beforeEachNotch == "a pointer move")
            {
                pump.Add(new SdlEvent(SdlEventKind.MouseMotion, windowId, X: x, Y: y + notch + 1));
            }

            pump.Add(new SdlEvent(SdlEventKind.MouseWheel, windowId, X: x, Y: y + notch + 1, Data2: -1));
        }

        Pump(runtime, api, [.. pump]);
        for (int frame = 0; frame < 4; frame++)
        {
            runtime.PumpOnce(TimeSpan.FromMilliseconds(16));
        }

        Assert.Equal([-120, -120, -120], deltas);
        Assert.Equal(3 * 48, viewer.Presenter.VerticalOffset);
    }

    [Theory]
    [InlineData(1, InputMouseButton.Left)]
    [InlineData(2, InputMouseButton.Middle)]
    [InlineData(3, InputMouseButton.Right)]
    public void WheelNotchBeforeAMoveAndPressElsewhereInOnePumpIsRaisedWhereTheWheelTurned(byte sdlButton, InputMouseButton button)
    {
        FakeSdlApi api = new() { WindowPixelDensity = 1, WindowDisplayScale = 1 };
        RecordingGraphicsFactory graphics = new();
        using WindowApplicationRuntime runtime = new(new SdlWindowPlatform(api, graphics));
        Border a = new() { Width = 100, Height = 50 };
        Border b = new() { Width = 100, Height = 50, Margin = new Thickness(100, 0, 0, 0) };
        StackPanel panel = new()
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top
        };
        panel.VisualChildren.Add(a);
        panel.VisualChildren.Add(b);
        List<string> log = [];
        a.AddHandler(UIElement.MouseWheelEvent, (_, _) => log.Add("wheel on A"), handledEventsToo: true);
        b.AddHandler(UIElement.MouseWheelEvent, (_, _) => log.Add("wheel on B"), handledEventsToo: true);
        a.AddHandler(UIElement.MouseDownEvent, (_, args) => log.Add($"{((MouseButtonEventArgs)args).ChangedButton} down on A"), handledEventsToo: true);
        b.AddHandler(UIElement.MouseDownEvent, (_, args) => log.Add($"{((MouseButtonEventArgs)args).ChangedButton} down on B"), handledEventsToo: true);
        runtime.StartMainWindow(new Window { Content = panel, Width = 320, Height = 200 });
        uint windowId = Assert.IsType<SdlWindowSurface>(Assert.Single(graphics.Surfaces)).WindowId;
        Assert.Equal(200, b.ArrangedBounds.X);
        Pump(runtime, api, new SdlEvent(SdlEventKind.MouseMotion, windowId, X: 20, Y: 20));

        // The wheel turns over A; then the pointer moves to B and presses, all in one pump.
        Pump(runtime, api,
            new SdlEvent(SdlEventKind.MouseWheel, windowId, Data2: -1, X: 20, Y: 20),
            new SdlEvent(SdlEventKind.MouseMotion, windowId, X: 220, Y: 20),
            new SdlEvent(SdlEventKind.MouseButtonDown, windowId, X: 220, Y: 20, MouseButton: sdlButton));
        for (int frame = 0; frame < 4; frame++)
        {
            runtime.PumpOnce(TimeSpan.FromMilliseconds(16));
        }

        Assert.Equal(["wheel on A", $"{button} down on B"], log);
    }

    [Fact]
    public void PressAfterWheelNotchInOnePumpIsRaisedOnTheRowScrolledUnderThePointer()
    {
        FakeSdlApi api = new() { WindowPixelDensity = 1, WindowDisplayScale = 1 };
        RecordingGraphicsFactory graphics = new();
        using WindowApplicationRuntime runtime = new(new SdlWindowPlatform(api, graphics));
        StackPanel panel = new();
        List<Border> rows = [];
        for (int i = 0; i < 60; i++)
        {
            Border row = new() { Height = 20, Background = new SolidColorBrush(new Color((byte)(i * 4), 80, 120)) };
            rows.Add(row);
            panel.VisualChildren.Add(row);
        }

        ScrollViewer viewer = new() { Content = panel, Width = 300, Height = 200 };
        Window window = new() { Content = viewer, Width = 320, Height = 220 };
        List<string> log = [];
        window.AddHandler(UIElement.PreviewMouseWheelEvent, (_, _) => log.Add("wheel"), handledEventsToo: true);
        for (int i = 0; i < rows.Count; i++)
        {
            int index = i;
            rows[i].AddHandler(UIElement.PreviewMouseDownEvent, (_, _) => log.Add($"down on row {index}"), handledEventsToo: true);
        }

        runtime.StartMainWindow(window);
        uint windowId = Assert.IsType<SdlWindowSurface>(Assert.Single(graphics.Surfaces)).WindowId;
        float x = rows[5].ArrangedBounds.X + 20;
        float y = rows[5].ArrangedBounds.Y + 10;
        Pump(runtime, api, new SdlEvent(SdlEventKind.MouseMotion, windowId, X: x, Y: y));

        // One wheel notch down scrolls row 7 under the pointer; the press arrives in the same pump.
        Pump(runtime, api,
            new SdlEvent(SdlEventKind.MouseWheel, windowId, X: x, Y: y, Data2: -1),
            new SdlEvent(SdlEventKind.MouseButtonDown, windowId, X: x, Y: y, MouseButton: 1));
        for (int frame = 0; frame < 4; frame++)
        {
            runtime.PumpOnce(TimeSpan.FromMilliseconds(16));
        }

        Assert.True(viewer.Presenter.VerticalOffset > 0);
        int rowUnderPointer = rows.FindIndex(row => y >= row.ArrangedBounds.Y && y < row.ArrangedBounds.Y + row.ArrangedBounds.Height);
        Assert.Equal(7, rowUnderPointer);
        Assert.Equal(["wheel", "down on row 7"], log);
    }

    private static void Pump(WindowApplicationRuntime runtime, FakeSdlApi api, params SdlEvent[] events)
    {
        api.Enqueue(events);
        runtime.PumpOnce(TimeSpan.FromMilliseconds(16));
    }
}
