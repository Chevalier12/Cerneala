namespace Cerneala.UI.Input;

public sealed class CommandBinding
{
    private readonly RoutedEventHandler<ExecutedRoutedEventArgs>? executed;
    private readonly Action<object, CanExecuteRoutedEventArgs>? canExecute;

    public CommandBinding(
        ICommand command,
        RoutedEventHandler<ExecutedRoutedEventArgs>? executed,
        Action<object, CanExecuteRoutedEventArgs>? canExecute = null)
    {
        Command = command ?? throw new ArgumentNullException(nameof(command));
        this.executed = executed;
        this.canExecute = canExecute;
    }

    public ICommand Command { get; }

    // As WPF's CommandBinding.Executed and CanExecute, the callbacks answer only the bubbling events. CommandRouter also
    // routes the preview events through the bindings, and a callback that answered both ran twice for one execution or
    // one query (#1034).
    public void OnExecuted(object sender, ExecutedRoutedEventArgs args)
    {
        ArgumentNullException.ThrowIfNull(args);
        if (!ReferenceEquals(Command, args.Command) || !ReferenceEquals(args.RoutedEvent, CommandEvents.ExecutedEvent))
        {
            return;
        }

        executed?.Invoke(sender, args);
    }

    public void OnCanExecute(object sender, CanExecuteRoutedEventArgs args)
    {
        ArgumentNullException.ThrowIfNull(args);
        if (!ReferenceEquals(Command, args.Command) || !ReferenceEquals(args.RoutedEvent, CommandEvents.CanExecuteEvent))
        {
            return;
        }

        canExecute?.Invoke(sender, args);
    }
}
