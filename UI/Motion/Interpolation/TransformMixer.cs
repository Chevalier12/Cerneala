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

        return mode == TransformInterpolationMode.Matrix
            ? MixMatrix(from, to, progress)
            : Compose(MixComponents(Decompose(from), Decompose(to), progress));
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
        Matrix3x2 matrix = transform.Matrix;

        // Canonical affine decomposition for UI transforms: translation, scale,
        // rotation, and equivalent X skew. Matrices with collapsed scale are
        // rejected instead of silently falling back to matrix lerp in component mode.
        float scaleX = MathF.Sqrt((matrix.M11 * matrix.M11) + (matrix.M12 * matrix.M12));
        if (scaleX <= DecompositionEpsilon)
        {
            throw new InvalidOperationException("Transform matrix cannot be decomposed because ScaleX is too close to zero.");
        }

        float rotation = MathF.Atan2(matrix.M12, matrix.M11);
        float determinant = (matrix.M11 * matrix.M22) - (matrix.M12 * matrix.M21);
        float scaleY = determinant / scaleX;
        if (MathF.Abs(scaleY) <= DecompositionEpsilon)
        {
            throw new InvalidOperationException("Transform matrix cannot be decomposed because ScaleY is too close to zero.");
        }

        float dot = (matrix.M11 * matrix.M21) + (matrix.M12 * matrix.M22);
        float skewX = MathF.Atan(dot / (scaleX * scaleY));

        return new TransformComponents(matrix.M31, matrix.M32, scaleX, scaleY, rotation, skewX, 0);
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
