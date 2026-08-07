using System.Numerics;
using Cerneala.Drawing;
using Cerneala.Drawing.Prism.Catalog;
using Cerneala.Drawing.Prism.ColorManagement;
using Cerneala.Drawing.Prism.Filters;
using Cerneala.Drawing.Prism.Graph;

namespace Cerneala.Tests.Drawing.Prism;

public sealed class PrismStampFilterTests
{
    [Fact]
    public void PlannerCreatesThreePassXDogPipeline()
    {
        PrismCatalogFilterPlan plan = CreatePlan(
            lightDarkBalance: 25,
            smoothness: 5);

        Assert.Collection(
            plan.Passes,
            pass => AssertPass(
                pass,
                PrismCatalogFilterPassKind.Horizontal,
                0,
                requiresOriginal: false),
            pass => AssertPass(
                pass,
                PrismCatalogFilterPassKind.Vertical,
                1,
                requiresOriginal: false),
            pass => AssertPass(
                pass,
                PrismCatalogFilterPassKind.Direct,
                2,
                requiresOriginal: true));
        Assert.Equal(0.5f, plan.Options4.W, 5);
        Assert.Equal(plan.GetOption("Foreground"), plan.Options5);
        Assert.Equal(plan.GetOption("Background"), plan.Options6);
    }

