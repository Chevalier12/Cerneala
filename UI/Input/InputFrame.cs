namespace Cerneala.UI.Input;

public sealed class InputFrame
{
    public InputFrame(
        PointerSnapshot previousPointer,
        PointerSnapshot currentPointer,
        KeyboardSnapshot previousKeyboard,
        KeyboardSnapshot currentKeyboard,
        IReadOnlyList<TextInputSnapshotEvent> textInputEvents)
    {
        ArgumentNullException.ThrowIfNull(previousPointer);
        ArgumentNullException.ThrowIfNull(currentPointer);
        ArgumentNullException.ThrowIfNull(previousKeyboard);
        ArgumentNullException.ThrowIfNull(currentKeyboard);
        ArgumentNullException.ThrowIfNull(textInputEvents);

        Pointer = new PointerFrame(previousPointer, currentPointer);
        Keyboard = new KeyboardFrame(previousKeyboard, currentKeyboard);
        TextInputEvents = textInputEvents.ToArray();
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

        internal KeyboardFrame(KeyboardSnapshot previous, KeyboardSnapshot current)
        {
            this.previous = previous;
            this.current = current;
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
    }
}
