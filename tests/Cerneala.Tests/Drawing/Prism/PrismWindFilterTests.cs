using System.Numerics;
using Cerneala.Drawing;
using Cerneala.Drawing.Prism;
using Cerneala.Drawing.Prism.Catalog;
using Cerneala.Drawing.Prism.ColorManagement;
using Cerneala.Drawing.Prism.Filters;
using Cerneala.Drawing.Prism.Graph;
using Cerneala.UI.Prism.Definitions;
using Cerneala.UI.Prism.Runtime;

namespace Cerneala.Tests.Drawing.Prism;

public sealed class PrismWindFilterTests
{
    [Fact]
    public void PlannerBuildsEnhancedLicPassesAndPreservesOriginalInput()
    {
        PrismCatalogFilterPlan plan = CreatePlan(
            "FromRight",
            strength: 4,
            seed: 17,
            pixelScale: 2,
            effectiveTransform: Matrix3x2.CreateScale(1.5f));

        Assert.Equal("LineIntegralConvolution", plan.Primitive.ToString());
        Assert.Equal(3, plan.Passes.Length);
        Assert.Equal([0, 1, 2], plan.Passes.Select(pass => pass.Iteration));
        Assert.Equal(
            [
                PrismCatalogFilterPassKind.Iteration,
                PrismCatalogFilterPassKind.Direct,
                PrismCatalogFilterPassKind.Iteration
            ],
            plan.Passes.Select(pass => pass.Kind));
        Assert.All(
            plan.Passes,
            pass => Assert.True(
                PrismCatalogFilterPlanner.RequiresOriginalInput(
                    plan.Filter,
                    pass)));
        Assert.Equal(12, plan.GetOption("Strength").X);
        Assert.Equal(0, plan.GetOption("Direction").X);
    }

    [Fact]
    public void CpuReferenceIsDeterministicSeededDirectionalAndAssociated()
    {
        const int width = 57;
        const int height = 31;
        PrismPremultipliedColor[] source = CreateImpulse(width, height);
        PrismPremultipliedColor[] fromRight = Apply(
            CreatePlan("FromRight", 4, 17),
            source,
            width,
            height);
        PrismPremultipliedColor[] repeated = Apply(
            CreatePlan("FromRight", 4, 17),
            source,
            width,
            height);
        PrismPremultipliedColor[] differentSeed = Apply(
            CreatePlan("FromRight", 4, 91),
            source,
            width,
            height);
        PrismPremultipliedColor[] fromLeft = Apply(
            CreatePlan("FromLeft", 4, 17),
            source,
            width,
            height);

        Assert.Equal(fromRight, repeated);
        Assert.True(MeanDifference(source, fromRight) > 0.002);
        Assert.True(MeanDifference(fromRight, differentSeed) > 0.00001);
        Assert.True(MeanDifference(fromRight, fromLeft) > 0.0001);
        Assert.True(
            SideEnergy(fromRight, width, height, left: true) >
            SideEnergy(fromRight, width, height, left: false));
        Assert.True(
            SideEnergy(fromLeft, width, height, left: false) >
            SideEnergy(fromLeft, width, height, left: true));
        Assert.All(fromRight, AssertFiniteAssociated);
        Assert.All(fromLeft, AssertFiniteAssociated);
    }

    [Fact]
    public void ZeroStrengthIsIdentity()
    {
        const int width = 17;
        const int height = 11;
        PrismPremultipliedColor[] source = CreateImpulse(width, height);

        PrismPremultipliedColor[] result = Apply(
            CreatePlan("FromRight", 0, 17),
            source,
            width,
            height);

        Assert.True(MeanDifference(source, result) < 0.0000001);
    }

    private static PrismCatalogFilterPlan CreatePlan(
        string direction,
        float strength,
        int seed,
        float pixelScale = 1,
        Matrix3x2? effectiveTransform = null) =>
        PrismCatalogFilterPlanner.Create(
            PrismFilterId.Wind,
            [
                Symbol(0, "Direction", direction),
                Symbol(1, "Method", "Wind"),
                new PrismGraphParameter(
                    2,
                    PrismGraphParameterValueKind.Integer,
                    integerValue: seed),
                new PrismGraphParameter(
                    3,
                    PrismGraphParameterValueKind.Number,
                    numberValue: strength)
            ],
            PrismBlendMode.Normal,
            pixelScale,
            effectiveTransform ?? Matrix3x2.Identity,
            new DrawRect(0, 0, 57, 31));

    private static PrismGraphParameter Symbol(
        int slot,
        string property,
        string value) =>
        new(
            slot,
            PrismGraphParameterValueKind.Symbol,
            integerValue: PrismCatalogRuntime.ResolveSymbol(property, value));

    private static PrismPremultipliedColor[] Apply(
        PrismCatalogFilterPlan plan,
        PrismPremultipliedColor[] source,
        int width,
        int height) =>
        PrismCatalogFilterMath.Apply(
            plan,
            source,
            width,
            height,
            PrismColorProfile.LinearSrgb);

    private static PrismPremultipliedColor[] CreateImpulse(
        int width,
        int height)
    {
        PrismPremultipliedColor[] pixels =
            new PrismPremultipliedColor[width * height];
        int center = width / 2;
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                double alpha = x is >= 1 and < 4
                    ? 0.35
                    : x == center
                        ? 0.95
                        : x == center + 1
                            ? 0.65
                            : 0.04;
                double stripe = x == center ? 0.95 : 0.08;
                double rowNoise = ((y * 13 + x * 7) & 7) / 100d;
                pixels[(y * width) + x] =
                    PrismPremultipliedColor.FromStraight(
                        Math.Clamp(stripe + rowNoise, 0, 1),
                        Math.Clamp((stripe * 0.45) + rowNoise, 0, 1),
                        Math.Clamp(0.12 + rowNoise, 0, 1),
                        alpha);
            }
        }
        return pixels;
    }

    private static double SideEnergy(
        PrismPremultipliedColor[] pixels,
        int width,
        int height,
        bool left)
    {
        int center = width / 2;
        int start = left ? center - 12 : center + 2;
        int end = left ? center - 1 : center + 13;
        double total = 0;
        for (int y = 0; y < height; y++)
        {
            for (int x = start; x <= end; x++)
            {
                PrismPremultipliedColor pixel = pixels[(y * width) + x];
                total += pixel.Red + pixel.Green + pixel.Blue;
            }
        }
        return total;
    }

    private static double MeanDifference(
        PrismPremultipliedColor[] first,
        PrismPremultipliedColor[] second) =>
        first.Zip(second, (left, right) =>
                Math.Abs(left.Red - right.Red) +
                Math.Abs(left.Green - right.Green) +
                Math.Abs(left.Blue - right.Blue) +
                Math.Abs(left.Alpha - right.Alpha))
            .Average();

    private static void AssertFiniteAssociated(
        PrismPremultipliedColor pixel)
    {
        Assert.True(double.IsFinite(pixel.Red));
        Assert.True(double.IsFinite(pixel.Green));
        Assert.True(double.IsFinite(pixel.Blue));
        Assert.True(double.IsFinite(pixel.Alpha));
        Assert.InRange(pixel.Alpha, 0, 1);
        Assert.InRange(pixel.Red, 0, pixel.Alpha + 0.000001);
        Assert.InRange(pixel.Green, 0, pixel.Alpha + 0.000001);
        Assert.InRange(pixel.Blue, 0, pixel.Alpha + 0.000001);
    }
}
