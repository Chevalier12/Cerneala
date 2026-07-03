using Cerneala.UI.Layout.Virtualization;

namespace Cerneala.Tests.UI.Layout;

public sealed class VirtualizationTests
{
    [Fact]
    public void RealizationWindowUsesViewportOffsetAndCache()
    {
        VirtualizationContext context = new(100, 10, 30, 20, CacheItems: 1);

        RealizationWindow window = context.GetRealizationWindow();

        Assert.Equal(new RealizationWindow(1, 6), window);
        Assert.True(window.Contains(1));
        Assert.True(window.Contains(5));
        Assert.False(window.Contains(6));
    }

    [Fact]
    public void EmptyContextProducesEmptyWindow()
    {
        Assert.True(new VirtualizationContext(0, 10, 30, 0).GetRealizationWindow().IsEmpty);
        Assert.True(new VirtualizationContext(10, 0, 30, 0).GetRealizationWindow().IsEmpty);
    }

    [Theory]
    [InlineData(float.NaN)]
    [InlineData(float.PositiveInfinity)]
    public void NonFiniteItemExtentReportsZeroTotalExtent(float itemExtent)
    {
        VirtualizationContext context = new(10, itemExtent, 30, 0);

        Assert.True(context.GetRealizationWindow().IsEmpty);
        Assert.Equal(0, context.TotalExtent);
    }
}
