using BenchmarkDotNet.Attributes;
using Cerneala.UI.Elements;
using Cerneala.UI.Invalidation;

namespace Cerneala.Benchmarks;

[MemoryDiagnoser]
[ShortRunJob]
public class QueueHasWorkBenchmarks
{
    private UIRoot root = null!;

    [Params(100, 1_000, 10_000)]
    public int TreeSize { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        (root, _) = QueueBenchmarkTree.Create(TreeSize);
    }

    [Benchmark]
    public bool IdleHasWork()
    {
        return root.Scheduler.HasWork;
    }

    [Benchmark]
    public bool RepeatedIdleHasWork()
    {
        bool result = false;
        for (int i = 0; i < 10; i++)
        {
            result |= root.Scheduler.HasWork;
        }

        return result;
    }
}

[MemoryDiagnoser]
[ShortRunJob]
public class QueueSnapshotBenchmarks
{
    private UIRoot root = null!;
    private UIElement[] elements = null!;

    [Params(1_000, 10_000)]
    public int TreeSize { get; set; }

    [Params(1, 10, 100, 1_000)]
    public int QueuedCount { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        (root, elements) = QueueBenchmarkTree.Create(TreeSize);
        QueueBenchmarkTree.EnqueueEvenly(root.RenderQueue.Enqueue, elements, QueuedCount);
    }

    [Benchmark]
    public IReadOnlyList<UIElement> Snapshot()
    {
        return root.RenderQueue.Snapshot();
    }
}

[MemoryDiagnoser]
[SimpleJob(launchCount: 1, warmupCount: 2, iterationCount: 5, invocationCount: 1)]
public class QueueDrainBenchmarks
{
    private UIRoot root = null!;
    private UIElement[] elements = null!;

    [Params(100, 1_000, 10_000)]
    public int EntryCount { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        (root, elements) = QueueBenchmarkTree.Create(EntryCount);
    }

    [IterationSetup]
    public void PrepareQueue()
    {
        foreach (UIElement element in elements)
        {
            root.RenderQueue.Enqueue(element);
        }
    }

    [Benchmark]
    public int Drain()
    {
        IReadOnlyList<UIElement> snapshot = root.RenderQueue.Snapshot();
        foreach (UIElement element in snapshot)
        {
            root.RenderQueue.Remove(element);
        }

        return snapshot.Count;
    }
}

[MemoryDiagnoser]
[SimpleJob(launchCount: 1, warmupCount: 2, iterationCount: 5, invocationCount: 1)]
public class QueueDrainComparisonBenchmarks
{
    private UIRoot root = null!;
    private UIElement[] elements = null!;
    private LegacyElementQueue legacy = null!;
    private RenderQueue current = null!;

    [Params(100, 1_000, 10_000)]
    public int EntryCount { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        (root, elements) = QueueBenchmarkTree.Create(EntryCount);
    }

    [IterationSetup]
    public void PrepareQueues()
    {
        legacy = new LegacyElementQueue(root);
        current = new RenderQueue(root);
        foreach (UIElement element in elements)
        {
            legacy.Enqueue(element);
            current.Enqueue(element);
        }
    }

    [Benchmark(Baseline = true)]
    public int LegacyDrain()
    {
        IReadOnlyList<UIElement> snapshot = legacy.Snapshot();
        foreach (UIElement element in snapshot)
        {
            legacy.Remove(element);
        }

        return snapshot.Count;
    }

    [Benchmark]
    public int CurrentDrain()
    {
        IReadOnlyList<UIElement> snapshot = current.Snapshot();
        foreach (UIElement element in snapshot)
        {
            current.Remove(element);
        }

        return snapshot.Count;
    }
}

[MemoryDiagnoser]
[ShortRunJob]
public class QueueSharedOrderBenchmarks
{
    private UIRoot root = null!;

    [Params(1_000, 10_000)]
    public int TreeSize { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        (root, UIElement[] elements) = QueueBenchmarkTree.Create(TreeSize);
        QueueBenchmarkTree.EnqueueEvenly(root.RenderQueue.Enqueue, elements, 100);
        QueueBenchmarkTree.EnqueueEvenly(root.AspectQueue.Enqueue, elements, 100);
        QueueBenchmarkTree.EnqueueEvenly(root.HitTestQueue.Enqueue, elements, 100);
    }

    [Benchmark]
    public int SnapshotsAcrossQueues()
    {
        return root.RenderQueue.Snapshot().Count +
            root.AspectQueue.Snapshot().Count +
            root.HitTestQueue.Snapshot().Count;
    }
}

[MemoryDiagnoser]
[SimpleJob(launchCount: 1, warmupCount: 2, iterationCount: 5, invocationCount: 1)]
public class QueueOrderRebuildBenchmarks
{
    private UIRoot root = null!;
    private UIElement[] elements = null!;
    private bool reversed;

