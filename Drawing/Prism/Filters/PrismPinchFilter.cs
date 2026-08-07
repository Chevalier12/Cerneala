using System.Numerics;

namespace Cerneala.Drawing.Prism.Filters;

internal static class PrismPinchFilter
{
    public static Vector2 Map(Vector4 options, Vector2 uv) =>
        PrismResamplingMath.MapPinch(options, uv);
}
