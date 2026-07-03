using Cerneala.UI.Elements;
using Cerneala.UI.Invalidation;

namespace Cerneala.Tests.UI.Elements;

public sealed class UIElementCollectionInvalidationTests
{
    [Fact]
    public void AttachedVisualChildAddInvalidatesOwnerAndAddedSubtree()
    {
        UIRoot root = new();
        UIElement parent = new();
        root.VisualChildren.Add(parent);
        ProcessInitialFrame(root);
        UIElement child = new();
        UIElement grandchild = new();
        child.VisualChildren.Add(grandchild);

        parent.VisualChildren.Add(child);

        Assert.Contains(parent, root.LayoutQueue.SnapshotMeasure());
        Assert.Contains(parent, root.LayoutQueue.SnapshotArrange());
        Assert.Contains(parent, root.RenderQueue.Snapshot());
        Assert.Contains(parent, root.HitTestQueue.Snapshot());
        Assert.Contains(child, root.LayoutQueue.SnapshotMeasure());
        Assert.Contains(child, root.LayoutQueue.SnapshotArrange());
        Assert.Contains(child, root.RenderQueue.Snapshot());
        Assert.Contains(child, root.HitTestQueue.Snapshot());
        Assert.Contains(grandchild, root.LayoutQueue.SnapshotMeasure());
        Assert.Contains(grandchild, root.LayoutQueue.SnapshotArrange());
        Assert.Contains(grandchild, root.RenderQueue.Snapshot());
        Assert.Contains(grandchild, root.HitTestQueue.Snapshot());
    }

    [Fact]
    public void AttachedVisualChildRemoveInvalidatesNonRootOwner()
    {
        UIRoot root = new();
        UIElement parent = new();
        UIElement child = new();
        root.VisualChildren.Add(parent);
        parent.VisualChildren.Add(child);
        ProcessInitialFrame(root);

        parent.VisualChildren.Remove(child);

        Assert.Contains(parent, root.LayoutQueue.SnapshotMeasure());
        Assert.Contains(parent, root.LayoutQueue.SnapshotArrange());
        Assert.Contains(parent, root.RenderQueue.Snapshot());
        Assert.Contains(parent, root.HitTestQueue.Snapshot());
        Assert.DoesNotContain(child, root.LayoutQueue.SnapshotMeasure());
        Assert.DoesNotContain(child, root.LayoutQueue.SnapshotArrange());
        Assert.DoesNotContain(child, root.RenderQueue.Snapshot());
        Assert.DoesNotContain(child, root.HitTestQueue.Snapshot());
    }

    [Fact]
    public void AttachedVisualChildRemoveInvalidatesRootOwner()
    {
        UIRoot root = new();
        UIElement child = new();
        root.VisualChildren.Add(child);
        ProcessInitialFrame(root);

        root.VisualChildren.Remove(child);

        Assert.Contains(root, root.LayoutQueue.SnapshotMeasure());
        Assert.Contains(root, root.LayoutQueue.SnapshotArrange());
        Assert.Contains(root, root.RenderQueue.Snapshot());
        Assert.Contains(root, root.HitTestQueue.Snapshot());
        Assert.DoesNotContain(child, root.LayoutQueue.SnapshotMeasure());
        Assert.DoesNotContain(child, root.LayoutQueue.SnapshotArrange());
        Assert.DoesNotContain(child, root.RenderQueue.Snapshot());
        Assert.DoesNotContain(child, root.HitTestQueue.Snapshot());
    }

    [Fact]
    public void DetachedVisualChildMutationDoesNotQueueRetainedWork()
    {
        UIElement parent = new();
        UIElement child = new();

        parent.VisualChildren.Add(child);

        Assert.Null(parent.Root);
        Assert.Null(child.Root);
    }

    private static void ProcessInitialFrame(UIRoot root)
    {
        root.Invalidate(
            InvalidationFlags.Measure |
            InvalidationFlags.Arrange |
            InvalidationFlags.Render |
            InvalidationFlags.HitTest |
            InvalidationFlags.Subtree,
            "Initial test frame");
        root.ProcessFrame();
        Assert.Equal(0, root.LayoutQueue.MeasureCount);
        Assert.Equal(0, root.LayoutQueue.ArrangeCount);
        Assert.Equal(0, root.RenderQueue.Count);
        Assert.Equal(0, root.HitTestQueue.Count);
    }
}
