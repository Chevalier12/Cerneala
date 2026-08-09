using System.Numerics;

namespace Cerneala.Drawing.Prism.Filters;

internal static class PrismChalkCharcoalFilter
{
    public static Vector4[] Apply(
        PrismCatalogFilterPlan plan,
        Vector4[] source,
        int width,
        int height) =>
        PrismCatalogInkMath.ChalkCharcoal(
            plan,
            source,
            width,
            height);
}
