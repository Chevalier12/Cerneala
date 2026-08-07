using System.Numerics;

namespace Cerneala.Drawing.Prism.Filters;

internal static class PrismSharpenFilter
{
    public static Vector4 Apply(
        Vector4[] source,
        int width,
        int height,
        int x,
        int y,
        float amount) =>
        PrismNeighborhoodMath.ContrastAdaptiveSharpen(
            source,
            width,
            height,
            x,
            y,
            amount);
}
