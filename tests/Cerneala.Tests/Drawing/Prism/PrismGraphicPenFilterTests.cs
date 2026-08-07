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

public sealed class PrismGraphicPenFilterTests
{
    [Fact]
    public void PlannerBuildsFivePassFlowXDogPipelineAndPacksControls()
    {
        PrismCatalogFilterPlan plan = CreatePlan(
            strokeLength: 12,
            strokeDirection: "Vertical",
            pixelScale: 2,
            effectiveTransform: Matrix3x2.CreateScale(1.5f));

        Assert.Equal(5, plan.Passes.Length);
        Assert.Equal(
            [0, 1, 4, 5, 6],
            plan.Passes.Select(pass => pass.Iteration));
        Assert.Equal(
            [
                PrismCatalogFilterPassKind.Direct,
                PrismCatalogFilterPassKind.Iteration,
                PrismCatalogFilterPassKind.Direct,
                PrismCatalogFilterPassKind.Iteration,
                PrismCatalogFilterPassKind.Direct
            ],
            plan.Passes.Select(pass => pass.Kind));
        Assert.False(PrismCatalogFilterPlanner.RequiresOriginalInput(
            plan.Filter,
            plan.Passes[1]));
        Assert.True(PrismCatalogFilterPlanner.RequiresOriginalInput(
            plan.Filter,
            plan.Passes[2]));
        Assert.True(PrismCatalogFilterPlanner.RequiresOriginalInput(
            plan.Filter,
            plan.Passes[4]));
        Assert.Equal(36, plan.GetOption("StrokeLength").X);
        Assert.Equal(3, plan.GetOption("StrokeDirection").X);
        Assert.InRange(plan.Options5.X, 0.5f, 4);
        Assert.True(plan.Options5.Y > plan.Options5.X);
        Assert.InRange(plan.Options5.Z, 2, 8);
        Assert.InRange(plan.Options5.W, 3, 8);
        Assert.Equal(3, plan.Options6.X);
        Assert.Equal(1, plan.Options6.Y);
        Assert.Equal(0.98f, plan.Options6.Z, 3);
    }

    [Fact]
    public void CpuReferenceIsDeterministicAlphaSafeAndHonorsEveryControl()
    {
        const int width = 53;
        const int height = 37;
        PrismPremultipliedColor[] source = CreateSubject(width, height);
        PrismPremultipliedColor[] baseline = Apply(
            CreatePlan(), source, width, height);
        PrismPremultipliedColor[] repeated = Apply(
            CreatePlan(), source, width, height);
        PrismPremultipliedColor[][] variants =
        [
            Apply(CreatePlan(strokeLength: 6), source, width, height),
            Apply(CreatePlan(lightDarkBalance: 78), source, width, height),
            Apply(CreatePlan(strokeDirection: "Horizontal"), source, width, height),
            Apply(
                CreatePlan(
                    foreground: new Color(115, 18, 35),
                    background: new Color(238, 218, 142)),
                source,
                width,
                height)
        ];

        Assert.Equal(baseline, repeated);
        Assert.True(MeanDifference(source, baseline) > 0.03);
        Assert.All(
            variants,
            variant => Assert.True(
                MeanDifference(baseline, variant) > 0.002));

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
    public void TonalHatchingUsesFiniteSegmentsInRequestedDirection()
    {
        const int width = 61;
        const int height = 41;
        PrismPremultipliedColor[] source = Enumerable.Repeat(
            PrismPremultipliedColor.FromStraight(0.28, 0.28, 0.28, 1),
            width * height).ToArray();
        PrismPremultipliedColor[] horizontal = Apply(
            CreatePlan(
                strokeLength: 9,
                lightDarkBalance: 72,
                strokeDirection: "Horizontal"),
            source,
            width,
            height);
        PrismPremultipliedColor[] vertical = Apply(
            CreatePlan(
                strokeLength: 9,
                lightDarkBalance: 72,
                strokeDirection: "Vertical"),
            source,
            width,
            height);

        Assert.True(MeanDifference(horizontal, vertical) > 0.01);
        Assert.InRange(CountDarkTransitions(horizontal, width, horizontal: true), 4, 80);
        Assert.InRange(CountDarkTransitions(vertical, width, horizontal: false), 4, 80);
    }

    private static PrismCatalogFilterPlan CreatePlan(
        float strokeLength = 15,
        float lightDarkBalance = 50,
        string strokeDirection = "RightDiagonal",
        Color? foreground = null,
        Color? background = null,
        float pixelScale = 1,
        Matrix3x2? effectiveTransform = null) =>
        PrismCatalogFilterPlanner.Create(
            PrismFilterId.GraphicPen,
            [
                ColorParameter(0, background ?? Color.White),
                ColorParameter(1, foreground ?? Color.Black),
                Number(2, lightDarkBalance),
                Symbol(3, "StrokeDirection", strokeDirection),
                Number(4, strokeLength)
            ],
            PrismBlendMode.Normal,
            pixelScale,
            effectiveTransform ?? Matrix3x2.Identity,
            new DrawRect(0, 0, 61, 41));

    private static PrismGraphParameter Number(int slot, float value) =>
        new(slot, PrismGraphParameterValueKind.Number, numberValue: value);

    private static PrismGraphParameter Symbol(
        int slot,
        string property,
        string value) =>
        new(
            slot,
            PrismGraphParameterValueKind.Symbol,
            integerValue: PrismCatalogRuntime.ResolveSymbol(property, value));

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

    private static PrismPremultipliedColor[] CreateSubject(
        int width,
        int height)
    {
        PrismPremultipliedColor[] pixels =
            new PrismPremultipliedColor[width * height];
        for (int y = 0; y < height; y++)
        {
            float curve =
                (width * 0.48f) +
                (MathF.Sin(y * 0.29f) * width * 0.15f);
            for (int x = 0; x < width; x++)
            {
                double ramp = x / (double)(width - 1);
                double side = x < curve ? 0.13 : 0.79;
                double texture = ((x * 13) + (y * 7)) % 17 / 120.0;
                double value = Math.Clamp(
                    (side * 0.7) + (ramp * 0.3) + texture,
                    0.02,
                    0.98);
                double alpha = (x + y) % 13 == 0 ? 0.37 : 0.86;
                pixels[(y * width) + x] =
                    PrismPremultipliedColor.FromStraight(
                        value,
                        value * 0.92,
                        value * 0.78,
                        alpha);
            }
        }

        pixels[0] = default;
        return pixels;
    }

    private static int CountDarkTransitions(
        PrismPremultipliedColor[] pixels,
        int width,
        bool horizontal)
    {
        int height = pixels.Length / width;
        int transitions = 0;
        bool previous = false;
        int length = horizontal ? width : height;
        for (int index = 0; index < length; index++)
        {
            int x = horizontal ? index : width / 2;
            int y = horizontal ? height / 2 : index;
            PrismPremultipliedColor pixel = pixels[(y * width) + x];
            bool dark = pixel.Red / pixel.Alpha < 0.5;
            if (index > 0 && dark != previous)
            {
                transitions++;
            }
            previous = dark;
        }
        return transitions;
    }

    private static double MeanDifference(
        PrismPremultipliedColor[] first,
        PrismPremultipliedColor[] second) =>
        first.Zip(second, (left, right) =>
                Math.Abs(left.Red - right.Red) +
                Math.Abs(left.Green - right.Green) +
                Math.Abs(left.Blue - right.Blue))
            .Average();
}
