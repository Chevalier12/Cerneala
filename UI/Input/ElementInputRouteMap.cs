using Cerneala.UI.Elements;

namespace Cerneala.UI.Input;

public sealed class ElementInputRouteMap
{
    private readonly Dictionary<UIElement, UiElementId> idsByElement = new(ReferenceEqualityComparer.Instance);
    private readonly Dictionary<UiElementId, UIElement> elementsById = [];
    private readonly List<UIElement> elementsInRouteOrder = [];

    public UiInputTree InputTree { get; } = new();

    public int Count => idsByElement.Count;

    public IReadOnlyList<UIElement> ElementsInRouteOrder => elementsInRouteOrder;

    public void Add(UIElement element, UiElementId id, UiElementId? parentId)
    {
        ArgumentNullException.ThrowIfNull(element);

        idsByElement.Add(element, id);
        elementsById.Add(id, element);
        elementsInRouteOrder.Add(element);
        InputTree.Add(id, parentId, element.IsEnabled);
    }

    public bool TryGetId(UIElement element, out UiElementId id)
    {
        ArgumentNullException.ThrowIfNull(element);
        return idsByElement.TryGetValue(element, out id);
    }

    public bool TryGetElement(UiElementId id, out UIElement? element)
    {
        return elementsById.TryGetValue(id, out element);
    }
}
