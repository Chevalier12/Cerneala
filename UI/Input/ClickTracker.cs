using Cerneala.UI.Elements;

namespace Cerneala.UI.Input;

public sealed class ClickTracker
{
    // WPF's MouseDevice continues a click sequence only for a press that comes within the system double-click time
    // (500 ms, the Windows default) and within the double-click size around the previous press (4 px on each axis).
    private static readonly TimeSpan DoubleClickTime = TimeSpan.FromMilliseconds(500);
    private const float DoubleClickHalfSize = 4f;

    // One press per mouse button, as each WPF mouse button has its own down and up messages: a right click while the
    // left button is held does not complete or clear the left press (#593).
    private readonly UIElement?[] pressedTargets = new UIElement?[(int)InputMouseButton.XButton2 + 1];
    private InputMouseButton lastPressedButton;
    private UIElement? lastPressedTarget;
    private TimeSpan lastPressTime;
    private float lastPressX;
    private float lastPressY;
    private int lastPressClickCount;

    public void Press(UIElement? target)
    {
        Press(InputMouseButton.Left, target, 0f, 0f, TimeSpan.Zero);
    }

    public int Release(UIElement? target)
    {
        return Release(InputMouseButton.Left, target);
    }

    internal UIElement? PressedTarget(InputMouseButton button) => pressedTargets[(int)button];

    // As WPF's MouseDevice, the click count is decided on the press: it continues the sequence when the button, the
    // target, the time and the position match the previous press, and starts again at 1 otherwise. A press of another
    // button therefore ends the consecutive-click sequence. A Servo press names its sequence instead of relying on the
    // time: Start always counts 1, and Continue counts like ByTime without the time test.
    internal int Press(
        InputMouseButton button,
        UIElement? target,
        float x,
        float y,
        TimeSpan time,
        PressClickSequence sequence = PressClickSequence.ByTime)
    {
        bool continuesSequence =
            sequence != PressClickSequence.Start &&
            target is not null &&
            button == lastPressedButton &&
            ReferenceEquals(target, lastPressedTarget) &&
            (sequence == PressClickSequence.Continue || time - lastPressTime <= DoubleClickTime) &&
            MathF.Abs(x - lastPressX) <= DoubleClickHalfSize &&
            MathF.Abs(y - lastPressY) <= DoubleClickHalfSize;

        int clickCount = continuesSequence ? lastPressClickCount + 1 : 1;
        lastPressedButton = button;
        lastPressedTarget = target;
        lastPressTime = time;
        lastPressX = x;
        lastPressY = y;
        lastPressClickCount = clickCount;
        pressedTargets[(int)button] = target;
        return clickCount;
    }

    internal int Release(InputMouseButton button, UIElement? target)
    {
        UIElement? pressedTarget = pressedTargets[(int)button];
        bool isClick = pressedTarget is not null && ReferenceEquals(pressedTarget, target);
        pressedTargets[(int)button] = null;
        if (!isClick)
        {
            ResetSequence();
            return 0;
        }

        // As WPF's MouseDevice, which keeps one click count for the device, a release that completes a click reports the
        // count that the most recent press of any button set.
        return lastPressClickCount;
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
        ResetSequence();
    }

    private void ResetSequence()
    {
        lastPressedTarget = null;
        lastPressClickCount = 0;
    }
}
