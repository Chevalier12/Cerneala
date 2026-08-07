using System.Numerics;

namespace Cerneala.Drawing.Prism.Filters;

internal static class PrismAdaptiveWideAngleFilter
{
    public static Vector2 Map(
        PrismResamplingPlan plan,
        Vector2 uv) =>
        PrismResamplingMath.MapAdaptiveWideAngle(plan, uv);
}
