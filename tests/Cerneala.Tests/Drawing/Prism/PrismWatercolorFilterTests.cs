using System.Numerics;
using Cerneala.Drawing;
using Cerneala.Drawing.Prism;
using Cerneala.Drawing.Prism.Catalog;
using Cerneala.Drawing.Prism.ColorManagement;
using Cerneala.Drawing.Prism.Filters;
using Cerneala.Drawing.Prism.Graph;
using Cerneala.UI.Prism.Definitions;

namespace Cerneala.Tests.Drawing.Prism;

public sealed class PrismWatercolorFilterTests
{
    [Fact]
    public void PlannerBuildsMeanShiftOpeningClosingAndCompositePasses()
    {
        PrismCatalogFilterPlan plan = CreatePlan(
            brushDetail: 8,
            shadowIntensity: 1,
            texture: 3,
            pixelScale: 2,
            effectiveTransform: Matrix3x2.CreateScale(1.5f));

        Assert.Equal(7, plan.Passes.Length);
        Assert.Equal(
            [
                PrismCatalogFilterPassKind.Iteration,
                PrismCatalogFilterPassKind.Iteration,
                PrismCatalogFilterPassKind.Direct,
                PrismCatalogFilterPassKind.Direct,
                PrismCatalogFilterPassKind.Direct,
                PrismCatalogFilterPassKind.Direct,
                PrismCatalogFilterPassKind.Direct
            ],
            plan.Passes.Select(pass => pass.Kind));
        Assert.Equal(
            [0, 1, 2, 3, 4, 5, 6],
            plan.Passes.Select(pass => pass.Iteration));
        Assert.Equal(6, plan.Passes[0].RadiusX);
        Assert.Equal(6, plan.Passes[1].RadiusY);
        Assert.Equal(3, plan.Passes[2].RadiusX);
        Assert.Equal(3, plan.Passes[5].RadiusY);
        Assert.Equal(3, plan.Passes[6].RadiusX);
        Assert.All(
            plan.Passes.Take(6),
            pass => Assert.False(
                PrismCatalogFilterPlanner.RequiresOriginalInput(
                    plan.Filter,
                    pass)));
        Assert.True(
            PrismCatalogFilterPlanner.RequiresOriginalInput(
                plan.Filter,
                plan.Passes[6]));
    }

    [Fact]
    public void CpuReferenceIsDeterministicAbstractedAndControlSensitive()
    {
        const int width = 33;
        const int height = 21;
        PrismPremultipliedColor[] source = CreateNoisyRegions(
            width,
            height);
        PrismPremultipliedColor[] baseline = Apply(
            CreatePlan(9, 1, 3),
            source,
            width,
            height);
        PrismPremultipliedColor[] repeated = Apply(
            CreatePlan(9, 1, 3),
            source,
            width,
            height);
        PrismPremultipliedColor[] coarse = Apply(
            CreatePlan(0, 1, 3),
            source,
            width,
            height);
        PrismPremultipliedColor[] noShadow = Apply(
            CreatePlan(9, 0, 0),
            source,
            width,
            height);
        PrismPremultipliedColor[] strongShadow = Apply(
            CreatePlan(9, 4, 0),
            source,
            width,
            height);
        PrismPremultipliedColor[] noTexture = Apply(
            CreatePlan(9, 1, 0),
            source,
            width,
            height);
        PrismPremultipliedColor[] heavyTexture = Apply(
            CreatePlan(9, 1, 10),
            source,
            width,
            height);

        Assert.Equal(baseline, repeated);
        Assert.True(MeanDifference(source, baseline) > 0.001);
        Assert.True(MeanDifference(baseline, coarse) > 0.0001);
        Assert.True(MeanDifference(noTexture, heavyTexture) > 0.0001);
        Assert.True(
            InteriorDeviation(noTexture, width, 3, 12) <
            InteriorDeviation(source, width, 3, 12) * 0.8);
        Assert.True(
            RegionMean(noTexture, width, 21, 29) -
            RegionMean(noTexture, width, 3, 12) >
            0.3);
        Assert.True(
            BoundaryMean(strongShadow, width) <
            BoundaryMean(noShadow, width) - 0.005);
        Assert.True(
            InteriorDeviation(heavyTexture, width, 3, 12) >
            InteriorDeviation(noTexture, width, 3, 12));

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
        float brushDetail,
        float shadowIntensity,
        float texture,
        float pixelScale = 1,
        Matrix3x2? effectiveTransform = null) =>
        PrismCatalogFilterPlanner.Create(
            PrismFilterId.Watercolor,
            [
                new PrismGraphParameter(
                    0,
                    PrismGraphParameterValueKind.Number,
                    numberValue: brushDetail),
                new PrismGraphParameter(
                    1,
                    PrismGraphParameterValueKind.Number,
                    numberValue: shadowIntensity),
                new PrismGraphParameter(
                    2,
                    PrismGraphParameterValueKind.Number,
                    numberValue: texture)
            ],
            PrismBlendMode.Normal,
            pixelScale,
            effectiveTransform ?? Matrix3x2.Identity,
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

    private static PrismPremultipliedColor[] CreateNoisyRegions(
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
                double noise = ((x + y) & 1) == 0 ? -0.06 : 0.06;
                double red = Math.Clamp(
                    (left ? 0.24 : 0.78) + noise,
                    0,
                    1);
                double green = Math.Clamp(
                    (left ? 0.16 : 0.68) + noise,
                    0,
                    1);
                double blue = Math.Clamp(
                    (left ? 0.1 : 0.52) + noise,
                    0,
                    1);
                double alpha = x == 0 && y == 0 ? 0 : 0.8;
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

    private static double RegionMean(
        PrismPremultipliedColor[] pixels,
        int width,
        int startX,
        int endX) =>
        pixels
            .Where((_, index) =>
                index % width >= startX &&
                index % width <= endX)
            .Average(Luminance);

    private static double BoundaryMean(
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

    private static double InteriorDeviation(
        PrismPremultipliedColor[] pixels,
        int width,
        int startX,
        int endX)
    {
        double[] values = pixels
            .Where((_, index) =>
                index % width >= startX &&
                index % width <= endX)
            .Select(Luminance)
            .ToArray();
        double mean = values.Average();
        return Math.Sqrt(values.Average(value =>
            (value - mean) * (value - mean)));
    }

    private static double Luminance(PrismPremultipliedColor pixel) =>
        pixel.Alpha <= 0
            ? 0
            : ((pixel.Red * 0.2126) +
                (pixel.Green * 0.7152) +
                (pixel.Blue * 0.0722)) /
            pixel.Alpha;
}
