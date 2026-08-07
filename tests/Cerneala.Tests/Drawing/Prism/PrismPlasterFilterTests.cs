using System.Numerics;
using Cerneala.Drawing;
using Cerneala.Drawing.Prism.Catalog;
using Cerneala.Drawing.Prism.ColorManagement;
using Cerneala.Drawing.Prism.Filters;
using Cerneala.Drawing.Prism.Graph;
using Cerneala.UI.Prism.Runtime;

namespace Cerneala.Tests.Drawing.Prism;

public sealed class PrismPlasterFilterTests
{
    [Fact]
    public void PlannerCreatesFivePassGuidedReliefPipeline()
    {
        PrismCatalogFilterPlan plan = CreatePlan(
            imageBalance: 20,
            smoothness: 6,
            lightDirection: "TopLeft");

        Assert.Collection(
            plan.Passes,
            pass => AssertPass(
                pass,
                PrismCatalogFilterPassKind.Horizontal,
                iteration: 0,
                requiresOriginal: false),
            pass => AssertPass(
                pass,
                PrismCatalogFilterPassKind.Vertical,
                iteration: 1,
                requiresOriginal: false),
            pass => AssertPass(
                pass,
                PrismCatalogFilterPassKind.Horizontal,
                iteration: 2,
                requiresOriginal: false),
            pass => AssertPass(
                pass,
                PrismCatalogFilterPassKind.Vertical,
                iteration: 3,
                requiresOriginal: true),
            pass => AssertPass(
                pass,
                PrismCatalogFilterPassKind.Direct,
                iteration: 4,
                requiresOriginal: true));

        Assert.Equal(0.4f, plan.Options5.X, 5);
        Assert.InRange(plan.Options5.Y, 1, 12);
        Assert.InRange(plan.Options5.Z, 0.00001f, 0.05f);
        Assert.Equal(7, plan.Options6.X);
    }

