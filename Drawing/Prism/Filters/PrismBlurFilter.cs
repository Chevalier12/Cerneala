using System.Numerics;

namespace Cerneala.Drawing.Prism.Filters;

internal static class PrismBlurFilter
{
    public static Vector4 Apply(
        Vector4[] source,
        int width,
        int height,
        int x,
        int y,
        PrismNeighborhoodPass pass,
        int edgeMode) =>
        PrismNeighborhoodMath.SampleOptimizedBilinearGaussian(
            source,
            width,
            height,
            x,
            y,
            pass,
            edgeMode);
}
