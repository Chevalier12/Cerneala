using System.Numerics;

namespace Cerneala.Drawing.Prism.Filters;

internal static class PrismIrisBlurFilter
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
        Vector2 delta = uv - new Vector2(
            plan.Options0.X,
            plan.Options0.Y);
        float angle = -plan.Options1.Y;
        float cosine = MathF.Cos(angle);
        float sine = MathF.Sin(angle);
        Vector2 rotated = new(
            ((delta.X * cosine) - (delta.Y * sine)) /
                MathF.Max(plan.Options0.Z, 0.000001f),
            ((delta.X * sine) + (delta.Y * cosine)) /
                MathF.Max(plan.Options0.W, 0.000001f));
        float transition = Math.Clamp(
            (rotated.Length() - 1) /
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
