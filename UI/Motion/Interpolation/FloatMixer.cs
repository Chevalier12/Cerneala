namespace Cerneala.UI.Motion.Interpolation;

public sealed class FloatMixer : ValueMixer<float>
{
    public override bool SupportsVectorOperations => true;

    public override float Mix(float from, float to, float progress)
    {
        return Lerp(from, to, progress);
    }

    public override bool EqualsWithinTolerance(float left, float right, float tolerance)
    {
        ThrowIfNegativeTolerance(tolerance);
        return MathF.Abs(left - right) <= tolerance;
    }

    public override float Add(float left, float right)
    {
        return left + right;
    }

    public override float Subtract(float left, float right)
    {
        return left - right;
    }

    public override float Scale(float value, float scalar)
    {
        return value * scalar;
    }

    public override float Magnitude(float value)
    {
        return MathF.Abs(value);
    }
}
