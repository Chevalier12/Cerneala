using System.Numerics;

namespace Cerneala.Drawing.Prism.Filters;

internal static class PrismGaussianBlurFilter
{
    public static Vector4 Apply(
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
}
