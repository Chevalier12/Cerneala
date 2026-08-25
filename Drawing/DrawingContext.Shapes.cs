namespace Cerneala.Drawing;

public sealed partial class DrawingContext
{
    public void FillRoundedRectangle(
        DrawRect bounds,
        DrawCornerRadius cornerRadius,
        Color color) =>
        _commands.Add(DrawCommand.FillRoundedRectangle(bounds, cornerRadius, color));

    public void FillRoundedRectangle(
        DrawRect bounds,
        DrawCornerRadius cornerRadius,
        IDrawBrush brush) =>
        _commands.Add(DrawCommand.FillRoundedRectangle(bounds, cornerRadius, brush));

    public void DrawRoundedRectangle(
        DrawRect bounds,
        DrawCornerRadius cornerRadius,
        Color color,
        float thickness) =>
        _commands.Add(DrawCommand.DrawRoundedRectangle(
            bounds,
            cornerRadius,
            color,
            thickness));

    public void DrawRoundedRectangle(
        DrawRect bounds,
        DrawCornerRadius cornerRadius,
        IDrawBrush brush,
        float thickness) =>
        _commands.Add(DrawCommand.DrawRoundedRectangle(
            bounds,
            cornerRadius,
            brush,
            thickness));

    public void DrawRoundedRectangle(
        DrawRect bounds,
        DrawCornerRadius cornerRadius,
        DrawPen pen) =>
        _commands.Add(DrawCommand.DrawRoundedRectangle(bounds, cornerRadius, pen));

    public void FillPolygon(
        IEnumerable<DrawPoint> points,
        Color color,
        DrawFillRule fillRule = DrawFillRule.NonZero) =>
        FillPath(DrawPathFactory.Polygon(points), color, fillRule);

    public void FillPolygon(
        IEnumerable<DrawPoint> points,
        IDrawBrush brush,
        DrawFillRule fillRule = DrawFillRule.NonZero) =>
        FillPath(DrawPathFactory.Polygon(points), brush, fillRule);

    public void DrawPolygon(
        IEnumerable<DrawPoint> points,
        Color color,
        float thickness) =>
        DrawPath(DrawPathFactory.Polygon(points), new DrawPen(new ColorBrush(color), thickness));

    public void DrawPolygon(
        IEnumerable<DrawPoint> points,
        IDrawBrush brush,
        float thickness) =>
        DrawPath(DrawPathFactory.Polygon(points), new DrawPen(brush, thickness));

    public void DrawPolygon(IEnumerable<DrawPoint> points, DrawPen pen) =>
        DrawPath(DrawPathFactory.Polygon(points), pen);

    public void DrawPolyline(
        IEnumerable<DrawPoint> points,
        Color color,
        float thickness) =>
        DrawPath(DrawPathFactory.Polyline(points), new DrawPen(new ColorBrush(color), thickness));

    public void DrawPolyline(
        IEnumerable<DrawPoint> points,
        IDrawBrush brush,
        float thickness) =>
        DrawPath(DrawPathFactory.Polyline(points), new DrawPen(brush, thickness));

    public void DrawPolyline(IEnumerable<DrawPoint> points, DrawPen pen) =>
        DrawPath(DrawPathFactory.Polyline(points), pen);

    public void DrawArc(
        DrawPoint center,
        float radiusX,
        float radiusY,
        float startAngle,
        float sweepAngle,
        Color color,
        float thickness,
        DrawArcDirection direction = DrawArcDirection.Clockwise) =>
        DrawPath(
            DrawPathFactory.Arc(center, radiusX, radiusY, startAngle, sweepAngle, direction),
            new DrawPen(new ColorBrush(color), thickness));

    public void DrawArc(
        DrawPoint center,
        float radiusX,
        float radiusY,
        float startAngle,
        float sweepAngle,
        IDrawBrush brush,
        float thickness,
        DrawArcDirection direction = DrawArcDirection.Clockwise) =>
        DrawPath(
            DrawPathFactory.Arc(center, radiusX, radiusY, startAngle, sweepAngle, direction),
            new DrawPen(brush, thickness));

    public void DrawArc(
        DrawPoint center,
        float radiusX,
        float radiusY,
        float startAngle,
        float sweepAngle,
        DrawPen pen,
        DrawArcDirection direction = DrawArcDirection.Clockwise) =>
        DrawPath(
            DrawPathFactory.Arc(center, radiusX, radiusY, startAngle, sweepAngle, direction),
            pen);

