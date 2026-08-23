using Microsoft.Win32;
using Cerneala.UI.Hosting.Windows;

namespace Cerneala.Tests.UI.Hosting;

public sealed class WindowsGpuPreferenceTests
{
    [Fact]
    public void RequestHighPerformanceCreatesAFrameworkDefaultForAnUnconfiguredExecutable()
    {
        string executablePath = $@"C:\CernealaTests\{Guid.NewGuid():N}.exe";

        try
        {
            DeletePreference(executablePath);

            bool created = WindowsGpuPreference.TryRequestHighPerformance(executablePath);

            Assert.True(created);
            Assert.Equal("GpuPreference=2;", ReadPreference(executablePath));
        }
        finally
        {
            DeletePreference(executablePath);
        }
    }

    [Fact]
    public void RequestHighPerformancePreservesAnExplicitUserPreference()
    {
        string executablePath = $@"C:\CernealaTests\{Guid.NewGuid():N}.exe";

        try
        {
            WritePreference(executablePath, "GpuPreference=1;");

            bool created = WindowsGpuPreference.TryRequestHighPerformance(executablePath);

            Assert.False(created);
            Assert.Equal("GpuPreference=1;", ReadPreference(executablePath));
        }
        finally
        {
            DeletePreference(executablePath);
        }
    }

    private static object? ReadPreference(string executablePath)
    {
        using RegistryKey? key = Registry.CurrentUser.OpenSubKey(WindowsGpuPreference.RegistryPath);
        return key?.GetValue(executablePath);
    }

    private static void WritePreference(string executablePath, string value)
    {
        using RegistryKey key = Registry.CurrentUser.CreateSubKey(
            WindowsGpuPreference.RegistryPath,
            writable: true);
        key.SetValue(executablePath, value, RegistryValueKind.String);
    }

    private static void DeletePreference(string executablePath)
    {
        using RegistryKey? key = Registry.CurrentUser.OpenSubKey(
            WindowsGpuPreference.RegistryPath,
            writable: true);
        key?.DeleteValue(executablePath, throwOnMissingValue: false);
    }
}
