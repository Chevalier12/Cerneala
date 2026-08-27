using Cerneala.UI.Hosting.Sdl;
using Cerneala.UI.Hosting.Windows;
using Cerneala.UI.Hosting.Windowing;

namespace Cerneala.Tests.UI.Hosting;

[Collection(WindowRuntimeTestCollection.Name)]
public sealed class ApplicationBackendRegistrationTests : IDisposable
{
    public ApplicationBackendRegistrationTests() =>
        WindowingBackendRegistry.ResetForTesting();

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

    public void Dispose()
    {
        WindowingBackendRegistry.ResetForTesting();
        WindowsDxApplicationBackend.EnsureRegistered();
    }
}
