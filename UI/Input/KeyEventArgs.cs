namespace Cerneala.UI.Input;

public sealed class KeyEventArgs : RoutedEventArgs
{
    public KeyEventArgs(RoutedEvent routedEvent, object originalSource, InputKey key)
        : this(routedEvent, originalSource, key, isControlDown: false, isShiftDown: false, isAltDown: false)
    {
    }

    public KeyEventArgs(
        RoutedEvent routedEvent,
        object originalSource,
        InputKey key,
        bool isControlDown,
        bool isShiftDown,
        bool isAltDown)
        : base(routedEvent, originalSource)
    {
        Key = key;
        IsControlDown = isControlDown;
        IsShiftDown = isShiftDown;
        IsAltDown = isAltDown;
    }

    public InputKey Key { get; }

    public bool IsControlDown { get; }

    public bool IsShiftDown { get; }

    public bool IsAltDown { get; }
}
