using System.Numerics;
using Cerneala.Drawing.Prism.Catalog;
using Cerneala.Drawing.Prism.ColorManagement;

namespace Cerneala.Drawing.Prism.Filters;

internal static partial class PrismResamplingMath
{
    internal static int TwirlTapCount(
        Vector4 options,
        Vector2 uv,
        int width,
        int height)
    {
        TwirlJacobian(
            options,
            uv,
            width,
            height,
            out Vector2 derivativeX,
            out Vector2 derivativeY);
        return FelineTapCount(
            MajorFootprint(
                derivativeX,
                derivativeY).Length);
    }

    internal static Vector4 SampleTwirlFeline(
        PrismResamplingPlan plan,
        Vector4[] source,
        int width,
        int height,
        Vector2 uv,
        Vector2 mapped,
        int edgeMode,
        Vector4 fill)
    {
        TwirlJacobian(
            plan.Options0,
            uv,
            width,
            height,
            out Vector2 derivativeX,
            out Vector2 derivativeY);
        return SampleFeline(
            source,
            width,
            height,
            mapped,
            derivativeX,
            derivativeY,
            edgeMode,
            fill);
    }

    private static Vector4 SampleFeline(
        Vector4[] source,
        int width,
        int height,
        Vector2 mapped,
        Vector2 derivativeX,
        Vector2 derivativeY,
        int edgeMode,
        Vector4 fill)
    {
        MajorAxis footprint =
            MajorFootprint(
                derivativeX,
                derivativeY);
        int tapCount = FelineTapCount(footprint.Length);
        if (tapCount == 1)
        {
            return Sample(
                source,
                width,
                height,
                mapped,
                edgeMode,
                fill);
        }

        float boundedLength = MathF.Min(
            footprint.Length,
            8);
        Vector2 axis =
            footprint.Direction *
            boundedLength /
            new Vector2(width, height);
        Vector4 total = Vector4.Zero;
        float totalWeight = 0;
        for (int tap = 0; tap < tapCount; tap++)
        {
            float position =
                ((tap + 0.5f) / tapCount) -
                0.5f;
            float weight = MathF.Exp(
                -2 * position * position);
            total += Sample(
                source,
                width,
                height,
                mapped + (axis * position),
                edgeMode,
                fill) * weight;
            totalWeight += weight;
        }
        return total / totalWeight;
    }

    private static void TwirlJacobian(
        Vector4 options,
        Vector2 uv,
        int width,
        int height,
        out Vector2 derivativeX,
        out Vector2 derivativeY)
    {
        Vector2 center = new(options.Y, options.Z);
        Vector2 delta = uv - center;
        float deltaLength = delta.Length();
        float radius = deltaLength / 0.70710678f;
        float twist =
            -options.X *
            Math.Clamp(1 - radius, 0, 1);
        Vector2 sourceSize = new(width, height);
        if (radius >= 1)
        {
            derivativeX = Vector2.UnitX;
            derivativeY = Vector2.UnitY;
            return;
        }

        Vector2 rotatedDelta = Rotate(delta, twist);
        Vector2 tangent = new(
            -rotatedDelta.Y,
            rotatedDelta.X);
        Vector2 radialDirection = deltaLength > 0.000001f
            ? delta / deltaLength
            : Vector2.Zero;
        Vector2 angleGradient =
            radialDirection *
            (options.X / 0.70710678f);
        Vector2 stepX = new(1f / width, 0);
        Vector2 stepY = new(0, 1f / height);
        derivativeX =
            (Rotate(stepX, twist) +
                (tangent *
                    Vector2.Dot(angleGradient, stepX))) *
            sourceSize;
        derivativeY =
            (Rotate(stepY, twist) +
                (tangent *
                    Vector2.Dot(angleGradient, stepY))) *
            sourceSize;
    }

    private static MajorAxis MajorFootprint(
        Vector2 derivativeX,
        Vector2 derivativeY)
    {
        double covarianceX =
            ((double)derivativeX.X * derivativeX.X) +
            ((double)derivativeY.X * derivativeY.X);
        double covarianceY =
            ((double)derivativeX.Y * derivativeX.Y) +
            ((double)derivativeY.Y * derivativeY.Y);
        double covarianceCross =
            ((double)derivativeX.X * derivativeX.Y) +
            ((double)derivativeY.X * derivativeY.Y);
        double difference = covarianceX - covarianceY;
        double discriminant = Math.Sqrt(Math.Max(
            (difference * difference) +
                (4 * covarianceCross * covarianceCross),
            0));
        double majorEigenvalue = Math.Max(
            (covarianceX + covarianceY + discriminant) * 0.5,
            0);
        double majorLength = Math.Sqrt(majorEigenvalue);
        Vector2 direction;
        if (Math.Abs(covarianceCross) > 0.000001)
        {
            double directionX = covarianceCross;
            double directionY =
                majorEigenvalue - covarianceX;
            double directionLength = Math.Sqrt(
                (directionX * directionX) +
                (directionY * directionY));
            direction = directionLength > 0
                ? new Vector2(
                    (float)(directionX / directionLength),
                    (float)(directionY / directionLength))
                : Vector2.UnitX;
        }
        else
        {
            direction = covarianceX >= covarianceY
                ? Vector2.UnitX
                : Vector2.UnitY;
        }
        return new MajorAxis(
            direction,
            (float)Math.Min(majorLength, float.MaxValue));
    }

    private static int FelineTapCount(float majorLength) =>
        majorLength <= 1
            ? 1
            : majorLength <= 4
                ? 4
                : 8;

    private readonly record struct MajorAxis(
        Vector2 Direction,
        float Length);

}
