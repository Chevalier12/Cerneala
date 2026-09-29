using Cerneala.UI.Input;

namespace Cerneala.UI.Servo;

internal sealed class ServoInputSequence
{
    private IReadOnlyList<ServoInputStep>? steps;
    private readonly Func<IReadOnlyList<ServoInputStep>>? createSteps;

    internal ServoInputSequence(
        IReadOnlyList<ServoInputStep> steps,
        int heldStepIndex = -1,
        TimeSpan holdDuration = default,
        TimeSpan cleanupTimeout = default)
    {
        this.steps = steps ?? throw new ArgumentNullException(nameof(steps));
        if (heldStepIndex >= 0)
        {
            if (heldStepIndex >= steps.Count - 1 ||
                holdDuration <= TimeSpan.Zero ||
                cleanupTimeout <= TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(nameof(heldStepIndex));
            }

            HeldStepIndex = heldStepIndex;
            HoldDuration = holdDuration;
            CleanupTimeout = cleanupTimeout;
            ServoInputStep held = steps[heldStepIndex];
            HoldContinuationStep = new ServoInputStep(
                new InputFrame(held.Pointer, held.Pointer, held.Keyboard, held.Keyboard, []),
                held.Pointer,
                held.Keyboard);
        }
    }

    internal ServoInputSequence(Func<IReadOnlyList<ServoInputStep>> createSteps)
    {
        this.createSteps = createSteps ?? throw new ArgumentNullException(nameof(createSteps));
    }

    // Target-dependent steps are materialized by the input owner after scheduled layout,
    // not when an action is queued from a relay or a presented-frame callback.
    internal IReadOnlyList<ServoInputStep> Steps => steps ??= createSteps!();

    internal int HeldStepIndex { get; } = -1;

    internal TimeSpan HoldDuration { get; }

    internal TimeSpan CleanupTimeout { get; }

    internal ServoInputStep HoldContinuationStep { get; }

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
