using Cerneala.Drawing;

namespace Cerneala.UI.Media;

public sealed record PathGeometry : Geometry
{
    public PathGeometry(IEnumerable<DrawPoint> points)
    {
        ArgumentNullException.ThrowIfNull(points);

        DrawPoint[] pointArray = points.ToArray();
        if (pointArray.Length == 0)
        {
            throw new ArgumentException("A path geometry requires at least one point.", nameof(points));
        }

        Points = Array.AsReadOnly(pointArray);
        Bounds = CalculateBounds(pointArray);
        StrokePath = pointArray.Length >= 2
            ? CreateStrokePath(pointArray)
            : null;
    }

    public IReadOnlyList<DrawPoint> Points { get; }

    public override DrawRect Bounds { get; }

    internal DrawPath? StrokePath { get; }

    private static DrawPath CreateStrokePath(
        IReadOnlyList<DrawPoint> points)
    {
        DrawPathBuilder builder = new DrawPathBuilder()
            .MoveTo(points[0]);
        for (int index = 1; index < points.Count; index++)
        {
            builder.LineTo(points[index]);
        }
        return builder.Build();
    }

    private static DrawRect CalculateBounds(IReadOnlyList<DrawPoint> points)
    {
        float minX = points[0].X;
        float minY = points[0].Y;
        float maxX = points[0].X;
        float maxY = points[0].Y;

        for (int i = 1; i < points.Count; i++)
        {
            DrawPoint point = points[i];
            minX = MathF.Min(minX, point.X);
            minY = MathF.Min(minY, point.Y);
            maxX = MathF.Max(maxX, point.X);
            maxY = MathF.Max(maxY, point.Y);
        }

        return new DrawRect(minX, minY, maxX - minX, maxY - minY);
    }
}
