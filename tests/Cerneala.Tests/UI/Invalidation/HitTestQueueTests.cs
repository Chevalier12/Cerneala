using Cerneala.UI.Elements;
using Cerneala.UI.Invalidation;

namespace Cerneala.Tests.UI.Invalidation;

public sealed class HitTestQueueTests
{
    [Fact]
    public void HitTestQueueDeduplicatesByReference()
    {
        UIRoot root = new();
        UIElement child = new();
        root.VisualChildren.Add(child);
        ClearInitialMutationWork(root);

        root.HitTestQueue.Enqueue(child);
        root.HitTestQueue.Enqueue(child);

        Assert.Equal(1, root.HitTestQueue.Count);
    }

    [Fact]
    public void HitTestQueueProcessesSeparatelyFromRender()
    {
        UIRoot root = new();
        UIElement child = new();
        root.VisualChildren.Add(child);
        ClearInitialMutationWork(root);

        child.Invalidate(InvalidationFlags.HitTest, "hit");

        Assert.Equal(1, root.HitTestQueue.Count);
        Assert.Equal(0, root.RenderQueue.Count);
    }

    [Fact]
    public void DetachedElementsAreRemovedFromHitTestWork()
    {
        UIRoot root = new();
        UIElement child = new();
        root.VisualChildren.Add(child);
        ClearInitialMutationWork(root);

        root.HitTestQueue.Enqueue(child);
        root.VisualChildren.Remove(child);

        Assert.DoesNotContain(child, root.HitTestQueue.Snapshot());
        Assert.Contains(root, root.HitTestQueue.Snapshot());
        Assert.Equal(1, root.HitTestQueue.Count);
    }

    private static void ClearInitialMutationWork(UIRoot root)
    {
        root.ProcessFrame();
    }
}
