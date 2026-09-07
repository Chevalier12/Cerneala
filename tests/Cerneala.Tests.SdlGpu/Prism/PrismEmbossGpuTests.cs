using Cerneala.Drawing;
using Cerneala.Tests.SdlGpu;
using Cerneala.Drawing.Prism;
using Cerneala.Drawing.Prism.Catalog;
using Cerneala.Drawing.Prism.ColorManagement;
using Cerneala.Drawing.Prism.Filters;
using Cerneala.Drawing.Prism.Graph;
using System.Numerics;

namespace Cerneala.Tests.Drawing.SdlGpu;

[Collection(SdlNativeTestCollection.Name)]
public sealed class PrismEmbossGpuTests
{
    [SdlNativeFact]
    public void GpuReliefMatchesAnalyticCpuAtCenterAndPreservesAlpha()
    {
        const int width = 5;
        const int height = 5;
        const float alpha = 0.6f;
        using SdlPrismKernelFixture fixture = new();
        Vector4[] sourcePixels = Enumerable.Repeat(
            SdlPrismKernelFixture.QuantizeHalf(new Vector4(0, 0, 0, alpha)),
            width * height)
            .ToArray();
        sourcePixels[(1 * width) + 3] =
            SdlPrismKernelFixture.QuantizeHalf(new Vector4(alpha, alpha, alpha, alpha));

        PrismCatalogFilterPlan plan = CreatePlan();
        Assert.Single(plan.Passes);
        Vector4[] gpuPixels = fixture.RunCatalog(plan, sourcePixels, width, height);
        Vector4 gpu = gpuPixels[(2 * width) + 2];
        PrismPremultipliedColor[] cpuSource = sourcePixels
            .Select(pixel => pixel)
            .Select(pixel => new PrismPremultipliedColor(
                pixel.X,
                pixel.Y,
                pixel.Z,
                pixel.W))
            .ToArray();
        PrismPremultipliedColor cpu = PrismCatalogFilterMath.Apply(
            plan,
            cpuSource,
            width,
            height,
            PrismColorProfile.LinearSrgb)[(2 * width) + 2];

        Assert.InRange(Math.Abs(gpu.X - cpu.Red), 0, 0.002f);
        Assert.InRange(Math.Abs(gpu.Y - cpu.Green), 0, 0.002f);
        Assert.InRange(Math.Abs(gpu.Z - cpu.Blue), 0, 0.002f);
        Assert.InRange(Math.Abs(gpu.W - alpha), 0, 0.002f);
        Assert.InRange(
            Math.Abs(gpu.X - (alpha * (0.5f + (3f / 16)))),
            0,
            0.002f);
    }

    private static PrismCatalogFilterPlan CreatePlan() =>
        PrismCatalogFilterPlanner.Create(
            PrismFilterId.Emboss,
            [
                Number(0, 1),
                Number(1, 0),
                Number(2, 1)
            ],
            PrismBlendMode.Normal,
            1,
            System.Numerics.Matrix3x2.Identity,
            new DrawRect(0, 0, 5, 5));

    private static PrismGraphParameter Number(int slot, float value) =>
        new(slot, PrismGraphParameterValueKind.Number, numberValue: value);
}
