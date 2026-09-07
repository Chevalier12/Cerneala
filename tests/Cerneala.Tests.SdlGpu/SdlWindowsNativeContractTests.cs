using System.Runtime.InteropServices;
using System.Diagnostics;
using Cerneala.Backends.SdlGpu;
using Cerneala.Drawing;
using Cerneala.Platforms.Sdl3;
using Cerneala.UI.Controls;
using Cerneala.UI.Hosting.Windowing;
using Cerneala.UI.Input;
using SDL3;

namespace Cerneala.Tests.SdlGpu;

[Collection(SdlNativeTestCollection.Name)]
public sealed class SdlWindowsNativeContractTests
{
    [WindowsNativeFact]
    public void RuntimePublishesHoveredElementCursorToTheNativeWindow()
    {
        RecordingGraphicsFactory factory = new();
        using WindowApplicationRuntime runtime = new(
            new SdlWindowPlatform(new NativeSdlApi(), factory, coordinateScaleOverride: 1));
        Button button = new() { Content = "Hover", Cursor = Cursor.Crosshair };
        Window model = CreateModel("cursor");
        model.Content = button;
        runtime.StartMainWindow(model);
        SdlWindowSurface surface = Assert.IsType<SdlWindowSurface>(Assert.Single(factory.Surfaces));
        var bounds = button.ArrangedBounds;
        SDL.GetGlobalMouseState(out float previousX, out float previousY);
        try
        {
            SDL.WarpMouseInWindow(surface.WindowHandle, bounds.X + bounds.Width / 2, bounds.Y + bounds.Height / 2);
            runtime.PumpOnce(TimeSpan.FromMilliseconds(16));

            nint crosshair = LoadCursor(0, 32515); // IDC_CROSS
            Assert.NotEqual(0, crosshair);
            Assert.Equal(crosshair, GetCursor());
            nint handle = FindHandle(model);
            SendMessage(handle, 0x0020, (nuint)handle, Pack(1, 0x0200)); // WM_SETCURSOR / HTCLIENT
            Assert.Equal(crosshair, GetCursor());
        }
        finally
        {
            SDL.WarpMouseGlobal(previousX, previousY);
        }
    }

    [WindowsNativeFact]
    public void NativeMaximizeRespectsWidthLimitWithoutHeightLimit() => AssertSingleAxisLimit(width: true);

    [WindowsNativeFact]
    public void NativeMaximizeRespectsHeightLimitWithoutWidthLimit() => AssertSingleAxisLimit(width: false);

    private static void AssertSingleAxisLimit(bool width)
    {
        using WindowApplicationRuntime runtime = CreateRuntime();
        Window model = CreateModel("single-axis maximize");
        if (width)
            model.MaxHeight = float.PositiveInfinity;
        else
            model.MaxWidth = float.PositiveInfinity;
        runtime.StartMainWindow(model);
        nint handle = FindHandle(model);
        SendMessage(handle, 0x0112, 0xF030, 0);
        runtime.PumpOnce(TimeSpan.FromMilliseconds(16));

        Assert.True(IsZoomed(handle));
        Assert.True(GetClientRect(handle, out NativeRect client));
        Assert.InRange(width ? client.Right - client.Left : client.Bottom - client.Top,
            1, (int)(width ? model.MaxWidth : model.MaxHeight));
    }

    [WindowsNativeFact]
    public void NativeMaximizeCoversTheMonitorWorkArea()
    {
        using WindowApplicationRuntime runtime = CreateRuntime();
        Window model = CreateModel("maximize");
        model.MaxWidth = float.PositiveInfinity;
        model.MaxHeight = float.PositiveInfinity;
        runtime.StartMainWindow(model);
        nint handle = FindHandle(model);

        SendMessage(handle, 0x0112, 0xF030, 0); // WM_SYSCOMMAND / SC_MAXIMIZE
        runtime.PumpOnce(TimeSpan.FromMilliseconds(16));

        Assert.True(IsZoomed(handle));
        AssertCoversWorkArea(handle);
    }

    [WindowsNativeFact]
    public void NativeMaximizeRespectsExplicitClientSizeLimits()
    {
        using WindowApplicationRuntime runtime = CreateRuntime();
        Window model = CreateModel("bounded maximize");
        runtime.StartMainWindow(model);
        nint handle = FindHandle(model);

        SendMessage(handle, 0x0112, 0xF030, 0);
        runtime.PumpOnce(TimeSpan.FromMilliseconds(16));

        Assert.True(IsZoomed(handle));
        AssertWithinClientLimits(handle, model);
    }

