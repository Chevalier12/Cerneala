using Cerneala.Drawing;

namespace Cerneala.UI.Media;

public sealed record LinearGradientBrush : Brush
{
    public LinearGradientBrush(DrawPoint startPoint, DrawPoint endPoint, IEnumerable<GradientStop> stops, float opacity = 1)
        : base(opacity)
    {
        ValidatePoint(startPoint, nameof(startPoint));
        ValidatePoint(endPoint, nameof(endPoint));
        StartPoint = startPoint;
        EndPoint = endPoint;
        Stops = GradientStopCollection.CreateOrdered(stops);
    }

    public DrawPoint StartPoint { get; }

    public DrawPoint EndPoint { get; }

    public IReadOnlyList<GradientStop> Stops { get; }

    public override DrawBrushKind Kind => DrawBrushKind.LinearGradient;

    public bool Equals(LinearGradientBrush? other)
    {
        return other is not null
            && StartPoint == other.StartPoint
            && EndPoint == other.EndPoint
            && Opacity.Equals(other.Opacity)
            && GradientStopCollection.SequenceEquals(Stops, other.Stops);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(StartPoint, EndPoint, Opacity, GradientStopCollection.GetSequenceHashCode(Stops));
    }

    protected override DrawBrushDescriptor CreateDescriptor()
    {
        return new LinearGradientDrawBrushDescriptor(StartPoint, EndPoint, GradientStopCollection.ToDrawStops(Stops), Opacity);
    }

    private static void ValidatePoint(DrawPoint point, string parameterName)
    {
        if (!float.IsFinite(point.X) || !float.IsFinite(point.Y))
        {
            throw new ArgumentOutOfRangeException(parameterName, "Gradient coordinates must be finite.");
        }
    }
}
