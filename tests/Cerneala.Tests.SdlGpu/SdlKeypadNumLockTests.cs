using Cerneala.Platforms.Sdl3;
using Cerneala.UI.Controls;
using Cerneala.UI.Elements;
using Cerneala.UI.Hosting.Windowing;
using Cerneala.UI.Input;
using SDL3;
using System.Runtime.InteropServices;

namespace Cerneala.Tests.SdlGpu;

// SDL keeps a keypad key's scancode in both Num Lock states and reports the state as
// SDL_KMOD_NUM in the key event's mod. WPF takes Key from the Windows virtual key, which
// is a navigation key for the keypad while Num Lock is off (#585).
[Collection(SdlNativeTestCollection.Name)]
public sealed class SdlKeypadNumLockTests
{
    private const int XScancode = 27;
    private const int EndScancode = 77;
    private const int Keypad4Scancode = 92;
    private const int KeypadPeriodScancode = 99;

    // The key names are those of WPF Key for the Windows virtual key of each keypad key.
    [Theory]
    [InlineData(89, "NumPad1", "End")]
    [InlineData(90, "NumPad2", "Down")]
    [InlineData(91, "NumPad3", "PageDown")]
    [InlineData(92, "NumPad4", "Left")]
    [InlineData(93, "NumPad5", "Clear")]
    [InlineData(94, "NumPad6", "Right")]
    [InlineData(95, "NumPad7", "Home")]
    [InlineData(96, "NumPad8", "Up")]
    [InlineData(97, "NumPad9", "PageUp")]
    [InlineData(98, "NumPad0", "Insert")]
    [InlineData(99, "Decimal", "Delete")]
    public void KeypadKeyRaisesTheKeyOfItsNumLockState(int scancode, string numLockOn, string numLockOff)
    {
        using FakeScenario scenario = new(new TextBox());
        scenario.Focus();

        scenario.Pump(new SdlEvent(SdlEventKind.KeyDown, scenario.WindowId, Scancode: scancode, NumLock: true));
        scenario.Pump(new SdlEvent(SdlEventKind.KeyUp, scenario.WindowId, Scancode: scancode, NumLock: true));
        scenario.Pump(new SdlEvent(SdlEventKind.KeyDown, scenario.WindowId, Scancode: scancode));
        scenario.Pump(new SdlEvent(SdlEventKind.KeyUp, scenario.WindowId, Scancode: scancode));

        Assert.Equal(
            [$"down {numLockOn}", $"up {numLockOn}", $"down {numLockOff}", $"up {numLockOff}"],
            scenario.Log);
    }

    [Fact]
    public void KeypadKeysWithNumLockOffEditTextLikeTheirNavigationKeys()
    {
        TextBox box = new() { Text = "abc" };
        using FakeScenario scenario = new(box);
        scenario.Focus();
        uint windowId = scenario.WindowId;

        scenario.Pump(new SdlEvent(SdlEventKind.KeyDown, windowId, Scancode: EndScancode));
        scenario.Pump(new SdlEvent(SdlEventKind.KeyUp, windowId, Scancode: EndScancode));
        scenario.Pump(new SdlEvent(SdlEventKind.KeyDown, windowId, Scancode: Keypad4Scancode));
        scenario.Pump(new SdlEvent(SdlEventKind.KeyUp, windowId, Scancode: Keypad4Scancode));
        scenario.Pump(
            new SdlEvent(SdlEventKind.KeyDown, windowId, Scancode: XScancode),
            new SdlEvent(SdlEventKind.TextInput, windowId, Text: "X"));
        scenario.Pump(new SdlEvent(SdlEventKind.KeyUp, windowId, Scancode: XScancode));
        Assert.Equal("abXc", box.Text);

        scenario.Pump(new SdlEvent(SdlEventKind.KeyDown, windowId, Scancode: KeypadPeriodScancode));
        scenario.Pump(new SdlEvent(SdlEventKind.KeyUp, windowId, Scancode: KeypadPeriodScancode));

        Assert.Equal("abX", box.Text);
        Assert.Equal(
            ["down End", "up End", "down Left", "up Left", "down X", "up X", "down Delete", "up Delete"],
            scenario.Log);
    }

