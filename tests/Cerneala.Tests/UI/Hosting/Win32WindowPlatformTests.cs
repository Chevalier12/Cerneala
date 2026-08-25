using System.Diagnostics;
using System.Runtime.InteropServices;
using Cerneala.UI.Controls;
using Cerneala.UI.Hosting;
using Cerneala.UI.Hosting.Windowing;
using Cerneala.UI.Hosting.Windows;
using Cerneala.Drawing;
using Cerneala.Drawing.MonoGame;
using Cerneala.Drawing.Prism.Graph;
using Cerneala.UI.Input;
using Cerneala.UI.Platform;
using Cerneala.UI.Resources;

namespace Cerneala.Tests.UI.Hosting;

public sealed class Win32WindowPlatformTests
{
    private const uint WmNcHitTest = 0x0084;
    private const uint WmEnterSizeMove = 0x0231;
    private const uint WmExitSizeMove = 0x0232;
    private const int HtBottomRight = 17;
    private const int GclpBackground = -10;
    private const int GclpIcon = -14;
    private const int GclpIconSmall = -34;

    [Theory]
    [InlineData(CursorShape.Default, Win32.IDC_ARROW)]
    [InlineData(CursorShape.Arrow, Win32.IDC_ARROW)]
    [InlineData(CursorShape.Hand, Win32.IDC_HAND)]
    [InlineData(CursorShape.IBeam, Win32.IDC_IBEAM)]
    [InlineData(CursorShape.Crosshair, Win32.IDC_CROSS)]
    [InlineData(CursorShape.ResizeHorizontal, Win32.IDC_SIZEWE)]
    [InlineData(CursorShape.ResizeVertical, Win32.IDC_SIZENS)]
    public void CursorServiceMapsPlatformShapesToWin32Resources(CursorShape shape, int resourceId)
    {
        int loadedResource = 0;
        nint appliedHandle = 0;
        Win32CursorService service = new(
            requestedResource =>
            {
                loadedResource = requestedResource;
                return requestedResource + 1000;
            },
            handle => appliedHandle = handle);

        service.SetCursor(shape);

        Assert.Equal(resourceId, loadedResource);
        Assert.Equal((nint)(resourceId + 1000), appliedHandle);
        Assert.Equal(shape, service.Current);
    }

    [Fact]
    public void HiddenCursorPublishesAZeroHandle()
    {
        bool loaded = false;
        nint appliedHandle = -1;
        Win32CursorService service = new(
            _ =>
            {
                loaded = true;
                return 1;
            },
            handle => appliedHandle = handle);

        service.SetCursor(CursorShape.Hidden);

        Assert.False(loaded);
        Assert.Equal(0, appliedHandle);
        Assert.Equal(CursorShape.Hidden, service.Current);
    }

