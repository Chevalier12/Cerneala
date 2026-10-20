using Cerneala.UI.Elements;

namespace Cerneala.UI.Input;

public sealed class PressedStateTracker
{
    // The element that a Space press of the input bridge holds pressed. As in WPF, where ButtonBase.IsSpaceKeyDown keeps
    // IsPressed through a mouse release and a lost mouse capture, an element that the left mouse button and Space both
    // press stays pressed until both have let it go (#661).
    private IInputPressable? keyboardPressedElement;

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

        if (!ReferenceEquals(PressedElement, keyboardPressedElement))
        {
            PressedElement.IsPressed = false;
        }

        PressedElement = null;
    }

    internal void PressKeyboard(IInputPressable pressable)
    {
        if (!ReferenceEquals(keyboardPressedElement, pressable))
        {
            ReleaseKeyboard();
        }

        keyboardPressedElement = pressable;
        pressable.IsPressed = true;
    }

    internal void ReleaseKeyboard()
    {
        if (keyboardPressedElement is null)
        {
            return;
        }

        if (!ReferenceEquals(keyboardPressedElement, PressedElement))
        {
            keyboardPressedElement.IsPressed = false;
        }

        keyboardPressedElement = null;
    }

    private static IInputPressable? ResolvePressable(
        UIElement? target,
        ElementInputRouteMap? routeMap)
    {
        IEnumerable<UIElement> route = routeMap is null
            ? VisualRoute(target)
            : target is null ? [] : routeMap.GetVisualRouteToRoot(target);
        foreach (UIElement current in route)
        {
            if (current is IInputPressable pressable)
            {
                return pressable;
            }

            // The press belongs to an element that owns its clicks, such as a non-editable ComboBox field, so an
            // enclosing button is not pressed.
            if (current.OwnsPointerClick)
            {
                return null;
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
