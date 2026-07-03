using Cerneala.UI.Elements;

namespace Cerneala.UI.Input;

public sealed class PressedStateTracker
{
    public IInputPressable? PressedElement { get; private set; }

    public void Press(UIElement? target)
    {
        if (target is not IInputPressable pressable)
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
}
