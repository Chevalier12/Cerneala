using Cerneala.UI.Elements;

namespace Cerneala.Tests.UI.Invalidation;

public sealed class ElementQueueContractTests
{
    [Theory]
    [InlineData(SimpleQueueKind.Render)]
    [InlineData(SimpleQueueKind.Aspect)]
    [InlineData(SimpleQueueKind.HitTest)]
    [InlineData(SimpleQueueKind.InheritedProperty)]
    [InlineData(SimpleQueueKind.CommandState)]
    public void SimpleWrappersShareDeduplicationOrderingAndReenqueueContract(SimpleQueueKind kind)
    {
        UIRoot root = new();
        UIElement parent = new();
        UIElement child = new();
        root.VisualChildren.Add(parent);
        parent.VisualChildren.Add(child);
        root.ProcessFrame();
        QueueAdapter queue = QueueAdapter.Create(root, kind);

        queue.Enqueue(child);
        queue.Enqueue(parent);
        queue.Enqueue(child);

        Assert.True(queue.HasWork());
        Assert.Equal(2, queue.Count());
        Assert.Equal([parent, child], queue.Snapshot());

        queue.Remove(parent);
        queue.Remove(parent);
        queue.Remove(child);
        queue.Remove(null!);
        Assert.False(queue.HasWork());

        queue.Enqueue(child);
        Assert.Equal([child], queue.Snapshot());
    }

    [Fact]
    public void DetachActivelyRemovesWholeSubtreeFromEveryQueue()
    {
        UIRoot root = new();
        UIElement subtree = new();
        UIElement descendant = new();
        UIElement sibling = new();
        root.VisualChildren.Add(subtree);
        root.VisualChildren.Add(sibling);
        subtree.VisualChildren.Add(descendant);
        root.ProcessFrame();

        foreach (UIElement element in new[] { subtree, descendant })
        {
            root.LayoutQueue.EnqueueMeasure(element);
            root.LayoutQueue.EnqueueArrange(element);
            root.InheritedPropertyQueue.Enqueue(element);
            root.CommandStateQueue.Enqueue(element);
            root.AspectQueue.Enqueue(element);
            root.RenderQueue.Enqueue(element);
            root.HitTestQueue.Enqueue(element);
        }

        root.RenderQueue.Enqueue(sibling);
        root.VisualChildren.Remove(subtree);

        AssertQueuesExclude(root, subtree, descendant);
        Assert.Contains(sibling, root.RenderQueue.Snapshot());
        Assert.Contains(root, root.LayoutQueue.SnapshotMeasure());
        Assert.Contains(root, root.LayoutQueue.SnapshotArrange());
        Assert.False(root.CommandStateQueue.HasWork);
    }

    [Fact]
    public void ReattachedSubtreeAcceptsFreshWork()
    {
        UIRoot root = new();
        UIElement child = new();
        root.VisualChildren.Add(child);
        root.ProcessFrame();
        root.RenderQueue.Enqueue(child);
        root.VisualChildren.Remove(child);

        root.VisualChildren.Add(child);
        root.ProcessFrame();
        root.RenderQueue.Enqueue(child);

        Assert.Equal([child], root.RenderQueue.Snapshot());
    }

    [Fact]
    public void DetachCleanupRunsAfterLifecycleCallbacks()
    {
        UIRoot root = new();
        RequeueOnDetachElement child = new();
        root.VisualChildren.Add(child);
        root.ProcessFrame();

        root.VisualChildren.Remove(child);

        Assert.DoesNotContain(child, root.RenderQueue.Snapshot());
        Assert.DoesNotContain(child, root.LayoutQueue.SnapshotMeasure());
    }

    private static void AssertQueuesExclude(UIRoot root, params UIElement[] detached)
    {
        IReadOnlyList<UIElement>[] snapshots =
        [
            root.LayoutQueue.SnapshotMeasure(),
            root.LayoutQueue.SnapshotArrange(),
            root.InheritedPropertyQueue.Snapshot(),
            root.CommandStateQueue.Snapshot(),
            root.AspectQueue.Snapshot(),
            root.RenderQueue.Snapshot(),
            root.HitTestQueue.Snapshot()
        ];

        foreach (IReadOnlyList<UIElement> snapshot in snapshots)
        {
            foreach (UIElement element in detached)
            {
                Assert.DoesNotContain(element, snapshot);
            }
        }
    }

    public enum SimpleQueueKind
    {
        Render,
        Aspect,
        HitTest,
        InheritedProperty,
        CommandState
    }

    private sealed record QueueAdapter(
        Action<UIElement> Enqueue,
        Action<UIElement> Remove,
        Func<IReadOnlyList<UIElement>> Snapshot,
        Func<int> Count,
        Func<bool> HasWork)
    {
        public static QueueAdapter Create(UIRoot root, SimpleQueueKind kind)
        {
            return kind switch
            {
                SimpleQueueKind.Render => new(root.RenderQueue.Enqueue, root.RenderQueue.Remove, root.RenderQueue.Snapshot, () => root.RenderQueue.Count, () => root.RenderQueue.HasWork),
                SimpleQueueKind.Aspect => new(root.AspectQueue.Enqueue, root.AspectQueue.Remove, root.AspectQueue.Snapshot, () => root.AspectQueue.Count, () => root.AspectQueue.HasWork),
                SimpleQueueKind.HitTest => new(root.HitTestQueue.Enqueue, root.HitTestQueue.Remove, root.HitTestQueue.Snapshot, () => root.HitTestQueue.Count, () => root.HitTestQueue.HasWork),
                SimpleQueueKind.InheritedProperty => new(root.InheritedPropertyQueue.Enqueue, root.InheritedPropertyQueue.Remove, root.InheritedPropertyQueue.Snapshot, () => root.InheritedPropertyQueue.Count, () => root.InheritedPropertyQueue.HasWork),
                SimpleQueueKind.CommandState => new(root.CommandStateQueue.Enqueue, root.CommandStateQueue.Remove, root.CommandStateQueue.Snapshot, () => root.CommandStateQueue.Count, () => root.CommandStateQueue.HasWork),
                _ => throw new ArgumentOutOfRangeException(nameof(kind))
            };
        }
    }

    private sealed class RequeueOnDetachElement : UIElement
    {
        protected override void OnDetached()
        {
            Root!.RenderQueue.Enqueue(this);
            Root.LayoutQueue.EnqueueMeasure(this);
        }
    }
}
