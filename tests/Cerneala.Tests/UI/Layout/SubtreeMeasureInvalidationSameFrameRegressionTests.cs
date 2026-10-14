using Cerneala.UI.Elements;
using Cerneala.UI.Invalidation;
using Cerneala.UI.Layout;
using StackPanel = Cerneala.UI.Layout.Panels.StackPanel;

namespace Cerneala.Tests.UI.Layout;

public sealed class SubtreeMeasureInvalidationSameFrameRegressionTests
{
    [Fact]
    public void SubtreeMeasureOnParentRemeasuresChildWithTheSameConstraint()
    {
        UIRoot root = new(100, 100);
        StackPanel parent = new();
        SizeElement child = new();
        parent.VisualChildren.Add(child);
        root.VisualChildren.Add(parent);
        Settle(root);
        Assert.Equal(1, child.MeasureCount);
        Assert.Equal(20, child.DesiredSize.Width);

        child.Width = 30;
        parent.Invalidate(InvalidationFlags.Measure | InvalidationFlags.Subtree, "subtree measure");
        root.ProcessFrame();

        Assert.Equal(2, child.MeasureCount);
        Assert.Equal(30, child.DesiredSize.Width);
        Assert.False(root.Scheduler.HasWork);
    }

    [Fact]
    public void SubtreeMeasureOnParentRemeasuresGrandchildWithTheSameConstraint()
    {
        UIRoot root = new(100, 100);
        StackPanel parent = new();
        CountingStackPanel middle = new();
        SizeElement grandchild = new();
        middle.VisualChildren.Add(grandchild);
        parent.VisualChildren.Add(middle);
        root.VisualChildren.Add(parent);
        Settle(root);
        Assert.Equal(1, middle.MeasureCount);
        Assert.Equal(1, grandchild.MeasureCount);
        Assert.Equal(20, grandchild.DesiredSize.Width);

        grandchild.Width = 30;
        parent.Invalidate(InvalidationFlags.Measure | InvalidationFlags.Subtree, "subtree measure");
        root.ProcessFrame();

        Assert.Equal(2, middle.MeasureCount);
        Assert.Equal(2, grandchild.MeasureCount);
        Assert.Equal(30, grandchild.DesiredSize.Width);
        Assert.Equal(30, middle.DesiredSize.Width);
        Assert.False(root.Scheduler.HasWork);
    }

    [Fact]
    public void SubtreeMeasureOnTargetRemeasuresItWhenItsParentMeasuresItFirst()
    {
        UIRoot root = new(100, 100);
        StackPanel parent = new();
        SizeElement child = new();
        parent.VisualChildren.Add(child);
        root.VisualChildren.Add(parent);
        Settle(root);
        Assert.Equal(1, child.MeasureCount);

        child.Width = 30;
        parent.Invalidate(InvalidationFlags.Measure, "parent measure");
        child.Invalidate(InvalidationFlags.Measure | InvalidationFlags.Subtree, "subtree measure");
        root.ProcessFrame();

        Assert.Equal(2, child.MeasureCount);
        Assert.Equal(30, child.DesiredSize.Width);
        Assert.False(root.Scheduler.HasWork);
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

    private sealed class CountingStackPanel : StackPanel
    {
        public int MeasureCount { get; private set; }

        protected override LayoutSize MeasureCore(MeasureContext context)
        {
            MeasureCount++;
            return base.MeasureCore(context);
        }
    }

    private sealed class SizeElement : UIElement
    {
        public float Width { get; set; } = 20;

        public int MeasureCount { get; private set; }

        protected override LayoutSize MeasureCore(MeasureContext context)
        {
            MeasureCount++;
            return new LayoutSize(Width, 10);
        }
    }
}
