using Cerneala.UI.Controls;
using Cerneala.UI.Data;
using Cerneala.UI.Elements;
using Cerneala.UI.Invalidation;
using Cerneala.UI.Layout;

namespace Cerneala.Tests.Controls;

public sealed class ItemsSourceObservableTests
{
    [Fact]
    public void ItemsControlItemsSourceInitializesContainersFromObservableList()
    {
        ItemsControl control = new() { ItemsSource = new ObservableList<string>(["one", "two"]) };

        control.Measure(new MeasureContext(new LayoutSize(200, 100)));

        Assert.Equal(2, control.ItemContainerGenerator.RealizedContainers.Count);
    }

    [Fact]
    public void ObservableItemsSourceAddInvalidatesMeasureArrangeRenderHitTest()
    {
        UIRoot root = new(200, 100);
        ObservableList<string> items = new(["one"]);
        ItemsControl control = new() { ItemsSource = items };
        root.VisualChildren.Add(control);
        root.ProcessFrame();

        items.Add("two");
        FrameStats stats = root.ProcessFrame();

        Assert.Equal(2, control.ItemContainerGenerator.RealizedContainers.Count);
        Assert.True(stats.MeasuredElements > 0);
        Assert.True(stats.ArrangedElements > 0);
        Assert.True(stats.RenderedElements > 0);
        Assert.True(stats.HitTestElements > 0);
    }

    [Fact]
    public void ReplacingItemsSourceUnsubscribesOldSource()
    {
        UIRoot root = new(200, 100);
        ObservableList<string> oldItems = new(["old"]);
        ObservableList<string> newItems = new(["new"]);
        ItemsControl control = new() { ItemsSource = oldItems };
        root.VisualChildren.Add(control);
        root.ProcessFrame();

        control.ItemsSource = newItems;
        root.ProcessFrame();
        oldItems.Add("ignored");
        FrameStats unchanged = root.ProcessFrame();

        Assert.Equal(1, control.ItemCount);
        Assert.Equal(1, unchanged.NoWorkFrames);
    }

    [Fact]
    public void ClearingItemsSourceReturnsToLocalItemsMode()
    {
        ItemsControl control = new() { ItemsSource = new ObservableList<string>(["source"]) };
        control.Items.Add("local");

        control.ItemsSource = null;
        control.Measure(new MeasureContext(new LayoutSize(200, 100)));

        Assert.Equal(1, control.ItemCount);
        Assert.Equal("local", control.GetItemAt(0));
    }
}
