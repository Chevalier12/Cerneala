namespace Cerneala.UI.Input;

public class RoutedPropertyChangedEventArgs<T> : RoutedEventArgs
{
    public RoutedPropertyChangedEventArgs(T oldValue, T newValue)
    {
        OldValue = oldValue;
        NewValue = newValue;
    }

    public RoutedPropertyChangedEventArgs(RoutedEvent routedEvent, object source, T oldValue, T newValue)
        : base(routedEvent, source)
    {
        OldValue = oldValue;
        NewValue = newValue;
    }

    public T OldValue { get; }
    public T NewValue { get; }
}
