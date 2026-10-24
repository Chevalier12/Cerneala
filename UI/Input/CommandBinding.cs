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

    public void OnExecuted(object sender, ExecutedRoutedEventArgs args)
    {
        ArgumentNullException.ThrowIfNull(args);
        if (!ReferenceEquals(Command, args.Command))
        {
            return;
        }

        executed?.Invoke(sender, args);
    }

    public void OnCanExecute(object sender, CanExecuteRoutedEventArgs args)
    {
        ArgumentNullException.ThrowIfNull(args);
        if (!ReferenceEquals(Command, args.Command))
        {
            return;
        }

        canExecute?.Invoke(sender, args);
    }
}
