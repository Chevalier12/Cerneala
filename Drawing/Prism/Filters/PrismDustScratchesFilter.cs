using System.Numerics;

namespace Cerneala.Drawing.Prism.Filters;

internal static class PrismDustScratchesFilter
{
    public static Vector4 Apply(
        Vector4[] source,
        int width,
        int height,
        int x,
        int y,
        int maximumRadius,
        float threshold) =>
        PrismNeighborhoodMath.AdaptiveThresholdedMedian(
            source,
            width,
            height,
            x,
            y,
            maximumRadius,
            threshold);
}
