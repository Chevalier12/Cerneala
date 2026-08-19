using System.Globalization;
using Cerneala.UI.Elements;
using Cerneala.UI.Hosting;
using Cerneala.UI.Input;
using Cerneala.UI.Layout;

namespace Cerneala.UI.Automation;

public sealed class RetainedAutomationInputDriver : IAutomationInputDriver
{
    private readonly UiHost host;
    private readonly Func<IReadOnlyList<InputFrame>, CancellationToken, Task>? frameSynchronizedDrag;
    private readonly HashSet<InputMouseButton> pressedButtons = [];
    private readonly HashSet<InputKey> pressedKeys = [];
    private PointerSnapshot pointer = PointerSnapshot.Empty;
    private KeyboardSnapshot keyboard = KeyboardSnapshot.Empty;
    private bool dragging;

    public RetainedAutomationInputDriver(UiHost host)
    {
        this.host = host ?? throw new ArgumentNullException(nameof(host));
    }

    internal RetainedAutomationInputDriver(
        UiHost host,
        Func<IReadOnlyList<InputFrame>, CancellationToken, Task> frameSynchronizedDrag)
        : this(host)
    {
        this.frameSynchronizedDrag = frameSynchronizedDrag ??
            throw new ArgumentNullException(nameof(frameSynchronizedDrag));
    }

    public void Click(UIElement target)
    {
        ArgumentNullException.ThrowIfNull(target);
        if (!ReferenceEquals(target.Root, host.Root))
        {
            throw new InvalidOperationException("The automation target does not belong to this driver's UI root.");
        }

        LayoutRect bounds = target.ArrangedBounds;
        if (bounds.Width <= 0 || bounds.Height <= 0)
        {
            throw new InvalidOperationException("The automation target has no arranged hit-test area.");
        }

        MovePointer(bounds.X + (bounds.Width / 2), bounds.Y + (bounds.Height / 2));
        SetPointerButton(InputMouseButton.Left, true);
        SetPointerButton(InputMouseButton.Left, false);
    }

    internal void ClickAt(float x, float y)
    {
        ValidatePointerPosition(x, y);

        MovePointer(x, y);
        SetPointerButton(InputMouseButton.Left, true);
        SetPointerButton(InputMouseButton.Left, false);
    }

    internal void MovePointerTo(float x, float y)
    {
        ValidatePointerPosition(x, y);
        MovePointer(x, y);
    }

    internal void SetPointerButtonAt(float x, float y, InputMouseButton button, bool isDown)
    {
        ValidatePointerPosition(x, y);
        if (button == InputMouseButton.None)
        {
            throw new ArgumentOutOfRangeException(nameof(button));
        }

        MovePointer(x, y);
        SetPointerButton(button, isDown);
    }

    internal void ScrollPointerAt(float x, float y, int wheelDelta)
    {
        ValidatePointerPosition(x, y);
        MovePointer(x, y);
        PointerSnapshot next = pointer.WithWheelValue(checked(pointer.WheelValue + wheelDelta));
        Dispatch(next, keyboard, []);
        pointer = next;
    }

    internal void LeavePointer() => MovePointer(-1, -1);

    internal void SetKeyState(InputKey key, bool isDown)
    {
        if (key is InputKey.None or InputKey.Unknown)
        {
            throw new ArgumentOutOfRangeException(nameof(key));
        }

        if (isDown)
        {
            pressedKeys.Add(key);
        }
        else
        {
            pressedKeys.Remove(key);
        }

        SetKeyboard(pressedKeys);
    }

    internal void ResetInput()
    {
        PointerSnapshot nextPointer = pointer;
        foreach (InputMouseButton button in pressedButtons)
        {
            nextPointer = nextPointer.WithButton(button, false);
        }

        KeyboardSnapshot nextKeyboard = KeyboardSnapshot.Empty;
        Dispatch(nextPointer, nextKeyboard, []);
        pointer = nextPointer;
        keyboard = nextKeyboard;
        pressedButtons.Clear();
        pressedKeys.Clear();
    }

    internal InputFrame GetCurrentFrame() =>
        new(pointer, pointer, keyboard, keyboard, []);

