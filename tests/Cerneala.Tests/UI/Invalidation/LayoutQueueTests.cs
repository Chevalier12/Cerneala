using Cerneala.UI.Elements;
using Cerneala.UI.Invalidation;

namespace Cerneala.Tests.UI.Invalidation;

public sealed class LayoutQueueTests
{
    [Fact]
    public void SameElementIsQueuedOnce()
    {
        UIRoot root = new();
        UIElement child = new();
        root.VisualChildren.Add(child);

        root.LayoutQueue.EnqueueMeasure(child);
        root.LayoutQueue.EnqueueMeasure(child);

        Assert.Equal(1, root.LayoutQueue.MeasureCount);
    }

    [Fact]
    public void EqualValueElementsRemainDistinct()
    {
        UIRoot root = new();
        EqualValueElement first = new(1);
        EqualValueElement second = new(1);
        root.VisualChildren.Add(first);
        root.VisualChildren.Add(second);

        root.LayoutQueue.EnqueueMeasure(first);
        root.LayoutQueue.EnqueueMeasure(second);

        Assert.Equal(2, root.LayoutQueue.MeasureCount);
    }

    [Fact]
    public void DrainOrderFollowsVisualTree()
    {
        UIRoot root = new();
        UIElement parent = new();
        UIElement child = new();
        root.VisualChildren.Add(parent);
        parent.VisualChildren.Add(child);

        root.LayoutQueue.EnqueueMeasure(child);
        root.LayoutQueue.EnqueueMeasure(parent);

        Assert.Equal([parent, child], root.LayoutQueue.SnapshotMeasure());
    }

    [Fact]
    public void DetachedElementsAreRemovedFromLayoutWork()
    {
        UIRoot root = new();
        UIElement child = new();
        root.VisualChildren.Add(child);

        root.LayoutQueue.EnqueueMeasure(child);
        root.LayoutQueue.EnqueueArrange(child);
        root.VisualChildren.Remove(child);

        Assert.Empty(root.LayoutQueue.SnapshotMeasure());
        Assert.Empty(root.LayoutQueue.SnapshotArrange());
        Assert.Equal(0, root.LayoutQueue.MeasureCount);
        Assert.Equal(0, root.LayoutQueue.ArrangeCount);
    }

    private sealed class EqualValueElement(int value) : UIElement
    {
        public override bool Equals(object? obj)
        {
            return obj is EqualValueElement other && other.GetHashCode() == value;
        }

        public override int GetHashCode()
        {
            return value;
        }
    }
}
