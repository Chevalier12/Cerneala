using System.Numerics;

namespace Cerneala.Drawing.Prism.Filters;

internal static class PrismFieldBlurFilter
{
    public static Vector4 Apply(
        PrismNeighborhoodPlan plan,
        PrismNeighborhoodPass pass,
        Vector4[] source,
        int width,
        int height,
        int x,
        int y,
        Vector4 center,
        Func<Vector2, Vector4> depthResource)
    {
        Vector2 uv = new(
            (x + 0.5f) / width,
            (y + 0.5f) / height);
        return PrismNeighborhoodMath.SampleFieldBlur(
            plan,
            pass,
            source,
            width,
            height,
            x,
            y,
            uv,
            center,
            depthResource);
    }
}
