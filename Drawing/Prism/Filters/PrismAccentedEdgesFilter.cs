using System.Numerics;

namespace Cerneala.Drawing.Prism.Filters;

internal static class PrismAccentedEdgesFilter
{
    public static Vector4[] Apply(
        PrismCatalogFilterPlan plan,
        Vector4[] source,
        int width,
        int height) =>
        PrismCatalogInkMath.AccentedEdges(
            plan,
            source,
            width,
            height);
}
