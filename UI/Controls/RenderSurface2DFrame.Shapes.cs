using Cerneala.Drawing;

namespace Cerneala.UI.Controls;

public sealed partial class RenderSurface2DFrame
{
    public void FillRoundedRectangle(DrawRect bounds, DrawCornerRadius radius, Color color)
    {
        EnsureActive();
        drawingContext.FillRoundedRectangle(bounds, radius, color);
    }

    public void FillRoundedRectangle(DrawRect bounds, DrawCornerRadius radius, IDrawBrush brush)
    {
        EnsureActive();
        drawingContext.FillRoundedRectangle(bounds, radius, brush);
    }

    public void DrawRoundedRectangle(DrawRect bounds, DrawCornerRadius radius, Color color, float thickness)
    {
        EnsureActive();
        drawingContext.DrawRoundedRectangle(bounds, radius, color, thickness);
    }

    public void DrawRoundedRectangle(DrawRect bounds, DrawCornerRadius radius, IDrawBrush brush, float thickness)
    {
        EnsureActive();
        drawingContext.DrawRoundedRectangle(bounds, radius, brush, thickness);
    }

    public void DrawRoundedRectangle(DrawRect bounds, DrawCornerRadius radius, DrawPen pen)
    {
        EnsureActive();
        drawingContext.DrawRoundedRectangle(bounds, radius, pen);
    }

    public void FillPolygon(IEnumerable<DrawPoint> points, Color color, DrawFillRule fillRule = DrawFillRule.NonZero)
    {
        EnsureActive();
        drawingContext.FillPolygon(points, color, fillRule);
    }

    public void FillPolygon(IEnumerable<DrawPoint> points, IDrawBrush brush, DrawFillRule fillRule = DrawFillRule.NonZero)
    {
        EnsureActive();
        drawingContext.FillPolygon(points, brush, fillRule);
    }

    public void DrawPolygon(IEnumerable<DrawPoint> points, Color color, float thickness)
    {
        EnsureActive();
        drawingContext.DrawPolygon(points, color, thickness);
    }

    public void DrawPolygon(IEnumerable<DrawPoint> points, IDrawBrush brush, float thickness)
    {
        EnsureActive();
        drawingContext.DrawPolygon(points, brush, thickness);
    }

    public void DrawPolygon(IEnumerable<DrawPoint> points, DrawPen pen)
    {
        EnsureActive();
        drawingContext.DrawPolygon(points, pen);
    }

    public void DrawPolyline(IEnumerable<DrawPoint> points, Color color, float thickness)
    {
        EnsureActive();
        drawingContext.DrawPolyline(points, color, thickness);
    }

    public void DrawPolyline(IEnumerable<DrawPoint> points, IDrawBrush brush, float thickness)
    {
        EnsureActive();
        drawingContext.DrawPolyline(points, brush, thickness);
    }

    public void DrawPolyline(IEnumerable<DrawPoint> points, DrawPen pen)
    {
        EnsureActive();
        drawingContext.DrawPolyline(points, pen);
    }

    public void DrawArc(DrawPoint center, float radiusX, float radiusY, float startAngle, float sweepAngle, Color color, float thickness, DrawArcDirection direction = DrawArcDirection.Clockwise)
    {
        EnsureActive();
        drawingContext.DrawArc(center, radiusX, radiusY, startAngle, sweepAngle, color, thickness, direction);
    }

    public void DrawArc(DrawPoint center, float radiusX, float radiusY, float startAngle, float sweepAngle, IDrawBrush brush, float thickness, DrawArcDirection direction = DrawArcDirection.Clockwise)
    {
        EnsureActive();
        drawingContext.DrawArc(center, radiusX, radiusY, startAngle, sweepAngle, brush, thickness, direction);
    }

    public void DrawArc(DrawPoint center, float radiusX, float radiusY, float startAngle, float sweepAngle, DrawPen pen, DrawArcDirection direction = DrawArcDirection.Clockwise)
    {
        EnsureActive();
        drawingContext.DrawArc(center, radiusX, radiusY, startAngle, sweepAngle, pen, direction);
    }

