using System.Numerics;
using Cerneala.Drawing;
using Cerneala.Drawing.Prism.Catalog;
using Cerneala.Drawing.Prism.ColorManagement;
using Cerneala.Drawing.Prism.Filters;
using Cerneala.Drawing.Prism.Graph;

namespace Cerneala.Tests.Drawing.Prism;

internal static class PrismStainedGlassTestData
{
    internal static PrismCatalogFilterPlan CreatePlan(
        float cellSize,
        float borderThickness,
        float lightIntensity,
        Color borderColor,
        int seed,
        int width,
        int height,
        float pixelScale = 1) =>
        PrismCatalogFilterPlanner.Create(
            PrismFilterId.StainedGlass,
            [
                ColorParameter(0, borderColor),
                Number(1, borderThickness),
                Number(2, cellSize),
                Number(3, lightIntensity),
                Integer(4, seed)
            ],
            PrismBlendMode.Normal,
            pixelScale,
            Matrix3x2.Identity,
            new DrawRect(0, 0, width, height));

    private static PrismGraphParameter Number(int slot, float value) =>
        new(slot, PrismGraphParameterValueKind.Number, numberValue: value);

    private static PrismGraphParameter ColorParameter(
        int slot,
        Color value) =>
        new(slot, PrismGraphParameterValueKind.Color, colorValue: value);

    private static PrismGraphParameter Integer(int slot, int value) =>
        new(slot, PrismGraphParameterValueKind.Integer, integerValue: value);

    internal static PrismPremultipliedColor[] CreateSubject(
        int width,
        int height)
    {
        PrismPremultipliedColor[] source =
            new PrismPremultipliedColor[width * height];
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                double red = 0.1 + (0.8 * x / (width - 1d));
                double green = 0.1 + (0.8 * y / (height - 1d));
                double blue = 0.15 +
                    (0.7 * ((x + y) % 11) / 10d);
                double alpha = 0.25 +
                    (0.7 * y / (height - 1d));
                source[(y * width) + x] =
                    PrismPremultipliedColor.FromStraight(
                        red,
                        green,
                        blue,
                        alpha);
            }
        }
        source[0] = default;
        return source;
    }

}
