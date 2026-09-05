using Cerneala.UI.Elements;

namespace Cerneala.UI.Input;

public sealed class PressedStateTracker
{
    public IInputPressable? PressedElement { get; private set; }

    public void Press(UIElement? target)
    {
        PressCore(target, routeMap: null);
    }

    internal void Press(UIElement? target, ElementInputRouteMap routeMap)
    {
        ArgumentNullException.ThrowIfNull(routeMap);
        PressCore(target, routeMap);
    }

    private void PressCore(UIElement? target, ElementInputRouteMap? routeMap)
    {
        IInputPressable? pressable = ResolvePressable(target, routeMap);
        if (pressable is null)
        {
            Cancel();
            return;
        }

        if (ReferenceEquals(PressedElement, pressable))
        {
            return;
        }

        Cancel();
        PressedElement = pressable;
        pressable.IsPressed = true;
    }

    public void Release()
    {
        Cancel();
    }

    public void Cancel()
    {
        if (PressedElement is null)
        {
            return;
        }

        PressedElement.IsPressed = false;
        PressedElement = null;
    }

    private static IInputPressable? ResolvePressable(
        UIElement? target,
        ElementInputRouteMap? routeMap)
    {
        IEnumerable<UIElement> route = routeMap is null
            ? VisualRoute(target)
            : target is null ? [] : routeMap.GetRouteToRoot(target);
        foreach (UIElement current in route)
        {
            if (current is IInputPressable pressable)
            {
                return pressable;
            }
        }

        return null;
    }

    private static IEnumerable<UIElement> VisualRoute(UIElement? target)
    {
        for (UIElement? current = target; current is not null; current = current.VisualParent)
        {
            yield return current;
        }
    }
}
