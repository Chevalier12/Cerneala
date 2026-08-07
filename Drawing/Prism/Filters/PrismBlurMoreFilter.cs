using System.Numerics;

namespace Cerneala.Drawing.Prism.Filters;

internal static class PrismBlurMoreFilter
{
    public static Vector4 Apply(
        Vector4[] source,
        int width,
        int height,
        int x,
        int y,
        PrismNeighborhoodPass pass,
        int edgeMode) =>
        PrismBlurFilter.Apply(
            source,
            width,
            height,
            x,
            y,
            pass,
            edgeMode);
}
