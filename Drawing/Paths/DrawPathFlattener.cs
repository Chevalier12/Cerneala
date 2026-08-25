namespace Cerneala.Drawing.Paths;

internal static class DrawPathFlattener
{
    public static IReadOnlyList<DrawPoint[]> Flatten(DrawPath path, float tolerance)
    {
        return FlattenContours(path, tolerance, minimumPointCount: 3)
            .Select(contour => contour.Points.ToArray())
            .ToArray();
    }

    public static IReadOnlyList<DrawStrokeContour> FlattenStroke(
        DrawPath path,
        float tolerance) =>
        FlattenContours(path, tolerance, minimumPointCount: 2);

    private static IReadOnlyList<DrawStrokeContour> FlattenContours(
        DrawPath path,
        float tolerance,
        int minimumPointCount)
    {
        ArgumentNullException.ThrowIfNull(path);
        if (!float.IsFinite(tolerance) || tolerance <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(tolerance));
        }

        List<DrawStrokeContour> flattened = [];
        foreach (DrawPathContour contour in path.Contours)
        {
            List<DrawPoint> points = [contour.StartPoint];
            DrawPoint current = contour.StartPoint;
            foreach (DrawPathSegment segment in contour.Segments)
            {
                switch (segment.Kind)
                {
                    case DrawPathSegmentKind.Move:
                        break;
                    case DrawPathSegmentKind.Line:
                        AddPoint(points, segment.EndPoint);
                        break;
                    case DrawPathSegmentKind.Quadratic:
                        FlattenQuadratic(points, current, segment.Control1, segment.EndPoint, tolerance, 0);
                        break;
                    case DrawPathSegmentKind.Cubic:
                        FlattenCubic(points, current, segment.Control1, segment.Control2, segment.EndPoint, tolerance, 0);
                        break;
                    case DrawPathSegmentKind.Arc:
                        FlattenArc(points, current, segment, tolerance);
                        break;
                    case DrawPathSegmentKind.Close:
                        break;
                }

                if (segment.Kind != DrawPathSegmentKind.Move)
                {
                    current = segment.EndPoint;
                }
            }

            if (points.Count >= minimumPointCount)
            {
                flattened.Add(new DrawStrokeContour(
                    points.ToArray(),
                    contour.IsClosed));
            }
        }

        return flattened;
    }

    private static void FlattenCubic(
        List<DrawPoint> points,
        DrawPoint first,
        DrawPoint control1,
        DrawPoint control2,
        DrawPoint end,
        float tolerance,
        int depth)
    {
        if (depth >= 12 ||
            (DistanceToLineSquared(control1, first, end) <= tolerance * tolerance &&
             DistanceToLineSquared(control2, first, end) <= tolerance * tolerance))
        {
            AddPoint(points, end);
            return;
        }

        DrawPoint a = Midpoint(first, control1);
        DrawPoint b = Midpoint(control1, control2);
        DrawPoint c = Midpoint(control2, end);
        DrawPoint d = Midpoint(a, b);
        DrawPoint e = Midpoint(b, c);
        DrawPoint middle = Midpoint(d, e);
        FlattenCubic(points, first, a, d, middle, tolerance, depth + 1);
        FlattenCubic(points, middle, e, c, end, tolerance, depth + 1);
    }

    private static void FlattenQuadratic(
        List<DrawPoint> points,
        DrawPoint first,
        DrawPoint control,
        DrawPoint end,
        float tolerance,
        int depth)
    {
        if (depth >= 12 ||
            DistanceToLineSquared(control, first, end) <= tolerance * tolerance)
        {
            AddPoint(points, end);
            return;
        }

        DrawPoint a = Midpoint(first, control);
        DrawPoint b = Midpoint(control, end);
        DrawPoint middle = Midpoint(a, b);
        FlattenQuadratic(points, first, a, middle, tolerance, depth + 1);
        FlattenQuadratic(points, middle, b, end, tolerance, depth + 1);
    }

    private static void FlattenArc(
        List<DrawPoint> points,
        DrawPoint first,
        DrawPathSegment segment,
        float tolerance)
    {
        if (!SvgArcGeometry.TryCreate(first, segment, out SvgArcGeometry arc))
        {
            AddPoint(points, segment.EndPoint);
            return;
        }

        double maximumRadius = Math.Max(arc.RadiusX, arc.RadiusY);
        double step = 2 * Math.Acos(Math.Clamp(1 - (tolerance / maximumRadius), -1, 1));
        if (!double.IsFinite(step) || step <= 0)
        {
            step = Math.PI / 16;
        }
        int segmentCount = Math.Clamp(
            (int)Math.Ceiling(Math.Abs(arc.DeltaAngle) / step),
            1,
            2048);
        double cosine = Math.Cos(arc.RotationRadians);
        double sine = Math.Sin(arc.RotationRadians);
        for (int index = 1; index <= segmentCount; index++)
        {
            double angle = arc.StartAngle + (arc.DeltaAngle * index / segmentCount);
            double x = arc.Center.X +
                (cosine * arc.RadiusX * Math.Cos(angle)) -
                (sine * arc.RadiusY * Math.Sin(angle));
            double y = arc.Center.Y +
                (sine * arc.RadiusX * Math.Cos(angle)) +
                (cosine * arc.RadiusY * Math.Sin(angle));
            AddPoint(
                points,
                index == segmentCount
                    ? segment.EndPoint
                    : new DrawPoint((float)x, (float)y));
        }
    }

    private static void AddPoint(List<DrawPoint> points, DrawPoint point)
    {
        if (points.Count == 0 || points[^1] != point)
        {
            points.Add(point);
        }
    }

    private static DrawPoint Midpoint(DrawPoint first, DrawPoint second) =>
        new((first.X + second.X) / 2, (first.Y + second.Y) / 2);

    private static float DistanceToLineSquared(
        DrawPoint point,
        DrawPoint start,
        DrawPoint end)
    {
        float dx = end.X - start.X;
        float dy = end.Y - start.Y;
        float lengthSquared = (dx * dx) + (dy * dy);
        if (lengthSquared <= float.Epsilon)
        {
            float x = point.X - start.X;
            float y = point.Y - start.Y;
            return (x * x) + (y * y);
        }

        float cross = ((point.X - start.X) * dy) -
            ((point.Y - start.Y) * dx);
        return cross * cross / lengthSquared;
    }
}

