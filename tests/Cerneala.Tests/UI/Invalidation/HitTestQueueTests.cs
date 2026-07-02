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

        root.HitTestQueue.Enqueue(child);
        root.VisualChildren.Remove(child);

        Assert.Empty(root.HitTestQueue.Snapshot());
        Assert.Equal(0, root.HitTestQueue.Count);
    }
}
