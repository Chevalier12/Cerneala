using Cerneala.UI.Input;

namespace Cerneala.UI.Elements;

public sealed class ElementHandlerStore
{
    private readonly Dictionary<RoutedEvent, List<RoutedEventHandler>> handlers = [];
    private readonly UIElement owner;

    internal ElementHandlerStore(UIElement owner)
    {
        this.owner = owner ?? throw new ArgumentNullException(nameof(owner));
    }

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
        InvalidateRoute("Input handler added");
    }

    public bool RemoveHandler(RoutedEvent routedEvent, RoutedEventHandler handler)
    {
        ArgumentNullException.ThrowIfNull(routedEvent);
        ArgumentNullException.ThrowIfNull(handler);

        if (!handlers.TryGetValue(routedEvent, out List<RoutedEventHandler>? registeredHandlers))
        {
            return false;
        }

        bool removed = registeredHandlers.Remove(handler);
        if (!removed)
        {
            return false;
        }

        if (registeredHandlers.Count == 0)
        {
            handlers.Remove(routedEvent);
        }

        InvalidateRoute("Input handler removed");
        return true;
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

    private void InvalidateRoute(string reason)
    {
        if (owner.Root is null)
        {
            return;
        }

        owner.Invalidate(Cerneala.UI.Invalidation.InvalidationFlags.HitTest, reason);
    }
}
