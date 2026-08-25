namespace Cerneala.Drawing.Paths;

internal static class DrawStrokeMeshBuilder
{
    public static DrawStrokeRenderMesh Build(
        DrawCommand command,
        float thickness,
        DrawStrokeStyle style,
        float coordinateScale)
    {
        DrawArgument.ThrowIfNotValidPixelSize(thickness, nameof(thickness));
        ArgumentNullException.ThrowIfNull(style);

        IReadOnlyList<DrawStrokeContour> contours = CreateContours(command, coordinateScale);
        DrawStrokeMesh stroke = DrawStrokeTessellator.Tessellate(contours, thickness, style);
        if (stroke.IsEmpty)
        {
            return new DrawStrokeRenderMesh(
                new DrawTriangleMesh([], []),
                [],
                0,
                0);
        }

        float minimumX = stroke.Vertices.Min(point => point.X) * coordinateScale;
        float minimumY = stroke.Vertices.Min(point => point.Y) * coordinateScale;
        int left = (int)MathF.Floor(minimumX);
        int top = (int)MathF.Floor(minimumY);
        DrawPoint[] vertices = new DrawPoint[stroke.Vertices.Length];
        for (int index = 0; index < stroke.Vertices.Length; index++)
        {
            DrawPoint point = stroke.Vertices[index];
            vertices[index] = new DrawPoint(
                (point.X * coordinateScale) - left,
                (point.Y * coordinateScale) - top);
        }

        return new DrawStrokeRenderMesh(
            new DrawTriangleMesh(vertices, stroke.Indices),
            stroke.Vertices,
            left,
            top);
    }

    private static IReadOnlyList<DrawStrokeContour> CreateContours(
        DrawCommand command,
        float coordinateScale)
    {
        return command.Kind switch
        {
            DrawCommandKind.DrawLine =>
            [new DrawStrokeContour([command.Position, command.EndPoint], false)],
            DrawCommandKind.DrawRectangle =>
            [CreateRectangle(command.Rect)],
            DrawCommandKind.DrawEllipse =>
            [CreateEllipse(command.Rect, coordinateScale)],
            DrawCommandKind.DrawPath or
            DrawCommandKind.DrawRoundedRectangle => CreatePath(command, coordinateScale),
            _ => throw new ArgumentException(
                $"Unsupported stroke command: {command.Kind}.",
                nameof(command))
        };
    }

    private static DrawStrokeContour CreateRectangle(DrawRect rect) =>
        new(
            [
                new DrawPoint(rect.X, rect.Y),
                new DrawPoint(rect.Right, rect.Y),
                new DrawPoint(rect.Right, rect.Bottom),
                new DrawPoint(rect.X, rect.Bottom)
            ],
            true);

    private static DrawStrokeContour CreateEllipse(
        DrawRect bounds,
        float coordinateScale)
    {
        int segmentCount = Math.Clamp(
            (int)MathF.Ceiling(
                MathF.PI * MathF.Max(bounds.Width, bounds.Height) * coordinateScale / 2),
            24,
            2048);
        DrawPoint[] points = new DrawPoint[segmentCount];
        float radiusX = bounds.Width / 2;
        float radiusY = bounds.Height / 2;
        float centerX = bounds.X + radiusX;
        float centerY = bounds.Y + radiusY;
        for (int index = 0; index < points.Length; index++)
        {
            float angle = MathF.Tau * index / points.Length;
            points[index] = new DrawPoint(
                centerX + (MathF.Cos(angle) * radiusX),
                centerY + (MathF.Sin(angle) * radiusY));
        }

        return new DrawStrokeContour(points, true);
    }

    private static IReadOnlyList<DrawStrokeContour> CreatePath(
        DrawCommand command,
        float coordinateScale)
    {
        DrawPath path = command.Path ??
            throw new ArgumentException("A path stroke requires a path.", nameof(command));
        float scaleX = command.SourceRect.Width > 0
            ? command.Rect.Width / command.SourceRect.Width
            : 1;
        float scaleY = command.SourceRect.Height > 0
            ? command.Rect.Height / command.SourceRect.Height
            : 1;
        float maximumScale = MathF.Max(MathF.Abs(scaleX), MathF.Abs(scaleY));
        float tolerance = 0.2f /
            MathF.Max(coordinateScale * MathF.Max(maximumScale, 0.0001f), 0.0001f);
        IReadOnlyList<DrawStrokeContour> flattened = DrawPathFlattener.FlattenStroke(path, tolerance);
        DrawStrokeContour[] mapped = new DrawStrokeContour[flattened.Count];
        for (int contourIndex = 0; contourIndex < flattened.Count; contourIndex++)
        {
            DrawStrokeContour contour = flattened[contourIndex];
            DrawPoint[] points = new DrawPoint[contour.Points.Count];
            for (int pointIndex = 0; pointIndex < points.Length; pointIndex++)
            {
                DrawPoint point = contour.Points[pointIndex];
                points[pointIndex] = new DrawPoint(
                    command.Rect.X + ((point.X - command.SourceRect.X) * scaleX),
                    command.Rect.Y + ((point.Y - command.SourceRect.Y) * scaleY));
            }
            mapped[contourIndex] = new DrawStrokeContour(points, contour.IsClosed);
        }

        return mapped;
    }
}
