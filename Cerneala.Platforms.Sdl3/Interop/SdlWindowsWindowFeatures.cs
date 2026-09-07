using System.ComponentModel;
using System.Runtime.InteropServices;
using SDL3;

namespace Cerneala.Platforms.Sdl3;

// Windows-only presentation details of SDL-owned windows. SDL still owns the
// window, message loop, resize operation, and destruction.
internal static class SdlWindowsWindowFeatures
{
    // A native callback must remain rooted for every window using it.
    private static readonly SDL.HitTest resizeGripHitTest = HitTestResizeGrip;

    public static bool SetResizeGrip(nint window, bool enabled) =>
        SDL.SetWindowHitTest(window, enabled ? resizeGripHitTest : null!, 0);

    public static void EnsureDefaultIcon(nint window)
    {
        nint handle = SDL.GetPointerProperty(SDL.GetWindowProperties(window), "SDL.window.win32.hwnd", 0);
        if (handle == 0)
        {
            throw new InvalidOperationException("SDL did not expose its Windows window handle.");
        }

        // Preserve SDL's executable/resource icon. Only supply the historical
        // system default when neither the window nor its class has an icon.
        nint icon = SendMessage(handle, 0x007F, 1, 0); // WM_GETICON / ICON_BIG
        if (icon != 0 || GetClassIcon(handle) != 0)
        {
            return;
        }

        icon = LoadIcon(0, 32512); // IDI_APPLICATION: shared, not owned by us.
        if (icon == 0)
        {
            throw new Win32Exception(Marshal.GetLastWin32Error());
        }

        SendMessage(handle, 0x0080, 1, icon); // WM_SETICON / ICON_BIG
        SendMessage(handle, 0x0080, 0, icon); // WM_SETICON / ICON_SMALL
    }

    private static SDL.HitTestResult HitTestResizeGrip(nint window, in SDL.Point point, nint data)
    {
        SDL.WindowFlags flags = SDL.GetWindowFlags(window);
        if ((flags & SDL.WindowFlags.Resizable) == 0 ||
            (flags & (SDL.WindowFlags.Minimized | SDL.WindowFlags.Maximized)) != 0 ||
            !SDL.GetWindowSize(window, out int width, out int height))
        {
            return SDL.HitTestResult.Normal;
        }

        // Match the Windows system-sized client corner, in native coordinates.
        return point.X >= Math.Max(0, width - GetSystemMetrics(2)) && point.X < width &&
            point.Y >= Math.Max(0, height - GetSystemMetrics(20)) && point.Y < height
            ? SDL.HitTestResult.ResizeBottomRight
            : SDL.HitTestResult.Normal;
    }

    private static nint GetClassIcon(nint window) => IntPtr.Size == 8
        ? GetClassLongPtr(window, -14) // GCLP_HICON
        : (nint)GetClassLong(window, -14);

    [DllImport("user32.dll", EntryPoint = "SendMessageW")]
    private static extern nint SendMessage(nint window, uint message, nuint wParam, nint lParam);

    [DllImport("user32.dll", EntryPoint = "LoadIconW", SetLastError = true)]
    private static extern nint LoadIcon(nint instance, nint resource);

    [DllImport("user32.dll", EntryPoint = "GetClassLongPtrW")]
    private static extern nint GetClassLongPtr(nint window, int index);

    [DllImport("user32.dll", EntryPoint = "GetClassLongW")]
    private static extern uint GetClassLong(nint window, int index);

    [DllImport("user32.dll")]
    private static extern int GetSystemMetrics(int index);
}
