using Cerneala.Drawing;

namespace Cerneala.UI.Motion.Interpolation;

public sealed class DrawRectMixer : ValueMixer<DrawRect>
{
    public override DrawRect Mix(DrawRect from, DrawRect to, float progress)
    {
        if (progress <= 0)
        {
            return from;
        }

        if (progress >= 1)
        {
            return to;
        }

        return new DrawRect(
            Lerp(from.X, to.X, progress),
            Lerp(from.Y, to.Y, progress),
            Lerp(from.Width, to.Width, progress),
            Lerp(from.Height, to.Height, progress));
    }

    public override bool EqualsWithinTolerance(DrawRect left, DrawRect right, float tolerance)
    {
        ThrowIfNegativeTolerance(tolerance);
        return MathF.Abs(left.X - right.X) <= tolerance
            && MathF.Abs(left.Y - right.Y) <= tolerance
            && MathF.Abs(left.Width - right.Width) <= tolerance
            && MathF.Abs(left.Height - right.Height) <= tolerance;
    }
}
