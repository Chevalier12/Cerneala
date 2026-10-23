using Cerneala.Platforms.Sdl3;
using Cerneala.UI.Controls;
using Cerneala.UI.Elements;
using Cerneala.UI.Hosting.Windowing;
using Cerneala.UI.Input;
using Cerneala.UI.Servo;
using ServoApi = Cerneala.UI.Servo.Servo;

namespace Cerneala.Tests.SdlGpu;

public sealed class ServoPlatformInputTests
{
    private const int BackspaceScancode = 42;
    private const int EndScancode = 77;
    private const int Keypad5Scancode = 93;
    private const int KeypadEnterScancode = 88;

    [Fact]
    public void AutoRepeatReachesTheAppThroughTheSdlEventQueue()
    {
        TextBox textBox = new() { Text = "GUVINTNYEG", Width = 200, Height = 40 };
        using Harness harness = new(textBox, "editor");
        harness.Focus("editor");
        List<(string Event, InputKey Key, bool IsRepeat)> keyEvents = [];
        harness.Window.AddHandler(UIElement.KeyDownEvent, (_, args) =>
        {
            KeyEventArgs keyArgs = (KeyEventArgs)args;
            keyEvents.Add(("KeyDown", keyArgs.Key, keyArgs.IsRepeat));
        }, handledEventsToo: true);
        harness.Window.AddHandler(UIElement.KeyUpEvent, (_, args) =>
            keyEvents.Add(("KeyUp", ((KeyEventArgs)args).Key, false)), handledEventsToo: true);

        harness.Send(ServoPlatformInput.KeyDown(InputKey.End));
        harness.Send(ServoPlatformInput.KeyUp(InputKey.End));
        harness.Api.PushedEvents.Clear();
        keyEvents.Clear();
        harness.Send(ServoPlatformInput.KeyDown(InputKey.Back));
        harness.Send(ServoPlatformInput.KeyDown(InputKey.Back, repeat: true));
        harness.Send(ServoPlatformInput.KeyDown(InputKey.Back, repeat: true));
        Assert.True(harness.Window.LastFrame!.Input.Keyboard.IsDown(InputKey.Back));
        Assert.DoesNotContain(keyEvents, entry => entry.Event == "KeyUp");
        harness.Send(ServoPlatformInput.KeyUp(InputKey.Back));

        // The events travelled as SDL events: pushed to the SDL queue, polled, mapped by the
        // window and collected by SdlInputSource.GetFrame, not built as an InputFrame by Servo.
        Assert.Equal(
            [SdlEventKind.KeyDown, SdlEventKind.KeyDown, SdlEventKind.KeyDown, SdlEventKind.KeyUp],
            harness.Api.PushedEvents.Select(pushed => pushed.Kind));
        Assert.All(harness.Api.PushedEvents, pushed =>
        {
            Assert.Equal(BackspaceScancode, pushed.Scancode);
            Assert.Equal(harness.WindowId, pushed.WindowId);
        });
        Assert.Equal([false, true, true, false], harness.Api.PushedEvents.Select(pushed => pushed.Repeat));
        Assert.Equal(
            [("KeyDown", InputKey.Back, false), ("KeyDown", InputKey.Back, true),
                ("KeyDown", InputKey.Back, true), ("KeyUp", InputKey.Back, false)],
            keyEvents);
        Assert.Equal("GUVINTN", textBox.Text);
        Assert.False(harness.Window.LastFrame!.Input.Keyboard.IsDown(InputKey.Back));
    }