    public void FillPie(
        DrawPoint center,
        float radiusX,
        float radiusY,
        float startAngle,
        float sweepAngle,
        Color color,
        DrawArcDirection direction = DrawArcDirection.Clockwise) =>
        FillPath(DrawPathFactory.Pie(center, radiusX, radiusY, startAngle, sweepAngle, direction), color);

    public void FillPie(
        DrawPoint center,
        float radiusX,
        float radiusY,
        float startAngle,
        float sweepAngle,
        IDrawBrush brush,
        DrawArcDirection direction = DrawArcDirection.Clockwise) =>
        FillPath(DrawPathFactory.Pie(center, radiusX, radiusY, startAngle, sweepAngle, direction), brush);

    public void DrawPie(
        DrawPoint center,
        float radiusX,
        float radiusY,
        float startAngle,
        float sweepAngle,
        Color color,
        float thickness,
        DrawArcDirection direction = DrawArcDirection.Clockwise) =>
        DrawPie(
            center,
            radiusX,
            radiusY,
            startAngle,
            sweepAngle,
            new DrawPen(new ColorBrush(color), thickness),
            direction);

    public void DrawPie(
        DrawPoint center,
        float radiusX,
        float radiusY,
        float startAngle,
        float sweepAngle,
        IDrawBrush brush,
        float thickness,
        DrawArcDirection direction = DrawArcDirection.Clockwise) =>
        DrawPie(
            center,
            radiusX,
            radiusY,
            startAngle,
            sweepAngle,
            new DrawPen(brush, thickness),
            direction);

    public void DrawPie(
        DrawPoint center,
        float radiusX,
        float radiusY,
        float startAngle,
        float sweepAngle,
        DrawPen pen,
        DrawArcDirection direction = DrawArcDirection.Clockwise) =>
        DrawPath(DrawPathFactory.Pie(center, radiusX, radiusY, startAngle, sweepAngle, direction), pen);

    public void FillChord(
        DrawPoint center,
        float radiusX,
        float radiusY,
        float startAngle,
        float sweepAngle,
        Color color,
        DrawArcDirection direction = DrawArcDirection.Clockwise) =>
        FillPath(DrawPathFactory.Chord(center, radiusX, radiusY, startAngle, sweepAngle, direction), color);

    public void FillChord(
        DrawPoint center,
        float radiusX,
        float radiusY,
        float startAngle,
        float sweepAngle,
        IDrawBrush brush,
        DrawArcDirection direction = DrawArcDirection.Clockwise) =>
        FillPath(DrawPathFactory.Chord(center, radiusX, radiusY, startAngle, sweepAngle, direction), brush);

    public void DrawChord(
        DrawPoint center,
        float radiusX,
        float radiusY,
        float startAngle,
        float sweepAngle,
        Color color,
        float thickness,
        DrawArcDirection direction = DrawArcDirection.Clockwise) =>
        DrawChord(
            center,
            radiusX,
            radiusY,
            startAngle,
            sweepAngle,
            new DrawPen(new ColorBrush(color), thickness),
            direction);

    public void DrawChord(
        DrawPoint center,
        float radiusX,
        float radiusY,
        float startAngle,
        float sweepAngle,
        IDrawBrush brush,
        float thickness,
        DrawArcDirection direction = DrawArcDirection.Clockwise) =>
        DrawChord(
            center,
            radiusX,
            radiusY,
            startAngle,
            sweepAngle,
            new DrawPen(brush, thickness),
            direction);

    public void DrawChord(
        DrawPoint center,
        float radiusX,
        float radiusY,
        float startAngle,
        float sweepAngle,
        DrawPen pen,
        DrawArcDirection direction = DrawArcDirection.Clockwise) =>
        DrawPath(DrawPathFactory.Chord(center, radiusX, radiusY, startAngle, sweepAngle, direction), pen);

    public void DrawPoint(DrawPoint point, Color color, float diameter = 1) =>
        FillCircle(point, diameter / 2, color);

    public void DrawPoint(DrawPoint point, IDrawBrush brush, float diameter = 1) =>
        FillCircle(point, diameter / 2, brush);

    public void FillCircle(DrawPoint center, float radius, Color color) =>
        FillEllipse(CircleBounds(center, radius), color);

    public void FillCircle(DrawPoint center, float radius, IDrawBrush brush) =>
        FillEllipse(CircleBounds(center, radius), brush);

    public void FillTriangle(
        DrawPoint first,
        DrawPoint second,
        DrawPoint third,
        Color color,
        DrawFillRule fillRule = DrawFillRule.NonZero) =>
        FillPolygon([first, second, third], color, fillRule);

