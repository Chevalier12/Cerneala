using System.Numerics;

namespace Cerneala.Drawing.Prism.Filters;

internal static class PrismSprayedStrokesFilter
{
    public static Vector4 ApplyPixel(
        PrismCatalogFilterPlan plan,
        Vector4[] source,
        int width,
        int height,
        int x,
        int y) =>
        PrismCatalogArtisticMath.SprayedStrokes(
            plan, source, width, height, x, y);
}