    public void FillPie(DrawPoint center, float radiusX, float radiusY, float startAngle, float sweepAngle, Color color, DrawArcDirection direction = DrawArcDirection.Clockwise)
    {
        EnsureActive();
        drawingContext.FillPie(center, radiusX, radiusY, startAngle, sweepAngle, color, direction);
    }

    public void FillPie(DrawPoint center, float radiusX, float radiusY, float startAngle, float sweepAngle, IDrawBrush brush, DrawArcDirection direction = DrawArcDirection.Clockwise)
    {
        EnsureActive();
        drawingContext.FillPie(center, radiusX, radiusY, startAngle, sweepAngle, brush, direction);
    }

    public void DrawPie(DrawPoint center, float radiusX, float radiusY, float startAngle, float sweepAngle, DrawPen pen, DrawArcDirection direction = DrawArcDirection.Clockwise)
    {
        EnsureActive();
        drawingContext.DrawPie(center, radiusX, radiusY, startAngle, sweepAngle, pen, direction);
    }

    public void DrawPie(DrawPoint center, float radiusX, float radiusY, float startAngle, float sweepAngle, Color color, float thickness, DrawArcDirection direction = DrawArcDirection.Clockwise)
    {
        EnsureActive();
        drawingContext.DrawPie(center, radiusX, radiusY, startAngle, sweepAngle, color, thickness, direction);
    }

    public void DrawPie(DrawPoint center, float radiusX, float radiusY, float startAngle, float sweepAngle, IDrawBrush brush, float thickness, DrawArcDirection direction = DrawArcDirection.Clockwise)
    {
        EnsureActive();
        drawingContext.DrawPie(center, radiusX, radiusY, startAngle, sweepAngle, brush, thickness, direction);
    }

    public void FillChord(DrawPoint center, float radiusX, float radiusY, float startAngle, float sweepAngle, Color color, DrawArcDirection direction = DrawArcDirection.Clockwise)
    {
        EnsureActive();
        drawingContext.FillChord(center, radiusX, radiusY, startAngle, sweepAngle, color, direction);
    }

    public void FillChord(DrawPoint center, float radiusX, float radiusY, float startAngle, float sweepAngle, IDrawBrush brush, DrawArcDirection direction = DrawArcDirection.Clockwise)
    {
        EnsureActive();
        drawingContext.FillChord(center, radiusX, radiusY, startAngle, sweepAngle, brush, direction);
    }

    public void DrawChord(DrawPoint center, float radiusX, float radiusY, float startAngle, float sweepAngle, DrawPen pen, DrawArcDirection direction = DrawArcDirection.Clockwise)
    {
        EnsureActive();
        drawingContext.DrawChord(center, radiusX, radiusY, startAngle, sweepAngle, pen, direction);
    }

    public void DrawChord(DrawPoint center, float radiusX, float radiusY, float startAngle, float sweepAngle, Color color, float thickness, DrawArcDirection direction = DrawArcDirection.Clockwise)
    {
        EnsureActive();
        drawingContext.DrawChord(center, radiusX, radiusY, startAngle, sweepAngle, color, thickness, direction);
    }

    public void DrawChord(DrawPoint center, float radiusX, float radiusY, float startAngle, float sweepAngle, IDrawBrush brush, float thickness, DrawArcDirection direction = DrawArcDirection.Clockwise)
    {
        EnsureActive();
        drawingContext.DrawChord(center, radiusX, radiusY, startAngle, sweepAngle, brush, thickness, direction);
    }

    public void DrawPoint(DrawPoint point, Color color, float diameter = 1)
    {
        EnsureActive();
        drawingContext.DrawPoint(point, color, diameter);
    }

    public void DrawPoint(DrawPoint point, IDrawBrush brush, float diameter = 1)
    {
        EnsureActive();
        drawingContext.DrawPoint(point, brush, diameter);
    }

    public void FillCircle(DrawPoint center, float radius, Color color)
    {
        EnsureActive();
        drawingContext.FillCircle(center, radius, color);
    }

    public void FillCircle(DrawPoint center, float radius, IDrawBrush brush)
    {
        EnsureActive();
        drawingContext.FillCircle(center, radius, brush);
    }

