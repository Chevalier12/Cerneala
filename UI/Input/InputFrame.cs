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
    {
        ArgumentNullException.ThrowIfNull(previousPointer);
        ArgumentNullException.ThrowIfNull(currentPointer);
        ArgumentNullException.ThrowIfNull(previousKeyboard);
        ArgumentNullException.ThrowIfNull(currentKeyboard);
        ArgumentNullException.ThrowIfNull(textInputEvents);
        ArgumentNullException.ThrowIfNull(keyRepeatEvents);

        Pointer = new PointerFrame(previousPointer, currentPointer);
        Keyboard = new KeyboardFrame(
            previousKeyboard,
            currentKeyboard,
            keyRepeatEvents.Where(key => key is not InputKey.None and not InputKey.Unknown).ToArray());
        TextInputEvents = Array.AsReadOnly(textInputEvents.ToArray());
    }

    public PointerFrame Pointer { get; }

    public KeyboardFrame Keyboard { get; }

    public IReadOnlyList<TextInputSnapshotEvent> TextInputEvents { get; }

    public sealed class PointerFrame
    {
        private readonly PointerSnapshot previous;
        private readonly PointerSnapshot current;

        internal PointerFrame(PointerSnapshot previous, PointerSnapshot current)
        {
            this.previous = previous;
            this.current = current;
        }

        public float X => current.X;

        public float Y => current.Y;

        public int WheelValue => current.WheelValue;

        public int WheelDelta => current.WheelValue - previous.WheelValue;

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
