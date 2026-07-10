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

    internal Action<IServiceCollection> ConfigureServices { get; }

    internal Func<IServiceProvider, Window> CreateMainWindow { get; }
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
            throw new InvalidOperationException("Only one generated MainWindow startup descriptor may be registered.");
        }

        pendingStartup = descriptor;
    }

    public static void Run(GeneratedWindowStartupDescriptor descriptor)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
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
            hostedServices = BuildServices(descriptor);
            hostedRuntime = WindowApplicationRuntime.CurrentOrDefault;
            try
            {
                hostedRuntime.StartMainWindow(descriptor.CreateMainWindow(hostedServices));
            }
            catch
            {
                hostedRuntime.Dispose();
                hostedRuntime = null;
                hostedServices.Dispose();
                hostedServices = null;
                throw;
            }
        }

        hostedRuntime?.PumpOnce(elapsedTime);
    }

    internal static void StopHosted()
    {
        hostedRuntime?.Dispose();
        hostedRuntime = null;
        hostedServices?.Dispose();
        hostedServices = null;
    }

    internal static void ResetForTesting()
    {
        StopHosted();
        pendingStartup = null;
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
}
