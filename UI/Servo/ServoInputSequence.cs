using Cerneala.UI.Input;

namespace Cerneala.UI.Servo;

internal sealed class ServoInputSequence
{
    internal ServoInputSequence(IReadOnlyList<ServoInputStep> steps)
    {
        Steps = steps ?? throw new ArgumentNullException(nameof(steps));
    }

    internal IReadOnlyList<ServoInputStep> Steps { get; }

    internal static ServoInputStep CreateResetStep(
        PointerSnapshot pointer,
        KeyboardSnapshot keyboard)
    {
        ArgumentNullException.ThrowIfNull(pointer);
        ArgumentNullException.ThrowIfNull(keyboard);
        PointerSnapshot released = pointer;
        foreach (InputMouseButton button in Enum.GetValues<InputMouseButton>())
        {
            if (button != InputMouseButton.None)
            {
                released = released.WithButton(button, false);
            }
        }

        KeyboardSnapshot emptyKeyboard = KeyboardSnapshot.Empty;
        return new ServoInputStep(
            new InputFrame(pointer, released, keyboard, emptyKeyboard, []),
            released,
            emptyKeyboard);
    }
}

internal readonly record struct ServoInputStep(
    InputFrame Frame,
    PointerSnapshot Pointer,
    KeyboardSnapshot Keyboard);
