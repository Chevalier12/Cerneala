namespace Cerneala.UI.Hosting.Windowing;

internal interface IWindowingBackend
{
    IWindowPlatform CreatePlatform(
        bool useMultisampling,
        float? coordinateScaleOverride);
}

internal static class WindowingBackendRegistry
{
    private static readonly object Sync = new();
    private static IWindowingBackend? backend;

    public static void Register(IWindowingBackend candidate)
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
                    $"Windowing backend '{backend.GetType().FullName}' is already registered.");
            }
        }
    }

    public static IWindowPlatform CreatePlatform(
        bool useMultisampling,
        float? coordinateScaleOverride)
    {
        lock (Sync)
        {
            return (backend ?? throw new InvalidOperationException(
                "No windowing backend is registered. Register an application backend before creating a window."))
                .CreatePlatform(useMultisampling, coordinateScaleOverride);
        }
    }

    internal static void ResetForTesting()
    {
        lock (Sync)
        {
            backend = null;
        }
    }
}
