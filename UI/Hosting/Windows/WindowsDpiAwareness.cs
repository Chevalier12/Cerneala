using System.ComponentModel;
using System.Runtime.InteropServices;

namespace Cerneala.UI.Hosting.Windows;

internal static class WindowsDpiAwareness
{
    private static readonly object Gate = new();
    private static bool initialized;

    public static void EnsurePerMonitorV2()
    {
        if (initialized)
        {
            return;
        }

        lock (Gate)
        {
            if (initialized)
            {
                return;
            }

            if (!Win32.SetProcessDpiAwarenessContext(Win32.DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2))
            {
                int error = Marshal.GetLastWin32Error();
                nint current = Win32.GetThreadDpiAwarenessContext();
                if (!Win32.AreDpiAwarenessContextsEqual(current, Win32.DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2))
                {
                    throw new Win32Exception(
                        error,
                        "Cerneala requires Per-Monitor V2 DPI awareness before creating the first native Window.");
                }
            }

            initialized = true;
        }
    }
}
