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
        ClearInitialMutationWork(root);

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
        ClearInitialMutationWork(root);

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
        ClearInitialMutationWork(root);

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
        ClearInitialMutationWork(root);

        root.LayoutQueue.EnqueueMeasure(child);
        root.LayoutQueue.EnqueueArrange(child);
        root.VisualChildren.Remove(child);

        Assert.DoesNotContain(child, root.LayoutQueue.SnapshotMeasure());
        Assert.DoesNotContain(child, root.LayoutQueue.SnapshotArrange());
        Assert.Contains(root, root.LayoutQueue.SnapshotMeasure());
        Assert.Contains(root, root.LayoutQueue.SnapshotArrange());
        Assert.Equal(1, root.LayoutQueue.MeasureCount);
        Assert.Equal(1, root.LayoutQueue.ArrangeCount);
    }

    private static void ClearInitialMutationWork(UIRoot root)
    {
        root.ProcessFrame();
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