    // Num Lock pressed while keypad 4 is held: the key-up and the auto-repeats belong to the
    // key that went down, so no key stays down.
    [Theory]
    [InlineData(false, "Left")]
    [InlineData(true, "NumPad4")]
    public void HeldKeypadKeyKeepsItsKeyWhenNumLockChanges(bool numLockAtPress, string key)
    {
        using FakeScenario scenario = new(new TextBox());
        scenario.Focus();
        uint windowId = scenario.WindowId;

        scenario.Pump(new SdlEvent(SdlEventKind.KeyDown, windowId, Scancode: Keypad4Scancode, NumLock: numLockAtPress));
        scenario.Pump(new SdlEvent(SdlEventKind.KeyDown, windowId, Scancode: Keypad4Scancode, Repeat: true, NumLock: !numLockAtPress));
        scenario.Pump(new SdlEvent(SdlEventKind.KeyUp, windowId, Scancode: Keypad4Scancode, NumLock: !numLockAtPress));

        Assert.Equal([$"down {key}", $"down {key}", $"up {key}"], scenario.Log);
        Assert.False(scenario.Window.LastFrame!.Input.Keyboard.IsDown(InputKey.Left));
        Assert.False(scenario.Window.LastFrame!.Input.Keyboard.IsDown(InputKey.NumPad4));
    }

    private sealed class FakeScenario : IDisposable
    {
        private readonly FakeSdlApi api = new() { WindowPixelDensity = 1, WindowDisplayScale = 1 };
        private readonly WindowApplicationRuntime runtime;
        private readonly UIElement target;

        public FakeScenario(UIElement target)
        {
            RecordingGraphicsFactory graphics = new();
            runtime = new(new SdlWindowPlatform(api, graphics));
            this.target = target;
            target.AddHandler(UIElement.KeyDownEvent, (_, args) => Log.Add("down " + ((KeyEventArgs)args).Key), handledEventsToo: true);
            target.AddHandler(UIElement.KeyUpEvent, (_, args) => Log.Add("up " + ((KeyEventArgs)args).Key), handledEventsToo: true);
            Window = new Window { Content = target, Width = 320, Height = 200 };
            runtime.StartMainWindow(Window);
            WindowId = Assert.IsType<SdlWindowSurface>(Assert.Single(graphics.Surfaces)).WindowId;
        }

        public List<string> Log { get; } = [];

        public Window Window { get; }

        public uint WindowId { get; }

        public void Focus()
        {
            var bounds = target.ArrangedBounds;
            float x = bounds.X + bounds.Width / 2;
            float y = bounds.Y + bounds.Height / 2;
            Pump(new SdlEvent(SdlEventKind.MouseButtonDown, WindowId, X: x, Y: y, MouseButton: 1));
            Pump(new SdlEvent(SdlEventKind.MouseButtonUp, WindowId, X: x, Y: y, MouseButton: 1));
            Assert.True(target.IsKeyboardFocused);
        }

        public void Pump(params SdlEvent[] events)
        {
            api.Enqueue(events);
            runtime.PumpOnce(TimeSpan.FromMilliseconds(16));
        }

        public void Dispose() => runtime.Dispose();
    }

    // The scenario of #585 through the real SDL event queue.
    [SdlNativeFact]
    public void NativeKeypadKeyWithNumLockOffMovesTheCaretLikeItsNavigationKey()
    {
        NativeSdlApi api = new();
        RecordingGraphicsFactory graphics = new();
        using WindowApplicationRuntime runtime = new(new SdlWindowPlatform(api, graphics, coordinateScaleOverride: 1));
        TextBox box = new() { Text = "abc" };
        List<string> keys = [];
        box.AddHandler(UIElement.KeyDownEvent, (_, args) => keys.Add(((KeyEventArgs)args).Key.ToString()), handledEventsToo: true);
        runtime.StartMainWindow(new Window { Content = box, Width = 320, Height = 200, Title = $"SDL keypad {Guid.NewGuid():N}" });
        uint windowId = Assert.IsType<SdlWindowSurface>(Assert.Single(graphics.Surfaces)).WindowId;
        PumpNative(runtime, api, new SdlEvent(SdlEventKind.MouseButtonDown, windowId, X: 20, Y: 20, MouseButton: 1));
        PumpNative(runtime, api, new SdlEvent(SdlEventKind.MouseButtonUp, windowId, X: 20, Y: 20, MouseButton: 1));

        PushNativeKey(runtime, windowId, EndScancode, down: true, SDL.Keymod.Num);
        PushNativeKey(runtime, windowId, EndScancode, down: false, SDL.Keymod.Num);
        PushNativeKey(runtime, windowId, Keypad4Scancode, down: true, SDL.Keymod.None);
        PushNativeKey(runtime, windowId, Keypad4Scancode, down: false, SDL.Keymod.None);
        PushNativeKey(runtime, windowId, XScancode, down: true, SDL.Keymod.Num);
        PumpNative(runtime, api, new SdlEvent(SdlEventKind.TextInput, windowId, Text: "X"));
        PushNativeKey(runtime, windowId, XScancode, down: false, SDL.Keymod.Num);
        PushNativeKey(runtime, windowId, Keypad4Scancode, down: true, SDL.Keymod.Num);
        PushNativeKey(runtime, windowId, Keypad4Scancode, down: false, SDL.Keymod.Num);

        Assert.Equal(["End", "Left", "X", "NumPad4"], keys);
        Assert.Equal("abXc", box.Text);
    }

