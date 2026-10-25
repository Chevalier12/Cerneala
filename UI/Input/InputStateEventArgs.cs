using System.Numerics;

namespace Cerneala.UI.Input;

public sealed class InputStateEventArgs : RoutedEventArgs
{
    private readonly KeyboardSnapshot keyboard;
    private readonly PointerSnapshot pointer;

    public InputStateEventArgs(
        RoutedEvent routedEvent,
        object originalSource,
        KeyboardSnapshot keyboard,
        PointerSnapshot pointer,
        TimeSpan elapsed)
        : base(routedEvent, originalSource)
    {
        ArgumentNullException.ThrowIfNull(keyboard);
        ArgumentNullException.ThrowIfNull(pointer);
        this.keyboard = keyboard;
        this.pointer = pointer;
        Elapsed = elapsed;
    }

    // The time since the previous update of the hold: zero on the first update of a hold.
    public TimeSpan Elapsed { get; }

    // The pointer position in root coordinates, as MouseEventArgs.GetPosition reports it relative to the root.
    public Vector2 PointerPosition => new(pointer.X, pointer.Y);

    public bool IsDown(InputKey key)
    {
        return keyboard.IsDown(key);
    }

    public bool IsDown(InputMouseButton button)
    {
        return pointer.IsDown(button);
    }
}
