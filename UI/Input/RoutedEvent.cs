namespace Cerneala.UI.Input;

public sealed class RoutedEvent
{
    internal RoutedEvent(string name, Type ownerType, RoutingStrategy routingStrategy, Type argsType)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Routed event name cannot be empty.", nameof(name));
        }

        Name = name;
        OwnerType = ownerType ?? throw new ArgumentNullException(nameof(ownerType));
        RoutingStrategy = routingStrategy;
        ArgsType = argsType ?? throw new ArgumentNullException(nameof(argsType));
    }

    public string Name { get; }

    public Type OwnerType { get; }

    public RoutingStrategy RoutingStrategy { get; }

    public Type ArgsType { get; }
}
