namespace Cerneala.UI.Input;

public class RoutedEventArgs
{
    public RoutedEventArgs()
    {
    }

    public RoutedEventArgs(RoutedEvent routedEvent, object originalSource)
    {
        RoutedEvent = routedEvent ?? throw new ArgumentNullException(nameof(routedEvent));
        OriginalSource = originalSource ?? throw new ArgumentNullException(nameof(originalSource));
        Source = originalSource;
    }

    public RoutedEvent RoutedEvent { get; set; } = null!;

    public object OriginalSource { get; internal set; } = null!;

    public object Source { get; set; } = null!;

    public bool Handled { get; set; }
}
