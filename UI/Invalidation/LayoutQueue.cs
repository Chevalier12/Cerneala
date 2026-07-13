using Cerneala.UI.Elements;

namespace Cerneala.UI.Invalidation;

internal enum LayoutQueueEntryKind
{
    Propagated,
    Required,
    Direct
}

public sealed class LayoutQueue
{
    private readonly ElementWorkQueue<LayoutQueueEntryKind> measure;
    private readonly ElementWorkQueue<LayoutQueueEntryKind> arrange;

    public LayoutQueue(UIRoot root)
    {
        ArgumentNullException.ThrowIfNull(root);
        measure = new ElementWorkQueue<LayoutQueueEntryKind>(root, Promote);
        arrange = new ElementWorkQueue<LayoutQueueEntryKind>(root, Promote);
    }

    public int MeasureCount => measure.Count;

    public int ArrangeCount => arrange.Count;

    public bool HasWork => measure.HasWork || arrange.HasWork;

    public void EnqueueMeasure(UIElement element)
    {
        EnqueueMeasure(element, LayoutQueueEntryKind.Direct);
    }

    internal void EnqueueMeasure(UIElement element, LayoutQueueEntryKind kind)
    {
        measure.Enqueue(element, kind);
    }

    public void EnqueueArrange(UIElement element)
    {
        EnqueueArrange(element, LayoutQueueEntryKind.Direct);
    }

    internal void EnqueueArrange(UIElement element, LayoutQueueEntryKind kind)
    {
        arrange.Enqueue(element, kind);
    }

    internal bool ContainsMeasure(UIElement element)
    {
        return measure.Contains(element);
    }

    internal bool ContainsArrange(UIElement element)
    {
        return arrange.Contains(element);
    }

    internal LayoutQueueEntryKind GetMeasureKind(UIElement element)
    {
        return measure.GetMetadataOrDefault(element, LayoutQueueEntryKind.Direct);
    }

    internal LayoutQueueEntryKind GetArrangeKind(UIElement element)
    {
        return arrange.GetMetadataOrDefault(element, LayoutQueueEntryKind.Direct);
    }

    internal void RequireMeasure(UIElement element)
    {
        EnqueueMeasure(element, LayoutQueueEntryKind.Required);
    }

    internal void RequireArrange(UIElement element)
    {
        EnqueueArrange(element, LayoutQueueEntryKind.Required);
    }

    public IReadOnlyList<UIElement> SnapshotMeasure()
    {
        return measure.Snapshot();
    }

    internal IReadOnlyList<UIElement> SnapshotMeasureIncremental()
    {
        return measure.Snapshot(reverse: true);
    }

    public IReadOnlyList<UIElement> SnapshotArrange()
    {
        return arrange.Snapshot();
    }

    public void RemoveMeasure(UIElement element)
    {
        measure.Remove(element);
    }

    public void RemoveArrange(UIElement element)
    {
        arrange.Remove(element);
    }

    private static LayoutQueueEntryKind Promote(
        LayoutQueueEntryKind current,
        LayoutQueueEntryKind incoming)
    {
        return incoming > current ? incoming : current;
    }
}
