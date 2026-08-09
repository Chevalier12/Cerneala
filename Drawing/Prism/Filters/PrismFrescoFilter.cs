using System.Numerics;

namespace Cerneala.Drawing.Prism.Filters;

internal static class PrismFrescoFilter
{
    public static Vector4[] Apply(
        PrismCatalogFilterPlan plan,
        Vector4[] source,
        int width,
        int height) =>
        PrismCatalogPainterlyMath.Fresco(plan, source, width, height);
}
