using System.Collections.Concurrent;
using System.ComponentModel;
using System.Runtime.InteropServices;
using Cerneala.UI.Controls;
using Cerneala.UI.Hosting;
using Cerneala.UI.Input;

namespace Cerneala.UI.Hosting.Windows;

internal sealed class Win32WindowPlatform : IWindowPlatform
{
    private const string WindowClassName = "Cerneala.NativeWindow";
    private static readonly object RegistrationGate = new();
    private static readonly ConcurrentDictionary<nint, Win32PlatformWindow> Windows = [];
    private static readonly Win32.WndProc WindowProcedure = WndProc;
    private static ushort classAtom;
    private readonly IWindowGraphicsSessionFactory graphicsSessionFactory;
    private bool disposed;

    public Win32WindowPlatform()
        : this(new WindowsDxWindowGraphicsSessionFactory())
    {
    }

    internal Win32WindowPlatform(IWindowGraphicsSessionFactory graphicsSessionFactory)
    {
        if (!OperatingSystem.IsWindows())
        {
            throw new PlatformNotSupportedException("Win32WindowPlatform is available only on Windows.");
        }

        WindowsDpiAwareness.EnsurePerMonitorV2();
        EnsureWindowClass();
        this.graphicsSessionFactory = graphicsSessionFactory ?? throw new ArgumentNullException(nameof(graphicsSessionFactory));
    }

    public IPlatformWindow CreateWindow(Window window, IWindowPlatformCallbacks callbacks)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        ArgumentNullException.ThrowIfNull(window);
        ArgumentNullException.ThrowIfNull(callbacks);

