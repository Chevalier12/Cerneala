using System.Reflection;
using Cerneala.Drawing;
using Cerneala.UI;
using Cerneala.UI.Controls;
using Cerneala.UI.Hosting;
using Cerneala.UI.Hosting.Windows;
using Cerneala.UI.Input;
using Cerneala.UI.Resources;
using Microsoft.Extensions.DependencyInjection;

namespace Cerneala.Tests.UI.Hosting;

[Collection(WindowRuntimeTestCollection.Name)]
public sealed class ApplicationRuntimeTests : IDisposable
{
    public ApplicationRuntimeTests()
    {
        Application.ResetForTesting();
        WindowApplicationRuntime.ResetForTesting();
    }

    public void Dispose()
    {
        WindowApplicationRuntime.ResetForTesting();
        Application.ResetForTesting();
    }

    [Fact]
    public void ApplicationExposesTheRequiredPublicLifecycleContract()
    {
        Type application = RequireApplicationType();

        Assert.True(application.IsClass);
        Assert.False(application.IsSealed);
        AssertProperty(application, "Current", application, isStatic: true);
        AssertProperty(application, "Resources", typeof(ResourceDictionary));
        AssertProperty(application, "Services", typeof(IServiceProvider));
        AssertProperty(application, "MainWindow", typeof(Window));
        AssertProperty(application, "Windows");
        AssertProperty(application, "ActiveWindow", typeof(Window));
        AssertProperty(application, "ShutdownMode", RequireShutdownModeType());
        AssertMethod(application, "Shutdown", Type.EmptyTypes);
        AssertMethod(application, "Shutdown", [typeof(int)]);
        AssertEvent(application, "Startup");
        AssertEvent(application, "Exit");

        AssertProtectedVirtual(application, "ConfigureServices");
        AssertProtectedVirtual(application, "OnStartup");
        AssertProtectedVirtual(application, "OnExit");
    }

    [Fact]
    public void ApplicationShutdownModeContainsExactlyTheThreeDesktopPolicies()
    {
        Type mode = RequireShutdownModeType();

        Assert.True(mode.IsEnum);
        Assert.Equal(
            ["OnLastWindowClose", "OnMainWindowClose", "OnExplicitShutdown"],
            Enum.GetNames(mode));
    }

    [Theory]
    [InlineData("OnLastWindowClose")]
    [InlineData("OnMainWindowClose")]
    [InlineData("OnExplicitShutdown")]
    public void ApplicationCanSelectEveryShutdownPolicy(string modeName)
    {
        Type applicationType = RequireApplicationType();
        Type modeType = RequireShutdownModeType();
        object application = Activator.CreateInstance(applicationType)!;
        PropertyInfo property = applicationType.GetProperty("ShutdownMode")!;
        object mode = Enum.Parse(modeType, modeName);

        property.SetValue(application, mode);

        Assert.Equal(mode, property.GetValue(application));
    }

    [Fact]
    public void ShutdownIsIdempotentAndPreservesTheFirstExitCode()
    {
        Type applicationType = RequireApplicationType();
        object application = Activator.CreateInstance(applicationType)!;
        MethodInfo shutdown = AssertMethod(applicationType, "Shutdown", [typeof(int)]);
        int exitCount = 0;
        EventInfo exit = AssertEvent(applicationType, "Exit");
        Delegate handler = CreateCountingHandler(exit.EventHandlerType!, () => exitCount++);
        exit.AddEventHandler(application, handler);

        shutdown.Invoke(application, [23]);
        shutdown.Invoke(application, [99]);

        Assert.Equal(1, exitCount);
        PropertyInfo exitCode = applicationType.GetProperty(
            "ExitCode",
            BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)!;
        Assert.Equal(23, exitCode.GetValue(application));
    }

    [Fact]
    public void LifecycleEventArgumentsCarryCommandLineArgumentsAndExitCode()
    {
        Assembly assembly = typeof(Window).Assembly;
        Type startupArgs = assembly.GetType("Cerneala.UI.ApplicationStartupEventArgs", throwOnError: true)!;
        Type exitArgs = assembly.GetType("Cerneala.UI.ApplicationExitEventArgs", throwOnError: true)!;

        AssertProperty(startupArgs, "Args");
        AssertProperty(exitArgs, "ExitCode", typeof(int));
    }

