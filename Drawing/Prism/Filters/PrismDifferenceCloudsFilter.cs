using System.Numerics;

namespace Cerneala.Drawing.Prism.Filters;

internal static class PrismDifferenceCloudsFilter
{
    public static Vector4 ApplyPixel(
        PrismCatalogFilterPlan plan,
        Vector4 center,
        int x,
        int y) =>
        PrismCloudsFilter.ApplyPixel(
            plan,
            center,
            x,
            y,
            difference: true);
}
