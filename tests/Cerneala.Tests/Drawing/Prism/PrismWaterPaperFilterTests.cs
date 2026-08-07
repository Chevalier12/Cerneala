using System.Numerics;
using Cerneala.Drawing;
using Cerneala.Drawing.Prism;
using Cerneala.Drawing.Prism.Catalog;
using Cerneala.Drawing.Prism.ColorManagement;
using Cerneala.Drawing.Prism.Filters;
using Cerneala.Drawing.Prism.Graph;
using Cerneala.UI.Prism.Definitions;

namespace Cerneala.Tests.Drawing.Prism;

public sealed class PrismWaterPaperFilterTests
{
    [Fact]
    public void PlannerBuildsPigmentAndSubstratePasses()
    {
        PrismCatalogFilterPlan plan = CreatePlan(
            fiberLength: 15,
            brightness: 60,
            contrast: 80,
            seed: 17,
            pixelScale: 2,
            effectiveTransform: Matrix3x2.CreateScale(1.5f));

        Assert.Equal(
            PrismCatalogFilterPrimitive.Artistic,
            plan.Primitive);
        Assert.Equal(2, plan.Passes.Length);
        Assert.Equal(
            [
                PrismCatalogFilterPassKind.Iteration,
                PrismCatalogFilterPassKind.Direct
            ],
            plan.Passes.Select(pass => pass.Kind));
        Assert.Equal([0, 1], plan.Passes.Select(pass => pass.Iteration));
        Assert.Equal(45, plan.GetOption("FiberLength").X);
        Assert.False(
            PrismCatalogFilterPlanner.RequiresOriginalInput(
                plan.Filter,
                plan.Passes[0]));
        Assert.True(
            PrismCatalogFilterPlanner.RequiresOriginalInput(
                plan.Filter,
                plan.Passes[1]));
    }

    [Fact]
    public void CpuReferenceIsDeterministicSeededAndControlSensitive()
    {
        const int width = 41;
        const int height = 29;
        PrismPremultipliedColor[] source = CreatePigmentedRegions(
            width,
            height);
        PrismPremultipliedColor[] baseline = Apply(
            CreatePlan(15, 60, 80, 17),
            source,
            width,
            height);
        PrismPremultipliedColor[] repeated = Apply(
            CreatePlan(15, 60, 80, 17),
            source,
            width,
            height);
        PrismPremultipliedColor[] differentSeed = Apply(
            CreatePlan(15, 60, 80, 91),
            source,
            width,
            height);
        PrismPremultipliedColor[] shortFibers = Apply(
            CreatePlan(4, 60, 80, 17),
            source,
            width,
            height);
        PrismPremultipliedColor[] longFibers = Apply(
            CreatePlan(28, 60, 80, 17),
            source,
            width,
            height);
        PrismPremultipliedColor[] dark = Apply(
            CreatePlan(15, 25, 80, 17),
            source,
            width,
            height);
        PrismPremultipliedColor[] bright = Apply(
            CreatePlan(15, 85, 80, 17),
            source,
            width,
            height);
        PrismPremultipliedColor[] lowContrast = Apply(
            CreatePlan(15, 60, 10, 17),
            source,
            width,
            height);
        PrismPremultipliedColor[] highContrast = Apply(
            CreatePlan(15, 60, 100, 17),
            source,
            width,
            height);

        Assert.Equal(baseline, repeated);
        Assert.True(MeanDifference(source, baseline) > 0.001);
        Assert.True(MeanDifference(baseline, differentSeed) > 0.0001);
        Assert.True(MeanDifference(shortFibers, longFibers) > 0.0001);
        Assert.True(MeanLuminance(bright) > MeanLuminance(dark) + 0.05);
        Assert.True(
            LuminanceDeviation(highContrast) >
            LuminanceDeviation(lowContrast) + 0.005);

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
        float fiberLength,
        float brightness,
        float contrast,
        int seed,
        float pixelScale = 1,
        Matrix3x2? effectiveTransform = null) =>
        PrismCatalogFilterPlanner.Create(
            PrismFilterId.WaterPaper,
            [
                new PrismGraphParameter(
                    0,
                    PrismGraphParameterValueKind.Number,
                    numberValue: brightness),
                new PrismGraphParameter(
                    1,
                    PrismGraphParameterValueKind.Number,
                    numberValue: contrast),
                new PrismGraphParameter(
                    2,
                    PrismGraphParameterValueKind.Number,
                    numberValue: fiberLength),
                new PrismGraphParameter(
                    3,
                    PrismGraphParameterValueKind.Integer,
                    integerValue: seed)
            ],
            PrismBlendMode.Normal,
            pixelScale,
            effectiveTransform ?? Matrix3x2.Identity,
            new DrawRect(0, 0, 41, 29));

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

    private static PrismPremultipliedColor[] CreatePigmentedRegions(
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
                double noise = ((x + y) & 1) == 0 ? -0.025 : 0.025;
                double alpha = x == 0 && y == 0
                    ? 0
                    : 0.55 + (0.35 * y / (height - 1));
                pixels[(y * width) + x] =
                    PrismPremultipliedColor.FromStraight(
                        Math.Clamp((left ? 0.26 : 0.72) + noise, 0, 1),
                        Math.Clamp((left ? 0.38 : 0.31) + noise, 0, 1),
                        Math.Clamp((left ? 0.68 : 0.18) + noise, 0, 1),
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

    private static double LuminanceDeviation(
        PrismPremultipliedColor[] pixels)
    {
        double[] values = pixels
            .Where(pixel => pixel.Alpha > 0)
            .Select(Luminance)
            .ToArray();
        double mean = values.Average();
        return Math.Sqrt(values.Average(value =>
            (value - mean) * (value - mean)));
    }

    private static double Luminance(PrismPremultipliedColor pixel) =>
        ((pixel.Red * 0.2126) +
            (pixel.Green * 0.7152) +
            (pixel.Blue * 0.0722)) /
        pixel.Alpha;
}
