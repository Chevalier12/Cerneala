namespace Cerneala.UI.Hosting.Windows;

internal interface IWindowPlatformBackend
{
    IWindowPlatform CreatePlatform(
        IWindowGraphicsSessionFactory graphicsSessionFactory,
        float? coordinateScaleOverride);
}

internal static class WindowPlatformBackendRegistry
{
    private static readonly object Sync = new();
    private static IWindowPlatformBackend? backend;

    public static void Register(IWindowPlatformBackend candidate)
    {
        ArgumentNullException.ThrowIfNull(candidate);

        lock (Sync)
        {
            if (backend is null)
            {
                backend = candidate;
                return;
            }

            if (backend.GetType() != candidate.GetType())
            {
                throw new InvalidOperationException(
                    $"Window platform backend '{backend.GetType().FullName}' is already registered.");
            }
        }
    }

    public static IWindowPlatform CreatePlatform(
        IWindowGraphicsSessionFactory graphicsSessionFactory,
        float? coordinateScaleOverride)
    {
        ArgumentNullException.ThrowIfNull(graphicsSessionFactory);

        lock (Sync)
        {
            return (backend ?? throw new InvalidOperationException(
                "No native window platform is registered. Register an application backend before creating a window."))
                .CreatePlatform(graphicsSessionFactory, coordinateScaleOverride);
        }
    }
}
