using Cerneala.UI.Media;

namespace Cerneala.UI.Motion.Interpolation;

public sealed class TransformMixer : ValueMixer<Transform>
{
    private const float DecompositionEpsilon = 0.000001f;
    private readonly TransformInterpolationMode mode;

    public TransformMixer(TransformInterpolationMode mode = TransformInterpolationMode.Components)
    {
        this.mode = mode;
    }

    public override bool SupportsVectorOperations => false;

    public override Transform Mix(Transform from, Transform to, float progress)
    {
        ArgumentNullException.ThrowIfNull(from);
        ArgumentNullException.ThrowIfNull(to);
        if (progress <= 0)
        {
            return from;
        }

        if (progress >= 1)
        {
            return to;
        }

        if (mode == TransformInterpolationMode.Matrix
            || !TryResolveComponents(from, to, out TransformComponents fromComponents, out TransformComponents toComponents))
        {
            return MixMatrix(from, to, progress);
        }

        return Compose(MixComponents(fromComponents, toComponents, progress));
    }

    public override bool EqualsWithinTolerance(Transform left, Transform right, float tolerance)
    {
        ArgumentNullException.ThrowIfNull(left);
        ArgumentNullException.ThrowIfNull(right);
        ThrowIfNegativeTolerance(tolerance);
        return MathF.Abs(left.Matrix.M11 - right.Matrix.M11) <= tolerance
            && MathF.Abs(left.Matrix.M12 - right.Matrix.M12) <= tolerance
            && MathF.Abs(left.Matrix.M21 - right.Matrix.M21) <= tolerance
            && MathF.Abs(left.Matrix.M22 - right.Matrix.M22) <= tolerance
            && MathF.Abs(left.Matrix.M31 - right.Matrix.M31) <= tolerance
            && MathF.Abs(left.Matrix.M32 - right.Matrix.M32) <= tolerance;
    }

    public static TransformComponents Decompose(Transform transform)
    {
        ArgumentNullException.ThrowIfNull(transform);
        if (!TryDecompose(transform, out TransformComponents components, out string? failureReason))
        {
            throw new InvalidOperationException(failureReason);
        }

        return components;
    }

    // Shared pair resolution for component-space consumers. False means that the
    // pair requires matrix interpolation; public Decompose remains strict.
    internal static bool TryResolveComponents(
        Transform from,
        Transform to,
        out TransformComponents fromComponents,
        out TransformComponents toComponents)
    {
        bool fromValid = TryDecompose(from, out fromComponents, out _);
        bool toValid = TryDecompose(to, out toComponents, out _);
        if (fromValid && toValid)
        {
            return true;
        }

        if (fromValid)
        {
            return TryResolveDegenerateComponents(to, fromComponents, out toComponents);
        }

        return toValid && TryResolveDegenerateComponents(from, toComponents, out fromComponents);
    }

    private static bool TryDecompose(Transform transform, out TransformComponents components, out string? failureReason)
    {
        Matrix3x2 matrix = transform.Matrix;
        components = default;
        failureReason = null;
        float scaleX = MathF.Sqrt((matrix.M11 * matrix.M11) + (matrix.M12 * matrix.M12));
        if (scaleX <= DecompositionEpsilon)
        {
            failureReason = "Transform matrix cannot be decomposed because ScaleX is too close to zero.";
            return false;
        }

        float rotation = MathF.Atan2(matrix.M12, matrix.M11);
        float determinant = (matrix.M11 * matrix.M22) - (matrix.M12 * matrix.M21);
        float scaleY = determinant / scaleX;
        if (MathF.Abs(scaleY) <= DecompositionEpsilon)
        {
            failureReason = "Transform matrix cannot be decomposed because ScaleY is too close to zero.";
            return false;
        }

        float dot = (matrix.M11 * matrix.M21) + (matrix.M12 * matrix.M22);
        float skewX = MathF.Atan(dot / (scaleX * scaleY));

        components = new TransformComponents(matrix.M31, matrix.M32, scaleX, scaleY, rotation, skewX, 0);
        return true;
    }

