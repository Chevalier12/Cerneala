using System.Numerics;

namespace Cerneala.Drawing.Prism.Filters;

internal static class PrismDarkStrokesFilter
{
    public static Vector4[] Apply(
        PrismCatalogFilterPlan plan,
        Vector4[] source,
        int width,
        int height) =>
        PrismCatalogFilterMath.DarkStrokes(plan, source, width, height);
}
