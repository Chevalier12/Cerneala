using Cerneala.UI.Elements;

namespace Cerneala.UI.Input;

public sealed class HoverTracker
{
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

        UIElement? old = HoveredElement;
        HoveredElement = next;

        if (old is not null)
        {
            old.IsPointerOver = false;
            RaiseDirect(routeMap, old, InputEvents.MouseLeaveEvent, x, y);
        }

        if (next is not null)
        {
            next.IsPointerOver = true;
            RaiseDirect(routeMap, next, InputEvents.MouseEnterEvent, x, y);
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
