using System.Numerics;

namespace Cerneala.Drawing.Prism.Filters;

internal static class PrismPolarCoordinatesFilter
{
    public static Vector2 Map(
        Vector4 options,
        Vector2 uv,
        int width,
        int height) =>
        PrismResamplingMath.MapPolar(options, uv, width, height);

    public static Vector4 Sample(
        PrismResamplingPlan plan,
        Vector4[] source,
        int width,
        int height,
        Vector2 uv,
        Vector2 mapped) =>
        PrismResamplingMath.SamplePolarEwa(
            plan,
            source,
            width,
            height,
            uv,
            mapped);
}
