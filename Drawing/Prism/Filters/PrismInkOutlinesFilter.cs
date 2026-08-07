using System.Numerics;

namespace Cerneala.Drawing.Prism.Filters;

internal static class PrismInkOutlinesFilter
{
    public static Vector4[] Apply(
        PrismCatalogFilterPlan plan,
        Vector4[] source,
        int width,
        int height) =>
        PrismCatalogFilterMath.InkOutlines(plan, source, width, height);
}
