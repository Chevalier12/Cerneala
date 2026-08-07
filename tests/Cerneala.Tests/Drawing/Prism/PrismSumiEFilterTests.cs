using System.Numerics;
using Cerneala.Drawing;
using Cerneala.Drawing.Prism;
using Cerneala.Drawing.Prism.Catalog;
using Cerneala.Drawing.Prism.ColorManagement;
using Cerneala.Drawing.Prism.Filters;
using Cerneala.Drawing.Prism.Graph;
using Cerneala.UI.Prism.Definitions;

namespace Cerneala.Tests.Drawing.Prism;

public sealed class PrismSumiEFilterTests
{
    [Fact]
    public void PlannerBuildsDirectionalWashAndSeparableXDogPasses()
    {
        PrismCatalogFilterPlan plan = CreatePlan(
            strokeWidth: 10,
            strokePressure: 2,
            contrast: 2,
            pixelScale: 2,
            effectiveTransform: Matrix3x2.CreateScale(1.5f));

        Assert.Equal(3, plan.Passes.Length);
        Assert.Equal(
            [
                PrismCatalogFilterPassKind.Direct,
                PrismCatalogFilterPassKind.Horizontal,
                PrismCatalogFilterPassKind.Vertical
            ],
            plan.Passes.Select(pass => pass.Kind));
        Assert.Equal([0, 1, 2], plan.Passes.Select(pass => pass.Iteration));
        Assert.InRange(plan.Passes[0].RadiusX, 1, 6);
        Assert.True(plan.Options3.Y > plan.Options3.X);
        Assert.Equal(plan.Options3.W, plan.Passes[1].RadiusX);
        Assert.Equal(plan.Options3.W, plan.Passes[2].RadiusY);
        Assert.False(
            PrismCatalogFilterPlanner.RequiresOriginalInput(
                plan.Filter,
                plan.Passes[0]));
        Assert.False(
            PrismCatalogFilterPlanner.RequiresOriginalInput(
                plan.Filter,
                plan.Passes[1]));
        Assert.True(
            PrismCatalogFilterPlanner.RequiresOriginalInput(
                plan.Filter,
                plan.Passes[2]));
    }

    [Fact]
    public void CpuReferenceIsDeterministicTonalAndControlSensitive()
    {
        const int width = 31;
        const int height = 19;
        PrismPremultipliedColor[] source = CreateColoredRegions(width, height);
        PrismPremultipliedColor[] baseline = Apply(
            CreatePlan(10, 2, 2),
            source,
            width,
            height);
        PrismPremultipliedColor[] repeated = Apply(
            CreatePlan(10, 2, 2),
            source,
            width,
            height);
        PrismPremultipliedColor[] narrowBrush = Apply(
            CreatePlan(2, 2, 2),
            source,
            width,
            height);
        PrismPremultipliedColor[] heavyPressure = Apply(
            CreatePlan(10, 8, 2),
            source,
            width,
            height);
        PrismPremultipliedColor[] highContrast = Apply(
            CreatePlan(10, 2, 7),
            source,
            width,
            height);

        Assert.Equal(baseline, repeated);
        Assert.True(MeanDifference(source, baseline) > 0.05);
        Assert.True(MeanDifference(narrowBrush, baseline) > 0.001);
        Assert.True(MeanDifference(heavyPressure, baseline) > 0.001);
        Assert.True(MeanDifference(highContrast, baseline) > 0.001);
        Assert.True(
            MeanLuminance(heavyPressure) < MeanLuminance(baseline));
        Assert.True(
            BoundaryLuminance(baseline, width) <
            InteriorLuminance(baseline, width) - 0.005);
        Assert.True(MaximumChannelSpread(baseline) < 0.04);

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
        float strokeWidth,
        float strokePressure,
        float contrast,
        float pixelScale = 1,
        Matrix3x2? effectiveTransform = null) =>
        PrismCatalogFilterPlanner.Create(
            PrismFilterId.SumiE,
            [
                new PrismGraphParameter(
                    0,
                    PrismGraphParameterValueKind.Number,
                    numberValue: strokeWidth),
                new PrismGraphParameter(
                    1,
                    PrismGraphParameterValueKind.Number,
                    numberValue: strokePressure),
                new PrismGraphParameter(
                    2,
                    PrismGraphParameterValueKind.Number,
                    numberValue: contrast)
            ],
            PrismBlendMode.Normal,
            pixelScale,
            effectiveTransform ?? Matrix3x2.Identity,
            new DrawRect(0, 0, 31, 19));

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

    private static PrismPremultipliedColor[] CreateColoredRegions(
        int width,
        int height)
    {
        PrismPremultipliedColor[] pixels =
            new PrismPremultipliedColor[width * height];
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                bool left = x < width / 2;
                double grain = ((x + (y * 3)) & 1) == 0 ? -0.035 : 0.035;
                bool inkLine = x == width / 2;
                double red = inkLine
                    ? 0.015
                    : Math.Clamp(
                        (left ? 0.18 : 0.82) + grain,
                        0,
                        1);
                double green = inkLine
                    ? 0.02
                    : Math.Clamp(
                        (left ? 0.62 : 0.28) + grain,
                        0,
                        1);
                double blue = inkLine
                    ? 0.018
                    : Math.Clamp(
                        (left ? 0.78 : 0.12) + grain,
                        0,
                        1);
                double alpha = x == 0 && y == 0 ? 0 : 0.82;
                pixels[(y * width) + x] =
                    PrismPremultipliedColor.FromStraight(
                        red,
                        green,
                        blue,
                        alpha);
            }
        }
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

    private static double MeanLuminance(
        PrismPremultipliedColor[] pixels) =>
        pixels.Where(pixel => pixel.Alpha > 0).Average(Luminance);

    private static double BoundaryLuminance(
        PrismPremultipliedColor[] pixels,
        int width)
    {
        int boundary = width / 2;
        return pixels
            .Where((_, index) =>
                index % width == boundary - 1 ||
                index % width == boundary)
            .Average(Luminance);
    }

    private static double InteriorLuminance(
        PrismPremultipliedColor[] pixels,
        int width) =>
        pixels
            .Where((_, index) =>
                index % width is >= 3 and <= 10 ||
                index % width is >= 20 and <= 27)
            .Average(Luminance);

    private static double MaximumChannelSpread(
        PrismPremultipliedColor[] pixels) =>
        pixels
            .Where(pixel => pixel.Alpha > 0)
            .Max(pixel =>
            {
                double minimum = Math.Min(
                    pixel.Red,
                    Math.Min(pixel.Green, pixel.Blue));
                double maximum = Math.Max(
                    pixel.Red,
                    Math.Max(pixel.Green, pixel.Blue));
                return (maximum - minimum) / pixel.Alpha;
            });

    private static double Luminance(PrismPremultipliedColor pixel) =>
        pixel.Alpha <= 0
            ? 0
            : ((pixel.Red * 0.2126) +
                (pixel.Green * 0.7152) +
                (pixel.Blue * 0.0722)) /
            pixel.Alpha;
}
