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
public sealed class PrismCraquelureGpuTests
{
    [Fact]
    public void SelectorRoutesCraquelureToDedicatedKernel()
    {
        Assert.Equal(29, SdlGpuPrismKernelSelector.ResolveCatalogFilter(PrismFilterId.Craquelure));
    }

    [SdlNativeFact]
    public void WarpedVoronoiPassMatchesCpuAndPreservesAssociatedAlpha()
    {
        const int width = 40;
        const int height = 28;
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
            Assert.InRange(Math.Abs(gpu.X - (float)cpu.Red), 0, 0.035f);
            Assert.InRange(Math.Abs(gpu.Y - (float)cpu.Green), 0, 0.035f);
            Assert.InRange(Math.Abs(gpu.Z - (float)cpu.Blue), 0, 0.035f);
            Assert.InRange(Math.Abs(gpu.W - (float)cpu.Alpha), 0, 0.012f);
            Assert.InRange(gpu.X, 0, gpu.W + 0.012f);
            Assert.InRange(gpu.Y, 0, gpu.W + 0.012f);
            Assert.InRange(gpu.Z, 0, gpu.W + 0.012f);
        }
    }

    private static PrismCatalogFilterPlan CreatePlan() =>
        PrismCatalogFilterPlanner.Create(
            PrismFilterId.Craquelure,
            [
                Number(0, 8),
                Number(1, 7),
                Number(2, 11),
                Integer(3, 1937)
            ],
            PrismBlendMode.Normal,
            1,
            System.Numerics.Matrix3x2.Identity,
            new DrawRect(0, 0, 40, 28));

    private static PrismGraphParameter Number(int slot, float value) =>
        new(slot, PrismGraphParameterValueKind.Number, numberValue: value);

    private static PrismGraphParameter Integer(int slot, int value) =>
        new(slot, PrismGraphParameterValueKind.Integer, integerValue: value);

    private static Vector4[] CreateSubject(
        int width,
        int height)
    {
        Vector4[] pixels = new Vector4[width * height];
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float red = 0.42f + (0.18f * x / (width - 1f));
                float green = 0.38f + (0.16f * y / (height - 1f));
                float blue = 0.34f + (0.1f * x / (width - 1f));
                float alpha = 0.3f + (0.65f * y / (height - 1f));
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

    private static PrismPremultipliedColor ToPrismColor(
        Vector4 value)
    {
        Vector4 color = value;
        return new PrismPremultipliedColor(
            color.X,
            color.Y,
            color.Z,
            color.W);
    }
}
