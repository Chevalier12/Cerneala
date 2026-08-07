using System.Numerics;
using Cerneala.Drawing;
using Cerneala.Drawing.Prism;
using Cerneala.Drawing.Prism.Catalog;
using Cerneala.Drawing.Prism.ColorManagement;
using Cerneala.Drawing.Prism.Filters;
using Cerneala.Drawing.Prism.Graph;

namespace Cerneala.Tests.Drawing.Prism;

public sealed class PrismSpongeFilterTests
{
    [Fact]
    public void PlannerBuildsOneBoundedDeviceScaledPassAndRecognizesNoOp()
    {
        PrismCatalogFilterPlan plan = CreatePlan(
            brushSize: 6,
            pixelScale: 2,
            effectiveTransform: Matrix3x2.CreateScale(1.5f));
        PrismCatalogFilterPlan capped = CreatePlan(
            brushSize: 100,
            pixelScale: 2,
            effectiveTransform: Matrix3x2.CreateScale(1.5f));
        PrismCatalogFilterPlan disabled = CreatePlan(brushSize: 0);

        PrismCatalogFilterPass pass = Assert.Single(plan.Passes);
        Assert.Equal(PrismCatalogFilterPassKind.Direct, pass.Kind);
        Assert.Equal(18, pass.RadiusX);
        Assert.Equal(18, pass.RadiusY);
        Assert.False(pass.IsNoOp);
        Assert.Equal(36, Assert.Single(capped.Passes).RadiusX);
        Assert.True(Assert.Single(disabled.Passes).IsNoOp);
    }

    [Fact]
    public void PolynomialKuwaharaIsDeterministicAndHonorsEveryControl()
    {
        const int width = 25;
        const int height = 17;
        PrismPremultipliedColor[] source = CreateNoisyEdge(width, height);
        PrismPremultipliedColor[] baseline = Apply(
            CreatePlan(), source, width, height);
        PrismPremultipliedColor[] repeated = Apply(
            CreatePlan(), source, width, height);
        PrismPremultipliedColor[] broader = Apply(
            CreatePlan(brushSize: 6), source, width, height);
        PrismPremultipliedColor[] defined = Apply(
            CreatePlan(definition: 22), source, width, height);
        PrismPremultipliedColor[] smooth = Apply(
            CreatePlan(smoothness: 14), source, width, height);

        Assert.Equal(baseline, repeated);
        Assert.True(MeanDifference(source, baseline) > 0.001);
        Assert.True(MeanDifference(baseline, broader) > 0.00001);
        Assert.True(MeanDifference(baseline, defined) > 0.00001);
        Assert.True(MeanDifference(baseline, smooth) > 0.00001);

        for (int index = 0; index < source.Length; index++)
        {
            PrismPremultipliedColor pixel = baseline[index];
            Assert.Equal(source[index].Alpha, pixel.Alpha, 5);
            AssertFiniteAssociated(pixel);
        }
    }

    [Fact]
    public void PolynomialKuwaharaSmoothsRegionsWithoutWashingAcrossEdge()
    {
        const int width = 25;
        const int height = 17;
        PrismPremultipliedColor[] source = CreateNoisyEdge(width, height);
        PrismPremultipliedColor[] result = Apply(
            CreatePlan(brushSize: 5, definition: 16, smoothness: 8),
            source,
            width,
            height);

        double sourceVariation = RegionVariation(source, width, height);
        double resultVariation = RegionVariation(result, width, height);
        double leftMean = RegionMean(result, width, height, left: true);
        double rightMean = RegionMean(result, width, height, left: false);

        Assert.True(
            resultVariation < sourceVariation * 0.8,
            $"Expected regional variation below {sourceVariation:F6}, got {resultVariation:F6}.");
        Assert.True(
            rightMean - leftMean > 0.35,
            $"Expected the main edge to survive, got means {leftMean:F6} and {rightMean:F6}.");
    }