    // #618 with real Windows keyboard input. With Num Lock on, Shift + keypad 4 makes Windows
    // send VK_LEFT between a fake Shift release and a fake Shift press, and WPF raises Left
    // without Shift. SDL reports that key exactly like keypad 4 alone. Each case starts with
    // the caret key (End or Home), presses the keypad key, then types x; expectedKeys are the
    // KeyDown events of the text box.
    [WindowsNativeTheory]
    [InlineData(true, (ushort)0x4F, (ushort)0x4B, true, "End,LeftShift+Shift,Left,LeftShift+Shift,X", "abxc")]
    [InlineData(true, (ushort)0x47, (ushort)0x53, true, "Home,LeftShift+Shift,Delete,LeftShift+Shift,X", "xbc")]
    [InlineData(true, (ushort)0x4F, (ushort)0x4B, false, "End,NumPad4,X", "abc4x")]
    [InlineData(false, (ushort)0x4F, (ushort)0x4B, true, "End,LeftShift+Shift,Left+Shift,X", "abx")]
    public void NativeKeypadKeyRaisesTheKeyWindowsSendsForIt(
        bool numLock, ushort caretScan, ushort keypadScan, bool shift, string expectedKeys, string text)
    {
        NativeSdlApi api = new();
        RecordingGraphicsFactory graphics = new();
        using WindowApplicationRuntime runtime = new(new SdlWindowPlatform(api, graphics, coordinateScaleOverride: 1));
        TextBox box = new() { Text = "abc" };
        List<string> keys = [];
        box.AddHandler(UIElement.KeyDownEvent, (_, args) =>
        {
            KeyEventArgs key = (KeyEventArgs)args;
            keys.Add(key.Key + (key.IsShiftDown ? "+Shift" : ""));
        }, handledEventsToo: true);
        string title = $"SDL keypad shift {Guid.NewGuid():N}";
        runtime.StartMainWindow(new Window { Content = box, Width = 320, Height = 200, Title = title });
        uint windowId = Assert.IsType<SdlWindowSurface>(Assert.Single(graphics.Surfaces)).WindowId;
        nint handle = FindWindow(null, title);
        Assert.NotEqual(0, handle);
        MakeForeground(runtime, handle);
        Assert.Equal(SDL.GetWindowFromID(windowId), SDL.GetKeyboardFocus());
        PumpNative(runtime, api, new SdlEvent(SdlEventKind.MouseButtonDown, windowId, X: 20, Y: 20, MouseButton: 1));
        PumpNative(runtime, api, new SdlEvent(SdlEventKind.MouseButtonUp, windowId, X: 20, Y: 20, MouseButton: 1));
        Assert.True(box.IsKeyboardFocused);

        bool numLockWas = (GetKeyState(VirtualKeyNumLock) & 1) != 0;
        if (numLockWas != numLock)
        {
            ToggleNumLock(runtime);
        }

        try
        {
            Assert.Equal(numLock, (GetKeyState(VirtualKeyNumLock) & 1) != 0);
            keys.Clear();
            SendScancodes(runtime, (caretScan, false, true), (caretScan, true, true));
            if (shift)
            {
                SendScancodes(runtime, (ShiftScan, false, false), (keypadScan, false, false), (keypadScan, true, false), (ShiftScan, true, false));
            }
            else
            {
                SendScancodes(runtime, (keypadScan, false, false), (keypadScan, true, false));
            }

            SendScancodes(runtime, (XScan, false, false), (XScan, true, false));
        }
        finally
        {
            if (numLockWas != numLock)
            {
                ToggleNumLock(runtime);
            }
        }

        Assert.Equal(expectedKeys.Split(','), keys);
        Assert.Equal(text, box.Text);
    }

    private const ushort ShiftScan = 0x2A;
    private const ushort XScan = 0x2D;
    private const int VirtualKeyNumLock = 0x90;

