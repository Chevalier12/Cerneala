using Cerneala.Drawing;

namespace Cerneala.UI.Motion.Interpolation;

public sealed class DrawRectMixer : ValueMixer<DrawRect>
{
    public override DrawRect Mix(DrawRect from, DrawRect to, float progress)
    {
        if (progress == 0)
        {
            return from;
        }

        if (progress == 1)
        {
            return to;
        }

        // Overshoot extrapolates, but a DrawRect cannot hold a negative size,
        // so the size channels clamp at zero like color channels clamp at 0..255.
        return new DrawRect(
            Lerp(from.X, to.X, progress),
            Lerp(from.Y, to.Y, progress),
            MathF.Max(0, Lerp(from.Width, to.Width, progress)),
            MathF.Max(0, Lerp(from.Height, to.Height, progress)));
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
