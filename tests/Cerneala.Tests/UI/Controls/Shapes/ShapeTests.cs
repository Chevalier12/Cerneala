using Cerneala.Drawing;
using Cerneala.UI.Controls.Shapes;
using Cerneala.UI.Elements;
using Cerneala.UI.Invalidation;
using Cerneala.UI.Layout;
using Cerneala.UI.Media;
using RectangleShape = Cerneala.UI.Controls.Shapes.Rectangle;
using PathShape = Cerneala.UI.Controls.Shapes.Path;

namespace Cerneala.Tests.UI.Controls.Shapes;

public sealed class ShapeTests
{
    [Fact]
    public void RectangleShapeRendersFillAndStroke()
    {
        UIRoot root = new();
        RectangleShape rectangle = new()
        {
            Fill = new SolidColorBrush(Color.White),
            Stroke = new SolidColorBrush(Color.Black),
            StrokeThickness = 3
        };
        root.VisualChildren.Add(rectangle);
        root.ProcessFrame();
        rectangle.Arrange(new ArrangeContext(new LayoutRect(1, 2, 30, 20)));
        root.Invalidate(InvalidationFlags.Render | InvalidationFlags.Subtree, "test");
        root.ProcessFrame();

        DrawCommandList commands = root.RetainedRenderer.Commit(root);

        Assert.Equal(2, commands.Count);
        Assert.Equal(DrawCommandKind.FillRectangle, commands[0].Kind);
        Assert.Equal(DrawCommandKind.DrawRectangle, commands[1].Kind);
        Assert.Equal(new DrawRect(2.5f, 3.5f, 27, 17), commands[0].Rect);
        Assert.Equal(new DrawRect(2.5f, 3.5f, 27, 17), commands[1].Rect);
    }

    [Fact]
    public void EllipseShapeRendersFillAndStroke()
    {
        UIRoot root = new();
        Ellipse ellipse = new()
        {
            Fill = new SolidColorBrush(Color.White),
            Stroke = new SolidColorBrush(Color.Black),
            StrokeThickness = 3
        };
        root.VisualChildren.Add(ellipse);
        root.ProcessFrame();
        ellipse.Arrange(new ArrangeContext(new LayoutRect(1, 2, 30, 20)));
        root.Invalidate(InvalidationFlags.Render | InvalidationFlags.Subtree, "test");
        root.ProcessFrame();

        DrawCommandList commands = root.RetainedRenderer.Commit(root);

        Assert.Equal(2, commands.Count);
        Assert.Equal(DrawCommandKind.FillEllipse, commands[0].Kind);
        Assert.Equal(DrawCommandKind.DrawEllipse, commands[1].Kind);
        Assert.Equal(new DrawRect(2.5f, 3.5f, 27, 17), commands[0].Rect);
        Assert.Equal(new DrawRect(2.5f, 3.5f, 27, 17), commands[1].Rect);
    }

    [Fact]
    public void RectangleShapeAppliesRenderTransformToRenderedBounds()
    {
        UIRoot root = new();
        RectangleShape rectangle = new()
        {
            Fill = new SolidColorBrush(Color.White),
            RenderTransform = new Transform(Matrix3x2.CreateTranslation(10, 20))
        };
        root.VisualChildren.Add(rectangle);
        root.ProcessFrame();
        rectangle.Arrange(new ArrangeContext(new LayoutRect(1, 2, 30, 20)));
        root.Invalidate(InvalidationFlags.Render | InvalidationFlags.Subtree, "test");
        root.ProcessFrame();

        DrawCommandList commands = root.RetainedRenderer.Commit(root);

        int index = Assert.Single(Enumerable.Range(0, commands.Count)
            .Where(index => commands[index].Kind == DrawCommandKind.FillRectangle));
        DrawCommandStateAnalysis state = new DrawCommandStateAnalyzer().Analyze(commands);
        Assert.Equal(new DrawRect(11, 22, 30, 20), state.Entries[index].Bounds);
    }

    [Fact]
    public void PathShapeRendersOneNativeStrokePath()
    {
        UIRoot root = new();
        PathShape path = new()
        {
            Data = new PathGeometry([new DrawPoint(0, 0), new DrawPoint(10, 0), new DrawPoint(10, 10)]),
            Stroke = new SolidColorBrush(Color.Black),
            StrokeThickness = 2
        };
        root.VisualChildren.Add(path);
        root.ProcessFrame();
        path.Arrange(new ArrangeContext(new LayoutRect(0, 0, 20, 20)));
        root.Invalidate(InvalidationFlags.Render | InvalidationFlags.Subtree, "test");
        root.ProcessFrame();

        DrawCommandList commands = root.RetainedRenderer.Commit(root);

        DrawCommand command = Assert.Single(commands);
        Assert.Equal(DrawCommandKind.DrawPath, command.Kind);
        Assert.NotNull(command.Path);
        Assert.NotNull(command.Pen);
        Assert.Equal(2, command.Pen.Thickness);
    }

