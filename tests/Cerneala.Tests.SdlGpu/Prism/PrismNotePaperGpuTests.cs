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
public sealed class PrismNotePaperGpuTests
{
    [Fact]
    public void SelectorRoutesNotePaperToDedicatedKernel()
    {
        Assert.Equal(25, SdlGpuPrismKernelSelector.ResolveCatalogFilter(PrismFilterId.NotePaper));
    }

    [SdlNativeFact]
    public void HeightFieldPipelineRendersPaperHolesAndPreservesAlpha()
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
        Assert.True(difference / resultPixels.Length > 0.08);
    }

    private static PrismCatalogFilterPlan CreatePlan() =>
        PrismCatalogFilterPlanner.Create(
            PrismFilterId.NotePaper,
            [
                ColorParameter(0, Cerneala.Drawing.Color.White),
                ColorParameter(1, Cerneala.Drawing.Color.Black),
                Number(2, 14),
                Number(3, 25),
                Number(4, 18)
            ],
            PrismBlendMode.Normal,
            1,
            System.Numerics.Matrix3x2.Identity,
            new DrawRect(0, 0, 32, 20));

    private static PrismGraphParameter Number(int slot, float value) =>
        new(slot, PrismGraphParameterValueKind.Number, numberValue: value);

    private static PrismGraphParameter ColorParameter(
        int slot,
        Cerneala.Drawing.Color value) =>
        new(
            slot,
            PrismGraphParameterValueKind.Color,
            colorValue: value);

    private static Vector4[] CreateSubject(
        int width,
        int height)
    {
        Vector4[] pixels = new Vector4[width * height];
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float value = x < width / 2
                    ? 0.12f + (y / (height - 1f) * 0.18f)
                    : 0.68f + (y / (height - 1f) * 0.2f);
                float alpha = 0.42f + (0.5f * x / (width - 1f));
                pixels[(y * width) + x] = SdlPrismKernelFixture.QuantizeHalf(new Vector4(
                    value * alpha,
                    value * 0.91f * alpha,
                    value * 0.76f * alpha,
                    alpha));
            }
        }
        return pixels;
    }
}
