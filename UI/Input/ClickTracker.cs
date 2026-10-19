using Cerneala.UI.Elements;

namespace Cerneala.UI.Input;

public sealed class ClickTracker
{
    // One press per mouse button, as each WPF mouse button has its own down and up messages: a right click while the
    // left button is held does not complete or clear the left press (#593).
    private readonly UIElement?[] pressedTargets = new UIElement?[(int)InputMouseButton.XButton2 + 1];
    private InputMouseButton lastPressedButton;
    private InputMouseButton lastClickedButton;
    private UIElement? lastClickedTarget;
    private int clickCount;

    public void Press(UIElement? target)
    {
        Press(InputMouseButton.Left, target);
    }

    public int Release(UIElement? target)
    {
        return Release(InputMouseButton.Left, target);
    }

    internal UIElement? PressedTarget(InputMouseButton button) => pressedTargets[(int)button];

    // As WPF's MouseDevice starts the click count at 1 when the pressed button differs from the previously pressed
    // button, a press of another button ends the consecutive-click sequence.
    internal void Press(InputMouseButton button, UIElement? target)
    {
        if (button != lastPressedButton)
        {
            lastClickedTarget = null;
            clickCount = 0;
        }

        lastPressedButton = button;
        pressedTargets[(int)button] = target;
    }

    internal int Release(InputMouseButton button, UIElement? target)
    {
        UIElement? pressedTarget = pressedTargets[(int)button];
        bool isClick = pressedTarget is not null && ReferenceEquals(pressedTarget, target);
        pressedTargets[(int)button] = null;
        if (!isClick)
        {
            lastClickedTarget = null;
            clickCount = 0;
            return 0;
        }

        clickCount = ReferenceEquals(lastClickedTarget, target) && lastClickedButton == button ? clickCount + 1 : 1;
        lastClickedTarget = target;
        lastClickedButton = button;
        return clickCount;
    }

    // A press that did not reach its click target, because a handler marked its preview handled, completes no click on
    // release (#620).
    internal void CancelPress(InputMouseButton button)
    {
        pressedTargets[(int)button] = null;
    }

    public void Cancel()
    {
        Array.Clear(pressedTargets);
        lastClickedTarget = null;
        clickCount = 0;
    }
}
