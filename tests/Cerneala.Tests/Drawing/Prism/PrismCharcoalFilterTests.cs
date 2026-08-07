using System.Numerics;
using Cerneala.Drawing;
using Cerneala.Drawing.Prism;
using Cerneala.Drawing.Prism.Catalog;
using Cerneala.Drawing.Prism.ColorManagement;
using Cerneala.Drawing.Prism.Filters;
using Cerneala.Drawing.Prism.Graph;
using Cerneala.UI.Prism.Definitions;

namespace Cerneala.Tests.Drawing.Prism;

public sealed class PrismCharcoalFilterTests
{
    [Fact]
    public void PlannerBuildsEtfAndFdogPipeline()
    {
        PrismCatalogFilterPlan plan = CreatePlan(
            thickness: 1,
            detail: 5,
            balance: 50,
            Color.Black,
            Color.White);

        Assert.Equal(6, plan.Passes.Length);
        Assert.Equal(
            [0, 1, 2, 4, 5, 6],
            plan.Passes.Select(pass => pass.Iteration));
        Assert.Equal(
            [
                PrismCatalogFilterPassKind.Direct,
                PrismCatalogFilterPassKind.Iteration,
                PrismCatalogFilterPassKind.Iteration,
                PrismCatalogFilterPassKind.Direct,
                PrismCatalogFilterPassKind.Iteration,
                PrismCatalogFilterPassKind.Direct
            ],
            plan.Passes.Select(pass => pass.Kind));
        Assert.False(PrismCatalogFilterPlanner.RequiresOriginalInput(
            plan.Filter,
            plan.Passes[2]));
        Assert.True(PrismCatalogFilterPlanner.RequiresOriginalInput(
            plan.Filter,
            plan.Passes[3]));
        Assert.True(PrismCatalogFilterPlanner.RequiresOriginalInput(
            plan.Filter,
            plan.Passes[5]));
        Assert.InRange(plan.Options5.X, 0.5f, 4f);
        Assert.True(plan.Options5.Y > plan.Options5.X);
        Assert.InRange(plan.Options6.X, 2f, 5f);
        Assert.Equal(2, plan.Options6.Y);
    }

    [Fact]
    public void DetailControlsEtfRefinementCount()
    {
        PrismCatalogFilterPlan low = CreatePlan(
            1, 0, 50, Color.Black, Color.White);
        PrismCatalogFilterPlan high = CreatePlan(
            1, 10, 50, Color.Black, Color.White);

        Assert.Equal(5, low.Passes.Length);
        Assert.Equal(7, high.Passes.Length);
        Assert.Equal(1, low.Options6.Y);
        Assert.Equal(3, high.Options6.Y);
    }

    [Fact]
    public void CpuReferenceIsDeterministicAndHonorsEveryControl()
    {
        const int width = 43;
        const int height = 29;
        PrismPremultipliedColor[] source = CreateSubject(width, height);

        PrismPremultipliedColor[] baseline = Apply(
            CreatePlan(1, 5, 50, Color.Black, Color.White),
            source,
            width,
            height);
        PrismPremultipliedColor[] repeated = Apply(
            CreatePlan(1, 5, 50, Color.Black, Color.White),
            source,
            width,
            height);
        PrismPremultipliedColor[] thick = Apply(
            CreatePlan(3, 5, 50, Color.Black, Color.White),
            source,
            width,
            height);
        PrismPremultipliedColor[] detailed = Apply(
            CreatePlan(1, 9, 50, Color.Black, Color.White),
            source,
            width,
            height);
        PrismPremultipliedColor[] dark = Apply(
            CreatePlan(1, 5, 85, Color.Black, Color.White),
            source,
            width,
            height);
        PrismPremultipliedColor[] colored = Apply(
            CreatePlan(
                1,
                5,
                50,
                new Color(220, 28, 40),
                new Color(32, 216, 232)),
            source,
            width,
            height);

        Assert.Equal(baseline, repeated);
        Assert.True(MeanDifference(source, baseline) > 0.02);
        Assert.True(MeanDifference(baseline, thick) > 0.003);
        Assert.True(MeanDifference(baseline, detailed) > 0.001);
        Assert.True(MeanDifference(baseline, dark) > 0.01);
        Assert.True(MeanDifference(baseline, colored) > 0.05);

        for (int index = 0; index < source.Length; index++)
        {
            PrismPremultipliedColor pixel = baseline[index];
            Assert.Equal(source[index].Alpha, pixel.Alpha, 5);
            Assert.True(double.IsFinite(pixel.Red));
            Assert.InRange(pixel.Red, 0, pixel.Alpha);
            Assert.InRange(pixel.Green, 0, pixel.Alpha);
            Assert.InRange(pixel.Blue, 0, pixel.Alpha);
        }
    }

    private static PrismCatalogFilterPlan CreatePlan(
        float thickness,
        float detail,
        float balance,
        Color foreground,
        Color background) =>
        PrismCatalogFilterPlanner.Create(
            PrismFilterId.Charcoal,
            [
                new PrismGraphParameter(
                    0,
                    PrismGraphParameterValueKind.Color,
                    colorValue: background),
                new PrismGraphParameter(
                    1,
                    PrismGraphParameterValueKind.Number,
                    numberValue: thickness),
                new PrismGraphParameter(
                    2,
                    PrismGraphParameterValueKind.Number,
                    numberValue: detail),
                new PrismGraphParameter(
                    3,
                    PrismGraphParameterValueKind.Color,
                    colorValue: foreground),
                new PrismGraphParameter(
                    4,
                    PrismGraphParameterValueKind.Number,
                    numberValue: balance)
            ],
            PrismBlendMode.Normal,
            pixelScale: 1,
            Matrix3x2.Identity,
            new DrawRect(0, 0, 43, 29));

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

    private static PrismPremultipliedColor[] CreateSubject(
        int width,
        int height)
    {
        PrismPremultipliedColor[] pixels =
            new PrismPremultipliedColor[width * height];
        for (int y = 0; y < height; y++)
        {
            float curve =
                (width * 0.48f) +
                (MathF.Sin(y * 0.3f) * width * 0.12f);
            for (int x = 0; x < width; x++)
            {
                double ramp = x / (double)(width - 1);
                double side = x < curve ? 0.18 : 0.78;
                double texture = ((x * 13) + (y * 7)) % 11 / 80.0;
                double value = Math.Clamp(
                    (side * 0.72) + (ramp * 0.28) + texture,
                    0.02,
                    0.98);
                double alpha = (x + y) % 13 == 0 ? 0.42 : 0.86;
                pixels[(y * width) + x] =
                    PrismPremultipliedColor.FromStraight(
                        value,
                        value * 0.94,
                        value * 0.82,
                        alpha);
            }
        }

        pixels[0] = default;
        return pixels;
    }

    private static double MeanDifference(
        PrismPremultipliedColor[] first,
        PrismPremultipliedColor[] second) =>
        first.Zip(
                second,
                (left, right) =>
                    Math.Abs(left.Red - right.Red) +
                    Math.Abs(left.Green - right.Green) +
                    Math.Abs(left.Blue - right.Blue))
            .Average();
}
