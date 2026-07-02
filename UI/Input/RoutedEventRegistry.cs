namespace Cerneala.UI.Input;

public static class RoutedEventRegistry
{
    public static RoutedEvent Register(string name, Type ownerType, RoutingStrategy routingStrategy, Type argsType)
    {
        if (!typeof(RoutedEventArgs).IsAssignableFrom(argsType))
        {
            throw new ArgumentException("Routed event args type must derive from RoutedEventArgs.", nameof(argsType));
        }

        return new RoutedEvent(name, ownerType, routingStrategy, argsType);
    }
}
