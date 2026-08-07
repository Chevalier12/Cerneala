using System.Numerics;
using Cerneala.Drawing;
using Cerneala.Drawing.Prism.Catalog;
using Cerneala.Drawing.Prism.ColorManagement;
using Cerneala.Drawing.Prism.Filters;
using Cerneala.Drawing.Prism.Graph;

namespace Cerneala.Tests.Drawing.Prism;

public sealed class PrismReticulationFilterTests
{
    [Fact]
    public void PlannerCreatesSingleScaleAwareCellularPass()
    {
        PrismCatalogFilterPlan plan = CreatePlan(
            density: 12,
            foregroundLevel: 40,
            backgroundLevel: 5,
            seed: 17);
        PrismCatalogFilterPlan scaled = CreatePlan(
            density: 12,
            foregroundLevel: 40,
            backgroundLevel: 5,
            seed: 17,
            pixelScale: 2);

        PrismCatalogFilterPass pass = Assert.Single(plan.Passes);
        Assert.Equal(PrismCatalogFilterPassKind.Direct, pass.Kind);
        Assert.Equal(0, pass.RadiusX);
        Assert.Equal(0, pass.RadiusY);
        Assert.Equal(14.4f, plan.Options4.X, 5);
        Assert.Equal(0.8f, plan.Options4.Y, 5);
        Assert.Equal(0.1f, plan.Options4.Z, 5);
        Assert.Equal(28.8f, scaled.Options4.X, 5);
    }

    [Fact]
    public void EveryControlChangesItsOwnedCellularResponseDeterministically()
    {
        const int width = 72;
        const int height = 48;
        PrismPremultipliedColor[] source = CreateToneRamp(width, height);
        PrismCatalogFilterPlan baseline = CreatePlan(12, 40, 5, 91);

        PrismPremultipliedColor[] first = Apply(
            baseline,
            source,
            width,
            height);
        PrismPremultipliedColor[] repeated = Apply(
            baseline,
            source,
            width,
            height);
        PrismPremultipliedColor[] dense = Apply(
            CreatePlan(42, 40, 5, 91),
            source,
            width,
            height);
        PrismPremultipliedColor[] foreground = Apply(
            CreatePlan(12, 8, 5, 91),
            source,
            width,
            height);
        PrismPremultipliedColor[] background = Apply(
            CreatePlan(12, 40, 35, 91),
            source,
            width,
            height);
        PrismPremultipliedColor[] seeded = Apply(
            CreatePlan(12, 40, 5, 92),
            source,
            width,
            height);

        Assert.Equal(first, repeated);
        Assert.False(first.SequenceEqual(dense));
        Assert.False(first.SequenceEqual(foreground));
        Assert.False(first.SequenceEqual(background));
        Assert.False(first.SequenceEqual(seeded));
        Assert.True(
            CreatePlan(42, 40, 5, 91).Options4.X <
            baseline.Options4.X);
    }

    [Fact]
    public void ToneConditioningAggregatesShadowsAndPreservesAssociatedAlpha()
    {
        const int width = 64;
        const int height = 40;
        PrismPremultipliedColor[] source = CreateToneRamp(width, height);
        PrismPremultipliedColor[] result = Apply(
            CreatePlan(18, 50, 5, 1234),
            source,
            width,
            height);

        double shadowCoverage = ChangedFraction(
            source,
            result,
            width,
            1,
            width / 3);
        double highlightCoverage = ChangedFraction(
            source,
            result,
            width,
            (width * 2) / 3,
            width - 1);

        Assert.True(
            shadowCoverage > highlightCoverage * 1.2,
            $"Shadow coverage {shadowCoverage:F4}; " +
            $"highlight coverage {highlightCoverage:F4}.");
        Assert.Equal(default, result[0]);
        for (int index = 1; index < result.Length; index++)
        {
            Assert.Equal(source[index].Alpha, result[index].Alpha, 6);
            AssertFiniteAssociated(result[index]);
        }
    }

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

    private static PrismCatalogFilterPlan CreatePlan(
        float density,
        float foregroundLevel,
        float backgroundLevel,
        int seed,
        float pixelScale = 1) =>
        PrismCatalogFilterPlanner.Create(
            PrismFilterId.Reticulation,
            [
                Number(0, backgroundLevel),
                Number(1, density),
                Number(2, foregroundLevel),
                Integer(3, seed)
            ],
            PrismBlendMode.Normal,
            pixelScale,
            Matrix3x2.Identity,
            new DrawRect(0, 0, 72, 48));

    private static PrismGraphParameter Number(int slot, float value) =>
        new(slot, PrismGraphParameterValueKind.Number, numberValue: value);

    private static PrismGraphParameter Integer(int slot, int value) =>
        new(slot, PrismGraphParameterValueKind.Integer, integerValue: value);

    private static PrismPremultipliedColor[] CreateToneRamp(
        int width,
        int height)
    {
        PrismPremultipliedColor[] source =
            new PrismPremultipliedColor[width * height];
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                double luminance = 0.08 + (0.84 * x / (width - 1d));
                double alpha = 0.25 + (0.7 * y / (height - 1d));
                source[(y * width) + x] =
                    PrismPremultipliedColor.FromStraight(
                        luminance,
                        luminance,
                        luminance,
                        alpha);
            }
        }
        source[0] = default;
        return source;
    }

    private static double ChangedFraction(
        PrismPremultipliedColor[] source,
        PrismPremultipliedColor[] result,
        int width,
        int startX,
        int endX)
    {
        int changed = 0;
        int count = 0;
        for (int index = 0; index < source.Length; index++)
        {
            int x = index % width;
            if (x < startX || x > endX || source[index].Alpha <= 0)
            {
                continue;
            }

            double change = Math.Abs(
                (source[index].Red / source[index].Alpha) -
                (result[index].Red / result[index].Alpha));
            if (change > 0.01)
            {
                changed++;
            }
            count++;
        }
        return (double)changed / count;
    }

    private static void AssertFiniteAssociated(
        PrismPremultipliedColor color)
    {
        Assert.True(double.IsFinite(color.Red));
        Assert.True(double.IsFinite(color.Green));
        Assert.True(double.IsFinite(color.Blue));
        Assert.True(double.IsFinite(color.Alpha));
        Assert.InRange(color.Alpha, 0, 1);
        Assert.InRange(color.Red, 0, color.Alpha);
        Assert.InRange(color.Green, 0, color.Alpha);
        Assert.InRange(color.Blue, 0, color.Alpha);
    }
}
