using Cerneala.UI.Controls;
using Cerneala.UI.Detective;
using Cerneala.UI.Elements;
using Cerneala.UI.Invalidation;
using Cerneala.UI.Layout;
using StackPanel = Cerneala.UI.Layout.Panels.StackPanel;
using VirtualizingStackPanel = Cerneala.UI.Layout.Panels.VirtualizingStackPanel;

namespace Cerneala.Tests.Controls;

// The presenter measures and arranges its panel subtree itself and removes the queued layout work of that subtree
// and the measure work propagated to its ancestors, so it must also clear the dirty flags that the scheduler clears
// for completed work. An ancestor's own queued measure is not covered and stays queued.
public sealed class ItemsPresenterDirtyStateRegressionTests
{
    [Fact]
    public void ItemsControlLeavesNoDirtyElementAfterTheFirstLayout()
    {
        (UIRoot root, _, _) = CreateTree(CreateItemsControl());

        Settle(root);

        Assert.Empty(DirtyElements(root));
    }

    [Fact]
    public void ItemsControlLeavesNoDirtyElementAfterAMeasureInvalidation()
    {
        (UIRoot root, CountingPanel outer, UIElement items) = CreateTree(CreateItemsControl());
        Settle(root);

        items.Invalidate(InvalidationFlags.Measure, "test");
        Settle(root);

        Assert.Empty(DirtyElements(root));
        Assert.Equal(InvalidationFlags.None, outer.DirtyState.Flags);
        Assert.Equal("Dirty tree" + Environment.NewLine + "- none", new DirtyTreeDumper().Dump(root));
    }

    [Fact]
    public void ListBoxLeavesNoDirtyElementAfterTheFirstLayoutAndAfterAMeasureInvalidation()
    {
        ListBox list = new();
        list.SetItems(new[] { "a", "b", "c" });
        (UIRoot root, _, _) = CreateTree(list);

        Settle(root);
        Assert.Empty(DirtyElements(root));

        list.Invalidate(InvalidationFlags.Measure, "test");
        Settle(root);
        Assert.Empty(DirtyElements(root));
    }

    [Fact]
    public void QueuedMeasureOfAnUnchangedAncestorReusesItsMeasureCache()
    {
        (UIRoot root, CountingPanel outer, UIElement items) = CreateTree(CreateItemsControl());
        Settle(root);
        items.Invalidate(InvalidationFlags.Measure, "test");
        Settle(root);
        int measureCount = outer.MeasureCount;

        root.LayoutQueue.EnqueueMeasure(outer);
        Settle(root);

        Assert.Equal(measureCount, outer.MeasureCount);
    }

    // Issue #400: the ItemsControl is measured first because it is deeper, and its presenter must not drop the
    // measure that the ancestor queued for itself in the same frame.
    [Fact]
    public void AncestorWithItsOwnQueuedMeasureIsMeasuredAgainWhenTheItemsKeepTheirSize()
    {
        (UIRoot root, CountingPanel outer, UIElement items) = CreateTree(CreateSizedItemsControl());
        outer.VisualChildren.Add(new Border { Width = 50, Height = 20 });
        Settle(root);
        Assert.Equal(new LayoutSize(50, 42), outer.DesiredSize);
        int measureCount = outer.MeasureCount;

        outer.Orientation = Orientation.Horizontal;
        items.Invalidate(InvalidationFlags.Measure, "test");
        Settle(root);

        Assert.Equal(measureCount + 1, outer.MeasureCount);
        Assert.Equal(new LayoutSize(59, 22), outer.DesiredSize);
        Assert.Empty(DirtyElements(root));
    }

    [Fact]
    public void AncestorMeasureInvalidatedInTheSameFrameAsItsItemsControlRunsOnce()
    {
        (UIRoot root, CountingPanel outer, UIElement items) = CreateTree(CreateSizedItemsControl());
        Settle(root);
        int measureCount = outer.MeasureCount;

        outer.Invalidate(InvalidationFlags.Measure, "test");
        items.Invalidate(InvalidationFlags.Measure, "test");
        Settle(root);

        Assert.Equal(measureCount + 1, outer.MeasureCount);
        Assert.Empty(DirtyElements(root));
    }

    // Issue #487: the scroll presenter invalidates the ItemsControl's items for the new viewport during its own
    // measure and then measures the ItemsControl, so that measure completes the ItemsControl's queued measure.
    [Theory]
    [InlineData(300f)]
    [InlineData(120f)]
    public void VirtualizingItemsControlInAScrollViewerSettlesInTheFrameThatMeasuresANewViewportHeight(float height)
    {
        ItemsControl items = new() { ItemsPanel = new VirtualizingStackPanel() };
        items.SetItems(Enumerable.Range(0, 1000).Select(_ => (object)new Border { Height = 20 }).ToArray());
        ScrollViewer viewer = new() { Height = 100, Content = items };
        UIRoot root = new(300, 400);
        root.VisualChildren.Add(viewer);
        Settle(root);
        viewer.ScrollInfo.SetVerticalOffset(500);
        Settle(root);

        viewer.Height = height;
        root.ProcessFrame();

        Assert.Empty(DirtyElements(root));
        Assert.False(root.Scheduler.HasWork);
    }

    private static ItemsControl CreateSizedItemsControl()
    {
        ItemsControl items = new();
        items.SetItems(new object[] { new Border { Width = 9, Height = 22 } });
        return items;
    }

    private static ItemsControl CreateItemsControl()
    {
        ItemsControl items = new();
        items.SetItems(new[] { "a" });
        return items;
    }

    private static (UIRoot Root, CountingPanel Outer, UIElement Child) CreateTree(UIElement child)
    {
        UIRoot root = new(300, 200);
        CountingPanel outer = new();
        outer.VisualChildren.Add(child);
        root.VisualChildren.Add(outer);
        return (root, outer, child);
    }

    private static string[] DirtyElements(UIRoot root)
    {
        return ElementTreeWalker.PreOrder(root)
            .Where(element => element.DirtyState.IsDirty)
            .Select(element => $"{element.GetType().Name}: {element.DirtyState.Flags}")
            .ToArray();
    }

    private static void Settle(UIRoot root)
    {
        root.ProcessFrame();
        for (int frame = 0; frame < 20 && root.Scheduler.HasWork; frame++)
        {
            root.ProcessFrame();
        }

        Assert.False(root.Scheduler.HasWork);
    }

    private sealed class CountingPanel : StackPanel
    {
        public int MeasureCount { get; private set; }

        protected override LayoutSize MeasureCore(MeasureContext context)
        {
            MeasureCount++;
            return base.MeasureCore(context);
        }
    }
}
