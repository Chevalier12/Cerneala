using System.Numerics;

namespace Cerneala.Drawing.Prism.Filters;

internal static class PrismSelectiveColorFilter
{
    public static Vector3 Apply(
        PrismAdjustmentPlan plan,
        Vector3 color)
    {
        float maximum = MathF.Max(
            color.X,
            MathF.Max(color.Y, color.Z));
        float minimum = MathF.Min(
            color.X,
            MathF.Min(color.Y, color.Z));
        Span<float> weights = stackalloc float[9]
        {
            MathF.Max(color.X - MathF.Max(color.Y, color.Z), 0),
            MathF.Max(MathF.Min(color.X, color.Y) - color.Z, 0),
            MathF.Max(color.Y - MathF.Max(color.X, color.Z), 0),
            MathF.Max(MathF.Min(color.Y, color.Z) - color.X, 0),
            MathF.Max(color.Z - MathF.Max(color.X, color.Y), 0),
            MathF.Max(MathF.Min(color.X, color.Z) - color.Y, 0),
            MathF.Max((minimum * 2) - 1, 0),
            MathF.Max(
                1 -
                (MathF.Abs(maximum - 0.5f) +
                    MathF.Abs(minimum - 0.5f)),
                0),
            MathF.Max(1 - (maximum * 2), 0)
        };
        bool relative = plan.Parameters9.X < 0.5f;
        Vector3 delta = Vector3.Zero;
        for (int index = 0; index < weights.Length; index++)
        {
            Vector4 adjustment = Parameter(plan, index);
            float weight = weights[index];
            delta.X += weight * ChannelDelta(
                color.X,
                adjustment.X,
                adjustment.W,
                relative);
            delta.Y += weight * ChannelDelta(
                color.Y,
                adjustment.Y,
                adjustment.W,
                relative);
            delta.Z += weight * ChannelDelta(
                color.Z,
                adjustment.Z,
                adjustment.W,
                relative);
        }
        return color + delta;
    }

    private static float ChannelDelta(
        float value,
        float adjustment,
        float blackAdjustment,
        bool relative)
    {
        float delta =
            ((-1 - adjustment) * blackAdjustment) - adjustment;
        if (relative)
        {
            delta *= 1 - value;
        }
        return Math.Clamp(delta, -value, 1 - value);
    }

    private static Vector4 Parameter(
        PrismAdjustmentPlan plan,
        int index) =>
        index switch
        {
            0 => plan.Parameters0,
            1 => plan.Parameters1,
            2 => plan.Parameters2,
            3 => plan.Parameters3,
            4 => plan.Parameters4,
            5 => plan.Parameters5,
            6 => plan.Parameters6,
            7 => plan.Parameters7,
            8 => plan.Parameters8,
            _ => Vector4.Zero
        };
}