    [Fact]
    public void GuidedLuminanceSuppressesTextureWithoutSmearingMainEdge()
    {
        const int width = 48;
        const int height = 20;
        Vector4[] source = new Vector4[width * height];
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float baseValue = x < width / 2 ? 0.18f : 0.82f;
                float texture = ((x + y) & 1) == 0 ? -0.07f : 0.07f;
                float value = baseValue + texture;
                source[(y * width) + x] = new Vector4(value, value, value, 1);
            }
        }

        float[] filtered = PrismPlasterFilter.GuidedLuminanceForTesting(
            source,
            width,
            height,
            radius: 5,
            epsilon: 0.012f);

        float sourceTexture = MeanCheckerDifference(
            source.Select(pixel => pixel.X).ToArray(),
            width,
            height,
            3,
            width / 2 - 3);
        float filteredTexture = MeanCheckerDifference(
            filtered,
            width,
            height,
            3,
            width / 2 - 3);
        float edge = ColumnMean(filtered, width, height, width / 2 + 2) -
            ColumnMean(filtered, width, height, width / 2 - 3);

        Assert.True(filteredTexture < sourceTexture * 0.7f);
        Assert.True(edge > 0.5f);
    }

    [Fact]
    public void ReliefUsesDirectionAndDuotoneWhilePreservingAlpha()
    {
        const int width = 40;
        const int height = 28;
        PrismPremultipliedColor[] source = CreateSubject(width, height);
        Color foreground = new(225, 40, 35);
        Color background = new(30, 65, 225);
        PrismCatalogFilterPlan topLeft = CreatePlan(
            20,
            4,
            "TopLeft",
            foreground,
            background);
        PrismCatalogFilterPlan bottomRight = CreatePlan(
            20,
            4,
            "BottomRight",
            foreground,
            background);

        PrismPremultipliedColor[] first = Apply(
            topLeft,
            source,
            width,
            height);
        PrismPremultipliedColor[] opposite = Apply(
            bottomRight,
            source,
            width,
            height);

        Assert.False(first.SequenceEqual(opposite));
        Assert.Contains(first, pixel => pixel.Red > pixel.Blue * 1.5);
        Assert.Contains(first, pixel => pixel.Blue > pixel.Red * 1.5);
        for (int index = 0; index < first.Length; index++)
        {
            Assert.Equal(source[index].Alpha, first[index].Alpha, 5);
            AssertFiniteAssociated(first[index]);
        }
    }

    [Fact]
    public void ImageBalanceAndSmoothnessOwnVisibleDeterministicControls()
    {
        const int width = 40;
        const int height = 28;
        PrismPremultipliedColor[] source = CreateSubject(width, height);
        PrismCatalogFilterPlan baseline = CreatePlan(20, 2, "TopLeft");
        PrismCatalogFilterPlan balance = CreatePlan(42, 2, "TopLeft");
        PrismCatalogFilterPlan smooth = CreatePlan(20, 12, "TopLeft");

        PrismPremultipliedColor[] first = Apply(baseline, source, width, height);
        PrismPremultipliedColor[] repeated = Apply(baseline, source, width, height);
        PrismPremultipliedColor[] changedBalance = Apply(balance, source, width, height);
        PrismPremultipliedColor[] changedSmoothness = Apply(smooth, source, width, height);

        Assert.Equal(first, repeated);
        Assert.False(first.SequenceEqual(changedBalance));
        Assert.False(first.SequenceEqual(changedSmoothness));
        Assert.True(
            MeanAdjacentDifference(changedSmoothness, width, height) <
            MeanAdjacentDifference(first, width, height));
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
                PrismFilterId.Plaster,
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
        float imageBalance,
        float smoothness,
        string lightDirection,
        Color? foreground = null,
        Color? background = null) =>
        PrismCatalogFilterPlanner.Create(
            PrismFilterId.Plaster,
            [
                ColorParameter(0, background ?? Color.White),
                ColorParameter(1, foreground ?? Color.Black),
                Number(2, imageBalance),
                Symbol(3, "LightDirection", lightDirection),
                Number(4, smoothness)
            ],
            PrismBlendMode.Normal,
            pixelScale: 1,
            Matrix3x2.Identity,
            new DrawRect(0, 0, 40, 28));

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

    private static PrismGraphParameter ColorParameter(int slot, Color value) =>
        new(
            slot,
            PrismGraphParameterValueKind.Color,
            colorValue: value);

    private static PrismPremultipliedColor[] CreateSubject(
        int width,
        int height)
    {
        PrismPremultipliedColor[] source =
            new PrismPremultipliedColor[width * height];
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                double horizontal = x / (width - 1d);
                double ridge = Math.Exp(
                    -Math.Pow((x - (width * 0.54)) / 4.2, 2));
                double texture = 0.055 * Math.Sin((x * 1.7) + (y * 2.1));
                double luminance = Math.Clamp(
                    0.08 + (horizontal * 0.72) - (ridge * 0.28) + texture,
                    0,
                    1);
                double alpha = 0.3 + (0.65 * y / (height - 1d));
                source[(y * width) + x] =
                    PrismPremultipliedColor.FromStraight(
                        luminance,
                        luminance * 0.92,
                        luminance * 0.8,
                        alpha);
            }
        }
        source[0] = default;
        return source;
    }

    private static float MeanCheckerDifference(
        float[] values,
        int width,
        int height,
        int startX,
        int endX)
    {
        float total = 0;
        int count = 0;
        for (int y = 0; y < height; y++)
        {
            for (int x = startX; x < endX; x++)
            {
                total += MathF.Abs(
                    values[(y * width) + x] -
                    values[(y * width) + x + 1]);
                count++;
            }
        }
        return total / count;
    }

    private static float ColumnMean(
        float[] values,
        int width,
        int height,
        int x)
    {
        float total = 0;
        for (int y = 0; y < height; y++)
        {
            total += values[(y * width) + x];
        }
        return total / height;
    }

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
