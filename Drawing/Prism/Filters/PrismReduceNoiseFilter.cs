using System.Numerics;

namespace Cerneala.Drawing.Prism.Filters;

internal static class PrismReduceNoiseFilter
{
    public static Vector4 ApplyPixel(Vector4 center) => center;

    public static Vector4[] ApplyDomainTransformPass(
        PrismNeighborhoodPlan plan,
        PrismNeighborhoodPass pass,
        Vector4[] source,
        int width,
        int height) =>
        PrismNeighborhoodMath.ApplyDomainTransformPass(
            plan,
            pass,
            source,
            width,
            height);

    public static Vector4[] ApplyJpegDeblockPass(
        PrismNeighborhoodPass pass,
        Vector4[] source,
        int width,
        int height) =>
        PrismNeighborhoodMath.ApplyJpegDeblockPass(
            pass,
            source,
            width,
            height);

    public static Vector4 Recombine(
        PrismNeighborhoodPlan plan,
        Vector4 original,
        Vector4 filtered) =>
        PrismNeighborhoodMath.RecombineReduceNoise(
            plan,
            original,
            filtered);
}
