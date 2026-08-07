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

public sealed class PrismConteCrayonFilterTests
{
    [Fact]
    public void PlannerBuildsCompressedFlowXDogPipeline()
    {
        PrismCatalogFilterPlan plan = CreatePlan(
            texture: "Burlap",
            scaling: 2,
            relief: 0.6f,
            lightDirection: "BottomLeft",
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
        Assert.Equal(2, plan.GetOption("Texture").X);
        Assert.Equal(6, plan.GetOption("Scaling").X);
        Assert.Equal(5, plan.GetOption("LightDirection").X);
        Assert.InRange(plan.Options8.X, 0.5f, 4);
        Assert.True(plan.Options8.Y > plan.Options8.X);
        Assert.InRange(plan.Options8.Z, 1, 8);
        Assert.InRange(plan.Options8.W, 3, 8);
    }

    [Fact]
    public void CpuReferenceIsDeterministicAlphaSafeAndHonorsEveryControl()
    {
        const int width = 47;
        const int height = 31;
        PrismPremultipliedColor[] source = CreateSubject(width, height);
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
            Apply(CreatePlan(foregroundLevel: 18), source, width, height),
            Apply(CreatePlan(backgroundLevel: 14), source, width, height),
            Apply(CreatePlan(texture: "Brick"), source, width, height),
            Apply(CreatePlan(scaling: 2.5f), source, width, height),
            Apply(CreatePlan(relief: 0.9f), source, width, height),
            Apply(CreatePlan(lightDirection: "Bottom"), source, width, height),
            Apply(
                CreatePlan(
                    foreground: new Color(180, 20, 45),
                    background: new Color(235, 205, 120)),
                source,
                width,
                height)
        ];

        Assert.Equal(baseline, repeated);
        Assert.True(MeanDifference(source, baseline) > 0.02);
        Assert.All(
            variants,
            variant => Assert.True(
                MeanDifference(baseline, variant) > 0.0005));
        Assert.True(RegionRange(baseline, width, width / 3, (2 * width) / 3) > 0.08);

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
    public void ReliefLightingChangesAcrossOpposingDirections()
    {
        const int width = 43;
        const int height = 27;
        PrismPremultipliedColor[] source = CreateSubject(width, height);
        PrismPremultipliedColor[] top = Apply(
            CreatePlan(relief: 1, lightDirection: "Top"),
            source,
            width,
            height);
        PrismPremultipliedColor[] bottom = Apply(
            CreatePlan(relief: 1, lightDirection: "Bottom"),
            source,
            width,
            height);
        PrismPremultipliedColor[] flat = Apply(
            CreatePlan(relief: 0, lightDirection: "Top"),
            source,
            width,
            height);

        Assert.True(MeanDifference(top, bottom) > 0.005);
        Assert.True(MeanDifference(top, flat) > 0.002);
        Assert.True(MeanDifference(bottom, flat) > 0.002);
    }

    private static PrismCatalogFilterPlan CreatePlan(
        float foregroundLevel = 11,
        float backgroundLevel = 7,
        string texture = "Canvas",
        float scaling = 1,
        float relief = 0.2f,
        string lightDirection = "Top",
        Color? foreground = null,
        Color? background = null,
        float pixelScale = 1,
        Matrix3x2? effectiveTransform = null) =>
        PrismCatalogFilterPlanner.Create(
            PrismFilterId.ConteCrayon,
            [
                ColorParameter(0, background ?? Color.White),
                Number(1, backgroundLevel),
                ColorParameter(2, foreground ?? Color.Black),
                Number(3, foregroundLevel),
                Symbol(4, "LightDirection", lightDirection),
                Number(5, relief),
                Number(6, scaling),
                Symbol(7, "Texture", texture)
            ],
            PrismBlendMode.Normal,
            pixelScale,
            effectiveTransform ?? Matrix3x2.Identity,
            new DrawRect(0, 0, 47, 31));

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
                (MathF.Sin(y * 0.31f) * width * 0.14f);
            for (int x = 0; x < width; x++)
            {
                double ramp = x / (double)(width - 1);
                double side = x < curve ? 0.16 : 0.76;
                double texture = ((x * 11) + (y * 7)) % 13 / 90.0;
                double value = Math.Clamp(
                    (side * 0.72) + (ramp * 0.28) + texture,
                    0.02,
                    0.98);
                double alpha = (x + y) % 11 == 0 ? 0.38 : 0.84;
                pixels[(y * width) + x] =
                    PrismPremultipliedColor.FromStraight(
                        value,
                        value * 0.93,
                        value * 0.8,
                        alpha);
            }
        }

        pixels[0] = default;
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

    private static double RegionRange(
        PrismPremultipliedColor[] pixels,
        int width,
        int startX,
        int endX)
    {
        double[] values = pixels
            .Where((pixel, index) =>
                pixel.Alpha > 0 &&
                index % width >= startX &&
                index % width <= endX)
            .Select(Luminance)
            .ToArray();
        return values.Max() - values.Min();
    }

    private static double Luminance(PrismPremultipliedColor pixel) =>
        ((pixel.Red * 0.2126) +
            (pixel.Green * 0.7152) +
            (pixel.Blue * 0.0722)) /
        pixel.Alpha;
}
