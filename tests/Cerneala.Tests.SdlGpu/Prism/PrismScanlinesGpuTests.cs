using Cerneala.Drawing;
using Cerneala.Backends.SdlGpu;
using Cerneala.Drawing.Prism;
using Cerneala.Drawing.Prism.Catalog;
using Cerneala.Drawing.Prism.ColorManagement;
using Cerneala.Drawing.Prism.Filters;
using Cerneala.Drawing.Prism.Graph;
using System.Numerics;
using Cerneala.Tests.SdlGpu;

namespace Cerneala.Tests.Drawing.SdlGpu;

[Collection(SdlNativeTestCollection.Name)]
public sealed class PrismScanlinesGpuTests
{
    [SdlNativeFact]
    public void GpuMatchesGeneralizedGaussianCpuFallback()
    {
        const int width = 12;
        const int height = 20;
        using SdlPrismKernelFixture fixture = new();
        Vector4[] sourcePixels = CreateSource(width, height);
        PrismCatalogFilterPlan plan = CreatePlan(width, height);
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
            AssertClose(index, "R", gpu.X, cpu.Red);
            AssertClose(index, "G", gpu.Y, cpu.Green);
            AssertClose(index, "B", gpu.Z, cpu.Blue);
            AssertClose(index, "A", gpu.W, cpu.Alpha);
        }
    }

    private static void AssertClose(
        int index,
        string channel,
        double gpu,
        double cpu) =>
        Assert.True(
            Math.Abs(gpu - cpu) <= 0.004,
            $"Pixel {index} {channel}: GPU={gpu:F6}, CPU={cpu:F6}");

    private static Vector4[] CreateSource(int width, int height)
    {
        Vector4[] pixels = new Vector4[width * height];
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float alpha = 0.3f + (0.04f * x) + (0.01f * y);
                pixels[(y * width) + x] = SdlPrismKernelFixture.QuantizeHalf(new Vector4(
                    (0.15f + (0.05f * x)) * alpha,
                    (0.2f + (0.025f * y)) * alpha,
                    (0.8f - (0.04f * x)) * alpha,
                    alpha));
            }
        }
        return pixels;
    }

    private static PrismCatalogFilterPlan CreatePlan(
        int width,
        int height) =>
        PrismCatalogFilterPlanner.Create(
            PrismFilterId.Scanlines,
            [
                ColorParameter(0, new Cerneala.Drawing.Color(32, 96, 224, 180)),
                Number(1, 7.5f),
                Number(2, 0.72f),
                Number(3, 0.17f),
                Number(4, 0.65f),
                Number(5, 0.42f)
            ],
            PrismBlendMode.Normal,
            1,
            System.Numerics.Matrix3x2.Identity,
            new DrawRect(0, 0, width, height));

    private static PrismPremultipliedColor ToPrismColor(Vector4 value)
    {
        Vector4 color = value;
        return new PrismPremultipliedColor(
            color.X,
            color.Y,
            color.Z,
            color.W);
    }

    private static PrismGraphParameter Number(int slot, float value) =>
        new(slot, PrismGraphParameterValueKind.Number, numberValue: value);

    private static PrismGraphParameter ColorParameter(
        int slot,
        Cerneala.Drawing.Color value) =>
        new(slot, PrismGraphParameterValueKind.Color, colorValue: value);
}
