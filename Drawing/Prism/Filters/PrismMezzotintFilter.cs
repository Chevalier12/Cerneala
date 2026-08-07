using System.Numerics;

namespace Cerneala.Drawing.Prism.Filters;

internal static class PrismMezzotintFilter
{
    public static Vector4 ApplyPixel(
        PrismCatalogFilterPlan plan,
        Vector4 center,
        int x,
        int y)
    {
        float threshold = PrismMezzotintThreshold.Sample(
            x,
            y,
            PrismCatalogFilterMath.Seed(plan, "Seed"),
            plan.Options2);
        float value = PrismCatalogFilterMath.Luminance(center) >= threshold
            ? 1
            : 0;
        return PrismCatalogFilterMath.Associated(
            new Vector3(value),
            center.W);
    }
}
