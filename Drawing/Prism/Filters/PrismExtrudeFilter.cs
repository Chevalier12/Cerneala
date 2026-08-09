using System.Numerics;

namespace Cerneala.Drawing.Prism.Filters;

internal static class PrismExtrudeFilter
{
    public static Vector4 ApplyPixel(
        PrismCatalogFilterPlan plan,
        Vector4[] source,
        int width,
        int height,
        int x,
        int y) =>
        PrismCatalogGeometryMath.Extrude(
            plan, source, width, height, x, y);
}
