using System.Numerics;

namespace Cerneala.Drawing.Prism.Filters;

internal static class PrismBrightnessContrastFilter
{
    private const float ContrastPivot = 0.18f;
    private const float MinimumContrast = 0.001f;

    public static Vector3 Apply(
        Vector3 color,
        Vector4 parameters)
    {
        if (parameters.Z > 0.5f)
        {
            float legacyFactor = MathF.Max(0, 1 + parameters.Y);
            return ((color - new Vector3(0.5f)) * legacyFactor) +
                new Vector3(0.5f + parameters.X);
        }

        float exposure = MathF.Pow(2, parameters.X);
        float contrast = MathF.Max(
            MinimumContrast,
            MathF.Pow(2, parameters.Y * 2));
        if (contrast == 1)
        {
            return color * exposure;
        }

        Vector3 baseColor = Vector3.Max(
            Vector3.Zero,
            color * (exposure / ContrastPivot));
        return new Vector3(
            MathF.Pow(baseColor.X, contrast),
            MathF.Pow(baseColor.Y, contrast),
            MathF.Pow(baseColor.Z, contrast)) * ContrastPivot;
    }
}