    [Fact]
    public void PressAndReleaseInOnePumpReachTheAppBeforeTheAwaitReturns()
    {
        TextBox textBox = new() { Width = 200, Height = 40 };
        using Harness harness = new(textBox, "editor");
        harness.Focus("editor");
        int keyDowns = 0;
        int keyUps = 0;
        int framesWithKeyDown = 0;
        harness.Window.AddHandler(UIElement.KeyDownEvent, (_, args) =>
        {
            if (((KeyEventArgs)args).Key == InputKey.A) keyDowns++;
        }, handledEventsToo: true);
        harness.Window.AddHandler(UIElement.KeyUpEvent, (_, args) =>
        {
            if (((KeyEventArgs)args).Key == InputKey.A) keyUps++;
        }, handledEventsToo: true);
        harness.Window.FrameRendered += (_, _) =>
        {
            if (harness.Window.LastFrame!.Input.Keyboard.IsDown(InputKey.A)) framesWithKeyDown++;
        };

        int keyDownsAtCompletion = -1;
        int keyUpsAtCompletion = -1;
        Task send = harness.Servo.SendPlatformInputAsync(
            ServoPlatformInput.KeyDown(InputKey.A),
            ServoPlatformInput.KeyUp(InputKey.A));
        // Delays the thread that completes send, as a busy runner can, so the asserts must not race the observing continuation (#750).
        send.ContinueWith(_ => Thread.Sleep(50), TaskContinuationOptions.ExecuteSynchronously);
        Task observed = send.ContinueWith(
            _ =>
            {
                keyDownsAtCompletion = Volatile.Read(ref keyDowns);
                keyUpsAtCompletion = Volatile.Read(ref keyUps);
            },
            TaskContinuationOptions.ExecuteSynchronously);
        harness.Pump(send);
        send.GetAwaiter().GetResult();
        observed.GetAwaiter().GetResult();

        // SdlInputSource defers the release of a key pressed in the same pump to the next frame.
        Assert.Equal(1, keyDownsAtCompletion);
        Assert.Equal(1, keyUpsAtCompletion);
        Assert.Equal(1, framesWithKeyDown);
        Assert.False(harness.Window.LastFrame!.Input.Keyboard.IsDown(InputKey.A));
        Assert.Equal(2, harness.Api.PushedEvents.Count);
    }

    [Fact]
    public void ShiftedLetterWithTextAndReleasesInOnePumpTypesTheText()
    {
        TextBox textBox = new() { Width = 200, Height = 40 };
        using Harness harness = new(textBox, "editor");
        harness.Focus("editor");
        List<string> keyEvents = [];
        harness.Window.AddHandler(UIElement.KeyDownEvent, (_, args) =>
            keyEvents.Add("Down:" + ((KeyEventArgs)args).Key), handledEventsToo: true);
        harness.Window.AddHandler(UIElement.KeyUpEvent, (_, args) =>
            keyEvents.Add("Up:" + ((KeyEventArgs)args).Key), handledEventsToo: true);

        harness.Send(
            ServoPlatformInput.KeyDown(InputKey.LeftShift),
            ServoPlatformInput.KeyDown(InputKey.A),
            ServoPlatformInput.Text("A"),
            ServoPlatformInput.KeyUp(InputKey.A),
            ServoPlatformInput.KeyUp(InputKey.LeftShift));

        Assert.Equal("A", textBox.Text);
        Assert.Equal(["Down:LeftShift", "Down:A", "Up:A", "Up:LeftShift"], keyEvents);
        Assert.False(harness.Window.LastFrame!.Input.Keyboard.IsDown(InputKey.A));
        Assert.False(harness.Window.LastFrame.Input.Keyboard.IsDown(InputKey.LeftShift));
    }

    [Fact]
    public void PointerMoveAndButtonPairInOnePumpClicksOnce()
    {
        Button button = new() { Content = "Click", Width = 160, Height = 48 };
        using Harness harness = new(button, "button");
        ServoPoint center = Center(button);
        int clicks = 0;
        button.Click += (_, _) => clicks++;

        int clicksAtCompletion = -1;
        Task send = harness.Servo.SendPlatformInputAsync(
            ServoPlatformInput.PointerMove(center),
            ServoPlatformInput.PointerDown(center, InputMouseButton.Left),
            ServoPlatformInput.PointerUp(center, InputMouseButton.Left));
        // Delays the thread that completes send, as a busy runner can, so the asserts must not race the observing continuation (#750).
        send.ContinueWith(_ => Thread.Sleep(50), TaskContinuationOptions.ExecuteSynchronously);
        Task observed = send.ContinueWith(
            _ => clicksAtCompletion = Volatile.Read(ref clicks),
            TaskContinuationOptions.ExecuteSynchronously);
        harness.Pump(send);
        send.GetAwaiter().GetResult();
        observed.GetAwaiter().GetResult();

        Assert.Equal(1, clicksAtCompletion);
        Assert.Equal(1, clicks);
        Assert.Equal(
            [SdlEventKind.MouseMotion, SdlEventKind.MouseButtonDown, SdlEventKind.MouseButtonUp],
            harness.Api.PushedEvents.Select(pushed => pushed.Kind));
        Assert.Equal([(byte)0, (byte)1, (byte)1], harness.Api.PushedEvents.Select(pushed => pushed.MouseButton));
    }

