using Cerneala.Drawing;

namespace Cerneala.UI.Media;

public readonly record struct Matrix3x2
{
    public Matrix3x2(float m11, float m12, float m21, float m22, float m31, float m32)
    {
        ThrowIfNotFinite(m11, nameof(m11));
        ThrowIfNotFinite(m12, nameof(m12));
        ThrowIfNotFinite(m21, nameof(m21));
        ThrowIfNotFinite(m22, nameof(m22));
        ThrowIfNotFinite(m31, nameof(m31));
        ThrowIfNotFinite(m32, nameof(m32));

        M11 = m11;
        M12 = m12;
        M21 = m21;
        M22 = m22;
        M31 = m31;
        M32 = m32;
    }

    public static Matrix3x2 Identity { get; } = new(1, 0, 0, 1, 0, 0);

    public float M11 { get; }

    public float M12 { get; }

    public float M21 { get; }

    public float M22 { get; }

    public float M31 { get; }

    public float M32 { get; }

    public static Matrix3x2 CreateTranslation(float x, float y)
    {
        ThrowIfNotFinite(x, nameof(x));
        ThrowIfNotFinite(y, nameof(y));
        return new Matrix3x2(1, 0, 0, 1, x, y);
    }

    public static Matrix3x2 CreateScale(float x, float y)
    {
        ThrowIfNotFinite(x, nameof(x));
        ThrowIfNotFinite(y, nameof(y));
        return new Matrix3x2(x, 0, 0, y, 0, 0);
    }

    public static Matrix3x2 CreateRotation(float radians)
    {
        ThrowIfNotFinite(radians, nameof(radians));
        float sin = MathF.Sin(radians);
        float cos = MathF.Cos(radians);
        return new Matrix3x2(cos, sin, -sin, cos, 0, 0);
    }

    public static Matrix3x2 CreateSkew(float radiansX, float radiansY)
    {
        ThrowIfNotFinite(radiansX, nameof(radiansX));
        ThrowIfNotFinite(radiansY, nameof(radiansY));
        return new Matrix3x2(1, MathF.Tan(radiansY), MathF.Tan(radiansX), 1, 0, 0);
    }

    public DrawPoint Transform(DrawPoint point)
    {
        return new DrawPoint(
            (point.X * M11) + (point.Y * M21) + M31,
            (point.X * M12) + (point.Y * M22) + M32);
    }

    public static Matrix3x2 Multiply(Matrix3x2 left, Matrix3x2 right)
    {
        return new Matrix3x2(
            (left.M11 * right.M11) + (left.M12 * right.M21),
            (left.M11 * right.M12) + (left.M12 * right.M22),
            (left.M21 * right.M11) + (left.M22 * right.M21),
            (left.M21 * right.M12) + (left.M22 * right.M22),
            (left.M31 * right.M11) + (left.M32 * right.M21) + right.M31,
            (left.M31 * right.M12) + (left.M32 * right.M22) + right.M32);
    }

    private static void ThrowIfNotFinite(float value, string parameterName)
    {
        if (!float.IsFinite(value))
        {
            throw new ArgumentOutOfRangeException(parameterName, "Matrix values must be finite.");
        }
    }
}
