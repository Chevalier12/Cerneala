using System.Numerics;
using Cerneala.Drawing;
using Cerneala.Drawing.Prism;
using Cerneala.Drawing.Prism.Catalog;
using Cerneala.Drawing.Prism.ColorManagement;
using Cerneala.Drawing.Prism.Filters;
using Cerneala.Drawing.Prism.Graph;
using Cerneala.UI.Prism.Definitions;
using Cerneala.UI.Prism.Runtime;

namespace Cerneala.Tests.Drawing.Prism;

public sealed class PrismUnderpaintingFilterTests
{
    [Fact]
    public void PlannerBuildsBoundedDeviceScaledAkfAndCanonicalSymbols()
    {
        PrismCatalogFilterPlan plan = CreatePlan(
            brushSize: 6,
            texture: "Burlap",
            lightDirection: "BottomLeft",
            pixelScale: 2,
            effectiveTransform: Matrix3x2.CreateScale(1.5f));
        PrismCatalogFilterPlan capped = CreatePlan(
            brushSize: 100,
            pixelScale: 2,
            effectiveTransform: Matrix3x2.CreateScale(1.5f));

        PrismCatalogFilterPass pass = Assert.Single(plan.Passes);
        PrismCatalogFilterPass cappedPass = Assert.Single(capped.Passes);
        Assert.Equal(PrismCatalogFilterPassKind.Direct, pass.Kind);
        Assert.Equal(18, pass.RadiusX);
        Assert.Equal(18, pass.RadiusY);
        Assert.Equal(2, plan.GetOption("Texture").X);
        Assert.Equal(5, plan.GetOption("LightDirection").X);
        Assert.Equal(36, cappedPass.RadiusX);
        Assert.Equal(36, cappedPass.RadiusY);
    }

    [Fact]
    public void CpuReferenceIsDeterministicEdgeAwareAndHonorsEveryControl()
    {
        const int width = 21;
        const int height = 15;
        PrismPremultipliedColor[] source = CreateNoisyEdge(width, height);
        PrismPremultipliedColor[] baseline = Apply(
            CreatePlan(),
            source,
            width,
            height);
        PrismPremultipliedColor[] repeated = Apply(
            CreatePlan(),
            source,
            width,
            height);
        PrismPremultipliedColor[][] variants =
        [
            Apply(CreatePlan(brushSize: 10), source, width, height),
            Apply(
                CreatePlan(textureCoverage: 0.8f),
                source,
                width,
                height),
            Apply(CreatePlan(texture: "Brick"), source, width, height),
            Apply(CreatePlan(scaling: 2.5f), source, width, height),
            Apply(CreatePlan(relief: 0.9f), source, width, height),
            Apply(
                CreatePlan(lightDirection: "TopLeft"),
                source,
                width,
                height),
            Apply(CreatePlan(invert: true), source, width, height)
        ];

        Assert.Equal(baseline, repeated);
        Assert.True(MeanDifference(source, baseline) > 0.001);
        Assert.All(
            variants,
            variant => Assert.True(
                MeanDifference(baseline, variant) > 0.00001));
        Assert.True(
            RegionDeviation(baseline, width, 2, 7) <
            RegionDeviation(source, width, 2, 7));
        Assert.True(
            RegionMean(baseline, width, 14, 19) -
            RegionMean(baseline, width, 2, 7) >
            0.25);

        for (int index = 0; index < source.Length; index++)
        {
            PrismPremultipliedColor pixel = baseline[index];
            Assert.Equal(source[index].Alpha, pixel.Alpha, 5);
            Assert.True(double.IsFinite(pixel.Red));
            Assert.True(double.IsFinite(pixel.Green));
            Assert.True(double.IsFinite(pixel.Blue));
            Assert.InRange(pixel.Red, 0, pixel.Alpha);
            Assert.InRange(pixel.Green, 0, pixel.Alpha);
            Assert.InRange(pixel.Blue, 0, pixel.Alpha);
        }
    }

    private static PrismCatalogFilterPlan CreatePlan(
        float brushSize = 6,
        float textureCoverage = 0.2f,
        string texture = "Canvas",
        float scaling = 1,
        float relief = 0.04f,
        string lightDirection = "Top",
        bool invert = false,
        float pixelScale = 1,
        Matrix3x2? effectiveTransform = null) =>
        PrismCatalogFilterPlanner.Create(
            PrismFilterId.Underpainting,
            [
                Number(0, brushSize),
                new PrismGraphParameter(
                    1,
                    PrismGraphParameterValueKind.Boolean,
                    booleanValue: invert),
                Symbol(2, "LightDirection", lightDirection),
                Number(3, relief),
                Number(4, scaling),
                Symbol(5, "Texture", texture),
                Number(6, textureCoverage)
            ],
            PrismBlendMode.Normal,
            pixelScale,
            effectiveTransform ?? Matrix3x2.Identity,
            new DrawRect(0, 0, 21, 15));

    private static PrismGraphParameter Number(
        int slot,
        float value) =>
        new(
            slot,
            PrismGraphParameterValueKind.Number,
            numberValue: value);

    private static PrismGraphParameter Symbol(
        int slot,
        string property,
        string value) =>
        new(
            slot,
            PrismGraphParameterValueKind.Symbol,
            integerValue: PrismCatalogRuntime.ResolveSymbol(
                property,
                value));

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
                double alpha = x == 0 && y == 0 ? 0 : 0.75;
                pixels[(y * width) + x] =
                    PrismPremultipliedColor.FromStraight(
                        Math.Clamp(
                            (left ? 0.18 : 0.82) + noise,
                            0,
                            1),
                        Math.Clamp(
                            (left ? 0.32 : 0.7) - (noise * 0.5),
                            0,
                            1),
                        Math.Clamp(
                            (left ? 0.62 : 0.2) + (noise * 0.25),
                            0,
                            1),
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

    private static double Luminance(
        PrismPremultipliedColor pixel) =>
        pixel.Alpha <= 0
            ? 0
            : ((pixel.Red * 0.2126) +
                (pixel.Green * 0.7152) +
                (pixel.Blue * 0.0722)) /
            pixel.Alpha;
}
