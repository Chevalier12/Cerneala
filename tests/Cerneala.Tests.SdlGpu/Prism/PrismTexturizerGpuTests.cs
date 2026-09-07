using Cerneala.Drawing;
using Cerneala.Backends.SdlGpu;
using Cerneala.Tests.SdlGpu;
using Cerneala.Drawing.Prism;
using Cerneala.Drawing.Prism.Catalog;
using Cerneala.Drawing.Prism.ColorManagement;
using Cerneala.Drawing.Prism.Filters;
using Cerneala.Drawing.Prism.Graph;
using Cerneala.UI.Prism.Runtime;
using System.Numerics;

namespace Cerneala.Tests.Drawing.SdlGpu;

[Collection(SdlNativeTestCollection.Name)]
public sealed class PrismTexturizerGpuTests
{
    [Fact]
    public void SelectorRoutesTexturizerToDedicatedKernel()
    {
        Assert.Equal(30, SdlGpuPrismKernelSelector.ResolveCatalogFilter(PrismFilterId.Texturizer));
    }

    [SdlNativeFact]
    public void ScharrHeightLightingMatchesCpuAndPreservesAssociatedAlpha()
    {
        const int width = 36;
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
            Assert.InRange(Math.Abs(gpu.X - (float)cpu.Red), 0, 0.04f);
            Assert.InRange(Math.Abs(gpu.Y - (float)cpu.Green), 0, 0.04f);
            Assert.InRange(Math.Abs(gpu.Z - (float)cpu.Blue), 0, 0.04f);
            Assert.InRange(Math.Abs(gpu.W - (float)cpu.Alpha), 0, 0.012f);
            Assert.InRange(gpu.X, 0, gpu.W + 0.012f);
            Assert.InRange(gpu.Y, 0, gpu.W + 0.012f);
            Assert.InRange(gpu.Z, 0, gpu.W + 0.012f);
        }
    }

    private static PrismCatalogFilterPlan CreatePlan() =>
        PrismCatalogFilterPlanner.Create(
            PrismFilterId.Texturizer,
            [
                new(0, PrismGraphParameterValueKind.Boolean, booleanValue: false),
                Symbol(1, "LightDirection", "TopRight"),
                Number(2, 0.24f),
                Number(3, 1.35f),
                Symbol(4, "Texture", "Burlap"),
                new(5, PrismGraphParameterValueKind.Resource)
            ],
            PrismBlendMode.Normal,
            1,
            System.Numerics.Matrix3x2.Identity,
            new DrawRect(0, 0, 36, 28));

    private static PrismGraphParameter Number(int slot, float value) =>
        new(slot, PrismGraphParameterValueKind.Number, numberValue: value);

    private static PrismGraphParameter Symbol(
        int slot,
        string property,
        string value) =>
        new(
            slot,
            PrismGraphParameterValueKind.Symbol,
            integerValue: PrismCatalogRuntime.ResolveSymbol(property, value));

    private static Vector4[] CreateSubject(
        int width,
        int height)
    {
        Vector4[] pixels = new Vector4[width * height];
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float alpha = 0.3f + (0.65f * y / (height - 1f));
                pixels[(y * width) + x] = SdlPrismKernelFixture.QuantizeHalf(new Vector4(
                    (0.35f + (0.3f * x / (width - 1f))) * alpha,
                    (0.3f + (0.2f * y / (height - 1f))) * alpha,
                    0.28f * alpha,
                    alpha));
            }
        }
        pixels[0] = default;
        return pixels;
    }

    private static PrismPremultipliedColor ToPrismColor(Vector4 value)
    {
        Vector4 color = value;
        return new PrismPremultipliedColor(color.X, color.Y, color.Z, color.W);
    }
}
