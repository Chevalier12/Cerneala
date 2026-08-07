using System.Numerics;

namespace Cerneala.Drawing.Prism.Filters;

internal static class PrismTiltShiftFilter
{
    public static Vector4 Apply(
        PrismNeighborhoodPlan plan,
        PrismNeighborhoodPass pass,
        Vector4[] source,
        int width,
        int height,
        int x,
        int y,
        Vector4 center)
    {
        Vector2 uv = new(
            (x + 0.5f) / width,
            (y + 0.5f) / height);
        Vector2 direction = new(
            -MathF.Sin(plan.Options0.Z),
            MathF.Cos(plan.Options0.Z));
        float distance = MathF.Abs(Vector2.Dot(
            uv - new Vector2(plan.Options0.X, plan.Options0.Y),
            direction));
        float transition = Math.Clamp(
            (distance - plan.Options0.W) /
                MathF.Max(plan.Options1.X, 0.000001f),
            0,
            1);
        float amount =
            transition * transition * (3 - (2 * transition));
        PrismNeighborhoodPass adjusted = pass with
        {
            RadiusX = pass.RadiusX * amount,
            RadiusY = pass.RadiusY * amount
        };
        Vector4 blurred = PrismNeighborhoodMath.SampleDisk(
            source,
            width,
            height,
            x,
            y,
            adjusted,
            PrismNeighborhoodMath.EdgeMode(plan));
        return Vector4.Lerp(center, blurred, amount);
    }
}
