using Cerneala.UI;
using Cerneala.UI.Hosting.Sdl;
using Cerneala.UI.Hosting.Windows;
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
    public void SdlGpuThenWindowsDxRegistrationFailsDeterministically()
    {
        SdlGpuApplicationBackend.EnsureRegistered();

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
            WindowsDxApplicationBackend.EnsureRegistered);

        Assert.Equal(
            "Windowing backend 'Cerneala.UI.Hosting.Sdl.SdlGpuApplicationBackend+SdlGpuWindowingBackend' is already registered.",
            exception.Message);
    }

    [Fact]
    public void WindowsDxThenSdlGpuRegistrationFailsDeterministically()
    {
        WindowsDxApplicationBackend.EnsureRegistered();

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
            SdlGpuApplicationBackend.EnsureRegistered);

        Assert.Equal(
            "Windowing backend 'Cerneala.UI.Hosting.Windows.WindowsDxApplicationBackend+WindowsDxWindowingBackend' is already registered.",
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

    public void Dispose()
    {
        WindowApplicationRuntime.ResetForTesting();
        Application.ResetForTesting();
        WindowingBackendRegistry.ResetForTesting();
        WindowsDxApplicationBackend.EnsureRegistered();
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
