namespace Cerneala.UI.Motion.Interpolation;

public sealed class DoubleMixer : ValueMixer<double>
{
    public override bool SupportsVectorOperations => true;

    public override double Mix(double from, double to, float progress)
    {
        return from + ((to - from) * progress);
    }

    public override bool EqualsWithinTolerance(double left, double right, float tolerance)
    {
        ThrowIfNegativeTolerance(tolerance);
        return Math.Abs(left - right) <= tolerance;
    }

    public override double Add(double left, double right)
    {
        return left + right;
    }

    public override double Subtract(double left, double right)
    {
        return left - right;
    }

    public override double Scale(double value, float scalar)
    {
        return value * scalar;
    }

    public override float Magnitude(double value)
    {
        return (float)Math.Abs(value);
    }
}
