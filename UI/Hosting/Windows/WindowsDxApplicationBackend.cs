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
        Win32ApplicationPlatform.EnsureRegistered();
        WindowGraphicsBackendRegistry.Register(WindowsDxGraphicsBackend.Instance);
    }

    private sealed class WindowsDxGraphicsBackend : IWindowGraphicsBackend
    {
        public static WindowsDxGraphicsBackend Instance { get; } = new();

        public IWindowGraphicsSessionFactory CreateSessionFactory(bool useMultisampling)
        {
            return new WindowsDxWindowGraphicsSessionFactory(useMultisampling);
        }
    }
}
