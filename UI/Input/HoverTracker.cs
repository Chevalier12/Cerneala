using Cerneala.UI.Elements;

namespace Cerneala.UI.Input;

public sealed class HoverTracker
{
    private IReadOnlyList<UIElement> hoveredPath = [];

    public UIElement? HoveredElement { get; private set; }

    public bool Update(HitTestResult? target, ElementInputRouteMap routeMap)
    {
        return Update(target, routeMap, target?.X ?? 0, target?.Y ?? 0);
    }

    public bool Update(HitTestResult? target, ElementInputRouteMap routeMap, float x, float y)
    {
        ArgumentNullException.ThrowIfNull(routeMap);

        UIElement? next = target?.Element;
        if (ReferenceEquals(HoveredElement, next))
        {
            return false;
        }

        IReadOnlyList<UIElement> oldPath = hoveredPath;
        IReadOnlyList<UIElement> nextPath = BuildPath(next);
        HoveredElement = next;
        hoveredPath = nextPath;

        foreach (UIElement oldElement in oldPath)
        {
            if (ContainsReference(nextPath, oldElement))
            {
                continue;
            }

            oldElement.IsPointerOver = false;
            RaiseDirect(routeMap, oldElement, InputEvents.MouseLeaveEvent, x, y);
        }

        foreach (UIElement nextElement in nextPath)
        {
            if (ContainsReference(oldPath, nextElement))
            {
                continue;
            }

            nextElement.IsPointerOver = true;
            RaiseDirect(routeMap, nextElement, InputEvents.MouseEnterEvent, x, y);
        }

        return true;
    }

    private static IReadOnlyList<UIElement> BuildPath(UIElement? element)
    {
        if (element is null)
        {
            return [];
        }

        List<UIElement> path = [];
        for (UIElement? current = element; current is not null; current = current.VisualParent)
        {
            path.Add(current);
        }

        return path;
    }

    private static bool ContainsReference(IReadOnlyList<UIElement> elements, UIElement target)
    {
        foreach (UIElement element in elements)
        {
            if (ReferenceEquals(element, target))
            {
                return true;
            }
        }

        return false;
    }

    private static void RaiseDirect(ElementInputRouteMap routeMap, UIElement element, RoutedEvent routedEvent, float x, float y)
    {
        if (!routeMap.TryGetId(element, out UiElementId id))
        {
            return;
        }

        RoutedEventRouter.Raise(routeMap.InputTree, id, new MouseEventArgs(routedEvent, id, (int)MathF.Round(x), (int)MathF.Round(y)));
    }
}