    [Fact]
    public void OnLastWindowCloseExitsOnlyAfterTheLastSuccessfulClose()
    {
        (Application app, WindowApplicationRuntime runtime) = Install(ApplicationShutdownMode.OnLastWindowClose);
        Window first = new();
        Window second = new();
        int exitCount = 0;
        app.Exit += (_, _) => exitCount++;
        first.Show();
        second.Show();

        first.Close();

        Assert.Equal(0, exitCount);
        Assert.Same(app, Application.Current);
        Assert.Single(app.Windows);

        second.Close();

        Assert.Equal(1, exitCount);
        Assert.Null(Application.Current);
        Assert.Empty(runtime.Windows);
    }

    [Fact]
    public void OnMainWindowCloseUsesTheWindowDesignatedAtCloseTime()
    {
        (Application app, WindowApplicationRuntime runtime) = Install(ApplicationShutdownMode.OnMainWindowClose);
        Window original = new();
        Window replacement = new();
        int exitCount = 0;
        app.Exit += (_, _) => exitCount++;
        runtime.StartMainWindow(original);
        replacement.Show();
        app.MainWindow = replacement;

        original.Close();

        Assert.Equal(0, exitCount);
        Assert.False(replacement.IsClosed);
        Assert.Same(replacement, app.MainWindow);

        replacement.Close();

        Assert.Equal(1, exitCount);
        Assert.Null(Application.Current);
    }

    [Fact]
    public void OnExplicitShutdownKeepsApplicationAliveAfterAllWindowsClose()
    {
        (Application app, _) = Install(ApplicationShutdownMode.OnExplicitShutdown);
        Window window = new();
        int exitCount = 0;
        int observedExitCode = -1;
        app.Exit += (_, args) =>
        {
            exitCount++;
            observedExitCode = args.ExitCode;
        };
        window.Show();

        window.Close();

        Assert.Equal(0, exitCount);
        Assert.Same(app, Application.Current);
        Assert.Empty(app.Windows);

        app.Shutdown(17);
        app.Shutdown(99);

        Assert.Equal(1, exitCount);
        Assert.Equal(17, observedExitCode);
        Assert.Null(Application.Current);
    }

    [Fact]
    public void CancelledMainWindowCloseDoesNotExitDisposeOrCloseSecondaryWindows()
    {
        (Application app, WindowApplicationRuntime runtime) = Install(ApplicationShutdownMode.OnMainWindowClose);
        TrackingProvider services = new();
        app.PublishServices(services);
        Window main = new();
        Window secondary = new();
        bool cancel = true;
        int exitCount = 0;
        app.Exit += (_, _) => exitCount++;
        main.Closing += (_, args) => args.Cancel = cancel;
        runtime.StartMainWindow(main);
        secondary.Show();

        main.Close();

        Assert.Equal(0, exitCount);
        Assert.Equal(0, services.DisposeCount);
        Assert.False(main.IsClosed);
        Assert.False(secondary.IsClosed);

        cancel = false;
        main.Close();

        Assert.Equal(1, exitCount);
        Assert.Equal(1, services.DisposeCount);
        Assert.True(secondary.IsClosed);
    }

    [Fact]
    public void LifecyclePublishesServicesBeforeStartupAndDisposesAfterExit()
    {
        List<string> events = [];
        TrackingApplication app = new(events);
        WindowApplicationRuntime runtime = new(new FakeWindowPlatform());
        WindowApplicationRuntime.Install(runtime);
        app.Install(runtime);
        TrackingProvider services = new(events);
        app.PublishServices(services);
        app.Start(["first", "second"]);

        app.Shutdown(31);

        Assert.Equal(
            ["startup:first,second", "exit:31", "services-disposed"],
            events);
        Assert.Equal(1, services.DisposeCount);
        Assert.Null(Application.Current);
    }

    [Fact]
    public async Task LifecycleOperationsRejectAnotherThread()
    {
        (Application app, _) = Install(ApplicationShutdownMode.OnExplicitShutdown);

        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => Task.Run(app.Shutdown));

