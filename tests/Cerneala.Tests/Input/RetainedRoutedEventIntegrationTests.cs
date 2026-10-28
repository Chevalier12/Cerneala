using Cerneala.UI.Elements;
using Cerneala.UI.Input;
using Cerneala.UI.Layout;

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

    // As WPF's UIElement.RaiseEvent: the route of an event that an attached element raises follows its parents whatever
    // their IsEnabled and Visibility. Being outside the input route only keeps pointer and keyboard input away.
    [Theory]
    [InlineData("disabled child")]
    [InlineData("hidden child")]
    [InlineData("collapsed child")]
    [InlineData("disabled parent")]
    [InlineData("hidden parent")]
    [InlineData("collapsed parent")]
    public void BubbleEventRaisedOutsideTheInputRouteReachesEveryAncestor(string state)
    {
        UIRoot root = new();
        UIElement parent = new();
        UIElement child = new();
        root.VisualChildren.Add(parent);
        parent.VisualChildren.Add(child);
        List<string> calls = [];
        child.Handlers.AddHandler(InputEvents.MouseDownEvent, (_, _) => calls.Add("child"));
        parent.Handlers.AddHandler(InputEvents.MouseDownEvent, (_, _) => calls.Add("parent"));
        root.Handlers.AddHandler(InputEvents.MouseDownEvent, (_, _) => calls.Add("root"));
        Apply(state, state.EndsWith("child", StringComparison.Ordinal) ? child : parent);
        root.ProcessFrame();

        child.RaiseEvent(new MouseButtonEventArgs(InputEvents.MouseDownEvent, child, InputMouseButton.Left, 0, 0, 1));

        Assert.Equal(["child", "parent", "root"], calls);
    }

    // A Tunnel event that a disabled element raises starts at the root, as in WPF.
    [Fact]
    public void TunnelEventRaisedByADisabledElementStartsAtTheRoot()
    {
        UIRoot root = new();
        UIElement parent = new();
        UIElement child = new() { IsEnabled = false };
        root.VisualChildren.Add(parent);
        parent.VisualChildren.Add(child);
        List<string> calls = [];
        root.Handlers.AddHandler(InputEvents.PreviewMouseDownEvent, (_, _) => calls.Add("root"));
        parent.Handlers.AddHandler(InputEvents.PreviewMouseDownEvent, (_, _) => calls.Add("parent"));
        child.Handlers.AddHandler(InputEvents.PreviewMouseDownEvent, (_, _) => calls.Add("child"));
        root.ProcessFrame();

        child.RaiseEvent(new MouseButtonEventArgs(InputEvents.PreviewMouseDownEvent, child, InputMouseButton.Left, 0, 0, 1));

        Assert.Equal(["root", "parent", "child"], calls);
    }

    // The route of a raised event changes with the tree, not only with the input route: a handler added to a hidden
    // element after an earlier raise runs on the next raise.
    [Fact]
    public void RaisedEventRouteFollowsHandlersAddedToAHiddenAncestor()
    {
        UIRoot root = new();
        UIElement parent = new() { Visibility = Visibility.Hidden };
        UIElement child = new();
        root.VisualChildren.Add(parent);
        parent.VisualChildren.Add(child);
        List<string> calls = [];
        child.RaiseEvent(new MouseButtonEventArgs(InputEvents.MouseDownEvent, child, InputMouseButton.Left, 0, 0, 1));

        parent.Handlers.AddHandler(InputEvents.MouseDownEvent, (_, _) => calls.Add("parent"));
        child.RaiseEvent(new MouseButtonEventArgs(InputEvents.MouseDownEvent, child, InputMouseButton.Left, 0, 0, 1));

        Assert.Equal(["parent"], calls);
    }

    // Raising an event does not change where input goes: a disabled, hidden or collapsed element stays outside the
    // input route.
    [Theory]
    [InlineData("disabled child")]
    [InlineData("hidden child")]
    [InlineData("collapsed child")]
    public void RaisingAnEventKeepsTheElementOutsideTheInputRoute(string state)
    {
        UIRoot root = new();
        UIElement child = new();
        root.VisualChildren.Add(child);
        Apply(state, child);

        child.RaiseEvent(new MouseButtonEventArgs(InputEvents.MouseDownEvent, child, InputMouseButton.Left, 0, 0, 1));

        Assert.False(root.InputCache.EnsureCurrent(root).Contains(child));
    }

    private static void Apply(string state, UIElement element)
    {
        if (state.StartsWith("disabled", StringComparison.Ordinal))
        {
            element.IsEnabled = false;
        }
        else
        {
            element.Visibility = state.StartsWith("hidden", StringComparison.Ordinal) ? Visibility.Hidden : Visibility.Collapsed;
        }
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
