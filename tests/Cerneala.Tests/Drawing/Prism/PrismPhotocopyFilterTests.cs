using System.Numerics;
using Cerneala.Drawing;
using Cerneala.Drawing.Prism.Catalog;
using Cerneala.Drawing.Prism.ColorManagement;
using Cerneala.Drawing.Prism.Filters;
using Cerneala.Drawing.Prism.Graph;

namespace Cerneala.Tests.Drawing.Prism;

public sealed class PrismPhotocopyFilterTests
{
    [Fact]
    public void PlannerCreatesThreePassXDogPipeline()
    {
        PrismCatalogFilterPlan plan = CreatePlan(detail: 2, darkness: 8);

        Assert.Collection(
            plan.Passes,
            pass => AssertPass(pass, PrismCatalogFilterPassKind.Horizontal, 0, false),
            pass => AssertPass(pass, PrismCatalogFilterPassKind.Vertical, 1, false),
            pass => AssertPass(pass, PrismCatalogFilterPassKind.Direct, 2, true));
        Assert.Equal(1, plan.Options4.X, 5);
        Assert.Equal(1.8f, plan.Options4.Y, 5);
        Assert.Equal(6, plan.Options4.Z, 5);
        Assert.Equal(0.2f, plan.Options4.W, 5);
    }

    [Fact]
    public void XDogMapsDarkMassesAndPaperToConfiguredColors()
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
            detail: 2,
            darkness: 8,
            foreground: new Color(225, 30, 25),
            background: new Color(25, 45, 225));

        PrismPremultipliedColor[] result = Apply(plan, source, width, height);
        PrismPremultipliedColor dark = result[(height / 2 * width) + 4];
        PrismPremultipliedColor paper = result[(height / 2 * width) + width - 5];

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
    public void PlannerClampsHighDeviceScaleToAValidGaussianPair()
    {
        PrismCatalogFilterPlan plan = CreatePlan(
            detail: 0,
            darkness: 8,
            pixelScale: 8);

        Assert.Equal(3.75f, plan.Options4.X, 5);
        Assert.Equal(4, plan.Options4.Y, 5);
        Assert.Equal(12, plan.Options4.Z, 5);
    }

    [Fact]
    public void DetailAndDarknessOwnSeparateDeterministicControls()
    {
        const int width = 48;
        const int height = 32;
        PrismPremultipliedColor[] source = CreateDetailedSource(width, height);
        PrismCatalogFilterPlan baseline = CreatePlan(2, 8);
        PrismCatalogFilterPlan lowDetail = CreatePlan(0, 8);
        PrismCatalogFilterPlan highDetail = CreatePlan(20, 8);
        PrismCatalogFilterPlan lowDarkness = CreatePlan(2, 4);
        PrismCatalogFilterPlan highDarkness = CreatePlan(2, 20);

        PrismPremultipliedColor[] first = Apply(baseline, source, width, height);
        PrismPremultipliedColor[] repeated = Apply(baseline, source, width, height);
        PrismPremultipliedColor[] coarse = Apply(lowDetail, source, width, height);
        PrismPremultipliedColor[] fine = Apply(highDetail, source, width, height);
        PrismPremultipliedColor[] light = Apply(lowDarkness, source, width, height);
        PrismPremultipliedColor[] dark = Apply(highDarkness, source, width, height);

        Assert.Equal(first, repeated);
        Assert.False(coarse.SequenceEqual(fine));
        Assert.True(highDetail.Options4.X < lowDetail.Options4.X);
        Assert.True(MeanLuminance(dark) < MeanLuminance(light) - 0.05);
        for (int index = 0; index < first.Length; index++)
        {
            Assert.Equal(source[index].Alpha, first[index].Alpha, 6);
            AssertFiniteAssociated(first[index]);
        }
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
                PrismFilterId.Photocopy,
                pass));
    }

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

    private static PrismCatalogFilterPlan CreatePlan(
        float detail,
        float darkness,
        Color? foreground = null,
        Color? background = null,
        float pixelScale = 1) =>
        PrismCatalogFilterPlanner.Create(
            PrismFilterId.Photocopy,
            [
                ColorParameter(0, background ?? Color.White),
                Number(1, darkness),
                Number(2, detail),
                ColorParameter(3, foreground ?? Color.Black)
            ],
            PrismBlendMode.Normal,
            pixelScale,
            Matrix3x2.Identity,
            new DrawRect(0, 0, 48, 32));

    private static PrismGraphParameter Number(int slot, float value) =>
        new(slot, PrismGraphParameterValueKind.Number, numberValue: value);

    private static PrismGraphParameter ColorParameter(int slot, Color value) =>
        new(slot, PrismGraphParameterValueKind.Color, colorValue: value);

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

    private static double MeanLuminance(
        PrismPremultipliedColor[] pixels) =>
        pixels.Where(pixel => pixel.Alpha > 0)
            .Average(pixel =>
                ((pixel.Red + pixel.Green + pixel.Blue) / 3) /
                pixel.Alpha);

    private static void AssertFiniteAssociated(
        PrismPremultipliedColor color)
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
