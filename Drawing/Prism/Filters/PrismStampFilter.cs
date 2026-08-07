using System.Numerics;

namespace Cerneala.Drawing.Prism.Filters;

internal static class PrismStampFilter
{
    public static Vector4[] Apply(
        PrismCatalogFilterPlan plan,
        Vector4[] source,
        int width,
        int height) =>
        PrismPhotocopyFilter.Apply(plan, source, width, height);
}
