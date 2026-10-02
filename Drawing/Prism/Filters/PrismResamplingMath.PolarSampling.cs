using System.Numerics;
using Cerneala.Drawing.Prism.Catalog;
using Cerneala.Drawing.Prism.ColorManagement;

namespace Cerneala.Drawing.Prism.Filters;

internal static partial class PrismResamplingMath
{
    internal static Vector4 SamplePolarEwa(
        PrismResamplingPlan plan,
        Vector4[] source,
        int width,
        int height,
        Vector2 uv,
        Vector2 mapped)
    {
        Vector2 sourceSize = new(width, height);
        PolarJacobian(
            plan.Options0,
            uv,
            width,
            height,
            out Vector2 derivativeX,
            out Vector2 derivativeY);
        float covarianceX =
            (derivativeX.X * derivativeX.X) +
            (derivativeY.X * derivativeY.X);
        float covarianceY =
            (derivativeX.Y * derivativeX.Y) +
            (derivativeY.Y * derivativeY.Y);
        float covarianceCross =
            (derivativeX.X * derivativeX.Y) +
            (derivativeY.X * derivativeY.Y);
        float trace = covarianceX + covarianceY;
        float discriminant = MathF.Sqrt(MathF.Max(
            ((covarianceX - covarianceY) *
                (covarianceX - covarianceY)) +
            (4 * covarianceCross * covarianceCross),
            0));
        float majorEigenvalue =
            MathF.Max((trace + discriminant) * 0.5f, 0);
        float minorEigenvalue =
            MathF.Max((trace - discriminant) * 0.5f, 0);
        float majorLength = MathF.Sqrt(majorEigenvalue);
        float minorLength = MathF.Max(
            MathF.Sqrt(minorEigenvalue),
            1);
        if (majorLength <= 1)
        {
            return SamplePolarSource(
                source,
                width,
                height,
                mapped,
                plan.Options0.X >= 0.5f);
        }

        minorLength = MathF.Max(
            minorLength,
            majorLength / 8);
        Vector2 majorDirection;
        if (MathF.Abs(covarianceCross) > 0.000001f)
        {
            majorDirection = Vector2.Normalize(new Vector2(
                covarianceCross,
                majorEigenvalue - covarianceX));
        }
        else
        {
            majorDirection = covarianceX >= covarianceY
                ? Vector2.UnitX
                : Vector2.UnitY;
        }
        Vector2 minorDirection = new(
            -majorDirection.Y,
            majorDirection.X);
        Vector2 majorAxis =
            majorDirection * majorLength / sourceSize;
        Vector2 minorAxis =
            minorDirection * minorLength / sourceSize;
        const float innerRadius = 0.2f;
        const float outerComponent = 0.31819805f;
        const float innerWeight = 0.92311635f;
        const float outerWeight = 0.66697681f;
        const float totalWeight =
            4 * (innerWeight + outerWeight);
        bool wrapAngle = plan.Options0.X >= 0.5f;
        Vector4 total =
            SamplePolarSource(
                source,
                width,
                height,
                mapped + (majorAxis * innerRadius),
                wrapAngle) *
            innerWeight;
        total += SamplePolarSource(
                source,
                width,
                height,
                mapped - (majorAxis * innerRadius),
                wrapAngle) *
            innerWeight;
        total += SamplePolarSource(
                source,
                width,
                height,
                mapped + (minorAxis * innerRadius),
                wrapAngle) *
            innerWeight;
        total += SamplePolarSource(
                source,
                width,
                height,
                mapped - (minorAxis * innerRadius),
                wrapAngle) *
            innerWeight;
        total += SamplePolarSource(
                source,
                width,
                height,
                mapped +
                    ((majorAxis + minorAxis) * outerComponent),
                wrapAngle) *
            outerWeight;
        total += SamplePolarSource(
                source,
                width,
                height,
                mapped +
                    ((majorAxis - minorAxis) * outerComponent),
                wrapAngle) *
            outerWeight;
        total += SamplePolarSource(
                source,
                width,
                height,
                mapped +
                    ((-majorAxis + minorAxis) * outerComponent),
                wrapAngle) *
            outerWeight;
        total += SamplePolarSource(
                source,
                width,
                height,
                mapped -
                    ((majorAxis + minorAxis) * outerComponent),
                wrapAngle) *
            outerWeight;
        return total / totalWeight;
    }

