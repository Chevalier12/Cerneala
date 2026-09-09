using Cerneala.Drawing;
using Cerneala.UI.Controls.Shapes;
using Cerneala.UI.Core;
using Cerneala.UI.Elements;
using Cerneala.UI.Invalidation;
using Cerneala.UI.Layout;
using Cerneala.UI.Media;
using PathShape = Cerneala.UI.Controls.Shapes.Path;
using RectangleShape = Cerneala.UI.Controls.Shapes.Rectangle;

namespace Cerneala.Tests.UI.Controls.Shapes;

public sealed class ShapePrimitiveTests
{
    [Fact]
    public void LineUsesItsTwoEndpointsAndRebuildsOnlyWhenTheyChange()
    {
        Line line = new()
        {
            StartPoint = new DrawPoint(0, 0), EndPoint = new DrawPoint(30, 20),
            Stroke = new SolidColorBrush(Color.Black), StrokeThickness = 2
        };
        DrawPath first = StrokePath(Record(line));
        Assert.Equal(new DrawPoint(0, 0), first.Contours[0].StartPoint);
        Assert.Equal(new DrawPoint(30, 20), first.Contours[0].Segments[1].EndPoint);
        Assert.Same(first, StrokePath(Record(line)));

        line.EndPoint = new DrawPoint(50, 40);
        Assert.True(line.DirtyState.Has(InvalidationFlags.Measure));
        Assert.NotSame(first, StrokePath(Record(line)));
        Assert.Equal(new LayoutSize(52, 42), line.Measure(new MeasureContext(new LayoutSize(100, 100))));
    }

    [Fact]
    public void PolylineIsOpenAndPolygonClosesItsSharedFillAndStrokePath()
    {
        DrawPoint[] points = [new(0, 0), new(30, 0), new(30, 20)];
        Polyline polyline = new() { Points = points, Stroke = new SolidColorBrush(Color.Black) };
        Polygon polygon = new()
        {
            Points = points, Fill = new SolidColorBrush(Color.White),
            Stroke = new SolidColorBrush(Color.Black), FillRule = DrawFillRule.EvenOdd
        };

        Assert.False(Assert.Single(StrokePath(Record(polyline)).Contours).IsClosed);
        DrawCommand[] commands = PaintCommands(Record(polygon));
        Assert.Equal([DrawCommandKind.FillPath, DrawCommandKind.DrawPath], commands.Select(command => command.Kind));
        Assert.Same(commands[0].Path, commands[1].Path);
        Assert.True(Assert.Single(commands[0].Path!.Contours).IsClosed);
        Assert.Equal(DrawFillRule.EvenOdd, commands[0].FillRule);
        Assert.Same(commands[0].Path, StrokePath(Record(polygon)));
    }

    [Fact]
    public void PointAssignmentsSnapshotMutableInputsIncludingSetValue()
    {
        DrawPoint[] source = [new(0, 0), new(10, 0), new(10, 10)];
        Polyline polyline = new() { Points = source };
        Polygon polygon = new();
        polygon.SetValue(Polygon.PointsProperty, (IReadOnlyList<DrawPoint>)source, UiPropertyValueSource.Local);
        source[0] = new DrawPoint(99, 99);

        Assert.Equal(new DrawPoint(0, 0), polyline.Points[0]);
        Assert.Equal(new DrawPoint(0, 0), polygon.Points[0]);
        Assert.Throws<NotSupportedException>(() => ((IList<DrawPoint>)polyline.Points)[0] = source[0]);
        Assert.Throws<NotSupportedException>(() => ((IList<DrawPoint>)polygon.Points)[0] = source[0]);
        Assert.Throws<ArgumentNullException>(() => polyline.Points = null!);
        Assert.Throws<ArgumentNullException>(() => polygon.Points = null!);
    }

    [Fact]
    public void InsufficientPointsProduceNoGeometry()
    {
        Assert.Empty(Record(new Polyline { Stroke = new SolidColorBrush(Color.Black) }));
        Assert.Empty(Record(new Polyline { Points = [new(1, 2)], Stroke = new SolidColorBrush(Color.Black) }));
        Assert.Empty(Record(new Polygon { Points = [new(1, 2), new(3, 4)], Fill = new SolidColorBrush(Color.White) }));
    }

    [Fact]
    public void ReplacingPointsInvalidatesLayoutAndReplacesRetainedGeometry()
    {
        Polyline shape = new()
        {
            Points = [new(0, 0), new(10, 10)], Stroke = new SolidColorBrush(Color.Black)
        };
        DrawPath first = StrokePath(Record(shape));
        shape.Points = [new(0, 0), new(20, 30)];
        Assert.True(shape.DirtyState.Has(InvalidationFlags.Measure));
        Assert.True(shape.DirtyState.Has(InvalidationFlags.Render));
        Assert.NotSame(first, StrokePath(Record(shape)));
    }

    [Fact]
    public void RoundedRectangleUsesFourEllipticalArcsAndClampsEffectiveRadii()
    {
        RectangleShape rectangle = new()
        {
            RadiusX = 50, RadiusY = 3,
            Fill = new SolidColorBrush(Color.White), Stroke = new SolidColorBrush(Color.Black)
        };
        DrawCommand[] commands = PaintCommands(Record(rectangle, new LayoutRect(0, 0, 40, 20)));
        Assert.Equal(2, commands.Length);
        Assert.Same(commands[0].Path, commands[1].Path);
        DrawPath path = commands[0].Path!;
        DrawPathContour contour = Assert.Single(path.Contours);
        DrawPathSegment[] arcs = contour.Segments.Where(segment => segment.Kind == DrawPathSegmentKind.Arc).ToArray();
        Assert.Equal(4, arcs.Length);
        Assert.All(arcs, arc => { Assert.Equal(20, arc.RadiusX); Assert.Equal(3, arc.RadiusY); });
        Assert.True(contour.IsClosed);
        Assert.Equal(50, rectangle.RadiusX);
        Assert.Same(path, StrokePath(Record(rectangle, new LayoutRect(10, 20, 40, 20))));
        Assert.NotSame(path, StrokePath(Record(rectangle, new LayoutRect(10, 20, 60, 20))));
    }

