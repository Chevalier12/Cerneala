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
        IReadOnlyList<UIElement> nextPath = BuildPath(next, routeMap);
        if (ReferenceEquals(HoveredElement, next) &&
            PathsEqual(hoveredPath, nextPath))
        {
            return false;
        }

        IReadOnlyList<UIElement> oldPath = hoveredPath;
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

    internal bool IsCurrentRouteValid(ElementInputRouteMap routeMap)
    {
        ArgumentNullException.ThrowIfNull(routeMap);
        IReadOnlyList<UIElement> currentPath = BuildPath(HoveredElement, routeMap);
        return PathsEqual(hoveredPath, currentPath);
    }

    private static IReadOnlyList<UIElement> BuildPath(
        UIElement? element,
        ElementInputRouteMap routeMap)
    {
        if (element is null)
        {
            return [];
        }

        return routeMap.GetRouteToRoot(element);
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

    private static bool PathsEqual(
        IReadOnlyList<UIElement> first,
        IReadOnlyList<UIElement> second)
    {
        if (first.Count != second.Count)
        {
            return false;
        }

        for (int index = 0; index < first.Count; index++)
        {
            if (!ReferenceEquals(first[index], second[index]))
            {
                return false;
            }
        }

        return true;
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
