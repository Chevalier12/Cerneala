using System.Numerics;
using Cerneala.Drawing;
using Cerneala.Drawing.Prism.Catalog;
using Cerneala.Drawing.Prism.ColorManagement;
using Cerneala.Drawing.Prism.Filters;
using Cerneala.Drawing.Prism.Graph;

namespace Cerneala.Tests.Drawing.Prism;

public sealed class PrismCraquelureFilterTests
{
    [Fact]
    public void PlannerCreatesSingleScaleAwareVoronoiPass()
    {
        PrismCatalogFilterPlan plan = CreatePlan(10, 6, 9, 17);
        PrismCatalogFilterPlan scaled = CreatePlan(
            10,
            6,
            9,
            17,
            pixelScale: 2);

        PrismCatalogFilterPass pass = Assert.Single(plan.Passes);
        Assert.Equal(PrismCatalogFilterPassKind.Direct, pass.Kind);
        Assert.Equal(0, pass.RadiusX);
        Assert.Equal(0, pass.RadiusY);
        AssertVector(plan.Options4, new Vector4(10, 0.11f, 0.6f, 0.9f));
        Assert.Equal(20, scaled.Options4.X, 0.00001f);
    }

    [Fact]
    public void EveryControlChangesItsOwnedResponseDeterministically()
    {
        const int width = 64;
        const int height = 48;
        PrismPremultipliedColor[] source = CreateSubject(width, height);
        PrismCatalogFilterPlan baseline = CreatePlan(10, 6, 9, 91);

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
        PrismPremultipliedColor[] spaced = Apply(
            CreatePlan(18, 6, 9, 91),
            source,
            width,
            height);
        PrismPremultipliedColor[] deep = Apply(
            CreatePlan(10, 10, 9, 91),
            source,
            width,
            height);
        PrismPremultipliedColor[] bright = Apply(
            CreatePlan(10, 6, 2, 91),
            source,
            width,
            height);
        PrismPremultipliedColor[] seeded = Apply(
            CreatePlan(10, 6, 9, 92),
            source,
            width,
            height);

        Assert.Equal(first, repeated);
        Assert.False(
            first.SequenceEqual(spaced),
            $"Baseline settings {baseline.Options4}; spaced settings " +
            $"{CreatePlan(18, 6, 9, 91).Options4}.");
        Assert.False(first.SequenceEqual(deep));
        Assert.False(first.SequenceEqual(bright));
        Assert.False(first.SequenceEqual(seeded));
    }

    [Fact]
    public void CracksHaveDarkCoresBrightRimsAndAssociatedAlpha()
    {
        const int width = 72;
        const int height = 48;
        PrismPremultipliedColor[] source = CreateSubject(width, height);
        PrismPremultipliedColor[] result = Apply(
            CreatePlan(12, 8, 10, 1234),
            source,
            width,
            height);
        int darker = 0;
        int brighter = 0;

        for (int index = 0; index < result.Length; index++)
        {
            Assert.Equal(source[index].Alpha, result[index].Alpha, 6);
            AssertFiniteAssociated(result[index]);
            if (result[index].Alpha <= 0)
            {
                continue;
            }

            double sourceRed = source[index].Red / source[index].Alpha;
            double resultRed = result[index].Red / result[index].Alpha;
            darker += resultRed < sourceRed - 0.08 ? 1 : 0;
            brighter += resultRed > sourceRed + 0.015 ? 1 : 0;
        }

        Assert.True(darker > result.Length / 20, $"Dark core count: {darker}.");
        Assert.True(brighter > result.Length / 50, $"Bright rim count: {brighter}.");
        Assert.Equal(default, result[0]);
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
        float spacing,
        float depth,
        float brightness,
        int seed,
        float pixelScale = 1) =>
        PrismCatalogFilterPlanner.Create(
            PrismFilterId.Craquelure,
            [
                Number(0, brightness),
                Number(1, depth),
                Number(2, spacing),
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

    private static PrismPremultipliedColor[] CreateSubject(
        int width,
        int height)
    {
        PrismPremultipliedColor[] source =
            new PrismPremultipliedColor[width * height];
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                double red = 0.42 + (0.18 * x / (width - 1d));
                double green = 0.38 + (0.16 * y / (height - 1d));
                double blue = 0.34 + (0.1 * x / (width - 1d));
                double alpha = 0.3 + (0.65 * y / (height - 1d));
                source[(y * width) + x] =
                    PrismPremultipliedColor.FromStraight(
                        red,
                        green,
                        blue,
                        alpha);
            }
        }
        source[0] = default;
        return source;
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

    private static void AssertVector(Vector4 actual, Vector4 expected)
    {
        Assert.Equal(expected.X, actual.X, 0.00001f);
        Assert.Equal(expected.Y, actual.Y, 0.00001f);
        Assert.Equal(expected.Z, actual.Z, 0.00001f);
        Assert.Equal(expected.W, actual.W, 0.00001f);
    }
}