    private static bool TryResolveDegenerateComponents(
        Transform transform,
        TransformComponents reference,
        out TransformComponents components)
    {
        Matrix3x2 matrix = transform.Matrix;
        Matrix3x2 basis = Compose(reference with
        {
            TranslationX = 0,
            TranslationY = 0,
            ScaleX = 1,
            ScaleY = 1
        }).Matrix;

        // Each scale multiplies one row of skew * rotation. Project onto those
        // rows, retaining the sign even when a scale is below the strict threshold.
        float scaleX = ProjectScale(matrix.M11, matrix.M12, basis.M11, basis.M12);
        float scaleY = ProjectScale(matrix.M21, matrix.M22, basis.M21, basis.M22);
        components = reference with
        {
            TranslationX = matrix.M31,
            TranslationY = matrix.M32,
            ScaleX = scaleX,
            ScaleY = scaleY
        };

        if (!float.IsFinite(scaleX) || !float.IsFinite(scaleY))
        {
            return false;
        }

        Matrix3x2 reconstructed = Compose(components).Matrix;
        return MathF.Abs(matrix.M11 - reconstructed.M11) <= DecompositionEpsilon
            && MathF.Abs(matrix.M12 - reconstructed.M12) <= DecompositionEpsilon
            && MathF.Abs(matrix.M21 - reconstructed.M21) <= DecompositionEpsilon
            && MathF.Abs(matrix.M22 - reconstructed.M22) <= DecompositionEpsilon;
    }

    private static float ProjectScale(float x, float y, float basisX, float basisY)
    {
        // Double intermediates avoid losing the tiny row or overflowing the
        // projection's dot products; the stored component remains a float.
        return (float)((((double)x * basisX) + ((double)y * basisY))
            / (((double)basisX * basisX) + ((double)basisY * basisY)));
    }

    public static Transform Compose(TransformComponents components)
    {
        float cos = MathF.Cos(components.RotationRadians);
        float sin = MathF.Sin(components.RotationRadians);
        float skewX = MathF.Tan(components.SkewX);
        float skewY = MathF.Tan(components.SkewY);

        Matrix3x2 scale = new(components.ScaleX, 0, 0, components.ScaleY, 0, 0);
        Matrix3x2 skew = new(1, skewY, skewX, 1, 0, 0);
        Matrix3x2 rotation = new(cos, sin, -sin, cos, 0, 0);
        Matrix3x2 translation = Matrix3x2.CreateTranslation(components.TranslationX, components.TranslationY);

        return new Transform(Matrix3x2.Multiply(Matrix3x2.Multiply(Matrix3x2.Multiply(scale, skew), rotation), translation));
    }

    private static Transform MixMatrix(Transform from, Transform to, float progress)
    {
        return new Transform(new Matrix3x2(
            Lerp(from.Matrix.M11, to.Matrix.M11, progress),
            Lerp(from.Matrix.M12, to.Matrix.M12, progress),
            Lerp(from.Matrix.M21, to.Matrix.M21, progress),
            Lerp(from.Matrix.M22, to.Matrix.M22, progress),
            Lerp(from.Matrix.M31, to.Matrix.M31, progress),
            Lerp(from.Matrix.M32, to.Matrix.M32, progress)));
    }

    private static TransformComponents MixComponents(TransformComponents from, TransformComponents to, float progress)
    {
        return new TransformComponents(
            Lerp(from.TranslationX, to.TranslationX, progress),
            Lerp(from.TranslationY, to.TranslationY, progress),
            Lerp(from.ScaleX, to.ScaleX, progress),
            Lerp(from.ScaleY, to.ScaleY, progress),
            LerpAngle(from.RotationRadians, to.RotationRadians, progress),
            Lerp(from.SkewX, to.SkewX, progress),
            Lerp(from.SkewY, to.SkewY, progress));
    }

    private static float LerpAngle(float from, float to, float progress)
    {
        float delta = to - from;
        while (delta > MathF.PI)
        {
            delta -= MathF.Tau;
        }

        while (delta < -MathF.PI)
        {
            delta += MathF.Tau;
        }

        return from + (delta * progress);
    }

}
