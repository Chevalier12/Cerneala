using System.Numerics;

namespace Cerneala.Drawing.Prism.Filters;

internal static class PrismGradientMapFilter
{
    public static Vector3 Apply(
        PrismAdjustmentPlan plan,
        Vector3 color,
        Vector2 pixelPosition,
        PrismGradientMapLut? lookup,
        Func<Vector3, Vector3>? analyticLookup)
    {
        float coordinate = PrismAdjustmentMath.Luminance(color);
        if (plan.Parameters0.Z > 0.5f)
        {
            ReadOnlySpan<byte> bayer4x4 =
            [
                 0,  8,  2, 10,
                12,  4, 14,  6,
                 3, 11,  1,  9,
                15,  7, 13,  5
            ];
            int x = (int)MathF.Floor(pixelPosition.X) & 3;
            int y = (int)MathF.Floor(pixelPosition.Y) & 3;
            coordinate = Math.Clamp(
                coordinate +
                ((bayer4x4[(y * 4) + x] - 7.5f) /
                    (16 * 255f)),
                0,
                1);
        }
        if (plan.Parameters0.Y > 0.5f)
        {
            coordinate = 1 - coordinate;
        }
        if (lookup is not null)
        {
            return lookup.Sample(coordinate);
        }
        return (analyticLookup ?? throw new InvalidOperationException(
            "GradientMap requires a one-dimensional LUT."))(
                new Vector3(coordinate));
    }
}