    private static void PolarJacobian(
        Vector4 options,
        Vector2 uv,
        int width,
        int height,
        out Vector2 derivativeX,
        out Vector2 derivativeY)
    {
        Vector2 center = new(options.Y, options.Z);
        Vector2 sourceSize = new(width, height);
        Vector2 centerPixels = center * sourceSize;
        Vector2 cornerDistance = new(
            MathF.Max(
                centerPixels.X,
                width - centerPixels.X),
            MathF.Max(
                centerPixels.Y,
                height - centerPixels.Y));
        float maximumRadius = MathF.Max(
            cornerDistance.Length(),
            0.000001f);
        if (options.X < 0.5f)
        {
            float angle =
                (uv.X - center.X) * MathF.Tau;
            float polarRadius =
                (uv.Y - center.Y + 0.5f) *
                maximumRadius;
            Vector2 direction = new(
                MathF.Cos(angle),
                MathF.Sin(angle));
            Vector2 tangent = new(
                -direction.Y,
                direction.X);
            derivativeX =
                tangent * polarRadius * MathF.Tau / width;
            derivativeY =
                direction * maximumRadius / height;
            return;
        }

        Vector2 deltaPixels =
            (uv - center) * sourceSize;
        float radiusSquared = deltaPixels.LengthSquared();
        float radialScale = height / maximumRadius;
        if (radiusSquared < 0.000001f)
        {
            derivativeX = new Vector2(0, radialScale);
            derivativeY = new Vector2(
                width * 0.25f,
                radialScale);
            return;
        }

        float radius = MathF.Sqrt(radiusSquared);
        float angularScale =
            width / (MathF.Tau * radiusSquared);
        derivativeX = new Vector2(
            -deltaPixels.Y * angularScale,
            deltaPixels.X * radialScale / radius);
        derivativeY = new Vector2(
            deltaPixels.X * angularScale,
            deltaPixels.Y * radialScale / radius);
    }

    private static Vector4 SamplePolarSource(
        Vector4[] source,
        int width,
        int height,
        Vector2 uv,
        bool wrapAngle)
    {
        if (!wrapAngle)
        {
            return Sample(
                source,
                width,
                height,
                uv,
                1,
                Vector4.Zero);
        }
        if (uv.Y < 0 || uv.Y > 1)
        {
            return Vector4.Zero;
        }

        uv.X -= MathF.Floor(uv.X);
        float sampleX = (uv.X * width) - 0.5f;
        float sampleY = (uv.Y * height) - 0.5f;
        int x0 = (int)MathF.Floor(sampleX);
        int y0 = (int)MathF.Floor(sampleY);
        float fractionX = sampleX - x0;
        float fractionY = sampleY - y0;
        Vector4 top = Vector4.Lerp(
            SamplePolarPixel(
                source,
                width,
                height,
                x0,
                y0),
            SamplePolarPixel(
                source,
                width,
                height,
                x0 + 1,
                y0),
            fractionX);
        Vector4 bottom = Vector4.Lerp(
            SamplePolarPixel(
                source,
                width,
                height,
                x0,
                y0 + 1),
            SamplePolarPixel(
                source,
                width,
                height,
                x0 + 1,
                y0 + 1),
            fractionX);
        return Vector4.Lerp(top, bottom, fractionY);
    }

    private static Vector4 SamplePolarPixel(
        Vector4[] source,
        int width,
        int height,
        int x,
        int y)
    {
        if (y < 0 || y >= height)
        {
            return Vector4.Zero;
        }
        return source[(y * width) + Wrap(x, width)];
    }

}
