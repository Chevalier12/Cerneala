namespace Cerneala.UI.Hosting.Windows;

internal static class Win32ApplicationPlatform
{
    public static void EnsureRegistered()
    {
        WindowsGpuPreference.TryRequestHighPerformance();
        WindowPlatformBackendRegistry.Register(Win32PlatformBackend.Instance);
    }

    private sealed class Win32PlatformBackend : IWindowPlatformBackend
    {
        public static Win32PlatformBackend Instance { get; } = new();

        public IWindowPlatform CreatePlatform(
            IWindowGraphicsSessionFactory graphicsSessionFactory,
            float? coordinateScaleOverride)
        {
            return new Win32WindowPlatform(graphicsSessionFactory, coordinateScaleOverride);
        }
    }
}
