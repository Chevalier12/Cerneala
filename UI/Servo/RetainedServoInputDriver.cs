using System.Globalization;
using Cerneala.UI.Hosting;
using Cerneala.UI.Input;
using Cerneala.UI.Elements;

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

    public Task HoverAsync(Func<UIRoot, ServoActionTarget> resolveTarget, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(resolveTarget);
        return DispatchAsync(new ServoInputSequence(() =>
        {
            ServoActionTarget target = ResolveTarget(resolveTarget);
            List<ServoInputStep> steps = [];
            AppendPointerMove(steps, target.X, target.Y);
            return steps;
        }), cancellationToken);
    }

    public Task ClickAsync(Func<UIRoot, ServoActionTarget> resolveTarget, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(resolveTarget);
        return DispatchAsync(new ServoInputSequence(() =>
        {
            ServoActionTarget target = ResolveTarget(resolveTarget);
            List<ServoInputStep> steps = [];
            AppendPointerMove(steps, target.X, target.Y);
            AppendPointerButton(steps, InputMouseButton.Left, isDown: true);
            AppendPointerButton(steps, InputMouseButton.Left, isDown: false);
            return steps;
        }), cancellationToken);
    }

    public Task DragAsync(
        Func<UIRoot, ServoActionTarget> resolveTarget,
        float endX,
        float endY,
        int steps,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(resolveTarget);
        ValidatePointerPosition(endX, endY);
        ArgumentOutOfRangeException.ThrowIfLessThan(steps, 1);
        return DispatchAsync(new ServoInputSequence(() =>
        {
            ServoActionTarget target = ResolveTarget(resolveTarget);
            List<ServoInputStep> frames = new(steps + 3);
            AppendPointerMove(frames, target.X, target.Y);
            AppendPointerButton(frames, InputMouseButton.Left, isDown: true);
            for (int step = 1; step <= steps; step++)
            {
                float progress = step / (float)steps;
                AppendPointerMove(
                    frames,
                    Lerp(target.X, endX, progress),
                    Lerp(target.Y, endY, progress));
            }

            AppendPointerButton(frames, InputMouseButton.Left, isDown: false);
            return frames;
        }), cancellationToken);
    }

    public Task ScrollAsync(
        Func<UIRoot, ServoActionTarget> resolveTarget,
        int wheelDelta,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(resolveTarget);
        return DispatchAsync(new ServoInputSequence(() =>
        {
            ServoActionTarget target = ResolveTarget(resolveTarget);
            List<ServoInputStep> steps = [];
            AppendPointerMove(steps, target.X, target.Y);
            PointerSnapshot next = pointer.WithWheelValue(checked(pointer.WheelValue + wheelDelta));
            AppendStep(steps, next, keyboard, []);
            return steps;
        }), cancellationToken);
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

    private Task DispatchAsync(
        IReadOnlyList<ServoInputStep> steps,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (steps.Count == 0)
        {
            return Task.CompletedTask;
        }

        return DispatchAsync(new ServoInputSequence(steps), cancellationToken);
    }

    private async Task DispatchAsync(
        ServoInputSequence sequence,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (dispatchSequence is not null)
        {
            await dispatchSequence(sequence, cancellationToken).ConfigureAwait(false);
            return;
        }

        DispatchImmediately(sequence, cancellationToken);
    }

    private void DispatchImmediately(
        IReadOnlyList<ServoInputStep> steps,
        CancellationToken cancellationToken = default)
    {
        if (steps.Count > 0) DispatchImmediately(new ServoInputSequence(steps), cancellationToken);
    }

    private void DispatchImmediately(
        ServoInputSequence sequence,
        CancellationToken cancellationToken)
    {
        ServoInputStep? attempted = null;
        try
        {
            int nextIndex = 0;
            do
            {
                cancellationToken.ThrowIfCancellationRequested();
                host.Update(
                    () =>
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        ServoInputStep step = sequence.Steps[nextIndex++];
                        attempted = step;
                        return step.Frame;
                    },
                    host.Viewport,
                    TimeSpan.Zero,
                    advanceRenderTime: true);
            }
            while (nextIndex < sequence.Steps.Count);

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

    private ServoActionTarget ResolveTarget(Func<UIRoot, ServoActionTarget> resolveTarget)
    {
        ServoActionTarget target = resolveTarget(host.Root ?? throw new ServoException(
            "The Servo UiHost does not currently have a root."));
        ValidatePointerPosition(target.X, target.Y);
        return target;
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
