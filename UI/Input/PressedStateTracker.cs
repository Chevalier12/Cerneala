using Cerneala.UI.Elements;

namespace Cerneala.UI.Input;

public sealed class PressedStateTracker
{
    public IInputPressable? PressedElement { get; private set; }

    public void Press(UIElement? target)
    {
        IInputPressable? pressable = ResolvePressable(target);
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

    private static IInputPressable? ResolvePressable(UIElement? target)
    {
        for (UIElement? current = target; current is not null; current = current.VisualParent)
        {
            if (current is IInputPressable pressable)
            {
                return pressable;
            }
        }

        return null;
    }
}
