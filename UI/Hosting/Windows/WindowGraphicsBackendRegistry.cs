namespace Cerneala.UI.Hosting.Windows;

internal interface IWindowGraphicsBackend
{
    IWindowGraphicsSessionFactory CreateSessionFactory(bool useMultisampling);
}

internal static class WindowGraphicsBackendRegistry
{
    private static readonly object Sync = new();
    private static IWindowGraphicsBackend? backend;

    public static void Register(IWindowGraphicsBackend candidate)
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
                    $"Window graphics backend '{backend.GetType().FullName}' is already registered.");
            }
        }
    }

    public static IWindowGraphicsSessionFactory CreateSessionFactory(bool useMultisampling)
    {
        lock (Sync)
        {
            return (backend ?? throw new InvalidOperationException(
                "No window graphics backend is registered. Register an application backend before creating a window."))
                .CreateSessionFactory(useMultisampling);
        }
    }
}
