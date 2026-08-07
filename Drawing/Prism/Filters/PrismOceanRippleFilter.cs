using System.Numerics;

namespace Cerneala.Drawing.Prism.Filters;

internal static class PrismOceanRippleFilter
{
    public static Vector2 Map(
        PrismResamplingPlan plan,
        Vector2 uv,
        int x,
        int y,
        int width,
        int height) =>
        PrismResamplingMath.MapOceanRipple(
            plan,
            uv,
            x,
            y,
            width,
            height);
}
