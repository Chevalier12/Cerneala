using System.Numerics;
using Cerneala.Drawing;
using Cerneala.Drawing.Prism;
using Cerneala.Drawing.Prism.Catalog;
using Cerneala.Drawing.Prism.ColorManagement;
using Cerneala.Drawing.Prism.Filters;
using Cerneala.Drawing.Prism.Graph;
using Cerneala.UI.Prism.Definitions;

namespace Cerneala.Tests.Drawing.Prism;

public sealed class PrismTornEdgesFilterTests
{
    [Fact]
    public void PlannerBuildsDualGaussianAndBinaryCompositePasses()
    {
        PrismCatalogFilterPlan plan = CreatePlan(
            imageBalance: 25,
            smoothness: 11,
            contrast: 17);

        Assert.Equal(
            [
                PrismCatalogFilterPassKind.Horizontal,
                PrismCatalogFilterPassKind.Vertical,
                PrismCatalogFilterPassKind.Direct
            ],
            plan.Passes.Select(pass => pass.Kind));
        Assert.Equal([0, 1, 2], plan.Passes.Select(pass => pass.Iteration));
        Assert.Equal(25, plan.GetOption("ImageBalance").X);
        Assert.Equal(11, plan.GetOption("Smoothness").X);
        Assert.Equal(17, plan.GetOption("Contrast").X);
        Assert.InRange(plan.Options5.X, 0.5f, 3.75f);
        Assert.InRange(plan.Options5.Y, plan.Options5.X, 4);
        Assert.Equal(plan.Options5.Z, plan.Passes[0].RadiusX);
        Assert.Equal(plan.Options5.Z, plan.Passes[1].RadiusY);
        Assert.False(PrismCatalogFilterPlanner.RequiresOriginalInput(
            plan.Filter,
            plan.Passes[1]));
        Assert.True(PrismCatalogFilterPlanner.RequiresOriginalInput(
            plan.Filter,
            plan.Passes[2]));
    }

    [Fact]
    public void CpuReferenceIsDeterministicBinaryAndPreservesAlpha()
    {
        const int width = 47;
        const int height = 29;
        PrismPremultipliedColor[] source = CreateSubject(width, height);
        PrismCatalogFilterPlan plan = CreatePlan(25, 4, 17);

        PrismPremultipliedColor[] first = Apply(plan, source, width, height);
        PrismPremultipliedColor[] second = Apply(plan, source, width, height);

        Assert.Equal(first, second);
        Assert.Contains(first, IsBlack);
        Assert.Contains(first, IsWhite);
        for (int index = 0; index < first.Length; index++)
        {
            Assert.Equal(source[index].Alpha, first[index].Alpha, 5);
            Assert.True(IsBlack(first[index]) || IsWhite(first[index]));
        }
    }

    [Fact]
    public void ImageBalanceAndSmoothnessControlTornForegroundBoundary()
    {
        const int width = 61;
        const int height = 37;
        PrismPremultipliedColor[] source = CreateSubject(width, height);
        PrismPremultipliedColor[] light = Apply(
            CreatePlan(12, 3, 17), source, width, height);
        PrismPremultipliedColor[] dark = Apply(
            CreatePlan(38, 3, 17), source, width, height);
        PrismPremultipliedColor[] smooth = Apply(
            CreatePlan(25, 15, 17), source, width, height);
        PrismPremultipliedColor[] torn = Apply(
            CreatePlan(25, 1, 17), source, width, height);

        Assert.True(dark.Count(IsBlack) > light.Count(IsBlack));
        Assert.True(BoundaryTransitions(torn, width) >
            BoundaryTransitions(smooth, width));
    }

    [Fact]
    public void NoiseCannotContaminateFlatRegions()
    {
        const int width = 35;
        const int height = 21;
        PrismPremultipliedColor dark =
            PrismPremultipliedColor.FromStraight(0.05, 0.05, 0.05, 0.7);
        PrismPremultipliedColor light =
            PrismPremultipliedColor.FromStraight(0.95, 0.95, 0.95, 0.7);
        PrismCatalogFilterPlan plan = CreatePlan(25, 1, 17);

        PrismPremultipliedColor[] darkResult = Apply(
            plan,
            Enumerable.Repeat(dark, width * height).ToArray(),
            width,
            height);
        PrismPremultipliedColor[] lightResult = Apply(
            plan,
            Enumerable.Repeat(light, width * height).ToArray(),
            width,
            height);

        Assert.All(darkResult, pixel => Assert.True(IsBlack(pixel)));
        Assert.All(lightResult, pixel => Assert.True(IsWhite(pixel)));
    }

    [Fact]
    public void UsesConfiguredForegroundAndBackgroundColors()
    {
        const int width = 32;
        const int height = 16;
        PrismPremultipliedColor[] source = new PrismPremultipliedColor[width * height];
        for (int index = 0; index < source.Length; index++)
        {
            double luminance = index % width < width / 2 ? 0.04 : 0.96;
            source[index] = PrismPremultipliedColor.FromStraight(
                luminance,
                luminance,
                luminance,
                0.65);
        }
        PrismCatalogFilterPlan plan = CreatePlan(
            25,
            4,
            17,
            foreground: new Color(225, 30, 25),
            background: new Color(25, 45, 225));

        PrismPremultipliedColor[] result = Apply(plan, source, width, height);
        PrismPremultipliedColor foreground =
            result[(height / 2 * width) + 4];
        PrismPremultipliedColor background =
            result[(height / 2 * width) + width - 5];

        Assert.True(foreground.Red > foreground.Blue * 2);
        Assert.True(background.Blue > background.Red * 2);
    }

    private static PrismCatalogFilterPlan CreatePlan(
        float imageBalance,
        float smoothness,
        float contrast,
        Color? foreground = null,
        Color? background = null) =>
        PrismCatalogFilterPlanner.Create(
            PrismFilterId.TornEdges,
            [
                ColorParameter(0, background ?? Color.White),
                Number(1, contrast),
                ColorParameter(2, foreground ?? Color.Black),
                Number(3, imageBalance),
                Number(4, smoothness)
            ],
            PrismBlendMode.Normal,
            1,
            Matrix3x2.Identity,
            new DrawRect(0, 0, 61, 37));

    private static PrismGraphParameter Number(int slot, float value) =>
        new(slot, PrismGraphParameterValueKind.Number, numberValue: value);

    private static PrismGraphParameter ColorParameter(int slot, Color value) =>
        new(slot, PrismGraphParameterValueKind.Color, colorValue: value);

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
            for (int x = 0; x < width; x++)
            {
                double edge = (width * 0.5) +
                    (4 * Math.Sin(y * 0.47)) +
                    (2 * Math.Sin(y * 1.31));
                double distance = x - edge;
                double value = Math.Clamp(0.5 + (distance / 14), 0.05, 0.95);
                double alpha = ((x + y) % 11 == 0) ? 0.45 : 0.8;
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

    private static int BoundaryTransitions(
        PrismPremultipliedColor[] pixels,
        int width)
    {
        int transitions = 0;
        for (int index = width; index < pixels.Length; index++)
        {
            if (IsBlack(pixels[index]) != IsBlack(pixels[index - width]))
            {
                transitions++;
            }
        }
        return transitions;
    }

    private static bool IsBlack(PrismPremultipliedColor pixel) =>
        pixel.Alpha <= 0 || pixel.Red <= 0.00001;

    private static bool IsWhite(PrismPremultipliedColor pixel) =>
        pixel.Alpha <= 0 ||
        Math.Abs(pixel.Red - pixel.Alpha) <= 0.00001;
}
