namespace Cerneala.UI.Motion.States;

public readonly record struct MotionVisualStateSnapshot(
    bool IsPointerOver,
    bool IsKeyboardFocused,
    bool IsKeyboardFocusWithin,
    bool IsEnabled);