        Assert.Contains("owning UI thread", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void StandaloneGeneratedLifecyclePropagatesArgumentsAndExitCode()
    {
        List<string> events = [];
        DisposableProbe probe = new(events);
        GeneratedLifecycleApplication app = new(events, shutdownOnStartup: true);
        WindowApplicationRuntime.Install(new WindowApplicationRuntime(new FakeWindowPlatform()));
        GeneratedWindowStartupDescriptor descriptor = new(
            () =>
            {
                events.Add("construct");
                return app;
            },
            services =>
            {
                events.Add("descriptor-configure");
                services.AddSingleton(_ => probe);
            },
            provider =>
            {
                events.Add("resolve-window");
                return new Window();
            },
            "TestInput.ShellWindow");

        int exitCode = GeneratedWindowApplication.Run(descriptor, ["first", "second"]);

        Assert.Equal(42, exitCode);
        Assert.Equal(
            [
                "construct",
                "descriptor-configure",
                "app-configure",
                "startup:first,second",
                "exit:42",
                "services-disposed"
            ],
            events);
        Assert.Null(Application.Current);
        Assert.Null(WindowApplicationRuntime.Current);
    }

    [Fact]
    public void HostedGeneratedLifecycleCreatesOnceUsesEmptyArgumentsAndStopsCleanly()
    {
        List<string> events = [];
        DisposableProbe probe = new(events);
        GeneratedLifecycleApplication app = new(events, shutdownOnStartup: false);
        int applicationFactoryCount = 0;
        int windowFactoryCount = 0;
        WindowApplicationRuntime.Install(new WindowApplicationRuntime(new FakeWindowPlatform()));
        GeneratedWindowApplication.RegisterStartup(new GeneratedWindowStartupDescriptor(
            () =>
            {
                applicationFactoryCount++;
                events.Add("construct");
                return app;
            },
            services =>
            {
                events.Add("descriptor-configure");
                services.AddSingleton(_ => probe);
            },
            provider =>
            {
                windowFactoryCount++;
                events.Add("resolve-window");
                return new Window();
            },
            "TestInput.ShellWindow"));

        GeneratedWindowApplication.PumpHosted(TimeSpan.FromMilliseconds(16));
        GeneratedWindowApplication.PumpHosted(TimeSpan.FromMilliseconds(16));

        Assert.Equal(1, applicationFactoryCount);
        Assert.Equal(1, windowFactoryCount);
        Assert.Same(app, Application.Current);
        Assert.Equal(
            ["construct", "descriptor-configure", "app-configure", "startup:", "resolve-window"],
            events);

        GeneratedWindowApplication.StopHosted();
        GeneratedWindowApplication.StopHosted();

        Assert.Equal(
            [
                "construct",
                "descriptor-configure",
                "app-configure",
                "startup:",
                "resolve-window",
                "exit:0",
                "services-disposed"
            ],
            events);
        Assert.Null(Application.Current);
        Assert.Null(WindowApplicationRuntime.Current);
    }

    [Theory]
    [InlineData("construct", "construct Application")]
    [InlineData("descriptor-configure", "configure and build services")]
    [InlineData("app-configure", "configure and build services")]
    [InlineData("startup", "run Application startup")]
    [InlineData("resolve-window", "resolve and create startup Window")]
    [InlineData("show-window", "show startup Window")]
    public void GeneratedStartupFailuresPreserveOriginalExceptionAndCleanInstalledState(
        string failurePoint,
        string expectedStage)
    {
        InvalidOperationException expected = new("planned startup failure");
        FailureApplication? app = null;
        FakeWindowPlatform platform = new(failurePoint == "show-window" ? expected : null);
        WindowApplicationRuntime.Install(new WindowApplicationRuntime(platform));
        GeneratedWindowStartupDescriptor descriptor = new(
            () =>
            {
                if (failurePoint == "construct")
                {
                    throw expected;
                }

                return app = new FailureApplication(failurePoint, expected);
            },
            _ =>
            {
                if (failurePoint == "descriptor-configure")
                {
                    throw expected;
                }
            },
            _ =>
            {
                if (failurePoint == "resolve-window")
                {
                    throw expected;
                }

                return new Window();
            },
            "TestInput.FailingWindow");

        InvalidOperationException actual = Assert.Throws<InvalidOperationException>(
            () => GeneratedWindowApplication.Run(descriptor, []));

        Assert.Same(expected, actual);
        Assert.Equal(expectedStage, actual.Data["Cerneala.StartupStage"]);
        Assert.Equal("TestInput.FailingWindow", actual.Data["Cerneala.StartupTarget"]);
        Assert.Null(Application.Current);
        if (failurePoint == "construct")
        {
            Assert.Equal(0, app?.ExitCount ?? 0);
        }
        else
        {
            Assert.Equal(1, app!.ExitCount);
            Assert.Null(WindowApplicationRuntime.Current);
        }
    }

    [Fact]
    public void GeneratedServiceProviderBuildFailureRunsExitAndClearsStaticState()
    {
        FailureApplication app = new(string.Empty, new InvalidOperationException());
        WindowApplicationRuntime.Install(new WindowApplicationRuntime(new FakeWindowPlatform()));
        GeneratedWindowStartupDescriptor descriptor = new(
            () => app,
            services => services.AddSingleton(typeof(IList<>), typeof(string)),
            _ => new Window(),
            "TestInput.FailingWindow");

        Exception exception = Assert.ThrowsAny<Exception>(
            () => GeneratedWindowApplication.Run(descriptor, []));

        Assert.Equal("configure and build services", exception.Data["Cerneala.StartupStage"]);
        Assert.Equal(1, app.ExitCount);
        Assert.Null(Application.Current);
        Assert.Null(WindowApplicationRuntime.Current);
    }

    private static Type RequireApplicationType()
    {
        return typeof(Window).Assembly.GetType("Cerneala.UI.Application", throwOnError: true)!;
    }

    private static Type RequireShutdownModeType()
    {
        return typeof(Window).Assembly.GetType("Cerneala.UI.ApplicationShutdownMode", throwOnError: true)!;
    }

    private static PropertyInfo AssertProperty(
        Type owner,
        string name,
        Type? propertyType = null,
        bool isStatic = false)
    {
        PropertyInfo property = owner.GetProperty(name, BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static)!;
        Assert.NotNull(property);
        Assert.Equal(isStatic, property.GetMethod!.IsStatic);
        if (propertyType is not null)
        {
            Assert.Equal(propertyType, property.PropertyType);
        }

        return property;
    }

    private static MethodInfo AssertMethod(Type owner, string name, Type[] parameterTypes)
    {
        MethodInfo method = owner.GetMethod(name, parameterTypes)!;
        Assert.NotNull(method);
        return method;
    }

    private static EventInfo AssertEvent(Type owner, string name)
    {
        EventInfo eventInfo = owner.GetEvent(name)!;
        Assert.NotNull(eventInfo);
        return eventInfo;
    }

    private static void AssertProtectedVirtual(Type owner, string name)
    {
        MethodInfo method = owner.GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic)!;
        Assert.NotNull(method);
        Assert.True(method.IsFamily);
        Assert.True(method.IsVirtual);
    }

