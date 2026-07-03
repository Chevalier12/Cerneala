using Cerneala.UI.Hosting;

namespace Cerneala.Tests.UI.Hosting;

public sealed class UiViewportTests
{
    [Fact]
    public void EqualValuesCompareEqual()
    {
        Assert.Equal(new UiViewport(800, 600, 2), new UiViewport(800, 600, 2));
    }

    [Theory]
    [InlineData(-1, 10, 1)]
    [InlineData(10, -1, 1)]
    [InlineData(10, 10, 0)]
    public void InvalidValuesThrow(float width, float height, float scale)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new UiViewport(width, height, scale));
    }

    [Theory]
    [InlineData(float.NaN, 10, 1)]
    [InlineData(float.PositiveInfinity, 10, 1)]
    [InlineData(10, float.NaN, 1)]
    [InlineData(10, float.PositiveInfinity, 1)]
    [InlineData(10, 10, float.NaN)]
    [InlineData(10, 10, float.PositiveInfinity)]
    public void NonFiniteValuesThrow(float width, float height, float scale)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new UiViewport(width, height, scale));
    }
}
