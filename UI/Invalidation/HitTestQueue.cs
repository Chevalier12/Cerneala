using Cerneala.UI.Elements;

namespace Cerneala.UI.Invalidation;

public sealed class HitTestQueue
{
    private readonly ElementWorkQueue<ElementQueueUnit> work;

    public HitTestQueue(UIRoot root)
    {
        work = new ElementWorkQueue<ElementQueueUnit>(root ?? throw new ArgumentNullException(nameof(root)));
    }

    public int Count => work.Count;

    public bool HasWork => work.HasWork;

    public void Enqueue(UIElement element)
    {
        work.Enqueue(element, ElementQueueUnit.Value);
    }

    internal bool Contains(UIElement element)
    {
        return work.Contains(element);
    }

    public IReadOnlyList<UIElement> Snapshot()
    {
        return work.Snapshot();
    }

    public void Remove(UIElement element)
    {
        work.Remove(element);
    }
}
