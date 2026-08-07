using System.Numerics;

namespace Cerneala.Drawing.Prism.Filters;

internal static class PrismDespeckleFilter
{
    public static Vector4[] Apply(
        Vector4[] source,
        int width,
        int height,
        float threshold,
        float radius,
        int iterationCount) =>
        PrismNeighborhoodMath.ApplyProgressiveDespeckle(
            source,
            width,
            height,
            threshold,
            radius,
            iterationCount);

    public static Vector4 ApplyPixel(
        PrismNeighborhoodPlan plan,
        Vector4[] source,
        int width,
        int height,
        int x,
        int y,
        Vector4 center) =>
        PrismNeighborhoodMath.ReplaceOutlier(
            center,
            PrismNeighborhoodMath.Median3x3(
                source,
                width,
                height,
                x,
                y),
            plan.Options0.X);
}
