using Cerneala.UI.Input;

namespace Cerneala.UI.Elements;

public sealed class ElementIdProvider
{
    private long nextId;
    private readonly Dictionary<UIElement, UiElementId> idsByElement = new(ReferenceEqualityComparer.Instance);
    private readonly Dictionary<UiElementId, UIElement> elementsById = [];

    public UiElementId GetOrCreate(UIElement element)
    {
        ArgumentNullException.ThrowIfNull(element);

        if (idsByElement.TryGetValue(element, out UiElementId existingId))
        {
            return existingId;
        }

        UiElementId id = new($"ui-{++nextId}");
        idsByElement.Add(element, id);
        elementsById.Add(id, element);
        return id;
    }

    public bool TryGetElement(UiElementId id, out UIElement? element)
    {
        return elementsById.TryGetValue(id, out element);
    }

    public bool TryGetId(UIElement element, out UiElementId id)
    {
        ArgumentNullException.ThrowIfNull(element);
        return idsByElement.TryGetValue(element, out id);
    }

    public void Release(UIElement element)
    {
        ArgumentNullException.ThrowIfNull(element);

        if (!idsByElement.Remove(element, out UiElementId id))
        {
            return;
        }

        elementsById.Remove(id);
    }
}
