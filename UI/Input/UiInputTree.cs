namespace Cerneala.UI.Input;

public delegate void RoutedEventHandler(UiElementId sender, RoutedEventArgs args);

public sealed class UiInputTree
{
    private readonly Dictionary<UiElementId, UiInputElement> elements = [];
    private readonly Dictionary<(UiElementId ElementId, RoutedEvent RoutedEvent), List<RoutedEventHandler>> handlers = [];

    public void Add(UiElementId id, UiElementId? parentId, bool isEnabled = true)
    {
        if (parentId.HasValue && !elements.ContainsKey(parentId.Value))
        {
            throw new ArgumentException("Parent element must be added before the child element.", nameof(parentId));
        }

        if (!elements.TryAdd(id, new UiInputElement(id, parentId, isEnabled)))
        {
            throw new InvalidOperationException($"Element '{id}' is already registered in this input tree.");
        }
    }

    public void AddHandler(UiElementId id, RoutedEvent routedEvent, RoutedEventHandler handler)
    {
        ArgumentNullException.ThrowIfNull(routedEvent);
        ArgumentNullException.ThrowIfNull(handler);

        if (!elements.ContainsKey(id))
        {
            throw new ArgumentException("Element must be added before handlers can be registered.", nameof(id));
        }

        (UiElementId, RoutedEvent) key = (id, routedEvent);
        if (!handlers.TryGetValue(key, out List<RoutedEventHandler>? registeredHandlers))
        {
            registeredHandlers = [];
            handlers.Add(key, registeredHandlers);
        }

        registeredHandlers.Add(handler);
    }

    public IReadOnlyList<UiElementId> GetRouteToRoot(UiElementId targetId)
    {
        if (!elements.ContainsKey(targetId))
        {
            throw new ArgumentException("Target element is not registered in this input tree.", nameof(targetId));
        }

        List<UiElementId> route = [];
        UiElementId? currentId = targetId;

        while (currentId.HasValue)
        {
            UiInputElement current = elements[currentId.Value];
            route.Add(current.Id);
            currentId = current.ParentId;
        }

        return route;
    }

    internal IReadOnlyList<RoutedEventHandler> GetHandlers(UiElementId id, RoutedEvent routedEvent)
    {
        if (!elements[id].IsEnabled)
        {
            return [];
        }

        return handlers.TryGetValue((id, routedEvent), out List<RoutedEventHandler>? registeredHandlers)
            ? registeredHandlers
            : [];
    }
}