    [Theory]
    [InlineData(0, 10)]
    [InlineData(10, 0)]
    [InlineData(0, 0)]
    public void AZeroCornerRadiusKeepsTheNativeSquareRectangle(float radiusX, float radiusY)
    {
        RectangleShape rectangle = new()
        {
            RadiusX = radiusX, RadiusY = radiusY, Fill = new SolidColorBrush(Color.White)
        };
        Assert.Equal(DrawCommandKind.FillRectangle, Assert.Single(Record(rectangle)).Kind);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(float.NaN)]
    [InlineData(float.PositiveInfinity)]
    public void CornerRadiiRejectInvalidValues(float value)
    {
        RectangleShape rectangle = new();
        Assert.ThrowsAny<ArgumentException>(() => rectangle.RadiusX = value);
        Assert.ThrowsAny<ArgumentException>(() => rectangle.RadiusY = value);
    }

    [Fact]
    public void RichPathPreservesCurvesArcsContoursAndFillRule()
    {
        PathGeometry geometry = PathGeometry.Parse(
            "M0 0 Q10 0 10 10 C10 20 20 20 20 10 A5 3 20 0 1 30 10 Z M2 2 L4 2 L4 4 Z");
        PathShape shape = new()
        {
            Data = geometry, Fill = new SolidColorBrush(Color.White),
            Stroke = new SolidColorBrush(Color.Black), FillRule = DrawFillRule.EvenOdd
        };
        DrawCommand[] commands = PaintCommands(Record(shape));
        Assert.Equal(2, commands.Length);
        Assert.All(commands, command => Assert.Same(geometry.Path, command.Path));
        Assert.Equal(DrawFillRule.EvenOdd, commands[0].FillRule);
        Assert.Equal(2, geometry.Path!.Contours.Count);
        Assert.Contains(geometry.Path.Contours[0].Segments, segment => segment.Kind == DrawPathSegmentKind.Quadratic);
        Assert.Contains(geometry.Path.Contours[0].Segments, segment => segment.Kind == DrawPathSegmentKind.Cubic);
        Assert.Contains(geometry.Path.Contours[0].Segments, segment => segment.Kind == DrawPathSegmentKind.Arc);
        Assert.All(geometry.Path.Contours, contour => Assert.True(contour.IsClosed));
        Assert.Same(geometry.Path, StrokePath(Record(shape)));
    }

    [Fact]
    public void FillAndStrokeAreIndependentAndUndefinedFillRulesAreRejected()
    {
        PathShape shape = new() { Data = PathGeometry.Parse("M0 0L20 0L20 20Z"), Fill = new SolidColorBrush(Color.White) };
        Assert.Equal(DrawCommandKind.FillPath, Assert.Single(Record(shape)).Kind);
        shape.Fill = null;
        shape.Stroke = new SolidColorBrush(Color.Black);
        Assert.Equal(DrawCommandKind.DrawPath, Assert.Single(Record(shape)).Kind);
        shape.StrokeThickness = 0;
        Assert.Empty(Record(shape));
        Assert.ThrowsAny<ArgumentException>(() => shape.FillRule = (DrawFillRule)99);
    }

    [Fact]
    public void ExplicitGeometryOverridesPrimitiveSpecificData()
    {
        RectangleGeometry geometry = new(new DrawRect(1, 2, 3, 4));
        Shape[] shapes =
        [
            new Line(), new Polyline(), new Polygon(),
            new RectangleShape { RadiusX = 5, RadiusY = 6 },
            new PathShape { Data = PathGeometry.Parse("M0 0L10 10") }
        ];
        foreach (Shape shape in shapes)
        {
            shape.Geometry = geometry;
            shape.Fill = new SolidColorBrush(Color.White);
            DrawCommand command = Assert.Single(Record(shape));
            Assert.Equal(DrawCommandKind.FillRectangle, command.Kind);
            Assert.Equal(geometry.Bounds, command.Rect);
        }
    }

    private static DrawCommandList Record(Shape shape, LayoutRect? bounds = null)
    {
        UIRoot root = shape.Root ?? new UIRoot();
        if (shape.Root is null)
        {
            root.VisualChildren.Add(shape);
        }
        root.ProcessFrame();
        shape.Arrange(new ArrangeContext(bounds ?? new LayoutRect(0, 0, 100, 100)));
        root.Invalidate(InvalidationFlags.Render | InvalidationFlags.Subtree, "shape-primitives");
        root.ProcessFrame();
        return root.RetainedRenderer.Commit(root);
    }

    private static DrawCommand[] PaintCommands(DrawCommandList commands) => commands
        .Where(command => command.Kind is DrawCommandKind.FillPath or DrawCommandKind.DrawPath).ToArray();

    private static DrawPath StrokePath(DrawCommandList commands) => Assert.Single(commands
        .Where(command => command.Kind == DrawCommandKind.DrawPath)).Path!;
}
