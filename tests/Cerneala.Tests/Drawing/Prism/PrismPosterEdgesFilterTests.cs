using System.Numerics;
using Cerneala.Drawing;
using Cerneala.Drawing.Prism;
using Cerneala.Drawing.Prism.Catalog;
using Cerneala.Drawing.Prism.ColorManagement;
using Cerneala.Drawing.Prism.Filters;
using Cerneala.Drawing.Prism.Graph;
using Cerneala.UI.Prism.Definitions;

namespace Cerneala.Tests.Drawing.Prism;

public sealed class PrismPosterEdgesFilterTests
{
    [Fact]
    public void PlannerBuildsGuidedFilterAndCompositePasses()
    {
        PrismCatalogFilterPlan plan = CreatePlan(
            edgeThickness: 2,
            edgeIntensity: 1,
            posterization: 4,
            pixelScale: 2,
            effectiveTransform: Matrix3x2.CreateScale(1.5f));

        Assert.Equal(6, plan.Passes.Length);
        Assert.Equal(
            [
                PrismCatalogFilterPassKind.Horizontal,
                PrismCatalogFilterPassKind.Vertical,
                PrismCatalogFilterPassKind.Direct,
                PrismCatalogFilterPassKind.Horizontal,
                PrismCatalogFilterPassKind.Vertical,
                PrismCatalogFilterPassKind.Direct
            ],
            plan.Passes.Select(pass => pass.Kind));
        Assert.Equal(
            [0, 1, 2, 3, 4, 5],
            plan.Passes.Select(pass => pass.Iteration));
        Assert.Equal(2, plan.GetOption("EdgeThickness").X);
        Assert.Equal(1, plan.GetOption("EdgeIntensity").X);
        Assert.Equal(4, plan.GetOption("Posterization").X);
        Assert.Equal(6, plan.Passes[0].RadiusX);
        Assert.Equal(6, plan.Passes[1].RadiusY);
        Assert.Equal(6, plan.Passes[3].RadiusX);
        Assert.Equal(6, plan.Passes[4].RadiusY);
        Assert.Equal(6, plan.Passes[5].RadiusX);
        Assert.False(PrismCatalogFilterPlanner.RequiresOriginalInput(
            plan.Filter,
            plan.Passes[1]));
        Assert.True(PrismCatalogFilterPlanner.RequiresOriginalInput(
            plan.Filter,
            plan.Passes[2]));
        Assert.True(PrismCatalogFilterPlanner.RequiresOriginalInput(
            plan.Filter,
            plan.Passes[4]));
        Assert.True(PrismCatalogFilterPlanner.RequiresOriginalInput(
            plan.Filter,
            plan.Passes[5]));
    }

    [Fact]
    public void CpuReferenceIsDeterministicEdgeAwareAndControlSensitive()
    {
        const int width = 25;
        const int height = 17;
        PrismPremultipliedColor[] source = CreateNoisyEdge(width, height);
        PrismPremultipliedColor[] baseline = Apply(
            CreatePlan(2, 1, 4), source, width, height);
        PrismPremultipliedColor[] repeated = Apply(
            CreatePlan(2, 1, 4), source, width, height);
        PrismPremultipliedColor[] thick = Apply(
            CreatePlan(4, 1, 4), source, width, height);
        PrismPremultipliedColor[] noInk = Apply(
            CreatePlan(2, 0, 4), source, width, height);
        PrismPremultipliedColor[] coarse = Apply(
            CreatePlan(2, 1, 2), source, width, height);

        Assert.Equal(baseline, repeated);
        Assert.True(MeanDifference(source, baseline) > 0.001);
        Assert.True(MeanDifference(baseline, thick) > 0.0001);
        Assert.True(MeanDifference(baseline, noInk) > 0.0001);
        Assert.True(MeanDifference(baseline, coarse) > 0.0001);
        Assert.True(
            RegionDeviation(baseline, width, 3, 9) <
            RegionDeviation(source, width, 3, 9));
        Assert.True(
            BoundaryMean(baseline, width) <
            BoundaryMean(noInk, width));

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
        float edgeThickness,
        float edgeIntensity,
        float posterization,
        float pixelScale = 1,
        Matrix3x2? effectiveTransform = null) =>
        PrismCatalogFilterPlanner.Create(
            PrismFilterId.PosterEdges,
            [
                new PrismGraphParameter(
                    0,
                    PrismGraphParameterValueKind.Number,
                    numberValue: edgeIntensity),
                new PrismGraphParameter(
                    1,
                    PrismGraphParameterValueKind.Number,
                    numberValue: edgeThickness),
                new PrismGraphParameter(
                    2,
                    PrismGraphParameterValueKind.Number,
                    numberValue: posterization)
            ],
            PrismBlendMode.Normal,
            pixelScale,
            effectiveTransform ?? Matrix3x2.Identity,
            new DrawRect(0, 0, 25, 17));

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

    private static PrismPremultipliedColor[] CreateNoisyEdge(
        int width,
        int height)
    {
        PrismPremultipliedColor[] pixels =
            new PrismPremultipliedColor[width * height];
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                double baseValue = x < width / 2 ? 0.2 : 0.8;
                double noise = ((x + y) & 1) == 0 ? -0.04 : 0.04;
                double alpha = x == 0 && y == 0 ? 0 : 0.7;
                double value = Math.Clamp(baseValue + noise, 0, 1);
                pixels[(y * width) + x] =
                    PrismPremultipliedColor.FromStraight(
                        value,
                        value,
                        value,
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

    private static double BoundaryMean(
        PrismPremultipliedColor[] pixels,
        int width) =>
        pixels
            .Where((_, index) =>
                index % width is 11 or 12 or 13)
            .Average(Luminance);

    private static double RegionDeviation(
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
