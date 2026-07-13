using Cerneala.UI.Elements;
using Cerneala.UI.Invalidation;

namespace Cerneala.Tests.UI.Invalidation;

public sealed class ElementQueueOrderIndexTests
{
    [Fact]
    public void HasWorkDoesNotBuildVisualOrderIndex()
    {
        UIRoot root = new();

        for (int i = 0; i < 100; i++)
        {
            Assert.False(root.Scheduler.HasWork);
        }

        Assert.Equal(0, root.QueueOrderIndex.BuildCount);
        Assert.Equal(0, root.QueueOrderIndex.TotalVisitedNodeCount);
    }

    [Fact]
    public void QueuesShareOneIndexBuildForSameTreeVersion()
    {
        UIRoot root = new();
        UIElement first = new();
        UIElement second = new();
        root.VisualChildren.Add(first);
        root.VisualChildren.Add(second);

        _ = root.RenderQueue.Snapshot();
        int builds = root.QueueOrderIndex.BuildCount;
        _ = root.AspectQueue.Snapshot();
        _ = root.HitTestQueue.Snapshot();
        _ = root.LayoutQueue.SnapshotMeasure();
        _ = root.LayoutQueue.SnapshotArrange();

        Assert.Equal(1, builds);
        Assert.Equal(builds, root.QueueOrderIndex.BuildCount);
        Assert.Equal(3, root.QueueOrderIndex.LastVisitedNodeCount);
    }

    [Fact]
    public void TreeMutationRebuildsOnceAndReflectsNewVisualOrder()
    {
        UIRoot root = new();
        UIElement first = new();
        UIElement second = new();
        root.VisualChildren.Add(first);
        root.VisualChildren.Add(second);
        root.ProcessFrame();
        int buildsBeforeMutation = root.QueueOrderIndex.BuildCount;

        root.VisualChildren.Move(1, 0);
        root.RenderQueue.Remove(root);
        root.RenderQueue.Enqueue(first);
        root.RenderQueue.Enqueue(second);

        Assert.Equal([second, first], root.RenderQueue.Snapshot());
        _ = root.AspectQueue.Snapshot();
        Assert.Equal(buildsBeforeMutation + 1, root.QueueOrderIndex.BuildCount);
    }

    [Fact]
    public void DetachedElementHasNoOrdinalAfterRebuild()
    {
        UIRoot root = new();
        UIElement child = new();
        root.VisualChildren.Add(child);
        root.ProcessFrame();

        root.VisualChildren.Remove(child);
        root.QueueOrderIndex.EnsureCurrent();

        Assert.False(root.QueueOrderIndex.TryGetOrdinal(child, out _));
        Assert.True(root.QueueOrderIndex.TryGetOrdinal(root, out int rootOrdinal));
        Assert.Equal(0, rootOrdinal);
    }

    [Fact]
    public void DeepTreeBuildUsesExistingIterativeWalker()
    {
        UIRoot root = new();
        UIElement parent = root;
        UIElement deepest = root;
        for (int i = 0; i < 1_000; i++)
        {
            deepest = new UIElement();
            parent.VisualChildren.Add(deepest);
            parent = deepest;
        }

        RenderQueue queue = new(root);
        queue.Enqueue(deepest);

        Assert.Equal([deepest], queue.Snapshot());
        Assert.Equal(1_001, root.QueueOrderIndex.LastVisitedNodeCount);
    }
}
