using System.Numerics;
using Cerneala.Drawing;
using Cerneala.Drawing.Prism;
using Cerneala.Drawing.Prism.Catalog;
using Cerneala.Drawing.Prism.ColorManagement;
using Cerneala.Drawing.Prism.Filters;
using Cerneala.Drawing.Prism.Graph;
using Cerneala.UI.Prism.Definitions;

namespace Cerneala.Tests.Drawing.Prism;

public sealed class PrismCrosshatchFilterTests
{
    [Fact]
    public void PlannerScalesStrokeLengthAndNeedsNoNeighborhood()
    {
        PrismCatalogFilterPlan plan = CreatePlan(
            strokeLength: 9,
            sharpness: 6,
            strength: 1,
            pixelScale: 2,
            effectiveTransform: Matrix3x2.CreateScale(1.5f));

        Assert.Equal(27, plan.Options0.X);
        Assert.Single(plan.Passes);
        Assert.Equal(0, plan.Passes[0].RadiusX);
        Assert.Equal(0, plan.Passes[0].RadiusY);
    }

    [Fact]
    public void ZeroStrengthPreservesTheSource()
    {
        const int width = 19;
        const int height = 13;
        PrismPremultipliedColor[] source = CreateToneBands(width, height);
        PrismCatalogFilterPlan plan = CreatePlan(9, 6, 0);

        Assert.Equal(0, plan.Options2.X);

        PrismPremultipliedColor[] result = Apply(
            plan,
            source,
            width,
            height);

        for (int index = 0; index < source.Length; index++)
        {
            Assert.True(
                Math.Abs(source[index].Red - result[index].Red) < 0.00001 &&
                Math.Abs(source[index].Green - result[index].Green) < 0.00001 &&
                Math.Abs(source[index].Blue - result[index].Blue) < 0.00001 &&
                Math.Abs(source[index].Alpha - result[index].Alpha) < 0.00001,
                $"Pixel {index}: expected {source[index]}, actual {result[index]}.");
        }
    }

    [Fact]
    public void CpuReferenceProducesNeutralNestedFineToneHatching()
    {
        const int width = 41;
        const int height = 25;
        PrismPremultipliedColor[] result = Apply(
            CreatePlan(9, 6, 1),
            CreateToneBands(width, height),
            width,
            height);

        Assert.All(
            result,
            pixel =>
            {
                Assert.Equal(pixel.Red, pixel.Green, 5);
                Assert.Equal(pixel.Green, pixel.Blue, 5);
                Assert.InRange(pixel.Red, 0, pixel.Alpha);
            });

        double darkMean = RegionMean(result, width, 0, (width / 2) - 1);
        double lightMean = RegionMean(result, width, width / 2, width - 1);
        Assert.True(darkMean < lightMean - 0.15);
        Assert.True(RegionRange(result, width, 0, (width / 2) - 1) > 0.2);
        Assert.True(RegionRange(result, width, width / 2, width - 1) > 0.05);
    }

    [Fact]
    public void StrokeLengthAndSharpnessIndependentlyControlThePattern()
    {
        const int width = 47;
        const int height = 31;
        PrismPremultipliedColor[] source = Enumerable
            .Repeat(
                PrismPremultipliedColor.FromStraight(0.45, 0.45, 0.45, 1),
                width * height)
            .ToArray();
        PrismPremultipliedColor[] baseline = Apply(
            CreatePlan(9, 6, 1),
            source,
            width,
            height);
        PrismPremultipliedColor[] longer = Apply(
            CreatePlan(14, 6, 1),
            source,
            width,
            height);
        PrismPremultipliedColor[] softer = Apply(
            CreatePlan(9, 1, 1),
            source,
            width,
            height);

        Assert.True(MeanDifference(baseline, longer) > 0.02);
        Assert.True(MeanDifference(baseline, softer) > 0.005);
    }

    private static PrismCatalogFilterPlan CreatePlan(
        float strokeLength,
        float sharpness,
        float strength,
        float pixelScale = 1,
        Matrix3x2? effectiveTransform = null) =>
        PrismCatalogFilterPlanner.Create(
            PrismFilterId.Crosshatch,
            [
                new PrismGraphParameter(
                    0,
                    PrismGraphParameterValueKind.Number,
                    numberValue: strokeLength),
                new PrismGraphParameter(
                    1,
                    PrismGraphParameterValueKind.Number,
                    numberValue: sharpness),
                new PrismGraphParameter(
                    2,
                    PrismGraphParameterValueKind.Number,
                    numberValue: strength)
            ],
            PrismBlendMode.Normal,
            pixelScale,
            effectiveTransform ?? Matrix3x2.Identity,
            new DrawRect(0, 0, 47, 31));

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

    private static PrismPremultipliedColor[] CreateToneBands(
        int width,
        int height)
    {
        PrismPremultipliedColor[] pixels =
            new PrismPremultipliedColor[width * height];
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                double tone = x < width / 2 ? 0.2 : 0.75;
                double alpha = x == 0 && y == 0 ? 0 : 0.8;
                pixels[(y * width) + x] =
                    PrismPremultipliedColor.FromStraight(
                        tone,
                        tone,
                        tone,
                        alpha);
            }
        }
        return pixels;
    }

    private static double RegionMean(
        PrismPremultipliedColor[] pixels,
        int width,
        int startX,
        int endX) =>
        pixels
            .Where((_, index) =>
                index % width >= startX &&
                index % width <= endX &&
                pixels[index].Alpha > 0)
            .Average(StraightTone);

    private static double RegionRange(
        PrismPremultipliedColor[] pixels,
        int width,
        int startX,
        int endX)
    {
        double[] tones = pixels
            .Where((_, index) =>
                index % width >= startX &&
                index % width <= endX &&
                pixels[index].Alpha > 0)
            .Select(StraightTone)
            .ToArray();
        return tones.Max() - tones.Min();
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

    private static double StraightTone(PrismPremultipliedColor pixel) =>
        pixel.Red / pixel.Alpha;
}
