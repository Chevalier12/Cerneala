using Cerneala.UI.Controls;
using Cerneala.UI.Elements;
using Cerneala.UI.Layout;
using Cerneala.UI.Layout.Panels;
using Cerneala.UI.Layout.Virtualization;

namespace Cerneala.Tests.UI.Layout;

public sealed class VirtualizationBreakerRegressionTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void MeasuredRowBecomingZeroHeightRemovesItsExtent(bool collapse)
    {
        VirtualizingStackPanel panel = new();
        Border first = new() { Height = 40 };
        Border second = new() { Height = 40 };
        panel.VisualChildren.Add(first);
        panel.VisualChildren.Add(second);
        panel.UpdateViewport(new ItemsVirtualizationViewport(2, 200, 0, 0));
        Layout(panel);
        Assert.Equal(80, panel.TotalExtent);
        Assert.Equal(40, second.ArrangedBounds.Y);

        if (collapse) { first.Visibility = Visibility.Collapsed; }
        else { first.Height = 0; }
        Layout(panel);

        Assert.Equal(0, first.DesiredSize.Height);
        Assert.True(panel.TotalExtent == 40 && second.ArrangedBounds.Y == 0,
            $"Expected extent=40 and second.Y=0; actual extent={panel.TotalExtent}, second.Y={second.ArrangedBounds.Y}.");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ScheduledZeroHeightRowUpdatesExtentAndSiblingPosition(bool collapse)
    {
        UIRoot root = new(100, 200);
        VirtualizingStackPanel panel = new();
        Border first = new() { Height = 40 };
        Border second = new() { Height = 40 };
        panel.VisualChildren.Add(first);
        panel.VisualChildren.Add(second);
        panel.UpdateViewport(new ItemsVirtualizationViewport(2, 200, 0, 0));
        root.VisualChildren.Add(panel);
        root.ProcessFrame();
        Assert.Equal(80, panel.TotalExtent);
        Assert.Equal(40, second.ArrangedBounds.Y);

        if (collapse) { first.Visibility = Visibility.Collapsed; }
        else { first.Height = 0; }
        for (int frame = 0; frame < 4; frame++) { root.ProcessFrame(); }

        Assert.False(root.Scheduler.HasWork);
        Assert.Equal(0, first.DesiredSize.Height);
        Assert.Equal(40, panel.TotalExtent);
        Assert.Equal(0, second.ArrangedBounds.Y);
    }

    [Fact]
    public void MeasuredZeroDoesNotMakeUnmeasuredItemsZeroHeight()
    {
        VirtualizingStackPanel panel = new();
        panel.VisualChildren.Add(new Border { Height = 0 });
        panel.UpdateViewport(new ItemsVirtualizationViewport(100, 200, 0, 0));
        Layout(panel);

        Assert.Equal(99 * 28, panel.TotalExtent);
        Assert.Equal(1, panel.RealizationWindow.StartIndex);
        Assert.Equal(9, panel.RealizationWindow.EndIndexExclusive);
    }

    private static void Layout(VirtualizingStackPanel panel)
    {
        panel.Measure(new MeasureContext(new LayoutSize(100, 200)));
        panel.Arrange(new ArrangeContext(new LayoutRect(0, 0, 100, 200)));
    }
}
