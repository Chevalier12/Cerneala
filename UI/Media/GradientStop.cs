using Cerneala.Drawing;

namespace Cerneala.UI.Media;

public readonly record struct GradientStop
{
    public GradientStop(float offset, Color color)
    {
        if (!float.IsFinite(offset) || offset < 0 || offset > 1)
        {
            throw new ArgumentOutOfRangeException(nameof(offset), "Gradient stop offset must be between 0 and 1.");
        }

        Offset = offset;
        Color = color;
    }

    public float Offset { get; }

    public Color Color { get; }
}
