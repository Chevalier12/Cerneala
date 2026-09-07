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
public sealed class PrismSolarizeGpuTests
{
    [SdlNativeFact]
    public void GpuMatchesClassicPerChannelHardThresholdAndCpuFallback()
    {
        const float alpha = 0.5f;
        using SdlPrismKernelFixture fixture = new();
        PrismCatalogFilterPlan plan = CreatePlan(0.25f);
        Assert.Single(plan.Passes);
        Vector4 gpu = Assert.Single(fixture.RunCatalog(plan,
            [new Vector4(0.125f * alpha, 0.25f * alpha, 0.75f * alpha, alpha)], 1, 1));
        PrismPremultipliedColor cpu = Assert.Single(
            PrismCatalogFilterMath.Apply(
                plan,
                [
                    PrismPremultipliedColor.FromStraight(
                        0.125,
                        0.25,
                        0.75,
                        alpha)
                ],
                1,
                1,
                PrismColorProfile.LinearSrgb));

        Assert.InRange(Math.Abs(gpu.X - 0.0625f), 0, 0.002f);
        Assert.InRange(Math.Abs(gpu.Y - 0.375f), 0, 0.002f);
        Assert.InRange(Math.Abs(gpu.Z - 0.125f), 0, 0.002f);
        Assert.InRange(Math.Abs(gpu.W - alpha), 0, 0.002f);
        Assert.InRange(Math.Abs(gpu.X - cpu.Red), 0, 0.002f);
        Assert.InRange(Math.Abs(gpu.Y - cpu.Green), 0, 0.002f);
        Assert.InRange(Math.Abs(gpu.Z - cpu.Blue), 0, 0.002f);
        Assert.InRange(Math.Abs(gpu.W - cpu.Alpha), 0, 0.002f);
    }

    private static PrismCatalogFilterPlan CreatePlan(float threshold) =>
        PrismCatalogFilterPlanner.Create(
            PrismFilterId.Solarize,
            [Number(0, threshold)],
            PrismBlendMode.Normal,
            1,
            System.Numerics.Matrix3x2.Identity,
            new DrawRect(0, 0, 1, 1));

    private static PrismGraphParameter Number(int slot, float value) =>
        new(slot, PrismGraphParameterValueKind.Number, numberValue: value);
}
