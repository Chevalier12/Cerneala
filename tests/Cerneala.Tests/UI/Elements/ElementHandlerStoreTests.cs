using Cerneala.UI.Elements;
using Cerneala.UI.Input;

namespace Cerneala.Tests.UI.Elements;

public sealed class ElementHandlerStoreTests
{
    [Fact]
    public void AddHandlerStoresHandlersByRoutedEvent()
    {
        RoutedEvent routedEvent = RoutedEventRegistry.Register(
            "ElementHandlerStoreEvent",
            typeof(ElementHandlerStoreTests),
            RoutingStrategy.Bubble,
            typeof(RoutedEventArgs));
        ElementHandlerStore store = new UIElement().Handlers;
        RoutedEventHandler handler = (_, _) => { };

        store.AddHandler(routedEvent, handler);

        Assert.Equal([handler], store.GetHandlers(routedEvent));
    }

    [Fact]
    public void EnumerateHandlersReturnsRegisteredHandlers()
    {
        RoutedEvent routedEvent = RoutedEventRegistry.Register(
            "ElementHandlerStoreEnumerateEvent",
            typeof(ElementHandlerStoreTests),
            RoutingStrategy.Bubble,
            typeof(RoutedEventArgs));
        ElementHandlerStore store = new UIElement().Handlers;
        RoutedEventHandler handler = (_, _) => { };
        store.AddHandler(routedEvent, handler);

        var registered = Assert.Single(store.EnumerateHandlers());

        Assert.Same(routedEvent, registered.RoutedEvent);
        Assert.Same(handler, registered.Handler);
    }
}
