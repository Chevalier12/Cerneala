using System.ComponentModel;
using System.Runtime.InteropServices;
using Cerneala.Drawing;
using Cerneala.Drawing.Skia;
using Cerneala.UI.Controls;
using Cerneala.UI.Hosting;
using Cerneala.UI.Input;
using Cerneala.UI.Resources;

namespace Cerneala.UI.Hosting.Windows;

internal sealed class Win32WindowPlatform : IWindowPlatform
{
    private const string WindowClassName = "Cerneala.NativeWindow";
    private static readonly object RegistrationGate = new();
    private static readonly Dictionary<nint, Win32PlatformWindow> Windows = [];
    private static readonly Win32.WndProc WindowProcedure = WndProc;
    private static ushort classAtom;
    private readonly SkiaImageLoader imageLoader = new();
    private readonly ImageResourceCache imageResourceCache;
    private bool disposed;

    public Win32WindowPlatform()
    {
        if (!OperatingSystem.IsWindows())
        {
            throw new PlatformNotSupportedException("Win32WindowPlatform is available only on Windows.");
        }

        EnsureWindowClass();
        imageResourceCache = new ImageResourceCache(imageLoader);
    }

    public IImageLoader ImageLoader => imageLoader;

    public ImageResourceCache ImageResourceCache => imageResourceCache;

    public IPlatformWindow CreateWindow(Window window, IWindowPlatformCallbacks callbacks)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        ArgumentNullException.ThrowIfNull(window);
        ArgumentNullException.ThrowIfNull(callbacks);

        Win32PlatformWindow created = new(window, callbacks);
        Windows.Add(created.Handle, created);
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

        imageResourceCache.Dispose();
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
        Windows.Remove(handle);
    }

    private static void EnsureWindowClass()
    {
        lock (RegistrationGate)
        {
            if (classAtom != 0)
            {
                return;
            }

            Win32.WNDCLASSEX windowClass = new()
            {
                cbSize = (uint)Marshal.SizeOf<Win32.WNDCLASSEX>(),
                style = Win32.CS_HREDRAW | Win32.CS_VREDRAW | Win32.CS_OWNDC,
                lpfnWndProc = Marshal.GetFunctionPointerForDelegate(WindowProcedure),
                hInstance = Win32.GetModuleHandle(null),
                hCursor = Win32.LoadCursor(0, Win32.IDC_ARROW),
                hbrBackground = (nint)(Win32.COLOR_WINDOW + 1),
                lpszClassName = WindowClassName
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
        private readonly SkiaDrawingBackend drawingBackend;
        private bool destroyed;
        private bool visible;
        private bool initialPlacementApplied;
        private uint style;
        private uint extendedStyle;
        private UiViewport viewport;
        private WindowState desiredState;

        public Win32PlatformWindow(Window window, IWindowPlatformCallbacks callbacks)
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
            drawingBackend = new SkiaDrawingBackend(clientRect.Width, clientRect.Height, scale);
        }

        public nint Handle { get; }

        public UiViewport Viewport => viewport;

        public IInputSource InputSource => inputSource;

        public IDrawingBackend DrawingBackend => drawingBackend;

        public void ApplyProperties(Window window)
        {
            if (destroyed)
            {
                return;
            }

            Win32.SetWindowText(Handle, window.Title);
            desiredState = window.WindowState;
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
                Win32.ShowWindow(Handle, ShowCommand(window.WindowState));
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

        public void Present()
        {
            if (destroyed)
            {
                return;
            }

            nint deviceContext = Win32.GetDC(Handle);
            try
            {
                Present(deviceContext);
            }
            finally
            {
                Win32.ReleaseDC(Handle, deviceContext);
            }
        }

        public void Dispose()
        {
            Destroy();
            drawingBackend.Dispose();
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
                    return 0;
                case Win32.WM_SIZE:
                    ResizeSurface();
                    ReportBounds();
                    return 0;
                case Win32.WM_GETMINMAXINFO:
                    ApplyMinMaxInfo(lParam);
                    return 0;
                case Win32.WM_DPICHANGED:
                    ApplyDpiChange(wParam, lParam);
                    return 0;
                case Win32.WM_PAINT:
                    Paint();
                    return 0;
                case Win32.WM_MOUSEMOVE:
                    inputSource.MovePointer(SignedLowWord(lParam), SignedHighWord(lParam));
                    callbacks.RenderRequested();
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
            drawingBackend.Resize(rect.Width, rect.Height, scale);
            callbacks.RenderRequested();
        }

        private void ReportBounds()
        {
            Win32.GetWindowRect(Handle, out Win32.RECT rect);
            float scale = inputSource.CoordinateScale;
            callbacks.BoundsChanged(viewport, rect.Left / scale, rect.Top / scale, NativeState());
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

            Marshal.StructureToPtr(info, lParam, fDeleteOld: false);
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
                Present(paint.hdc);
            }
            finally
            {
                Win32.EndPaint(Handle, in paint);
            }

            callbacks.RenderRequested();
        }

        private void Present(nint deviceContext)
        {
            Win32.BITMAPINFO info = Win32.BITMAPINFO.ForTopDownBgra(drawingBackend.PixelWidth, drawingBackend.PixelHeight);
            Win32.StretchDIBits(
                deviceContext,
                0,
                0,
                drawingBackend.PixelWidth,
                drawingBackend.PixelHeight,
                0,
                0,
                drawingBackend.PixelWidth,
                drawingBackend.PixelHeight,
                drawingBackend.Pixels,
                in info,
                Win32.DIB_RGB_COLORS,
                Win32.SRCCOPY);
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

        private static int ShowCommand(WindowState state)
        {
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
