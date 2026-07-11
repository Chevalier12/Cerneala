using Cerneala.Drawing;
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
}
