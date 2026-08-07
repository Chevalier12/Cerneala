using System.Numerics;
using Cerneala.Drawing;
using Cerneala.Drawing.Prism.Catalog;
using Cerneala.Drawing.Prism.ColorManagement;
using Cerneala.Drawing.Prism.Filters;
using Cerneala.Drawing.Prism.Graph;

namespace Cerneala.Tests.Drawing.Prism;

public sealed class PrismPatchworkFilterTests
{
    [Fact]
    public void PlannerEncodesScaleReliefAndSeed()
    {
        PrismCatalogFilterPlan plan = CreatePlan(
            squareSize: 4,
            relief: 8,
            seed: 0x12345678,
            pixelScale: 2);

        PrismCatalogFilterPass pass = Assert.Single(plan.Passes);
        Assert.Equal(PrismCatalogFilterPassKind.Direct, pass.Kind);
        Assert.Equal(8, plan.GetOption("SquareSize").X, 0.00001f);
        Assert.Equal(0.16f, plan.GetOption("Relief").X, 0.00001f);
        Assert.Equal(0x5678, plan.GetOption("Seed").X, 0.00001f);
        Assert.Equal(0x1234, plan.GetOption("Seed").Y, 0.00001f);
        Assert.Equal(4, pass.RadiusX, 0.00001f);
        Assert.Equal(4, pass.RadiusY, 0.00001f);

        PrismCatalogFilterPlan clamped = CreatePlan(0, -10, 0);
        Assert.Equal(1, clamped.GetOption("SquareSize").X, 0.00001f);
        Assert.Equal(0, clamped.GetOption("Relief").X, 0.00001f);
        Assert.Equal(
            1,
            CreatePlan(1, 100, 0).GetOption("Relief").X,
            0.00001f);
    }

    [Fact]
    public void ReliefZeroSamplesTheCenterOfEverySquare()
    {
        const int width = 8;
        const int height = 4;
        PrismPremultipliedColor[] source = CreateHorizontalRamp(width, height);

        PrismPremultipliedColor[] result = Apply(
            CreatePlan(4, 0, 0),
            source,
            width,
            height);

        AssertBlockColor(result, width, 0, 4, 0.4);
        AssertBlockColor(result, width, 4, 8, 0.5);
    }

    [Fact]
    public void ReliefIsDeterministicAndSeededPerCell()
    {
        const int width = 8;
        const int height = 4;
        PrismPremultipliedColor[] source = Enumerable.Repeat(
            PrismPremultipliedColor.FromStraight(0.5, 0.5, 0.5, 1),
            width * height).ToArray();

        PrismPremultipliedColor[] first = Apply(
            CreatePlan(4, 50, 7),
            source,
            width,
            height);
        PrismPremultipliedColor[] repeated = Apply(
            CreatePlan(4, 50, 7),
            source,
            width,
            height);
        PrismPremultipliedColor[] differentSeed = Apply(
            CreatePlan(4, 50, 8),
            source,
            width,
            height);

        for (int index = 0; index < first.Length; index++)
        {
            Assert.Equal(first[index].Red, repeated[index].Red, 0.00001);
            Assert.Equal(first[index].Green, repeated[index].Green, 0.00001);
            Assert.Equal(first[index].Blue, repeated[index].Blue, 0.00001);
            Assert.Equal(first[index].Alpha, repeated[index].Alpha, 0.00001);
        }
        Assert.Contains(
            Enumerable.Range(0, first.Length),
            index => Math.Abs(
                first[index].Red - differentSeed[index].Red) > 0.0001);
        Assert.True(Math.Abs(first[0].Red - first[5].Red) > 0.0001);
        Assert.True(Math.Abs(first[0].Red - first[1].Red) > 0.0001);
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
        float squareSize,
        float relief,
        int seed,
        float pixelScale = 1) =>
        PrismCatalogFilterPlanner.Create(
            PrismFilterId.Patchwork,
            [
                Number(0, relief),
                Integer(1, seed),
                Number(2, squareSize)
            ],
            PrismBlendMode.Normal,
            pixelScale,
            Matrix3x2.Identity,
            new DrawRect(0, 0, 8, 4));

    private static PrismGraphParameter Number(int slot, float value) =>
        new(slot, PrismGraphParameterValueKind.Number, numberValue: value);

    private static PrismGraphParameter Integer(int slot, int value) =>
        new(slot, PrismGraphParameterValueKind.Integer, integerValue: value);

    private static PrismPremultipliedColor[] CreateHorizontalRamp(
        int width,
        int height)
    {
        double[] values = [0, 0.2, 0.6, 0.8, 0.1, 0.3, 0.7, 0.9];
        PrismPremultipliedColor[] pixels = new PrismPremultipliedColor[width * height];
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                double value = values[x];
                pixels[(y * width) + x] =
                    PrismPremultipliedColor.FromStraight(value, value, value, 1);
            }
        }
        return pixels;
    }

    private static void AssertBlockColor(
        PrismPremultipliedColor[] pixels,
        int width,
        int startX,
        int endX,
        double expected)
    {
        for (int y = 0; y < 4; y++)
        {
            for (int x = startX; x < endX; x++)
            {
                PrismPremultipliedColor actual = pixels[(y * width) + x];
                Assert.Equal(expected, actual.Red, 0.00001);
                Assert.Equal(expected, actual.Green, 0.00001);
                Assert.Equal(expected, actual.Blue, 0.00001);
                Assert.Equal(1, actual.Alpha, 0.00001);
            }
        }
    }
}
