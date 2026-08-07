using System.Numerics;
using Cerneala.Drawing;
using Cerneala.Drawing.Prism;
using Cerneala.Drawing.Prism.Catalog;
using Cerneala.Drawing.Prism.ColorManagement;
using Cerneala.Drawing.Prism.Filters;
using Cerneala.Drawing.Prism.Graph;
using Cerneala.UI.Prism.Runtime;

namespace Cerneala.Tests.Drawing.Prism;

public sealed class PrismTraceContourFilterTests
{
    [Theory]
    [InlineData("Lower", 1)]
    [InlineData("Upper", 2)]
    public void TracesOnlyTheSelectedSideOfALevelSet(
        string edge,
        int expectedBoundaryX)
    {
        const int width = 5;
        const int height = 3;
        PrismPremultipliedColor[] source = CreateVerticalStep(
            width,
            height,
            splitX: 2,
            lower: 0.25,
            upper: 0.75,
            alpha: 0.6);

        PrismPremultipliedColor[] result = Apply(
            CreatePlan(edge, level: 0.5f),
            source,
            width,
            height);

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                PrismPremultipliedColor pixel = result[(y * width) + x];
                double expected = x == expectedBoundaryX ? 0 : 0.6;
                Assert.Equal(expected, pixel.Red, 6);
                Assert.Equal(expected, pixel.Green, 6);
                Assert.Equal(expected, pixel.Blue, 6);
                Assert.Equal(0.6, pixel.Alpha, 6);
            }
        }
    }

    [Fact]
    public void UsesEightNeighborhoodAndClassifiesLevelEqualityAsUpper()
    {
        const int width = 3;
        const int height = 3;
        PrismPremultipliedColor[] source = Enumerable.Repeat(
            PrismPremultipliedColor.FromStraight(0.25, 0.25, 0.25, 1),
            width * height).ToArray();
        source[0] = PrismPremultipliedColor.FromStraight(0.5, 0.5, 0.5, 1);

        PrismPremultipliedColor[] lower = Apply(
            CreatePlan("Lower", level: 0.5f),
            source,
            width,
            height);
        PrismPremultipliedColor[] upper = Apply(
            CreatePlan("Upper", level: 0.5f),
            source,
            width,
            height);

        Assert.Equal(0, lower[4].Red, 6);
        Assert.Equal(0, upper[0].Red, 6);
        Assert.Equal(1, upper[4].Red, 6);
    }

    [Fact]
    public void ConstantRegionIsWhiteAndPreservesEveryAlpha()
    {
        const int width = 4;
        const int height = 3;
        PrismPremultipliedColor[] source = new PrismPremultipliedColor[width * height];
        for (int index = 0; index < source.Length; index++)
        {
            double alpha = index / (double)(source.Length - 1);
            source[index] = PrismPremultipliedColor.FromStraight(
                0.7,
                0.7,
                0.7,
                alpha);
        }

        PrismPremultipliedColor[] first = Apply(
            CreatePlan("Upper", level: 0.5f),
            source,
            width,
            height);
        PrismPremultipliedColor[] second = Apply(
            CreatePlan("Upper", level: 0.5f),
            source,
            width,
            height);

        Assert.Equal(first, second);
        for (int index = 0; index < source.Length; index++)
        {
            Assert.Equal(source[index].Alpha, first[index].Alpha, 6);
            Assert.Equal(source[index].Alpha, first[index].Red, 6);
            Assert.Equal(source[index].Alpha, first[index].Green, 6);
            Assert.Equal(source[index].Alpha, first[index].Blue, 6);
        }
    }

    private static PrismCatalogFilterPlan CreatePlan(string edge, float level) =>
        PrismCatalogFilterPlanner.Create(
            PrismFilterId.TraceContour,
            [
                new PrismGraphParameter(
                    0,
                    PrismGraphParameterValueKind.Symbol,
                    integerValue: PrismCatalogRuntime.ResolveSymbol("Edge", edge)),
                new PrismGraphParameter(
                    1,
                    PrismGraphParameterValueKind.Number,
                    numberValue: level)
            ],
            PrismBlendMode.Normal,
            pixelScale: 1,
            Matrix3x2.Identity,
            new DrawRect(0, 0, 5, 3));

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

    private static PrismPremultipliedColor[] CreateVerticalStep(
        int width,
        int height,
        int splitX,
        double lower,
        double upper,
        double alpha)
    {
        PrismPremultipliedColor[] pixels =
            new PrismPremultipliedColor[width * height];
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                double value = x < splitX ? lower : upper;
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
}
