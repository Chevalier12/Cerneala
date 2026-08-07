using System.Numerics;

namespace Cerneala.Drawing.Prism.Filters;

internal static class PrismScanlinesFilter
{
    public static Vector4 ApplyPixel(
        PrismCatalogFilterPlan plan,
        Vector4 center,
        int height,
        int y)
    {
        float frequency = MathF.Max(
            1,
            PrismCatalogFilterMath.Option(
                plan,
                "Frequency",
                320));
        float phase = PrismCatalogFilterMath.Option(
            plan,
            "Phase",
            0);
        float thickness = Math.Clamp(
            PrismCatalogFilterMath.Option(
                plan,
                "Thickness",
                0.5f),
            0,
            1);
        float lineOpacity = Math.Clamp(
            PrismCatalogFilterMath.Option(
                plan,
                "LineOpacity",
                0.18f),
            0,
            1);
        float softness = Math.Clamp(
            PrismCatalogFilterMath.Option(
                plan,
                "Softness",
                0),
            0,
            1);
        float coverage = lineOpacity * PixelCoverage(
            frequency,
            phase,
            thickness,
            softness,
            MathF.Max(height, 1),
            y);
        Vector4 color = PrismCatalogFilterMath.OptionVector(
            plan,
            "Color",
            new Vector4(0, 0, 0, 1));
        return PrismCatalogFilterMath.Associated(
            Vector3.Lerp(
                PrismCatalogFilterMath.Unpremultiply(center),
                new Vector3(color.X, color.Y, color.Z),
                Math.Clamp(coverage * color.W, 0, 1)),
            center.W);
    }

    private static float PixelCoverage(
        float frequency,
        float phase,
        float thickness,
        float softness,
        float height,
        int y)
    {
        if (thickness <= 0)
        {
            return 0;
        }
        if (thickness >= 1)
        {
            return 1;
        }

        float footprint = frequency / height;
        float position = ((y + 0.5f) * footprint) + phase;
        return GeneralizedGaussian(
            position,
            footprint,
            thickness,
            softness);
    }


    private static float GeneralizedGaussian(
        float position,
        float footprint,
        float thickness,
        float softness)
    {
        float line = position - MathF.Floor(position);
        float distance = MathF.Abs(line - 0.5f);
        float halfWidth = thickness * 0.5f;
        float effectiveHalfWidth = MathF.Sqrt(
            (halfWidth * halfWidth) +
            (MathF.Min(footprint, 1) * MathF.Min(footprint, 1) / 12));
        float normalizedDistance = distance / effectiveHalfWidth;
        float shape = float.Lerp(12, 2, softness);
        float hardScan = float.Lerp(-12, -0.5f, softness);
        return MathF.Pow(
            2,
            hardScan * MathF.Pow(normalizedDistance, shape));
    }
}