    [Fact]
    public void WheelDeltaUsesTheUnitOfServoScrollAsync()
    {
        Button button = new() { Content = "Wheel", Width = 160, Height = 48 };
        using Harness harness = new(button, "button");
        ServoPoint center = Center(button);
        List<int> deltas = [];
        harness.Window.AddHandler(UIElement.MouseWheelEvent, (_, args) =>
            deltas.Add(((MouseWheelEventArgs)args).Delta), handledEventsToo: true);

        harness.Send(ServoPlatformInput.PointerMove(center));
        harness.Send(ServoPlatformInput.Wheel(center, 240));
        harness.Send(ServoPlatformInput.Wheel(center, -120));
        int platformWheelValue = harness.Window.LastFrame!.Input.Pointer.WheelValue;
        Task scroll = harness.Servo.ScrollAsync(ServoTarget.ById("button"), 240);
        harness.Pump(scroll);
        scroll.GetAwaiter().GetResult();

        Assert.Equal([240, -120, 240], deltas);
        Assert.Equal(120, platformWheelValue);
        SdlEvent[] wheels = [.. harness.Api.PushedEvents.Where(pushed => pushed.Kind == SdlEventKind.MouseWheel)];
        Assert.Equal([2, -1], wheels.Select(pushed => pushed.Data2));
        Assert.All(wheels, pushed => Assert.False(pushed.WheelFlipped));
    }

    [Fact]
    public void AnEventThatCannotBeMappedPushesNothing()
    {
        Button button = new() { Content = "Wheel", Width = 160, Height = 48 };
        using Harness harness = new(button, "button");
        ServoPoint center = Center(button);

        // SDL reports the wheel in whole notches of 120 units.
        Task send = harness.Servo.SendPlatformInputAsync(
            ServoPlatformInput.KeyDown(InputKey.A),
            ServoPlatformInput.Wheel(center, 100));
        harness.Pump(send);

        ArgumentException failure = Assert.Throws<ArgumentException>(() => send.GetAwaiter().GetResult());
        Assert.Contains("100", failure.Message);
        Assert.Empty(harness.Api.PushedEvents);
    }

    [Fact]
    public void ARejectedPushFailsTheOperation()
    {
        Button button = new() { Content = "Rejected", Width = 160, Height = 48 };
        using Harness harness = new(button, "button");
        harness.Api.PushEventResult = false;

        Task send = harness.Servo.SendPlatformInputAsync(ServoPlatformInput.KeyDown(InputKey.A));
        harness.Pump(send);

        InvalidOperationException failure = Assert.Throws<InvalidOperationException>(
            () => send.GetAwaiter().GetResult());
        Assert.Contains("SDL event push", failure.Message);
    }

    [Fact]
    public void AnOperationCompletesWhenThePlatformDropsItsEvents()
    {
        TextBox textBox = new() { Width = 200, Height = 40 };
        using Harness harness = new(textBox, "editor");
        harness.Focus("editor");
        harness.Api.Windows.Values.Single().TextInputActive = false;

        harness.Send(ServoPlatformInput.Text("x"));

        Assert.Single(harness.Api.PushedEvents);
        Assert.NotEqual("x", textBox.Text);
    }

    [Fact]
    public void CancelingDoesNotRecallPushedEvents()
    {
        TextBox textBox = new() { Width = 200, Height = 40 };
        using Harness harness = new(textBox, "editor");
        harness.Focus("editor");
        using CancellationTokenSource cancellation = new();

        Task send = harness.Servo.SendPlatformInputAsync(
            [ServoPlatformInput.KeyDown(InputKey.A)], cancellation.Token);
        Assert.False(send.IsCompleted);
        cancellation.Cancel();
        harness.Pump(send);

        Assert.ThrowsAny<OperationCanceledException>(() => send.GetAwaiter().GetResult());
        Assert.Single(harness.Api.PushedEvents);
        for (int frame = 0; frame < 4; frame++)
        {
            harness.Runtime.PumpOnce(TimeSpan.FromMilliseconds(16));
        }

        Assert.True(harness.Window.LastFrame!.Input.Keyboard.IsDown(InputKey.A));
        harness.Send(ServoPlatformInput.KeyUp(InputKey.A));
        Assert.False(harness.Window.LastFrame!.Input.Keyboard.IsDown(InputKey.A));
    }

