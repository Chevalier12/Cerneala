using Cerneala.Drawing;

namespace Cerneala.UI.Motion.Interpolation;

public sealed class DrawSizeMixer : ValueMixer<DrawSize>
{
    public override bool SupportsVectorOperations => true;

    public override DrawSize Mix(DrawSize from, DrawSize to, float progress)
    {
        return new DrawSize(Lerp(from.Width, to.Width, progress), Lerp(from.Height, to.Height, progress));
    }

    public override bool EqualsWithinTolerance(DrawSize left, DrawSize right, float tolerance)
    {
        ThrowIfNegativeTolerance(tolerance);
        return MathF.Abs(left.Width - right.Width) <= tolerance
            && MathF.Abs(left.Height - right.Height) <= tolerance;
    }

    public override DrawSize Add(DrawSize left, DrawSize right)
    {
        return new DrawSize(left.Width + right.Width, left.Height + right.Height);
    }

    public override DrawSize Subtract(DrawSize left, DrawSize right)
    {
        return new DrawSize(left.Width - right.Width, left.Height - right.Height);
    }

    public override DrawSize Scale(DrawSize value, float scalar)
    {
        return new DrawSize(value.Width * scalar, value.Height * scalar);
    }

    public override float Magnitude(DrawSize value)
    {
        return MathF.Sqrt((value.Width * value.Width) + (value.Height * value.Height));
    }
}
