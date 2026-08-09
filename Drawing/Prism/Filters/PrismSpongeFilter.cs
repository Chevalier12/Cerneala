using System.Numerics;

namespace Cerneala.Drawing.Prism.Filters;

internal static class PrismSpongeFilter
{
    public static Vector4 ApplyPixel(
        PrismCatalogFilterPlan plan,
        PrismCatalogFilterPass pass,
        Vector4[] source,
        int width,
        int height,
        int x,
        int y) =>
        PrismCatalogTextureMath.Sponge(
            plan, pass, source, width, height, x, y);
}
