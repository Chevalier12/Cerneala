using Cerneala.Drawing;
using Cerneala.UI.Controls.Shapes;
using Cerneala.UI.Elements;
using Cerneala.UI.Invalidation;
using Cerneala.UI.Layout;
using Cerneala.UI.Media;
using PathShape = Cerneala.UI.Controls.Shapes.Path;
using RectangleShape = Cerneala.UI.Controls.Shapes.Rectangle;

namespace Cerneala.Tests.UI.Controls.Shapes;

public sealed class ShapeRenderingContractTests
{
    [Fact]
    public void ShapeOpacityUsesTheUiElementPropertyAndAffectsItsPaint()
    {
        RectangleShape rectangle = new() { Fill = new SolidColorBrush(Color.White), Opacity = 0.5f };

        DrawCommandList commands = Record(rectangle, new LayoutRect(0, 0, 30, 20));

        Assert.Equal(0.5f, ((UIElement)rectangle).Opacity);
        Assert.Same(UIElement.OpacityProperty, Shape.OpacityProperty);
        Assert.Equal(0.5f, Assert.Single(commands).BrushOpacity);
    }

    [Fact]
    public void ShapeTransformUsesTheUiElementProperty()
    {
        RectangleShape rectangle = new()
        {
            RenderTransform = new Transform(Matrix3x2.CreateRotation(MathF.PI / 4))
        };

        Assert.Equal(rectangle.RenderTransform, ((UIElement)rectangle).RenderTransform);
        Assert.Same(UIElement.RenderTransformProperty, Shape.RenderTransformProperty);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void InitiallyTransparentShapeCanBecomeVisibleWithoutRebuildingLocalCommands(bool usePath)
    {
        Shape shape = usePath
            ? new PathShape { Data = PathGeometry.Parse("M0 0L30 0L15 20Z") }
            : new RectangleShape();
        shape.Fill = new SolidColorBrush(Color.White);
        shape.Opacity = 0;
        UIRoot root = new(100, 100);
        root.VisualChildren.Add(shape);
        root.ProcessFrame();
        Assert.Empty(root.RetainedRenderer.Commit(root));
        var localCache = root.RetainedRenderCache.GetElementCache(shape);
        long renderVersion = localCache.RenderVersion;

        foreach (float opacity in new[] { 0.5f, 0, 1 })
        {
            shape.Opacity = opacity;
            root.ProcessFrame();
            DrawCommandList commands = root.RetainedRenderer.Commit(root);
            if (opacity == 0)
            {
                Assert.Empty(commands);
            }
            else
            {
                DrawCommand fill = Assert.Single(commands.Where(command =>
                    command.Kind is DrawCommandKind.FillRectangle or DrawCommandKind.FillPath));
                Assert.Equal(opacity, fill.BrushOpacity);
            }
            Assert.Equal(renderVersion, localCache.RenderVersion);
        }
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    public void ZeroAreaPathSkipsFillWithoutSuppressingItsStroke(int scene)
    {
        Shape shape = scene switch
        {
            0 => new Line { EndPoint = new DrawPoint(30, 0) },
            1 => new Line { EndPoint = new DrawPoint(0, 20) },
            2 => new Polyline { Points = [new(0, 0), new(15, 0), new(30, 0)] },
            3 => new Polygon { Points = [new(0, 0), new(15, 0), new(30, 0)] },
            _ => new PathShape { Data = PathGeometry.Parse("M0 0L30 0") }
        };
        shape.Fill = new SolidColorBrush(Color.White);
        shape.Stroke = new SolidColorBrush(Color.Black);

        DrawCommandList commands = Record(shape, new LayoutRect(10, 20, 30, 20));

        Assert.DoesNotContain(commands, command => command.Kind == DrawCommandKind.FillPath);
        Assert.Single(commands, command => command.Kind == DrawCommandKind.DrawPath);
    }

    [Fact]
    public void PathCoordinatesAreLocalToItsArrangedPosition()
    {
        PathShape path = new()
        {
            Data = new PathGeometry([new DrawPoint(0, 0), new DrawPoint(10, 10)]),
            Stroke = new SolidColorBrush(Color.Black)
        };

        DrawCommandList commands = Record(path, new LayoutRect(100, 200, 30, 30));
        DrawCommandStateAnalysis analysis = new DrawCommandStateAnalyzer().Analyze(commands);
        int index = Assert.Single(Enumerable.Range(0, commands.Count)
            .Where(index => commands[index].Kind == DrawCommandKind.DrawPath));
        DrawCommand command = commands[index];
        var position = System.Numerics.Vector2.Transform(
            new System.Numerics.Vector2(command.Rect.X, command.Rect.Y),
            analysis.Entries[index].Transform);

        Assert.Equal(100, position.X);
        Assert.Equal(200, position.Y);
    }

    private static DrawCommandList Record(Shape shape, LayoutRect bounds)
    {
        UIRoot root = new();
        root.VisualChildren.Add(shape);
        root.ProcessFrame();
        shape.Arrange(new ArrangeContext(bounds));
        root.Invalidate(InvalidationFlags.Render | InvalidationFlags.Subtree, "shape-contract");
        root.ProcessFrame();
        return root.RetainedRenderer.Commit(root);
    }
}
