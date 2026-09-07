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
public sealed class PrismMosaicTilesGpuTests
{
    [Fact]
    public void SelectorRoutesMosaicTilesToDedicatedKernel()
    {
        Assert.Equal(32, SdlGpuPrismKernelSelector.ResolveCatalogFilter(PrismFilterId.MosaicTiles));
    }

    [SdlNativeFact]
    public void MosaicTilesShaderMatchesCpuAndPreservesAssociatedAlpha()
    {
        const int width = 12;
        const int height = 8;
        using SdlPrismKernelFixture fixture = new();
        Vector4[] sourcePixels = CreateSubject(width, height);
        PrismCatalogFilterPlan plan = CreatePlan();
        Assert.Single(plan.Passes);
        Vector4[] gpuPixels = fixture.RunCatalog(plan, sourcePixels, width, height);
        PrismPremultipliedColor[] cpuPixels = PrismCatalogFilterMath.Apply(
            plan,
            sourcePixels.Select(ToPrismColor).ToArray(),
            width,
            height,
            PrismColorProfile.LinearSrgb);

        for (int index = 0; index < gpuPixels.Length; index++)
        {
            Vector4 gpu = gpuPixels[index];
            PrismPremultipliedColor cpu = cpuPixels[index];
            Assert.InRange(Math.Abs(gpu.X - (float)cpu.Red), 0, 0.006f);
            Assert.InRange(Math.Abs(gpu.Y - (float)cpu.Green), 0, 0.006f);
            Assert.InRange(Math.Abs(gpu.Z - (float)cpu.Blue), 0, 0.006f);
            Assert.InRange(Math.Abs(gpu.W - (float)cpu.Alpha), 0, 0.006f);
            Assert.InRange(gpu.X, 0, gpu.W + 0.006f);
            Assert.InRange(gpu.Y, 0, gpu.W + 0.006f);
            Assert.InRange(gpu.Z, 0, gpu.W + 0.006f);
        }
    }

    private static PrismCatalogFilterPlan CreatePlan() =>
        PrismCatalogFilterPlanner.Create(
            PrismFilterId.MosaicTiles,
            [
                Number(0, 2),
                Number(1, 7),
                Number(2, 4)
            ],
            PrismBlendMode.Normal,
            1,
            System.Numerics.Matrix3x2.Identity,
            new DrawRect(0, 0, 12, 8));

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
                float alpha = 0.25f + (0.7f * y / (height - 1f));
                float red = 0.1f + (0.75f * x / (width - 1f));
                float green = 0.15f + (0.7f * y / (height - 1f));
                float blue = 0.2f + (0.5f * (x + y) / (width + height - 2f));
                pixels[(y * width) + x] = SdlPrismKernelFixture.QuantizeHalf(new Vector4(
                    red * alpha,
                    green * alpha,
                    blue * alpha,
                    alpha));
            }
        }
        pixels[0] = default;
        return pixels;
    }

    private static PrismPremultipliedColor ToPrismColor(Vector4 value)
    {
        Vector4 color = value;
        return new PrismPremultipliedColor(
            color.X,
            color.Y,
            color.Z,
            color.W);
    }
}
