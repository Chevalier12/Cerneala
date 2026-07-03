namespace Cerneala.UI.Input;

public sealed class RoutedCommand : ICommand
{
    public RoutedCommand(string name, Type ownerType)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Command name cannot be empty.", nameof(name));
        }

        Name = name;
        OwnerType = ownerType ?? throw new ArgumentNullException(nameof(ownerType));
    }

    public string Name { get; }

    public Type OwnerType { get; }

    public bool CanExecute(object? parameter)
    {
        throw new InvalidOperationException("RoutedCommand requires CommandRouter.CanExecute with a retained command context.");
    }

    public void Execute(object? parameter)
    {
        throw new InvalidOperationException("RoutedCommand requires CommandRouter.Execute with a retained command context.");
    }
}