    [Fact]
    public void PolynomialKuwaharaPreservesConstantAssociatedColor()
    {
        const int width = 13;
        const int height = 9;
        PrismPremultipliedColor constant =
            PrismPremultipliedColor.FromStraight(0.28, 0.46, 0.73, 0.61);
        PrismPremultipliedColor[] source =
            Enumerable.Repeat(constant, width * height).ToArray();

        PrismPremultipliedColor[] result = Apply(
            CreatePlan(brushSize: 8, definition: 20, smoothness: 12),
            source,
            width,
            height);

        Assert.All(
            result,
            pixel =>
            {
                Assert.Equal(constant.Red, pixel.Red, 5);
                Assert.Equal(constant.Green, pixel.Green, 5);
                Assert.Equal(constant.Blue, pixel.Blue, 5);
                Assert.Equal(constant.Alpha, pixel.Alpha, 5);
            });
    }

    private static PrismCatalogFilterPlan CreatePlan(
        float brushSize = 2,
        float definition = 12,
        float smoothness = 5,
        float pixelScale = 1,
        Matrix3x2? effectiveTransform = null) =>
        PrismCatalogFilterPlanner.Create(
            PrismFilterId.Sponge,
            [
                Number(0, brushSize),
                Number(1, definition),
                Number(2, smoothness)
            ],
            PrismBlendMode.Normal,
            pixelScale,
            effectiveTransform ?? Matrix3x2.Identity,
            new DrawRect(0, 0, 25, 17));

    private static PrismGraphParameter Number(int slot, float value) =>
        new(
            slot,
            PrismGraphParameterValueKind.Number,
            numberValue: value);

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
                bool left = x < width / 2;
                double noise = ((x + y) & 1) == 0 ? -0.1 : 0.1;
                double value = Math.Clamp(
                    (left ? 0.2 : 0.8) + noise,
                    0,
                    1);
                double alpha = x == 0 && y == 0 ? 0 : 0.68;
                pixels[(y * width) + x] =
                    PrismPremultipliedColor.FromStraight(
                        value,
                        Math.Clamp(value * 0.85, 0, 1),
                        Math.Clamp(value * 0.65, 0, 1),
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

    private static double RegionVariation(
        PrismPremultipliedColor[] pixels,
        int width,
        int height)
    {
        double total = 0;
        int count = 0;
        for (int y = 1; y < height - 1; y++)
        {
            for (int x = 1; x < width - 2; x++)
            {
                if (x == (width / 2) - 1 || x == width / 2)
                {
                    continue;
                }
                total += Math.Abs(
                    Luminance(pixels[(y * width) + x]) -
                    Luminance(pixels[(y * width) + x + 1]));
                count++;
            }
        }
        return total / count;
    }

    private static double RegionMean(
        PrismPremultipliedColor[] pixels,
        int width,
        int height,
        bool left)
    {
        int startX = left ? 1 : (width / 2) + 2;
        int endX = left ? (width / 2) - 2 : width - 1;
        double total = 0;
        int count = 0;
        for (int y = 1; y < height - 1; y++)
        {
            for (int x = startX; x < endX; x++)
            {
                total += Luminance(pixels[(y * width) + x]);
                count++;
            }
        }
        return total / count;
    }

    private static double Luminance(PrismPremultipliedColor pixel) =>
        pixel.Alpha <= 0
            ? 0
            : ((pixel.Red * 0.2126) +
                (pixel.Green * 0.7152) +
                (pixel.Blue * 0.0722)) /
            pixel.Alpha;

    private static void AssertFiniteAssociated(
        PrismPremultipliedColor pixel)
    {
        Assert.True(double.IsFinite(pixel.Red));
        Assert.True(double.IsFinite(pixel.Green));
        Assert.True(double.IsFinite(pixel.Blue));
        Assert.True(double.IsFinite(pixel.Alpha));
        Assert.InRange(pixel.Alpha, 0, 1);
        Assert.InRange(pixel.Red, 0, pixel.Alpha);
        Assert.InRange(pixel.Green, 0, pixel.Alpha);
        Assert.InRange(pixel.Blue, 0, pixel.Alpha);
    }
}
