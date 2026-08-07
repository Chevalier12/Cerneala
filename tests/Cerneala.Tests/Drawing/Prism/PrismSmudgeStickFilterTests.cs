using System.Numerics;
using Cerneala.Drawing;
using Cerneala.Drawing.Prism;
using Cerneala.Drawing.Prism.Catalog;
using Cerneala.Drawing.Prism.ColorManagement;
using Cerneala.Drawing.Prism.Filters;
using Cerneala.Drawing.Prism.Graph;
using Cerneala.UI.Prism.Definitions;

namespace Cerneala.Tests.Drawing.Prism;

public sealed class PrismSmudgeStickFilterTests
{
    [Fact]
    public void PlannerBuildsOneBoundedDeviceScaledPassAndRecognizesNoOp()
    {
        PrismCatalogFilterPlan plan = CreatePlan(
            strokeLength: 6,
            pixelScale: 2,
            effectiveTransform: Matrix3x2.CreateScale(1.5f));
        PrismCatalogFilterPlan capped = CreatePlan(
            strokeLength: 100,
            pixelScale: 2,
            effectiveTransform: Matrix3x2.CreateScale(1.5f));
        PrismCatalogFilterPlan zeroLength = CreatePlan(strokeLength: 0);
        PrismCatalogFilterPlan zeroIntensity = CreatePlan(intensity: 0);

        PrismCatalogFilterPass pass = Assert.Single(plan.Passes);
        Assert.Equal(PrismCatalogFilterPassKind.Direct, pass.Kind);
        Assert.Equal(18, pass.RadiusX);
        Assert.Equal(18, pass.RadiusY);
        Assert.False(pass.IsNoOp);
        Assert.Equal(36, Assert.Single(capped.Passes).RadiusX);
        Assert.True(Assert.Single(zeroLength.Passes).IsNoOp);
        Assert.True(Assert.Single(zeroIntensity.Passes).IsNoOp);
    }

    [Fact]
    public void CpuReferenceIsDeterministicAndHonorsEveryControl()
    {
        const int width = 17;
        const int height = 13;
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
        PrismPremultipliedColor[] longer = Apply(
            CreatePlan(strokeLength: 8),
            source,
            width,
            height);
        PrismPremultipliedColor[] highlighted = Apply(
            CreatePlan(highlightArea: 12),
            source,
            width,
            height);
        PrismPremultipliedColor[] disabled = Apply(
            CreatePlan(intensity: 0),
            source,
            width,
            height);

        Assert.Equal(baseline, repeated);
        Assert.True(MeanDifference(source, disabled) < 0.000001);
        Assert.True(MeanDifference(source, baseline) > 0.001);
        Assert.True(MeanDifference(baseline, longer) > 0.00001);
        Assert.True(MeanDifference(baseline, highlighted) > 0.00001);

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

    [Fact]
    public void CpuReferenceFavorsDiagonalStrokeFlow()
    {
        const int size = 17;
        const int center = size / 2;
        PrismPremultipliedColor[] source = Enumerable
            .Repeat(
                PrismPremultipliedColor.FromStraight(0.8, 0.8, 0.8, 1),
                size * size)
            .ToArray();
        source[(center * size) + center] =
            PrismPremultipliedColor.FromStraight(0.65, 0.65, 0.65, 1);

        PrismPremultipliedColor[] output = Apply(
            CreatePlan(strokeLength: 8),
            source,
            size,
            size);
        double diagonal =
            Luminance(output[((center - 2) * size) + center - 2]) +
            Luminance(output[((center + 2) * size) + center + 2]);
        double axial =
            Luminance(output[((center - 2) * size) + center]) +
            Luminance(output[((center + 2) * size) + center]);

        Assert.True(
            diagonal < axial - 0.0001,
            $"Expected diagonal flow below axial flow, got {diagonal} and {axial}.");
    }

    private static PrismCatalogFilterPlan CreatePlan(
        float strokeLength = 2,
        float highlightArea = 0,
        float intensity = 10,
        float pixelScale = 1,
        Matrix3x2? effectiveTransform = null) =>
        PrismCatalogFilterPlanner.Create(
            PrismFilterId.SmudgeStick,
            [
                Number(0, highlightArea),
                Number(1, intensity),
                Number(2, strokeLength)
            ],
            PrismBlendMode.Normal,
            pixelScale,
            effectiveTransform ?? Matrix3x2.Identity,
            new DrawRect(0, 0, 17, 13));

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
                double noise = ((x + y) & 1) == 0 ? -0.12 : 0.12;
                double alpha = x == 0 && y == 0 ? 0 : 0.72;
                pixels[(y * width) + x] =
                    PrismPremultipliedColor.FromStraight(
                        Math.Clamp((left ? 0.16 : 0.84) + noise, 0, 1),
                        Math.Clamp((left ? 0.3 : 0.72) - noise, 0, 1),
                        Math.Clamp((left ? 0.58 : 0.18) + noise, 0, 1),
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

    private static double Luminance(PrismPremultipliedColor pixel) =>
        pixel.Alpha <= 0
            ? 0
            : ((pixel.Red * 0.2126) +
                (pixel.Green * 0.7152) +
                (pixel.Blue * 0.0722)) /
            pixel.Alpha;
}
