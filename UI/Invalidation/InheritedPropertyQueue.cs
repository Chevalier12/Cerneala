using Cerneala.UI.Elements;

namespace Cerneala.UI.Invalidation;

public sealed class InheritedPropertyQueue
{
    private readonly UIRoot root;
    private readonly HashSet<UIElement> elements = new(ReferenceEqualityComparer.Instance);
    private readonly List<UIElement> order = [];

    public InheritedPropertyQueue(UIRoot root)
    {
        this.root = root ?? throw new ArgumentNullException(nameof(root));
    }

    public int Count => elements.Count;

    public bool HasWork => Snapshot().Count > 0;

    public void Enqueue(UIElement element)
    {
        ArgumentNullException.ThrowIfNull(element);
        if (elements.Add(element))
        {
            order.Add(element);
        }
    }

    public IReadOnlyList<UIElement> Snapshot()
    {
        ElementQueueOrder.RemoveElementsOutsideRoot(root, elements, order);
        return ElementQueueOrder.Sort(root, order.Where(elements.Contains));
    }

    public void Remove(UIElement element)
    {
        if (elements.Remove(element))
        {
            order.RemoveAll(candidate => ReferenceEquals(candidate, element));
        }
    }
}
