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
public sealed class PrismReticulationGpuTests
{
    [Fact]
    public void SelectorRoutesReticulationToDedicatedKernel()
    {
        Assert.Equal(27, SdlGpuPrismKernelSelector.ResolveCatalogFilter(PrismFilterId.Reticulation));
    }

    [SdlNativeFact]
    public void WorleyPassMatchesCpuAndPreservesAssociatedAlpha()
    {
        const int width = 48;
        const int height = 32;
        using SdlPrismKernelFixture fixture = new();
        Vector4[] sourcePixels = CreateSubject(width, height);
        PrismCatalogFilterPlan plan = CreatePlan();
        Assert.Single(plan.Passes);
        Vector4[] gpuPixels = fixture.RunCatalog(plan, sourcePixels, width, height);
        PrismPremultipliedColor[] cpuSource = sourcePixels
            .Select(ToPrismColor)
            .ToArray();
        PrismPremultipliedColor[] cpuPixels = PrismCatalogFilterMath.Apply(
            plan,
            cpuSource,
            width,
            height,
            PrismColorProfile.LinearSrgb);

        for (int index = 0; index < gpuPixels.Length; index++)
        {
            Vector4 gpu = gpuPixels[index];
            PrismPremultipliedColor cpu = cpuPixels[index];
            Assert.InRange(Math.Abs(gpu.X - (float)cpu.Red), 0, 0.025f);
            Assert.InRange(Math.Abs(gpu.Y - (float)cpu.Green), 0, 0.025f);
            Assert.InRange(Math.Abs(gpu.Z - (float)cpu.Blue), 0, 0.025f);
            Assert.InRange(Math.Abs(gpu.W - (float)cpu.Alpha), 0, 0.012f);
            Assert.InRange(gpu.X, 0, gpu.W + 0.012f);
            Assert.InRange(gpu.Y, 0, gpu.W + 0.012f);
            Assert.InRange(gpu.Z, 0, gpu.W + 0.012f);
        }
    }

    private static PrismCatalogFilterPlan CreatePlan() =>
        PrismCatalogFilterPlanner.Create(
            PrismFilterId.Reticulation,
            [
                Number(0, 7),
                Number(1, 16),
                Number(2, 42),
                Integer(3, 1937)
            ],
            PrismBlendMode.Normal,
            1,
            System.Numerics.Matrix3x2.Identity,
            new DrawRect(0, 0, 48, 32));

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
                float luminance = 0.08f + (0.84f * x / (width - 1f));
                float alpha = 0.3f + (0.65f * y / (height - 1f));
                pixels[(y * width) + x] = SdlPrismKernelFixture.QuantizeHalf(new Vector4(
                    luminance * alpha,
                    luminance * alpha,
                    luminance * alpha,
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
