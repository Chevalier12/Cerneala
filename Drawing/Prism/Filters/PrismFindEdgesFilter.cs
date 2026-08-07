using System.Numerics;

namespace Cerneala.Drawing.Prism.Filters;

internal static class PrismFindEdgesFilter
{
    public static Vector4 ApplyPixel(
        PrismCatalogFilterPlan plan,
        Vector4[] source,
        int width,
        int height,
        int x,
        int y,
        Vector4 center)
    {
        float gradient = PrismCatalogFilterMath.Scharr(
            source,
            width,
            height,
            x,
            y);
        float threshold = PrismCatalogFilterMath.Option(
            plan,
            "Threshold",
            0.1f);
        float value = Math.Clamp(
            (gradient - threshold) /
                MathF.Max(1 - threshold, 0.0001f),
            0,
            1);
        return PrismCatalogFilterMath.Associated(
            new Vector3(1 - value),
            center.W);
    }
}
