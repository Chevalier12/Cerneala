using System.Numerics;
using Cerneala.Drawing;
using Cerneala.Drawing.Prism;
using Cerneala.Drawing.Prism.Catalog;
using Cerneala.Drawing.Prism.ColorManagement;
using Cerneala.Drawing.Prism.Filters;
using Cerneala.Drawing.Prism.Graph;
using Cerneala.UI.Prism.Definitions;

namespace Cerneala.Tests.Drawing.Prism;

public sealed class PrismAccentedEdgesFilterTests
{
    [Fact]
    public void PlannerBuildsDualGaussianAndCompositePasses()
    {
        PrismCatalogFilterPlan plan = CreatePlan(
            edgeWidth: 2,
            edgeBrightness: 38,
            smoothness: 5,
            pixelScale: 2,
            effectiveTransform: Matrix3x2.CreateScale(1.5f));

        Assert.Equal(3, plan.Passes.Length);
        Assert.Equal(
            [
                PrismCatalogFilterPassKind.Horizontal,
                PrismCatalogFilterPassKind.Vertical,
                PrismCatalogFilterPassKind.Direct
            ],
            plan.Passes.Select(pass => pass.Kind));
        Assert.Equal([0, 1, 2], plan.Passes.Select(pass => pass.Iteration));
        Assert.Equal(2, plan.GetOption("EdgeWidth").X);
        Assert.Equal(38, plan.GetOption("EdgeBrightness").X);
        Assert.Equal(5, plan.GetOption("Smoothness").X);
        Assert.Equal(3, plan.Options3.X, 5);
        Assert.Equal(4.8f, plan.Options3.Y, 5);
        Assert.Equal(8, plan.Passes[0].RadiusX);
        Assert.Equal(8, plan.Passes[1].RadiusY);
        Assert.False(PrismCatalogFilterPlanner.RequiresOriginalInput(
            plan.Filter,
            plan.Passes[1]));
        Assert.True(PrismCatalogFilterPlanner.RequiresOriginalInput(
            plan.Filter,
            plan.Passes[2]));
    }

    [Fact]
    public void CpuReferenceIsDeterministicAndControlsInkToChalkTransition()
    {
        const int width = 31;
        const int height = 19;
        PrismPremultipliedColor[] source = CreateEdge(width, height);
        PrismPremultipliedColor[] ink = Apply(
            CreatePlan(2, 0, 5), source, width, height);
        PrismPremultipliedColor[] repeated = Apply(
            CreatePlan(2, 0, 5), source, width, height);
        PrismPremultipliedColor[] chalk = Apply(
            CreatePlan(2, 50, 5), source, width, height);
        PrismPremultipliedColor[] wide = Apply(
            CreatePlan(5, 0, 5), source, width, height);
        PrismPremultipliedColor[] soft = Apply(
            CreatePlan(2, 0, 15), source, width, height);

        Assert.Equal(ink, repeated);
        Assert.True(MeanDifference(source, ink) > 0.001);
        Assert.True(MeanDifference(ink, chalk) > 0.001);
        Assert.True(MeanDifference(ink, wide) > 0.0001);
        Assert.True(MeanDifference(ink, soft) > 0.0001);
        Assert.True(BoundaryMean(ink, width) < BoundaryMean(source, width));
        Assert.True(BoundaryMean(chalk, width) > BoundaryMean(ink, width));

        for (int index = 0; index < source.Length; index++)
        {
            PrismPremultipliedColor pixel = ink[index];
            Assert.Equal(source[index].Alpha, pixel.Alpha, 5);
            Assert.True(double.IsFinite(pixel.Red));
            Assert.InRange(pixel.Red, 0, pixel.Alpha);
            Assert.InRange(pixel.Green, 0, pixel.Alpha);
            Assert.InRange(pixel.Blue, 0, pixel.Alpha);
        }
    }

    [Fact]
    public void ConstantColorRemainsUnchanged()
    {
        const int width = 13;
        const int height = 9;
        PrismPremultipliedColor color =
            PrismPremultipliedColor.FromStraight(0.35, 0.55, 0.75, 0.6);
        PrismPremultipliedColor[] source =
            Enumerable.Repeat(color, width * height).ToArray();

        PrismPremultipliedColor[] result = Apply(
            CreatePlan(3, 12, 7), source, width, height);

        for (int index = 0; index < result.Length; index++)
        {
            Assert.Equal(source[index].Red, result[index].Red, 5);
            Assert.Equal(source[index].Green, result[index].Green, 5);
            Assert.Equal(source[index].Blue, result[index].Blue, 5);
            Assert.Equal(source[index].Alpha, result[index].Alpha, 5);
        }
    }

    private static PrismCatalogFilterPlan CreatePlan(
        float edgeWidth,
        float edgeBrightness,
        float smoothness,
        float pixelScale = 1,
        Matrix3x2? effectiveTransform = null) =>
        PrismCatalogFilterPlanner.Create(
            PrismFilterId.AccentedEdges,
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

    private static PrismPremultipliedColor[] CreateEdge(int width, int height)
    {
        PrismPremultipliedColor[] pixels =
            new PrismPremultipliedColor[width * height];
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                double value = x < width / 2 ? 0.2 : 0.8;
                double alpha = (x + y) % 7 == 0 ? 0.45 : 0.8;
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
            .Where((_, index) => index % width is 13 or 14 or 15 or 16)
            .Average(Luminance);

    private static double Luminance(PrismPremultipliedColor pixel) =>
        pixel.Alpha <= 0
            ? 0
            : ((pixel.Red * 0.2126) +
                (pixel.Green * 0.7152) +
                (pixel.Blue * 0.0722)) /
            pixel.Alpha;
}
