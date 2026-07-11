using Cerneala.Drawing;

namespace Cerneala.UI.Motion.Interpolation;

public sealed class ColorMixer : ValueMixer<Color>
{
    public override Color Mix(Color from, Color to, float progress)
    {
        if (progress <= 0)
        {
            return from;
        }

        if (progress >= 1)
        {
            return to;
        }

        return new Color(
            MixChannel(from.R, to.R, progress),
            MixChannel(from.G, to.G, progress),
            MixChannel(from.B, to.B, progress),
            MixChannel(from.A, to.A, progress));
    }

    public override bool EqualsWithinTolerance(Color left, Color right, float tolerance)
    {
        ThrowIfNegativeTolerance(tolerance);
        return Math.Abs(left.R - right.R) <= tolerance
            && Math.Abs(left.G - right.G) <= tolerance
            && Math.Abs(left.B - right.B) <= tolerance
            && Math.Abs(left.A - right.A) <= tolerance;
    }

    private static byte MixChannel(byte from, byte to, float progress)
    {
        return (byte)Math.Clamp(MathF.Round(Lerp(from, to, progress), MidpointRounding.AwayFromZero), byte.MinValue, byte.MaxValue);
    }
}
