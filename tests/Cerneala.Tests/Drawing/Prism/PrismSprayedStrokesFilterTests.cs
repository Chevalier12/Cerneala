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

public sealed class PrismSprayedStrokesFilterTests
{
    [Fact]
    public void PlannerScalesGeometryAndCanonicalizesDirection()
    {
        const int seed = 2_000_000_007;
        PrismCatalogFilterPlan plan = CreatePlan(
            strokeLength: 12,
            sprayRadius: 7,
            direction: "LeftDiagonal",
            seed,
            pixelScale: 2,
            effectiveTransform: Matrix3x2.CreateScale(1.5f));

        Assert.Equal(2, plan.Options0.X);
        Assert.Equal(seed, IntegerBits(plan.Options1));
        Assert.Equal(21, plan.Options2.X);
        Assert.Equal(36, plan.Options3.X);
        Assert.Single(plan.Passes);
        Assert.Equal(
            PrismCatalogFilterPassKind.Direct,
            plan.Passes[0].Kind);
        Assert.Equal(46.08f, plan.Passes[0].RadiusX, 3);
        Assert.Equal(46.08f, plan.Passes[0].RadiusY, 3);
        Assert.False(plan.Passes[0].IsNoOp);
    }

    [Fact]
    public void CpuReferenceIsDeterministicAndEveryControlChangesTheStrokes()
    {
        const int width = 41;
        const int height = 29;
        PrismPremultipliedColor[] source = CreatePattern(width, height);
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
            Apply(CreatePlan(seed: 41), source, width, height),
            Apply(
                CreatePlan(direction: "Horizontal"),
                source,
                width,
                height),
            Apply(
                CreatePlan(strokeLength: 20),
                source,
                width,
                height),
            Apply(
                CreatePlan(sprayRadius: 12),
                source,
                width,
                height)
        ];

        Assert.Equal(baseline, repeated);
        Assert.True(MeanDifference(source, baseline) > 0.0001);
        Assert.All(
            variants,
            variant => Assert.True(
                MeanDifference(baseline, variant) > 0.00001));
    }

    [Fact]
    public void CpuReferencePreservesAssociatedAlphaIncludingTransparency()
    {
        const int width = 31;
        const int height = 23;
        PrismPremultipliedColor[] source = CreatePattern(width, height);

        PrismPremultipliedColor[] result = Apply(
            CreatePlan(strokeLength: 18, sprayRadius: 10, seed: 73),
            source,
            width,
            height);

        for (int index = 0; index < source.Length; index++)
        {
            PrismPremultipliedColor pixel = result[index];
            Assert.Equal(source[index].Alpha, pixel.Alpha, 6);
            Assert.True(double.IsFinite(pixel.Red));
            Assert.True(double.IsFinite(pixel.Green));
            Assert.True(double.IsFinite(pixel.Blue));
            Assert.InRange(pixel.Red, 0, pixel.Alpha);
            Assert.InRange(pixel.Green, 0, pixel.Alpha);
            Assert.InRange(pixel.Blue, 0, pixel.Alpha);
            if (pixel.Alpha == 0)
            {
                Assert.Equal(0, pixel.Red);
                Assert.Equal(0, pixel.Green);
                Assert.Equal(0, pixel.Blue);
            }
        }
    }

    [Fact]
    public void ZeroLengthAndRadiusAreANoOp()
    {
        const int width = 13;
        const int height = 9;
        PrismPremultipliedColor[] source = CreatePattern(width, height);
        PrismCatalogFilterPlan plan = CreatePlan(
            strokeLength: 0,
            sprayRadius: 0);

        PrismPremultipliedColor[] result = Apply(
            plan,
            source,
            width,
            height);

        Assert.True(plan.Passes[0].IsNoOp);
        Assert.All(source.Zip(result), pair =>
        {
            Assert.Equal(pair.First.Red, pair.Second.Red, 5);
            Assert.Equal(pair.First.Green, pair.Second.Green, 5);
            Assert.Equal(pair.First.Blue, pair.Second.Blue, 5);
            Assert.Equal(pair.First.Alpha, pair.Second.Alpha, 5);
        });
    }

    private static PrismCatalogFilterPlan CreatePlan(
        float strokeLength = 12,
        float sprayRadius = 7,
        string direction = "RightDiagonal",
        int seed = 17,
        float pixelScale = 1,
        Matrix3x2? effectiveTransform = null) =>
        PrismCatalogFilterPlanner.Create(
            PrismFilterId.SprayedStrokes,
            [
                new PrismGraphParameter(
                    0,
                    PrismGraphParameterValueKind.Symbol,
                    integerValue: PrismCatalogRuntime.ResolveSymbol(
                        "Direction",
                        direction)),
                new PrismGraphParameter(
                    1,
                    PrismGraphParameterValueKind.Integer,
                    integerValue: seed),
                Number(2, sprayRadius),
                Number(3, strokeLength)
            ],
            PrismBlendMode.Normal,
            pixelScale,
            effectiveTransform ?? Matrix3x2.Identity,
            new DrawRect(0, 0, 41, 29));

    private static PrismGraphParameter Number(int slot, float value) =>
        new(
            slot,
            PrismGraphParameterValueKind.Number,
            numberValue: value);

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

    private static PrismPremultipliedColor[] CreatePattern(
        int width,
        int height)
    {
        PrismPremultipliedColor[] pixels =
            new PrismPremultipliedColor[width * height];
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                double alpha = (x + y) % 17 == 0
                    ? 0
                    : 0.35 + (0.6 * y / Math.Max(height - 1d, 1));
                double checker = ((x / 4) + (y / 3)) % 2 == 0
                    ? 0.18
                    : 0.82;
                double wave =
                    0.5 + (0.5 * Math.Sin((x * 0.7) + (y * 0.31)));
                pixels[(y * width) + x] =
                    PrismPremultipliedColor.FromStraight(
                        checker,
                        wave,
                        1 - (checker * 0.7),
                        alpha);
            }
        }
        return pixels;
    }

    private static int IntegerBits(Vector4 value) =>
        unchecked(
            (int)(
                ((uint)value.Y << 16) |
                ((uint)value.X & 0xffffu)));

    private static double MeanDifference(
        PrismPremultipliedColor[] first,
        PrismPremultipliedColor[] second) =>
        first.Zip(second).Average(pair =>
            Math.Abs(pair.First.Red - pair.Second.Red) +
            Math.Abs(pair.First.Green - pair.Second.Green) +
            Math.Abs(pair.First.Blue - pair.Second.Blue));
}
