namespace Cerneala.UI.Motion.Styling;

public readonly record struct MotionVisualStateSnapshot(
    bool IsPointerOver,
    bool IsKeyboardFocused,
    bool IsKeyboardFocusWithin,
    bool IsEnabled);
