using Cerneala.UI.Controls;
using Cerneala.UI.Hosting.Windows;
using Cerneala.UI.Resources;
using Microsoft.Extensions.DependencyInjection;

namespace Cerneala.UI;

public class Application
{
    private static Application? current;

    private readonly int ownerThreadId = Environment.CurrentManagedThreadId;
    private WindowApplicationRuntime? runtime;
    private IServiceProvider? services;
    private Window? mainWindow;
    private bool shutdownRequested;
    private bool exitRaised;
    private int exitCode;

    public Application()
    {
        Resources = new ResourceDictionary();
    }

    public static Application? Current => current;

    public ResourceDictionary Resources { get; }

    public IServiceProvider Services =>
        services ?? throw new InvalidOperationException("Application services are not available before startup configuration completes.");

    public Window? MainWindow
    {
        get
        {
            VerifyAccess();
            return mainWindow;
        }
        set
        {
            VerifyAccess();
            if (value?.IsClosed == true)
            {
                throw new InvalidOperationException("A closed Window cannot become Application.MainWindow.");
            }

            mainWindow = value;
        }
    }

    public IReadOnlyList<Window> Windows
    {
        get
        {
            VerifyAccess();
            return runtime?.Windows ?? Array.Empty<Window>();
        }
    }

    public Window? ActiveWindow
    {
        get
        {
            VerifyAccess();
            return runtime?.ActiveWindow;
        }
    }

    public ApplicationShutdownMode ShutdownMode { get; set; } = ApplicationShutdownMode.OnLastWindowClose;

    public event EventHandler<ApplicationStartupEventArgs>? Startup;

    public event EventHandler<ApplicationExitEventArgs>? Exit;

    internal int ExitCode => exitCode;

    internal bool IsShutdownRequested => shutdownRequested;

    public void Shutdown()
    {
        Shutdown(0);
    }

    public void Shutdown(int exitCode)
    {
        VerifyAccess();
        if (shutdownRequested)
        {
            return;
        }

        shutdownRequested = true;
        this.exitCode = exitCode;
        runtime?.CloseAll();
        CompleteExit();
    }

    protected virtual void ConfigureServices(IServiceCollection services)
    {
    }

    protected virtual void OnStartup(ApplicationStartupEventArgs args)
    {
        Startup?.Invoke(this, args);
    }

    protected virtual void OnExit(ApplicationExitEventArgs args)
    {
        Exit?.Invoke(this, args);
    }

    internal static void ResetForTesting()
    {
        current?.ResetStateForTesting();
        current = null;
    }

    internal void Install(WindowApplicationRuntime runtime)
    {
        VerifyAccess();
        ArgumentNullException.ThrowIfNull(runtime);
        if (current is not null && !ReferenceEquals(current, this))
        {
            throw new InvalidOperationException("Only one Application may be installed on the UI thread.");
        }

        if (this.runtime is not null && !ReferenceEquals(this.runtime, runtime))
        {
            throw new InvalidOperationException("The Application is already attached to another Window runtime.");
        }

        current = this;
        this.runtime = runtime;
        runtime.SetApplication(this);
    }

    internal void ConfigureAndPublishServices(IServiceCollection serviceCollection)
    {
        VerifyAccess();
        ArgumentNullException.ThrowIfNull(serviceCollection);
        ConfigureServices(serviceCollection);
        services = serviceCollection.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateScopes = true
        });
    }

    internal void PublishServices(IServiceProvider serviceProvider)
    {
        VerifyAccess();
        services = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

    internal void Start(IReadOnlyList<string> args)
    {
        VerifyAccess();
        OnStartup(new ApplicationStartupEventArgs(args));
    }

    internal void HandleWindowClosed(Window window)
    {
        VerifyAccess();
        if (shutdownRequested || runtime is null)
        {
            return;
        }

        bool shouldShutdown = ShutdownMode switch
        {
            ApplicationShutdownMode.OnLastWindowClose => runtime.Windows.Count == 0,
            ApplicationShutdownMode.OnMainWindowClose => ReferenceEquals(window, mainWindow),
            ApplicationShutdownMode.OnExplicitShutdown => false,
            _ => throw new InvalidOperationException($"Unsupported Application shutdown mode '{ShutdownMode}'.")
        };

        if (shouldShutdown)
        {
            Shutdown();
        }
    }

    internal void CompleteExit()
    {
        VerifyAccess();
        if (exitRaised)
        {
            return;
        }

        exitRaised = true;
        try
        {
            OnExit(new ApplicationExitEventArgs(exitCode));
        }
        finally
        {
            if (services is IDisposable disposable)
            {
                disposable.Dispose();
            }

            services = null;
            DetachRuntime();
        }
    }

    internal void ResetStateForTesting()
    {
        if (services is IDisposable disposable)
        {
            disposable.Dispose();
        }

        services = null;
        runtime = null;
        current = null;
    }

    private void DetachRuntime()
    {
        runtime?.ClearApplication(this);
        runtime = null;
        if (ReferenceEquals(current, this))
        {
            current = null;
        }
    }

    private void VerifyAccess()
    {
        if (Environment.CurrentManagedThreadId != ownerThreadId)
        {
            throw new InvalidOperationException("Application APIs must be called on the owning UI thread.");
        }
    }
}
