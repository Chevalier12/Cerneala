using Cerneala.UI.Elements;

namespace Cerneala.UI.Input;

public delegate void RoutedEventHandler(object sender, RoutedEventArgs args);

public delegate void RoutedEventHandler<in TArgs>(object sender, TArgs args)
    where TArgs : RoutedEventArgs;

internal readonly record struct InputRoutedEventHandlerRegistration(RoutedEventHandler Handler, bool HandledEventsToo);

public sealed class UiInputTree
{
    private readonly Dictionary<UIElement, UiInputElement> elements = new(ReferenceEqualityComparer.Instance);
    private readonly Dictionary<UIElement, Dictionary<RoutedEvent, List<InputRoutedEventHandlerRegistration>>> handlers =
        new(ReferenceEqualityComparer.Instance);

    public void Add(UIElement element, UIElement? parent, bool isEnabled = true)
    {
        ArgumentNullException.ThrowIfNull(element);
        if (parent is not null && !elements.ContainsKey(parent))
        {
            throw new ArgumentException("Parent element must be added before the child element.", nameof(parent));
        }

        if (!elements.TryAdd(element, new UiInputElement(element, parent, isEnabled)))
        {
            throw new InvalidOperationException($"Element '{element.GetType().Name}' is already registered in this input tree.");
        }
    }

    public void AddHandler(UIElement element, RoutedEvent routedEvent, RoutedEventHandler handler, bool handledEventsToo = false)
    {
        ArgumentNullException.ThrowIfNull(element);
        ArgumentNullException.ThrowIfNull(routedEvent);
        ArgumentNullException.ThrowIfNull(handler);

        if (!elements.ContainsKey(element))
        {
            throw new ArgumentException("Element must be added before handlers can be registered.", nameof(element));
        }

        if (!handlers.TryGetValue(element, out Dictionary<RoutedEvent, List<InputRoutedEventHandlerRegistration>>? elementHandlers))
        {
            elementHandlers = [];
            handlers.Add(element, elementHandlers);
        }

        if (!elementHandlers.TryGetValue(routedEvent, out List<InputRoutedEventHandlerRegistration>? registeredHandlers))
        {
            registeredHandlers = [];
            elementHandlers.Add(routedEvent, registeredHandlers);
        }

        registeredHandlers.Add(new InputRoutedEventHandlerRegistration(handler, handledEventsToo));
    }

    public IReadOnlyList<UIElement> GetRouteToRoot(UIElement target)
    {
        ArgumentNullException.ThrowIfNull(target);
        if (!elements.ContainsKey(target))
        {
            throw new ArgumentException("Target element is not registered in this input tree.", nameof(target));
        }

        List<UIElement> route = [];
        UIElement? current = target;

        while (current is not null)
        {
            UiInputElement entry = elements[current];
            route.Add(entry.Element);
            current = entry.Parent;
        }

        return route;
    }

    internal IReadOnlyList<RoutedEventHandler> GetHandlers(UIElement element, RoutedEvent routedEvent)
    {
        return GetHandlerRegistrations(element, routedEvent).Select(item => item.Handler).ToArray();
    }

    internal IReadOnlyList<InputRoutedEventHandlerRegistration> GetHandlerRegistrations(UIElement element, RoutedEvent routedEvent)
    {
        if (!elements[element].IsEnabled)
        {
            return [];
        }

        return handlers.TryGetValue(element, out Dictionary<RoutedEvent, List<InputRoutedEventHandlerRegistration>>? elementHandlers) &&
            elementHandlers.TryGetValue(routedEvent, out List<InputRoutedEventHandlerRegistration>? registeredHandlers)
            ? registeredHandlers.ToArray()
            : [];
    }
}
