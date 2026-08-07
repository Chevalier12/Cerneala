using System.Numerics;

namespace Cerneala.Drawing.Prism.Filters;

internal static class PrismSmartSharpenFilter
{
    public static Vector4 Sample(
        PrismNeighborhoodPlan plan,
        PrismNeighborhoodPass pass,
        Vector4[] source,
        int width,
        int height,
        int x,
        int y,
        bool correction) =>
        PrismNeighborhoodMath.SampleRichardsonLucyPsf(
            plan,
            pass,
            source,
            width,
            height,
            x,
            y,
            correction);

    public static Vector4 Ratio(
        Vector4 original,
        Vector4 blurred,
        float reduceNoise) =>
        PrismNeighborhoodMath.RichardsonLucyRatio(
            original,
            blurred,
            reduceNoise);

    public static Vector4 Update(
        Vector4 estimate,
        Vector4 correction) =>
        PrismNeighborhoodMath.RichardsonLucyUpdate(
            estimate,
            correction);

    public static Vector4 Recombine(
        PrismNeighborhoodPlan plan,
        Vector4[] original,
        int width,
        int height,
        int x,
        int y,
        Vector4 restored) =>
        PrismNeighborhoodMath.SmartSharpenRecombine(
            plan,
            original,
            width,
            height,
            x,
            y,
            restored);
}
