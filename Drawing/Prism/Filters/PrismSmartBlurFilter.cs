using System.Numerics;

namespace Cerneala.Drawing.Prism.Filters;

internal static class PrismSmartBlurFilter
{
    public static Vector4 Apply(
        PrismNeighborhoodPlan plan,
        PrismNeighborhoodPass pass,
        Vector4[] source,
        int width,
        int height,
        int x,
        int y) =>
        PrismNeighborhoodMath.SampleSmartBlur(
            plan,
            pass,
            source,
            width,
            height,
            x,
            y);
}