    public void FillTriangle(DrawPoint first, DrawPoint second, DrawPoint third, Color color, DrawFillRule fillRule = DrawFillRule.NonZero)
    {
        EnsureActive();
        drawingContext.FillTriangle(first, second, third, color, fillRule);
    }

    public void FillTriangle(DrawPoint first, DrawPoint second, DrawPoint third, IDrawBrush brush, DrawFillRule fillRule = DrawFillRule.NonZero)
    {
        EnsureActive();
        drawingContext.FillTriangle(first, second, third, brush, fillRule);
    }

    public void DrawTriangle(DrawPoint first, DrawPoint second, DrawPoint third, DrawPen pen)
    {
        EnsureActive();
        drawingContext.DrawTriangle(first, second, third, pen);
    }

    public void DrawTriangle(DrawPoint first, DrawPoint second, DrawPoint third, Color color, float thickness)
    {
        EnsureActive();
        drawingContext.DrawTriangle(first, second, third, color, thickness);
    }

    public void DrawTriangle(DrawPoint first, DrawPoint second, DrawPoint third, IDrawBrush brush, float thickness)
    {
        EnsureActive();
        drawingContext.DrawTriangle(first, second, third, brush, thickness);
    }

    public void FillRegularPolygon(DrawPoint center, float radius, int sideCount, Color color, float rotation = 0)
    {
        EnsureActive();
        drawingContext.FillRegularPolygon(center, radius, sideCount, color, rotation);
    }

    public void FillRegularPolygon(DrawPoint center, float radius, int sideCount, IDrawBrush brush, float rotation = 0)
    {
        EnsureActive();
        drawingContext.FillRegularPolygon(center, radius, sideCount, brush, rotation);
    }

    public void DrawRegularPolygon(DrawPoint center, float radius, int sideCount, DrawPen pen, float rotation = 0)
    {
        EnsureActive();
        drawingContext.DrawRegularPolygon(center, radius, sideCount, pen, rotation);
    }

    public void DrawRegularPolygon(DrawPoint center, float radius, int sideCount, Color color, float thickness, float rotation = 0)
    {
        EnsureActive();
        drawingContext.DrawRegularPolygon(center, radius, sideCount, color, thickness, rotation);
    }

    public void DrawRegularPolygon(DrawPoint center, float radius, int sideCount, IDrawBrush brush, float thickness, float rotation = 0)
    {
        EnsureActive();
        drawingContext.DrawRegularPolygon(center, radius, sideCount, brush, thickness, rotation);
    }

    public void FillStar(DrawPoint center, float outerRadius, float innerRadius, int pointCount, Color color, float rotation = 0, DrawFillRule fillRule = DrawFillRule.EvenOdd)
    {
        EnsureActive();
        drawingContext.FillStar(center, outerRadius, innerRadius, pointCount, color, rotation, fillRule);
    }

    public void FillStar(DrawPoint center, float outerRadius, float innerRadius, int pointCount, IDrawBrush brush, float rotation = 0, DrawFillRule fillRule = DrawFillRule.EvenOdd)
    {
        EnsureActive();
        drawingContext.FillStar(center, outerRadius, innerRadius, pointCount, brush, rotation, fillRule);
    }

    public void DrawStar(DrawPoint center, float outerRadius, float innerRadius, int pointCount, DrawPen pen, float rotation = 0)
    {
        EnsureActive();
        drawingContext.DrawStar(center, outerRadius, innerRadius, pointCount, pen, rotation);
    }

    public void DrawStar(DrawPoint center, float outerRadius, float innerRadius, int pointCount, Color color, float thickness, float rotation = 0)
    {
        EnsureActive();
        drawingContext.DrawStar(center, outerRadius, innerRadius, pointCount, color, thickness, rotation);
    }

    public void DrawStar(DrawPoint center, float outerRadius, float innerRadius, int pointCount, IDrawBrush brush, float thickness, float rotation = 0)
    {
        EnsureActive();
        drawingContext.DrawStar(center, outerRadius, innerRadius, pointCount, brush, thickness, rotation);
    }
}
