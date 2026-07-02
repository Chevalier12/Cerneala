namespace Cerneala.UI.Input;

public sealed class KeyboardFocusChangedEventArgs : RoutedEventArgs
{
    public KeyboardFocusChangedEventArgs(RoutedEvent routedEvent, object originalSource, object? oldFocus, object? newFocus)
        : base(routedEvent, originalSource)
    {
        OldFocus = oldFocus;
        NewFocus = newFocus;
    }

    public object? OldFocus { get; }

    public object? NewFocus { get; }
}
