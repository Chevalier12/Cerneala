using System.Numerics;

namespace Cerneala.Drawing.Prism.Filters;

internal static class PrismZigZagFilter
{
    public static Vector2 Map(
        Vector4 options0,
        Vector4 options1,
        Vector2 uv,
        int width,
        int height) =>
        PrismResamplingMath.MapZigZag(
            options0,
            options1,
            uv,
            width,
            height);
}
