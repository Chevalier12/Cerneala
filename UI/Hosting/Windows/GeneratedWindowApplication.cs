using Cerneala.UI.Controls;
using Microsoft.Extensions.DependencyInjection;

namespace Cerneala.UI.Hosting.Windows;

public sealed class GeneratedWindowStartupDescriptor
{
    public GeneratedWindowStartupDescriptor(
        Action<IServiceCollection> configureServices,
        Func<IServiceProvider, Window> createMainWindow)
    {
        ConfigureServices = configureServices ?? throw new ArgumentNullException(nameof(configureServices));
        CreateMainWindow = createMainWindow ?? throw new ArgumentNullException(nameof(createMainWindow));
    }

    public GeneratedWindowStartupDescriptor(
        Func<Application> createApplication,
        Action<IServiceCollection> configureServices,
        Func<IServiceProvider, Window> createStartupWindow,
        string? startupWindowTypeName = null)
    {
        CreateApplication = createApplication ?? throw new ArgumentNullException(nameof(createApplication));
        ConfigureServices = configureServices ?? throw new ArgumentNullException(nameof(configureServices));
        CreateMainWindow = createStartupWindow ?? throw new ArgumentNullException(nameof(createStartupWindow));
        StartupWindowTypeName = string.IsNullOrWhiteSpace(startupWindowTypeName)
            ? "<startup Window>"
            : startupWindowTypeName;
    }

    internal Action<IServiceCollection> ConfigureServices { get; }

    internal Func<IServiceProvider, Window> CreateMainWindow { get; }

    internal Func<Application>? CreateApplication { get; }

    internal string StartupWindowTypeName { get; } = "<legacy MainWindow>";
}

public static class GeneratedWindowApplication
{
    private static GeneratedWindowStartupDescriptor? pendingStartup;
    private static ServiceProvider? hostedServices;
    private static WindowApplicationRuntime? hostedRuntime;

    public static void RegisterStartup(GeneratedWindowStartupDescriptor descriptor)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        if (pendingStartup is not null && !ReferenceEquals(pendingStartup, descriptor))
        {
            throw new InvalidOperationException("Only one generated Application startup descriptor may be registered.");
        }

        pendingStartup = descriptor;
    }

    public static int Run(GeneratedWindowStartupDescriptor descriptor)
    {
        return Run(descriptor, Environment.GetCommandLineArgs().Skip(1).ToArray());
    }

    public static int Run(GeneratedWindowStartupDescriptor descriptor, IReadOnlyList<string> args)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        ArgumentNullException.ThrowIfNull(args);
        if (descriptor.CreateApplication is null)
        {
            RunLegacy(descriptor);
            return 0;
        }

        Application application = ExecuteStartupStage(
            descriptor,
            "construct Application",
            descriptor.CreateApplication);
        WindowApplicationRuntime runtime = WindowApplicationRuntime.CurrentOrDefault;
        Exception? failure = null;
        try
        {
            StartApplication(descriptor, application, runtime, args);
            if (!application.IsShutdownRequested)
            {
                runtime.RunStandalone(application);
            }

            return application.ExitCode;
        }
        catch (Exception exception)
        {
            failure = exception;
            throw;
        }
        finally
        {
            DisposeRuntime(runtime, failure);
        }
    }

    internal static void PumpHosted(TimeSpan elapsedTime)
    {
        if (pendingStartup is not null && hostedRuntime is null)
        {
            if (WindowApplicationRuntime.Current is null && !OperatingSystem.IsWindows())
            {
                throw new PlatformNotSupportedException("Generated Window startup is currently available only on Windows.");
            }

            GeneratedWindowStartupDescriptor descriptor = pendingStartup;
            pendingStartup = null;
            hostedRuntime = WindowApplicationRuntime.CurrentOrDefault;
            Exception? failure = null;
            try
            {
                if (descriptor.CreateApplication is null)
                {
                    hostedServices = BuildServices(descriptor);
                    hostedRuntime.StartMainWindow(descriptor.CreateMainWindow(hostedServices));
                }
                else
                {
                    Application application = ExecuteStartupStage(
                        descriptor,
                        "construct Application",
                        descriptor.CreateApplication);
                    StartApplication(descriptor, application, hostedRuntime, Array.Empty<string>());
                    if (application.IsShutdownRequested)
                    {
                        hostedRuntime.Dispose();
                        hostedRuntime = null;
                    }
                }
            }
            catch (Exception exception)
            {
                failure = exception;
                if (hostedRuntime is not null)
                {
                    DisposeRuntime(hostedRuntime, failure);
                }

                hostedRuntime = null;
                hostedServices?.Dispose();
                hostedServices = null;
                throw;
            }
        }

        hostedRuntime?.PumpOnce(elapsedTime);
    }

    internal static void StopHosted()
    {
        pendingStartup = null;
        hostedRuntime?.Dispose();
        hostedRuntime = null;
        hostedServices?.Dispose();
        hostedServices = null;
    }

    internal static void ResetForTesting()
    {
        StopHosted();
    }

    private static ServiceProvider BuildServices(GeneratedWindowStartupDescriptor descriptor)
    {
        ServiceCollection services = new();
        descriptor.ConfigureServices(services);
        return services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateScopes = true
        });
    }

    private static void RunLegacy(GeneratedWindowStartupDescriptor descriptor)
    {
        using ServiceProvider services = BuildServices(descriptor);
        Window mainWindow = descriptor.CreateMainWindow(services);
        WindowApplicationRuntime runtime = WindowApplicationRuntime.CurrentOrDefault;
        try
        {
            runtime.RunStandalone(mainWindow);
        }
        finally
        {
            runtime.Dispose();
        }
    }

    private static void StartApplication(
        GeneratedWindowStartupDescriptor descriptor,
        Application application,
        WindowApplicationRuntime runtime,
        IReadOnlyList<string> args)
    {
        ExecuteStartupStage(descriptor, "install Application.Current", () => application.Install(runtime));
        ExecuteStartupStage(
            descriptor,
            "configure and build services",
            () =>
            {
                ServiceCollection services = new();
                descriptor.ConfigureServices(services);
                application.ConfigureAndPublishServices(services);
            });
        ExecuteStartupStage(descriptor, "run Application startup", () => application.Start(args));
        if (application.IsShutdownRequested)
        {
            return;
        }

        Window startupWindow = ExecuteStartupStage(
            descriptor,
            "resolve and create startup Window",
            () => descriptor.CreateMainWindow(application.Services));
        ExecuteStartupStage(
            descriptor,
            "show startup Window",
            () => runtime.StartMainWindow(startupWindow));
    }

    private static void ExecuteStartupStage(
        GeneratedWindowStartupDescriptor descriptor,
        string stage,
        Action action)
    {
        ExecuteStartupStage(
            descriptor,
            stage,
            () =>
            {
                action();
                return true;
            });
    }

    private static T ExecuteStartupStage<T>(
        GeneratedWindowStartupDescriptor descriptor,
        string stage,
        Func<T> action)
    {
        try
        {
            return action();
        }
        catch (Exception exception)
        {
            exception.Data["Cerneala.StartupStage"] = stage;
            exception.Data["Cerneala.StartupTarget"] = descriptor.StartupWindowTypeName;
            throw;
        }
    }

    private static void DisposeRuntime(WindowApplicationRuntime runtime, Exception? startupFailure)
    {
        try
        {
            runtime.Dispose();
        }
        catch (Exception cleanupFailure) when (startupFailure is not null)
        {
            startupFailure.Data["Cerneala.CleanupFailure"] = cleanupFailure;
        }
    }
}
