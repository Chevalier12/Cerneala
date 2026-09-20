using System.Numerics;

namespace Cerneala.Drawing.Prism.Filters;

internal static class PrismLensBlurFilter
{
    public static Vector4 Apply(
        PrismNeighborhoodPlan plan,
        PrismNeighborhoodPass pass,
        Vector4[] source,
        int width,
        int height,
        int x,
        int y,
        Func<Vector2, Vector4>? resource)
    {
        Vector2 uv = new((x + 0.5f) / width, (y + 0.5f) / height);
        float depth = plan.Options1.W;
        if (resource is not null)
        {
            Vector4 map = resource(uv);
            depth = (int)plan.Options1.Z switch
            {
                1 => map.X,
                2 => map.Y,
                3 => map.Z,
                4 => map.W,
                _ => Vector3.Dot(
                    new Vector3(map.X, map.Y, map.Z),
                    PrismNeighborhoodMath.LuminanceWeights)
            };
        }
        if (plan.Options2.X > 0.5f)
        {
            depth = 1 - depth;
        }

        float focus = resource is null
            ? 1
            : Math.Clamp(MathF.Abs(depth - plan.Options1.W), 0, 1);
        float radius = pass.RadiusX * focus;
        if (radius <= 0.000001f)
        {
            return source[(y * width) + x];
        }

        int count = Math.Max(1, pass.SampleCount);
        int blades = Math.Max(3, (int)MathF.Round(plan.Options0.Y));
        float curvature = Math.Clamp(plan.Options0.Z, 0, 1);
        float rotation = plan.Options0.W;
        Vector4 total = Vector4.Zero;
        float totalWeight = 0;
        for (int index = 0; index < count; index++)
        {
            float fraction = count <= 1 ? 0 : (float)index / (count - 1);
            float angle = index * 2.39996323f;
            float sector = (2 * MathF.PI) / blades;
            float local = MathF.IEEERemainder(angle - rotation, sector);
            float polygonRadius = MathF.Cos(MathF.PI / blades) /
                MathF.Max(MathF.Cos(local), 0.000001f);
            float apertureRadius = polygonRadius +
                ((1 - polygonRadius) * curvature);
            float distance = MathF.Sqrt(fraction) * apertureRadius * radius;
            Vector4 sample = PrismNeighborhoodMath.SampleBilinear(
                source,
                width,
                height,
                x + (MathF.Cos(angle) * distance),
                y + (MathF.Sin(angle) * distance),
                edgeMode: 0);
            float luminance = PrismNeighborhoodMath.Luminance(sample);
            float boost = luminance >= plan.Options1.Y
                ? MathF.Max(0, plan.Options1.X)
                : 0;
            sample = new Vector4(
                new Vector3(sample.X, sample.Y, sample.Z) * (1 + boost),
                sample.W);
            total += sample;
            totalWeight++;
        }
        return total / MathF.Max(totalWeight, 0.000001f);
    }
}