    [Fact]
    public void XDogMapsMassesToConfiguredDuoToneAndPreservesAssociatedAlpha()
    {
        const int width = 32;
        const int height = 16;
        PrismPremultipliedColor[] source = new PrismPremultipliedColor[width * height];
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                double luminance = x < width / 2 ? 0.04 : 0.96;
                source[(y * width) + x] =
                    PrismPremultipliedColor.FromStraight(
                        luminance,
                        luminance,
                        luminance,
                        0.65);
            }
        }
        source[0] = default;
        PrismCatalogFilterPlan plan = CreatePlan(
            lightDarkBalance: 25,
            smoothness: 5,
            foreground: new Color(225, 30, 25),
            background: new Color(25, 45, 225));

        PrismPremultipliedColor[] result = Apply(plan, source, width, height);
        PrismPremultipliedColor dark = result[(height / 2 * width) + 4];
        PrismPremultipliedColor paper =
            result[(height / 2 * width) + width - 5];

        Assert.True(dark.Red > dark.Blue * 2);
        Assert.True(paper.Blue > paper.Red * 2);
        Assert.Equal(default, result[0]);
        for (int index = 1; index < result.Length; index++)
        {
            Assert.Equal(source[index].Alpha, result[index].Alpha, 6);
            AssertFiniteAssociated(result[index]);
        }
    }

    [Fact]
    public void BalanceAndSmoothnessOwnSeparateDeterministicControls()
    {
        const int width = 48;
        const int height = 32;
        PrismPremultipliedColor[] source = CreateDetailedSource(width, height);
        PrismPremultipliedColor[] tonalRamp = CreateTonalRamp(width, height);
        PrismCatalogFilterPlan baseline = CreatePlan(25, 5);
        PrismCatalogFilterPlan coarse = CreatePlan(25, 1);
        PrismCatalogFilterPlan smooth = CreatePlan(25, 50);
        PrismCatalogFilterPlan light = CreatePlan(10, 5);
        PrismCatalogFilterPlan dark = CreatePlan(40, 5);

        PrismPremultipliedColor[] first = Apply(baseline, source, width, height);
        PrismPremultipliedColor[] repeated = Apply(baseline, source, width, height);
        PrismPremultipliedColor[] coarseResult =
            Apply(coarse, source, width, height);
        PrismPremultipliedColor[] smoothResult =
            Apply(smooth, source, width, height);
        PrismPremultipliedColor[] lightResult =
            Apply(light, tonalRamp, width, height);
        PrismPremultipliedColor[] darkResult =
            Apply(dark, tonalRamp, width, height);

        Assert.Equal(first, repeated);
        Assert.False(coarseResult.SequenceEqual(smoothResult));
        Assert.True(smooth.Options4.X > coarse.Options4.X);
        Assert.True(MeanLuminance(darkResult) < MeanLuminance(lightResult) - 0.05);
    }

    [Fact]
    public void PlannerClampsControlsAndDeviceScaleToValidXDogSettings()
    {
        PrismCatalogFilterPlan low = CreatePlan(-100, -100);
        PrismCatalogFilterPlan high = CreatePlan(1000, 1000, pixelScale: 8);

        Assert.Equal(new Vector4(0.5f, 0.9f, 3, 0), low.Options4);
        Assert.Equal(new Vector4(3.75f, 4, 12, 1), high.Options4);
    }

    private static void AssertPass(
        PrismCatalogFilterPass pass,
        PrismCatalogFilterPassKind kind,
        int iteration,
        bool requiresOriginal)
    {
        Assert.Equal(kind, pass.Kind);
        Assert.Equal(iteration, pass.Iteration);
        Assert.Equal(
            requiresOriginal,
            PrismCatalogFilterPlanner.RequiresOriginalInput(
                PrismFilterId.Stamp,
                pass));
    }

    private static PrismCatalogFilterPlan CreatePlan(
        float lightDarkBalance,
        float smoothness,
        Color? foreground = null,
        Color? background = null,
        float pixelScale = 1) =>
        PrismCatalogFilterPlanner.Create(
            PrismFilterId.Stamp,
            [
                ColorParameter(0, background ?? Color.White),
                ColorParameter(1, foreground ?? Color.Black),
                Number(2, lightDarkBalance),
                Number(3, smoothness)
            ],
            PrismBlendMode.Normal,
            pixelScale,
            Matrix3x2.Identity,
            new DrawRect(0, 0, 48, 32));

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

    private static PrismPremultipliedColor[] CreateDetailedSource(
        int width,
        int height)
    {
        PrismPremultipliedColor[] source =
            new PrismPremultipliedColor[width * height];
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                double checker = ((x / 2) + (y / 2)) % 2 == 0 ? -0.12 : 0.12;
                double luminance = Math.Clamp(
                    0.12 + (0.76 * x / (width - 1d)) + checker,
                    0.02,
                    0.98);
                double alpha = 0.3 + (0.65 * y / (height - 1d));
                source[(y * width) + x] =
                    PrismPremultipliedColor.FromStraight(
                        luminance,
                        luminance,
                        luminance,
                        alpha);
            }
        }
        source[0] = default;
        return source;
    }

    private static PrismPremultipliedColor[] CreateTonalRamp(
        int width,
        int height)
    {
        PrismPremultipliedColor[] source =
            new PrismPremultipliedColor[width * height];
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                double luminance = 0.02 + (0.96 * x / (width - 1d));
                double alpha = 0.3 + (0.65 * y / (height - 1d));
                source[(y * width) + x] =
                    PrismPremultipliedColor.FromStraight(
                        luminance,
                        luminance,
                        luminance,
                        alpha);
            }
        }
        return source;
    }

    private static double MeanLuminance(PrismPremultipliedColor[] pixels) =>
        pixels.Where(pixel => pixel.Alpha > 0)
            .Average(pixel =>
                ((pixel.Red + pixel.Green + pixel.Blue) / 3) /
                pixel.Alpha);

    private static void AssertFiniteAssociated(PrismPremultipliedColor color)
    {
        Assert.True(double.IsFinite(color.Red));
        Assert.True(double.IsFinite(color.Green));
        Assert.True(double.IsFinite(color.Blue));
        Assert.True(double.IsFinite(color.Alpha));
        Assert.InRange(color.Alpha, 0, 1);
        Assert.InRange(color.Red, 0, color.Alpha);
        Assert.InRange(color.Green, 0, color.Alpha);
        Assert.InRange(color.Blue, 0, color.Alpha);
    }
}
