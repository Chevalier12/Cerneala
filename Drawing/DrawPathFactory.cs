namespace Cerneala.Drawing;

public enum DrawArcDirection
{
    Clockwise,
    CounterClockwise
}

public static class DrawPathFactory
{
    public static DrawPath Polygon(IEnumerable<DrawPoint> points) =>
        CreatePointPath(points, close: true, minimumPointCount: 3);

    public static DrawPath Polyline(IEnumerable<DrawPoint> points) =>
        CreatePointPath(points, close: false, minimumPointCount: 2);

    public static DrawPath RoundedRectangle(
        DrawRect bounds,
        DrawCornerRadius cornerRadius)
    {
        DrawCornerRadius radii = cornerRadius.Normalize(bounds);
        DrawPathBuilder builder = new();
        builder.MoveTo(new DrawPoint(bounds.X + radii.TopLeft, bounds.Y));
        AddCorner(
            builder,
            new DrawPoint(bounds.Right - radii.TopRight, bounds.Y),
            radii.TopRight,
            new DrawPoint(bounds.Right, bounds.Y + radii.TopRight));
        AddCorner(
            builder,
            new DrawPoint(bounds.Right, bounds.Bottom - radii.BottomRight),
            radii.BottomRight,
            new DrawPoint(bounds.Right - radii.BottomRight, bounds.Bottom));
        AddCorner(
            builder,
            new DrawPoint(bounds.X + radii.BottomLeft, bounds.Bottom),
            radii.BottomLeft,
            new DrawPoint(bounds.X, bounds.Bottom - radii.BottomLeft));
        AddCorner(
            builder,
            new DrawPoint(bounds.X, bounds.Y + radii.TopLeft),
            radii.TopLeft,
            new DrawPoint(bounds.X + radii.TopLeft, bounds.Y));
        return builder.Close().Build();
    }

    public static DrawPath Arc(
        DrawPoint center,
        float radiusX,
        float radiusY,
        float startAngle,
        float sweepAngle,
        DrawArcDirection direction = DrawArcDirection.Clockwise)
    {
        ValidateArc(radiusX, radiusY, startAngle, sweepAngle, direction);
        DrawPoint start = PointOnEllipse(center, radiusX, radiusY, startAngle);
        DrawPathBuilder builder = new DrawPathBuilder().MoveTo(start);
        AppendArc(
            builder,
            center,
            radiusX,
            radiusY,
            startAngle,
            sweepAngle,
            direction,
            start);
        return builder.Build();
    }

    public static DrawPath Pie(
        DrawPoint center,
        float radiusX,
        float radiusY,
        float startAngle,
        float sweepAngle,
        DrawArcDirection direction = DrawArcDirection.Clockwise)
    {
        ValidateArc(radiusX, radiusY, startAngle, sweepAngle, direction);
        DrawPoint start = PointOnEllipse(center, radiusX, radiusY, startAngle);
        DrawPathBuilder builder = new DrawPathBuilder()
            .MoveTo(center)
            .LineTo(start);
        AppendArc(
            builder,
            center,
            radiusX,
            radiusY,
            startAngle,
            sweepAngle,
            direction,
            start);
        return builder.Close().Build();
    }

    public static DrawPath Chord(
        DrawPoint center,
        float radiusX,
        float radiusY,
        float startAngle,
        float sweepAngle,
        DrawArcDirection direction = DrawArcDirection.Clockwise)
    {
        ValidateArc(radiusX, radiusY, startAngle, sweepAngle, direction);
        DrawPoint start = PointOnEllipse(center, radiusX, radiusY, startAngle);
        DrawPathBuilder builder = new DrawPathBuilder().MoveTo(start);
        AppendArc(
            builder,
            center,
            radiusX,
            radiusY,
            startAngle,
            sweepAngle,
            direction,
            start);
        return builder.Close().Build();
    }

    public static DrawPath RegularPolygon(
        DrawPoint center,
        float radius,
        int sideCount,
        float rotation = 0)
    {
        ValidateRadialShape(radius, sideCount, rotation, nameof(sideCount));
        DrawPoint[] points = new DrawPoint[sideCount];
        for (int index = 0; index < points.Length; index++)
        {
            float angle = rotation + (MathF.Tau * index / sideCount);
            points[index] = PointOnEllipse(center, radius, radius, angle);
        }
        return Polygon(points);
    }

