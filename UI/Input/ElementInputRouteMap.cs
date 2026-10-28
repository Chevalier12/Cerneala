using Cerneala.UI.Elements;

namespace Cerneala.UI.Input;

public sealed class ElementInputRouteMap
{
    private readonly List<UIElement> elementsInRouteOrder = [];
    private readonly Dictionary<UIElement, UIElement?> visualParents = new(ReferenceEqualityComparer.Instance);

    public UiInputTree InputTree { get; } = new();

    public int Count => visualParents.Count;

    public IReadOnlyList<UIElement> ElementsInRouteOrder => elementsInRouteOrder;

    public void Add(UIElement element, UIElement? parent)
    {
        Add(element, parent, parent);
    }

    // The visual parent differs from the route parent only for projected content: its route parent is the projection
    // owner, its visual parent the nearest registered visual ancestor.
    internal void Add(UIElement element, UIElement? parent, UIElement? visualParent)
    {
        ArgumentNullException.ThrowIfNull(element);
        if (visualParent is not null && !visualParents.ContainsKey(visualParent))
        {
            throw new ArgumentException("Visual parent element must be added before the child element.", nameof(visualParent));
        }

        if (visualParents.ContainsKey(element))
        {
            throw new ArgumentException("Element is already registered in this route map.", nameof(element));
        }

        InputTree.Add(element, parent, element.IsEnabled);
        visualParents.Add(element, visualParent);
        elementsInRouteOrder.Add(element);
    }

    // Whether the element is registered in this map and its input route is available now.
    public bool Contains(UIElement element)
    {
        ArgumentNullException.ThrowIfNull(element);
        return element is not IInputRouteGuard { IsInputRouteAvailable: false } && visualParents.ContainsKey(element);
    }

    // The registered visual parent of a registered element, or null.
    internal UIElement? GetVisualParent(UIElement element)
    {
        return visualParents.GetValueOrDefault(element);
    }

    internal IReadOnlyList<UIElement> GetRouteToRoot(UIElement element)
    {
        ArgumentNullException.ThrowIfNull(element);
        if (!Contains(element))
        {
            return [];
        }

        return Available(InputTree.GetRouteToRoot(element));
    }

    // The registered visual ancestors of an element, nearest first. Unlike GetRouteToRoot, it does not continue from
    // projected content to its projection owner, so it answers "is the pointer or focus inside this element visually".
    internal IReadOnlyList<UIElement> GetVisualRouteToRoot(UIElement element)
    {
        ArgumentNullException.ThrowIfNull(element);
        if (!Contains(element))
        {
            return [];
        }

        List<UIElement> elements = [];
        for (UIElement? current = element; current is not null; current = visualParents[current])
        {
            elements.Add(current);
        }

        return Available(elements);
    }

    private List<UIElement> Available(IReadOnlyList<UIElement> elements)
    {
        List<UIElement> route = new(elements.Count);
        foreach (UIElement routeElement in elements)
        {
            if (Contains(routeElement))
            {
                route.Add(routeElement);
            }
        }

        return route;
    }
}
