using System.Collections.ObjectModel;
using Cerneala.Drawing.Paths;

namespace Cerneala.Drawing;

public enum DrawPathSegmentKind
{
    Move,
    Line,
    Quadratic,
    Cubic,
    Arc,
    Close
}

public readonly record struct DrawPathSegment
{
    private DrawPathSegment(
        DrawPathSegmentKind kind,
        DrawPoint endPoint,
        DrawPoint control1,
        DrawPoint control2,
        float radiusX,
        float radiusY,
        float rotationDegrees,
        bool isLargeArc,
        bool sweep)
    {
        Kind = kind;
        EndPoint = endPoint;
        Control1 = control1;
        Control2 = control2;
        RadiusX = radiusX;
        RadiusY = radiusY;
        RotationDegrees = rotationDegrees;
        IsLargeArc = isLargeArc;
        Sweep = sweep;
    }

    public DrawPathSegmentKind Kind { get; }
    public DrawPoint EndPoint { get; }
    public DrawPoint Control1 { get; }
    public DrawPoint Control2 { get; }
    public float RadiusX { get; }
    public float RadiusY { get; }
    public float RotationDegrees { get; }
    public bool IsLargeArc { get; }
    public bool Sweep { get; }

    internal static DrawPathSegment Move(DrawPoint point) =>
        new(DrawPathSegmentKind.Move, point, default, default, 0, 0, 0, false, false);

    internal static DrawPathSegment Line(DrawPoint point) =>
        new(DrawPathSegmentKind.Line, point, default, default, 0, 0, 0, false, false);

    internal static DrawPathSegment Quadratic(DrawPoint control, DrawPoint endPoint) =>
        new(DrawPathSegmentKind.Quadratic, endPoint, control, default, 0, 0, 0, false, false);

    internal static DrawPathSegment Cubic(DrawPoint control1, DrawPoint control2, DrawPoint endPoint) =>
        new(DrawPathSegmentKind.Cubic, endPoint, control1, control2, 0, 0, 0, false, false);

    internal static DrawPathSegment Arc(
        float radiusX,
        float radiusY,
        float rotationDegrees,
        bool isLargeArc,
        bool sweep,
        DrawPoint endPoint) =>
        new(
            DrawPathSegmentKind.Arc,
            endPoint,
            default,
            default,
            radiusX,
            radiusY,
            rotationDegrees,
            isLargeArc,
            sweep);

    internal static DrawPathSegment Close(DrawPoint startPoint) =>
        new(DrawPathSegmentKind.Close, startPoint, default, default, 0, 0, 0, false, false);
}

public sealed class DrawPathContour
{
    internal DrawPathContour(IReadOnlyList<DrawPathSegment> segments, bool isClosed)
    {
        DrawPathSegment[] snapshot = segments.ToArray();
        Segments = new ReadOnlyCollection<DrawPathSegment>(snapshot);
        IsClosed = isClosed;
    }

    public IReadOnlyList<DrawPathSegment> Segments { get; }
    public DrawPoint StartPoint => Segments[0].EndPoint;
    public bool IsClosed { get; }
}

public sealed class DrawPath
{
    private static long nextStableId;

    internal DrawPath(IReadOnlyList<DrawPathContour> contours)
    {
        DrawPathContour[] snapshot = contours.ToArray();
        Contours = new ReadOnlyCollection<DrawPathContour>(snapshot);
        Bounds = CalculateBounds(snapshot);
        StableId = Interlocked.Increment(ref nextStableId);
    }

    public IReadOnlyList<DrawPathContour> Contours { get; }
    public DrawRect Bounds { get; }
    public long StableId { get; }