    [Params(1_000, 10_000)]
    public int TreeSize { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        (root, elements) = QueueBenchmarkTree.Create(TreeSize);
        QueueBenchmarkTree.EnqueueEvenly(root.RenderQueue.Enqueue, elements, 100);
        _ = root.RenderQueue.Snapshot();
    }

    [IterationSetup]
    public void MutateTree()
    {
        int from = reversed ? TreeSize - 1 : 0;
        int to = reversed ? 0 : TreeSize - 1;
        root.VisualChildren.Move(from, to);
        root.RenderQueue.Remove(root);
        reversed = !reversed;
    }

    [Benchmark]
    public IReadOnlyList<UIElement> RebuildAndSnapshot()
    {
        return root.RenderQueue.Snapshot();
    }
}

[MemoryDiagnoser]
[ShortRunJob]
public class LayoutQueuePromotionBenchmarks
{
    private LayoutQueue queue = null!;
    private UIElement element = null!;

    [GlobalSetup]
    public void Setup()
    {
        UIRoot root;
        (root, UIElement[] elements) = QueueBenchmarkTree.Create(1);
        queue = root.LayoutQueue;
        element = elements[0];
        queue.EnqueueMeasure(element, LayoutQueueEntryKind.Propagated);
    }

    [Benchmark]
    public void PromoteMetadata()
    {
        queue.EnqueueMeasure(element, LayoutQueueEntryKind.Direct);
    }
}

[MemoryDiagnoser]
[SimpleJob(launchCount: 1, warmupCount: 2, iterationCount: 5, invocationCount: 1)]
public class QueueDetachBenchmarks
{
    private UIRoot root = null!;
    private UIElement subtree = null!;

    [Params(10, 100, 1_000)]
    public int SubtreeSize { get; set; }

    [IterationSetup]
    public void Setup()
    {
        root = new UIRoot();
        subtree = new UIElement();
        root.VisualChildren.Add(subtree);
        for (int i = 0; i < SubtreeSize; i++)
        {
            UIElement child = new();
            subtree.VisualChildren.Add(child);
        }

        root.ProcessFrame();
        foreach (UIElement element in ElementTreeWalker.PreOrder(subtree, ElementChildRole.Visual))
        {
            root.RenderQueue.Enqueue(element);
            root.AspectQueue.Enqueue(element);
            root.LayoutQueue.EnqueueMeasure(element);
        }
    }

    [Benchmark]
    public bool DetachScheduledSubtree()
    {
        root.VisualChildren.Remove(subtree);
        return root.Scheduler.HasWork;
    }
}

internal static class QueueBenchmarkTree
{
    public static (UIRoot Root, UIElement[] Elements) Create(int size)
    {
        UIRoot root = new();
        UIElement[] elements = new UIElement[size];
        for (int i = 0; i < size; i++)
        {
            UIElement element = new();
            elements[i] = element;
            root.VisualChildren.Add(element);
        }

        root.ProcessFrame();
        return (root, elements);
    }

    public static void EnqueueEvenly(Action<UIElement> enqueue, UIElement[] elements, int count)
    {
        int stride = Math.Max(1, elements.Length / count);
        for (int i = 0; i < count; i++)
        {
            enqueue(elements[Math.Min(i * stride, elements.Length - 1)]);
        }
    }
}

internal sealed class LegacyElementQueue
{
    private readonly UIRoot root;
    private readonly HashSet<UIElement> elements = new(ReferenceEqualityComparer.Instance);
    private readonly List<UIElement> order = [];

    public LegacyElementQueue(UIRoot root)
    {
        this.root = root;
    }

    public void Enqueue(UIElement element)
    {
        if (elements.Add(element))
        {
            order.Add(element);
        }
    }

    public IReadOnlyList<UIElement> Snapshot()
    {
        Dictionary<UIElement, int> visualOrder = new(ReferenceEqualityComparer.Instance);
        int index = 0;
        foreach (UIElement element in ElementTreeWalker.PreOrder(root, ElementChildRole.Visual))
        {
            visualOrder[element] = index++;
        }

        return order
            .Where(elements.Contains)
            .Select((element, enqueueIndex) => new LegacyOrder(element, enqueueIndex))
            .OrderBy(item => visualOrder.TryGetValue(item.Element, out int ordinal) ? ordinal : int.MaxValue)
            .ThenBy(item => item.EnqueueIndex)
            .Select(item => item.Element)
            .ToArray();
    }

    public void Remove(UIElement element)
    {
        if (elements.Remove(element))
        {
            order.RemoveAll(candidate => ReferenceEquals(candidate, element));
        }
    }

    private readonly record struct LegacyOrder(UIElement Element, int EnqueueIndex);
}