    private static void MakeForeground(WindowApplicationRuntime runtime, nint handle)
    {
        uint foregroundThread = GetWindowThreadProcessId(GetForegroundWindow(), out _);
        uint currentThread = GetCurrentThreadId();
        AttachThreadInput(currentThread, foregroundThread, true);
        try
        {
            SetForegroundWindow(handle);
        }
        finally
        {
            AttachThreadInput(currentThread, foregroundThread, false);
        }

        PumpFor(runtime, 300);
        Assert.Equal(handle, GetForegroundWindow());
        // SDL takes the keyboard focus when the window is activated while it is already the
        // foreground window. The activation SetForegroundWindow causes can come first, so
        // the window gets WM_SETFOCUS again; without the SDL keyboard focus, SDL sends the
        // keys to no window.
        SetFocus(0);
        SetFocus(handle);
        PumpFor(runtime, 300);
    }

    private static void ToggleNumLock(WindowApplicationRuntime runtime)
    {
        SendInputs(
            new NativeInput { Type = 1, Keyboard = new NativeKeyboardInput { VirtualKey = VirtualKeyNumLock } },
            new NativeInput { Type = 1, Keyboard = new NativeKeyboardInput { VirtualKey = VirtualKeyNumLock, Flags = 0x2 } });
        PumpFor(runtime, 300);
    }

    // Each input is a scan code with KEYEVENTF_SCANCODE; extended adds KEYEVENTF_EXTENDEDKEY.
    private static void SendScancodes(WindowApplicationRuntime runtime, params (ushort Scan, bool Up, bool Extended)[] inputs)
    {
        foreach ((ushort scan, bool up, bool extended) in inputs)
        {
            SendInputs(new NativeInput
            {
                Type = 1,
                Keyboard = new NativeKeyboardInput { ScanCode = scan, Flags = 0x8u | (up ? 0x2u : 0) | (extended ? 0x1u : 0) }
            });
            PumpFor(runtime, 150);
        }

        PumpFor(runtime, 150);
    }

    private static void SendInputs(params NativeInput[] inputs) =>
        Assert.Equal((uint)inputs.Length, SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<NativeInput>()));

    private static void PumpFor(WindowApplicationRuntime runtime, int milliseconds)
    {
        long end = Environment.TickCount64 + milliseconds;
        while (Environment.TickCount64 < end)
        {
            runtime.PumpOnce(TimeSpan.FromMilliseconds(16));
        }
    }

    private sealed class WindowsNativeTheoryAttribute : TheoryAttribute
    {
        public WindowsNativeTheoryAttribute() => Skip = !OperatingSystem.IsWindows()
            ? "Windows-specific native SDL contract."
            : SdlNativeFactAttribute.NativeSkipReason;
    }

    [DllImport("user32.dll", EntryPoint = "FindWindowW", CharSet = CharSet.Unicode)]
    private static extern nint FindWindow(string? className, string windowName);
    [DllImport("user32.dll")]
    private static extern nint GetForegroundWindow();
    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetForegroundWindow(nint window);
    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(nint window, out uint processId);
    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool AttachThreadInput(uint attach, uint attachTo, [MarshalAs(UnmanagedType.Bool)] bool doAttach);
    [DllImport("kernel32.dll")]
    private static extern uint GetCurrentThreadId();
    [DllImport("user32.dll")]
    private static extern nint SetFocus(nint window);
    [DllImport("user32.dll")]
    private static extern short GetKeyState(int virtualKey);
    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint SendInput(uint count, [In] NativeInput[] inputs, int size);

    [StructLayout(LayoutKind.Explicit, Size = 40)]
    private struct NativeInput
    {
        [FieldOffset(0)] public uint Type;
        [FieldOffset(8)] public NativeKeyboardInput Keyboard;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeKeyboardInput
    {
        public ushort VirtualKey, ScanCode;
        public uint Flags, Time;
        public nuint ExtraInfo;
    }

    private static void PushNativeKey(WindowApplicationRuntime runtime, uint windowId, int scancode, bool down, SDL.Keymod mod)
    {
        SDL.Event native = default;
        native.Key = new SDL.KeyboardEvent
        {
            Type = down ? SDL.EventType.KeyDown : SDL.EventType.KeyUp,
            WindowID = windowId,
            Scancode = (SDL.Scancode)scancode,
            Key = SDL.GetKeyFromScancode((SDL.Scancode)scancode, mod, false),
            Mod = mod,
            Down = down
        };
        Assert.True(SDL.PushEvent(ref native));
        runtime.PumpOnce(TimeSpan.FromMilliseconds(16));
    }

    private static void PumpNative(WindowApplicationRuntime runtime, NativeSdlApi api, SdlEvent @event)
    {
        Assert.True(api.PushEvent(@event));
        runtime.PumpOnce(TimeSpan.FromMilliseconds(16));
    }
}
