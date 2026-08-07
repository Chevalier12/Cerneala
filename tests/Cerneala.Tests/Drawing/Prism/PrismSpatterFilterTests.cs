using System.Numerics;
using Cerneala.Drawing;
using Cerneala.Drawing.Prism;
using Cerneala.Drawing.Prism.Catalog;
using Cerneala.Drawing.Prism.ColorManagement;
using Cerneala.Drawing.Prism.Filters;
using Cerneala.Drawing.Prism.Graph;

namespace Cerneala.Tests.Drawing.Prism;

public sealed class PrismSpatterFilterTests
{
    [Fact]
    public void PlannerScalesRadiusAndPreservesTheFullSeed()
    {
        const int seed = 2_000_000_007;
        PrismCatalogFilterPlan plan = CreatePlan(
            sprayRadius: 10,
            smoothness: 5,
            seed,
            pixelScale: 2,
            effectiveTransform: Matrix3x2.CreateScale(1.5f));

        Assert.Equal(30, plan.Options2.X);
        Assert.Equal(unchecked((uint)seed), plan.SpatterSeed);
        Assert.Single(plan.Passes);
        Assert.Equal(0, plan.Passes[0].RadiusX);
        Assert.Equal(0, plan.Passes[0].RadiusY);
    }

    [Fact]
    public void RecursiveWangFieldIsProgressiveAndFullyPacked()
    {
        PrismSpatterPointField field =
            PrismRecursiveWangBlueNoise.PointField;
        Vector4[] occupied = field.PackedPoints
            .Where(point => point.W > 0)
            .ToArray();

        Assert.Equal(PrismRecursiveWangBlueNoise.GridSize, field.GridSize);
        Assert.Equal(PrismRecursiveWangBlueNoise.LayerCount, field.LayerCount);
        Assert.Equal(PrismRecursiveWangBlueNoise.PointCount, field.PointCount);
        Assert.Equal(field.PointCount, occupied.Length);
        Assert.All(occupied, point =>
        {
            Assert.InRange(point.X, 0, 1);
            Assert.InRange(point.Y, 0, 1);
            Assert.InRange(point.Z, 0.000001f, 0.999999f);
            Assert.Equal(1, point.W);
        });
        Assert.InRange(
            occupied.Count(point => point.Z <= 0.25f),
            (field.PointCount / 4) - 1,
            (field.PointCount / 4) + 1);
    }

    [Fact]
    public void DensitySeedRadiusAndSmoothnessControlDistinctBehavior()
    {
        const int width = 128;
        const int height = 64;
        PrismPremultipliedColor[] source = CreateToneBands(
            width,
            height,
            alpha: 0.7);
        PrismPremultipliedColor[] baseline = Apply(
            CreatePlan(10, 5, 12345),
            source,
            width,
            height);
        PrismPremultipliedColor[] repeated = Apply(
            CreatePlan(10, 5, 12345),
            source,
            width,
            height);
        PrismPremultipliedColor[] reseeded = Apply(
            CreatePlan(10, 5, 54321),
            source,
            width,
            height);
        PrismPremultipliedColor[] larger = Apply(
            CreatePlan(16, 5, 12345),
            source,
            width,
            height);
        PrismPremultipliedColor[] smoother = Apply(
            CreatePlan(10, 14, 12345),
            source,
            width,
            height);

        Assert.Equal(baseline, repeated);
        Assert.True(MeanDifference(baseline, reseeded) > 0.01);
        Assert.True(MeanDifference(baseline, larger) > 0.01);
        Assert.True(MeanDifference(baseline, smoother) > 0.001);
        Assert.True(
            RegionDarkness(baseline, width, 0, (width / 2) - 1) >
            RegionDarkness(baseline, width, width / 2, width - 1) + 0.08);
        Assert.All(baseline, pixel =>
        {
            Assert.Equal(0.7, pixel.Alpha, 6);
            Assert.InRange(pixel.Red, 0, pixel.Alpha);
            Assert.InRange(pixel.Green, 0, pixel.Alpha);
            Assert.InRange(pixel.Blue, 0, pixel.Alpha);
        });
    }

    [Fact]
    public void ZeroRadiusPreservesTheSource()
    {
        const int width = 12;
        const int height = 8;
        PrismPremultipliedColor[] source = CreateToneBands(
            width,
            height,
            alpha: 0.6);

        PrismPremultipliedColor[] result = Apply(
            CreatePlan(0, 5, 1),
            source,
            width,
            height);

        Assert.All(source.Zip(result), pair =>
        {
            Assert.Equal(pair.First.Red, pair.Second.Red, 6);
            Assert.Equal(pair.First.Green, pair.Second.Green, 6);
            Assert.Equal(pair.First.Blue, pair.Second.Blue, 6);
            Assert.Equal(pair.First.Alpha, pair.Second.Alpha, 6);
        });
    }

    private static PrismCatalogFilterPlan CreatePlan(
        float sprayRadius,
        float smoothness,
        int seed,
        float pixelScale = 1,
        Matrix3x2? effectiveTransform = null) =>
        PrismCatalogFilterPlanner.Create(
            PrismFilterId.Spatter,
            [
                new PrismGraphParameter(
                    0,
                    PrismGraphParameterValueKind.Integer,
                    integerValue: seed),
                new PrismGraphParameter(
                    1,
                    PrismGraphParameterValueKind.Number,
                    numberValue: smoothness),
                new PrismGraphParameter(
                    2,
                    PrismGraphParameterValueKind.Number,
                    numberValue: sprayRadius)
            ],
            PrismBlendMode.Normal,
            pixelScale,
            effectiveTransform ?? Matrix3x2.Identity,
            new DrawRect(0, 0, 128, 64));

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

    private static PrismPremultipliedColor[] CreateToneBands(
        int width,
        int height,
        double alpha)
    {
        PrismPremultipliedColor[] pixels =
            new PrismPremultipliedColor[width * height];
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                double tone = x < width / 2 ? 0.12 : 0.82;
                pixels[(y * width) + x] =
                    PrismPremultipliedColor.FromStraight(
                        tone,
                        tone,
                        tone,
                        alpha);
            }
        }
        return pixels;
    }

    private static double MeanDifference(
        PrismPremultipliedColor[] left,
        PrismPremultipliedColor[] right) =>
        left.Zip(right).Average(pair =>
            Math.Abs(pair.First.Red - pair.Second.Red) +
            Math.Abs(pair.First.Green - pair.Second.Green) +
            Math.Abs(pair.First.Blue - pair.Second.Blue));

    private static double RegionDarkness(
        PrismPremultipliedColor[] pixels,
        int width,
        int startX,
        int endX) =>
        pixels
            .Where((_, index) =>
            {
                int x = index % width;
                return x >= startX && x <= endX;
            })
            .Average(pixel =>
                1 - (pixel.Red / pixel.Alpha));
}
