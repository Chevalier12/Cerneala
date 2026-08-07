using System.Numerics;
using Cerneala.Drawing;
using Cerneala.Drawing.Prism.Catalog;
using Cerneala.Drawing.Prism.ColorManagement;
using Cerneala.Drawing.Prism.Filters;
using Cerneala.Drawing.Prism.Graph;

namespace Cerneala.Tests.Drawing.Prism;

public sealed class PrismEmbossFilterTests
{
    [Fact]
    public void PlannerConvertsHeightToDevicePixelSamplingRadius()
    {
        PrismCatalogFilterPlan plan = CreatePlan(
            angle: 0,
            height: 2,
            amount: 1,
            pixelScale: 2,
            effectiveTransform: Matrix3x2.CreateScale(1.5f));

        PrismCatalogFilterPass pass = Assert.Single(plan.Passes);
        Assert.Equal(6, pass.RadiusX);
        Assert.Equal(6, pass.RadiusY);
        Assert.Equal(2, plan.GetOption("Height").X);
    }

    [Fact]
    public void CornerCoefficientDrivesDirectionalRelief()
    {
        const int width = 5;
        const int height = 5;
        PrismPremultipliedColor[] source = BlackSource(width, height, 0.6);
        source[(1 * width) + 3] =
            PrismPremultipliedColor.FromStraight(1, 1, 1, 0.6);

        PrismPremultipliedColor horizontal = ApplyCenter(
            CreatePlan(angle: 0, height: 1, amount: 1),
            source,
            width,
            height);
        PrismPremultipliedColor diagonal = ApplyCenter(
            CreatePlan(angle: 45, height: 1, amount: 1),
            source,
            width,
            height);

        Assert.Equal(0.6, horizontal.Alpha, 6);
        Assert.Equal(0.6 * (0.5 + (3d / 16)), horizontal.Red, 6);
        Assert.Equal(horizontal.Red, horizontal.Green, 6);
        Assert.Equal(horizontal.Red, horizontal.Blue, 6);
        Assert.Equal(0.6 * 0.5, diagonal.Red, 6);
    }

    [Fact]
    public void HeightAngleAndAmountOwnIndependentReliefControls()
    {
        const int width = 7;
        const int height = 7;
        PrismPremultipliedColor[] source = BlackSource(width, height, 1);
        source[(1 * width) + 5] =
            PrismPremultipliedColor.FromStraight(1, 1, 1, 1);

        PrismPremultipliedColor shortHeight = ApplyCenter(
            CreatePlan(angle: 0, height: 1, amount: 1),
            source,
            width,
            height);
        PrismPremultipliedColor tallHeight = ApplyCenter(
            CreatePlan(angle: 0, height: 2, amount: 1),
            source,
            width,
            height);
        PrismPremultipliedColor rotated = ApplyCenter(
            CreatePlan(angle: 90, height: 2, amount: 1),
            source,
            width,
            height);
        PrismPremultipliedColor reduced = ApplyCenter(
            CreatePlan(angle: 0, height: 2, amount: 0.5f),
            source,
            width,
            height);

        Assert.Equal(0.5, shortHeight.Red, 6);
        Assert.Equal(0.5 + (3d / 16), tallHeight.Red, 6);
        Assert.Equal(0.5 - (3d / 16), rotated.Red, 6);
        Assert.Equal(0.5 + (3d / 32), reduced.Red, 6);
    }

    private static PrismPremultipliedColor ApplyCenter(
        PrismCatalogFilterPlan plan,
        PrismPremultipliedColor[] source,
        int width,
        int height)
    {
        PrismPremultipliedColor[] result = PrismCatalogFilterMath.Apply(
            plan,
            source,
            width,
            height,
            PrismColorProfile.LinearSrgb);
        return result[((height / 2) * width) + (width / 2)];
    }

    private static PrismCatalogFilterPlan CreatePlan(
        float angle,
        float height,
        float amount,
        float pixelScale = 1,
        Matrix3x2? effectiveTransform = null) =>
        PrismCatalogFilterPlanner.Create(
            PrismFilterId.Emboss,
            [
                Number(0, amount),
                Number(1, angle),
                Number(2, height)
            ],
            PrismBlendMode.Normal,
            pixelScale,
            effectiveTransform ?? Matrix3x2.Identity,
            new DrawRect(0, 0, 7, 7));

    private static PrismGraphParameter Number(int slot, float value) =>
        new(slot, PrismGraphParameterValueKind.Number, numberValue: value);

    private static PrismPremultipliedColor[] BlackSource(
        int width,
        int height,
        double alpha) =>
        Enumerable.Repeat(
            PrismPremultipliedColor.FromStraight(0, 0, 0, alpha),
            width * height)
        .ToArray();
}