        Win32PlatformWindow created = new(window, callbacks, graphicsSessionFactory);
        if (!Windows.TryAdd(created.Handle, created))
        {
            created.Dispose();
            throw new InvalidOperationException("A native window with the same handle is already registered.");
        }
        return created;
    }

    public void PumpEvents()
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        while (Win32.PeekMessage(out Win32.MSG message, 0, 0, 0, Win32.PM_REMOVE))
        {
            Win32.TranslateMessage(in message);
            Win32.DispatchMessage(in message);
        }
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
    }

    private static nint WndProc(nint hwnd, uint message, nuint wParam, nint lParam)
    {
        if (Windows.TryGetValue(hwnd, out Win32PlatformWindow? window))
        {
            return window.ProcessMessage(message, wParam, lParam);
        }

        return Win32.DefWindowProc(hwnd, message, wParam, lParam);
    }

    private static void RemoveWindow(nint handle)
    {
        Windows.TryRemove(handle, out _);
    }

    private static void EnsureWindowClass()
    {
        lock (RegistrationGate)
        {
            if (classAtom != 0)
            {
                return;
            }

            nint instance = Win32.GetModuleHandle(null);
            nint applicationIcon = Win32.LoadIcon(instance, Win32.IDI_APPLICATION);
            if (applicationIcon == 0)
            {
                applicationIcon = Win32.LoadIcon(0, Win32.IDI_APPLICATION);
            }

            Win32.WNDCLASSEX windowClass = new()
            {
                cbSize = (uint)Marshal.SizeOf<Win32.WNDCLASSEX>(),
                style = Win32.CS_HREDRAW | Win32.CS_VREDRAW | Win32.CS_OWNDC,
                lpfnWndProc = Marshal.GetFunctionPointerForDelegate(WindowProcedure),
                hInstance = instance,
                hIcon = applicationIcon,
                hCursor = Win32.LoadCursor(0, Win32.IDC_ARROW),
                hbrBackground = 0,
                lpszClassName = WindowClassName,
                hIconSm = applicationIcon
            };
            classAtom = Win32.RegisterClassEx(in windowClass);
            if (classAtom == 0)
            {
                throw new Win32Exception(Marshal.GetLastWin32Error(), "Could not register the Cerneala native window class.");
            }
        }
    }

    private sealed class Win32PlatformWindow : IPlatformWindow
    {
        private readonly IWindowPlatformCallbacks callbacks;
        private readonly Window window;
        private readonly Win32InputSource inputSource = new();
        private readonly IWindowGraphicsSession graphicsSession;
        private bool destroyed;
        private bool visible;
        private bool initialPlacementApplied;
        private uint style;
        private uint extendedStyle;
        private UiViewport viewport;
        private WindowState desiredState;
        private int graphicsPixelWidth;
        private int graphicsPixelHeight;
        private float graphicsScale;
        private bool applyingMaximizeCommand;
        private bool isInSizeMove;

        public Win32PlatformWindow(
            Window window,
            IWindowPlatformCallbacks callbacks,
            IWindowGraphicsSessionFactory graphicsSessionFactory)
        {
            this.window = window;
            this.callbacks = callbacks;
            desiredState = window.WindowState;
            style = StyleFor(window.ResizeMode);
            extendedStyle = ExtendedStyleFor(window);
            float scale = 1;
            int pixelWidth = Math.Max(1, (int)MathF.Ceiling(window.Width * scale));
            int pixelHeight = Math.Max(1, (int)MathF.Ceiling(window.Height * scale));
            Win32.RECT rect = new(0, 0, pixelWidth, pixelHeight);
            Win32.AdjustWindowRectEx(ref rect, style, false, extendedStyle);
            Handle = Win32.CreateWindowEx(
                extendedStyle,
                WindowClassName,
                window.Title,
                style,
                Win32.CW_USEDEFAULT,
                Win32.CW_USEDEFAULT,
                rect.Width,
                rect.Height,
                0,
                0,
                Win32.GetModuleHandle(null),
                0);
            if (Handle == 0)
            {
                throw new Win32Exception(Marshal.GetLastWin32Error(), "Could not create a native Cerneala window.");
            }

            scale = Math.Max(1, Win32.GetDpiForWindow(Handle)) / 96f;
            inputSource.CoordinateScale = scale;
            Win32.GetClientRect(Handle, out Win32.RECT clientRect);
            viewport = UiViewport.FromPhysicalPixels(clientRect.Width, clientRect.Height, scale);
            try
            {
                graphicsSession = graphicsSessionFactory.Create(
                    Handle,
                    Math.Max(1, clientRect.Width),
                    Math.Max(1, clientRect.Height),
                    scale);
                graphicsPixelWidth = Math.Max(1, clientRect.Width);
                graphicsPixelHeight = Math.Max(1, clientRect.Height);
                graphicsScale = scale;
            }
            catch
            {
                Win32.DestroyWindow(Handle);
                throw;
            }
        }

        public nint Handle { get; }

        public UiViewport Viewport => viewport;

        public IInputSource InputSource => inputSource;

        public IWindowGraphicsSession GraphicsSession => graphicsSession;

        public void ApplyProperties(Window window)
        {
            if (destroyed)
            {
                return;
            }

            Win32.SetWindowText(Handle, window.Title);
            WindowState requestedState = window.WindowState;
            desiredState = requestedState;
            uint nextStyle = StyleFor(window.ResizeMode);
            uint nextExtendedStyle = ExtendedStyleFor(window);
            if (style != nextStyle)
            {
                style = nextStyle;
                Win32.SetWindowLongPtr(Handle, Win32.GWL_STYLE, (nint)style);
            }

            if (extendedStyle != nextExtendedStyle)
            {
                extendedStyle = nextExtendedStyle;
                Win32.SetWindowLongPtr(Handle, Win32.GWL_EXSTYLE, (nint)extendedStyle);
            }

            float scale = inputSource.CoordinateScale;
            int clientWidth = Math.Max(1, (int)MathF.Ceiling(window.Width * scale));
            int clientHeight = Math.Max(1, (int)MathF.Ceiling(window.Height * scale));
            Win32.RECT outer = new(0, 0, clientWidth, clientHeight);
            Win32.AdjustWindowRectExForDpi(ref outer, style, false, extendedStyle, (uint)MathF.Round(scale * 96));
            (int x, int y) = ResolvePosition(window, outer.Width, outer.Height, scale);
            nint insertAfter = window.Topmost ? Win32.HWND_TOPMOST : Win32.HWND_NOTOPMOST;
            Win32.SetWindowPos(
                Handle,
                insertAfter,
                x,
                y,
                outer.Width,
                outer.Height,
                Win32.SWP_NOACTIVATE | Win32.SWP_FRAMECHANGED);
            initialPlacementApplied = true;
            if (visible)
            {
                Win32.ShowWindow(Handle, ShowCommand(requestedState));
            }
        }

        public void SetOwner(IPlatformWindow? owner)
        {
            Win32.SetWindowLongPtr(Handle, Win32.GWLP_HWNDPARENT, owner?.Handle ?? 0);
        }

        public void SetEnabled(bool enabled)
        {
            Win32.EnableWindow(Handle, enabled);
        }

        public void Show()
        {
            visible = true;
            Win32.ShowWindow(Handle, ShowCommand(desiredState));
            Win32.UpdateWindow(Handle);
        }

        public void Hide()
        {
            visible = false;
            Win32.ShowWindow(Handle, Win32.SW_HIDE);
        }

        public void Activate()
        {
            Win32.SetForegroundWindow(Handle);
        }

        public void Destroy()
        {
            if (destroyed)
            {
                return;
            }

            destroyed = true;
            RemoveWindow(Handle);
            Win32.DestroyWindow(Handle);
        }

        public void Dispose()
        {
            Destroy();
            graphicsSession.Dispose();
        }

        public nint ProcessMessage(uint message, nuint wParam, nint lParam)
        {
            switch (message)
            {
                case Win32.WM_CLOSE:
                    callbacks.RequestClose();
                    return 0;
                case Win32.WM_ACTIVATE:
                    callbacks.ActivationChanged((wParam & 0xFFFF) != Win32.WA_INACTIVE);
                    return 0;
                case Win32.WM_MOVE:
                    ReportBounds();
                    if (isInSizeMove)
                    {
                        callbacks.RenderImmediately();
                    }

                    return 0;
                case Win32.WM_SIZE:
                    ResizeSurface();
                    ReportBounds();
                    if (isInSizeMove)
                    {
                        callbacks.RenderImmediately();
                    }

                    return 0;
                case Win32.WM_ENTERSIZEMOVE:
                    isInSizeMove = true;
                    return 0;
                case Win32.WM_EXITSIZEMOVE:
                    isInSizeMove = false;
                    callbacks.RenderImmediately();
                    return 0;
                case Win32.WM_SYSCOMMAND when (wParam & Win32.SC_MASK) == Win32.SC_MAXIMIZE:
                    applyingMaximizeCommand = true;
                    try
                    {
                        return Win32.DefWindowProc(Handle, message, wParam, lParam);
                    }
                    finally
                    {
                        applyingMaximizeCommand = false;
                    }
                case Win32.WM_GETMINMAXINFO:
                    ApplyMinMaxInfo(lParam);
                    return 0;
                case Win32.WM_NCHITTEST when window.ResizeMode == ResizeMode.CanResizeWithGrip:
                    return HitTestResizeGrip(message, wParam, lParam);
                case Win32.WM_DPICHANGED:
                    ApplyDpiChange(wParam, lParam);
                    return 0;
                case Win32.WM_ERASEBKGND:
                    return 1;
                case Win32.WM_PAINT:
                    Paint();
                    return 0;
                case Win32.WM_MOUSEMOVE:
                    if (inputSource.MovePointer(SignedLowWord(lParam), SignedHighWord(lParam)))
                    {
                        callbacks.RenderRequested();
                    }

                    return 0;
                case Win32.WM_LBUTTONDOWN:
                    inputSource.SetButton(InputMouseButton.Left, true);
                    callbacks.RenderRequested();
                    return 0;
                case Win32.WM_LBUTTONUP:
                    inputSource.SetButton(InputMouseButton.Left, false);
                    callbacks.RenderRequested();
                    return 0;
                case Win32.WM_RBUTTONDOWN:
                    inputSource.SetButton(InputMouseButton.Right, true);
                    callbacks.RenderRequested();
                    return 0;
                case Win32.WM_RBUTTONUP:
                    inputSource.SetButton(InputMouseButton.Right, false);
                    callbacks.RenderRequested();
                    return 0;
                case Win32.WM_MBUTTONDOWN:
                    inputSource.SetButton(InputMouseButton.Middle, true);
                    callbacks.RenderRequested();
                    return 0;
                case Win32.WM_MBUTTONUP:
                    inputSource.SetButton(InputMouseButton.Middle, false);
                    callbacks.RenderRequested();
                    return 0;
                case Win32.WM_MOUSEWHEEL:
                    inputSource.AddWheelDelta(SignedHighWord((nint)wParam));
                    callbacks.RenderRequested();
                    return 0;
                case Win32.WM_KEYDOWN:
                case Win32.WM_SYSKEYDOWN:
                    inputSource.SetKey((uint)wParam, true);
                    callbacks.RenderRequested();
                    return 0;
                case Win32.WM_KEYUP:
                case Win32.WM_SYSKEYUP:
                    inputSource.SetKey((uint)wParam, false);
                    callbacks.RenderRequested();
                    return 0;
                case Win32.WM_CHAR:
                    inputSource.AddText((char)wParam);
                    callbacks.RenderRequested();
                    return 0;
            }

            return Win32.DefWindowProc(Handle, message, wParam, lParam);
        }

        private void ResizeSurface()
        {
            Win32.GetClientRect(Handle, out Win32.RECT rect);
            float scale = Math.Max(1, Win32.GetDpiForWindow(Handle)) / 96f;
            inputSource.CoordinateScale = scale;
            viewport = UiViewport.FromPhysicalPixels(rect.Width, rect.Height, scale);
            if (rect.Width > 0 &&
                rect.Height > 0 &&
                (rect.Width != graphicsPixelWidth || rect.Height != graphicsPixelHeight || scale != graphicsScale))
            {
                graphicsSession.Resize(rect.Width, rect.Height, scale);
                graphicsPixelWidth = rect.Width;
                graphicsPixelHeight = rect.Height;
                graphicsScale = scale;
            }

            callbacks.RenderRequested();
        }

        private void ReportBounds()
        {
            Win32.GetWindowRect(Handle, out Win32.RECT rect);
            float scale = inputSource.CoordinateScale;
            WindowState state = NativeState();
            float left = state == WindowState.Normal || !float.IsFinite(window.Left)
                ? rect.Left / scale
                : window.Left;
            float top = state == WindowState.Normal || !float.IsFinite(window.Top)
                ? rect.Top / scale
                : window.Top;
            callbacks.BoundsChanged(viewport, left, top, state);
        }

        private void ApplyDpiChange(nuint wParam, nint lParam)
        {
            uint dpi = (uint)(wParam & 0xFFFF);
            float scale = Math.Max(1, dpi) / 96f;
            inputSource.CoordinateScale = scale;
            Win32.RECT suggested = Marshal.PtrToStructure<Win32.RECT>(lParam);
            Win32.SetWindowPos(
                Handle,
                0,
                suggested.Left,
                suggested.Top,
                suggested.Width,
                suggested.Height,
                Win32.SWP_NOZORDER | Win32.SWP_NOACTIVATE);
            ResizeSurface();
        }

        private void ApplyMinMaxInfo(nint lParam)
        {
            Win32.MINMAXINFO info = Marshal.PtrToStructure<Win32.MINMAXINFO>(lParam);
            float scale = inputSource.CoordinateScale;
            (int minWidth, int minHeight) = OuterSize(window.MinWidth, window.MinHeight, scale);
            info.ptMinTrackSize = new Win32.POINT(minWidth, minHeight);
            if (float.IsFinite(window.MaxWidth) && float.IsFinite(window.MaxHeight))
            {
                (int maxWidth, int maxHeight) = OuterSize(window.MaxWidth, window.MaxHeight, scale);
                info.ptMaxTrackSize = new Win32.POINT(maxWidth, maxHeight);
            }
            else
            {
                int maxWidth = float.IsFinite(window.MaxWidth) ? OuterSize(window.MaxWidth, 1, scale).Width : info.ptMaxTrackSize.X;
                int maxHeight = float.IsFinite(window.MaxHeight) ? OuterSize(1, window.MaxHeight, scale).Height : info.ptMaxTrackSize.Y;
                info.ptMaxTrackSize = new Win32.POINT(maxWidth, maxHeight);
            }

            if (applyingMaximizeCommand || desiredState == WindowState.Maximized)
            {
                info.ptMaxTrackSize = new Win32.POINT(
                    Math.Max(info.ptMaxTrackSize.X, info.ptMaxSize.X),
                    Math.Max(info.ptMaxTrackSize.Y, info.ptMaxSize.Y));
            }

            Marshal.StructureToPtr(info, lParam, fDeleteOld: false);
        }

        private nint HitTestResizeGrip(uint message, nuint wParam, nint lParam)
        {
            nint nativeResult = Win32.DefWindowProc(Handle, message, wParam, lParam);
            if (nativeResult != Win32.HTCLIENT || NativeState() != WindowState.Normal)
            {
                return nativeResult;
            }

            Win32.POINT point = new(SignedLowWord(lParam), SignedHighWord(lParam));
            if (!Win32.ScreenToClient(Handle, ref point) || !Win32.GetClientRect(Handle, out Win32.RECT client))
            {
                return nativeResult;
            }

            int gripWidth = Win32.GetSystemMetrics(Win32.SM_CXVSCROLL);
            int gripHeight = Win32.GetSystemMetrics(Win32.SM_CYHSCROLL);
            return point.X >= client.Right - gripWidth && point.Y >= client.Bottom - gripHeight
                ? Win32.HTBOTTOMRIGHT
                : nativeResult;
        }

        private (int Width, int Height) OuterSize(float logicalWidth, float logicalHeight, float scale)
        {
            Win32.RECT rect = new(
                0,
                0,
                Math.Max(1, (int)MathF.Ceiling(logicalWidth * scale)),
                Math.Max(1, (int)MathF.Ceiling(logicalHeight * scale)));
            Win32.AdjustWindowRectExForDpi(ref rect, style, false, extendedStyle, (uint)MathF.Round(scale * 96));
            return (rect.Width, rect.Height);
        }

        private void Paint()
        {
            Win32.BeginPaint(Handle, out Win32.PAINTSTRUCT paint);
            try
            {
            }
            finally
            {
                Win32.EndPaint(Handle, in paint);
            }

            callbacks.RenderRequested();
        }

        private (int X, int Y) ResolvePosition(Window window, int outerWidth, int outerHeight, float scale)
        {
            if (float.IsFinite(window.Left) && float.IsFinite(window.Top))
            {
                return ((int)MathF.Round(window.Left * scale), (int)MathF.Round(window.Top * scale));
            }

            if (initialPlacementApplied)
            {
                Win32.GetWindowRect(Handle, out Win32.RECT current);
                return (current.Left, current.Top);
            }

            if (window.WindowStartupLocation == WindowStartupLocation.CenterOwner && window.Owner is not null)
            {
                nint ownerHandle = Win32.GetWindowLongPtr(Handle, Win32.GWLP_HWNDPARENT);
                if (ownerHandle != 0 && Win32.GetWindowRect(ownerHandle, out Win32.RECT ownerRect))
                {
                    return (
                        ownerRect.Left + ((ownerRect.Width - outerWidth) / 2),
                        ownerRect.Top + ((ownerRect.Height - outerHeight) / 2));
                }
            }

            if (window.WindowStartupLocation is WindowStartupLocation.CenterScreen or WindowStartupLocation.CenterOwner)
            {
                return (
                    (Win32.GetSystemMetrics(Win32.SM_CXSCREEN) - outerWidth) / 2,
                    (Win32.GetSystemMetrics(Win32.SM_CYSCREEN) - outerHeight) / 2);
            }

            return (Win32.CW_USEDEFAULT, Win32.CW_USEDEFAULT);
        }

        private WindowState NativeState()
        {
            if (Win32.IsIconic(Handle))
            {
                return WindowState.Minimized;
            }

            return Win32.IsZoomed(Handle) ? WindowState.Maximized : WindowState.Normal;
        }

        private static uint StyleFor(ResizeMode resizeMode)
        {
            uint baseStyle = Win32.WS_OVERLAPPED | Win32.WS_CAPTION | Win32.WS_SYSMENU;
            return resizeMode switch
            {
                ResizeMode.NoResize => baseStyle,
                ResizeMode.CanMinimize => baseStyle | Win32.WS_MINIMIZEBOX,
                _ => baseStyle | Win32.WS_MINIMIZEBOX | Win32.WS_MAXIMIZEBOX | Win32.WS_THICKFRAME
            };
        }

        private static uint ExtendedStyleFor(Window window)
        {
            return window.ShowInTaskbar ? Win32.WS_EX_APPWINDOW : Win32.WS_EX_TOOLWINDOW;
        }

        private int ShowCommand(WindowState state)
        {
            if (state == WindowState.Normal && (Win32.IsIconic(Handle) || Win32.IsZoomed(Handle)))
            {
                return Win32.SW_RESTORE;
            }

            return state switch
            {
                WindowState.Minimized => Win32.SW_SHOWMINIMIZED,
                WindowState.Maximized => Win32.SW_SHOWMAXIMIZED,
                _ => Win32.SW_SHOW
            };
        }

        private static int SignedLowWord(nint value) => unchecked((short)((long)value & 0xFFFF));

        private static int SignedHighWord(nint value) => unchecked((short)(((long)value >> 16) & 0xFFFF));
    }
}
