using System.Numerics;

namespace Cerneala.Drawing.Prism.Filters;

internal static class PrismWaveFilter
{
    public static Vector2 Map(
        PrismResamplingPlan plan,
        Vector2 uv,
        int width,
        int height) =>
        PrismResamplingMath.MapWave(plan, uv, width, height);

    public static Vector4 Sample(
        PrismResamplingPlan plan,
        Vector4[] source,
        int width,
        int height,
        Vector2 uv,
        int edgeMode,
        Vector4 fill) =>
        PrismResamplingMath.SampleWaveFeline(
            plan,
            source,
            width,
            height,
            uv,
            edgeMode,
            fill);
}
