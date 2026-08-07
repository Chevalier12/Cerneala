using System.Numerics;

namespace Cerneala.Drawing.Prism.Filters;

internal static class PrismSumiEFilter
{
    public static Vector4[] Apply(
        PrismCatalogFilterPlan plan,
        Vector4[] source,
        int width,
        int height) =>
        PrismCatalogFilterMath.SumiE(plan, source, width, height);
}