    [WindowsNativeFact]
    public void ProgrammaticWindowStateCanRestoreAndMaximizeAgain()
    {
        using WindowApplicationRuntime runtime = CreateRuntime();
        Window model = CreateModel("programmatic maximize");
        runtime.StartMainWindow(model);
        nint handle = FindHandle(model);

        model.WindowState = WindowState.Maximized;
        runtime.PumpOnce(TimeSpan.FromMilliseconds(16));
        Assert.True(IsZoomed(handle));
        Assert.Equal(WindowState.Maximized, model.WindowState);
        AssertWithinClientLimits(handle, model);
        Assert.InRange(model.Left, 79, 81);
        Assert.InRange(model.Top, 59, 61);

        model.WindowState = WindowState.Normal;
        runtime.PumpOnce(TimeSpan.FromMilliseconds(16));
        Assert.False(IsZoomed(handle));
        Assert.Equal(WindowState.Normal, model.WindowState);
        Assert.InRange(model.Left, 79, 81);
        Assert.InRange(model.Top, 59, 61);
        Assert.True(GetClientRect(handle, out NativeRect restored));
        Assert.Equal(640, restored.Right - restored.Left);
        Assert.Equal(480, restored.Bottom - restored.Top);

        model.WindowState = WindowState.Maximized;
        runtime.PumpOnce(TimeSpan.FromMilliseconds(16));
        Assert.True(IsZoomed(handle));
        AssertWithinClientLimits(handle, model);
    }

