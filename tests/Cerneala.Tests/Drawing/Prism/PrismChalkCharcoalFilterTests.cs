using System.Numerics;
using Cerneala.Drawing;
using Cerneala.Drawing.Prism;
using Cerneala.Drawing.Prism.Catalog;
using Cerneala.Drawing.Prism.ColorManagement;
using Cerneala.Drawing.Prism.Filters;
using Cerneala.Drawing.Prism.Graph;
using Cerneala.UI.Prism.Definitions;

namespace Cerneala.Tests.Drawing.Prism;

public sealed class PrismChalkCharcoalFilterTests
{
    [Fact]
    public void PlannerBuildsDualGaussianAndCompositePasses()
    {
        PrismCatalogFilterPlan plan = CreatePlan(
            charcoalArea: 4,
            chalkArea: 8,
            strokePressure: 2,
            foreground: new Color(255, 16, 32, 48),
            background: new Color(255, 224, 208, 192));

        Assert.Equal(3, plan.Passes.Length);
        Assert.Equal(
            [
                PrismCatalogFilterPassKind.Horizontal,
                PrismCatalogFilterPassKind.Vertical,
                PrismCatalogFilterPassKind.Direct
            ],
            plan.Passes.Select(pass => pass.Kind));
        Assert.Equal([0, 1, 2], plan.Passes.Select(pass => pass.Iteration));
        Assert.Equal(1.5f, plan.Options5.X, 5);
        Assert.Equal(2.5f, plan.Options5.Y, 5);
        Assert.Equal(3, plan.Options5.Z, 5);
        Assert.Equal(5, plan.Options5.W, 5);
        Assert.Equal(5, plan.Passes[0].RadiusX);
        Assert.Equal(5, plan.Passes[1].RadiusY);
        Assert.False(PrismCatalogFilterPlanner.RequiresOriginalInput(
            plan.Filter,
            plan.Passes[1]));
        Assert.True(PrismCatalogFilterPlanner.RequiresOriginalInput(
            plan.Filter,
            plan.Passes[2]));
    }

    [Fact]
    public void CpuReferenceIsDeterministicAndHonorsEveryControl()
    {
        const int width = 33;
        const int height = 21;
        PrismPremultipliedColor[] source = CreateSubject(width, height);

        PrismPremultipliedColor[] baseline = Apply(
            CreatePlan(6, 6, 1, Color.Black, Color.White),
            source,
            width,
            height);
        PrismPremultipliedColor[] repeated = Apply(
            CreatePlan(6, 6, 1, Color.Black, Color.White),
            source,
            width,
            height);
        PrismPremultipliedColor[] broadCharcoal = Apply(
            CreatePlan(12, 6, 1, Color.Black, Color.White),
            source,
            width,
            height);
        PrismPremultipliedColor[] broadChalk = Apply(
            CreatePlan(6, 12, 1, Color.Black, Color.White),
            source,
            width,
            height);
        PrismPremultipliedColor[] pressed = Apply(
            CreatePlan(6, 6, 4, Color.Black, Color.White),
            source,
            width,
            height);
        PrismPremultipliedColor[] colored = Apply(
            CreatePlan(
                6,
                6,
                1,
                new Color(220, 24, 24),
                new Color(24, 208, 232)),
            source,
            width,
            height);

        Assert.Equal(baseline, repeated);
        Assert.True(MeanDifference(source, baseline) > 0.02);
        Assert.True(MeanDifference(baseline, broadCharcoal) > 0.001);
        Assert.True(MeanDifference(baseline, broadChalk) > 0.001);
        Assert.True(MeanDifference(baseline, pressed) > 0.001);
        Assert.True(MeanDifference(baseline, colored) > 0.05);
        Assert.True(DarkRegionMean(colored, width) > 0.15);
        Assert.True(LightRegionCyanMean(colored, width) > 0.35);

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
        float charcoalArea,
        float chalkArea,
        float strokePressure,
        Color foreground,
        Color background) =>
        PrismCatalogFilterPlanner.Create(
            PrismFilterId.ChalkCharcoal,
            [
                new PrismGraphParameter(
                    0,
                    PrismGraphParameterValueKind.Color,
                    colorValue: background),
                new PrismGraphParameter(
                    1,
                    PrismGraphParameterValueKind.Number,
                    numberValue: chalkArea),
                new PrismGraphParameter(
                    2,
                    PrismGraphParameterValueKind.Number,
                    numberValue: charcoalArea),
                new PrismGraphParameter(
                    3,
                    PrismGraphParameterValueKind.Color,
                    colorValue: foreground),
                new PrismGraphParameter(
                    4,
                    PrismGraphParameterValueKind.Number,
                    numberValue: strokePressure)
            ],
            PrismBlendMode.Normal,
            pixelScale: 1,
            Matrix3x2.Identity,
            new DrawRect(0, 0, 33, 21));

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
            for (int x = 0; x < width; x++)
            {
                double ramp = x / (double)(width - 1);
                double ridge = ((x / 5) + (y / 4)) % 2 == 0 ? 0.12 : -0.08;
                double value = Math.Clamp(ramp + ridge, 0.03, 0.97);
                double alpha = (x + y) % 9 == 0 ? 0.45 : 0.82;
                pixels[(y * width) + x] =
                    PrismPremultipliedColor.FromStraight(
                        value,
                        value * 0.92,
                        value * 0.78,
                        alpha);
            }
        }

        pixels[0] = default;
        return pixels;
    }

    private static double MeanDifference(
        PrismPremultipliedColor[] first,
        PrismPremultipliedColor[] second) =>
        first.Zip(second, (left, right) =>
                Math.Abs(left.Red - right.Red) +
                Math.Abs(left.Green - right.Green) +
                Math.Abs(left.Blue - right.Blue))
            .Average();

    private static double DarkRegionMean(
        PrismPremultipliedColor[] pixels,
        int width) =>
        pixels.Where((_, index) => index % width < width / 3)
            .Average(pixel => pixel.Alpha <= 0 ? 0 : pixel.Red / pixel.Alpha);

    private static double LightRegionCyanMean(
        PrismPremultipliedColor[] pixels,
        int width) =>
        pixels.Where((_, index) => index % width >= (2 * width) / 3)
            .Average(pixel => pixel.Alpha <= 0
                ? 0
                : ((pixel.Green + pixel.Blue) * 0.5) / pixel.Alpha);
}
