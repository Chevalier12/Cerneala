using System.Numerics;
using Cerneala.Drawing;
using Cerneala.Drawing.Prism;
using Cerneala.Drawing.Prism.Catalog;
using Cerneala.Drawing.Prism.ColorManagement;
using Cerneala.Drawing.Prism.Filters;
using Cerneala.Drawing.Prism.Graph;

namespace Cerneala.Tests.Drawing.Prism;

public sealed class PrismChromeFilterTests
{
    [Fact]
    public void PlannerCreatesThreePassSeparablePipeline()
    {
        PrismCatalogFilterPlan plan = CreatePlan(
            detail: 4,
            smoothness: 7);

        Assert.Collection(
            plan.Passes,
            pass =>
            {
                Assert.Equal(PrismCatalogFilterPassKind.Horizontal, pass.Kind);
                Assert.Equal(0, pass.Iteration);
                Assert.False(
                    PrismCatalogFilterPlanner.RequiresOriginalInput(
                        PrismFilterId.Chrome,
                        pass));
            },
            pass =>
            {
                Assert.Equal(PrismCatalogFilterPassKind.Vertical, pass.Kind);
                Assert.Equal(1, pass.Iteration);
                Assert.False(
                    PrismCatalogFilterPlanner.RequiresOriginalInput(
                        PrismFilterId.Chrome,
                        pass));
            },
            pass =>
            {
                Assert.Equal(PrismCatalogFilterPassKind.Direct, pass.Kind);
                Assert.Equal(2, pass.Iteration);
                Assert.True(
                    PrismCatalogFilterPlanner.RequiresOriginalInput(
                        PrismFilterId.Chrome,
                        pass));
            });
        Assert.InRange(plan.Options2.X, 0.5f, 4f);
        Assert.InRange(plan.Options2.Y, 1f, 8f);
        Assert.True(plan.Options2.Z > 1f);
        Assert.InRange(plan.Options2.W, 0.03f, 0.15f);
    }

    [Fact]
    public void AnalyticChromeProducesBandedMonochromeAndKeepsAlpha()
    {
        const int width = 17;
        const int height = 11;
        PrismPremultipliedColor[] source = CreateReliefSource(width, height);
        PrismCatalogFilterPlan plan = CreatePlan(
            detail: 6,
            smoothness: 5);

        PrismPremultipliedColor[] result =
            PrismCatalogFilterMath.Apply(
                plan,
                source,
                width,
                height,
                PrismColorProfile.LinearSrgb);

        Assert.False(source.SequenceEqual(result));
        for (int index = 0; index < result.Length; index++)
        {
            Assert.Equal(source[index].Alpha, result[index].Alpha, 6);
            Assert.Equal(result[index].Red, result[index].Green, 5);
            Assert.Equal(result[index].Red, result[index].Blue, 5);
            AssertFiniteAssociated(result[index]);
        }

        double minimum = result.Min(pixel => pixel.Red);
        double maximum = result.Max(pixel => pixel.Red);
        Assert.True(
            maximum - minimum > 0.25,
            $"Chrome contrast {maximum - minimum:F4} must expose reflective bands.");
        Assert.Equal(default, result[0]);
    }

    [Fact]
    public void DetailAndSmoothnessControlDifferentPipelineInvariants()
    {
        const int width = 17;
        const int height = 11;
        PrismPremultipliedColor[] source = CreateReliefSource(width, height);
        PrismCatalogFilterPlan lowDetail = CreatePlan(1, 7);
        PrismCatalogFilterPlan highDetail = CreatePlan(9, 7);
        PrismCatalogFilterPlan lowSmoothness = CreatePlan(4, 1);
        PrismCatalogFilterPlan highSmoothness = CreatePlan(4, 14);

        Assert.True(highDetail.Options2.Z > lowDetail.Options2.Z);
        Assert.Equal(lowDetail.Options2.X, highDetail.Options2.X);
        Assert.True(highSmoothness.Options2.X > lowSmoothness.Options2.X);
        Assert.True(highSmoothness.Options2.Y > lowSmoothness.Options2.Y);
        Assert.True(highSmoothness.Options2.W > lowSmoothness.Options2.W);

        PrismPremultipliedColor[] lowDetailResult = Apply(lowDetail);
        PrismPremultipliedColor[] highDetailResult = Apply(highDetail);
        PrismPremultipliedColor[] lowSmoothnessResult = Apply(lowSmoothness);
        PrismPremultipliedColor[] highSmoothnessResult = Apply(highSmoothness);

        Assert.False(lowDetailResult.SequenceEqual(highDetailResult));
        Assert.False(lowSmoothnessResult.SequenceEqual(highSmoothnessResult));

        PrismPremultipliedColor[] Apply(PrismCatalogFilterPlan plan) =>
            PrismCatalogFilterMath.Apply(
                plan,
                source,
                width,
                height,
                PrismColorProfile.LinearSrgb);
    }

    private static PrismCatalogFilterPlan CreatePlan(
        float detail,
        float smoothness) =>
        PrismCatalogFilterPlanner.Create(
            PrismFilterId.Chrome,
            [
                new PrismGraphParameter(
                    0,
                    PrismGraphParameterValueKind.Number,
                    numberValue: detail),
                new PrismGraphParameter(
                    1,
                    PrismGraphParameterValueKind.Number,
                    numberValue: smoothness)
            ],
            PrismBlendMode.Normal,
            pixelScale: 1,
            Matrix3x2.Identity,
            new DrawRect(0, 0, 17, 11));

    private static PrismPremultipliedColor[] CreateReliefSource(
        int width,
        int height)
    {
        PrismPremultipliedColor[] source =
            new PrismPremultipliedColor[width * height];
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                double wave =
                    0.5 +
                    (0.28 * Math.Sin(x * 0.72)) +
                    (0.18 * Math.Cos(y * 0.91));
                double alpha = 0.55 + (0.4 * x / (width - 1d));
                source[(y * width) + x] =
                    PrismPremultipliedColor.FromStraight(
                        Math.Clamp(wave, 0, 1),
                        Math.Clamp(0.2 + (0.7 * wave), 0, 1),
                        Math.Clamp(0.9 - (0.6 * wave), 0, 1),
                        alpha);
            }
        }
        source[0] = default;
        return source;
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
