using System.Numerics;
using Cerneala.Drawing;
using Cerneala.Drawing.Prism.Catalog;
using Cerneala.Drawing.Prism.ColorManagement;
using Cerneala.Drawing.Prism.Filters;
using Cerneala.Drawing.Prism.Graph;

namespace Cerneala.Tests.Drawing.Prism;

public sealed class PrismMosaicTilesFilterTests
{
    [Fact]
    public void PlannerEncodesScaleAwareMosaicSettings()
    {
        PrismCatalogFilterPlan plan = CreatePlan(
            tileSize: 4,
            groutWidth: 1,
            lightenGrout: 5,
            pixelScale: 2);

        PrismCatalogFilterPass pass = Assert.Single(plan.Passes);
        Assert.Equal(PrismCatalogFilterPassKind.Direct, pass.Kind);
        Assert.Equal(8, plan.GetOption("TileSize").X, 0.00001f);
        Assert.Equal(2, plan.GetOption("GroutWidth").X, 0.00001f);
        Assert.Equal(0.5f, plan.GetOption("LightenGrout").X, 0.00001f);
        Assert.Equal(4, pass.RadiusX, 0.00001f);
        Assert.Equal(4, pass.RadiusY, 0.00001f);

        PrismCatalogFilterPlan clamped = CreatePlan(0, 20, 20);
        PrismCatalogFilterPass clampedPass = Assert.Single(clamped.Passes);
        Assert.Equal(1, clamped.GetOption("TileSize").X, 0.00001f);
        Assert.Equal(1, clamped.GetOption("GroutWidth").X, 0.00001f);
        Assert.Equal(1, clamped.GetOption("LightenGrout").X, 0.00001f);
        Assert.Equal(0.5f, clampedPass.RadiusX, 0.00001f);
        Assert.Equal(0.5f, clampedPass.RadiusY, 0.00001f);
    }

    [Fact]
    public void SamplesBlockCenterAndLightensOnlyTheGrout()
    {
        const int width = 8;
        const int height = 4;
        PrismPremultipliedColor[] source = CreateHorizontalRamp(width, height);

        PrismPremultipliedColor[] blocks = Apply(
            CreatePlan(4, 0, 0),
            source,
            width,
            height);
        AssertBlockColor(blocks, width, 0, 4, 0.4);
        AssertBlockColor(blocks, width, 4, 8, 0.5);

        PrismPremultipliedColor[] grouted = Apply(
            CreatePlan(4, 2, 10),
            source,
            width,
            height);
        AssertColor(grouted[0], 1, 1, 1, 1);
        AssertColor(grouted[width + 1], 0.4, 0.4, 0.4, 1);
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
        float tileSize,
        float groutWidth,
        float lightenGrout,
        float pixelScale = 1) =>
        PrismCatalogFilterPlanner.Create(
            PrismFilterId.MosaicTiles,
            [
                Number(0, groutWidth),
                Number(1, lightenGrout),
                Number(2, tileSize)
            ],
            PrismBlendMode.Normal,
            pixelScale,
            Matrix3x2.Identity,
            new DrawRect(0, 0, 8, 4));

    private static PrismGraphParameter Number(int slot, float value) =>
        new(slot, PrismGraphParameterValueKind.Number, numberValue: value);

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
                AssertColor(
                    pixels[(y * width) + x],
                    expected,
                    expected,
                    expected,
                    1);
            }
        }
    }

    private static void AssertColor(
        PrismPremultipliedColor actual,
        double red,
        double green,
        double blue,
        double alpha)
    {
        Assert.Equal(red, actual.Red, 0.00001);
        Assert.Equal(green, actual.Green, 0.00001);
        Assert.Equal(blue, actual.Blue, 0.00001);
        Assert.Equal(alpha, actual.Alpha, 0.00001);
    }
}
