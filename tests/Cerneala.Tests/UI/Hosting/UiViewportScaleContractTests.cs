using Cerneala.UI.Hosting;

namespace Cerneala.Tests.UI.Hosting;

public sealed class UiViewportScaleContractTests
{
    [Fact]
    public void FromPhysicalPixelsDividesWidthAndHeightByScale()
    {
        UiViewport viewport = UiViewport.FromPhysicalPixels(2400, 1350, 1.5f);

        Assert.Equal(1600, viewport.Width);
        Assert.Equal(900, viewport.Height);
        Assert.Equal(1.5f, viewport.Scale);
    }

    [Theory]
    [InlineData(-1, 100, 1)]
    [InlineData(100, -1, 1)]
    [InlineData(100, 100, 0)]
    [InlineData(100, 100, -1)]
    [InlineData(100, 100, float.NaN)]
    [InlineData(100, 100, float.PositiveInfinity)]
    public void FromPhysicalPixelsRejectsInvalidPixelSizeOrScale(int pixelWidth, int pixelHeight, float scale)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => UiViewport.FromPhysicalPixels(pixelWidth, pixelHeight, scale));
    }

    [Theory]
    [InlineData(0.25f, 2, 1)]
    [InlineData(10.25f, 2, 21)]
    [InlineData(10.75f, 2, 22)]
    public void LogicalToPhysicalRoundsDeterministically(float logical, float scale, int expectedPhysicalPixel)
    {
        int physicalPixel = UiCoordinateMapper.LogicalToPhysicalPixel(logical, scale);

        Assert.Equal(expectedPhysicalPixel, physicalPixel);
    }

    [Fact]
    public void PhysicalToLogicalPreservesFractionalLogicalCoordinates()
    {
        float logical = UiCoordinateMapper.PhysicalToLogical(101, 2);

        Assert.Equal(50.5f, logical);
    }

    [Fact]
    public void ViewportEqualityIncludesScale()
    {
        UiViewport first = new(800, 600, 1);
        UiViewport second = new(800, 600, 2);

        Assert.NotEqual(first, second);
    }
}
