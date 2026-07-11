using Cerneala.Drawing;
using Cerneala.UI.Controls;
using Cerneala.UI.Elements;
using Cerneala.UI.Invalidation;
using Cerneala.UI.Layout;
using Cerneala.UI.Media;

namespace Cerneala.Tests.Controls;

public sealed class BorderTests
{
    [Fact]
    public void BorderMeasuresAndArrangesChildInsidePaddingAndBorder()
    {
        Border border = new()
        {
            Padding = new Thickness(2),
            BorderThickness = new Thickness(1),
            Child = new FixedElement(new LayoutSize(10, 5))
        };

        LayoutSize desired = border.Measure(new MeasureContext(new LayoutSize(100, 100)));
        border.Arrange(new ArrangeContext(new LayoutRect(0, 0, 30, 20)));

        Assert.Equal(new LayoutSize(16, 11), desired);
        Assert.Equal(new LayoutRect(3, 3, 24, 14), border.Child!.ArrangedBounds);
    }

    [Fact]
    public void BorderRendersBackgroundAndStrokeCommands()
    {
        Border border = new()
        {
            Background = new SolidColorBrush(Color.White),
            BorderBrush = new SolidColorBrush(Color.Black),
            BorderThickness = new Thickness(2)
        };
        UIRoot root = new();
        root.VisualChildren.Add(border);
        root.ProcessFrame();
        border.Arrange(new ArrangeContext(new LayoutRect(1, 2, 30, 20)));
        root.Invalidate(InvalidationFlags.Render | InvalidationFlags.Subtree, "test");
        root.ProcessFrame();

        DrawCommandList commands = root.RetainedRenderer.Commit(root);

        Assert.Equal(2, commands.Count);
        Assert.Equal(DrawCommandKind.FillRectangle, commands[0].Kind);
        Assert.Equal(DrawCommandKind.DrawRectangle, commands[1].Kind);
        Assert.Equal(new DrawRect(1, 2, 30, 20), commands[0].Rect);
    }

    [Fact]
    public void BorderPreservesCompositeBrushesInStrokeCommands()
    {
        Brush[] brushes =
        [
            new LinearGradientBrush(new DrawPoint(0, 0), new DrawPoint(10, 0), [new GradientStop(0, Color.White), new GradientStop(1, Color.Black)]),
            new RadialGradientBrush(new DrawPoint(5, 5), 5, 5, [new GradientStop(0, Color.White), new GradientStop(1, Color.Black)]),
            new ImageBrush("border.png")
        ];

        foreach (Brush brush in brushes)
        {
            Border border = new() { BorderBrush = brush, BorderThickness = new Thickness(1) };
            UIRoot root = new(10, 10);
            root.VisualChildren.Add(border);
            border.Arrange(new ArrangeContext(new LayoutRect(0, 0, 10, 10)));
            root.ProcessFrame();

            DrawCommand command = Assert.Single(root.RetainedRenderer.Commit(root).Where(candidate => candidate.Kind == DrawCommandKind.DrawRectangle));
            Assert.Equal(DrawCommandKind.DrawRectangle, command.Kind);
            Assert.Same(brush, command.Brush);
        }
    }

    [Fact]
    public void BorderPreservesCompositeBrushesInFillCommands()
    {
        Brush[] brushes =
        [
            new LinearGradientBrush(new DrawPoint(0, 0), new DrawPoint(10, 0), [new GradientStop(0, Color.White), new GradientStop(1, Color.Black)]),
            new RadialGradientBrush(new DrawPoint(5, 5), 5, 5, [new GradientStop(0, Color.White), new GradientStop(1, Color.Black)]),
            new ImageBrush("background.png")
        ];

        foreach (Brush brush in brushes)
        {
            Border border = new() { Background = brush };
            UIRoot root = new(10, 10);
            root.VisualChildren.Add(border);
            border.Arrange(new ArrangeContext(new LayoutRect(0, 0, 10, 10)));
            root.ProcessFrame();

            DrawCommand command = Assert.Single(root.RetainedRenderer.Commit(root).Where(candidate => candidate.Kind == DrawCommandKind.FillRectangle));
            Assert.Same(brush, command.Brush);
        }
    }

    private sealed class FixedElement(LayoutSize size) : UIElement
    {
        protected override LayoutSize MeasureCore(MeasureContext context)
        {
            return size;
        }
    }
}
