using Cerneala.UI.Elements;

namespace Cerneala.UI.Input;

public sealed class ClickTracker
{
    private UIElement? pressedTarget;
    private UIElement? lastClickedTarget;
    private int clickCount;

    public void Press(UIElement? target)
    {
        pressedTarget = target;
    }

    public int Release(UIElement? target)
    {
        bool isClick = pressedTarget is not null && ReferenceEquals(pressedTarget, target);
        pressedTarget = null;
        if (!isClick)
        {
            lastClickedTarget = null;
            clickCount = 0;
            return 0;
        }

        clickCount = ReferenceEquals(lastClickedTarget, target) ? clickCount + 1 : 1;
        lastClickedTarget = target;
        return clickCount;
    }

    public void Cancel()
    {
        pressedTarget = null;
        lastClickedTarget = null;
        clickCount = 0;
    }
}
