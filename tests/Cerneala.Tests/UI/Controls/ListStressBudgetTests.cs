using Cerneala.UI.Controls;
using Cerneala.UI.Data;
using Cerneala.UI.Elements;
using Cerneala.UI.Invalidation;
using Cerneala.UI.Layout;
using Cerneala.UI.Layout.Panels;
using Cerneala.UI.Layout.Virtualization;

namespace Cerneala.Tests.UI.Controls;

public sealed class ListStressBudgetTests
{
    private const int ItemCount = 500;
    private const int VisibleRealizedContainers = 6;
    private const float ItemExtent = 10;
    private const float ViewportExtent = 50;

    [Fact]
    public void LargeObservableListInitialFrameRealizesOnlyVisibleWindow()
    {
        UIRoot root = RootWithList(out ItemsControl list, out _);

        root.ProcessFrame();

        Assert.Equal(new RealizationWindow(0, VisibleRealizedContainers), list.ItemsPresenter.CurrentRealizationWindow);
        Assert.Equal(VisibleRealizedContainers, list.ItemContainerGenerator.RealizedContainers.Count);
        Assert.True(list.ItemContainerGenerator.RealizedContainers.Count < ItemCount);
    }

    [Fact]
    public void LargeObservableListAppendDoesNotRecreateUnrelatedRealizedContainers()
    {
        UIRoot root = RootWithList(out ItemsControl list, out ObservableList<string> items);
        root.ProcessFrame();
        Dictionary<int, UIElement> before = SnapshotRealizedContainers(list);

        items.Add("Appended row");
        FrameStats stats = root.ProcessFrame();

        AssertSameRealizedContainers(before, list);
        Assert.Equal(VisibleRealizedContainers, list.ItemContainerGenerator.RealizedContainers.Count);
        Assert.True(stats.MeasuredElements <= VisibleRealizedContainers + 2, $"Measured {stats.MeasuredElements} elements for append outside the window.");
        Assert.True(stats.RenderedElements <= 18, $"Rendered {stats.RenderedElements} elements for append outside the window.");
    }

    [Fact]
    public void LargeObservableListReplaceUpdatesOnlyCompatibleRealizedContainerWithinBudget()
    {
        UIRoot root = RootWithList(out ItemsControl list, out ObservableList<string> items);
        root.ProcessFrame();
        int targetIndex = 2;
        UIElement container = list.ItemContainerGenerator.RealizedContainers[targetIndex];

        items[targetIndex] = "Replacement row";
        FrameStats stats = root.ProcessFrame();

        Assert.Same(container, list.ItemContainerGenerator.RealizedContainers[targetIndex]);
        Assert.Equal("Replacement row", ItemContainerGenerator.GetItem(container));
        Assert.True(stats.MeasuredElements <= 9, $"Measured {stats.MeasuredElements} elements for one realized replacement.");
        Assert.True(stats.RenderedElements <= 18, $"Rendered {stats.RenderedElements} elements for one realized replacement.");
    }

    [Fact]
    public void LargeListScrollMovesRealizationWindowWithinBudget()
    {
        UIRoot root = RootWithList(out ItemsControl list, out ObservableList<string> items);
        root.ProcessFrame();

        list.SetVirtualizationContext(new VirtualizationContext(items.Count, ItemExtent, ViewportExtent, 120, CacheItems: 1));
        FrameStats stats = root.ProcessFrame();

        Assert.Equal(new RealizationWindow(11, 18), list.ItemsPresenter.CurrentRealizationWindow);
        Assert.Equal(7, list.ItemContainerGenerator.RealizedContainers.Count);
        Assert.Equal(11, ItemContainerGenerator.GetItemIndex(list.ItemsPresenter.LayoutPanelRoot!.VisualChildren[0]));
        Assert.True(stats.MeasuredElements <= 9, $"Measured {stats.MeasuredElements} elements for one list scroll.");
        Assert.True(stats.ArrangedElements <= 21, $"Arranged {stats.ArrangedElements} elements for one list scroll.");
        Assert.True(stats.RenderedElements <= 21, $"Rendered {stats.RenderedElements} elements for one list scroll.");
    }

    [Fact]
    public void LargeListScrollInsideSameRealizationWindowDoesNotRebuildItems()
    {
        UIRoot root = RootWithList(out ItemsControl list, out ObservableList<string> items);
        list.SetVirtualizationContext(new VirtualizationContext(items.Count, ItemExtent, ViewportExtent, 1, CacheItems: 1));
        root.ProcessFrame();
        Cerneala.UI.Layout.Panels.Panel panel = list.ItemsPresenter.LayoutPanelRoot!;
        Dictionary<int, UIElement> before = SnapshotRealizedContainers(list);
        TestScrollInfo scrollInfo = new()
        {
            ExtentHeight = items.Count * ItemExtent,
            ViewportHeight = ViewportExtent,
            VerticalOffset = 2
        };

        list.UpdateVirtualizationFromScrollInfo(scrollInfo, ItemExtent, cacheItems: 1);
        FrameStats stats = root.ProcessFrame();

        Assert.Equal(new RealizationWindow(0, 7), list.ItemsPresenter.CurrentRealizationWindow);
        Assert.Same(panel, list.ItemsPresenter.LayoutPanelRoot);
        AssertSameRealizedContainers(before, list);
        Assert.Equal(0, stats.MeasuredElements);
        Assert.Equal(0, stats.ArrangedElements);
        Assert.Equal(0, stats.RenderedElements);
    }

    private static UIRoot RootWithList(out ItemsControl list, out ObservableList<string> items)
    {
        items = LargeList(ItemCount);
        list = new ItemsControl
        {
            ItemsSource = items,
            ItemsPanel = new ItemsPanelTemplate(() => new VirtualizingStackPanel())
        };
        list.SetVirtualizationContext(new VirtualizationContext(items.Count, ItemExtent, ViewportExtent, 0, CacheItems: 1));
        UIRoot root = new(200, 60);
        root.VisualChildren.Add(list);
        return root;
    }

    private static ObservableList<string> LargeList(int count)
    {
        return new ObservableList<string>(Enumerable.Range(0, count).Select(index => $"Row {index}"));
    }

    private static Dictionary<int, UIElement> SnapshotRealizedContainers(ItemsControl list)
    {
        return list.ItemContainerGenerator.RealizedContainers.ToDictionary(pair => pair.Key, pair => pair.Value);
    }

    private static void AssertSameRealizedContainers(Dictionary<int, UIElement> before, ItemsControl list)
    {
        foreach ((int index, UIElement container) in before)
        {
            Assert.True(list.ItemContainerGenerator.RealizedContainers.TryGetValue(index, out UIElement? current), $"Index {index} was unrealized.");
            Assert.Same(container, current);
        }
    }

    private sealed class TestScrollInfo : IScrollInfo
    {
        public float HorizontalOffset { get; private set; }

        public float VerticalOffset { get; set; }

        public float ExtentWidth { get; set; }

        public float ExtentHeight { get; set; }

        public float ViewportWidth { get; set; }

        public float ViewportHeight { get; set; }

        public bool CanHorizontallyScroll { get; set; } = true;

        public bool CanVerticallyScroll { get; set; } = true;

        public void SetHorizontalOffset(float offset)
        {
            HorizontalOffset = offset;
        }

        public void SetVerticalOffset(float offset)
        {
            VerticalOffset = offset;
        }
    }
}
