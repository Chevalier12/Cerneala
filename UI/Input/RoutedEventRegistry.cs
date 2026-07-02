namespace Cerneala.UI.Input;

public static class RoutedEventRegistry
{
    public static RoutedEvent Register(string name, Type ownerType, RoutingStrategy routingStrategy, Type argsType)
    {
        ArgumentNullException.ThrowIfNull(argsType);

        if (!Enum.IsDefined(routingStrategy))
        {
            throw new ArgumentOutOfRangeException(nameof(routingStrategy), routingStrategy, "Unsupported routing strategy.");
        }

        if (!typeof(RoutedEventArgs).IsAssignableFrom(argsType))
        {
            throw new ArgumentException("Routed event args type must derive from RoutedEventArgs.", nameof(argsType));
        }

        return new RoutedEvent(name, ownerType, routingStrategy, argsType);
    }
}
