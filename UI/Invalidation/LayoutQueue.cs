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
    private readonly UIRoot root;
    private readonly HashSet<UIElement> measure = new(ReferenceEqualityComparer.Instance);
    private readonly HashSet<UIElement> arrange = new(ReferenceEqualityComparer.Instance);
    private readonly List<UIElement> measureOrder = [];
    private readonly List<UIElement> arrangeOrder = [];
    private readonly Dictionary<UIElement, LayoutQueueEntryKind> measureKinds = new(ReferenceEqualityComparer.Instance);
    private readonly Dictionary<UIElement, LayoutQueueEntryKind> arrangeKinds = new(ReferenceEqualityComparer.Instance);

    public LayoutQueue(UIRoot root)
    {
        this.root = root ?? throw new ArgumentNullException(nameof(root));
    }

    public int MeasureCount => measure.Count;

    public int ArrangeCount => arrange.Count;

    public bool HasWork => SnapshotMeasure().Count > 0 || SnapshotArrange().Count > 0;

    public void EnqueueMeasure(UIElement element)
    {
        EnqueueMeasure(element, LayoutQueueEntryKind.Direct);
    }

    internal void EnqueueMeasure(UIElement element, LayoutQueueEntryKind kind)
    {
        ArgumentNullException.ThrowIfNull(element);
        if (measure.Add(element))
        {
            measureOrder.Add(element);
        }

        Promote(measureKinds, element, kind);
    }

    public void EnqueueArrange(UIElement element)
    {
        EnqueueArrange(element, LayoutQueueEntryKind.Direct);
    }

    internal void EnqueueArrange(UIElement element, LayoutQueueEntryKind kind)
    {
        ArgumentNullException.ThrowIfNull(element);
        if (arrange.Add(element))
        {
            arrangeOrder.Add(element);
        }

        Promote(arrangeKinds, element, kind);
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
        return measureKinds.GetValueOrDefault(element, LayoutQueueEntryKind.Direct);
    }

    internal LayoutQueueEntryKind GetArrangeKind(UIElement element)
    {
        return arrangeKinds.GetValueOrDefault(element, LayoutQueueEntryKind.Direct);
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
        ElementQueueOrder.RemoveElementsOutsideRoot(root, measure, measureOrder);
        RemoveStaleKinds(measure, measureKinds);
        return ElementQueueOrder.Sort(root, measureOrder.Where(measure.Contains));
    }

    internal IReadOnlyList<UIElement> SnapshotMeasureIncremental()
    {
        return SnapshotMeasure().Reverse().ToArray();
    }

    public IReadOnlyList<UIElement> SnapshotArrange()
    {
        ElementQueueOrder.RemoveElementsOutsideRoot(root, arrange, arrangeOrder);
        RemoveStaleKinds(arrange, arrangeKinds);
        return ElementQueueOrder.Sort(root, arrangeOrder.Where(arrange.Contains));
    }

    public void RemoveMeasure(UIElement element)
    {
        if (measure.Remove(element))
        {
            measureOrder.RemoveAll(candidate => ReferenceEquals(candidate, element));
            measureKinds.Remove(element);
        }
    }

    public void RemoveArrange(UIElement element)
    {
        if (arrange.Remove(element))
        {
            arrangeOrder.RemoveAll(candidate => ReferenceEquals(candidate, element));
            arrangeKinds.Remove(element);
        }
    }

    private static void Promote(
        Dictionary<UIElement, LayoutQueueEntryKind> kinds,
        UIElement element,
        LayoutQueueEntryKind kind)
    {
        if (!kinds.TryGetValue(element, out LayoutQueueEntryKind current) || kind > current)
        {
            kinds[element] = kind;
        }
    }

    private static void RemoveStaleKinds(
        HashSet<UIElement> queued,
        Dictionary<UIElement, LayoutQueueEntryKind> kinds)
    {
        foreach (UIElement element in kinds.Keys.Where(element => !queued.Contains(element)).ToArray())
        {
            kinds.Remove(element);
        }
    }
}
