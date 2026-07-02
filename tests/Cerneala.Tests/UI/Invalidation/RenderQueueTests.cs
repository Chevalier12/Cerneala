using Cerneala.UI.Elements;
using Cerneala.UI.Invalidation;

namespace Cerneala.Tests.UI.Invalidation;

public sealed class RenderQueueTests
{
    [Fact]
    public void RenderQueueDeduplicatesByReference()
    {
        UIRoot root = new();
        UIElement child = new();
        root.VisualChildren.Add(child);

        root.RenderQueue.Enqueue(child);
        root.RenderQueue.Enqueue(child);

        Assert.Equal(1, root.RenderQueue.Count);
    }

    [Fact]
    public void RenderOnlyInvalidationDoesNotScheduleMeasure()
    {
        UIRoot root = new();
        UIElement child = new();
        root.VisualChildren.Add(child);

        child.Invalidate(InvalidationFlags.Render, "render");

        Assert.Equal(0, root.LayoutQueue.MeasureCount);
        Assert.Equal(1, root.RenderQueue.Count);
    }

    [Fact]
    public void RenderOrderIsDeterministic()
    {
        UIRoot root = new();
        UIElement first = new();
        UIElement second = new();
        root.VisualChildren.Add(first);
        root.VisualChildren.Add(second);

        root.RenderQueue.Enqueue(second);
        root.RenderQueue.Enqueue(first);

        Assert.Equal([first, second], root.RenderQueue.Snapshot());
    }

    [Fact]
    public void DetachedElementsAreRemovedFromRenderWork()
    {
        UIRoot root = new();
        UIElement child = new();
        root.VisualChildren.Add(child);

        root.RenderQueue.Enqueue(child);
        root.VisualChildren.Remove(child);

        Assert.Empty(root.RenderQueue.Snapshot());
        Assert.Equal(0, root.RenderQueue.Count);
    }
}
