using System.Numerics;

namespace Cerneala.Drawing.Prism.Filters;

internal static class PrismAverageFilter
{
    public static Vector4 Apply(
        Vector4[] source,
        int width,
        int height,
        int x,
        int y) =>
        PrismNeighborhoodMath.Neighborhood3x3(
            source,
            width,
            height,
            x,
            y);
}
