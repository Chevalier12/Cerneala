using Cerneala.UI.Controls.Primitives;
using Cerneala.UI.Elements;

namespace Cerneala.UI.Input;

public sealed class PressedStateTracker
{
    public ButtonBase? PressedElement { get; private set; }

    public void Press(UIElement? target)
    {
        if (target is not ButtonBase button)
        {
            Cancel();
            return;
        }

        if (ReferenceEquals(PressedElement, button))
        {
            return;
        }

        Cancel();
        PressedElement = button;
        button.IsPressed = true;
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