    public async Task DragAsync(
        UIElement target,
        float startXRatio,
        float startYRatio,
        float endXRatio,
        float endYRatio,
        int steps = 12,
        CancellationToken cancellationToken = default)
    {
        ValidateTarget(target);
        ValidateRatio(startXRatio, nameof(startXRatio));
        ValidateRatio(startYRatio, nameof(startYRatio));
        ValidateRatio(endXRatio, nameof(endXRatio));
        ValidateRatio(endYRatio, nameof(endYRatio));
        ArgumentOutOfRangeException.ThrowIfLessThan(steps, 1);
        cancellationToken.ThrowIfCancellationRequested();
        if (dragging)
        {
            throw new InvalidOperationException("An automation pointer drag is already in progress.");
        }

        LayoutRect bounds = target.ArrangedBounds;
        if (bounds.Width <= 0 || bounds.Height <= 0)
        {
            throw new InvalidOperationException("The automation target has no arranged hit-test area.");
        }

        dragging = true;
        try
        {
            List<InputFrame> frames = new(steps + 3);
            AppendPointerMove(frames, PointInBounds(bounds, startXRatio, startYRatio));
            AppendPointerButton(frames, InputMouseButton.Left, isDown: true);
            for (int step = 1; step <= steps; step++)
            {
                float progress = step / (float)steps;
                AppendPointerMove(frames, PointInBounds(
                    bounds,
                    Lerp(startXRatio, endXRatio, progress),
                    Lerp(startYRatio, endYRatio, progress)));
            }
            AppendPointerButton(frames, InputMouseButton.Left, isDown: false);

            if (frameSynchronizedDrag is not null)
            {
                await frameSynchronizedDrag(frames, cancellationToken);
            }
            else
            {
                foreach (InputFrame frame in frames)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    host.Update(frame, host.Viewport, TimeSpan.Zero);
                }
            }
        }
        finally
        {
            dragging = false;
        }
    }

    public void PressKey(InputKey key, AutomationModifiers modifiers = AutomationModifiers.None)
    {
        if (key is InputKey.None or InputKey.Unknown)
        {
            throw new ArgumentOutOfRangeException(nameof(key));
        }

        InputKey[] modifierKeys = ModifierKeys(modifiers);
        SetKeyboard([.. modifierKeys]);
        SetKeyboard([.. modifierKeys, key]);
        SetKeyboard([.. modifierKeys]);
        SetKeyboard([]);
    }

    public void SendText(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        TextElementEnumerator enumerator = StringInfo.GetTextElementEnumerator(text);
        while (enumerator.MoveNext())
        {
            Dispatch([new TextInputSnapshotEvent(enumerator.GetTextElement())]);
        }
    }

    private void MovePointer(float x, float y)
    {
        PointerSnapshot next = pointer.WithPosition(x, y);
        Dispatch(next, keyboard, []);
        pointer = next;
    }

    private void AppendPointerMove(List<InputFrame> frames, LayoutPoint point)
    {
        PointerSnapshot next = pointer.WithPosition(point.X, point.Y);
        frames.Add(new InputFrame(pointer, next, keyboard, keyboard, []));
        pointer = next;
    }

    private void AppendPointerButton(List<InputFrame> frames, InputMouseButton button, bool isDown)
    {
        PointerSnapshot next = pointer.WithButton(button, isDown);
        frames.Add(new InputFrame(pointer, next, keyboard, keyboard, []));
        pointer = next;
    }

    private void SetPointerButton(InputMouseButton button, bool isDown)
    {
        PointerSnapshot next = pointer.WithButton(button, isDown);
        Dispatch(next, keyboard, []);
        pointer = next;
        if (isDown)
        {
            pressedButtons.Add(button);
        }
        else
        {
            pressedButtons.Remove(button);
        }
    }

    private void SetKeyboard(IReadOnlyCollection<InputKey> downKeys)
    {
        InputKey[] keys = downKeys.ToArray();
        KeyboardSnapshot next = KeyboardSnapshot.FromDownKeys(keys);
        Dispatch(pointer, next, []);
        keyboard = next;
        pressedKeys.Clear();
        pressedKeys.UnionWith(keys);
    }

    private void Dispatch(IReadOnlyList<TextInputSnapshotEvent> textInputEvents)
    {
        Dispatch(pointer, keyboard, textInputEvents);
    }

    private void Dispatch(
        PointerSnapshot nextPointer,
        KeyboardSnapshot nextKeyboard,
        IReadOnlyList<TextInputSnapshotEvent> textInputEvents)
    {
        InputFrame frame = new(pointer, nextPointer, keyboard, nextKeyboard, textInputEvents);
        host.Update(frame, host.Viewport, TimeSpan.Zero);
    }

    private static InputKey[] ModifierKeys(AutomationModifiers modifiers)
    {
        List<InputKey> keys = [];
        if ((modifiers & AutomationModifiers.Shift) != 0)
        {
            keys.Add(InputKey.LeftShift);
        }

        if ((modifiers & AutomationModifiers.Control) != 0)
        {
            keys.Add(InputKey.LeftCtrl);
        }

        if ((modifiers & AutomationModifiers.Alt) != 0)
        {
            keys.Add(InputKey.LeftAlt);
        }

        return keys.ToArray();
    }

    private void ValidateTarget(UIElement target)
    {
        ArgumentNullException.ThrowIfNull(target);
        if (!ReferenceEquals(target.Root, host.Root))
        {
            throw new InvalidOperationException("The automation target does not belong to this driver's UI root.");
        }
    }

    private static void ValidateRatio(float ratio, string parameterName)
    {
        if (!float.IsFinite(ratio) || ratio is < 0 or > 1)
        {
            throw new ArgumentOutOfRangeException(parameterName, ratio, "Automation drag ratios must be between 0 and 1.");
        }
    }

    private static void ValidatePointerPosition(float x, float y)
    {
        if (!float.IsFinite(x) || !float.IsFinite(y))
        {
            throw new ArgumentOutOfRangeException(nameof(x), "Preview pointer coordinates must be finite.");
        }
    }

    private static LayoutPoint PointInBounds(LayoutRect bounds, float xRatio, float yRatio)
    {
        float x = xRatio == 1
            ? MathF.BitDecrement(bounds.X + bounds.Width)
            : bounds.X + (bounds.Width * xRatio);
        float y = yRatio == 1
            ? MathF.BitDecrement(bounds.Y + bounds.Height)
            : bounds.Y + (bounds.Height * yRatio);
        return new LayoutPoint(x, y);
    }

    private static float Lerp(float start, float end, float progress) =>
        start + ((end - start) * progress);
}
