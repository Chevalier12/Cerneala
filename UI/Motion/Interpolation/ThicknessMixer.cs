using Cerneala.UI.Layout;

namespace Cerneala.UI.Motion.Interpolation;

public sealed class ThicknessMixer : ValueMixer<Thickness>
{
    public override bool SupportsVectorOperations => true;

    public override Thickness Mix(Thickness from, Thickness to, float progress)
    {
        return new Thickness(
            Lerp(from.Left, to.Left, progress),
            Lerp(from.Top, to.Top, progress),
            Lerp(from.Right, to.Right, progress),
            Lerp(from.Bottom, to.Bottom, progress));
    }

    public override bool EqualsWithinTolerance(Thickness left, Thickness right, float tolerance)
    {
        ThrowIfNegativeTolerance(tolerance);
        return MathF.Abs(left.Left - right.Left) <= tolerance
            && MathF.Abs(left.Top - right.Top) <= tolerance
            && MathF.Abs(left.Right - right.Right) <= tolerance
            && MathF.Abs(left.Bottom - right.Bottom) <= tolerance;
    }

    public override Thickness Add(Thickness left, Thickness right)
    {
        return new Thickness(left.Left + right.Left, left.Top + right.Top, left.Right + right.Right, left.Bottom + right.Bottom);
    }

    public override Thickness Subtract(Thickness left, Thickness right)
    {
        return new Thickness(left.Left - right.Left, left.Top - right.Top, left.Right - right.Right, left.Bottom - right.Bottom);
    }

    public override Thickness Scale(Thickness value, float scalar)
    {
        return new Thickness(value.Left * scalar, value.Top * scalar, value.Right * scalar, value.Bottom * scalar);
    }

    public override float Magnitude(Thickness value)
    {
        return MathF.Sqrt((value.Left * value.Left) + (value.Top * value.Top) + (value.Right * value.Right) + (value.Bottom * value.Bottom));
    }
}