    private static Delegate CreateCountingHandler(Type handlerType, Action callback)
    {
        MethodInfo invoke = handlerType.GetMethod("Invoke")!;
        ParameterInfo[] parameters = invoke.GetParameters();
        Assert.Equal(2, parameters.Length);
        MethodInfo adapter = typeof(ApplicationRuntimeTests).GetMethod(
            nameof(CountEvent),
            BindingFlags.Static | BindingFlags.NonPublic)!;
        return adapter.CreateDelegate(handlerType, callback);
    }

    private static void CountEvent(Action callback, object? sender, EventArgs args)
    {
        callback();
    }

    private static (Application App, WindowApplicationRuntime Runtime) Install(ApplicationShutdownMode mode)
    {
        WindowApplicationRuntime runtime = new(new FakeWindowPlatform());
        WindowApplicationRuntime.Install(runtime);
        Application app = new() { ShutdownMode = mode };
        app.Install(runtime);
        return (app, runtime);
    }

    private sealed class TrackingApplication : Application
    {
        private readonly List<string> events;

        public TrackingApplication(List<string> events)
        {
            this.events = events;
        }

        protected override void OnStartup(ApplicationStartupEventArgs args)
        {
            events.Add($"startup:{string.Join(',', args.Args)}");
            base.OnStartup(args);
        }

        protected override void OnExit(ApplicationExitEventArgs args)
        {
            events.Add($"exit:{args.ExitCode}");
            base.OnExit(args);
        }
    }

    private sealed class TrackingProvider : IServiceProvider, IDisposable
    {
        private readonly List<string>? events;

        public TrackingProvider(List<string>? events = null)
        {
            this.events = events;
        }

        public int DisposeCount { get; private set; }

        public object? GetService(Type serviceType)
        {
            return null;
        }

        public void Dispose()
        {
            DisposeCount++;
            events?.Add("services-disposed");
        }
    }

