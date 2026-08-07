using System.Numerics;
using Cerneala.Drawing;
using Cerneala.Drawing.Prism;
using Cerneala.Drawing.Prism.Catalog;
using Cerneala.Drawing.Prism.ColorManagement;
using Cerneala.Drawing.Prism.Filters;
using Cerneala.Drawing.Prism.Graph;

namespace Cerneala.Tests.Drawing.Prism;

public sealed class PrismSolarizeFilterTests
{
    [Fact]
    public void PlannerPacksThresholdIntoTheFirstShaderOption()
    {
        PrismCatalogFilterPlan plan = CreatePlan(0.25f);

        Assert.Equal(PrismCatalogFilterPrimitive.Color, plan.Primitive);
        Assert.Equal(0.25f, plan.GetOption("Threshold").X);
        Assert.Equal(0.25f, plan.Options0.X);
        Assert.Single(plan.Passes);
    }

    [Fact]
    public void HardThresholdSolarizesStraightChannelsAndPreservesAlpha()
    {
        PrismPremultipliedColor source =
            PrismPremultipliedColor.FromStraight(
                0.125,
                0.25,
                0.75,
                0.5);

        PrismPremultipliedColor result = Assert.Single(
            PrismCatalogFilterMath.Apply(
                CreatePlan(0.25f),
                [source],
                1,
                1,
                PrismColorProfile.LinearSrgb));

        Assert.Equal(0.0625, result.Red, 6);
        Assert.Equal(0.375, result.Green, 6);
        Assert.Equal(0.125, result.Blue, 6);
        Assert.Equal(0.5, result.Alpha, 6);
    }

    [Fact]
    public void TransparentInputRemainsFiniteAndTransparent()
    {
        PrismPremultipliedColor result = Assert.Single(
            PrismCatalogFilterMath.Apply(
                CreatePlan(0.25f),
                [default],
                1,
                1,
                PrismColorProfile.LinearSrgb));

        Assert.Equal(default, result);
        Assert.True(double.IsFinite(result.Red));
        Assert.True(double.IsFinite(result.Green));
        Assert.True(double.IsFinite(result.Blue));
        Assert.True(double.IsFinite(result.Alpha));
    }

    private static PrismCatalogFilterPlan CreatePlan(float threshold) =>
        PrismCatalogFilterPlanner.Create(
            PrismFilterId.Solarize,
            [Number(0, threshold)],
            PrismBlendMode.Normal,
            1,
            Matrix3x2.Identity,
            new DrawRect(0, 0, 1, 1));

    private static PrismGraphParameter Number(int slot, float value) =>
        new(slot, PrismGraphParameterValueKind.Number, numberValue: value);
}
