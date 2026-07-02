namespace Cerneala.UI.Input;

public class RoutedEventArgs
{
    public RoutedEventArgs(RoutedEvent routedEvent, object originalSource)
    {
        RoutedEvent = routedEvent ?? throw new ArgumentNullException(nameof(routedEvent));
        OriginalSource = originalSource ?? throw new ArgumentNullException(nameof(originalSource));
        Source = originalSource;
    }

    public RoutedEvent RoutedEvent { get; }

    public object OriginalSource { get; }

    public object Source { get; set; }

    public bool Handled { get; set; }
}