    private sealed class GeneratedLifecycleApplication : Application
    {
        private readonly List<string> events;
        private readonly bool shutdownOnStartup;

        public GeneratedLifecycleApplication(List<string> events, bool shutdownOnStartup)
        {
            this.events = events;
            this.shutdownOnStartup = shutdownOnStartup;
        }

        protected override void ConfigureServices(IServiceCollection services)
        {
            events.Add("app-configure");
            base.ConfigureServices(services);
        }

        protected override void OnStartup(ApplicationStartupEventArgs args)
        {
            _ = Services.GetRequiredService<DisposableProbe>();
            events.Add($"startup:{string.Join(',', args.Args)}");
            base.OnStartup(args);
            if (shutdownOnStartup)
            {
                Shutdown(42);
            }
        }

        protected override void OnExit(ApplicationExitEventArgs args)
        {
            events.Add($"exit:{args.ExitCode}");
            base.OnExit(args);
        }
    }

    private sealed class FailureApplication : Application
    {
        private readonly string failurePoint;
        private readonly Exception failure;

        public FailureApplication(string failurePoint, Exception failure)
        {
            this.failurePoint = failurePoint;
            this.failure = failure;
        }

        public int ExitCount { get; private set; }

        protected override void ConfigureServices(IServiceCollection services)
        {
            if (failurePoint == "app-configure")
            {
                throw failure;
            }
        }

        protected override void OnStartup(ApplicationStartupEventArgs args)
        {
            if (failurePoint == "startup")
            {
                throw failure;
            }
        }

        protected override void OnExit(ApplicationExitEventArgs args)
        {
            ExitCount++;
        }
    }

    private sealed class DisposableProbe : IDisposable
    {
        private readonly List<string> events;

        public DisposableProbe(List<string> events)
        {
            this.events = events;
        }

        public void Dispose()
        {
            events.Add("services-disposed");
        }
    }

    private sealed class FakeWindowPlatform : IWindowPlatform
    {
        private readonly Exception? createWindowFailure;

        public FakeWindowPlatform(Exception? createWindowFailure = null)
        {
            this.createWindowFailure = createWindowFailure;
        }

        public IPlatformWindow CreateWindow(Window window, IWindowPlatformCallbacks callbacks)
        {
            if (createWindowFailure is not null)
            {
                throw createWindowFailure;
            }

            return new FakePlatformWindow(callbacks);
        }

        public void PumpEvents()
        {
        }

        public void Dispose()
        {
        }
    }

    private sealed class FakePlatformWindow : IPlatformWindow
    {
        private readonly IWindowPlatformCallbacks callbacks;

        public FakePlatformWindow(IWindowPlatformCallbacks callbacks)
        {
            this.callbacks = callbacks;
        }

        public nint Handle => 1;

        public UiViewport Viewport { get; } = new(800, 600);

        public IInputSource InputSource { get; } = new EmptyInputSource();

        public IWindowGraphicsSession GraphicsSession { get; } = new FakeGraphicsSession();

        public void ApplyProperties(Window source)
        {
        }

        public void SetOwner(IPlatformWindow? owner)
        {
        }

        public void SetEnabled(bool enabled)
        {
        }

        public void Show()
        {
        }

        public void Hide()
        {
        }

        public void Activate()
        {
            callbacks.ActivationChanged(true);
        }

        public void Destroy()
        {
        }

        public void Dispose()
        {
            GraphicsSession.Dispose();
        }
    }

    private sealed class EmptyInputSource : IInputSource
    {
        public InputFrame GetFrame()
        {
            return new InputFrame(
                PointerSnapshot.Empty,
                PointerSnapshot.Empty,
                KeyboardSnapshot.Empty,
                KeyboardSnapshot.Empty,
                []);
        }
    }

    private sealed class FakeGraphicsSession : IWindowGraphicsSession
    {
        public IDrawingBackend DrawingBackend { get; } = new NullDrawingBackend();

        public IImageLoader? ImageLoader => null;

        public ImageResourceCache? ImageResourceCache => null;

        public void Resize(int pixelWidth, int pixelHeight, float coordinateScale)
        {
        }

        public void BeginFrame(Color clearColor)
        {
        }

        public void Present()
        {
        }

        public void Dispose()
        {
        }
    }

    private sealed class NullDrawingBackend : IDrawingBackend
    {
        public void Render(DrawCommandList commands)
        {
        }
    }
}
