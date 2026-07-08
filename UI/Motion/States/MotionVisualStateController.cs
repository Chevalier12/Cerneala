using Cerneala.UI.Elements;

namespace Cerneala.UI.Motion.States;

public sealed class MotionVisualStateController
{
    public MotionVisualStateSnapshot Capture(UIElement element)
    {
        ArgumentNullException.ThrowIfNull(element);
        return new MotionVisualStateSnapshot(
            element.IsPointerOver,
            element.IsKeyboardFocused,
            element.IsKeyboardFocusWithin,
            element.IsEnabled);
    }
}