    [Fact]
    public void PathShapeTransformsTheCompleteNativeStrokeGeometry()
    {
        UIRoot root = new();
        PathShape path = new()
        {
            Data = new PathGeometry(
            [
                new DrawPoint(0, 0),
                new DrawPoint(10, 0),
                new DrawPoint(10, 10)
            ]),
            Stroke = new SolidColorBrush(Color.Black),
            StrokeThickness = 2,
            RenderTransform = new Transform(
                Matrix3x2.CreateTranslation(10, 20))
        };
        root.VisualChildren.Add(path);
        root.ProcessFrame();
        path.Arrange(new ArrangeContext(new LayoutRect(0, 0, 20, 20)));
        root.Invalidate(
            InvalidationFlags.Render | InvalidationFlags.Subtree,
            "test");
        root.ProcessFrame();

        DrawCommandList commands = root.RetainedRenderer.Commit(root);
        int index = Assert.Single(Enumerable.Range(0, commands.Count)
            .Where(index => commands[index].Kind == DrawCommandKind.DrawPath));
        DrawCommand command = commands[index];
        DrawPathContour contour = Assert.Single(command.Path!.Contours);
        var transform = new DrawCommandStateAnalyzer().Analyze(commands).Entries[index].Transform;

        Assert.Same(path.Data!.Path, command.Path);
        Assert.Equal(new DrawPoint(10, 20), WorldPoint(contour.StartPoint));
        Assert.Equal(new DrawPoint(20, 20), WorldPoint(contour.Segments[1].EndPoint));
        Assert.Equal(new DrawPoint(20, 30), WorldPoint(contour.Segments[2].EndPoint));

        DrawPoint WorldPoint(DrawPoint point)
        {
            var world = System.Numerics.Vector2.Transform(new(point.X, point.Y), transform);
            return new DrawPoint(world.X, world.Y);
        }
    }

    [Fact]
    public void PathShapeStretchesSvgGeometryIntoArrangedBounds()
    {
        UIRoot root = new();
        SolidColorBrush fill = new(Color.Black);
        PathShape path = new()
        {
            Geometry = new SvgGeometry("M0 0L72 72Z", new DrawRect(0, 0, 72, 72)),
            Fill = fill
        };
        root.VisualChildren.Add(path);
        root.ProcessFrame();
        path.Arrange(new ArrangeContext(new LayoutRect(3, 4, 144, 96)));
        root.Invalidate(InvalidationFlags.Render | InvalidationFlags.Subtree, "test");
        root.ProcessFrame();

        DrawCommand command = Assert.Single(root.RetainedRenderer.Commit(root));

        Assert.Equal(DrawCommandKind.FillPath, command.Kind);
        Assert.Equal(new DrawRect(3, 4, 144, 96), command.Rect);
        Assert.Equal(new DrawRect(0, 0, 72, 72), command.SourceRect);
        Assert.NotNull(command.Path);
        Assert.Null(command.PathData);
        Assert.Same(fill, command.Brush);
    }

    [Fact]
    public void SvgPathParsesMarkupFriendlyDataAndViewBox()
    {
        UIRoot root = new();
        SolidColorBrush fill = new(Color.Black);
        SvgPath path = new()
        {
            Data = "M1 2L23 22Z",
            ViewBox = "0 0 24 24",
            Fill = fill
        };
        root.VisualChildren.Add(path);
        root.ProcessFrame();
        path.Arrange(new ArrangeContext(new LayoutRect(3, 4, 48, 48)));
        root.Invalidate(InvalidationFlags.Render | InvalidationFlags.Subtree, "test");
        root.ProcessFrame();

        DrawCommand command = Assert.Single(root.RetainedRenderer.Commit(root));

        Assert.Equal(DrawCommandKind.FillPath, command.Kind);
        Assert.NotNull(command.Path);
        Assert.Null(command.PathData);
        Assert.Equal(new DrawRect(0, 0, 24, 24), command.SourceRect);
        Assert.Equal(new DrawRect(3, 4, 48, 48), command.Rect);
        Assert.Same(fill, command.Brush);
    }

    [Fact]
    public void SvgPathRejectsInvalidViewBox()
    {
        SvgPath path = new();

        Assert.Throws<ArgumentException>(() => path.ViewBox = "0 0 0 24");
    }

    [Fact]
    public void ShapePropertyChangeInvalidatesRender()
    {
        RectangleShape rectangle = new();

        rectangle.Fill = new SolidColorBrush(Color.White);

        Assert.True(rectangle.DirtyState.Has(InvalidationFlags.Render));
    }

    [Fact]
    public void ShapeMeasureUsesGeometryBounds()
    {
        RectangleShape rectangle = new()
        {
            Geometry = new RectangleGeometry(new DrawRect(0, 0, 30, 20)),
            Stroke = new SolidColorBrush(Color.Black),
            StrokeThickness = 2
        };

        LayoutSize desired = rectangle.Measure(new MeasureContext(new LayoutSize(100, 100)));

        Assert.Equal(new LayoutSize(32, 22), desired);
    }

