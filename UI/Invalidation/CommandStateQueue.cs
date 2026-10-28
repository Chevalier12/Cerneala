using Cerneala.UI.Elements;

namespace Cerneala.UI.Invalidation;

public sealed class CommandStateQueue
{
    private readonly ElementWorkQueue<ElementQueueUnit> work;

    public CommandStateQueue(UIRoot root)
    {
        // An attached element outside the visual tree, such as the content of a closed Overlay, still has a command to
        // follow: its refresh runs after the visual elements instead of being dropped as stale (#1035). A RoutedCommand
        // that cannot reach its binding from there defers until the input route changes, for example when the overlay
        // opens.
        work = new ElementWorkQueue<ElementQueueUnit>(
            root ?? throw new ArgumentNullException(nameof(root)),
            keepsAttachedElementsOutsideVisualTree: true);
    }

    public int Count => work.Count;

    public bool HasWork => work.HasWork;

    public void Enqueue(UIElement element)
    {
        work.Enqueue(element, ElementQueueUnit.Value);
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
