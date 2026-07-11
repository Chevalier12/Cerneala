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
