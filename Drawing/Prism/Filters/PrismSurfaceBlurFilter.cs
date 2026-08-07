using System.Numerics;

namespace Cerneala.Drawing.Prism.Filters;

internal static class PrismSurfaceBlurFilter
{
    public static Vector4 Apply(
        PrismNeighborhoodPlan plan,
        PrismNeighborhoodPass pass,
        Vector4[] source,
        int width,
        int height,
        int x,
        int y) =>
        PrismNeighborhoodMath.SampleSurfaceBilateral(
            plan,
            pass,
            source,
            width,
            height,
            x,
            y);
}
