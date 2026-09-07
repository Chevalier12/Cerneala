using Cerneala.Backends.SdlGpu;
using Cerneala.Drawing.Prism.Catalog;

namespace Cerneala.Tests.Drawing.SdlGpu;

public sealed class PrismChromeGpuTests
{
    [Fact]
    public void SelectorRoutesChromeToDedicatedKernel()
    {
        Assert.Equal(24, SdlGpuPrismKernelSelector.ResolveCatalogFilter(PrismFilterId.Chrome));
    }
}
