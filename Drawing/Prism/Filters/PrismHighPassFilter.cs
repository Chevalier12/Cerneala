using System.Numerics;

namespace Cerneala.Drawing.Prism.Filters;

internal static class PrismHighPassFilter
{
    public static Vector4 Sample(
        Vector4[] source,
        int width,
        int height,
        int x,
        int y,
        PrismNeighborhoodPass pass,
        int edgeMode,
        float sigma) =>
        PrismNeighborhoodMath.SampleIncrementalGaussian(
            source,
            width,
            height,
            x,
            y,
            pass,
            edgeMode,
            sigma);

    public static Vector4 Recombine(
        Vector4 center,
        Vector4 blurred) =>
        PrismNeighborhoodMath.HighPass(center, blurred);
}
