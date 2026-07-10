using Cerneala.UI.Input;

namespace Cerneala.UI.Elements;

public sealed class ElementHandlerStore
{
    private readonly Dictionary<RoutedEvent, List<RoutedEventHandlerRegistration>> handlers = [];
    private readonly UIElement owner;

    internal ElementHandlerStore(UIElement owner)
    {
        this.owner = owner ?? throw new ArgumentNullException(nameof(owner));
    }

    public void AddHandler(RoutedEvent routedEvent, RoutedEventHandler handler, bool handledEventsToo = false)
    {
        ArgumentNullException.ThrowIfNull(routedEvent);
        ArgumentNullException.ThrowIfNull(handler);

        if (!handlers.TryGetValue(routedEvent, out List<RoutedEventHandlerRegistration>? registeredHandlers))
        {
            registeredHandlers = [];
            handlers.Add(routedEvent, registeredHandlers);
        }

        registeredHandlers.Add(new RoutedEventHandlerRegistration(handler, handledEventsToo));
        InvalidateRoute("Input handler added");
    }

    public bool RemoveHandler(RoutedEvent routedEvent, RoutedEventHandler handler)
    {
        ArgumentNullException.ThrowIfNull(routedEvent);
        ArgumentNullException.ThrowIfNull(handler);

        if (!handlers.TryGetValue(routedEvent, out List<RoutedEventHandlerRegistration>? registeredHandlers))
        {
            return false;
        }

        int index = registeredHandlers.FindIndex(item => item.Handler == handler);
        bool removed = index >= 0;
        if (!removed)
        {
            return false;
        }

        registeredHandlers.RemoveAt(index);

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

        return handlers.TryGetValue(routedEvent, out List<RoutedEventHandlerRegistration>? registeredHandlers)
            ? registeredHandlers.Select(item => item.Handler).ToArray()
            : [];
    }

    public IEnumerable<(RoutedEvent RoutedEvent, RoutedEventHandler Handler, bool HandledEventsToo)> EnumerateHandlers()
    {
        foreach ((RoutedEvent routedEvent, List<RoutedEventHandlerRegistration> registeredHandlers) in handlers)
        {
            foreach (RoutedEventHandlerRegistration registration in registeredHandlers)
            {
                yield return (routedEvent, registration.Handler, registration.HandledEventsToo);
            }
        }
    }

    internal IReadOnlyList<RoutedEventHandlerRegistration> GetRegistrations(RoutedEvent routedEvent)
    {
        return handlers.TryGetValue(routedEvent, out List<RoutedEventHandlerRegistration>? registeredHandlers)
            ? registeredHandlers.ToArray()
            : [];
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

internal readonly record struct RoutedEventHandlerRegistration(RoutedEventHandler Handler, bool HandledEventsToo);
