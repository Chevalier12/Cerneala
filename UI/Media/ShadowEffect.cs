using Cerneala.Drawing;

namespace Cerneala.UI.Media;

public readonly record struct ShadowEffect
{
    public ShadowEffect(DrawPoint offset, float blurRadius, DrawColor color)
    {
        if (!float.IsFinite(blurRadius) || blurRadius < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(blurRadius), "Blur radius must be finite and non-negative.");
        }

        Offset = offset;
        BlurRadius = blurRadius;
        Color = color;
    }

    public DrawPoint Offset { get; }

    public float BlurRadius { get; }

    public DrawColor Color { get; }
}
