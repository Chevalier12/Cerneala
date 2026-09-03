using System.Globalization;
using Cerneala.UI.Hosting;
using Cerneala.UI.Input;

namespace Cerneala.UI.Servo;

internal sealed class RetainedServoInputDriver : IServoInputDriver
{
    private const ServoModifiers AllModifiers =
        ServoModifiers.Shift | ServoModifiers.Control | ServoModifiers.Alt;

    private readonly UiHost host;
    private readonly Func<ServoInputSequence, CancellationToken, Task>? dispatchSequence;
    private PointerSnapshot pointer = PointerSnapshot.Empty;
    private KeyboardSnapshot keyboard = KeyboardSnapshot.Empty;

    internal RetainedServoInputDriver(UiHost host)
    {
        this.host = host ?? throw new ArgumentNullException(nameof(host));
    }

    internal RetainedServoInputDriver(
        UiHost host,
        Func<ServoInputSequence, CancellationToken, Task> dispatchSequence)
        : this(host)
    {
        this.dispatchSequence = dispatchSequence ??
            throw new ArgumentNullException(nameof(dispatchSequence));
    }

    public Task HoverAsync(float x, float y, CancellationToken cancellationToken)
    {
        ValidatePointerPosition(x, y);
        List<ServoInputStep> steps = [];
        AppendPointerMove(steps, x, y);
        return DispatchAsync(steps, cancellationToken);
    }

    public Task ClickAsync(float x, float y, CancellationToken cancellationToken)
    {
        ValidatePointerPosition(x, y);
        List<ServoInputStep> steps = [];
        AppendPointerMove(steps, x, y);
        AppendPointerButton(steps, InputMouseButton.Left, isDown: true);
        AppendPointerButton(steps, InputMouseButton.Left, isDown: false);
        return DispatchAsync(steps, cancellationToken);
    }

    public Task DragAsync(
        float startX,
        float startY,
        float endX,
        float endY,
        int steps,
        CancellationToken cancellationToken)
    {
        ValidatePointerPosition(startX, startY);
        ValidatePointerPosition(endX, endY);
        ArgumentOutOfRangeException.ThrowIfLessThan(steps, 1);
        List<ServoInputStep> frames = new(steps + 3);
        AppendPointerMove(frames, startX, startY);
        AppendPointerButton(frames, InputMouseButton.Left, isDown: true);
        for (int step = 1; step <= steps; step++)
        {
            float progress = step / (float)steps;
            AppendPointerMove(
                frames,
                Lerp(startX, endX, progress),
                Lerp(startY, endY, progress));
        }

        AppendPointerButton(frames, InputMouseButton.Left, isDown: false);
        return DispatchAsync(frames, cancellationToken);
    }

    public Task ScrollAsync(
        float x,
        float y,
        int wheelDelta,
        CancellationToken cancellationToken)
    {
        ValidatePointerPosition(x, y);
        List<ServoInputStep> steps = [];
        AppendPointerMove(steps, x, y);
        PointerSnapshot next = pointer.WithWheelValue(checked(pointer.WheelValue + wheelDelta));
        AppendStep(steps, next, keyboard, []);
        return DispatchAsync(steps, cancellationToken);
    }

    public Task PressKeyAsync(
        InputKey key,
        ServoModifiers modifiers,
        CancellationToken cancellationToken)
    {
        ValidateKey(key);
        ValidateModifiers(modifiers);
        InputKey[] modifierKeys = ModifierKeys(modifiers);
        List<ServoInputStep> steps = [];
        AppendKeyboard(steps, modifierKeys);
        AppendKeyboard(steps, [.. modifierKeys, key]);
        AppendKeyboard(steps, modifierKeys);
        AppendKeyboard(steps, []);
        return DispatchAsync(steps, cancellationToken);
    }

    public Task SendTextAsync(string text, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(text);
        List<ServoInputStep> steps = [];
        TextElementEnumerator enumerator = StringInfo.GetTextElementEnumerator(text);
        while (enumerator.MoveNext())
        {
            AppendStep(
                steps,
                pointer,
                keyboard,
                [new TextInputSnapshotEvent(enumerator.GetTextElement())]);
        }

        return DispatchAsync(steps, cancellationToken);
    }

    internal void ClickAt(float x, float y)
    {
        ValidatePointerPosition(x, y);
        List<ServoInputStep> steps = [];
        AppendPointerMove(steps, x, y);
        AppendPointerButton(steps, InputMouseButton.Left, isDown: true);
        AppendPointerButton(steps, InputMouseButton.Left, isDown: false);
        DispatchImmediately(steps);
    }

    internal void MovePointerTo(float x, float y)
    {
        ValidatePointerPosition(x, y);
        List<ServoInputStep> steps = [];
        AppendPointerMove(steps, x, y);
        DispatchImmediately(steps);
    }

    internal void SetPointerButtonAt(
        float x,
        float y,
        InputMouseButton button,
        bool isDown)
    {
        ValidatePointerPosition(x, y);
        if (button == InputMouseButton.None)
        {
            throw new ArgumentOutOfRangeException(nameof(button));
        }

        List<ServoInputStep> steps = [];
        AppendPointerMove(steps, x, y);
        AppendPointerButton(steps, button, isDown);
        DispatchImmediately(steps);
    }

