using Cerneala.Drawing.Prism.Catalog;
using Cerneala.UI.Prism.Runtime;

namespace Cerneala.Tests.Drawing.Prism;

public sealed class PrismCloudsCatalogContractTests
{
    [Fact]
    public void SpectrumMatchesTheSymbolContractConsumedByThePlanner()
    {
        PrismCatalogPropertyDescriptor spectrum = PrismCatalogRuntime
            .GetEntry((int)PrismFilterId.Clouds)
            .Properties
            .Single(property => property.Name == "Spectrum");

        Assert.Equal(PrismCatalogValueType.Symbol, spectrum.ValueType);
    }
}
