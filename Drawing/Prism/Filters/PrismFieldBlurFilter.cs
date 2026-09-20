using System.Numerics;

namespace Cerneala.Drawing.Prism.Filters;

internal static class PrismFieldBlurFilter
{
    public static Vector4 Apply(
        PrismNeighborhoodPlan plan,
        PrismNeighborhoodPass pass,
        Vector4[] source,
        int width,
        int height,
        int x,
        int y,
        Vector4 center,
        Func<Vector2, Vector4> depthResource)
    {
        Vector2 uv = new(
            (x + 0.5f) / width,
            (y + 0.5f) / height);
        return Sample(
            plan,
            pass,
            source,
            width,
            height,
            x,
            y,
            uv,
            center,
            depthResource);
    }

    private static Vector4 Sample(
        PrismNeighborhoodPlan plan,
        PrismNeighborhoodPass pass,
        Vector4[] source,
        int width,
        int height,
        int x,
        int y,
        Vector2 uv,
        Vector4 center,
        Func<Vector2, Vector4> depthResource)
    {
        float depth = Math.Clamp(depthResource(uv).X, 0, 1);
        if (plan.Options0.Y > 0.5f)
        {
            depth = 1 - depth;
        }

        float focalDistance = Math.Clamp(plan.Options0.X, 0, 1);
        float range = MathF.Max(
            MathF.Max(focalDistance, 1 - focalDistance),
            0.000001f);
        float coc = Math.Clamp(
            MathF.Abs(depth - focalDistance) / range,
            0,
            1);
        if (coc <= 0.000001f)
        {
            return center;
        }

        int count = Math.Max(1, pass.SampleCount);
        float radiusX = pass.RadiusX * coc;
        float radiusY = pass.RadiusY * coc;
        float highlight = MathF.Max(0, plan.Options0.Z);
        Vector4 total = default;
        float totalWeight = 0;
        for (int index = 0; index < count; index++)
        {
            float fraction = count == 1 ? 0 : (float)index / (count - 1);
            float angle = index * 2.39996323f;
            Vector4 sample = PrismNeighborhoodMath.Sample(
                source,
                width,
                height,
                x + (MathF.Cos(angle) * MathF.Sqrt(fraction) * radiusX),
                y + (MathF.Sin(angle) * MathF.Sqrt(fraction) * radiusY),
                PrismNeighborhoodMath.EdgeMode(plan));
            float straightLuminance = sample.W > 0.000001f
                ? Vector3.Dot(
                    new Vector3(sample.X, sample.Y, sample.Z) / sample.W,
                    PrismNeighborhoodMath.LuminanceWeights)
                : 0;
            float weight = 1 + (highlight * Math.Clamp(straightLuminance, 0, 1));
            total += sample * weight;
            totalWeight += weight;
        }

        return total / MathF.Max(totalWeight, 0.000001f);
    }
}
