using Cerneala.Drawing;
using Cerneala.UI.Controls;
using Cerneala.UI.Core;
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
            // A negative side is the inward border: it takes room from the content (a positive side takes none).
            BorderThickness = new Thickness(-1),
            Child = new FixedElement(new LayoutSize(10, 5))
        };

        LayoutSize desired = border.Measure(new MeasureContext(new LayoutSize(100, 100)));
        border.Arrange(new ArrangeContext(new LayoutRect(0, 0, 30, 20)));

        Assert.Equal(new LayoutSize(16, 11), desired);
        Assert.Equal(new LayoutRect(3, 3, 24, 14), border.Child!.ArrangedBounds);
    }

    [Theory]
    [InlineData(-4, -4, -4, -4, 4, 4, 92, 32)]
    [InlineData(4, 4, 4, 4, 0, 0, 100, 40)]
    [InlineData(-4, 3, -2, 3, 4, 0, 94, 40)]
    public void OnlyNegativeBorderSidesTakeRoomFromTheChild(
        float left,
        float top,
        float right,
        float bottom,
        float x,
        float y,
        float width,
        float height)
    {
        Border border = new()
        {
            BorderThickness = new Thickness(left, top, right, bottom),
            Child = new FixedElement(new LayoutSize(10, 5))
        };

        border.Measure(new MeasureContext(new LayoutSize(200, 200)));
        border.Arrange(new ArrangeContext(new LayoutRect(0, 0, 100, 40)));

        Assert.Equal(new LayoutRect(x, y, width, height), border.Child!.ArrangedBounds);
    }

    [Fact]
    public void PositiveBorderThicknessTakesNoRoomButPaddingStillDoes()
    {
        Border border = new()
        {
            Padding = new Thickness(2),
            BorderThickness = new Thickness(4),
            Child = new FixedElement(new LayoutSize(10, 5))
        };

        LayoutSize desired = border.Measure(new MeasureContext(new LayoutSize(200, 200)));
        border.Arrange(new ArrangeContext(new LayoutRect(0, 0, 100, 40)));

        Assert.Equal(new LayoutSize(14, 9), desired);
        Assert.Equal(new LayoutRect(2, 2, 96, 36), border.Child!.ArrangedBounds);
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

    [Fact]
    public void NegativeUniformBorderThicknessIsDrawnInsideTheArrangedBounds()
    {
        Border border = new()
        {
            BorderBrush = new SolidColorBrush(Color.Black),
            BorderThickness = new Thickness(-2)
        };

        DrawCommandList commands = RenderAt(border, new LayoutRect(1, 2, 30, 20));

        DrawCommand stroke = Assert.Single(commands.Where(candidate => candidate.Kind == DrawCommandKind.DrawRectangle));
        // The centred pen of thickness 2 covers the 2px just inside the arranged bounds.
        Assert.Equal(new DrawRect(2, 3, 28, 18), stroke.Rect);
        Assert.Equal(2, stroke.Thickness);
    }

    [Fact]
    public void PositiveUniformBorderThicknessIsDrawnOutsideTheArrangedBounds()
    {
        Border border = new()
        {
            BorderBrush = new SolidColorBrush(Color.Black),
            BorderThickness = new Thickness(4)
        };

        DrawCommandList commands = RenderAt(border, new LayoutRect(0, 0, 100, 40));

        // The centred pen of thickness 4 on the bounds inflated by 2 covers the 4px just outside (0, 0, 100, 40).
        DrawCommand stroke = Assert.Single(commands.Where(candidate => candidate.Kind == DrawCommandKind.DrawRectangle));
        Assert.Equal(new DrawRect(-2, -2, 104, 44), stroke.Rect);
        Assert.Equal(4, stroke.Thickness);
    }

    [Fact]
    public void PositiveUniformBorderThicknessLargerThanTheBoundsIsStillAStrokeAroundThem()
    {
        Border border = new()
        {
            BorderBrush = new SolidColorBrush(Color.Black),
            BorderThickness = new Thickness(5)
        };

        DrawCommandList commands = RenderAt(border, new LayoutRect(1, 2, 4, 6));

        DrawCommand stroke = Assert.Single(commands.Where(candidate => candidate.Kind == DrawCommandKind.DrawRectangle));
        Assert.Equal(new DrawRect(-1.5f, -0.5f, 9, 11), stroke.Rect);
        Assert.Equal(5, stroke.Thickness);
    }

    [Fact]
    public void MixedSignBorderThicknessIsOneEvenOddPathWithAnOuterAndAnInnerFigure()
    {
        Border border = new()
        {
            Background = new SolidColorBrush(Color.White),
            BorderBrush = new SolidColorBrush(Color.Black),
            BorderThickness = new Thickness(-4, 3, -2, 3)
        };

        DrawCommandList commands = RenderAt(border, new LayoutRect(0, 0, 100, 40));

        Assert.DoesNotContain(commands, candidate => candidate.Kind == DrawCommandKind.DrawRectangle);
        DrawCommand background = Assert.Single(commands.Where(candidate => candidate.Kind == DrawCommandKind.FillRectangle));
        Assert.Equal(new DrawRect(0, 0, 100, 40), background.Rect);
        DrawCommand path = Assert.Single(commands.Where(candidate => candidate.Kind == DrawCommandKind.FillPath));
        Assert.Equal(DrawFillRule.EvenOdd, path.FillRule);
        Assert.Equal(2, path.Path!.Contours.Count);
    }

    [Fact]
    public void OutsideBorderPathIsLocalToItsOuterFigureAndKeepsItsIdentityWhileTheBorderMoves()
    {
        Border border = new()
        {
            BorderBrush = new SolidColorBrush(Color.Black),
            BorderThickness = new Thickness(1, 2, 3, 4)
        };
        UIRoot root = new();
        root.VisualChildren.Add(border);
        root.ProcessFrame();
        border.Arrange(new ArrangeContext(new LayoutRect(10, 10, 40, 30)));
        root.Invalidate(InvalidationFlags.Render | InvalidationFlags.Subtree, "test");
        root.ProcessFrame();
        DrawPath first = Assert.Single(root.RetainedRenderer.Commit(root).Where(candidate => candidate.Kind == DrawCommandKind.FillPath)).Path!;

        border.Arrange(new ArrangeContext(new LayoutRect(20, 30, 40, 30)));
        root.Invalidate(InvalidationFlags.Render | InvalidationFlags.Subtree, "test");
        root.ProcessFrame();
        DrawPath moved = Assert.Single(root.RetainedRenderer.Commit(root).Where(candidate => candidate.Kind == DrawCommandKind.FillPath)).Path!;

        Assert.Same(first, moved);
    }

    [Fact]
    public void PositiveBorderThicknessWithACornerRadiusIsOneEvenOddPathAndTheBackgroundStaysRounded()
    {
        Border border = new()
        {
            Background = new SolidColorBrush(Color.White),
            BorderBrush = new SolidColorBrush(Color.Black),
            BorderThickness = new Thickness(4),
            CornerRadius = new CornerRadius(8)
        };

        DrawCommandList commands = RenderAt(border, new LayoutRect(10, 10, 40, 30));

        DrawCommand background = Assert.Single(commands.Where(candidate => candidate.Kind == DrawCommandKind.FillRoundedRectangle));
        Assert.Equal(new DrawRect(10, 10, 40, 30), background.Rect);
        Assert.Equal(new DrawCornerRadius(8), background.CornerRadius);
        DrawCommand path = Assert.Single(commands.Where(candidate => candidate.Kind == DrawCommandKind.FillPath));
        Assert.Equal(DrawFillRule.EvenOdd, path.FillRule);
        Assert.Equal(2, path.Path!.Contours.Count);
    }

    [Fact]
    public void NegativeUniformBorderThicknessThatCoversTheWholeRectFillsIt()
    {
        Border border = new()
        {
            BorderBrush = new SolidColorBrush(Color.Black),
            BorderThickness = new Thickness(-5)
        };

        DrawCommandList commands = RenderAt(border, new LayoutRect(1, 2, 10, 20));

        Assert.DoesNotContain(commands, candidate => candidate.Kind == DrawCommandKind.DrawRectangle);
        DrawCommand fill = Assert.Single(commands.Where(candidate => candidate.Kind == DrawCommandKind.FillRectangle));
        Assert.Equal(new DrawRect(1, 2, 10, 20), fill.Rect);
    }

    [Fact]
    public void SingleSideBorderThicknessIsOneEvenOddPathAndNotAStrokeOfTheWholeRect()
    {
        Border border = new()
        {
            BorderBrush = new SolidColorBrush(Color.Black),
            BorderThickness = new Thickness(0, 0, 0, 2)
        };

        DrawCommandList commands = RenderAt(border, new LayoutRect(1, 2, 30, 20));

        Assert.DoesNotContain(commands, candidate => candidate.Kind == DrawCommandKind.DrawRectangle);
        DrawCommand path = Assert.Single(commands.Where(candidate => candidate.Kind == DrawCommandKind.FillPath));
        Assert.Equal(DrawFillRule.EvenOdd, path.FillRule);
    }

    [Fact]
    public void CornerRadiusDefaultsToZeroAndKeepsSquareCommands()
    {
        Border border = new()
        {
            Background = new SolidColorBrush(Color.White),
            BorderBrush = new SolidColorBrush(Color.Black),
            BorderThickness = new Thickness(2)
        };

        Assert.Equal(CornerRadius.Zero, border.CornerRadius);
        Assert.Equal(new CornerRadius(0, 0, 0, 0), border.CornerRadius);
        DrawCommandList commands = RenderAt(border, new LayoutRect(1, 2, 30, 20));
        Assert.DoesNotContain(commands, candidate => candidate.Kind == DrawCommandKind.FillRoundedRectangle);
        Assert.DoesNotContain(commands, candidate => candidate.Kind == DrawCommandKind.FillPath);
        Assert.Contains(commands, candidate => candidate.Kind == DrawCommandKind.FillRectangle);
        Assert.Contains(commands, candidate => candidate.Kind == DrawCommandKind.DrawRectangle);
    }

    [Fact]
    public void UniformCornerRadiusUsesARoundedBackgroundAndAPathBorder()
    {
        Border border = new()
        {
            Background = new SolidColorBrush(Color.White),
            BorderBrush = new SolidColorBrush(Color.Black),
            BorderThickness = new Thickness(2),
            CornerRadius = new CornerRadius(8)
        };

        DrawCommandList commands = RenderAt(border, new LayoutRect(1, 2, 30, 20));

        Assert.DoesNotContain(commands, candidate => candidate.Kind == DrawCommandKind.FillRectangle);
        Assert.DoesNotContain(commands, candidate => candidate.Kind == DrawCommandKind.DrawRectangle);
        DrawCommand background = Assert.Single(commands.Where(candidate => candidate.Kind == DrawCommandKind.FillRoundedRectangle));
        Assert.Equal(new DrawRect(1, 2, 30, 20), background.Rect);
        Assert.Equal(new DrawCornerRadius(8), background.CornerRadius);
        DrawCommand stroke = Assert.Single(commands.Where(candidate => candidate.Kind == DrawCommandKind.FillPath));
        Assert.Equal(DrawFillRule.EvenOdd, stroke.FillRule);
        Assert.Equal(2, stroke.Path!.Contours.Count);
    }

    [Fact]
    public void CornerRadiusLargerThanTheBoundsIsNormalizedLikeDrawCornerRadius()
    {
        Border border = new()
        {
            Background = new SolidColorBrush(Color.White),
            CornerRadius = new CornerRadius(100)
        };

        DrawCommandList commands = RenderAt(border, new LayoutRect(0, 0, 30, 20));

        DrawCommand background = Assert.Single(commands.Where(candidate => candidate.Kind == DrawCommandKind.FillRoundedRectangle));
        Assert.Equal(new DrawCornerRadius(100).Normalize(new DrawRect(0, 0, 30, 20)), background.CornerRadius);
    }

    [Fact]
    public void BorderThicknessThatFillsTheBoundsLeavesOnlyTheOuterFigure()
    {
        Border border = new()
        {
            BorderBrush = new SolidColorBrush(Color.Black),
            BorderThickness = new Thickness(0, 0, 0, -30),
            CornerRadius = new CornerRadius(4)
        };

        DrawCommandList commands = RenderAt(border, new LayoutRect(0, 0, 30, 20));

        DrawCommand path = Assert.Single(commands.Where(candidate => candidate.Kind == DrawCommandKind.FillPath));
        Assert.Single(path.Path!.Contours);
    }

    [Fact]
    public void NoBorderPathIsDrawnWithoutThicknessOrBrush()
    {
        Border noThickness = new() { BorderBrush = new SolidColorBrush(Color.Black), BorderThickness = Thickness.Zero, CornerRadius = new CornerRadius(4) };
        Border noBrush = new() { BorderBrush = null, BorderThickness = new Thickness(2), CornerRadius = new CornerRadius(4) };

        Assert.DoesNotContain(RenderAt(noThickness, new LayoutRect(0, 0, 30, 20)), candidate => candidate.Kind == DrawCommandKind.FillPath);
        Assert.DoesNotContain(RenderAt(noBrush, new LayoutRect(0, 0, 30, 20)), candidate => candidate.Kind == DrawCommandKind.FillPath);
    }

    [Fact]
    public void BorderReusesItsPathWhileSizeThicknessAndRadiusAreUnchanged()
    {
        Border border = new()
        {
            BorderBrush = new SolidColorBrush(Color.Black),
            BorderThickness = new Thickness(0, 0, 0, 2)
        };
        UIRoot root = new();
        root.VisualChildren.Add(border);
        root.ProcessFrame();
        border.Arrange(new ArrangeContext(new LayoutRect(1, 2, 30, 20)));
        root.Invalidate(InvalidationFlags.Render | InvalidationFlags.Subtree, "test");
        root.ProcessFrame();
        DrawPath first = Assert.Single(root.RetainedRenderer.Commit(root).Where(candidate => candidate.Kind == DrawCommandKind.FillPath)).Path!;

        border.Arrange(new ArrangeContext(new LayoutRect(5, 6, 30, 20)));
        border.Background = new SolidColorBrush(Color.White);
        root.Invalidate(InvalidationFlags.Render | InvalidationFlags.Subtree, "test");
        root.ProcessFrame();
        DrawPath moved = Assert.Single(root.RetainedRenderer.Commit(root).Where(candidate => candidate.Kind == DrawCommandKind.FillPath)).Path!;

        Assert.Same(first, moved);
    }

    [Theory]
    [InlineData(-1f)]
    [InlineData(float.NaN)]
    [InlineData(float.PositiveInfinity)]
    public void CornerRadiusRejectsNegativeAndNonFiniteCorners(float invalid)
    {
        Assert.Throws<ArgumentException>(() => new Border { CornerRadius = new CornerRadius(invalid) });
        Assert.Throws<ArgumentException>(() => new Border { CornerRadius = new CornerRadius(1, 2, invalid, 4) });
    }

    [Fact]
    public void CornerRadiusChangeInvalidatesRenderButNotMeasure()
    {
        Assert.True(Border.CornerRadiusProperty.Options.HasFlag(UiPropertyOptions.AffectsRender));
        Assert.False(Border.CornerRadiusProperty.Options.HasFlag(UiPropertyOptions.AffectsMeasure));
    }

    private static DrawCommandList RenderAt(Border border, LayoutRect bounds)
    {
        UIRoot root = new();
        root.VisualChildren.Add(border);
        root.ProcessFrame();
        border.Arrange(new ArrangeContext(bounds));
        root.Invalidate(InvalidationFlags.Render | InvalidationFlags.Subtree, "test");
        root.ProcessFrame();
        return root.RetainedRenderer.Commit(root);
    }

    private sealed class FixedElement(LayoutSize size) : UIElement
    {
        protected override LayoutSize MeasureCore(MeasureContext context)
        {
            return size;
        }
    }
}
