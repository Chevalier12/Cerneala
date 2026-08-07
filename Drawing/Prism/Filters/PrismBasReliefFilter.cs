using System.Numerics;

namespace Cerneala.Drawing.Prism.Filters;

internal static class PrismBasReliefFilter
{
    public static Vector4[] Apply(
        PrismCatalogFilterPlan plan,
        Vector4[] source,
        int width,
        int height) =>
        PrismCatalogFilterMath.BasRelief(plan, source, width, height);
}
