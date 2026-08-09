using System.Numerics;

namespace Cerneala.Drawing.Prism.Filters;

internal static class PrismHalftonePatternFilter
{
    public static Vector4 ApplyPixel(
        PrismCatalogFilterPlan plan,
        Vector4 center,
        int width,
        int height,
        int x,
        int y) =>
        PrismCatalogProceduralMath.HalftonePattern(
            plan,
            center,
            new Vector2(x + 0.5f, y + 0.5f),
            new Vector2(width * 0.5f, height * 0.5f));
}
