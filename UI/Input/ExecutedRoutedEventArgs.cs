namespace Cerneala.UI.Input;

public sealed class ExecutedRoutedEventArgs : RoutedEventArgs
{
    public ExecutedRoutedEventArgs(RoutedEvent routedEvent, object originalSource, ICommand command, object? parameter)
        : base(routedEvent, originalSource)
    {
        Command = command ?? throw new ArgumentNullException(nameof(command));
        Parameter = parameter;
    }

    public ICommand Command { get; }

    public object? Parameter { get; }
}
