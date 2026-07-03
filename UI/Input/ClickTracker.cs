using Cerneala.UI.Elements;

namespace Cerneala.UI.Input;

public sealed class ClickTracker
{
    private UIElement? pressedTarget;

    public void Press(UIElement? target)
    {
        pressedTarget = target;
    }

    public int Release(UIElement? target)
    {
        bool isClick = pressedTarget is not null && ReferenceEquals(pressedTarget, target);
        Cancel();
        return isClick ? 1 : 0;
    }

    public void Cancel()
    {
        pressedTarget = null;
    }
}
