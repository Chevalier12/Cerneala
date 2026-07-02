namespace Cerneala.UI.Input;

public sealed class CanExecuteRoutedEventArgs : RoutedEventArgs
{
    public CanExecuteRoutedEventArgs(RoutedEvent routedEvent, object originalSource, ICommand command, object? parameter)
        : base(routedEvent, originalSource)
    {
        Command = command ?? throw new ArgumentNullException(nameof(command));
        Parameter = parameter;
    }

    public ICommand Command { get; }

    public object? Parameter { get; }

    public bool CanExecute { get; set; }
}
