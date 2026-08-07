using System.Numerics;

namespace Cerneala.Drawing.Prism.Filters;

internal static class PrismCrosshatchFilter
{
    public static Vector4 ApplyPixel(
        PrismCatalogFilterPlan plan,
        Vector4 center,
        int x,
        int y) =>
        PrismCatalogFilterMath.Crosshatch(plan, center, x, y);
}
