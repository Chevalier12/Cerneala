using Cerneala.Drawing;
using Cerneala.Backends.SdlGpu;
using Cerneala.Tests.SdlGpu;
using Cerneala.Drawing.Prism;
using Cerneala.Drawing.Prism.Catalog;
using Cerneala.Drawing.Prism.ColorManagement;
using Cerneala.Drawing.Prism.Filters;
using Cerneala.Drawing.Prism.Graph;
using Cerneala.UI.Prism.Definitions;
using Cerneala.UI.Prism.Runtime;
using System.Numerics;

namespace Cerneala.Tests.Drawing.SdlGpu;

[Collection(SdlNativeTestCollection.Name)]
public sealed class PrismGraphicPenGpuTests
{
    [SdlNativeFact]
    public void FlowXDogPipelineRendersWithDedicatedGraphicPenTechnique()
    {
        const int width = 32;
        const int height = 24;
        using SdlPrismKernelFixture fixture = new();
        Vector4[] sourcePixels = CreateSubject(width, height);
        PrismCatalogFilterPlan plan = CreatePlan();
        Vector4[] resultPixels = fixture.RunCatalog(plan, sourcePixels, width, height);
        Vector4[] flowPixels = fixture.RunCatalog(plan, sourcePixels, width, height,
            passCount: plan.Passes.TakeWhile(pass => pass.Iteration <= 5).Count());
        Assert.Equal(Vector4.One, plan.Options0);
        Assert.Equal(68, plan.Options2.X);
        Assert.Equal(12, plan.Options4.X);
        Assert.NotNull(flowPixels);
        double meanFlowResponse = flowPixels
            .Select(pixel => pixel)
            .Average(pixel => Math.Abs((pixel.Z * 2) - 1));
        Assert.InRange(meanFlowResponse, 0, 0.35);
        double difference = 0;
        PrismPremultipliedColor[] cpuSource = sourcePixels
            .Select(pixel => pixel)
            .Select(pixel => new PrismPremultipliedColor(
                pixel.X,
                pixel.Y,
                pixel.Z,
                pixel.W))
            .ToArray();
        PrismPremultipliedColor[] cpuResult = PrismCatalogFilterMath.Apply(
            plan,
            cpuSource,
            width,
            height,
            PrismColorProfile.LinearSrgb);
        double cpuInk = 0;
        double gpuInk = 0;
        for (int index = 0; index < resultPixels.Length; index++)
        {
            Vector4 source = sourcePixels[index];
            Vector4 result = resultPixels[index];
            difference +=
                Math.Abs(source.X - result.X) +
                Math.Abs(source.Y - result.Y) +
                Math.Abs(source.Z - result.Z);
            if (result.W > 0.0001f)
            {
                cpuInk += 1 -
                    (((cpuResult[index].Red * 0.2126) +
                        (cpuResult[index].Green * 0.7152) +
                        (cpuResult[index].Blue * 0.0722)) /
                    cpuResult[index].Alpha);
                gpuInk += 1 -
                    (((result.X * 0.2126) +
                        (result.Y * 0.7152) +
                        (result.Z * 0.0722)) /
                    result.W);
            }
            Assert.InRange(Math.Abs(source.W - result.W), 0, 0.01f);
            Assert.InRange(result.X, 0, result.W + 0.01f);
            Assert.InRange(result.Y, 0, result.W + 0.01f);
            Assert.InRange(result.Z, 0, result.W + 0.01f);
        }

        Assert.True(difference / resultPixels.Length > 0.04);
        double cpuInkMean = cpuInk / resultPixels.Length;
        double gpuInkMean = gpuInk / resultPixels.Length;
        Assert.True(
            Math.Abs(cpuInkMean - gpuInkMean) <= 0.12,
            $"CPU ink {cpuInkMean:F4}, GPU ink {gpuInkMean:F4}, " +
            $"flow response {meanFlowResponse:F4}.");
    }

    private static PrismCatalogFilterPlan CreatePlan() =>
        PrismCatalogFilterPlanner.Create(
            PrismFilterId.GraphicPen,
            [
                ColorParameter(0, Cerneala.Drawing.Color.White),
                ColorParameter(1, Cerneala.Drawing.Color.Black),
                Number(2, 68),
                Symbol(3, "StrokeDirection", "LeftDiagonal"),
                Number(4, 12)
            ],
            PrismBlendMode.Normal,
            1,
            System.Numerics.Matrix3x2.Identity,
            new DrawRect(0, 0, 32, 24));

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
                float edge = x < width / 2 ? 0.12f : 0.8f;
                float grain = ((x * 7) + (y * 11)) % 17 / 120f;
                float value = Math.Clamp(edge + grain, 0, 1);
                float alpha = (x + y) % 9 == 0 ? 0.45f : 0.86f;
                pixels[(y * width) + x] = SdlPrismKernelFixture.QuantizeHalf(new Vector4(
                    value * alpha,
                    value * 0.92f * alpha,
                    value * 0.74f * alpha,
                    alpha));
            }
        }

        return pixels;
    }
}