    [Fact]
    public void EllipseStrokeStaysInsideTheArrangedBounds()
    {
        Ellipse ellipse = new()
        {
            Stroke = new SolidColorBrush(Color.Black),
            StrokeThickness = 1
        };

        DrawCommand command = Assert.Single(Record(ellipse, new LayoutRect(0, 0, 12, 12)));

        Assert.Equal(DrawCommandKind.DrawEllipse, command.Kind);
        Assert.Equal(new DrawRect(0.5f, 0.5f, 11, 11), command.Rect);
    }

    [Fact]
    public void StrokeThicknessLargerThanTheBoundsLeavesNothingToDraw()
    {
        Shape[] shapes = [new Ellipse(), new RectangleShape()];
        foreach (Shape shape in shapes)
        {
            shape.Fill = new SolidColorBrush(Color.White);
            shape.Stroke = new SolidColorBrush(Color.Black);
            shape.StrokeThickness = 40;

            DrawCommandList commands = Record(shape, new LayoutRect(1, 2, 30, 20));

            Assert.DoesNotContain(commands, command => command.Kind is
                DrawCommandKind.FillEllipse or DrawCommandKind.DrawEllipse or
                DrawCommandKind.FillRectangle or DrawCommandKind.DrawRectangle);
        }
    }

    [Fact]
    public void WithoutAStrokeTheFillUsesTheFullArrangedBounds()
    {
        Shape[] shapes = [new Ellipse(), new RectangleShape()];
        foreach (Shape shape in shapes)
        {
            shape.Fill = new SolidColorBrush(Color.White);
            shape.StrokeThickness = 6;

            DrawCommand command = Assert.Single(Record(shape, new LayoutRect(1, 2, 30, 20)));

            Assert.Equal(new DrawRect(1, 2, 30, 20), command.Rect);
        }
    }

    [Fact]
    public void ZeroStrokeThicknessKeepsTheFullArrangedBounds()
    {
        Shape[] shapes = [new Ellipse(), new RectangleShape()];
        foreach (Shape shape in shapes)
        {
            shape.Fill = new SolidColorBrush(Color.White);
            shape.Stroke = new SolidColorBrush(Color.Black);
            shape.StrokeThickness = 0;

            DrawCommand command = Assert.Single(Record(shape, new LayoutRect(1, 2, 30, 20)));

            Assert.Equal(new DrawRect(1, 2, 30, 20), command.Rect);
        }
    }

    [Fact]
    public void ATransparentStrokeStillDeflatesTheFillLikeWpf()
    {
        Shape[] shapes = [new Ellipse(), new RectangleShape()];
        foreach (Shape shape in shapes)
        {
            shape.Fill = new SolidColorBrush(Color.White);
            shape.Stroke = new SolidColorBrush(new Color(0, 0, 0, 0));
            shape.StrokeThickness = 4;

            DrawCommand command = Assert.Single(Record(shape, new LayoutRect(1, 2, 30, 20)));

            Assert.Equal(new DrawRect(3, 4, 26, 16), command.Rect);
        }
    }

    [Fact]
    public void ExplicitGeometryIsNotDeflatedByTheStroke()
    {
        Shape[] shapes = [new Ellipse(), new RectangleShape()];
        foreach (Shape shape in shapes)
        {
            shape.Geometry = new RectangleGeometry(new DrawRect(5, 6, 10, 8));
            shape.Fill = new SolidColorBrush(Color.White);
            shape.Stroke = new SolidColorBrush(Color.Black);
            shape.StrokeThickness = 4;

            DrawCommand[] commands = Record(shape, new LayoutRect(1, 2, 30, 20)).ToArray();

            Assert.Equal(2, commands.Length);
            Assert.Equal(DrawCommandKind.FillRectangle, commands[0].Kind);
            Assert.Equal(DrawCommandKind.DrawRectangle, commands[1].Kind);
            Assert.All(commands, command => Assert.Equal(new DrawRect(5, 6, 10, 8), command.Rect));
        }
    }

    [Fact]
    public void EllipseAndRectangleNaturalSizeIsTheStrokeThicknessOnly()
    {
        Ellipse stroked = new() { Stroke = new SolidColorBrush(Color.Black), StrokeThickness = 2 };
        Ellipse unstroked = new() { StrokeThickness = 2 };
        RectangleShape strokedRectangle = new() { Stroke = new SolidColorBrush(Color.Black), StrokeThickness = 2 };

        Assert.Equal(new LayoutSize(2, 2), stroked.Measure(new MeasureContext(new LayoutSize(100, 100))));
        Assert.Equal(new LayoutSize(0, 0), unstroked.Measure(new MeasureContext(new LayoutSize(100, 100))));
        Assert.Equal(new LayoutSize(2, 2), strokedRectangle.Measure(new MeasureContext(new LayoutSize(100, 100))));
    }

    private static DrawCommandList Record(Shape shape, LayoutRect bounds)
    {
        UIRoot root = new();
        root.VisualChildren.Add(shape);
        root.ProcessFrame();
        shape.Arrange(new ArrangeContext(bounds));
        root.Invalidate(InvalidationFlags.Render | InvalidationFlags.Subtree, "shape-stroke-inset");
        root.ProcessFrame();
        return root.RetainedRenderer.Commit(root);
    }
}
