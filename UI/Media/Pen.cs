namespace Cerneala.UI.Media;

public sealed record Pen
{
    public Pen(Brush brush, float thickness)
    {
        if (!float.IsFinite(thickness) || thickness < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(thickness), "Pen thickness must be finite and non-negative.");
        }

        Brush = brush ?? throw new ArgumentNullException(nameof(brush));
        Thickness = thickness;
    }

    public Brush Brush { get; }

    public float Thickness { get; }
}
