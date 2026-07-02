namespace Cerneala.UI.Input;

public sealed class KeyEventArgs : RoutedEventArgs
{
    public KeyEventArgs(RoutedEvent routedEvent, object originalSource, InputKey key)
        : base(routedEvent, originalSource)
    {
        Key = key;
    }

    public InputKey Key { get; }
}
