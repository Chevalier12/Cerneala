using Cerneala.Drawing;

namespace Cerneala.UI.Media;

public sealed record LinearGradientBrush : Brush
{
    public LinearGradientBrush(DrawPoint startPoint, DrawPoint endPoint, IEnumerable<GradientStop> stops)
    {
        StartPoint = startPoint;
        EndPoint = endPoint;
        Stops = GradientStopCollection.CreateOrdered(stops);
    }

    public DrawPoint StartPoint { get; }

    public DrawPoint EndPoint { get; }

    public IReadOnlyList<GradientStop> Stops { get; }

    public bool Equals(LinearGradientBrush? other)
    {
        return other is not null
            && StartPoint == other.StartPoint
            && EndPoint == other.EndPoint
            && GradientStopCollection.SequenceEquals(Stops, other.Stops);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(StartPoint, EndPoint, GradientStopCollection.GetSequenceHashCode(Stops));
    }
}
