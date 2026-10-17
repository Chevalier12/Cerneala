using Cerneala.UI.Controls;
using Cerneala.UI.Detective;
using Cerneala.UI.Elements;
using Cerneala.UI.Invalidation;
using Cerneala.UI.Layout;
using StackPanel = Cerneala.UI.Layout.Panels.StackPanel;

namespace Cerneala.Tests.Controls;

// The presenter measures and arranges its panel subtree itself and removes the queued layout work of that subtree
// and of its ancestors, so it must also clear the dirty flags that the scheduler clears for completed work.
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
