namespace Cerneala.UI.Input;

public sealed class RoutedEvent
{
    private readonly HashSet<Type> ownerTypes;

    internal RoutedEvent(string name, Type ownerType, RoutingStrategy routingStrategy, Type argsType)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Routed event name cannot be empty.", nameof(name));
        }

        Name = name;
        OwnerType = ownerType ?? throw new ArgumentNullException(nameof(ownerType));
        ownerTypes = [ownerType];
        RoutingStrategy = routingStrategy;
        ArgsType = argsType ?? throw new ArgumentNullException(nameof(argsType));
    }

    public string Name { get; }

    public Type OwnerType { get; }

    public RoutingStrategy RoutingStrategy { get; }

    public Type ArgsType { get; }

    public RoutedEvent AddOwner(Type ownerType)
    {
        ArgumentNullException.ThrowIfNull(ownerType);
        ownerTypes.Add(ownerType);
        return this;
    }

    public IReadOnlyCollection<Type> OwnerTypes => ownerTypes;

    public bool IsOwnedBy(Type ownerType)
    {
        ArgumentNullException.ThrowIfNull(ownerType);
        return ownerTypes.Contains(ownerType);
    }
}