    public void FillTriangle(
        DrawPoint first,
        DrawPoint second,
        DrawPoint third,
        IDrawBrush brush,
        DrawFillRule fillRule = DrawFillRule.NonZero) =>
        FillPolygon([first, second, third], brush, fillRule);

    public void DrawTriangle(
        DrawPoint first,
        DrawPoint second,
        DrawPoint third,
        Color color,
        float thickness) =>
        DrawTriangle(
            first,
            second,
            third,
            new DrawPen(new ColorBrush(color), thickness));

    public void DrawTriangle(
        DrawPoint first,
        DrawPoint second,
        DrawPoint third,
        IDrawBrush brush,
        float thickness) =>
        DrawTriangle(first, second, third, new DrawPen(brush, thickness));

    public void DrawTriangle(
        DrawPoint first,
        DrawPoint second,
        DrawPoint third,
        DrawPen pen) =>
        DrawPolygon([first, second, third], pen);

    public void FillRegularPolygon(
        DrawPoint center,
        float radius,
        int sideCount,
        Color color,
        float rotation = 0) =>
        FillPath(DrawPathFactory.RegularPolygon(center, radius, sideCount, rotation), color);

    public void FillRegularPolygon(
        DrawPoint center,
        float radius,
        int sideCount,
        IDrawBrush brush,
        float rotation = 0) =>
        FillPath(DrawPathFactory.RegularPolygon(center, radius, sideCount, rotation), brush);

    public void DrawRegularPolygon(
        DrawPoint center,
        float radius,
        int sideCount,
        Color color,
        float thickness,
        float rotation = 0) =>
        DrawRegularPolygon(
            center,
            radius,
            sideCount,
            new DrawPen(new ColorBrush(color), thickness),
            rotation);

    public void DrawRegularPolygon(
        DrawPoint center,
        float radius,
        int sideCount,
        IDrawBrush brush,
        float thickness,
        float rotation = 0) =>
        DrawRegularPolygon(
            center,
            radius,
            sideCount,
            new DrawPen(brush, thickness),
            rotation);

    public void DrawRegularPolygon(
        DrawPoint center,
        float radius,
        int sideCount,
        DrawPen pen,
        float rotation = 0) =>
        DrawPath(DrawPathFactory.RegularPolygon(center, radius, sideCount, rotation), pen);

    public void FillStar(
        DrawPoint center,
        float outerRadius,
        float innerRadius,
        int pointCount,
        Color color,
        float rotation = 0,
        DrawFillRule fillRule = DrawFillRule.EvenOdd) =>
        FillPath(
            DrawPathFactory.Star(center, outerRadius, innerRadius, pointCount, rotation),
            color,
            fillRule);

    public void FillStar(
        DrawPoint center,
        float outerRadius,
        float innerRadius,
        int pointCount,
        IDrawBrush brush,
        float rotation = 0,
        DrawFillRule fillRule = DrawFillRule.EvenOdd) =>
        FillPath(
            DrawPathFactory.Star(center, outerRadius, innerRadius, pointCount, rotation),
            brush,
            fillRule);

    public void DrawStar(
        DrawPoint center,
        float outerRadius,
        float innerRadius,
        int pointCount,
        Color color,
        float thickness,
        float rotation = 0) =>
        DrawStar(
            center,
            outerRadius,
            innerRadius,
            pointCount,
            new DrawPen(new ColorBrush(color), thickness),
            rotation);

    public void DrawStar(
        DrawPoint center,
        float outerRadius,
        float innerRadius,
        int pointCount,
        IDrawBrush brush,
        float thickness,
        float rotation = 0) =>
        DrawStar(
            center,
            outerRadius,
            innerRadius,
            pointCount,
            new DrawPen(brush, thickness),
            rotation);

    public void DrawStar(
        DrawPoint center,
        float outerRadius,
        float innerRadius,
        int pointCount,
        DrawPen pen,
        float rotation = 0) =>
        DrawPath(DrawPathFactory.Star(center, outerRadius, innerRadius, pointCount, rotation), pen);

    private static DrawRect CircleBounds(DrawPoint center, float radius)
    {
        if (!float.IsFinite(radius) || radius <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(radius));
        }
        return new DrawRect(
            center.X - radius,
            center.Y - radius,
            radius * 2,
            radius * 2);
    }

    private sealed record ColorBrush(Color Color) : IDrawBrush
    {
        public DrawBrushKind Kind => DrawBrushKind.SolidColor;

        public float Opacity => Color.A / 255f;

        public Color? SolidColor => Color;

        public DrawBrushDescriptor CreateDescriptor() =>
            new SolidDrawBrushDescriptor(Color, 1);
    }
}
