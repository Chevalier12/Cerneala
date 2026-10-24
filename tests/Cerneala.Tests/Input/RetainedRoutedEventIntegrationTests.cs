using Cerneala.UI.Elements;
using Cerneala.UI.Input;

namespace Cerneala.Tests.Input;

public sealed class RetainedRoutedEventIntegrationTests
{
    [Fact]
    public void BubbleAndPreviewRoutesFollowRetainedVisualParents()
    {
        UIRoot root = new();
        UIElement parent = new();
        UIElement child = new();
        root.VisualChildren.Add(parent);
        parent.VisualChildren.Add(child);
        List<string> calls = [];
        root.Handlers.AddHandler(InputEvents.PreviewMouseDownEvent, (_, _) => calls.Add("preview-root"));
        parent.Handlers.AddHandler(InputEvents.PreviewMouseDownEvent, (_, _) => calls.Add("preview-parent"));
        child.Handlers.AddHandler(InputEvents.PreviewMouseDownEvent, (_, _) => calls.Add("preview-child"));
        child.Handlers.AddHandler(InputEvents.MouseDownEvent, (_, _) => calls.Add("bubble-child"));
        parent.Handlers.AddHandler(InputEvents.MouseDownEvent, (_, _) => calls.Add("bubble-parent"));
        root.Handlers.AddHandler(InputEvents.MouseDownEvent, (_, _) => calls.Add("bubble-root"));
        ElementInputRouteMap map = new ElementInputRouteBuilder().Build(root);
        Assert.True(map.Contains(child));

        RoutedEventRouter.Raise(map.InputTree, child, new MouseButtonEventArgs(InputEvents.PreviewMouseDownEvent, child, InputMouseButton.Left, 0, 0, 1));
        RoutedEventRouter.Raise(map.InputTree, child, new MouseButtonEventArgs(InputEvents.MouseDownEvent, child, InputMouseButton.Left, 0, 0, 1));

        Assert.Equal(["preview-root", "preview-parent", "preview-child", "bubble-child", "bubble-parent", "bubble-root"], calls);
    }

    [Fact]
    public void BubblingHandlersReceiveTheirOwnElementAsSenderAndSource()
    {
        UIRoot root = new();
        UIElement parent = new();
        UIElement child = new();
        root.VisualChildren.Add(parent);
        parent.VisualChildren.Add(child);
        List<(string Handler, object Sender, object Source, object OriginalSource)> calls = [];
        child.Handlers.AddHandler(InputEvents.MouseDownEvent, (sender, args) => calls.Add(("child", sender, args.Source, args.OriginalSource)));
        parent.Handlers.AddHandler(InputEvents.MouseDownEvent, (sender, args) => calls.Add(("parent", sender, args.Source, args.OriginalSource)));

        child.RaiseEvent(new MouseButtonEventArgs(InputEvents.MouseDownEvent, child, InputMouseButton.Left, 0, 0, 1));

        Assert.Equal(2, calls.Count);
        Assert.Equal("child", calls[0].Handler);
        Assert.Same(child, calls[0].Sender);
        Assert.Same(child, calls[0].Source);
        Assert.Same(child, calls[0].OriginalSource);
        Assert.Equal("parent", calls[1].Handler);
        Assert.Same(parent, calls[1].Sender);
        Assert.Same(parent, calls[1].Source);
        Assert.Same(child, calls[1].OriginalSource);
    }

    [Fact]
    public void RouterPassesEachRouteElementAsSender()
    {
        UIRoot root = new();
        UIElement parent = new();
        UIElement child = new();
        root.VisualChildren.Add(parent);
        parent.VisualChildren.Add(child);
        List<object> senders = [];
        root.Handlers.AddHandler(InputEvents.PreviewMouseDownEvent, (sender, _) => senders.Add(sender));
        parent.Handlers.AddHandler(InputEvents.PreviewMouseDownEvent, (sender, _) => senders.Add(sender));
        child.Handlers.AddHandler(InputEvents.PreviewMouseDownEvent, (sender, _) => senders.Add(sender));
        ElementInputRouteMap map = new ElementInputRouteBuilder().Build(root);

        RoutedEventRouter.Raise(map.InputTree, child, new MouseButtonEventArgs(InputEvents.PreviewMouseDownEvent, child, InputMouseButton.Left, 0, 0, 1));

        Assert.Equal(3, senders.Count);
        Assert.Same(root, senders[0]);
        Assert.Same(parent, senders[1]);
        Assert.Same(child, senders[2]);
    }

    [Fact]
    public void DetachedElementRaiseEventPassesTheElementAsSender()
    {
        UIElement element = new();
        object? sender = null;
        element.Handlers.AddHandler(InputEvents.MouseDownEvent, (handlerSender, _) => sender = handlerSender);

        element.RaiseEvent(new MouseButtonEventArgs(InputEvents.MouseDownEvent, element, InputMouseButton.Left, 0, 0, 1));

        Assert.Same(element, sender);
    }

    // A Direct event runs only the raising element's handlers. As on a route, the handler's sender and args.Source are
    // that element, and OriginalSource stays the object the args were created with.
    [Fact]
    public void DirectEventHandlerReceivesTheRaisingElementAsSenderAndSource()
    {
        RoutedEvent directEvent = RoutedEventRegistry.Register(
            "DirectSenderAndSource",
            typeof(RetainedRoutedEventIntegrationTests),
            RoutingStrategy.Direct,
            typeof(RoutedEventArgs));
        UIRoot root = new();
        UIElement element = new();
        UIElement other = new();
        root.VisualChildren.Add(element);
        root.VisualChildren.Add(other);
        object? sender = null;
        object? source = null;
        object? originalSource = null;
        element.Handlers.AddHandler(directEvent, (handlerSender, args) =>
        {
            sender = handlerSender;
            source = args.Source;
            originalSource = args.OriginalSource;
        });

        element.RaiseEvent(new RoutedEventArgs(directEvent, other));

        Assert.Same(element, sender);
        Assert.Same(element, source);
        Assert.Same(other, originalSource);
    }

    [Fact]
    public void ElementRoutedEventStoreUsesElementHandlerStore()
    {
        UIElement element = new();
        ElementRoutedEventStore store = new(element);
        bool called = false;

        store.AddHandler(InputEvents.MouseDownEvent, (_, _) => called = true);

        Assert.Single(store.GetHandlers(InputEvents.MouseDownEvent));
        store.GetHandlers(InputEvents.MouseDownEvent)[0](element, new RoutedEventArgs(InputEvents.MouseDownEvent, element));
        Assert.True(called);
    }
}
