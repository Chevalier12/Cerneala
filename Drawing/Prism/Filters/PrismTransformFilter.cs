using System.Numerics;

namespace Cerneala.Drawing.Prism.Filters;

internal static class PrismTransformFilter
{
    public static Vector2 Map(PrismResamplingPlan plan, Vector2 uv) =>
        PrismResamplingMath.MapTransform(plan, uv);

    public static Vector4 Sample(
        PrismResamplingPlan plan,
        PrismResamplingMath.MipLevel[] mipChain,
        Vector2 uv,
        Vector2 mapped,
        int width,
        int height,
        int edgeMode,
        Vector4 fill) =>
        PrismResamplingMath.SampleTransform(
            plan,
            mipChain,
            uv,
            mapped,
            width,
            height,
            edgeMode,
            fill);
}
