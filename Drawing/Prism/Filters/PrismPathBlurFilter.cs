using System.Numerics;

namespace Cerneala.Drawing.Prism.Filters;

internal static class PrismPathBlurFilter
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
        int count = Math.Max(1, pass.SampleCount);
        int intervalCount = count - 1;
        Vector2 origin = new(x, y);
        Vector4 center = source[(y * width) + x];
        Vector4 centerField = resource!(
            PathUv(origin, width, height));
        float centerWeight = Math.Clamp(centerField.W, 0, 1);
        if (intervalCount == 0 || centerWeight <= 0.000001f)
        {
            return center;
        }

        bool centered = plan.Options0.Z > 0.5f;
        int flashSync = centered ? 1 : (int)plan.Options1.Y;
        int backwardSteps = flashSync switch
        {
            0 => intervalCount,
            1 => intervalCount / 2,
            _ => 0
        };
        int forwardSteps = intervalCount - backwardSteps;
        Vector4 total = center * centerWeight;
        float totalWeight = centerWeight;
        AccumulatePathDirection(
            plan,
            source,
            width,
            height,
            x,
            y,
            resource,
            origin,
            centerField,
            backwardSteps,
            intervalCount,
            directionSign: -1,
            ref total,
            ref totalWeight);
        AccumulatePathDirection(
            plan,
            source,
            width,
            height,
            x,
            y,
            resource,
            origin,
            centerField,
            forwardSteps,
            intervalCount,
            directionSign: 1,
            ref total,
            ref totalWeight);
        return total / MathF.Max(totalWeight, 0.000001f);
    }

    private static void AccumulatePathDirection(
        PrismNeighborhoodPlan plan,
        Vector4[] source,
        int width,
        int height,
        int x,
        int y,
        Func<Vector2, Vector4> resource,
        Vector2 origin,
        Vector4 originField,
        int stepCount,
        int intervalCount,
        int directionSign,
        ref Vector4 total,
        ref float totalWeight)
    {
        Vector2 position = origin;
        for (int step = 1; step <= stepCount; step++)
        {
            if (!TryPathRk4Step(
                plan,
                resource,
                width,
                height,
                position,
                step == 1 ? originField : null,
                intervalCount,
                directionSign,
                out Vector2 next,
                out Vector2 tangent,
                out float validity,
                out float stepLength))
            {
                break;
            }

            position = next;
            float jitter = PathNoise(
                x,
                y,
                step,
                directionSign) *
                Math.Clamp(MathF.Abs(plan.Options1.Z), 0, 1) *
                stepLength *
                0.5f;
            Vector2 samplePosition = position + (tangent * jitter);
            float profileWeight = PathProfileWeight(
                plan,
                (float)step / Math.Max(stepCount, 1));
            float weight = validity * profileWeight;
            total += PrismNeighborhoodMath.SampleBilinear(
                source,
                width,
                height,
                samplePosition.X,
                samplePosition.Y,
                edgeMode: 0) * weight;
            totalWeight += weight;
        }
    }

    private static bool TryPathRk4Step(
        PrismNeighborhoodPlan plan,
        Func<Vector2, Vector4> resource,
        int width,
        int height,
        Vector2 position,
        Vector4? initialField,
        int intervalCount,
        int directionSign,
        out Vector2 next,
        out Vector2 tangent,
        out float validity,
        out float stepLength)
    {
        next = position;
        tangent = Vector2.Zero;
        validity = 0;
        stepLength = 0;
        bool firstValid = initialField.HasValue
            ? TryPathDerivative(
                plan,
                initialField.Value,
                intervalCount,
                directionSign,
                out Vector2 k1,
                out _,
                out _)
            : TryPathDerivative(
                plan,
                resource,
                width,
                height,
                position,
                intervalCount,
                directionSign,
                out k1,
                out _,
                out _);
        if (!firstValid)
        {
            return false;
        }
        if (!TryPathDerivative(
            plan,
            resource,
            width,
            height,
            position + (k1 * 0.5f),
            intervalCount,
            directionSign,
            out Vector2 k2,
            out _,
            out _))
        {
            return false;
        }
        if (!TryPathDerivative(
            plan,
            resource,
            width,
            height,
            position + (k2 * 0.5f),
            intervalCount,
            directionSign,
            out Vector2 k3,
            out _,
            out _))
        {
            return false;
        }
        if (!TryPathDerivative(
            plan,
            resource,
            width,
            height,
            position + k3,
            intervalCount,
            directionSign,
            out Vector2 k4,
            out tangent,
            out validity))
        {
            return false;
        }

        Vector2 displacement =
            (k1 + (k2 * 2) + (k3 * 2) + k4) / 6;
        next = position + displacement;
        stepLength = displacement.Length();
        return float.IsFinite(next.X) &&
            float.IsFinite(next.Y) &&
            float.IsFinite(stepLength);
    }

    private static bool TryPathDerivative(
        PrismNeighborhoodPlan plan,
        Func<Vector2, Vector4> resource,
        int width,
        int height,
        Vector2 position,
        int intervalCount,
        int directionSign,
        out Vector2 derivative,
        out Vector2 tangent,
        out float validity)
    {
        Vector4 field = resource(PathUv(position, width, height));
        return TryPathDerivative(
            plan,
            field,
            intervalCount,
            directionSign,
            out derivative,
            out tangent,
            out validity);
    }

    private static bool TryPathDerivative(
        PrismNeighborhoodPlan plan,
        Vector4 field,
        int intervalCount,
        int directionSign,
        out Vector2 derivative,
        out Vector2 tangent,
        out float validity)
    {
        validity = Math.Clamp(field.W, 0, 1);
        Vector2 direction = new(
            (field.X * 2) - 1,
            (field.Y * 2) - 1);
        float directionLengthSquared = direction.LengthSquared();
        if (validity <= 0.000001f ||
            directionLengthSquared <= 0.000001f ||
            !float.IsFinite(directionLengthSquared))
        {
            derivative = Vector2.Zero;
            tangent = Vector2.Zero;
            return false;
        }

        direction /= MathF.Sqrt(directionLengthSquared);
        float speed = plan.Options0.X +
            ((plan.Options0.W - plan.Options0.X) *
                Math.Clamp(field.Z, 0, 1));
        derivative =
            direction *
            (speed / Math.Max(intervalCount, 1)) *
            directionSign;
        float derivativeLength = derivative.Length();
        tangent = derivativeLength > 0.000001f
            ? derivative / derivativeLength
            : direction * directionSign;
        return float.IsFinite(derivative.X) &&
            float.IsFinite(derivative.Y);
    }

    private static float PathProfileWeight(
        PrismNeighborhoodPlan plan,
        float distanceFraction)
    {
        if ((int)plan.Options1.X == 0)
        {
            return 1;
        }

        float taper = Math.Clamp(plan.Options0.Y, 0, 1);
        return 1 - (taper * Math.Clamp(distanceFraction, 0, 1));
    }

    private static Vector2 PathUv(
        Vector2 pixelPosition,
        int width,
        int height) =>
        new(
            (pixelPosition.X + 0.5f) / width,
            (pixelPosition.Y + 0.5f) / height);

    private static float PathNoise(
        int x,
        int y,
        int step,
        int directionSign)
    {
        float hashX = x + (step * 19);
        float hashY = y + (directionSign < 0 ? 37 : 73);
        float value =
            (hashX * 127.1f) +
            (hashY * 311.7f);
        float unbounded = MathF.Sin(
            value + (2791 * 0.00006103515625f)) *
            43758.5453123f;
        float fraction = unbounded - MathF.Floor(unbounded);
        return (fraction * 2) - 1;
    }
}
