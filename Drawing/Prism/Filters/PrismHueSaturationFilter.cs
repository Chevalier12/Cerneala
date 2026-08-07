using System.Numerics;

namespace Cerneala.Drawing.Prism.Filters;

internal static class PrismHueSaturationFilter
{
    public static Vector3 Apply(
        Vector3 color,
        Vector4 parameters,
        bool colorize)
    {
        Vector3 hsl = PrismOkhsl.FromLinearSrgb(color);
        float weight = HueWeight(hsl.X, (int)parameters.X);
        if (colorize)
        {
            float targetHue = PrismAdjustmentMath.Repeat(
                parameters.Y / 360f);
            hsl.X = PrismAdjustmentMath.Repeat(
                hsl.X +
                (ShortestHueDelta(hsl.X, targetHue) * weight));
            hsl.Y = Math.Clamp(
                hsl.Y +
                ((0.5f + (parameters.Z * 0.5f) - hsl.Y) *
                    weight),
                0,
                1);
            hsl.Z = Math.Clamp(
                hsl.Z + (parameters.W * weight),
                0,
                1);
        }
        else
        {
            hsl.X = PrismAdjustmentMath.Repeat(
                hsl.X + ((parameters.Y / 360f) * weight));
            hsl.Y = Math.Clamp(
                hsl.Y * (1 + (parameters.Z * weight)),
                0,
                1);
            hsl.Z = Math.Clamp(
                hsl.Z + (parameters.W * weight),
                0,
                1);
        }
        return PrismOkhsl.ToLinearSrgb(hsl);
    }

    private static float HueWeight(float hue, int channel)
    {
        if (channel == 0)
        {
            return 1;
        }
        float center = (channel - 1) / 6f;
        float distance = MathF.Abs(hue - center);
        distance = MathF.Min(distance, 1 - distance);
        return Math.Clamp(1 - (distance * 6), 0, 1);
    }

    private static float ShortestHueDelta(float from, float to)
    {
        float delta = PrismAdjustmentMath.Repeat(
            to - from + 0.5f) - 0.5f;
        return delta == -0.5f ? 0.5f : delta;
    }
}
