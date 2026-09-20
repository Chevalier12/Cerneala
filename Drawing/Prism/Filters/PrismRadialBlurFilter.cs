using System.Numerics;

namespace Cerneala.Drawing.Prism.Filters;

internal static class PrismRadialBlurFilter
{
    public static Vector4 Apply(
        PrismNeighborhoodPlan plan,
        PrismNeighborhoodPass pass,
        Vector4[] source,
        int width,
        int height,
        int x,
        int y)
    {
        Vector2 center = plan.Operation == PrismNeighborhoodOperation.SpinBlur
            ? new Vector2(plan.Options0.X, plan.Options0.Y)
            : new Vector2(plan.Options0.Z, plan.Options0.W);
        Vector2 uv = new(
            (x + 0.5f) / width,
            (y + 0.5f) / height);
        Vector2 delta = uv - center;
        float amount = plan.Operation == PrismNeighborhoodOperation.SpinBlur
            ? plan.Options1.X
            : plan.Options0.Y;
        bool zoom = plan.Operation == PrismNeighborhoodOperation.RadialBlur &&
            plan.Options0.X > 0.5f;
        int count = Math.Max(1, pass.SampleCount);
        Vector4 total = Vector4.Zero;
        for (int index = 0; index < count; index++)
        {
            float position = count <= 1
                ? 0
                : ((float)index / (count - 1)) - 0.5f;
            Vector2 sampleUv;
            if (zoom)
            {
                sampleUv = Vector2.Lerp(
                    uv,
                    center,
                    position * amount);
            }
            else
            {
                Vector2 pixelDelta = new(
                    delta.X * width,
                    delta.Y * height);
                float angle = position * amount;
                float cosine = MathF.Cos(angle);
                float sine = MathF.Sin(angle);
                sampleUv = center + new Vector2(
                    ((pixelDelta.X * cosine) -
                        (pixelDelta.Y * sine)) / width,
                    ((pixelDelta.X * sine) +
                        (pixelDelta.Y * cosine)) / height);
            }
            total += PrismNeighborhoodMath.SampleBilinear(
                source,
                width,
                height,
                (sampleUv.X * width) - 0.5f,
                (sampleUv.Y * height) - 0.5f,
                edgeMode: 0);
        }
        return total / count;
    }
}
