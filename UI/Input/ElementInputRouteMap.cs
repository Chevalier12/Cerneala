using Cerneala.UI.Elements;

namespace Cerneala.UI.Input;

public sealed class ElementInputRouteMap
{
    private readonly Dictionary<UIElement, UiElementId> idsByElement = new(ReferenceEqualityComparer.Instance);
    private readonly Dictionary<UiElementId, UIElement> elementsById = [];
    private readonly List<UIElement> elementsInRouteOrder = [];
    private readonly Dictionary<UiElementId, UiElementId?> visualParentIds = [];

    public UiInputTree InputTree { get; } = new();

    public int Count => idsByElement.Count;

    public IReadOnlyList<UIElement> ElementsInRouteOrder => elementsInRouteOrder;

    public void Add(UIElement element, UiElementId id, UiElementId? parentId)
    {
        Add(element, id, parentId, parentId);
    }

    // The visual parent differs from the route parent only for projected content: its route parent is the projection
    // owner, its visual parent the nearest registered visual ancestor.
    internal void Add(UIElement element, UiElementId id, UiElementId? parentId, UiElementId? visualParentId)
    {
        ArgumentNullException.ThrowIfNull(element);
        if (visualParentId.HasValue && !visualParentIds.ContainsKey(visualParentId.Value))
        {
            throw new ArgumentException("Visual parent element must be added before the child element.", nameof(visualParentId));
        }

        idsByElement.Add(element, id);
        elementsById.Add(id, element);
        elementsInRouteOrder.Add(element);
        InputTree.Add(id, parentId, element.IsEnabled);
        visualParentIds.Add(id, visualParentId);
    }

    public bool TryGetId(UIElement element, out UiElementId id)
    {
        ArgumentNullException.ThrowIfNull(element);
        if (element is IInputRouteGuard { IsInputRouteAvailable: false })
        {
            id = default;
            return false;
        }
        return idsByElement.TryGetValue(element, out id);
    }

    public bool TryGetElement(UiElementId id, out UIElement? element)
    {
        if (!elementsById.TryGetValue(id, out element)) { return false; }
        if (element is not IInputRouteGuard { IsInputRouteAvailable: false }) { return true; }
        element = null;
        return false;
    }

    internal IReadOnlyList<UIElement> GetRouteToRoot(UIElement element)
    {
        ArgumentNullException.ThrowIfNull(element);
        if (!TryGetId(element, out UiElementId id))
        {
            return [];
        }

        return ToElements(InputTree.GetRouteToRoot(id));
    }

    // The registered visual ancestors of an element, nearest first. Unlike GetRouteToRoot, it does not continue from
    // projected content to its projection owner, so it answers "is the pointer or focus inside this element visually".
    internal IReadOnlyList<UIElement> GetVisualRouteToRoot(UIElement element)
    {
        ArgumentNullException.ThrowIfNull(element);
        if (!TryGetId(element, out UiElementId id))
        {
            return [];
        }

        List<UiElementId> ids = [];
        for (UiElementId? current = id; current.HasValue; current = visualParentIds[current.Value])
        {
            ids.Add(current.Value);
        }

        return ToElements(ids);
    }

    private List<UIElement> ToElements(IReadOnlyList<UiElementId> ids)
    {
        List<UIElement> route = new(ids.Count);
        foreach (UiElementId routeId in ids)
        {
            if (TryGetElement(routeId, out UIElement? routeElement) && routeElement is not null)
            {
                route.Add(routeElement);
            }
        }

        return route;
    }
}
