using Cerneala.UI.Controls;
using Cerneala.UI.Elements;
using Cerneala.UI.Invalidation;
using Cerneala.UI.Layout;
using Cerneala.UI.Layout.Panels;
using Cerneala.UI.Layout.Virtualization;

namespace Cerneala.Tests.Controls;

public sealed class ItemsControlRetainedInvalidationTests
{
    [Fact]
    public void ItemsControlItemAddInvalidatesMeasureArrangeRenderAndHitTest()
    {
        UIRoot root = RootWithItemsControl(out ItemsControl itemsControl);
        root.ProcessFrame();

        itemsControl.Items.Add("added");

        Assert.Contains(itemsControl, root.LayoutQueue.SnapshotMeasure());
        Assert.Contains(itemsControl, root.LayoutQueue.SnapshotArrange());
        Assert.Contains(itemsControl, root.RenderQueue.Snapshot());
        Assert.Contains(itemsControl, root.HitTestQueue.Snapshot());

        FrameStats stats = root.ProcessFrame();

        Assert.True(stats.MeasuredElements > 0);
        Assert.True(stats.ArrangedElements > 0);
        Assert.True(stats.RenderedElements > 0);
        Assert.True(stats.HitTestElements > 0);
    }

    [Fact]
    public void ItemsControlSecondUnchangedFrameDoesNoRetainedWork()
    {
        UIRoot root = RootWithItemsControl(out ItemsControl itemsControl);
        itemsControl.SetItems(new[] { "one", "two", "three" });

        root.ProcessFrame();
        FrameStats second = root.ProcessFrame();

        Assert.Equal(0, second.MeasuredElements);
        Assert.Equal(0, second.ArrangedElements);
        Assert.Equal(0, second.RenderedElements);
        Assert.Equal(0, second.HitTestElements);
        Assert.Equal(1, second.NoWorkFrames);
    }

    [Fact]
    public void ListBoxSelectionInvalidatesOnlyOldAndNewRealizedContainers()
    {
        UIRoot root = new(100, 100);
        ListBox listBox = new();
        listBox.SetItems(new[] { "zero", "one", "two", "three" });
        root.VisualChildren.Add(listBox);
        root.ProcessFrame();
        listBox.SelectedIndex = 1;
        root.ProcessFrame();
        ListBoxItem oldSelected = Assert.IsType<ListBoxItem>(listBox.ItemContainerGenerator.RealizedContainers[1]);
        ListBoxItem newSelected = Assert.IsType<ListBoxItem>(listBox.ItemContainerGenerator.RealizedContainers[3]);

        listBox.SelectedIndex = 3;

        IReadOnlyList<UIElement> renderQueue = root.RenderQueue.Snapshot();
        Assert.Contains(oldSelected, renderQueue);
        Assert.Contains(newSelected, renderQueue);
        Assert.DoesNotContain(listBox, renderQueue);
        Assert.Equal(2, renderQueue.Count(element => element is ListBoxItem));
    }

    [Fact]
    public void VirtualizedItemsControlRealizesOnlyVisibleWindow()
    {
        ItemsControl control = new()
        {
            ItemsPanel = new ItemsPanelTemplate(() => new VirtualizingStackPanel())
        };
        control.SetItems(Enumerable.Range(0, 100).Cast<object>());
        control.SetVirtualizationContext(new VirtualizationContext(100, 12, 36, 24, CacheItems: 1));

        control.Measure(new MeasureContext(new LayoutSize(100, 36)));

        Assert.Equal(new RealizationWindow(1, 6), control.ItemsPresenter.CurrentRealizationWindow);
        Assert.Equal(5, control.ItemsPresenter.LayoutPanelRoot!.VisualChildren.Count);
        Assert.Equal(1, ItemContainerGenerator.GetItemIndex(control.ItemsPresenter.LayoutPanelRoot.VisualChildren[0]));
        Assert.Equal(5, ItemContainerGenerator.GetItemIndex(control.ItemsPresenter.LayoutPanelRoot.VisualChildren[^1]));
    }

    private static UIRoot RootWithItemsControl(out ItemsControl itemsControl)
    {
        UIRoot root = new(100, 100);
        itemsControl = new ItemsControl
        {
            ItemsPanel = new ItemsPanelTemplate(() => new Cerneala.UI.Layout.Panels.StackPanel()),
            ItemTemplate = new DataTemplate<string>(text => new FixedElement(text))
        };
        itemsControl.SetItems(new[] { "one", "two" });
        root.VisualChildren.Add(itemsControl);
        return root;
    }

    private sealed class FixedElement(string text) : UIElement
    {
        public string Text { get; } = text;

        protected override LayoutSize MeasureCore(MeasureContext context)
        {
            return new LayoutSize(40, 10);
        }
    }
}
