using System.Numerics;

namespace Cerneala.Drawing.Prism.Filters;

internal static class PrismTwirlFilter
{
    public static Vector2 Map(Vector4 options, Vector2 uv) =>
        PrismResamplingMath.MapTwirl(options, uv);

    public static Vector4 Sample(
        PrismResamplingPlan plan,
        Vector4[] source,
        int width,
        int height,
        Vector2 uv,
        Vector2 mapped,
        int edgeMode,
        Vector4 fill) =>
        PrismResamplingMath.SampleTwirlFeline(
            plan,
            source,
            width,
            height,
            uv,
            mapped,
            edgeMode,
            fill);
}
