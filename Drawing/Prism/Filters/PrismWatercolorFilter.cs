using System.Numerics;

namespace Cerneala.Drawing.Prism.Filters;

internal static class PrismWatercolorFilter
{
    public static Vector4[] Apply(
        PrismCatalogFilterPlan plan,
        Vector4[] source,
        int width,
        int height) =>
        PrismCatalogInkMath.Watercolor(plan, source, width, height);
}