    [WindowsNativeFact]
    public void NativeOwnershipInputAndGraphicsLifetimesRemainIndependent()
    {
        NativeSdlApi api = new();
        using SdlGpuWindowGraphicsSessionFactory factory = new(api);
        using SdlWindowPlatform platform = new(api, factory, coordinateScaleOverride: 1);
        Window ownerModel = CreateModel("owner");
        Window childModel = CreateModel("child");
        using SdlPlatformWindow owner = Assert.IsType<SdlPlatformWindow>(
            platform.CreateWindow(ownerModel, new RecordingWindowCallbacks()));
        using SdlPlatformWindow child = Assert.IsType<SdlPlatformWindow>(
            platform.CreateWindow(childModel, new RecordingWindowCallbacks()));
        child.SetOwner(owner);
        nint ownerHandle = FindHandle(ownerModel);
        nint childHandle = FindHandle(childModel);
        GetWindowThreadProcessId(ownerHandle, out uint ownerProcess);
        GetWindowThreadProcessId(childHandle, out uint childProcess);
        Assert.Equal((uint)Environment.ProcessId, ownerProcess);
        Assert.Equal(ownerProcess, childProcess);
        Assert.Equal(ownerHandle, GetWindow(childHandle, 4));
        Assert.True(IsWindow(ownerHandle));
        Assert.True(IsWindow(childHandle));

        SdlGpuWindowGraphicsSession first = Assert.IsType<SdlGpuWindowGraphicsSession>(owner.GraphicsSession);
        SdlGpuWindowGraphicsSession second = Assert.IsType<SdlGpuWindowGraphicsSession>(child.GraphicsSession);
        Assert.IsType<SdlGpuDrawingBackend>(first.DrawingBackend);
        Assert.NotSame(first, second);
        Assert.NotEqual(first.FrameTexture, second.FrameTexture);
        // SDL intentionally shares the device; the per-window swapchains and targets do not.
        Assert.Equal(first.Device, second.Device);
        first.BeginFrame(new Color(20, 40, 60));
        first.CompleteFrame(present: true);
        owner.Show();
        platform.PumpEvents();
        _ = owner.InputSource.GetFrame();
        _ = child.InputSource.GetFrame();

        SDL.GetGlobalMouseState(out float previousX, out float previousY);
        bool mouseDownAttempted = false;
        bool keyDownAttempted = false;
        List<SdlEvent> observedEvents = [];
        SdlEventWatch inputWatch = value =>
        {
            lock (observedEvents) { observedEvents.Add(value); }
        };
        try
        {
            Assert.True(api.AddEventWatch(inputWatch));
            // Showing a window does not guarantee OS keyboard focus. Click it
            // before typing, otherwise SDL can emit a key event with windowID=0.
            // The window-local warp synthesizes SDL motion and suppresses the
            // associated Win32 motion. Exercise native tracking before clicking.
            NativePoint nativeClick = new() { X = 45, Y = 35 };
            Assert.True(ClientToScreen(ownerHandle, ref nativeClick));
            Assert.True(SDL.WarpMouseGlobal(nativeClick.X, nativeClick.Y));
            SDL.GetGlobalMouseState(out float clickX, out float clickY);
            nint windowAtClick = WindowFromPoint(new() { X = (int)clickX, Y = (int)clickY });
            GetWindowThreadProcessId(windowAtClick, out uint windowAtClickProcess);
            Assert.True(ownerHandle == windowAtClick,
                $"Native click target mismatch at ({clickX},{clickY}): expected window={ownerHandle}, " +
                $"process={Environment.ProcessId}; actual window={windowAtClick}, process={windowAtClickProcess}.");
            Assert.True(SpinWait.SpinUntil(() =>
            {
                platform.PumpEvents();
                return SDL.GetMouseFocus() == owner.Handle;
            }, TimeSpan.FromSeconds(2)), "Native mouse movement did not enter the owner window.");
            mouseDownAttempted = true;
            InjectNativeInput(
                new NativeInput { Data = new() { Mouse = new() { Flags = 0x0002 } } },
                new NativeInput { Data = new() { Mouse = new() { Flags = 0x0004 } } });
            Assert.True(SpinWait.SpinUntil(() =>
            {
                platform.PumpEvents();
                return SDL.GetKeyboardFocus() == owner.Handle;
            }, TimeSpan.FromSeconds(2)), "Clicking the owner did not give it native keyboard focus.");

            keyDownAttempted = true;
            InjectNativeInput(new NativeInput { Type = 1, Data = new() { Keyboard = new() { ScanCode = 0x1E, Flags = 0x0008 } } });
            Assert.True(SpinWait.SpinUntil(() =>
            {
                platform.PumpEvents();
                return SDL.GetKeyboardState(out _)[(int)SDL.Scancode.A];
            }, TimeSpan.FromSeconds(2)), "The native keyboard did not receive the A key-down.");
            InputFrame ownerInput = owner.InputSource.GetFrame();
            InputFrame childInput = child.InputSource.GetFrame();
            SDL.GetMouseState(out float localX, out float localY);
            SDL.GetGlobalMouseState(out float globalX, out float globalY);
            string eventHistory;
            lock (observedEvents) { eventHistory = string.Join("; ", observedEvents); }
            Assert.True(ownerInput.Pointer.X == 45,
                $"Pointer=({ownerInput.Pointer.X},{ownerInput.Pointer.Y}), SDL=({localX},{localY}), " +
                $"global=({globalX},{globalY}), mouseFocus={SDL.GetMouseFocus()}, owner={owner.Handle}, " +
                $"windowAtPointer={WindowFromPoint(new() { X = (int)globalX, Y = (int)globalY })}, nativeOwner={ownerHandle}. Events: {eventHistory}");
            Assert.Equal(35, ownerInput.Pointer.Y);
            Assert.True(ownerInput.Keyboard.IsDown(InputKey.A));
            Assert.False(childInput.Keyboard.IsDown(InputKey.A));
            Assert.Equal(0, childInput.Pointer.X);
            Assert.Equal(0, childInput.Pointer.Y);
        }
        finally
        {
            try
            {
                if (keyDownAttempted)
                    InjectNativeInput(new NativeInput { Type = 1, Data = new() { Keyboard = new() { ScanCode = 0x1E, Flags = 0x000A } } });
                if (mouseDownAttempted)
                    InjectNativeInput(new NativeInput { Data = new() { Mouse = new() { Flags = 0x0004 } } });
                platform.PumpEvents();
            }
            finally
            {
                api.RemoveEventWatch(inputWatch);
                SDL.WarpMouseGlobal(previousX, previousY);
            }
        }

        child.Destroy();
        owner.Destroy();
        Assert.False(IsWindow(childHandle));
        Assert.False(IsWindow(ownerHandle));
    }

    [WindowsNativeFact]
    public void NativeWindowsExposeApplicationIcons()
    {
        using SdlWindowPlatform platform = new(new NativeSdlApi(), new RecordingGraphicsFactory());
        Window model = CreateModel("icons");
        using var window = platform.CreateWindow(model, new RecordingWindowCallbacks());
        nint handle = FindHandle(model);

        Assert.NotEqual(0, GetIcon(handle, small: false));
        Assert.NotEqual(0, GetIcon(handle, small: true));
    }

