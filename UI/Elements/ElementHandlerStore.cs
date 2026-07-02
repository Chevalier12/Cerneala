using Cerneala.UI.Input;

namespace Cerneala.UI.Elements;

public sealed class ElementHandlerStore
{
    private readonly Dictionary<RoutedEvent, List<RoutedEventHandler>> handlers = [];

    public void AddHandler(RoutedEvent routedEvent, RoutedEventHandler handler)
    {
        ArgumentNullException.ThrowIfNull(routedEvent);
        ArgumentNullException.ThrowIfNull(handler);

        if (!handlers.TryGetValue(routedEvent, out List<RoutedEventHandler>? registeredHandlers))
        {
            registeredHandlers = [];
            handlers.Add(routedEvent, registeredHandlers);
        }

        registeredHandlers.Add(handler);
    }

    public IReadOnlyList<RoutedEventHandler> GetHandlers(RoutedEvent routedEvent)
    {
        ArgumentNullException.ThrowIfNull(routedEvent);

        return handlers.TryGetValue(routedEvent, out List<RoutedEventHandler>? registeredHandlers)
            ? registeredHandlers.ToArray()
            : [];
    }

    public IEnumerable<(RoutedEvent RoutedEvent, RoutedEventHandler Handler)> EnumerateHandlers()
    {
        foreach ((RoutedEvent routedEvent, List<RoutedEventHandler> registeredHandlers) in handlers)
        {
            foreach (RoutedEventHandler handler in registeredHandlers)
            {
                yield return (routedEvent, handler);
            }
        }
    }
}
