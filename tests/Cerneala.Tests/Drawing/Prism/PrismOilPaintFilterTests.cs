using System.Numerics;
using Cerneala.Drawing;
using Cerneala.Drawing.Prism;
using Cerneala.Drawing.Prism.Catalog;
using Cerneala.Drawing.Prism.ColorManagement;
using Cerneala.Drawing.Prism.Filters;
using Cerneala.Drawing.Prism.Graph;
using Cerneala.UI.Prism.Definitions;

namespace Cerneala.Tests.Drawing.Prism;

public sealed class PrismOilPaintFilterTests
{
    [Fact]
    public void PlannerUsesScaleDerivedKuwaharaRadius()
    {
        PrismCatalogFilterPlan plan = CreatePlan(pixelScale: 2);

        PrismCatalogFilterPass pass = Assert.Single(plan.Passes);
        Assert.Equal(PrismCatalogFilterPrimitive.Texture, plan.Primitive);
        Assert.Equal(PrismCatalogFilterPassKind.Direct, pass.Kind);
        Assert.Equal(6, pass.RadiusX);
        Assert.Equal(6, pass.RadiusY);
        Assert.False(pass.IsNoOp);
    }

    [Fact]
    public void CpuReferenceIsDeterministicEdgeAwareAndAllControlsMatter()
    {
        const int width = 19;
        const int height = 13;
        PrismPremultipliedColor[] source = CreateNoisyColorEdge(
            width,
            height);
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

        Assert.Equal(baseline, repeated);
        Assert.True(MeanDifference(source, baseline) > 0.001);
        AssertControlChangesOutput(
            baseline,
            Apply(CreatePlan(stylization: 0.1f), source, width, height));
        AssertControlChangesOutput(
            baseline,
            Apply(CreatePlan(cleanliness: 0.9f), source, width, height));
        AssertControlChangesOutput(
            baseline,
            Apply(CreatePlan(scale: 2), source, width, height));
        AssertControlChangesOutput(
            baseline,
            Apply(CreatePlan(bristleDetail: 0.9f), source, width, height));
        AssertControlChangesOutput(
            baseline,
            Apply(CreatePlan(lighting: false), source, width, height));
        AssertControlChangesOutput(
            baseline,
            Apply(CreatePlan(angle: 67), source, width, height));
        AssertControlChangesOutput(
            baseline,
            Apply(CreatePlan(shine: 0.9f), source, width, height));

        double left = RegionMean(baseline, width, 2, 7);
        double right = RegionMean(baseline, width, 12, 17);
        Assert.True(right - left > 0.25);
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

    private static void AssertControlChangesOutput(
        PrismPremultipliedColor[] baseline,
        PrismPremultipliedColor[] variant) =>
        Assert.True(MeanDifference(baseline, variant) > 0.000001);

    private static PrismCatalogFilterPlan CreatePlan(
        float stylization = 0.5f,
        float cleanliness = 0.5f,
        float scale = 1,
        float bristleDetail = 0.5f,
        bool lighting = true,
        float angle = 0,
        float shine = 0.5f,
        float pixelScale = 1) =>
        PrismCatalogFilterPlanner.Create(
            PrismFilterId.OilPaint,
            [
                NumberParameter(0, angle),
                NumberParameter(1, bristleDetail),
                NumberParameter(2, cleanliness),
                new PrismGraphParameter(
                    3,
                    PrismGraphParameterValueKind.Boolean,
                    booleanValue: lighting),
                NumberParameter(4, scale),
                NumberParameter(5, shine),
                NumberParameter(6, stylization)
            ],
            PrismBlendMode.Normal,
            pixelScale,
            Matrix3x2.Identity,
            new DrawRect(0, 0, 19, 13));

    private static PrismGraphParameter NumberParameter(
        int slot,
        float value) =>
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

    private static PrismPremultipliedColor[] CreateNoisyColorEdge(
        int width,
        int height)
    {
        PrismPremultipliedColor[] pixels =
            new PrismPremultipliedColor[width * height];
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                double baseValue = x < width / 2 ? 0.16 : 0.82;
                double noise = ((x + y) & 1) == 0 ? -0.07 : 0.07;
                double alpha = x == 0 && y == 0 ? 0 : 0.75;
                double red = Math.Clamp(baseValue + noise, 0, 1);
                double green = Math.Clamp(
                    baseValue + (noise * 0.55) + (y * 0.004),
                    0,
                    1);
                double blue = Math.Clamp(
                    baseValue - (noise * 0.45),
                    0,
                    1);
                pixels[(y * width) + x] =
                    new PrismPremultipliedColor(
                        red * alpha,
                        green * alpha,
                        blue * alpha,
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

    private static double Luminance(PrismPremultipliedColor pixel) =>
        pixel.Alpha <= 0
            ? 0
            : ((pixel.Red * 0.2126) +
                (pixel.Green * 0.7152) +
                (pixel.Blue * 0.0722)) /
            pixel.Alpha;
}