    [WindowsNativeFact]
    public void CanResizeWithGripExposesBottomRightClientResizeHitTarget()
    {
        using SdlWindowPlatform platform = new(new NativeSdlApi(), new RecordingGraphicsFactory());
        Window model = CreateModel("grip");
        model.ResizeMode = ResizeMode.CanResizeWithGrip;
        using var window = platform.CreateWindow(model, new RecordingWindowCallbacks());
        nint handle = FindHandle(model);
        Assert.True(GetClientRect(handle, out NativeRect client));
        NativePoint point = new() { X = client.Right - 1, Y = client.Bottom - 1 };
        Assert.True(ClientToScreen(handle, ref point));

        Assert.Equal((nint)17, SendMessage(handle, 0x0084, 0, Pack(point.X, point.Y)));
        Assert.Equal((nint)17, HitTestClient(handle,
            client.Right - GetSystemMetrics(2), client.Bottom - GetSystemMetrics(20)));
        Assert.NotEqual((nint)17, HitTestClient(handle,
            client.Right - GetSystemMetrics(2) - 1, client.Bottom - 1));
        Assert.NotEqual((nint)17, HitTestClient(handle,
            client.Right - 1, client.Bottom - GetSystemMetrics(20) - 1));

        window.Show();
        model.WindowState = WindowState.Maximized;
        window.ApplyProperties(model);
        platform.PumpEvents();
        Assert.True(IsZoomed(handle));
        Assert.True(GetClientRect(handle, out NativeRect maximized));
        Assert.NotEqual((nint)17, HitTestClient(handle, maximized.Right - 1, maximized.Bottom - 1));

        model.WindowState = WindowState.Normal;
        model.ResizeMode = ResizeMode.CanResize;
        window.ApplyProperties(model);
        platform.PumpEvents();
        Assert.NotEqual((nint)17, HitTestClient(handle, client.Right - 1, client.Bottom - 1));
    }

    private static nint HitTestClient(nint handle, int x, int y)
    {
        NativePoint point = new() { X = x, Y = y };
        Assert.True(ClientToScreen(handle, ref point));
        return SendMessage(handle, 0x0084, 0, Pack(point.X, point.Y));
    }

    [WindowsNativeFact]
    public void InteractiveResizePresentsBeforeNativeSizeMoveEnds() => AssertInteractivePresentation(resize: true);

    [WindowsNativeFact]
    public void InteractiveMovePresentsBeforeNativeSizeMoveEnds() => AssertInteractivePresentation(resize: false);

    private static void AssertInteractivePresentation(bool resize)
    {
        RecordingGraphicsFactory factory = new();
        using WindowApplicationRuntime runtime = new(new SdlWindowPlatform(new NativeSdlApi(), factory));
        Window model = CreateModel(resize ? "live resize" : "live move");
        runtime.StartMainWindow(model);
        runtime.PumpOnce(TimeSpan.FromMilliseconds(16));
        nint handle = FindHandle(model);
        RecordingGraphicsSession session = Assert.Single(factory.Sessions);
        int before = session.PresentCount;
        Assert.Equal(0, GetClassLongPtr(handle, -10)); // No class background brush erases the retained frame.

        SendMessage(handle, 0x0231, 0, 0); // WM_ENTERSIZEMOVE
        try
        {
            if (resize)
                model.Width = 420;
            else
                model.Left = 120;

            // SDL updates live resize from the native modal-loop timer, not
            // synchronously inside the property setter. Pump that native loop
            // without invoking runtime.PumpOnce (which could render anyway).
            Stopwatch deadline = Stopwatch.StartNew();
            while (session.PresentCount == before && deadline.Elapsed < TimeSpan.FromSeconds(2))
            {
                SDL.WaitEventTimeout(out SDL.Event _, 50);
            }

            Assert.True(session.PresentCount > before);
        }
        finally
        {
            SendMessage(handle, 0x0232, 0, 0); // WM_EXITSIZEMOVE
        }
    }

    private static WindowApplicationRuntime CreateRuntime()
    {
        NativeSdlApi api = new();
        return new(new SdlWindowPlatform(api, new SdlGpuWindowGraphicsSessionFactory(api), coordinateScaleOverride: 1));
    }

    private static Window CreateModel(string scenario) => new()
    {
        Title = $"SDL {scenario} {Guid.NewGuid():N}",
        Width = 640, Height = 480, MaxWidth = 700, MaxHeight = 600, Left = 80, Top = 60
    };

    private static nint FindHandle(Window model)
    {
        nint handle = FindWindow(null, model.Title);
        Assert.NotEqual(0, handle);
        return handle;
    }

