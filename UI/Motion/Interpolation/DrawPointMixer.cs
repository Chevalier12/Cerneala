using Cerneala.Drawing;

namespace Cerneala.UI.Motion.Interpolation;

public sealed class DrawPointMixer : ValueMixer<DrawPoint>
{
    public override bool SupportsVectorOperations => true;

    public override DrawPoint Mix(DrawPoint from, DrawPoint to, float progress)
    {
        return new DrawPoint(Lerp(from.X, to.X, progress), Lerp(from.Y, to.Y, progress));
    }

    public override bool EqualsWithinTolerance(DrawPoint left, DrawPoint right, float tolerance)
    {
        ThrowIfNegativeTolerance(tolerance);
        return MathF.Abs(left.X - right.X) <= tolerance
            && MathF.Abs(left.Y - right.Y) <= tolerance;
    }

    public override DrawPoint Add(DrawPoint left, DrawPoint right)
    {
        return new DrawPoint(left.X + right.X, left.Y + right.Y);
    }

    public override DrawPoint Subtract(DrawPoint left, DrawPoint right)
    {
        return new DrawPoint(left.X - right.X, left.Y - right.Y);
    }

    public override DrawPoint Scale(DrawPoint value, float scalar)
    {
        return new DrawPoint(value.X * scalar, value.Y * scalar);
    }

    public override float Magnitude(DrawPoint value)
    {
        return MathF.Sqrt((value.X * value.X) + (value.Y * value.Y));
    }
}
