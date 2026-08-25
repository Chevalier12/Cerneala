using System.Security;
using Microsoft.Win32;

namespace Cerneala.UI.Hosting.Windows;

internal static class WindowsGpuPreference
{
    internal const string RegistryPath = @"Software\Microsoft\DirectX\UserGpuPreferences";
    private const string HighPerformancePreference = "GpuPreference=2;";

    internal static bool TryRequestHighPerformance()
    {
        return TryRequestHighPerformance(Environment.ProcessPath);
    }

    internal static bool TryRequestHighPerformance(string? executablePath)
    {
        if (!OperatingSystem.IsWindows() || string.IsNullOrWhiteSpace(executablePath))
        {
            return false;
        }

        try
        {
            using RegistryKey? key = Registry.CurrentUser.CreateSubKey(RegistryPath, writable: true);
            if (key is null || key.GetValue(executablePath) is not null)
            {
                return false;
            }

            key.SetValue(executablePath, HighPerformancePreference, RegistryValueKind.String);
            return true;
        }
        catch (Exception exception) when (
            exception is IOException or SecurityException or UnauthorizedAccessException)
        {
            return false;
        }
    }
}
