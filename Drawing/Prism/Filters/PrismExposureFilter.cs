using System.Numerics;

namespace Cerneala.Drawing.Prism.Filters;

internal static class PrismExposureFilter
{
    private const float ContrastPivot = 0.18f;
    private const float MinimumContrast = 0.001f;
    private const float VideoTransferPower =
        0.54644808743169393f;

    public static Vector3 Apply(
        Vector3 color,
        Vector4 parameters,
        Vector4 options)
    {
        float exposure = parameters.X;
        float contrast = MathF.Max(
            MinimumContrast,
            parameters.Y * parameters.Z);
        float pivot = MathF.Max(
            MinimumContrast,
            parameters.W);
        int style = (int)options.X;
        bool inverse = options.Y > 0.5f;

        if (style == 2)
        {
            float logPivot = MathF.Max(
                0,
                MathF.Log2(pivot / ContrastPivot) *
                    options.Z +
                    options.W);
            float exposureOffset = exposure * options.Z;
            if (inverse)
            {
                float inverseOffset =
                    logPivot -
                    (logPivot / contrast) -
                    exposureOffset;
                return (color / contrast) +
                    new Vector3(inverseOffset);
            }

            float forwardOffset =
                ((exposureOffset - logPivot) * contrast) +
                logPivot;
            return (color * contrast) +
                new Vector3(forwardOffset);
        }

        float transferPower =
            style == 1 ? VideoTransferPower : 1;
        float adjustedPivot = MathF.Pow(pivot, transferPower);
        float exposureScale = MathF.Pow(
            MathF.Pow(2, exposure),
            transferPower);
        if (inverse)
        {
            if (contrast == 1)
            {
                return color / exposureScale;
            }

            return PowPositive(
                color / adjustedPivot,
                1 / contrast) *
                (adjustedPivot / exposureScale);
        }

        if (contrast == 1)
        {
            return color * exposureScale;
        }

        return PowPositive(
            color * (exposureScale / adjustedPivot),
            contrast) * adjustedPivot;
    }

    private static Vector3 PowPositive(
        Vector3 value,
        float power)
    {
        value = Vector3.Max(Vector3.Zero, value);
        return new Vector3(
            MathF.Pow(value.X, power),
            MathF.Pow(value.Y, power),
            MathF.Pow(value.Z, power));
    }
}
