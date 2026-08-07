using System.Numerics;

namespace Cerneala.Drawing.Prism.Filters;

internal static class PrismGlassFilter
{
    public static Vector2 Map(
        PrismResamplingPlan plan,
        Vector2 uv,
        int x,
        int y,
        int width,
        int height,
        Func<Vector2, Vector4>? resource) =>
        PrismResamplingMath.MapGlass(
            plan,
            uv,
            x,
            y,
            width,
            height,
            resource);
}
