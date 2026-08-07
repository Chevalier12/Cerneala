using System.Numerics;
using Cerneala.Drawing;
using Cerneala.Drawing.Prism.Catalog;
using Cerneala.Drawing.Prism.ColorManagement;
using Cerneala.Drawing.Prism.Filters;
using Cerneala.Drawing.Prism.Graph;

namespace Cerneala.Tests.Drawing.Prism;

public sealed class PrismNotePaperFilterTests
{
    [Fact]
    public void PlannerCreatesThreePassHeightFieldPipeline()
    {
        PrismCatalogFilterPlan plan = CreatePlan(
            imageBalance: 25,
            graininess: 10,
            relief: 11);

        Assert.Collection(
            plan.Passes,
            pass =>
            {
                Assert.Equal(PrismCatalogFilterPassKind.Horizontal, pass.Kind);
                Assert.Equal(0, pass.Iteration);
                Assert.False(
                    PrismCatalogFilterPlanner.RequiresOriginalInput(
                        PrismFilterId.NotePaper,
                        pass));
            },
            pass =>
            {
                Assert.Equal(PrismCatalogFilterPassKind.Vertical, pass.Kind);
                Assert.Equal(1, pass.Iteration);
                Assert.False(
                    PrismCatalogFilterPlanner.RequiresOriginalInput(
                        PrismFilterId.NotePaper,
                        pass));
            },
            pass =>
            {
                Assert.Equal(PrismCatalogFilterPassKind.Direct, pass.Kind);
                Assert.Equal(2, pass.Iteration);
                Assert.True(
                    PrismCatalogFilterPlanner.RequiresOriginalInput(
                        PrismFilterId.NotePaper,
                        pass));
            });

        Assert.Equal(0.5f, plan.Options5.X, 5);
        Assert.Equal(0.5f, plan.Options5.Y, 5);
        Assert.Equal(0.22f, plan.Options5.Z, 5);
    }

    [Fact]
    public void DarkAreasBecomeHolesThatExposeBackgroundColor()
    {
        const int width = 20;
        const int height = 12;
        PrismPremultipliedColor[] source = new PrismPremultipliedColor[width * height];
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                double luminance = x < width / 2 ? 0.05 : 0.95;
                source[(y * width) + x] =
                    PrismPremultipliedColor.FromStraight(
                        luminance,
                        luminance,
                        luminance,
                        0.7);
            }
        }

        PrismCatalogFilterPlan plan = CreatePlan(
            imageBalance: 25,
            graininess: 0,
            relief: 0,
            foreground: new Color(220, 35, 25),
            background: new Color(25, 55, 225));

        PrismPremultipliedColor[] result = Apply(plan, source, width, height);
        PrismPremultipliedColor dark = result[(height / 2 * width) + 2];
        PrismPremultipliedColor bright = result[(height / 2 * width) + width - 3];

        Assert.True(dark.Blue > dark.Red * 2);
        Assert.True(bright.Red > bright.Blue * 2);
        Assert.All(
            result,
            pixel =>
            {
                Assert.Equal(0.7, pixel.Alpha, 6);
                AssertFiniteAssociated(pixel);
            });
    }

    [Fact]
    public void ControlsOwnSeparateVisualInvariantsAndOutputIsDeterministic()
    {
        const int width = 48;
        const int height = 32;
        PrismPremultipliedColor[] source = CreateGradientSource(width, height);
        PrismCatalogFilterPlan baseline = CreatePlan(25, 10, 11);
        PrismCatalogFilterPlan balance = CreatePlan(42, 10, 11);
        PrismCatalogFilterPlan smoothPaper = CreatePlan(25, 0, 11);
        PrismCatalogFilterPlan coarsePaper = CreatePlan(25, 20, 11);
        PrismCatalogFilterPlan flatPaper = CreatePlan(25, 10, 0);
        PrismCatalogFilterPlan deepRelief = CreatePlan(25, 10, 45);

        PrismPremultipliedColor[] first = Apply(baseline, source, width, height);
        PrismPremultipliedColor[] repeated = Apply(baseline, source, width, height);
        PrismPremultipliedColor[] changedBalance = Apply(balance, source, width, height);
        PrismPremultipliedColor[] smooth = Apply(smoothPaper, source, width, height);
        PrismPremultipliedColor[] coarse = Apply(coarsePaper, source, width, height);
        PrismPremultipliedColor[] flat = Apply(flatPaper, source, width, height);
        PrismPremultipliedColor[] deep = Apply(deepRelief, source, width, height);

        Assert.Equal(first, repeated);
        Assert.False(first.SequenceEqual(changedBalance));
        Assert.False(smooth.SequenceEqual(coarse));
        Assert.False(flat.SequenceEqual(deep));
        Assert.True(MeanLuminance(changedBalance) > MeanLuminance(first));
        Assert.True(MeanAdjacentDifference(coarse, width, height) >
            MeanAdjacentDifference(smooth, width, height) + 0.002);
        Assert.True(MeanAdjacentDifference(deep, width, height) >
            MeanAdjacentDifference(flat, width, height) + 0.002);

        for (int index = 0; index < first.Length; index++)
        {
            Assert.Equal(source[index].Alpha, first[index].Alpha, 6);
            AssertFiniteAssociated(first[index]);
        }
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
        float imageBalance,
        float graininess,
        float relief,
        Color? foreground = null,
        Color? background = null) =>
        PrismCatalogFilterPlanner.Create(
            PrismFilterId.NotePaper,
            [
                ColorParameter(0, background ?? Color.White),
                ColorParameter(1, foreground ?? Color.Black),
                Number(2, graininess),
                Number(3, imageBalance),
                Number(4, relief)
            ],
            PrismBlendMode.Normal,
            pixelScale: 1,
            Matrix3x2.Identity,
            new DrawRect(0, 0, 48, 32));

    private static PrismGraphParameter Number(int slot, float value) =>
        new(slot, PrismGraphParameterValueKind.Number, numberValue: value);

    private static PrismGraphParameter ColorParameter(int slot, Color value) =>
        new(
            slot,
            PrismGraphParameterValueKind.Color,
            colorValue: value);

    private static PrismPremultipliedColor[] CreateGradientSource(
        int width,
        int height)
    {
        PrismPremultipliedColor[] source =
            new PrismPremultipliedColor[width * height];
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                double luminance =
                    (0.08 + (0.84 * x / (width - 1d))) +
                    (0.04 * Math.Sin(y * 0.7));
                double alpha = 0.35 + (0.6 * y / (height - 1d));
                source[(y * width) + x] =
                    PrismPremultipliedColor.FromStraight(
                        Math.Clamp(luminance, 0, 1),
                        Math.Clamp(luminance * 0.92, 0, 1),
                        Math.Clamp(luminance * 0.78, 0, 1),
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

    private static double MeanAdjacentDifference(
        PrismPremultipliedColor[] pixels,
        int width,
        int height)
    {
        double total = 0;
        int count = 0;
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width - 1; x++)
            {
                PrismPremultipliedColor left = pixels[(y * width) + x];
                PrismPremultipliedColor right = pixels[(y * width) + x + 1];
                if (left.Alpha <= 0 || right.Alpha <= 0)
                {
                    continue;
                }
                total += Math.Abs(
                    (left.Red / left.Alpha) -
                    (right.Red / right.Alpha));
                count++;
            }
        }
        return total / count;
    }

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
