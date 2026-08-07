using System.Numerics;

namespace Cerneala.Drawing.Prism.Filters;

internal static class PrismPathBlurFilter
{
    public static Vector4 Apply(
        PrismNeighborhoodPlan plan,
        PrismNeighborhoodPass pass,
        Vector4[] source,
        int width,
        int height,
        int x,
        int y,
        Func<Vector2, Vector4>? resource) =>
        PrismNeighborhoodMath.SamplePath(
            plan,
            pass,
            source,
            width,
            height,
            x,
            y,
            resource);
}
