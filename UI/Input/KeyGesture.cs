namespace Cerneala.UI.Input;

[Flags]
public enum KeyModifiers
{
    None = 0,
    Shift = 1,
    Control = 2,
    Alt = 4
}

public sealed class KeyGesture : InputGesture
{
    public KeyGesture(InputKey key, KeyModifiers modifiers = KeyModifiers.None)
    {
        if (key is InputKey.None or InputKey.Unknown)
        {
            throw new ArgumentException("Key gesture requires a concrete key.", nameof(key));
        }

        Key = key;
        Modifiers = modifiers;
    }

    public InputKey Key { get; }

    public KeyModifiers Modifiers { get; }

    public override bool Matches(InputFrame frame)
    {
        ArgumentNullException.ThrowIfNull(frame);
        return frame.Keyboard.IsPressed(Key) && ModifiersMatch(frame.Keyboard);
    }

    private bool ModifiersMatch(InputFrame.KeyboardFrame keyboard)
    {
        return HasModifier(keyboard, KeyModifiers.Shift, InputKey.LeftShift, InputKey.RightShift) &&
            HasModifier(keyboard, KeyModifiers.Control, InputKey.LeftCtrl, InputKey.RightCtrl) &&
            HasModifier(keyboard, KeyModifiers.Alt, InputKey.LeftAlt, InputKey.RightAlt);
    }

    private bool HasModifier(InputFrame.KeyboardFrame keyboard, KeyModifiers modifier, InputKey leftKey, InputKey rightKey)
    {
        bool expected = Modifiers.HasFlag(modifier);
        bool actual = keyboard.IsDown(leftKey) || keyboard.IsDown(rightKey);
        return expected == actual;
    }
}