    [Fact]
    public void KeypadKeyReachesTheAppAsItsSdlScancode()
    {
        TextBox textBox = new() { Width = 200, Height = 40 };
        using Harness harness = new(textBox, "editor");
        harness.Focus("editor");
        List<string> keyEvents = [];
        harness.Window.AddHandler(UIElement.KeyDownEvent, (_, args) =>
            keyEvents.Add("Down:" + ((KeyEventArgs)args).Key), handledEventsToo: true);
        harness.Window.AddHandler(UIElement.KeyUpEvent, (_, args) =>
            keyEvents.Add("Up:" + ((KeyEventArgs)args).Key), handledEventsToo: true);

        harness.Send(
            ServoPlatformInput.KeyDown(InputKey.NumPad5),
            ServoPlatformInput.Text("5"),
            ServoPlatformInput.KeyUp(InputKey.NumPad5),
            ServoPlatformInput.KeyDown(InputKey.OemPeriod),
            ServoPlatformInput.Text("."),
            ServoPlatformInput.KeyUp(InputKey.OemPeriod));

        Assert.Equal([Keypad5Scancode, 0, Keypad5Scancode, 55, 0, 55], harness.Api.PushedEvents.Select(pushed => pushed.Scancode));
        Assert.Equal(["Down:NumPad5", "Up:NumPad5", "Down:OemPeriod", "Up:OemPeriod"], keyEvents);
        Assert.Equal("5.", textBox.Text);
    }

    // The scenario of #408: a keypad digit typed while Backspace auto-repeats is deleted by
    // the next repeat.
    [Fact]
    public void KeypadDigitTypedWhileBackspaceRepeatsIsDeletedByTheNextRepeat()
    {
        TextBox textBox = new() { Text = "abcd", Width = 200, Height = 40 };
        using Harness harness = new(textBox, "editor");
        harness.Focus("editor");
        harness.Send(ServoPlatformInput.KeyDown(InputKey.End), ServoPlatformInput.KeyUp(InputKey.End));
        harness.Send(ServoPlatformInput.KeyDown(InputKey.Back));
        Assert.Equal("abc", textBox.Text);

        // In arrival order: "ab", "ab5", "ab".
        harness.Send(
            ServoPlatformInput.KeyDown(InputKey.Back, repeat: true),
            ServoPlatformInput.KeyDown(InputKey.NumPad5),
            ServoPlatformInput.Text("5"),
            ServoPlatformInput.KeyUp(InputKey.NumPad5),
            ServoPlatformInput.KeyDown(InputKey.Back, repeat: true));
        harness.Send(ServoPlatformInput.KeyUp(InputKey.Back));

        Assert.Equal("ab", textBox.Text);
    }

    [Fact]
    public void EveryKeyTheInputSourceMapsRoundTripsThroughMapInputKey()
    {
        int mapped = 0;
        for (int scancode = 0; scancode < 512; scancode++)
        {
            InputKey key = SdlInputSource.MapScancode(scancode, numLock: true);
            if (key is InputKey.Unknown or InputKey.None)
            {
                continue;
            }

            mapped++;
            if (scancode == KeypadEnterScancode)
            {
                // WPF reports keypad Enter as Enter, so Enter maps back to the main Enter key.
                Assert.Equal(InputKey.Enter, key);
                continue;
            }

            Assert.Equal(scancode, SdlInputSource.MapInputKey(key, out bool numLock));
            Assert.True(numLock);
        }

        Assert.True(mapped > 0);
        foreach (InputKey key in Enum.GetValues<InputKey>().Where(key => key is not (InputKey.Unknown or InputKey.None)))
        {
            int scancode = SdlInputSource.MapInputKey(key, out bool numLock);
            Assert.True(scancode >= 0, $"{key} has no SDL scancode.");
            Assert.Equal(key, SdlInputSource.MapScancode(scancode, numLock));
        }

        Assert.Equal(-1, SdlInputSource.MapInputKey(InputKey.None, out _));
        Assert.Equal(-1, SdlInputSource.MapInputKey(InputKey.Unknown, out _));
        Assert.Equal(EndScancode, SdlInputSource.MapInputKey(InputKey.End, out _));
        // Clear is keypad 5 with Num Lock off; the navigation keys go back to their own keys.
        Assert.Equal(Keypad5Scancode, SdlInputSource.MapInputKey(InputKey.Clear, out bool clearNumLock));
        Assert.False(clearNumLock);
    }

