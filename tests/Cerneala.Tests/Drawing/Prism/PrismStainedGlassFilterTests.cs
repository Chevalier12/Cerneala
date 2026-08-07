using System.Numerics;
using Cerneala.Drawing;
using Cerneala.Drawing.Prism.Catalog;
using Cerneala.Drawing.Prism.ColorManagement;
using Cerneala.Drawing.Prism.Filters;
using Cerneala.Drawing.Prism.Graph;

namespace Cerneala.Tests.Drawing.Prism;

public sealed class PrismStainedGlassFilterTests
{
    [Fact]
    public void PlannerCreatesSeedLogarithmicFloodAndCompositePasses()
    {
        PrismCatalogFilterPlan plan = CreatePlan(
            cellSize: 8,
            borderThickness: 1,
            lightIntensity: 3,
            borderColor: Color.Black,
            seed: 17,
            width: 64,
            height: 32);

        Assert.Equal(8, plan.Passes.Length);
        AssertPass(plan.Passes[0], PrismCatalogFilterPassKind.Direct, 0, 0);
        float[] expectedJumps = [32, 16, 8, 4, 2, 1];
        for (int index = 0; index < expectedJumps.Length; index++)
        {
            AssertPass(
                plan.Passes[index + 1],
                PrismCatalogFilterPassKind.Iteration,
                index + 1,
                expectedJumps[index]);
        }
        PrismCatalogFilterPass composite = plan.Passes[^1];
        AssertPass(composite, PrismCatalogFilterPassKind.Direct, 7, 1);
        Assert.True(
            PrismCatalogFilterPlanner.RequiresOriginalInput(
                PrismFilterId.StainedGlass,
                composite));
        Assert.Equal(8, plan.Options2.X, 5);
        Assert.Equal(1, plan.Options1.X, 5);
        Assert.Equal(3, plan.Options3.X, 5);
    }

    [Fact]
    public void PlannerScalesSpatialControlsAndPassCount()
    {
        PrismCatalogFilterPlan plan = CreatePlan(
            8,
            1.5f,
            3,
            Color.Black,
            17,
            64,
            32,
            pixelScale: 2);

        Assert.Equal(9, plan.Passes.Length);
        Assert.Equal(16, plan.Options2.X, 5);
        Assert.Equal(3, plan.Options1.X, 5);
        Assert.Equal(64, plan.Passes[1].RadiusX, 5);
    }

    [Fact]
    public void EveryControlOwnsADeterministicVisualResponse()
    {
        const int width = 64;
        const int height = 40;
        PrismPremultipliedColor[] source = CreateSubject(width, height);
        PrismCatalogFilterPlan baseline = CreatePlan(
            10, 1, 4, Color.Black, 91, width, height);

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
        PrismPremultipliedColor[] largerCells = Apply(
            CreatePlan(16, 1, 4, Color.Black, 91, width, height),
            source,
            width,
            height);
        PrismPremultipliedColor[] noBorder = Apply(
            CreatePlan(10, 0, 4, Color.Black, 91, width, height),
            source,
            width,
            height);
        PrismPremultipliedColor[] noLight = Apply(
            CreatePlan(10, 1, 0, Color.Black, 91, width, height),
            source,
            width,
            height);
        PrismPremultipliedColor[] redBorder = Apply(
            CreatePlan(10, 1, 4, new Color(220, 25, 20), 91, width, height),
            source,
            width,
            height);
        PrismPremultipliedColor[] reseeded = Apply(
            CreatePlan(10, 1, 4, Color.Black, 92, width, height),
            source,
            width,
            height);

        Assert.Equal(first, repeated);
        Assert.False(first.SequenceEqual(largerCells));
        Assert.False(first.SequenceEqual(noBorder));
        Assert.False(first.SequenceEqual(noLight));
        Assert.False(first.SequenceEqual(redBorder));
        Assert.False(first.SequenceEqual(reseeded));
    }

    [Fact]
    public void CompositePreservesSourceAlphaAndAssociatedColor()
    {
        const int width = 48;
        const int height = 32;
        PrismPremultipliedColor[] source = CreateSubject(width, height);
        PrismPremultipliedColor[] result = Apply(
            CreatePlan(9, 1, 5, Color.Black, 1234, width, height),
            source,
            width,
            height);

        for (int index = 0; index < result.Length; index++)
        {
            Assert.InRange(Math.Abs(source[index].Alpha - result[index].Alpha), 0d, 1e-6d);
            AssertFiniteAssociated(result[index]);
        }
    }

    private static void AssertPass(
        PrismCatalogFilterPass pass,
        PrismCatalogFilterPassKind kind,
        int iteration,
        float radius)
    {
        Assert.Equal(kind, pass.Kind);
        Assert.Equal(iteration, pass.Iteration);
        Assert.Equal(radius, pass.RadiusX, 5);
        Assert.Equal(radius, pass.RadiusY, 5);
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

    internal static PrismCatalogFilterPlan CreatePlan(
        float cellSize,
        float borderThickness,
        float lightIntensity,
        Color borderColor,
        int seed,
        int width,
        int height,
        float pixelScale = 1) =>
        PrismCatalogFilterPlanner.Create(
            PrismFilterId.StainedGlass,
            [
                ColorParameter(0, borderColor),
                Number(1, borderThickness),
                Number(2, cellSize),
                Number(3, lightIntensity),
                Integer(4, seed)
            ],
            PrismBlendMode.Normal,
            pixelScale,
            Matrix3x2.Identity,
            new DrawRect(0, 0, width, height));

    private static PrismGraphParameter Number(int slot, float value) =>
        new(slot, PrismGraphParameterValueKind.Number, numberValue: value);

    private static PrismGraphParameter ColorParameter(
        int slot,
        Color value) =>
        new(slot, PrismGraphParameterValueKind.Color, colorValue: value);

    private static PrismGraphParameter Integer(int slot, int value) =>
        new(slot, PrismGraphParameterValueKind.Integer, integerValue: value);

    internal static PrismPremultipliedColor[] CreateSubject(
        int width,
        int height)
    {
        PrismPremultipliedColor[] source =
            new PrismPremultipliedColor[width * height];
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                double red = 0.1 + (0.8 * x / (width - 1d));
                double green = 0.1 + (0.8 * y / (height - 1d));
                double blue = 0.15 +
                    (0.7 * ((x + y) % 11) / 10d);
                double alpha = 0.25 +
                    (0.7 * y / (height - 1d));
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
}
