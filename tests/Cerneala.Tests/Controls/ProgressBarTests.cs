using Cerneala.Drawing;
using Cerneala.UI.Controls;
using Cerneala.UI.Controls.Primitives;
using Cerneala.UI.Core;
using Cerneala.UI.Elements;
using Cerneala.UI.Invalidation;
using Cerneala.UI.Layout;

namespace Cerneala.Tests.Controls;

public sealed class ProgressBarTests
{
    [Fact]
    public void ProgressBarRendersValueRatio()
    {
        UIRoot root = new();
        ProgressBar progress = new()
        {
            Minimum = 0,
            Maximum = 100,
            Value = 25,
            Foreground = new Cerneala.UI.Media.SolidColorBrush(Color.White)
        };
        root.VisualChildren.Add(progress);
        root.ProcessFrame();
        progress.Arrange(new ArrangeContext(new LayoutRect(0, 0, 80, 10)));
        root.Invalidate(InvalidationFlags.Render | InvalidationFlags.Subtree, "test");
        root.ProcessFrame();

        DrawCommandList commands = root.RetainedRenderer.Commit(root);

        Assert.Equal(DrawCommandKind.FillRectangle, commands[1].Kind);
        Assert.Equal(new DrawRect(0, 0, 20, 10), commands[1].Rect);
        Assert.Equal(0.25f, progress.ValueRatio);
    }

    [Fact]
    public void DefaultBorderIsAStrokeOutsideTheArrangedBounds()
    {
        UIRoot root = new();
        ProgressBar progress = new();
        DrawCommandList commands = Render(root, progress, new LayoutRect(0, 0, 80, 10));

        // The default BorderThickness is 1, a positive side: a pen of thickness 1 centred on the bounds inflated by 0.5
        // covers exactly the 1 px ring just outside (0, 0, 80, 10).
        DrawCommand border = Assert.Single(commands, command => ReferenceEquals(command.Brush, progress.BorderBrush));
        Assert.Equal(DrawCommandKind.DrawRectangle, border.Kind);
        Assert.Equal(new DrawRect(-0.5f, -0.5f, 81, 11), border.Rect);
        Assert.Equal(1, border.Thickness);
    }

    [Fact]
    public void NegativeBorderIsAStrokeInsideTheArrangedBounds()
    {
        UIRoot root = new();
        ProgressBar progress = new() { BorderThickness = new Thickness(-1) };
        DrawCommandList commands = Render(root, progress, new LayoutRect(0, 0, 80, 10));

        DrawCommand border = Assert.Single(commands, command => ReferenceEquals(command.Brush, progress.BorderBrush));
        Assert.Equal(DrawCommandKind.DrawRectangle, border.Kind);
        Assert.Equal(new DrawRect(0.5f, 0.5f, 79, 9), border.Rect);
        Assert.Equal(1, border.Thickness);
    }

    [Theory]
    [InlineData(2f)]
    [InlineData(-2f)]
    public void BorderWithOneNonZeroSideIsNotAStrokeOnAllFourSides(float bottom)
    {
        UIRoot root = new();
        ProgressBar progress = new() { BorderThickness = new Thickness(0, 0, 0, bottom) };
        DrawCommandList commands = Render(root, progress, new LayoutRect(0, 0, 80, 10));

        // As Border: the sides differ, so the border is one even-odd ring, which here is only the bottom band: below the
        // bounds for +2, inside them for -2.
        DrawCommand border = Assert.Single(commands, command => ReferenceEquals(command.Brush, progress.BorderBrush));
        Assert.Equal(DrawCommandKind.FillPath, border.Kind);
        Assert.Equal(DrawFillRule.EvenOdd, border.FillRule);
    }

    [Theory]
    [InlineData(-1f, 1f, 1f, 1f)]
    [InlineData(-1f, 1f, 0f, 0.5f)]
    [InlineData(-3e38f, 3e38f, 3e38f, 1f)]
    [InlineData(-3e38f, 3e38f, 0f, 0.5f)]
    [InlineData(-3e38f, 3e38f, -3e38f, 0f)]
    public void ValueRatioIsRightForFiniteEndpointsWhoseDifferenceOverflowsFloat(float minimum, float maximum, float value, float expected)
    {
        ProgressBar progress = new();
        progress.Minimum = minimum;
        progress.Maximum = maximum;
        progress.Value = value;

        Assert.True(
            Math.Abs(progress.ValueRatio - expected) < 1e-4f,
            $"({minimum}, {maximum}, {value}): ValueRatio {progress.ValueRatio}, expected {expected}.");
    }

    // A full bar must fill the whole 80 px width; the (-1, 1, 1) row is the control.
    [Theory]
    [InlineData(-1f, 1f, 1f)]
    [InlineData(-3e38f, 3e38f, 3e38f)]
    public void FullBarDrawsAFillOfTheWholeWidth(float minimum, float maximum, float value)
    {
        UIRoot root = new();
        ProgressBar progress = new();
        progress.Minimum = minimum;
        progress.Maximum = maximum;
        progress.Value = value;

        DrawCommandList commands = Render(root, progress, new LayoutRect(0, 0, 80, 10));

        DrawCommand[] fills = [.. commands.Where(command => command.Kind == DrawCommandKind.FillRectangle && ReferenceEquals(command.Brush, progress.Foreground))];
        Assert.True(
            fills.Length == 1 && fills[0].Rect.Width == 80,
            $"({minimum}, {maximum}, {value}): {(fills.Length == 0 ? "no fill drawn" : "fill " + fills[0].Rect)}; ValueRatio {progress.ValueRatio}.");
    }

    private static DrawCommandList Render(UIRoot root, ProgressBar progress, LayoutRect bounds)
    {
        root.VisualChildren.Add(progress);
        root.ProcessFrame();
        progress.Arrange(new ArrangeContext(bounds));
        root.Invalidate(InvalidationFlags.Render | InvalidationFlags.Subtree, "test");
        root.ProcessFrame();
        return root.RetainedRenderer.Commit(root);
    }

    [Fact]
    public void MarkupValueRemainsEffectiveAfterRangeIsConfigured()
    {
        ProgressBar progress = new();

        progress.SetValue(RangeBase.MinimumProperty, 0f, UiPropertyValueSource.MarkupBase);
        progress.SetValue(RangeBase.MaximumProperty, 100f, UiPropertyValueSource.MarkupBase);
        progress.SetValue(RangeBase.ValueProperty, 68f, UiPropertyValueSource.MarkupBase);

        Assert.Equal(68f, progress.Value);
        Assert.Equal(0.68f, progress.ValueRatio);
    }
}
