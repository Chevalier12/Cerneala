using System.Numerics;

namespace Cerneala.Drawing.Prism.Filters;

internal static class PrismLensCorrectionFilter
{
    public static Vector4 Apply(
        PrismResamplingPlan plan,
        Vector4[] source,
        int width,
        int height,
        Vector2 uv) =>
        PrismResamplingMath.ApplyLensCorrection(
            plan,
            source,
            width,
            height,
            uv);
}