    [Fact]
    public void RuntimePublishesHoveredElementCursorToWin32()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        nint arrow = Win32.LoadCursor(0, Win32.IDC_ARROW);
        try
        {
            RecordingGraphicsFactory factory = new();
            using WindowApplicationRuntime runtime = new(new Win32WindowPlatform(factory));
            Button button = new()
            {
                Content = "Hover",
                Cursor = Cursor.Crosshair
            };
            Window source = new()
            {
                Title = $"Cerneala cursor {Guid.NewGuid():N}",
                Content = button
            };
            runtime.StartMainWindow(source);
            LayoutRect bounds = button.ArrangedBounds;
            float scale = factory.CoordinateScale;
            int x = (int)MathF.Round((bounds.X + (bounds.Width / 2)) * scale);
            int y = (int)MathF.Round((bounds.Y + (bounds.Height / 2)) * scale);

            SendMessage(factory.WindowHandle, Win32.WM_MOUSEMOVE, 0, PackCoordinates(x, y));
            runtime.PumpOnce(TimeSpan.FromMilliseconds(16));

            nint crosshair = Win32.LoadCursor(0, Win32.IDC_CROSS);
            Assert.Equal(crosshair, GetCursor());

            SendMessage(
                factory.WindowHandle,
                Win32.WM_SETCURSOR,
                (nuint)factory.WindowHandle,
                PackCoordinates(Win32.HTCLIENT, (int)Win32.WM_MOUSEMOVE));

            Assert.Equal(crosshair, GetCursor());
        }
        finally
        {
            Win32.SetCursor(arrow);
        }
    }

    [Fact]
    public void NativeMaximizeCoversTheMonitorWorkArea()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        using WindowApplicationRuntime runtime = new(CreateWindowsDxPlatform());
        string title = $"Cerneala maximize {Guid.NewGuid():N}";
        Window source = new()
        {
            Title = title,
            Width = 640,
            Height = 480,
            MaxWidth = 700,
            MaxHeight = 600,
            Left = 80,
            Top = 60
        };
        runtime.StartMainWindow(source);
        nint handle = FindWindow(null, title);
        Assert.NotEqual(0, handle);

        SendMessage(handle, Win32.WM_SYSCOMMAND, Win32.SC_MAXIMIZE, 0);
        runtime.PumpOnce(TimeSpan.FromMilliseconds(16));

        Assert.True(IsZoomed(handle));
        Assert.True(GetWindowRect(handle, out NativeRect windowRect));
        nint monitor = MonitorFromWindow(handle, 2);
        NativeMonitorInfo monitorInfo = new() { Size = (uint)Marshal.SizeOf<NativeMonitorInfo>() };
        Assert.True(GetMonitorInfo(monitor, ref monitorInfo));
        Assert.True(windowRect.Left <= monitorInfo.WorkArea.Left);
        Assert.True(windowRect.Top <= monitorInfo.WorkArea.Top);
        Assert.True(windowRect.Right >= monitorInfo.WorkArea.Right);
        Assert.True(windowRect.Bottom >= monitorInfo.WorkArea.Bottom);
    }

    [Fact]
    public void ProgrammaticWindowStateCanRestoreAndMaximizeAgain()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        using WindowApplicationRuntime runtime = new(CreateWindowsDxPlatform());
        string title = $"Cerneala programmatic maximize {Guid.NewGuid():N}";
        Window source = new()
        {
            Title = title,
            Width = 640,
            Height = 480,
            MaxWidth = 700,
            MaxHeight = 600,
            Left = 80,
            Top = 60
        };
        runtime.StartMainWindow(source);
        nint handle = FindWindow(null, title);
        Assert.NotEqual(0, handle);

        source.WindowState = WindowState.Maximized;
        runtime.PumpOnce(TimeSpan.FromMilliseconds(16));
        Assert.True(IsZoomed(handle));
        Assert.Equal(WindowState.Maximized, source.WindowState);
        Assert.InRange(source.Left, 79, 81);
        Assert.InRange(source.Top, 59, 61);

        source.WindowState = WindowState.Normal;
        runtime.PumpOnce(TimeSpan.FromMilliseconds(16));
        Assert.False(IsZoomed(handle));
        Assert.Equal(WindowState.Normal, source.WindowState);
        Assert.InRange(source.Left, 79, 81);
        Assert.InRange(source.Top, 59, 61);

        source.WindowState = WindowState.Maximized;
        runtime.PumpOnce(TimeSpan.FromMilliseconds(16));

        Assert.True(IsZoomed(handle));
        Assert.True(GetWindowRect(handle, out NativeRect windowRect));
        nint monitor = MonitorFromWindow(handle, 2);
        NativeMonitorInfo monitorInfo = new() { Size = (uint)Marshal.SizeOf<NativeMonitorInfo>() };
        Assert.True(GetMonitorInfo(monitor, ref monitorInfo));
        Assert.True(windowRect.Left <= monitorInfo.WorkArea.Left);
        Assert.True(windowRect.Top <= monitorInfo.WorkArea.Top);
        Assert.True(windowRect.Right >= monitorInfo.WorkArea.Right);
        Assert.True(windowRect.Bottom >= monitorInfo.WorkArea.Bottom);
    }

    [Fact]
    public void GraphicsFactoryReceivesTheHwndAndResizeIsCoalesced()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        RecordingGraphicsFactory factory = new();
        using Win32WindowPlatform platform = new(factory);
        Window source = new() { Title = "Factory", Width = 320, Height = 200 };
        using IPlatformWindow window = platform.CreateWindow(source, new CallbackSink());
        RecordingGraphicsSession session = Assert.Single(factory.Sessions);

        Assert.Equal(NativeHandle(window), factory.WindowHandle);
        Assert.True(factory.PixelWidth > 0);
        Assert.True(factory.PixelHeight > 0);
        Assert.Equal(window.Viewport.Scale, factory.CoordinateScale);

        source.Width = 420;
        source.Height = 260;
        window.ApplyProperties(source);

        Assert.Equal(1, session.ResizeCount);
        Assert.True(session.PixelWidth > factory.PixelWidth);
        Assert.True(session.PixelHeight > factory.PixelHeight);
        window.Hide();
        Assert.Equal(0, session.DisposeCount);
        window.Dispose();
        Assert.Equal(1, session.DisposeCount);
    }

    [Fact]
    public void WindowsDxFactoryRejectsANonWin32WindowSurface()
    {
        WindowsDxWindowGraphicsSessionFactory factory = new();

        ArgumentException exception = Assert.Throws<ArgumentException>(() =>
            factory.Create(new ForeignWindowSurface(), 32, 24, 1));

        Assert.Equal("windowSurface", exception.ParamName);
        Assert.Contains(nameof(Win32WindowSurface), exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void NativeWindowsBelongToCurrentProcessAndUseNativeOwnership()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        using Win32WindowPlatform platform = CreateWindowsDxPlatform();
        CallbackSink callbacks = new();
        using IPlatformWindow owner = platform.CreateWindow(new Window { Title = "Owner" }, callbacks);
        using IPlatformWindow child = platform.CreateWindow(new Window { Title = "Child" }, callbacks);

        child.SetOwner(owner);
        nint ownerHandle = NativeHandle(owner);
        nint childHandle = NativeHandle(child);
        GetWindowThreadProcessId(ownerHandle, out uint ownerProcessId);
        GetWindowThreadProcessId(childHandle, out uint childProcessId);

        Assert.Equal((uint)Process.GetCurrentProcess().Id, ownerProcessId);
        Assert.Equal(ownerProcessId, childProcessId);
        Assert.Equal(ownerHandle, GetWindow(childHandle, 4));
        Assert.True(IsWindow(ownerHandle));
        Assert.True(IsWindow(childHandle));

        DrawCommandList commands = new();
        commands.Add(DrawCommand.FillRectangle(new DrawRect(0, 0, 32, 32), new Color(20, 40, 60)));
        owner.GraphicsSession.BeginFrame(Color.White);
        Render(owner.GraphicsSession.DrawingBackend, commands);
        owner.Show();
        owner.GraphicsSession.Present();
        platform.PumpEvents();

        Assert.IsType<MonoGameDrawingBackend>(owner.GraphicsSession.DrawingBackend);
        Assert.NotSame(owner.GraphicsSession, child.GraphicsSession);
        Assert.NotSame(
            Assert.IsType<WindowsDxWindowGraphicsSession>(owner.GraphicsSession).GraphicsDevice,
            Assert.IsType<WindowsDxWindowGraphicsSession>(child.GraphicsSession).GraphicsDevice);

        SendMessage(ownerHandle, Win32.WM_MOUSEMOVE, 0, PackCoordinates(45, 35));
        SendMessage(ownerHandle, Win32.WM_KEYDOWN, 0x41, 0);
        InputFrame ownerInput = owner.InputSource.GetFrame();
        InputFrame childInput = child.InputSource.GetFrame();
        Assert.True(ownerInput.Pointer.X > 0);
        Assert.True(ownerInput.Pointer.Y > 0);
        Assert.True(ownerInput.Keyboard.IsDown(InputKey.A));
        Assert.Equal(0, childInput.Pointer.X);
        Assert.Equal(0, childInput.Pointer.Y);
        Assert.False(childInput.Keyboard.IsDown(InputKey.A));

        child.Destroy();
        owner.Destroy();
        Assert.False(IsWindow(childHandle));
        Assert.False(IsWindow(ownerHandle));
    }

    [Fact]
    public void NativeWindowsExposeApplicationIcons()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        using Win32WindowPlatform platform = new(new RecordingGraphicsFactory());
        using IPlatformWindow window = platform.CreateWindow(
            new Window { Title = "Application icon" },
            new CallbackSink());

        Assert.NotEqual(0, GetClassLongPtr(NativeHandle(window), GclpIcon));
        Assert.NotEqual(0, GetClassLongPtr(NativeHandle(window), GclpIconSmall));
    }

    [Fact]
    public void DuplicateMouseMoveDoesNotRequestAnotherFrame()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        using Win32WindowPlatform platform = new(new RecordingGraphicsFactory());
        CallbackSink callbacks = new();
        using IPlatformWindow window = platform.CreateWindow(new Window { Title = "Mouse move coalescing" }, callbacks);

        nint handle = NativeHandle(window);
        SendMessage(handle, Win32.WM_MOUSEMOVE, 0, PackCoordinates(0, 0));
        SendMessage(handle, Win32.WM_MOUSEMOVE, 0, PackCoordinates(0, 0));

        Assert.Equal(1, callbacks.RenderRequestCount);
    }

    [Fact]
    public void CanResizeWithGripExposesBottomRightClientResizeHitTarget()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        using Win32WindowPlatform platform = new(new RecordingGraphicsFactory());
        Window source = new() { Title = "Resize grip", ResizeMode = ResizeMode.CanResizeWithGrip };
        using IPlatformWindow window = platform.CreateWindow(source, new CallbackSink());
        nint handle = NativeHandle(window);
        Assert.True(GetClientRect(handle, out NativeRect client));
        NativePoint point = new(client.Right - 1, client.Bottom - 1);
        Assert.True(ClientToScreen(handle, ref point));

        nint gripResult = SendMessage(handle, WmNcHitTest, 0, PackCoordinates(point.X, point.Y));
        Assert.Equal((nint)HtBottomRight, gripResult);

        source.ResizeMode = ResizeMode.CanResize;
        window.ApplyProperties(source);
        nint normalResult = SendMessage(handle, WmNcHitTest, 0, PackCoordinates(point.X, point.Y));
        Assert.NotEqual((nint)HtBottomRight, normalResult);
    }

    [Fact]
    public void InteractiveResizePresentsBeforeNativeSizeMoveEnds()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        RecordingGraphicsFactory factory = new();
        using WindowApplicationRuntime runtime = new(new Win32WindowPlatform(factory));
        Window source = new() { Title = "Live resize", Width = 320, Height = 200 };
        runtime.StartMainWindow(source);
        runtime.PumpOnce(TimeSpan.FromMilliseconds(16));
        RecordingGraphicsSession session = Assert.Single(factory.Sessions);
        int presentedBeforeResize = session.PresentCount;
        Assert.Equal(0, GetClassLongPtr(factory.WindowHandle, GclpBackground));

        SendMessage(factory.WindowHandle, WmEnterSizeMove, 0, 0);
        source.Width = 420;

        Assert.True(session.PresentCount > presentedBeforeResize);
        SendMessage(factory.WindowHandle, WmExitSizeMove, 0, 0);
    }

    [Fact]
    public void InteractiveMovePresentsBeforeNativeSizeMoveEnds()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        RecordingGraphicsFactory factory = new();
        using WindowApplicationRuntime runtime = new(new Win32WindowPlatform(factory));
        Window source = new()
        {
            Title = "Live move",
            Width = 320,
            Height = 200,
            Left = 50,
            Top = 50
        };
        runtime.StartMainWindow(source);
        runtime.PumpOnce(TimeSpan.FromMilliseconds(16));
        RecordingGraphicsSession session = Assert.Single(factory.Sessions);
        int presentedBeforeMove = session.PresentCount;

        SendMessage(factory.WindowHandle, WmEnterSizeMove, 0, 0);
        source.Left = 120;

        Assert.True(session.PresentCount > presentedBeforeMove);
        SendMessage(factory.WindowHandle, WmExitSizeMove, 0, 0);
    }

    private sealed class CallbackSink : IWindowPlatformCallbacks
    {
        public int RenderRequestCount { get; private set; }

        public void RequestClose() { }

        public void ActivationChanged(bool active) { }

        public void BoundsChanged(UiViewport viewport, float left, float top, WindowState state) { }

        public void RenderRequested()
        {
            RenderRequestCount++;
        }
    }

    private sealed class RecordingGraphicsFactory : IWindowGraphicsSessionFactory
    {
        public List<RecordingGraphicsSession> Sessions { get; } = [];

        public nint WindowHandle { get; private set; }

        public int PixelWidth { get; private set; }

        public int PixelHeight { get; private set; }

        public float CoordinateScale { get; private set; }

        public IWindowGraphicsSession Create(
            IWindowSurface windowSurface,
            int pixelWidth,
            int pixelHeight,
            float coordinateScale)
        {
            WindowHandle = Assert.IsType<Win32WindowSurface>(windowSurface).Handle;
            PixelWidth = pixelWidth;
            PixelHeight = pixelHeight;
            CoordinateScale = coordinateScale;
            RecordingGraphicsSession session = new();
            Sessions.Add(session);
            return session;
        }
    }

    private sealed class RecordingGraphicsSession : IWindowGraphicsSession
    {
        private bool disposed;

        public IDrawingBackend DrawingBackend { get; } = new RecordingDrawingBackend();

        public IImageLoader? ImageLoader => null;

        public ImageResourceCache? ImageResourceCache => null;

        public int PixelWidth { get; private set; }

        public int PixelHeight { get; private set; }

        public int ResizeCount { get; private set; }

        public int DisposeCount { get; private set; }

        public int PresentCount { get; private set; }

        public void Resize(int pixelWidth, int pixelHeight, float coordinateScale)
        {
            PixelWidth = pixelWidth;
            PixelHeight = pixelHeight;
            ResizeCount++;
        }

        public void BeginFrame(Color clearColor) { }

        public void Present()
        {
            PresentCount++;
        }

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            DisposeCount++;
            disposed = true;
        }
    }

    private sealed class RecordingDrawingBackend : IDrawingBackend
    {
        public void Render(DrawCommandList commands, in DrawingFrameContext frameContext) { }
    }

    private sealed class ForeignWindowSurface : IWindowSurface
    {
    }

    private static Win32WindowPlatform CreateWindowsDxPlatform() =>
        new(new WindowsDxWindowGraphicsSessionFactory());

    private static nint NativeHandle(IPlatformWindow window) =>
        Assert.IsType<Win32WindowSurface>(window.Surface).Handle;

    private static void Render(IDrawingBackend backend, DrawCommandList commands)
    {
        PrismFrameAnalysis analysis = new PrismFrameAnalyzer().Analyze(commands);
        DrawingFrameContext frameContext = new(analysis);
        backend.Render(commands, in frameContext);
    }

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(nint window, out uint processId);

    [DllImport("user32.dll")]
    private static extern nint GetWindow(nint window, uint command);

    [DllImport("user32.dll")]
    private static extern nint GetCursor();

    [DllImport("user32.dll", EntryPoint = "GetClassLongPtrW")]
    private static extern nint GetClassLongPtr(nint window, int index);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool IsWindow(nint window);

    [DllImport("user32.dll", EntryPoint = "FindWindowW", CharSet = CharSet.Unicode)]
    private static extern nint FindWindow(string? className, string windowName);

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

    [DllImport("user32.dll", EntryPoint = "SendMessageW")]
    private static extern nint SendMessage(nint window, uint message, nuint wParam, nint lParam);

    private static nint PackCoordinates(int x, int y)
    {
        return (nint)((y << 16) | (x & 0xFFFF));
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeRect
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativePoint(int x, int y)
    {
        public int X = x;
        public int Y = y;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeMonitorInfo
    {
        public uint Size;
        public NativeRect Monitor;
        public NativeRect WorkArea;
        public uint Flags;
    }
}
