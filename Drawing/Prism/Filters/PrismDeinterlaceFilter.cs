using System.Numerics;

namespace Cerneala.Drawing.Prism.Filters;

internal static class PrismDeinterlaceFilter
{
    public static Vector4 ApplyPixel(
        PrismCatalogFilterPlan plan,
        Vector4[] source,
        int width,
        int height,
        int x,
        int y,
        Vector4 center) =>
        PrismCatalogProceduralMath.Deinterlace(
            plan,
            source,
            width,
            height,
            x,
            y,
            center);
}
