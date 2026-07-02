namespace Cerneala.UI.Input;

public readonly record struct InputButtonState(bool WasDown, bool IsDown)
{
    public bool IsPressed => IsDown && !WasDown;

    public bool IsReleased => !IsDown && WasDown;
}
