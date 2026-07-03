namespace Cerneala.UI.Input;

public sealed class KeyBinding : InputBinding
{
    public KeyBinding(ICommand command, InputKey key, KeyModifiers modifiers = KeyModifiers.None, object? commandParameter = null)
        : this(command, new KeyGesture(key, modifiers), commandParameter)
    {
    }

    public KeyBinding(ICommand command, KeyGesture gesture, object? commandParameter = null)
        : base(command, gesture, commandParameter)
    {
        KeyGesture = gesture;
    }

    public KeyGesture KeyGesture { get; }
}