    private static void AssertCoversWorkArea(nint handle)
    {
        Assert.True(GetWindowRect(handle, out NativeRect window));
        NativeMonitorInfo monitor = new() { Size = (uint)Marshal.SizeOf<NativeMonitorInfo>() };
        Assert.True(GetMonitorInfo(MonitorFromWindow(handle, 2), ref monitor));
        Assert.True(window.Left <= monitor.WorkArea.Left);
        Assert.True(window.Top <= monitor.WorkArea.Top);
        Assert.True(window.Right >= monitor.WorkArea.Right);
        Assert.True(window.Bottom >= monitor.WorkArea.Bottom);
    }

    private static void AssertWithinClientLimits(nint handle, Window model)
    {
        Assert.True(GetClientRect(handle, out NativeRect client));
        Assert.InRange(client.Right - client.Left, 1, (int)model.MaxWidth);
        Assert.InRange(client.Bottom - client.Top, 1, (int)model.MaxHeight);
    }

    private static nint GetIcon(nint handle, bool small)
    {
        nint icon = SendMessage(handle, 0x007F, small ? (nuint)0 : 1, 0); // WM_GETICON
        return icon != 0 ? icon : GetClassLongPtr(handle, small ? -34 : -14);
    }

    private static nint Pack(int x, int y) => (nint)((y << 16) | (x & 0xFFFF));

    private static void InjectNativeInput(params NativeInput[] inputs) =>
        Assert.Equal((uint)inputs.Length, SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<NativeInput>()));

    private sealed class WindowsNativeFactAttribute : FactAttribute
    {
        public WindowsNativeFactAttribute() => Skip = !OperatingSystem.IsWindows()
            ? "Windows-specific native SDL contract."
            : SdlNativeFactAttribute.NativeSkipReason;
    }

    [DllImport("user32.dll", EntryPoint = "SendMessageW")]
    private static extern nint SendMessage(nint window, uint message, nuint wParam, nint lParam);
    [DllImport("user32.dll", EntryPoint = "LoadCursorW")]
    private static extern nint LoadCursor(nint instance, nint resource);
    [DllImport("user32.dll")]
    private static extern nint GetCursor();
    [DllImport("user32.dll")]
    private static extern int GetSystemMetrics(int index);
    [DllImport("user32.dll", EntryPoint = "FindWindowW", CharSet = CharSet.Unicode)]
    private static extern nint FindWindow(string? className, string windowName);
    [DllImport("user32.dll", EntryPoint = "GetClassLongPtrW")]
    private static extern nint GetClassLongPtr(nint window, int index);
    [DllImport("user32.dll")]
    private static extern nint GetWindow(nint window, uint command);
    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(nint window, out uint processId);
    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool IsWindow(nint window);
    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool IsZoomed(nint window);
    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetWindowRect(nint window, out NativeRect rect);
    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetClientRect(nint window, out NativeRect rect);
    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool ClientToScreen(nint window, ref NativePoint point);
    [DllImport("user32.dll")]
    private static extern nint MonitorFromWindow(nint window, uint flags);
    [DllImport("user32.dll", EntryPoint = "GetMonitorInfoW")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetMonitorInfo(nint monitor, ref NativeMonitorInfo info);
    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint SendInput(uint count, [In] NativeInput[] inputs, int size);
    [DllImport("user32.dll")]
    private static extern nint WindowFromPoint(NativePoint point);
    [StructLayout(LayoutKind.Sequential)]
    private struct NativeInput { public uint Type; public NativeInputData Data; }
    [StructLayout(LayoutKind.Explicit)]
    private struct NativeInputData
    {
        [FieldOffset(0)] public NativeMouseInput Mouse;
        [FieldOffset(0)] public NativeKeyboardInput Keyboard;
    }
    [StructLayout(LayoutKind.Sequential)]
    private struct NativeMouseInput
    {
        public int X, Y;
        public uint MouseData, Flags, Time;
        public nuint ExtraInfo;
    }
    [StructLayout(LayoutKind.Sequential)]
    private struct NativeKeyboardInput
    {
        public ushort VirtualKey, ScanCode;
        public uint Flags, Time;
        public nuint ExtraInfo;
    }
    [StructLayout(LayoutKind.Sequential)]
    private struct NativeRect { public int Left; public int Top; public int Right; public int Bottom; }
    [StructLayout(LayoutKind.Sequential)]
    private struct NativePoint { public int X; public int Y; }
    [StructLayout(LayoutKind.Sequential)]
    private struct NativeMonitorInfo
    {
        public uint Size; public NativeRect Monitor; public NativeRect WorkArea; public uint Flags;
    }
}
