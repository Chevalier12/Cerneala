using System.Numerics;
using Cerneala.Drawing;
using Cerneala.Drawing.Prism;
using Cerneala.Drawing.Prism.Catalog;
using Cerneala.Drawing.Prism.ColorManagement;
using Cerneala.Drawing.Prism.Filters;
using Cerneala.Drawing.Prism.Graph;
using Cerneala.UI.Prism.Definitions;

namespace Cerneala.Tests.Drawing.Prism;

public sealed class PrismFrescoFilterTests
{
    [Fact]
    public void PlannerBuildsTensorTensorBlurAndKuwaharaPasses()
    {
        PrismCatalogFilterPlan plan = CreatePlan(
            brushSize: 3,
            brushDetail: 8,
            texture: 2,
            pixelScale: 2,
            effectiveTransform: Matrix3x2.CreateScale(1.5f));

        Assert.Equal(4, plan.Passes.Length);
        Assert.Equal(
            [
                PrismCatalogFilterPassKind.Direct,
                PrismCatalogFilterPassKind.Horizontal,
                PrismCatalogFilterPassKind.Vertical,
                PrismCatalogFilterPassKind.Iteration
            ],
            plan.Passes.Select(pass => pass.Kind));
        Assert.Equal([0, 1, 2, 3],
            plan.Passes.Select(pass => pass.Iteration));
        Assert.Equal(3, plan.Passes[0].RadiusX);
        Assert.Equal(3, plan.Passes[1].RadiusX);
        Assert.Equal(3, plan.Passes[2].RadiusY);
        Assert.Equal(6, plan.Passes[3].RadiusX);
        Assert.False(PrismCatalogFilterPlanner.RequiresOriginalInput(
            plan.Filter,
            plan.Passes[2]));
        Assert.True(PrismCatalogFilterPlanner.RequiresOriginalInput(
            plan.Filter,
            plan.Passes[3]));
    }

    [Fact]
    public void CpuReferenceIsDeterministicEdgeAwareAndControlSensitive()
    {
        const int width = 25;
        const int height = 17;
        PrismPremultipliedColor[] source = CreateNoisyEdge(
            width,
            height);
        PrismPremultipliedColor[] baseline = Apply(
            CreatePlan(3, 8, 0),
            source,
            width,
            height);
        PrismPremultipliedColor[] repeated = Apply(
            CreatePlan(3, 8, 0),
            source,
            width,
            height);
        PrismPremultipliedColor[] smallBrush = Apply(
            CreatePlan(1, 8, 0),
            source,
            width,
            height);
        PrismPremultipliedColor[] softDetail = Apply(
            CreatePlan(3, 2, 0),
            source,
            width,
            height);
        PrismPremultipliedColor[] textured = Apply(
            CreatePlan(3, 8, 8),
            source,
            width,
            height);

        Assert.Equal(baseline, repeated);
        Assert.True(MeanDifference(source, baseline) > 0.001);
        Assert.True(MeanDifference(baseline, smallBrush) > 0.0001);
        Assert.True(MeanDifference(baseline, softDetail) > 0.0001);
        Assert.True(MeanDifference(baseline, textured) > 0.0001);
        Assert.True(
            RegionDeviation(baseline, width, 3, 9) <
            RegionDeviation(source, width, 3, 9));

        double left = RegionMean(baseline, width, 3, 9);
        double right = RegionMean(baseline, width, 16, 22);
        Assert.True(right - left > 0.4);
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
        float brushSize,
        float brushDetail,
        float texture,
        float pixelScale = 1,
        Matrix3x2? effectiveTransform = null) =>
        PrismCatalogFilterPlanner.Create(
            PrismFilterId.Fresco,
            [
                new PrismGraphParameter(
                    0,
                    PrismGraphParameterValueKind.Number,
                    numberValue: brushSize),
                new PrismGraphParameter(
                    1,
                    PrismGraphParameterValueKind.Number,
                    numberValue: brushDetail),
                new PrismGraphParameter(
                    2,
                    PrismGraphParameterValueKind.Number,
                    numberValue: texture)
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
                double baseValue = x < width / 2 ? 0.15 : 0.85;
                double noise = ((x + y) & 1) == 0 ? -0.08 : 0.08;
                double alpha = x == 0 && y == 0 ? 0 : 0.75;
                double value = Math.Clamp(baseValue + noise, 0, 1);
                pixels[(y * width) + x] =
                    new PrismPremultipliedColor(
                        value * alpha,
                        value * alpha,
                        value * alpha,
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
