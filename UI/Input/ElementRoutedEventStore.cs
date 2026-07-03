using Cerneala.UI.Elements;

namespace Cerneala.UI.Input;

public sealed class ElementRoutedEventStore
{
    private readonly UIElement element;

    public ElementRoutedEventStore(UIElement element)
    {
        this.element = element ?? throw new ArgumentNullException(nameof(element));
    }

    public void AddHandler(RoutedEvent routedEvent, RoutedEventHandler handler)
    {
        element.Handlers.AddHandler(routedEvent, handler);
    }

    public IReadOnlyList<RoutedEventHandler> GetHandlers(RoutedEvent routedEvent)
    {
        return element.Handlers.GetHandlers(routedEvent);
    }
}
