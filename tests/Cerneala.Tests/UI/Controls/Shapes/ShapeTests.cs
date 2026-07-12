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
            StrokeThickness = 2
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
        Assert.Equal(new DrawRect(1, 2, 30, 20), commands[0].Rect);
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
        Assert.Equal(new DrawRect(1, 2, 30, 20), commands[0].Rect);
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

        DrawCommand command = Assert.Single(commands);
        Assert.Equal(DrawCommandKind.FillRectangle, command.Kind);
        Assert.Equal(new DrawRect(11, 22, 30, 20), command.Rect);
    }

    [Fact]
    public void PathShapeRendersLineSegments()
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

        Assert.Equal(2, commands.Count);
        Assert.All(commands, command => Assert.Equal(DrawCommandKind.DrawLine, command.Kind));
        Assert.Equal(new DrawPoint(0, 0), commands[0].Position);
        Assert.Equal(new DrawPoint(10, 0), commands[0].EndPoint);
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
        Assert.Equal("M0 0L72 72Z", command.PathData);
        Assert.Same(fill, command.Brush);
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
}
