using Cerneala.UI.Hosting.Windowing;

namespace Cerneala.UI.Hosting.Windows;

/// <summary>
/// Registers the WindowsDX graphics backend used by generated Cerneala applications.
/// </summary>
public static class WindowsDxApplicationBackend
{
    /// <summary>
    /// Ensures that the WindowsDX graphics backend is available to the window host.
    /// </summary>
    public static void EnsureRegistered()
    {
        WindowsGpuPreference.TryRequestHighPerformance();
        WindowingBackendRegistry.Register(WindowsDxWindowingBackend.Instance);
    }

    private sealed class WindowsDxWindowingBackend : IWindowingBackend
    {
        public static WindowsDxWindowingBackend Instance { get; } = new();

        public IWindowPlatform CreatePlatform(
            bool useMultisampling,
            float? coordinateScaleOverride)
        {
            return new Win32WindowPlatform(
                new WindowsDxWindowGraphicsSessionFactory(useMultisampling),
                coordinateScaleOverride);
        }
    }
}