    public static DrawPath Star(
        DrawPoint center,
        float outerRadius,
        float innerRadius,
        int pointCount,
        float rotation = 0)
    {
        ValidateRadialShape(outerRadius, pointCount, rotation, nameof(pointCount));
        if (!float.IsFinite(innerRadius) || innerRadius <= 0 || innerRadius > outerRadius)
        {
            throw new ArgumentOutOfRangeException(nameof(innerRadius));
        }

        DrawPoint[] points = new DrawPoint[checked(pointCount * 2)];
        for (int index = 0; index < points.Length; index++)
        {
            float radius = (index & 1) == 0 ? outerRadius : innerRadius;
            float angle = rotation + (MathF.PI * index / pointCount);
            points[index] = PointOnEllipse(center, radius, radius, angle);
        }
        return Polygon(points);
    }

    private static DrawPath CreatePointPath(
        IEnumerable<DrawPoint> points,
        bool close,
        int minimumPointCount)
    {
        ArgumentNullException.ThrowIfNull(points);
        DrawPoint[] snapshot = points.ToArray();
        if (snapshot.Length < minimumPointCount)
        {
            throw new ArgumentException(
                $"At least {minimumPointCount} points are required.",
                nameof(points));
        }

        DrawPathBuilder builder = new DrawPathBuilder().MoveTo(snapshot[0]);
        for (int index = 1; index < snapshot.Length; index++)
        {
            builder.LineTo(snapshot[index]);
        }
        return close ? builder.Close().Build() : builder.Build();
    }

    private static void AddCorner(
        DrawPathBuilder builder,
        DrawPoint lineEnd,
        float radius,
        DrawPoint arcEnd)
    {
        builder.LineTo(lineEnd);
        if (radius > 0)
        {
            builder.ArcTo(radius, radius, 0, isLargeArc: false, sweep: true, arcEnd);
        }
        else if (lineEnd != arcEnd)
        {
            builder.LineTo(arcEnd);
        }
    }

    private static void AppendArc(
        DrawPathBuilder builder,
        DrawPoint center,
        float radiusX,
        float radiusY,
        float startAngle,
        float sweepAngle,
        DrawArcDirection direction,
        DrawPoint start)
    {
        bool clockwise = direction == DrawArcDirection.Clockwise;
        float signedSweep = clockwise ? sweepAngle : -sweepAngle;
        if (sweepAngle == 0)
        {
            builder.LineTo(start);
            return;
        }
        if (sweepAngle == MathF.Tau)
        {
            DrawPoint midpoint = PointOnEllipse(
                center,
                radiusX,
                radiusY,
                startAngle + (clockwise ? MathF.PI : -MathF.PI));
            builder.ArcTo(radiusX, radiusY, 0, false, clockwise, midpoint);
            builder.ArcTo(radiusX, radiusY, 0, false, clockwise, start);
            return;
        }

        DrawPoint end = PointOnEllipse(
            center,
            radiusX,
            radiusY,
            startAngle + signedSweep);
        builder.ArcTo(
            radiusX,
            radiusY,
            0,
            isLargeArc: sweepAngle > MathF.PI,
            sweep: clockwise,
            end);
    }

    private static DrawPoint PointOnEllipse(
        DrawPoint center,
        float radiusX,
        float radiusY,
        float angle) =>
        new(
            center.X + (MathF.Cos(angle) * radiusX),
            center.Y + (MathF.Sin(angle) * radiusY));

    private static void ValidateArc(
        float radiusX,
        float radiusY,
        float startAngle,
        float sweepAngle,
        DrawArcDirection direction)
    {
        if (!float.IsFinite(radiusX) || radiusX <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(radiusX));
        }
        if (!float.IsFinite(radiusY) || radiusY <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(radiusY));
        }
        if (!float.IsFinite(startAngle))
        {
            throw new ArgumentOutOfRangeException(nameof(startAngle));
        }
        if (!float.IsFinite(sweepAngle) || sweepAngle < 0 || sweepAngle > MathF.Tau)
        {
            throw new ArgumentOutOfRangeException(nameof(sweepAngle));
        }
        if (!Enum.IsDefined(direction))
        {
            throw new ArgumentOutOfRangeException(nameof(direction));
        }
    }

    private static void ValidateRadialShape(
        float radius,
        int pointCount,
        float rotation,
        string pointCountParameterName)
    {
        if (!float.IsFinite(radius) || radius <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(radius));
        }
        if (pointCount < 3)
        {
            throw new ArgumentOutOfRangeException(pointCountParameterName);
        }
        if (!float.IsFinite(rotation))
        {
            throw new ArgumentOutOfRangeException(nameof(rotation));
        }
    }
}
