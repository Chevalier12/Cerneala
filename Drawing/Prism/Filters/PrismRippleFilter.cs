using System.Numerics;

namespace Cerneala.Drawing.Prism.Filters;

internal static class PrismRippleFilter
{
    public static Vector2 Map(
        PrismResamplingPlan plan,
        Vector2 uv,
        int width,
        int height) =>
        PrismResamplingMath.MapRipple(plan, uv, width, height);
}
