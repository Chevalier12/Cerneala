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
        return false;
    }

    public void Execute(object? parameter)
    {
        throw new InvalidOperationException("RoutedCommand cannot execute directly until command routing is available.");
    }
}
