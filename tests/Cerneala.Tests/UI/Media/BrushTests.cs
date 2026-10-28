using Cerneala.Drawing;
using Cerneala.UI.Elements;
using Cerneala.UI.Layout;
using Cerneala.UI.Media;

namespace Cerneala.Tests.UI.Media;

public sealed class BrushTests
{
    [Fact]
    public void SolidColorBrushExposesSolidColor()
    {
        SolidColorBrush brush = new(Color.White);

        Assert.Equal(Color.White, brush.SolidColor);
    }

    [Theory]
    [InlineData(-0.1f)]
    [InlineData(1.1f)]
    [InlineData(float.NaN)]
    [InlineData(float.PositiveInfinity)]
    public void BrushRejectsInvalidOpacity(float opacity)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new SolidColorBrush(Color.White, opacity));
    }

    [Fact]
    public void CompositeBrushesExposeKindAndNoSolidShortcut()
    {
        LinearGradientBrush brush = new(
            new DrawPoint(0, 0),
            new DrawPoint(10, 0),
            [new GradientStop(0, Color.White), new GradientStop(1, Color.Black)],
            0.5f);

        Assert.Equal(DrawBrushKind.LinearGradient, brush.Kind);
        Assert.Null(brush.SolidColor);
        Assert.Equal(0.5f, brush.Opacity);
    }

    [Fact]
    public void DrawingBrushCopiesCommandsAndUsesStructuralEquality()
    {
        DrawCommand[] commands = [DrawCommand.FillRectangle(new DrawRect(0, 0, 10, 10), Color.White)];
        DrawingBrush first = new(commands, new DrawRect(0, 0, 10, 10));
        DrawingBrush second = new(commands.ToArray(), new DrawRect(0, 0, 10, 10));

        commands[0] = DrawCommand.FillRectangle(new DrawRect(0, 0, 10, 10), Color.Black);

        Assert.Equal(first, second);
        Assert.Equal(Color.White, first.Commands[0].Color);
    }

    [Fact]
    public void TileBrushRejectsInvalidViewport()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new ImageBrush(
            (IDrawImage?)null,
            viewport: new DrawRect(0, 0, 0, 10)));
    }

    [Fact]
    public void LinearGradientBrushOrdersStops()
    {
        LinearGradientBrush brush = new(
            new DrawPoint(0, 0),
            new DrawPoint(10, 0),
            [new GradientStop(1, Color.Black), new GradientStop(0, Color.White)]);

        Assert.Equal([0, 1], brush.Stops.Select(stop => stop.Offset).ToArray());
    }

    [Fact]
    public void LinearGradientBrushStopsCannotBeMutatedThroughExposedCollection()
    {
        LinearGradientBrush brush = new(
            new DrawPoint(0, 0),
            new DrawPoint(10, 0),
            [new GradientStop(0, Color.White)]);

        if (brush.Stops is GradientStop[] exposedStops)
        {
            exposedStops[0] = new GradientStop(0, Color.Black);
        }

        Assert.Equal(Color.White, brush.Stops[0].Color);
    }

    [Fact]
    public void LinearGradientBrushUsesStopValuesForEquality()
    {
        LinearGradientBrush first = new(
            new DrawPoint(0, 0),
            new DrawPoint(10, 0),
            [new GradientStop(0, Color.White), new GradientStop(1, Color.Black)]);
        LinearGradientBrush second = new(
            new DrawPoint(0, 0),
            new DrawPoint(10, 0),
            [new GradientStop(1, Color.Black), new GradientStop(0, Color.White)]);

        Assert.Equal(first, second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    [Fact]
    public void RadialGradientBrushUsesStopValuesForEquality()
    {
        RadialGradientBrush first = new(
            new DrawPoint(5, 5),
            10,
            20,
            [new GradientStop(0, Color.White), new GradientStop(1, Color.Black)]);
        RadialGradientBrush second = new(
            new DrawPoint(5, 5),
            10,
            20,
            [new GradientStop(1, Color.Black), new GradientStop(0, Color.White)]);

        Assert.Equal(first, second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    [Fact]
    public void GradientStopRejectsInvalidOffset()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new GradientStop(-0.1f, Color.Black));
        Assert.Throws<ArgumentOutOfRangeException>(() => new GradientStop(1.1f, Color.Black));
    }

    [Fact]
    public void LinearGradientRejectsInvalidCoordinates()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new LinearGradientBrush(
            new DrawPoint(float.NaN, 0),
            new DrawPoint(1, 0),
            [new GradientStop(0, Color.White)]));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(float.NaN)]
    [InlineData(float.PositiveInfinity)]
    public void RadialGradientBrushRejectsInvalidRadii(float radius)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new RadialGradientBrush(new DrawPoint(0, 0), radius, 1, [new GradientStop(0, Color.White)]));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new RadialGradientBrush(new DrawPoint(0, 0), 1, radius, [new GradientStop(0, Color.White)]));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(float.NaN)]
    [InlineData(float.PositiveInfinity)]
    public void PenRejectsInvalidThickness(float thickness)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Pen(new SolidColorBrush(Color.Black), thickness));
    }

    private static readonly Color CaptureRed = new(255, 0, 0);
    private static readonly Color CaptureBlue = new(0, 0, 255);

    [Theory]
    [InlineData(Visibility.Visible, true)]
    [InlineData(Visibility.Hidden, false)]
    [InlineData(Visibility.Collapsed, false)]
    public void VisualBrushCaptureSkipsChildrenThatDoNotParticipateInRendering(Visibility blueVisibility, bool blueExpected)
    {
        (UIElement panel, Cerneala.UI.Controls.Shapes.Rectangle blue) = CreateCapturePanel();
        blue.Visibility = blueVisibility;

        DrawCommand[] commands = CaptureVisualBrush(panel);

        Assert.Equal(blueExpected, commands.Any(command => IsFill(command, CaptureBlue)));
        Assert.Contains(commands, command => IsFill(command, CaptureRed));
    }

    [Fact]
    public void VisualBrushCaptureAppliesChildOpacity()
    {
        (UIElement panel, Cerneala.UI.Controls.Shapes.Rectangle blue) = CreateCapturePanel();
        blue.Opacity = 0.5f;

        DrawCommand[] commands = CaptureVisualBrush(panel);

        DrawCommand blueFill = Assert.Single(commands, command => IsFill(command, CaptureBlue));
        Assert.Equal(0.5f, EffectiveOpacity(blueFill), 3);
        DrawCommand redFill = Assert.Single(commands, command => IsFill(command, CaptureRed));
        Assert.Equal(1f, EffectiveOpacity(redFill), 3);
    }

    [Fact]
    public void VisualBrushCaptureAppliesChildRenderTransform()
    {
        (UIElement panel, Cerneala.UI.Controls.Shapes.Rectangle blue) = CreateCapturePanel();
        blue.RenderTransform = new Transform(Matrix3x2.CreateRotation(0.5f));
        Matrix3x2 expected = Cerneala.UI.Elements.ElementVisualTransform.GetElementTransform(blue);

        DrawCommand[] commands = CaptureVisualBrush(panel);

        int blueIndex = Array.FindIndex(commands, command => IsFill(command, CaptureBlue));
        Assert.True(blueIndex > 0);
        DrawCommand push = commands[blueIndex - 1];
        Assert.Equal(DrawCommandKind.PushTransform, push.Kind);
        Assert.Equal(
            new System.Numerics.Matrix3x2(expected.M11, expected.M12, expected.M21, expected.M22, expected.M31, expected.M32),
            push.Transform);
        Assert.Equal(DrawCommandKind.PopTransform, commands[blueIndex + 1].Kind);
        Assert.DoesNotContain(commands[..Array.FindIndex(commands, command => IsFill(command, CaptureRed))],
            command => command.Kind == DrawCommandKind.PushTransform);
    }

    [Fact]
    public void VisualBrushCaptureAppliesChildClip()
    {
        (UIElement panel, Cerneala.UI.Controls.Shapes.Rectangle blue) = CreateCapturePanel();
        blue.ClipToBounds = true;
        blue.Arrange(new ArrangeContext(new LayoutRect(4, 4, 8, 8)));

        DrawCommand[] commands = CaptureVisualBrush(panel);

        int blueIndex = Array.FindIndex(commands, command => IsFill(command, CaptureBlue));
        Assert.True(blueIndex > 0);
        DrawCommand push = commands[blueIndex - 1];
        Assert.Equal(DrawCommandKind.PushClip, push.Kind);
        Assert.Equal(new DrawRect(4, 4, 8, 8), push.Rect);
        Assert.Equal(DrawCommandKind.PopClip, commands[blueIndex + 1].Kind);
    }

    private static (UIElement Panel, Cerneala.UI.Controls.Shapes.Rectangle Blue) CreateCapturePanel()
    {
        UIElement panel = new();
        Cerneala.UI.Controls.Shapes.Rectangle red = new() { Fill = new SolidColorBrush(CaptureRed) };
        Cerneala.UI.Controls.Shapes.Rectangle blue = new() { Fill = new SolidColorBrush(CaptureBlue) };
        panel.VisualChildren.Add(red);
        panel.VisualChildren.Add(blue);
        panel.Arrange(new ArrangeContext(new LayoutRect(0, 0, 16, 16)));
        red.Arrange(new ArrangeContext(new LayoutRect(0, 0, 16, 16)));
        blue.Arrange(new ArrangeContext(new LayoutRect(0, 0, 16, 16)));
        return (panel, blue);
    }

    private static DrawCommand[] CaptureVisualBrush(UIElement visual)
    {
        VisualDrawBrushDescriptor descriptor = Assert.IsType<VisualDrawBrushDescriptor>(
            ((IDrawBrush)new VisualBrush(visual)).CreateDescriptor());
        return descriptor.Commands.ToArray();
    }

    private static bool IsFill(DrawCommand command, Color color)
    {
        return command.Kind == DrawCommandKind.FillRectangle &&
            (command.Brush is SolidColorBrush brush ? brush.SolidColor == color : command.Color.R == color.R &&
                command.Color.G == color.G && command.Color.B == color.B);
    }

    private static float EffectiveOpacity(DrawCommand command)
    {
        return command.Brush is not null ? command.BrushOpacity : command.Color.A / 255f;
    }
}
