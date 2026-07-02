using Cerneala.UI.Elements;

namespace Cerneala.UI.Invalidation;

public sealed class LayoutQueue
{
    private readonly UIRoot root;
    private readonly HashSet<UIElement> measure = new(ReferenceEqualityComparer.Instance);
    private readonly HashSet<UIElement> arrange = new(ReferenceEqualityComparer.Instance);
    private readonly List<UIElement> measureOrder = [];
    private readonly List<UIElement> arrangeOrder = [];

    public LayoutQueue(UIRoot root)
    {
        this.root = root ?? throw new ArgumentNullException(nameof(root));
    }

    public int MeasureCount => measure.Count;

    public int ArrangeCount => arrange.Count;

    public bool HasWork => measure.Count > 0 || arrange.Count > 0;

    public void EnqueueMeasure(UIElement element)
    {
        ArgumentNullException.ThrowIfNull(element);
        if (measure.Add(element))
        {
            measureOrder.Add(element);
        }
    }

    public void EnqueueArrange(UIElement element)
    {
        ArgumentNullException.ThrowIfNull(element);
        if (arrange.Add(element))
        {
            arrangeOrder.Add(element);
        }
    }

    public IReadOnlyList<UIElement> SnapshotMeasure()
    {
        ElementQueueOrder.RemoveElementsOutsideRoot(root, measure, measureOrder);
        return ElementQueueOrder.Sort(root, measureOrder.Where(measure.Contains));
    }

    public IReadOnlyList<UIElement> SnapshotArrange()
    {
        ElementQueueOrder.RemoveElementsOutsideRoot(root, arrange, arrangeOrder);
        return ElementQueueOrder.Sort(root, arrangeOrder.Where(arrange.Contains));
    }

    public void RemoveMeasure(UIElement element)
    {
        if (measure.Remove(element))
        {
            measureOrder.RemoveAll(candidate => ReferenceEquals(candidate, element));
        }
    }

    public void RemoveArrange(UIElement element)
    {
        if (arrange.Remove(element))
        {
            arrangeOrder.RemoveAll(candidate => ReferenceEquals(candidate, element));
        }
    }
}
