namespace Cerneala.UI.Input;

public sealed class CommandBinding
{
    private readonly RoutedEventHandler? executed;
    private readonly Action<UiElementId, CanExecuteRoutedEventArgs>? canExecute;

    public CommandBinding(
        ICommand command,
        RoutedEventHandler? executed,
        Action<UiElementId, CanExecuteRoutedEventArgs>? canExecute = null)
    {
        Command = command ?? throw new ArgumentNullException(nameof(command));
        this.executed = executed;
        this.canExecute = canExecute;
    }

    public ICommand Command { get; }

    public void OnExecuted(UiElementId sender, ExecutedRoutedEventArgs args)
    {
        ArgumentNullException.ThrowIfNull(args);
        executed?.Invoke(sender, args);
    }

    public void OnCanExecute(UiElementId sender, CanExecuteRoutedEventArgs args)
    {
        ArgumentNullException.ThrowIfNull(args);
        canExecute?.Invoke(sender, args);
    }
}
