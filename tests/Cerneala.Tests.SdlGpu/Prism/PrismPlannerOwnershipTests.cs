using Cerneala.Drawing.Prism.Filters;
using Cerneala.Drawing.Prism.Catalog;

namespace Cerneala.Tests.SdlGpu.Prism;

public sealed class PrismPlannerOwnershipTests
{
    [Fact]
    public void CatalogOwnershipKeepsCatalogFiltersEnabled() =>
        Assert.True(PrismCatalogFilterPlanner.IsSupported(PrismFilterId.Wind));

    [Fact]
    public void CatalogOwnershipKeepsNeighborhoodFiltersEnabled() =>
        Assert.True(PrismNeighborhoodPlanner.IsSupported(PrismFilterId.GaussianBlur));

    [Fact]
    public void CatalogOwnershipKeepsResamplingFiltersEnabled() =>
        Assert.True(PrismResamplingPlanner.IsSupported(PrismFilterId.Transform));
}
