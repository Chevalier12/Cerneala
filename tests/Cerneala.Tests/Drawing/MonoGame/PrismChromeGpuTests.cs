using Cerneala.Drawing.MonoGame.Prism.Kernels;
using Cerneala.Drawing.Prism.Catalog;

namespace Cerneala.Tests.Drawing.MonoGame;

public sealed class PrismChromeGpuTests
{
    [Fact]
    public void RegistryRoutesChromeToDedicatedShaderTechnique()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        using PrismGraphExecutorTests.WindowsDxFixture fixture = new();
        using PrismKernelRegistry registry =
            new(fixture.Session.GraphicsDevice);

        Assert.True(
            registry.TryGetFilterKernel(
                PrismFilterId.Chrome,
                out PrismKernel kernel));
        Assert.Equal("ChromeFilter", kernel.Technique.Name);
    }
}
