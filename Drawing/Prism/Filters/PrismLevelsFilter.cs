using System.Numerics;

namespace Cerneala.Drawing.Prism.Filters;

internal static class PrismLevelsFilter
{
    public static Vector3 Apply(
        Vector3 color,
        Vector4 parameters,
        Vector4 output) =>
        Apply(
            color,
            (int)parameters.X,
            parameters.Y,
            parameters.Z,
            parameters.W,
            output.X,
            output.Y);

    public static Vector3 Apply(
        Vector3 color,
        int channel,
        float inputBlack,
        float inputWhite,
        float gamma,
        float outputBlack,
        float outputWhite)
    {
        float denominator =
            MathF.Max(inputWhite - inputBlack, 0.000001f);
        return PrismAdjustmentMath.ApplyChannelMap(
            color,
            channel,
            value =>
            {
                float normalized = Math.Clamp(
                    (value - inputBlack) / denominator,
                    0,
                    1);
                return outputBlack +
                    (MathF.Pow(
                        normalized,
                        1 / MathF.Max(gamma, 0.000001f)) *
                    (outputWhite - outputBlack));
            });
    }
}
