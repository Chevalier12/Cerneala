using System.Numerics;

namespace Cerneala.Drawing.Prism.Filters;

internal static class PrismShapeBlurFilter
{
    public static Vector4 Apply(
        PrismNeighborhoodPass pass,
        Vector4[] source,
        int width,
        int height,
        int x,
        int y,
        int edgeMode,
        Func<Vector2, Vector4> resource) =>
        PrismNeighborhoodMath.SampleShapePsf(
            pass,
            source,
            width,
            height,
            x,
            y,
            edgeMode,
            resource);
}
