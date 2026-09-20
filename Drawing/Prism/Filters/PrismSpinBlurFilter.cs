using System.Numerics;

namespace Cerneala.Drawing.Prism.Filters;

internal static class PrismSpinBlurFilter
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
        Vector4 centerSample = source[(y * width) + x];
        Vector2 center = new(plan.Options0.X, plan.Options0.Y);
        Vector2 uv = new(
            (x + 0.5f) / width,
            (y + 0.5f) / height);
        Vector2 delta = uv - center;
        Vector2 radius = new(plan.Options0.Z, plan.Options0.W);
        Vector2 normalized = new(
            delta.X / MathF.Max(radius.X, 0.000001f),
            delta.Y / MathF.Max(radius.Y, 0.000001f));
        float distance = normalized.Length();
        float feather = Math.Clamp(plan.Options1.Y, 0, 1);
        float mask;
        if (feather <= 0.000001f)
        {
            mask = distance <= 1 ? 1 : 0;
        }
        else
        {
            float transition = Math.Clamp(
                (1 - distance) / feather,
                0,
                1);
            mask =
                transition *
                transition *
                (3 - (2 * transition));
        }
        if (mask <= 0.000001f)
        {
            return centerSample;
        }

        Vector2 pixelDelta = new(
            delta.X * width,
            delta.Y * height);
        float rotation = plan.Options1.X;
        int count = PrismNeighborhoodPlanner.SpinSampleCount(
            MathF.Abs(rotation) * pixelDelta.Length(),
            pass.SampleCount);
        if (count <= 1)
        {
            return centerSample;
        }

        int intervals = count - 1;
        float angleStep = rotation / intervals;
        float noise = Math.Clamp(plan.Options2.Y, 0, 1);
        float strobeStrength = Math.Clamp(plan.Options1.Z, 0, 1);
        int strobeFlashes = Math.Max(
            0,
            (int)MathF.Round(plan.Options1.W));
        float strobeDuration = Math.Clamp(plan.Options2.X, 0, 1);
        Vector2 rotatedDelta = Rotate(
            pixelDelta,
            rotation * -0.5f);
        float stepCosine = MathF.Cos(angleStep);
        float stepSine = MathF.Sin(angleStep);
        Vector4 total = Vector4.Zero;
        float totalWeight = 0;
        for (int index = 0; index < count; index++)
        {
            Vector2 sampleDelta = rotatedDelta;
            if (noise > 0 &&
                index > 0 &&
                index < intervals &&
                index != intervals / 2)
            {
                float jitter =
                    Noise(x, y, 0x51f15e5du, (uint)index) *
                    noise *
                    angleStep *
                    0.5f;
                sampleDelta = Rotate(sampleDelta, jitter);
            }

            float position = (float)index / intervals;
            float weight = SpinStrobeWeight(
                position,
                strobeStrength,
                strobeFlashes,
                strobeDuration);
            Vector2 sampleUv = center + new Vector2(
                sampleDelta.X / width,
                sampleDelta.Y / height);
            total += PrismNeighborhoodMath.SampleBilinear(
                source,
                width,
                height,
                (sampleUv.X * width) - 0.5f,
                (sampleUv.Y * height) - 0.5f,
                edgeMode: 0) * weight;
            totalWeight += weight;
            rotatedDelta = new Vector2(
                (rotatedDelta.X * stepCosine) -
                    (rotatedDelta.Y * stepSine),
                (rotatedDelta.X * stepSine) +
                    (rotatedDelta.Y * stepCosine));
        }

        Vector4 blurred = totalWeight > 0.000001f
            ? total / totalWeight
            : centerSample;
        return Vector4.Lerp(centerSample, blurred, mask);
    }

    private static Vector2 Rotate(
        Vector2 value,
        float angle)
    {
        float cosine = MathF.Cos(angle);
        float sine = MathF.Sin(angle);
        return new Vector2(
            (value.X * cosine) - (value.Y * sine),
            (value.X * sine) + (value.Y * cosine));
    }

    private static float SpinStrobeWeight(
        float position,
        float strength,
        int flashes,
        float duration)
    {
        if (strength <= 0 || flashes <= 0)
        {
            return 1;
        }

        float phase = (position * flashes) + 0.5f;
        phase -= MathF.Floor(phase);
        float pulse = MathF.Abs(phase - 0.5f) <= duration * 0.5f
            ? 1
            : 0;
        return 1 + ((pulse - 1) * strength);
    }

    private static float Noise(
        int x,
        int y,
        uint seed,
        uint channel)
    {
        uint value =
            unchecked((uint)x * 0x9e3779b9u) ^
            unchecked((uint)y * 0x85ebca6bu) ^
            seed ^
            (channel * 0xc2b2ae35u);
        value ^= value >> 16;
        value *= 0x7feb352du;
        value ^= value >> 15;
        value *= 0x846ca68bu;
        value ^= value >> 16;
        return ((value & 0x00ffffffu) / 8388607.5f) - 1;
    }
}
