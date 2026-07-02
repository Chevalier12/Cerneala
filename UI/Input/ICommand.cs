namespace Cerneala.UI.Input;

public interface ICommand
{
    bool CanExecute(object? parameter);

    void Execute(object? parameter);
}
