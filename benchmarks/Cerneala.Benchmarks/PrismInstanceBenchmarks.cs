using BenchmarkDotNet.Attributes;
using Cerneala.Drawing.Prism.Catalog;
using Cerneala.UI.Prism.Definitions;
using Cerneala.UI.Prism.Runtime;

namespace Cerneala.Benchmarks;

[MemoryDiagnoser]
public class PrismInstanceBenchmarks
{
    private PrismInstance instance = null!;
    private PrismFilterState blur = null!;
    private bool alternate;

    [GlobalSetup]
    public void Setup()
    {
        PrismCompositionDefinition definition = new(
            "benchmark",
            [
                new PrismLayerDefinition(
                    new PrismNodeId(1),
                    "content",
                    filters: [new PrismFilterDefinition(PrismFilterId.Blur)])
            ]);
        instance = new PrismInstance(definition);
        blur = instance.GetLayerState(new PrismNodeId(1)).Filters[0];
    }

    [Benchmark]
    public PrismValueVersion UpdateTypedParameter()
    {
        alternate = !alternate;
        blur.SetValue(
            PrismCatalogGenerated.PrismFilterParameterKeys.Blur.RadiusKey,
            alternate ? 8f : 12f);
        return instance.ValueVersion;
    }
}
