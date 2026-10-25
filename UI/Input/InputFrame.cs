namespace Cerneala.UI.Input;

public sealed class InputFrame
{
    public InputFrame(
        PointerSnapshot previousPointer,
        PointerSnapshot currentPointer,
        KeyboardSnapshot previousKeyboard,
        KeyboardSnapshot currentKeyboard,
        IReadOnlyList<TextInputSnapshotEvent> textInputEvents)
        : this(previousPointer, currentPointer, previousKeyboard, currentKeyboard, textInputEvents, [])
    {
    }

    public InputFrame(
        PointerSnapshot previousPointer,
        PointerSnapshot currentPointer,
        KeyboardSnapshot previousKeyboard,
        KeyboardSnapshot currentKeyboard,
        IReadOnlyList<TextInputSnapshotEvent> textInputEvents,
        IReadOnlyList<InputKey> keyRepeatEvents)
        : this(
            previousPointer,
            currentPointer,
            previousKeyboard,
            currentKeyboard,
            textInputEvents,
            keyRepeatEvents,
            SingleWheelEvent(previousPointer, currentPointer))
    {
    }

    public InputFrame(
        PointerSnapshot previousPointer,
        PointerSnapshot currentPointer,
        KeyboardSnapshot previousKeyboard,
        KeyboardSnapshot currentKeyboard,
        IReadOnlyList<TextInputSnapshotEvent> textInputEvents,
        IReadOnlyList<InputKey> keyRepeatEvents,
        IReadOnlyList<int> wheelDeltas)
    {
        ArgumentNullException.ThrowIfNull(previousPointer);
        ArgumentNullException.ThrowIfNull(currentPointer);
        ArgumentNullException.ThrowIfNull(previousKeyboard);
        ArgumentNullException.ThrowIfNull(currentKeyboard);
        ArgumentNullException.ThrowIfNull(textInputEvents);
        ArgumentNullException.ThrowIfNull(keyRepeatEvents);
        ArgumentNullException.ThrowIfNull(wheelDeltas);

        int[] wheelEvents = wheelDeltas.Where(delta => delta != 0).ToArray();
        int total = 0;
        foreach (int delta in wheelEvents)
        {
            total = unchecked(total + delta);
        }

        if (total != unchecked(currentPointer.WheelValue - previousPointer.WheelValue))
        {
            throw new ArgumentException(
                "The wheel deltas must add up to the wheel change between the previous and current pointer snapshots.",
                nameof(wheelDeltas));
        }

        Pointer = new PointerFrame(previousPointer, currentPointer, wheelEvents);
        Keyboard = new KeyboardFrame(
            previousKeyboard,
            currentKeyboard,
            keyRepeatEvents.Where(key => key is not InputKey.None and not InputKey.Unknown).ToArray());
        TextInputEvents = Array.AsReadOnly(textInputEvents.ToArray());
    }

    private InputFrame(
        PointerFrame pointer,
        KeyboardFrame keyboard,
        IReadOnlyList<TextInputSnapshotEvent> textInputEvents,
        PressClickSequence pressClickSequence)
    {
        Pointer = pointer;
        Keyboard = keyboard;
        TextInputEvents = textInputEvents;
        PressClickSequence = pressClickSequence;
    }

    public PointerFrame Pointer { get; }

    // Only Servo input sets it; see PressClickSequence.
    internal PressClickSequence PressClickSequence { get; }

    public KeyboardFrame Keyboard { get; }

    public IReadOnlyList<TextInputSnapshotEvent> TextInputEvents { get; }

    // A frame built without wheel events reports the snapshots' wheel change as one event.
    private static int[] SingleWheelEvent(PointerSnapshot? previous, PointerSnapshot? current) =>
        previous is null || current is null || current.WheelValue == previous.WheelValue
            ? []
            : [unchecked(current.WheelValue - previous.WheelValue)];

    // The same frame with the pointer at (x, y) before and after it: buttons, wheel,
    // keys and text are unchanged, and the frame itself moves nothing.
    internal InputFrame WithPointerPosition(float x, float y) =>
        new(
            new PointerFrame(Pointer.Previous.WithPosition(x, y), Pointer.Current.WithPosition(x, y), [.. Pointer.WheelDeltas]),
            Keyboard,
            TextInputEvents,
            PressClickSequence);

    // The same frame whose mouse presses relate to the earlier click sequence as given.
    internal InputFrame WithPressClickSequence(PressClickSequence pressClickSequence) =>
        new(Pointer, Keyboard, TextInputEvents, pressClickSequence);

    public sealed class PointerFrame
    {
        private readonly PointerSnapshot previous;
        private readonly PointerSnapshot current;

        internal PointerFrame(PointerSnapshot previous, PointerSnapshot current, int[] wheelDeltas)
        {
            this.previous = previous;
            this.current = current;
            WheelDeltas = Array.AsReadOnly(wheelDeltas);
        }

        internal PointerSnapshot Previous => previous;

        internal PointerSnapshot Current => current;

        public float X => current.X;

        public float Y => current.Y;

        public int WheelValue => current.WheelValue;

        public int WheelDelta => current.WheelValue - previous.WheelValue;

        public IReadOnlyList<int> WheelDeltas { get; }

        public bool IsDown(InputMouseButton button)
        {
            return current.IsDown(button);
        }

        public bool IsPressed(InputMouseButton button)
        {
            return current.IsDown(button) && !previous.IsDown(button);
        }

        public bool IsReleased(InputMouseButton button)
        {
            return !current.IsDown(button) && previous.IsDown(button);
        }
    }

    public sealed class KeyboardFrame
    {
        private readonly KeyboardSnapshot previous;
        private readonly KeyboardSnapshot current;
        private readonly InputKey[] repeatEvents;

        internal KeyboardFrame(KeyboardSnapshot previous, KeyboardSnapshot current, InputKey[] repeatEvents)
        {
            this.previous = previous;
            this.current = current;
            this.repeatEvents = repeatEvents;
        }

        internal KeyboardSnapshot Current => current;

        public bool IsDown(InputKey key)
        {
            return current.IsDown(key);
        }

        public bool IsPressed(InputKey key)
        {
            return current.IsDown(key) && !previous.IsDown(key);
        }

        public bool IsReleased(InputKey key)
        {
            return !current.IsDown(key) && previous.IsDown(key);
        }

        public int GetRepeatCount(InputKey key)
        {
            int count = 0;
            foreach (InputKey repeated in repeatEvents)
            {
                if (repeated == key)
                {
                    count++;
                }
            }

            return count;
        }
    }
}
