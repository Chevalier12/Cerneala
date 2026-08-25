namespace Cerneala.Drawing;

public sealed class DrawPathBuilder
{
    private readonly List<MutableContour> contours = [];
    private MutableContour? currentContour;

    public DrawPathBuilder MoveTo(DrawPoint point)
    {
        currentContour = new MutableContour(point);
        contours.Add(currentContour);
        return this;
    }

    public DrawPathBuilder LineTo(DrawPoint point)
    {
        MutableContour contour = RequireOpenContour();
        contour.Segments.Add(DrawPathSegment.Line(point));
        contour.CurrentPoint = point;
        return this;
    }

    public DrawPathBuilder QuadraticTo(DrawPoint control, DrawPoint endPoint)
    {
        MutableContour contour = RequireOpenContour();
        contour.Segments.Add(DrawPathSegment.Quadratic(control, endPoint));
        contour.CurrentPoint = endPoint;
        return this;
    }

    public DrawPathBuilder CubicTo(DrawPoint control1, DrawPoint control2, DrawPoint endPoint)
    {
        MutableContour contour = RequireOpenContour();
        contour.Segments.Add(DrawPathSegment.Cubic(control1, control2, endPoint));
        contour.CurrentPoint = endPoint;
        return this;
    }

    public DrawPathBuilder ArcTo(
        float radiusX,
        float radiusY,
        float rotationDegrees,
        bool isLargeArc,
        bool sweep,
        DrawPoint endPoint)
    {
        if (!float.IsFinite(radiusX) || radiusX <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(radiusX));
        }
        if (!float.IsFinite(radiusY) || radiusY <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(radiusY));
        }
        if (!float.IsFinite(rotationDegrees))
        {
            throw new ArgumentOutOfRangeException(nameof(rotationDegrees));
        }

        MutableContour contour = RequireOpenContour();
        contour.Segments.Add(DrawPathSegment.Arc(
            radiusX,
            radiusY,
            rotationDegrees,
            isLargeArc,
            sweep,
            endPoint));
        contour.CurrentPoint = endPoint;
        return this;
    }

    public DrawPathBuilder Close()
    {
        MutableContour contour = RequireOpenContour();
        if (contour.Segments.Count == 1)
        {
            throw new InvalidOperationException("A contour must contain a drawable segment before it can be closed.");
        }

        contour.Segments.Add(DrawPathSegment.Close(contour.StartPoint));
        contour.CurrentPoint = contour.StartPoint;
        contour.IsClosed = true;
        return this;
    }

    public DrawPath Build()
    {
        if (contours.Count == 0)
        {
            throw new InvalidOperationException("A path requires at least one contour.");
        }

        List<DrawPathContour> snapshots = new(contours.Count);
        foreach (MutableContour contour in contours)
        {
            if (contour.Segments.Count == 1)
            {
                throw new InvalidOperationException("Every path contour requires at least one drawable segment.");
            }

            snapshots.Add(new DrawPathContour(contour.Segments, contour.IsClosed));
        }

        return new DrawPath(snapshots);
    }

    private MutableContour RequireOpenContour()
    {
        if (currentContour is null)
        {
            throw new InvalidOperationException("MoveTo must begin a contour before adding segments.");
        }
        if (currentContour.IsClosed)
        {
            throw new InvalidOperationException("MoveTo must begin a new contour after Close.");
        }

        return currentContour;
    }

    private sealed class MutableContour
    {
        public MutableContour(DrawPoint startPoint)
        {
            StartPoint = startPoint;
            CurrentPoint = startPoint;
            Segments.Add(DrawPathSegment.Move(startPoint));
        }

        public DrawPoint StartPoint { get; }
        public DrawPoint CurrentPoint { get; set; }
        public List<DrawPathSegment> Segments { get; } = [];
        public bool IsClosed { get; set; }
    }
}
