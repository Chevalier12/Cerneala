using System.Numerics;

namespace Cerneala.Drawing.Prism.Filters;

internal static class PrismBoxBlurFilter
{
    public static Vector4[] ApplyPass(
        Vector4[] source,
        int width,
        int height,
        int radiusX,
        int radiusY,
        int edgeMode) =>
        PrismNeighborhoodMath.ApplyBoxBlurSat(
            source,
            width,
            height,
            radiusX,
            radiusY,
            edgeMode);
}