    internal void ScrollPointerAt(float x, float y, int wheelDelta)
    {
        ValidatePointerPosition(x, y);
        List<ServoInputStep> steps = [];
        AppendPointerMove(steps, x, y);
        PointerSnapshot next = pointer.WithWheelValue(checked(pointer.WheelValue + wheelDelta));
        AppendStep(steps, next, keyboard, []);
        DispatchImmediately(steps);
    }

    internal void LeavePointer()
    {
        List<ServoInputStep> steps = [];
        AppendPointerMove(steps, -1, -1);
        DispatchImmediately(steps);
    }

    internal void SetKeyState(InputKey key, bool isDown)
    {
        ValidateKey(key);
        HashSet<InputKey> keys = Enum.GetValues<InputKey>()
            .Where(keyboard.IsDown)
            .ToHashSet();
        if (isDown)
        {
            keys.Add(key);
        }
        else
        {
            keys.Remove(key);
        }

        List<ServoInputStep> steps = [];
        AppendKeyboard(steps, keys);
        DispatchImmediately(steps);
    }

    internal void PressKey(InputKey key, ServoModifiers modifiers)
    {
        PressKeyAsync(key, modifiers, CancellationToken.None).GetAwaiter().GetResult();
    }

    internal void SendText(string text)
    {
        SendTextAsync(text, CancellationToken.None).GetAwaiter().GetResult();
    }

    internal void ResetInput()
    {
        ServoInputStep reset = ServoInputSequence.CreateResetStep(pointer, keyboard);
        pointer = reset.Pointer;
        keyboard = reset.Keyboard;
        DispatchImmediately([reset]);
    }

    internal InputFrame GetCurrentFrame() =>
        new(pointer, pointer, keyboard, keyboard, []);

    internal bool HasActivePointerRepeat => host.InputBridge.HasActivePointerRepeat;

    private async Task DispatchAsync(
        IReadOnlyList<ServoInputStep> steps,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (steps.Count == 0)
        {
            return;
        }

        ServoInputSequence sequence = new(steps);
        if (dispatchSequence is not null)
        {
            await dispatchSequence(sequence, cancellationToken).ConfigureAwait(false);
            return;
        }

        DispatchImmediately(steps, cancellationToken);
    }

    private void DispatchImmediately(
        IReadOnlyList<ServoInputStep> steps,
        CancellationToken cancellationToken = default)
    {
        ServoInputStep? attempted = null;
        try
        {
            foreach (ServoInputStep step in steps)
            {
                cancellationToken.ThrowIfCancellationRequested();
                attempted = step;
                host.Update(step.Frame, host.Viewport, TimeSpan.Zero);
            }

            cancellationToken.ThrowIfCancellationRequested();
        }
        catch
        {
            if (attempted is ServoInputStep current)
            {
                ServoInputStep reset = ServoInputSequence.CreateResetStep(
                    current.Pointer,
                    current.Keyboard);
                pointer = reset.Pointer;
                keyboard = reset.Keyboard;
                try
                {
                    host.Update(reset.Frame, host.Viewport, TimeSpan.Zero);
                }
                catch
                {
                    // Preserve the original operation failure after attempting the mandatory reset.
                }
            }

            throw;
        }
    }

    private void AppendPointerMove(List<ServoInputStep> steps, float x, float y)
    {
        AppendStep(steps, pointer.WithPosition(x, y), keyboard, []);
    }

    private void AppendPointerButton(
        List<ServoInputStep> steps,
        InputMouseButton button,
        bool isDown)
    {
        AppendStep(steps, pointer.WithButton(button, isDown), keyboard, []);
    }

    private void AppendKeyboard(
        List<ServoInputStep> steps,
        IEnumerable<InputKey> downKeys)
    {
        AppendStep(steps, pointer, KeyboardSnapshot.FromDownKeys(downKeys), []);
    }

    private void AppendStep(
        List<ServoInputStep> steps,
        PointerSnapshot nextPointer,
        KeyboardSnapshot nextKeyboard,
        IReadOnlyList<TextInputSnapshotEvent> textInputEvents)
    {
        InputFrame frame = new(pointer, nextPointer, keyboard, nextKeyboard, textInputEvents);
        pointer = nextPointer;
        keyboard = nextKeyboard;
        steps.Add(new ServoInputStep(frame, pointer, keyboard));
    }

    private static InputKey[] ModifierKeys(ServoModifiers modifiers)
    {
        List<InputKey> keys = [];
        if ((modifiers & ServoModifiers.Shift) != 0)
        {
            keys.Add(InputKey.LeftShift);
        }

        if ((modifiers & ServoModifiers.Control) != 0)
        {
            keys.Add(InputKey.LeftCtrl);
        }

        if ((modifiers & ServoModifiers.Alt) != 0)
        {
            keys.Add(InputKey.LeftAlt);
        }

        return keys.ToArray();
    }

    private static void ValidateKey(InputKey key)
    {
        if (!Enum.IsDefined(key) || key is InputKey.None or InputKey.Unknown)
        {
            throw new ArgumentOutOfRangeException(nameof(key));
        }
    }

    private static void ValidateModifiers(ServoModifiers modifiers)
    {
        if ((modifiers & ~AllModifiers) != 0)
        {
            throw new ArgumentOutOfRangeException(nameof(modifiers));
        }
    }

    private static void ValidatePointerPosition(float x, float y)
    {
        if (!float.IsFinite(x) || !float.IsFinite(y))
        {
            throw new ArgumentOutOfRangeException(nameof(x), "Servo pointer coordinates must be finite.");
        }
    }

    private static float Lerp(float start, float end, float progress) =>
        start + ((end - start) * progress);
}
