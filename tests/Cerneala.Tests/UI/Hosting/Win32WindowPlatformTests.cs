using System.Diagnostics;
using System.Runtime.InteropServices;
using Cerneala.UI.Controls;
using Cerneala.UI.Hosting;
using Cerneala.UI.Hosting.Windows;
using Cerneala.Drawing;
using Cerneala.Drawing.MonoGame;
using Cerneala.UI.Input;
using Cerneala.UI.Resources;

namespace Cerneala.Tests.UI.Hosting;

public sealed class Win32WindowPlatformTests
{
    private const uint WmNcHitTest = 0x0084;
    private const int HtBottomRight = 17;

    [Fact]
    public void NativeMaximizeCoversTheMonitorWorkArea()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        using WindowApplicationRuntime runtime = new(new Win32WindowPlatform());
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

        using WindowApplicationRuntime runtime = new(new Win32WindowPlatform());
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

        Assert.Equal(window.Handle, factory.WindowHandle);
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
    public void NativeWindowsBelongToCurrentProcessAndUseNativeOwnership()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        using Win32WindowPlatform platform = new();
        CallbackSink callbacks = new();
        using IPlatformWindow owner = platform.CreateWindow(new Window { Title = "Owner" }, callbacks);
        using IPlatformWindow child = platform.CreateWindow(new Window { Title = "Child" }, callbacks);

        child.SetOwner(owner);
        GetWindowThreadProcessId(owner.Handle, out uint ownerProcessId);
        GetWindowThreadProcessId(child.Handle, out uint childProcessId);

        Assert.Equal((uint)Process.GetCurrentProcess().Id, ownerProcessId);
        Assert.Equal(ownerProcessId, childProcessId);
        Assert.Equal(owner.Handle, GetWindow(child.Handle, 4));
        Assert.True(IsWindow(owner.Handle));
        Assert.True(IsWindow(child.Handle));

        DrawCommandList commands = new();
        commands.Add(DrawCommand.FillRectangle(new DrawRect(0, 0, 32, 32), new Color(20, 40, 60)));
        owner.GraphicsSession.BeginFrame(Color.White);
        owner.GraphicsSession.DrawingBackend.Render(commands);
        owner.Show();
        owner.GraphicsSession.Present();
        platform.PumpEvents();

        Assert.IsType<MonoGameDrawingBackend>(owner.GraphicsSession.DrawingBackend);
        Assert.NotSame(owner.GraphicsSession, child.GraphicsSession);
        Assert.NotSame(
            Assert.IsType<WindowsDxWindowGraphicsSession>(owner.GraphicsSession).GraphicsDevice,
            Assert.IsType<WindowsDxWindowGraphicsSession>(child.GraphicsSession).GraphicsDevice);

        SendMessage(owner.Handle, Win32.WM_MOUSEMOVE, 0, PackCoordinates(45, 35));
        SendMessage(owner.Handle, Win32.WM_KEYDOWN, 0x41, 0);
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
        Assert.False(IsWindow(child.Handle));
        Assert.False(IsWindow(owner.Handle));
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
        Assert.True(GetClientRect(window.Handle, out NativeRect client));
        NativePoint point = new(client.Right - 1, client.Bottom - 1);
        Assert.True(ClientToScreen(window.Handle, ref point));

        nint gripResult = SendMessage(window.Handle, WmNcHitTest, 0, PackCoordinates(point.X, point.Y));
        Assert.Equal((nint)HtBottomRight, gripResult);

        source.ResizeMode = ResizeMode.CanResize;
        window.ApplyProperties(source);
        nint normalResult = SendMessage(window.Handle, WmNcHitTest, 0, PackCoordinates(point.X, point.Y));
        Assert.NotEqual((nint)HtBottomRight, normalResult);
    }

    private sealed class CallbackSink : IWindowPlatformCallbacks
    {
        public void RequestClose() { }

        public void ActivationChanged(bool active) { }

        public void BoundsChanged(UiViewport viewport, float left, float top, WindowState state) { }

        public void RenderRequested() { }
    }

    private sealed class RecordingGraphicsFactory : IWindowGraphicsSessionFactory
    {
        public List<RecordingGraphicsSession> Sessions { get; } = [];

        public nint WindowHandle { get; private set; }

        public int PixelWidth { get; private set; }

        public int PixelHeight { get; private set; }

        public float CoordinateScale { get; private set; }

        public IWindowGraphicsSession Create(nint windowHandle, int pixelWidth, int pixelHeight, float coordinateScale)
        {
            WindowHandle = windowHandle;
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

        public void Resize(int pixelWidth, int pixelHeight, float coordinateScale)
        {
            PixelWidth = pixelWidth;
            PixelHeight = pixelHeight;
            ResizeCount++;
        }

        public void BeginFrame(Color clearColor) { }

        public void Present() { }

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
        public void Render(DrawCommandList commands) { }
    }

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(nint window, out uint processId);

    [DllImport("user32.dll")]
    private static extern nint GetWindow(nint window, uint command);

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