internal readonly record struct SvgArcGeometry(
    DrawPoint Center,
    float RadiusX,
    float RadiusY,
    float RotationRadians,
    double StartAngle,
    double DeltaAngle)
{
    public static bool TryCreate(
        DrawPoint first,
        DrawPathSegment segment,
        out SvgArcGeometry geometry)
    {
        double radiusX = Math.Abs(segment.RadiusX);
        double radiusY = Math.Abs(segment.RadiusY);
        DrawPoint end = segment.EndPoint;
        if (radiusX <= double.Epsilon || radiusY <= double.Epsilon || first == end)
        {
            geometry = default;
            return false;
        }

        double rotation = segment.RotationDegrees * Math.PI / 180;
        double cosine = Math.Cos(rotation);
        double sine = Math.Sin(rotation);
        double halfX = (first.X - end.X) / 2d;
        double halfY = (first.Y - end.Y) / 2d;
        double transformedX = (cosine * halfX) + (sine * halfY);
        double transformedY = (-sine * halfX) + (cosine * halfY);
        double radiiScale =
            (transformedX * transformedX / (radiusX * radiusX)) +
            (transformedY * transformedY / (radiusY * radiusY));
        if (radiiScale > 1)
        {
            double scale = Math.Sqrt(radiiScale);
            radiusX *= scale;
            radiusY *= scale;
        }

        double denominator =
            (radiusX * radiusX * transformedY * transformedY) +
            (radiusY * radiusY * transformedX * transformedX);
        double numerator = denominator <= double.Epsilon
            ? 0
            : Math.Max(
                0,
                ((radiusX * radiusX * radiusY * radiusY) -
                 (radiusX * radiusX * transformedY * transformedY) -
                 (radiusY * radiusY * transformedX * transformedX)) /
                denominator);
        double sign = segment.IsLargeArc == segment.Sweep ? -1 : 1;
        double factor = sign * Math.Sqrt(numerator);
        double centerXPrime = factor * (radiusX * transformedY / radiusY);
        double centerYPrime = factor * (-radiusY * transformedX / radiusX);
        double centerX =
            (cosine * centerXPrime) -
            (sine * centerYPrime) +
            ((first.X + end.X) / 2d);
        double centerY =
            (sine * centerXPrime) +
            (cosine * centerYPrime) +
            ((first.Y + end.Y) / 2d);

        double startAngle = VectorAngle(
            1,
            0,
            (transformedX - centerXPrime) / radiusX,
            (transformedY - centerYPrime) / radiusY);
        double deltaAngle = VectorAngle(
            (transformedX - centerXPrime) / radiusX,
            (transformedY - centerYPrime) / radiusY,
            (-transformedX - centerXPrime) / radiusX,
            (-transformedY - centerYPrime) / radiusY);
        if (!segment.Sweep && deltaAngle > 0)
        {
            deltaAngle -= Math.PI * 2;
        }
        else if (segment.Sweep && deltaAngle < 0)
        {
            deltaAngle += Math.PI * 2;
        }

        geometry = new SvgArcGeometry(
            new DrawPoint((float)centerX, (float)centerY),
            (float)radiusX,
            (float)radiusY,
            (float)rotation,
            startAngle,
            deltaAngle);
        return true;
    }

    private static double VectorAngle(
        double ux,
        double uy,
        double vx,
        double vy)
    {
        double dot = (ux * vx) + (uy * vy);
        double length = Math.Sqrt(
            ((ux * ux) + (uy * uy)) *
            ((vx * vx) + (vy * vy)));
        double angle = Math.Acos(Math.Clamp(dot / length, -1, 1));
        return ((ux * vy) - (uy * vx)) < 0 ? -angle : angle;
    }
}
