using Cerneala.UI;
using Cerneala.UI.Hosting.Sdl;
using Cerneala.UI.Hosting.Windowing;
using Cerneala.UI.Controls;

namespace Cerneala.Tests.UI.Hosting;

[Collection(WindowRuntimeTestCollection.Name)]
public sealed class ApplicationBackendRegistrationTests : IDisposable
{
    public ApplicationBackendRegistrationTests()
    {
        Application.ResetForTesting();
        WindowApplicationRuntime.ResetForTesting();
        WindowingBackendRegistry.ResetForTesting();
    }

    [Fact]
    public void SdlGpuRegistrationIsIdempotent()
    {
        SdlGpuApplicationBackend.EnsureRegistered();
        SdlGpuApplicationBackend.EnsureRegistered();
    }

    [Fact]
    public void SdlGpuThenAnotherBackendRegistrationFailsDeterministically()
    {
        SdlGpuApplicationBackend.EnsureRegistered();

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
            () => WindowingBackendRegistry.Register(new CapturingWindowingBackend()));

        Assert.Equal(
            "Windowing backend 'Cerneala.UI.Hosting.Sdl.SdlGpuApplicationBackend+SdlGpuWindowingBackend' is already registered.",
            exception.Message);
    }

    [Fact]
    public void AnotherBackendThenSdlGpuRegistrationFailsDeterministically()
    {
        WindowingBackendRegistry.Register(new CapturingWindowingBackend());

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
            SdlGpuApplicationBackend.EnsureRegistered);

        Assert.Equal(
            $"Windowing backend '{typeof(CapturingWindowingBackend).FullName}' is already registered.",
            exception.Message);
    }

    [Fact]
    public void GeneratedStartupPassesApplicationMultisamplingPreferenceToBackend()
    {
        CapturingWindowingBackend backend = new();
        WindowingBackendRegistry.Register(backend);
        GeneratedWindowStartupDescriptor descriptor = new(
            () => new ShutdownOnStartupApplication { UseMultisampling = false },
            _ => { },
            _ => new Window(),
            "TestInput.MainWindow");

        int exitCode = GeneratedWindowApplication.Run(descriptor, []);

        Assert.Equal(0, exitCode);
        Assert.False(backend.UseMultisampling);
    }

    // A failing startup window factory (a stage that already carried the keys) is the control.
    [Theory]
    [InlineData("windowFactoryFails", "resolve and create startup Window")]
    [InlineData("platformCreationFails", "create runtime")]
    [InlineData("hostedPlatformCreationFails", "create runtime")]
    [InlineData("legacyPlatformCreationFails", "create runtime")]
    [InlineData("hostedLegacyPlatformCreationFails", "create runtime")]
    public void StartupFailureCarriesStageAndTargetKeysWhenThePlatformCannotBeCreated(string row, string expectedStage)
    {
        InvalidOperationException expected = new("boom");
        WindowingBackendRegistry.Register(row == "windowFactoryFails"
            ? new CapturingWindowingBackend()
            : new ThrowingWindowingBackend(expected));
        bool legacy = row.Contains("legacy", StringComparison.OrdinalIgnoreCase);
        GeneratedWindowStartupDescriptor descriptor = legacy
            ? new(_ => { }, _ => new Window())
            : new(() => new Application(), _ => { }, _ => throw expected, "TestInput.MainWindow");

        InvalidOperationException actual;
        if (row.StartsWith("hosted", StringComparison.Ordinal))
        {
            GeneratedWindowApplication.RegisterStartup(descriptor);
            actual = Assert.Throws<InvalidOperationException>(
                () => GeneratedWindowApplication.PumpHosted(TimeSpan.FromMilliseconds(16)));
        }
        else
        {
            actual = Assert.Throws<InvalidOperationException>(
                () => GeneratedWindowApplication.Run(descriptor, []));
        }

        Assert.Same(expected, actual);
        string present = string.Join(", ", actual.Data.Keys.Cast<object>().Select(key => $"{key}={actual.Data[key]}"));
        string expectedTarget = legacy ? "<legacy MainWindow>" : "TestInput.MainWindow";
        Assert.True(
            Equals(expectedStage, actual.Data["Cerneala.StartupStage"])
                && Equals(expectedTarget, actual.Data["Cerneala.StartupTarget"]),
            $"{row}: Data keys present: [{present}]; expected Cerneala.StartupStage = '{expectedStage}' and Cerneala.StartupTarget = '{expectedTarget}'.");
    }

    public void Dispose()
    {
        GeneratedWindowApplication.ResetForTesting();
        WindowApplicationRuntime.ResetForTesting();
        Application.ResetForTesting();
        WindowingBackendRegistry.ResetForTesting();
        SdlGpuApplicationBackend.EnsureRegistered();
    }

    private sealed class ShutdownOnStartupApplication : Application
    {
        protected override void OnStartup(ApplicationStartupEventArgs args)
        {
            base.OnStartup(args);
            Shutdown();
        }
    }

    private sealed class CapturingWindowingBackend : IWindowingBackend
    {
        public bool? UseMultisampling { get; private set; }

        public IWindowPlatform CreatePlatform(bool useMultisampling, float? coordinateScaleOverride)
        {
            UseMultisampling = useMultisampling;
            return new NoOpWindowPlatform();
        }
    }

    private sealed class ThrowingWindowingBackend(Exception failure) : IWindowingBackend
    {
        public IWindowPlatform CreatePlatform(bool useMultisampling, float? coordinateScaleOverride) => throw failure;
    }

    private sealed class NoOpWindowPlatform : IWindowPlatform
    {
        public IPlatformWindow CreateWindow(Window window, IWindowPlatformCallbacks callbacks) =>
            throw new InvalidOperationException("The test application shuts down before creating a window.");

        public void PumpEvents()
        {
        }

        public void Dispose()
        {
        }
    }
}