    [Fact]
    public void KeypadKeysAreSentWithTheirNumLockState()
    {
        TextBox textBox = new() { Width = 200, Height = 40 };
        using Harness harness = new(textBox, "editor");
        harness.Focus("editor");
        List<string> keyEvents = [];
        harness.Window.AddHandler(UIElement.KeyDownEvent, (_, args) =>
            keyEvents.Add("Down:" + ((KeyEventArgs)args).Key), handledEventsToo: true);
        harness.Window.AddHandler(UIElement.KeyUpEvent, (_, args) =>
            keyEvents.Add("Up:" + ((KeyEventArgs)args).Key), handledEventsToo: true);

        harness.Send(
            ServoPlatformInput.KeyDown(InputKey.NumPad5),
            ServoPlatformInput.KeyUp(InputKey.NumPad5),
            ServoPlatformInput.KeyDown(InputKey.Clear),
            ServoPlatformInput.KeyUp(InputKey.Clear));

        Assert.Equal([Keypad5Scancode, Keypad5Scancode, Keypad5Scancode, Keypad5Scancode], harness.Api.PushedEvents.Select(pushed => pushed.Scancode));
        Assert.Equal([true, true, false, false], harness.Api.PushedEvents.Select(pushed => pushed.NumLock));
        Assert.Equal(["Down:NumPad5", "Up:NumPad5", "Down:Clear", "Up:Clear"], keyEvents);
    }

    private static ServoPoint Center(UIElement element)
    {
        var bounds = element.ArrangedBounds;
        return new ServoPoint(bounds.X + (bounds.Width / 2), bounds.Y + (bounds.Height / 2));
    }

    private sealed class Harness : IDisposable
    {
        private readonly UIElement content;

        public Harness(UIElement content, string id)
        {
            this.content = content;
            Api = new FakeSdlApi { WindowPixelDensity = 1, WindowDisplayScale = 1 };
            Graphics = new RecordingGraphicsFactory();
            Runtime = new WindowApplicationRuntime(new SdlWindowPlatform(Api, Graphics));
            ServoApi.SetId(content, id);
            Window = new Window { Content = content, Width = 320, Height = 200 };
            Runtime.StartMainWindow(Window);
            WindowId = Assert.IsType<SdlWindowSurface>(Assert.Single(Graphics.Surfaces)).WindowId;
            Servo = new ServoApi(Window, new ServoOptions { DefaultTimeout = TimeSpan.FromSeconds(10) });
        }

        public FakeSdlApi Api { get; }

        public RecordingGraphicsFactory Graphics { get; }

        public WindowApplicationRuntime Runtime { get; }

        public Window Window { get; }

        public ServoApi Servo { get; }

        public uint WindowId { get; }

        public void Focus(string id)
        {
            Task click = Servo.ClickAsync(ServoTarget.ById(id));
            Pump(click);
            click.GetAwaiter().GetResult();
            Assert.True(content.IsKeyboardFocused);
        }

        public void Send(params ServoPlatformInput[] events)
        {
            Task send = Servo.SendPlatformInputAsync(events);
            Pump(send);
            send.GetAwaiter().GetResult();
        }

        public void Pump(Task operation)
        {
            for (int frame = 0; frame < 64 && !operation.IsCompleted; frame++)
            {
                Runtime.PumpOnce(TimeSpan.FromMilliseconds(16));
                Thread.Sleep(1);
            }

            Assert.True(operation.IsCompleted, "The Servo operation did not complete within 64 frames.");
        }

        public void Dispose() => Runtime.Dispose();
    }
}
