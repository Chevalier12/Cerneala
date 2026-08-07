using System.Numerics;

namespace Cerneala.Drawing.Prism.Filters;

internal static class PrismFacetFilter
{
    public static Vector4 ApplyPixel(
        Vector4[] source,
        int width,
        int height,
        int x,
        int y) =>
        PrismCatalogFilterMath.AnisotropicKuwahara(
            source,
            width,
            height,
            x,
            y);
}
