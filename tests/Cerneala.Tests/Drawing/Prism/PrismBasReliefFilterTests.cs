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

public sealed class PrismBasReliefFilterTests
{
    [Fact]
    public void PlannerBuildsGuidedFilterPassesAndCanonicalLight()
    {
        PrismCatalogFilterPlan plan = CreatePlan(
            smoothness: 3,
            lightDirection: "BottomLeft",
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
        Assert.Equal(8, plan.Passes[0].RadiusX);
        Assert.Equal(8, plan.Passes[1].RadiusY);
        Assert.Equal(8, plan.Passes[3].RadiusX);
        Assert.Equal(8, plan.Passes[4].RadiusY);
        Assert.Equal(1, plan.Passes[5].RadiusX);
        Assert.Equal(5, plan.GetOption("LightDirection").X);
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
    public void CpuReferenceIsDeterministicDirectionalAndHonorsEveryControl()
    {
        const int width = 25;
        const int height = 17;
        PrismPremultipliedColor[] source = CreateNoisyStep(width, height);
        PrismPremultipliedColor[] left = Apply(
            CreatePlan(lightDirection: "Left"),
            source,
            width,
            height);
        PrismPremultipliedColor[] repeated = Apply(
            CreatePlan(lightDirection: "Left"),
            source,
            width,
            height);
        PrismPremultipliedColor[] right = Apply(
            CreatePlan(lightDirection: "Right"),
            source,
            width,
            height);
        PrismPremultipliedColor[][] variants =
        [
            Apply(CreatePlan(detail: 0), source, width, height),
            Apply(CreatePlan(smoothness: 7), source, width, height),
            Apply(
                CreatePlan(
                    foreground: new Color(20, 60, 220),
                    background: new Color(240, 180, 30)),
                source,
                width,
                height)
        ];

        Assert.Equal(left, repeated);
        Assert.True(BoundaryMean(left, width) > BoundaryMean(right, width));
        Assert.All(
            variants,
            variant => Assert.True(
                MeanDifference(left, variant) > 0.0001));

        for (int index = 0; index < source.Length; index++)
        {
            PrismPremultipliedColor pixel = left[index];
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
        float detail = 13,
        float smoothness = 3,
        string lightDirection = "BottomLeft",
        Color? foreground = null,
        Color? background = null,
        float pixelScale = 1,
        Matrix3x2? effectiveTransform = null) =>
        PrismCatalogFilterPlanner.Create(
            PrismFilterId.BasRelief,
            [
                ColorParameter(0, background ?? Color.White),
                Number(1, detail),
                ColorParameter(2, foreground ?? Color.Black),
                Symbol(3, "LightDirection", lightDirection),
                Number(4, smoothness)
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

    private static PrismGraphParameter ColorParameter(
        int slot,
        Color value) =>
        new(
            slot,
            PrismGraphParameterValueKind.Color,
            colorValue: value);

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

    private static PrismPremultipliedColor[] CreateNoisyStep(
        int width,
        int height)
    {
        PrismPremultipliedColor[] pixels =
            new PrismPremultipliedColor[width * height];
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                double noise = ((x + y) & 1) == 0 ? -0.04 : 0.04;
                double value = Math.Clamp(
                    (x < width / 2 ? 0.2 : 0.8) + noise,
                    0,
                    1);
                double alpha = x == 0 && y == 0 ? 0 : 0.7;
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
            .Where((_, index) => index % width is 11 or 12 or 13)
            .Average(Luminance);

    private static double Luminance(PrismPremultipliedColor pixel) =>
        pixel.Alpha <= 0
            ? 0
            : ((pixel.Red * 0.2126) +
                (pixel.Green * 0.7152) +
                (pixel.Blue * 0.0722)) /
            pixel.Alpha;
}
