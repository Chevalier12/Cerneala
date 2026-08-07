using System.Numerics;
using Cerneala.Drawing;
using Cerneala.Drawing.Prism;
using Cerneala.Drawing.Prism.Catalog;
using Cerneala.Drawing.Prism.ColorManagement;
using Cerneala.Drawing.Prism.Filters;
using Cerneala.Drawing.Prism.Graph;
using Cerneala.UI.Prism.Definitions;

namespace Cerneala.Tests.Drawing.Prism;

public sealed class PrismGlowingEdgesFilterTests
{
    [Fact]
    public void PlannerBuildsScharrGaussianAndCompositePasses()
    {
        PrismCatalogFilterPlan plan = CreatePlan(
            edgeWidth: 2,
            edgeBrightness: 6,
            smoothness: 5,
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
        Assert.Equal(6, plan.Options3.X);
        Assert.Equal(4, plan.Options3.Y);
        Assert.Equal(8, plan.Options3.Z);
        Assert.Equal(0.5666667f, plan.Options3.W, 5);
        Assert.Equal(6, plan.Passes[0].RadiusX);
        Assert.Equal(8, plan.Passes[1].RadiusX);
        Assert.Equal(8, plan.Passes[2].RadiusY);
        Assert.False(PrismCatalogFilterPlanner.RequiresOriginalInput(
            plan.Filter,
            plan.Passes[1]));
        Assert.True(PrismCatalogFilterPlanner.RequiresOriginalInput(
            plan.Filter,
            plan.Passes[2]));
    }

    [Fact]
    public void CpuReferenceIsDeterministicAlphaSafeAndControlSensitive()
    {
        const int width = 31;
        const int height = 19;
        PrismPremultipliedColor[] source = CreateEdge(width, height);
        PrismPremultipliedColor[] baseline = Apply(
            CreatePlan(2, 6, 5), source, width, height);
        PrismPremultipliedColor[] repeated = Apply(
            CreatePlan(2, 6, 5), source, width, height);
        PrismPremultipliedColor[] wide = Apply(
            CreatePlan(5, 6, 5), source, width, height);
        PrismPremultipliedColor[] dim = Apply(
            CreatePlan(2, 1, 5), source, width, height);
        PrismPremultipliedColor[] smooth = Apply(
            CreatePlan(2, 6, 15), source, width, height);

        Assert.Equal(baseline, repeated);
        Assert.True(MeanDifference(baseline, wide) > 0.0001);
        Assert.True(MeanLuminance(baseline) > MeanLuminance(dim));
        Assert.True(MeanDifference(baseline, smooth) > 0.0001);
        Assert.True(HaloMean(smooth, width) > HaloMean(baseline, width));

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

    [Fact]
    public void ConstantColorProducesBlackFieldWithPreservedAlpha()
    {
        const int width = 13;
        const int height = 9;
        PrismPremultipliedColor sourcePixel =
            PrismPremultipliedColor.FromStraight(0.4, 0.6, 0.8, 0.7);
        PrismPremultipliedColor[] source =
            Enumerable.Repeat(sourcePixel, width * height).ToArray();

        PrismPremultipliedColor[] result = Apply(
            CreatePlan(2, 6, 5), source, width, height);

        Assert.All(result, pixel =>
        {
            Assert.Equal(0, pixel.Red, 6);
            Assert.Equal(0, pixel.Green, 6);
            Assert.Equal(0, pixel.Blue, 6);
            Assert.Equal(sourcePixel.Alpha, pixel.Alpha, 6);
        });
    }

    private static PrismCatalogFilterPlan CreatePlan(
        float edgeWidth,
        float edgeBrightness,
        float smoothness,
        float pixelScale = 1,
        Matrix3x2? effectiveTransform = null) =>
        PrismCatalogFilterPlanner.Create(
            PrismFilterId.GlowingEdges,
            [
                new PrismGraphParameter(
                    0,
                    PrismGraphParameterValueKind.Number,
                    numberValue: edgeBrightness),
                new PrismGraphParameter(
                    1,
                    PrismGraphParameterValueKind.Number,
                    numberValue: edgeWidth),
                new PrismGraphParameter(
                    2,
                    PrismGraphParameterValueKind.Number,
                    numberValue: smoothness)
            ],
            PrismBlendMode.Normal,
            pixelScale,
            effectiveTransform ?? Matrix3x2.Identity,
            new DrawRect(0, 0, width: 31, height: 19));

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

    private static PrismPremultipliedColor[] CreateEdge(int width, int height)
    {
        PrismPremultipliedColor[] pixels =
            new PrismPremultipliedColor[width * height];
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                double value = x < width / 2 ? 0.15 : 0.85;
                double alpha = (x + y) % 9 == 0 ? 0.45 : 0.8;
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

    private static double MeanLuminance(
        PrismPremultipliedColor[] pixels) =>
        pixels.Average(Luminance);

    private static double HaloMean(
        PrismPremultipliedColor[] pixels,
        int width) =>
        pixels
            .Where((_, index) => index % width is 10 or 11 or 18 or 19)
            .Average(Luminance);

    private static double Luminance(PrismPremultipliedColor pixel) =>
        pixel.Alpha <= 0
            ? 0
            : ((pixel.Red * 0.2126) +
                (pixel.Green * 0.7152) +
                (pixel.Blue * 0.0722)) /
            pixel.Alpha;
}
