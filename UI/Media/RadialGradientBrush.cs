using Cerneala.Drawing;

namespace Cerneala.UI.Media;

public sealed record RadialGradientBrush : Brush
{
    public RadialGradientBrush(DrawPoint center, float radiusX, float radiusY, IEnumerable<GradientStop> stops)
    {
        if (!float.IsFinite(radiusX) || radiusX <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(radiusX), "RadiusX must be finite and positive.");
        }

        if (!float.IsFinite(radiusY) || radiusY <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(radiusY), "RadiusY must be finite and positive.");
        }

        Center = center;
        RadiusX = radiusX;
        RadiusY = radiusY;
        Stops = GradientStopCollection.CreateOrdered(stops);
    }

    public DrawPoint Center { get; }

    public float RadiusX { get; }

    public float RadiusY { get; }

    public IReadOnlyList<GradientStop> Stops { get; }

    public bool Equals(RadialGradientBrush? other)
    {
        return other is not null
            && Center == other.Center
            && RadiusX.Equals(other.RadiusX)
            && RadiusY.Equals(other.RadiusY)
            && GradientStopCollection.SequenceEquals(Stops, other.Stops);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Center, RadiusX, RadiusY, GradientStopCollection.GetSequenceHashCode(Stops));
    }
}
