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
public sealed class PrismWaterPaperGpuTests
{
    [Fact]
    public void SelectorRoutesWaterPaperToDedicatedKernel()
    {
        Assert.Equal(13, SdlGpuPrismKernelSelector.ResolveCatalogFilter(PrismFilterId.WaterPaper));
    }

    [SdlNativeFact]
    public void PigmentAndSubstratePipelineChangesColorAndPreservesAlpha()
    {
        const int width = 32;
        const int height = 20;
        using SdlPrismKernelFixture fixture = new();
        Vector4[] sourcePixels = CreateSubject(width, height);
        PrismCatalogFilterPlan plan = CreatePlan();
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
        Assert.True(difference / resultPixels.Length > 0.05);
    }

    private static PrismCatalogFilterPlan CreatePlan() =>
        PrismCatalogFilterPlanner.Create(
            PrismFilterId.WaterPaper,
            [
                Number(0, 60),
                Number(1, 80),
                Number(2, 15),
                new PrismGraphParameter(
                    3,
                    PrismGraphParameterValueKind.Integer,
                    integerValue: 17)
            ],
            PrismBlendMode.Normal,
            1,
            System.Numerics.Matrix3x2.Identity,
            new DrawRect(0, 0, 32, 20));

    private static PrismGraphParameter Number(int slot, float value) =>
        new(slot, PrismGraphParameterValueKind.Number, numberValue: value);

    private static Vector4[] CreateSubject(
        int width,
        int height)
    {
        Vector4[] pixels = new Vector4[width * height];
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                bool left = x < width / 2;
                float alpha = 0.42f + (0.5f * x / (width - 1f));
                float red = left ? 0.24f : 0.72f;
                float green = left ? 0.38f : 0.3f;
                float blue = left ? 0.7f : 0.16f;
                pixels[(y * width) + x] = SdlPrismKernelFixture.QuantizeHalf(new Vector4(
                    red * alpha,
                    green * alpha,
                    blue * alpha,
                    alpha));
            }
        }
        return pixels;
    }
}
