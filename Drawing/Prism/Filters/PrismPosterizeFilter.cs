using System.Numerics;

namespace Cerneala.Drawing.Prism.Filters;

internal static class PrismPosterizeFilter
{
    public static Vector3 Apply(Vector3 color, float levels)
    {
        float steps =
            MathF.Max(1, MathF.Floor(levels + 0.5f) - 1);
        return new Vector3(
            MathF.Floor((color.X * steps) + 0.5f) / steps,
            MathF.Floor((color.Y * steps) + 0.5f) / steps,
            MathF.Floor((color.Z * steps) + 0.5f) / steps);
    }
}
