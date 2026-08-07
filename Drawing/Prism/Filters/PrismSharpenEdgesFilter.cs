using System.Numerics;

namespace Cerneala.Drawing.Prism.Filters;

internal static class PrismSharpenEdgesFilter
{
    public static Vector4 Apply(
        Vector4[] source,
        int width,
        int height,
        int x,
        int y,
        float amount,
        float threshold) =>
        PrismNeighborhoodMath.SobelGatedContrastAdaptiveSharpen(
            source,
            width,
            height,
            x,
            y,
            amount,
            threshold);
}