    private static DrawRect CalculateBounds(IReadOnlyList<DrawPathContour> contours)
    {
        BoundsAccumulator bounds = default;
        foreach (DrawPathContour contour in contours)
        {
            DrawPoint current = contour.StartPoint;
            bounds.Include(current);
            foreach (DrawPathSegment segment in contour.Segments)
            {
                switch (segment.Kind)
                {
                    case DrawPathSegmentKind.Move:
                    case DrawPathSegmentKind.Line:
                    case DrawPathSegmentKind.Close:
                        bounds.Include(segment.EndPoint);
                        break;
                    case DrawPathSegmentKind.Quadratic:
                        bounds.Include(segment.Control1);
                        bounds.Include(segment.EndPoint);
                        break;
                    case DrawPathSegmentKind.Cubic:
                        bounds.Include(segment.Control1);
                        bounds.Include(segment.Control2);
                        bounds.Include(segment.EndPoint);
                        break;
                    case DrawPathSegmentKind.Arc:
                        bounds.IncludeArc(current, segment);
                        break;
                }

                if (segment.Kind != DrawPathSegmentKind.Move)
                {
                    current = segment.EndPoint;
                }
            }
        }

        return bounds.CreateRect();
    }

    private struct BoundsAccumulator
    {
        private float minX;
        private float minY;
        private float maxX;
        private float maxY;
        private bool hasPoint;

        public void Include(DrawPoint point)
        {
            if (!hasPoint)
            {
                minX = maxX = point.X;
                minY = maxY = point.Y;
                hasPoint = true;
                return;
            }

            minX = MathF.Min(minX, point.X);
            minY = MathF.Min(minY, point.Y);
            maxX = MathF.Max(maxX, point.X);
            maxY = MathF.Max(maxY, point.Y);
        }

        public void IncludeArc(DrawPoint start, DrawPathSegment segment)
        {
            Include(start);
            Include(segment.EndPoint);
            if (!SvgArcGeometry.TryCreate(start, segment, out SvgArcGeometry arc))
            {
                return;
            }

            double rotation = arc.RotationRadians;
            double xExtremum = Math.Atan2(
                -arc.RadiusY * Math.Sin(rotation),
                arc.RadiusX * Math.Cos(rotation));
            double yExtremum = Math.Atan2(
                arc.RadiusY * Math.Cos(rotation),
                arc.RadiusX * Math.Sin(rotation));
            IncludeArcCandidate(arc, xExtremum);
            IncludeArcCandidate(arc, xExtremum + Math.PI);
            IncludeArcCandidate(arc, yExtremum);
            IncludeArcCandidate(arc, yExtremum + Math.PI);
        }

        private void IncludeArcCandidate(SvgArcGeometry arc, double angle)
        {
            if (!IsAngleInSweep(angle, arc.StartAngle, arc.DeltaAngle))
            {
                return;
            }

            double cosine = Math.Cos(arc.RotationRadians);
            double sine = Math.Sin(arc.RotationRadians);
            double angleCosine = Math.Cos(angle);
            double angleSine = Math.Sin(angle);
            Include(new DrawPoint(
                arc.Center.X + (float)(
                    (arc.RadiusX * angleCosine * cosine) -
                    (arc.RadiusY * angleSine * sine)),
                arc.Center.Y + (float)(
                    (arc.RadiusX * angleCosine * sine) +
                    (arc.RadiusY * angleSine * cosine))));
        }

        private static bool IsAngleInSweep(
            double angle,
            double startAngle,
            double deltaAngle)
        {
            const double tolerance = 1e-10;
            return deltaAngle >= 0
                ? NormalizeAngle(angle - startAngle) <= deltaAngle + tolerance
                : NormalizeAngle(startAngle - angle) <= -deltaAngle + tolerance;
        }

        private static double NormalizeAngle(double angle)
        {
            double normalized = angle % (Math.PI * 2);
            return normalized < 0 ? normalized + (Math.PI * 2) : normalized;
        }

        public readonly DrawRect CreateRect() => hasPoint
            ? new DrawRect(minX, minY, maxX - minX, maxY - minY)
            : default;
    }
}
