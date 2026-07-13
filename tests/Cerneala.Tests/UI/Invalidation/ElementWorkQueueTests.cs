using Cerneala.UI.Elements;
using Cerneala.UI.Invalidation;

namespace Cerneala.Tests.UI.Invalidation;

public sealed class ElementWorkQueueTests
{
    [Fact]
    public void DeduplicatesByReferenceAndKeepsEqualInstancesDistinct()
    {
        UIRoot root = new();
        EqualValueElement first = new(1);
        EqualValueElement second = new(1);
        root.VisualChildren.Add(first);
        root.VisualChildren.Add(second);
        ElementWorkQueue<int> queue = new(root);

        queue.Enqueue(first, 1);
        queue.Enqueue(first, 2);
        queue.Enqueue(second, 3);

        Assert.Equal(2, queue.Count);
        Assert.True(queue.Contains(first));
        Assert.True(queue.Contains(second));
    }

    [Fact]
    public void RemoveAndReenqueueAreConstantTimeDictionaryOperations()
    {
        UIRoot root = new();
        UIElement child = new();
        root.VisualChildren.Add(child);
        ElementWorkQueue<int> queue = new(root);

        Assert.False(queue.Remove(child));
        queue.Enqueue(child, 1);
        Assert.True(queue.Remove(child));
        queue.Enqueue(child, 2);

        Assert.Equal(1, queue.Count);
        Assert.Equal(2, queue.GetMetadataOrDefault(child, -1));
    }

    [Fact]
    public void MetadataMergePromotesWithoutDowngrading()
    {
        UIRoot root = new();
        UIElement child = new();
        root.VisualChildren.Add(child);
        ElementWorkQueue<int> queue = new(root, static (current, incoming) => Math.Max(current, incoming));

        queue.Enqueue(child, 1);
        queue.Enqueue(child, 3);
        queue.Enqueue(child, 2);

        Assert.Equal(3, queue.GetMetadataOrDefault(child, -1));
    }

    [Fact]
    public void SnapshotIsStableAndSortsOnlyQueuedElements()
    {
        UIRoot root = new();
        UIElement first = new();
        UIElement second = new();
        root.VisualChildren.Add(first);
        root.VisualChildren.Add(second);
        ElementWorkQueue<ElementQueueUnit> queue = new(root);
        queue.Enqueue(first, ElementQueueUnit.Value);

        IReadOnlyList<UIElement> snapshot = queue.Snapshot();
        queue.Enqueue(second, ElementQueueUnit.Value);

        Assert.Equal([first], snapshot);
        Assert.Equal(1, queue.LastSnapshotSortCount);
        Assert.Equal([first, second], queue.Snapshot());
        Assert.Equal(2, queue.LastSnapshotSortCount);
    }

    [Fact]
    public void SnapshotDefensivelyPrunesElementOutsideRoot()
    {
        UIRoot root = new();
        UIElement detached = new();
        ElementWorkQueue<ElementQueueUnit> queue = new(root);
        queue.Enqueue(detached, ElementQueueUnit.Value);

        Assert.Empty(queue.Snapshot());
        Assert.False(queue.HasWork);
        Assert.Equal(0, queue.Count);
    }

    [Fact]
    public void CountAndHasWorkDoNotBuildIndexOrAllocateAfterWarmup()
    {
        UIRoot root = new();
        UIElement child = new();
        root.VisualChildren.Add(child);
        ElementWorkQueue<ElementQueueUnit> queue = new(root);
        queue.Enqueue(child, ElementQueueUnit.Value);
        ConsumeQueueState(queue, 1);

        long before = GC.GetAllocatedBytesForCurrentThread();
        int state = ConsumeQueueState(queue, 10_000);
        long allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        Assert.Equal(20_000, state);
        Assert.Equal(0, allocated);
        Assert.Equal(0, root.QueueOrderIndex.BuildCount);
    }

    private static int ConsumeQueueState(ElementWorkQueue<ElementQueueUnit> queue, int iterations)
    {
        int state = 0;
        for (int i = 0; i < iterations; i++)
        {
            state += queue.Count;
            state += queue.HasWork ? 1 : 0;
        }

        return state;
    }

    private sealed class EqualValueElement(int value) : UIElement
    {
        private int Value { get; } = value;

        public override bool Equals(object? obj)
        {
            return obj is EqualValueElement other && other.Value == Value;
        }

        public override int GetHashCode()
        {
            return Value;
        }
    }
}
