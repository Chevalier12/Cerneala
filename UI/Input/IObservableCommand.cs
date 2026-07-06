namespace Cerneala.UI.Input;

public interface IObservableCommand : ICommand
{
    event EventHandler? CanExecuteChanged;
}
