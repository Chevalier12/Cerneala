using System.Collections.Immutable;
using Cerneala.Drawing;
using Cerneala.Backends.SdlGpu;
using Cerneala.Tests.SdlGpu;
using Cerneala.Drawing.Prism;
using Cerneala.Drawing.Prism.Catalog;
using Cerneala.Drawing.Prism.ColorManagement;
using Cerneala.Drawing.Prism.Filters;
using Cerneala.Drawing.Prism.Graph;
using System.Numerics;

namespace Cerneala.Tests.Drawing.SdlGpu;

[Collection(SdlNativeTestCollection.Name)]
public sealed class PrismPhotocopyGpuTests
{
    [Theory]
    [InlineData(PrismFilterId.Photocopy)]
    [InlineData(PrismFilterId.Stamp)]
    [InlineData(PrismFilterId.TornEdges)]
    public void SelectorRoutesXDogFiltersToDedicatedKernel(
        PrismFilterId filter)
    {
        Assert.Equal(26, SdlGpuPrismKernelSelector.ResolveCatalogFilter(filter));
    }

    [SdlNativeTheory]
    [InlineData(PrismFilterId.Photocopy)]
    [InlineData(PrismFilterId.Stamp)]
    [InlineData(PrismFilterId.TornEdges)]
    public void XDogPipelineRendersDarkMassesAndPreservesAssociatedAlpha(
        PrismFilterId filter)
    {
        const int width = 32;
        const int height = 20;
        using SdlPrismKernelFixture fixture = new();
        Vector4[] sourcePixels = CreateSubject(width, height);
        PrismCatalogFilterPlan plan = CreatePlan(filter);
        Vector4[] resultPixels = fixture.RunCatalog(plan, sourcePixels, width, height);
        double difference = 0;
        for (int index = 0; index < resultPixels.Length; index++)
        {
            Vector4 source = sourcePixels[index];
            Vector4 result = resultPixels[index];
            difference +=
                Math.Abs(source.X - result.X) +
                Math.Abs(source.Y - result.Y) +
                Math.Abs(source.Z - result.Z);
            Assert.InRange(Math.Abs(source.W - result.W), 0, 0.012f);
            Assert.InRange(result.X, 0, result.W + 0.012f);
            Assert.InRange(result.Y, 0, result.W + 0.012f);
            Assert.InRange(result.Z, 0, result.W + 0.012f);
        }
        Assert.True(difference / resultPixels.Length > 0.02);

        Vector4 dark = resultPixels[(height / 2 * width) + 4];
        Vector4 paper = resultPixels[(height / 2 * width) + width - 5];
        Assert.True(dark.X / dark.W < 0.3f);
        Assert.True(paper.X / paper.W > 0.8f);
    }

    private static PrismCatalogFilterPlan CreatePlan(PrismFilterId filter)
    {
        ImmutableArray<PrismGraphParameter> parameters = filter switch
        {
            PrismFilterId.Photocopy =>
            [
                ColorParameter(0, Cerneala.Drawing.Color.White),
                Number(1, 8),
                Number(2, 2),
                ColorParameter(3, Cerneala.Drawing.Color.Black)
            ],
            PrismFilterId.Stamp =>
            [
                ColorParameter(0, Cerneala.Drawing.Color.White),
                ColorParameter(1, Cerneala.Drawing.Color.Black),
                Number(2, 25),
                Number(3, 5)
            ],
            PrismFilterId.TornEdges =>
            [
                ColorParameter(0, Cerneala.Drawing.Color.White),
                Number(1, 17),
                ColorParameter(2, Cerneala.Drawing.Color.Black),
                Number(3, 25),
                Number(4, 4)
            ],
            _ => throw new ArgumentOutOfRangeException(nameof(filter))
        };
        return PrismCatalogFilterPlanner.Create(
            filter,
            parameters,
            PrismBlendMode.Normal,
            1,
            System.Numerics.Matrix3x2.Identity,
            new DrawRect(0, 0, 32, 20));
    }

    private static PrismGraphParameter Number(int slot, float value) =>
        new(slot, PrismGraphParameterValueKind.Number, numberValue: value);

    private static PrismGraphParameter ColorParameter(
        int slot,
        Cerneala.Drawing.Color value) =>
        new(slot, PrismGraphParameterValueKind.Color, colorValue: value);

    private static Vector4[] CreateSubject(
        int width,
        int height)
    {
        Vector4[] pixels = new Vector4[width * height];
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float value = x < width / 2 ? 0.04f : 0.96f;
                float alpha = 0.42f + (0.5f * x / (width - 1f));
                pixels[(y * width) + x] = SdlPrismKernelFixture.QuantizeHalf(new Vector4(
                    value * alpha,
                    value * alpha,
                    value * alpha,
                    alpha));
            }
        }
        return pixels;
    }
}
