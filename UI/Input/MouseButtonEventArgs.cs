namespace Cerneala.UI.Input;

public sealed class MouseButtonEventArgs : MouseEventArgs
{
    public MouseButtonEventArgs(
        RoutedEvent routedEvent,
        object originalSource,
        InputMouseButton changedButton,
        int x,
        int y,
        int clickCount)
        : base(routedEvent, originalSource, x, y)
    {
        ChangedButton = changedButton;
        ClickCount = clickCount;
    }

    public InputMouseButton ChangedButton { get; }

    public int ClickCount { get; }

    // The route map the input bridge resolved and routed this event with, taken before any handler ran. Handlers that
    // decide click ownership use it so that a tree change by an earlier handler does not change the decision (#501).
    internal ElementInputRouteMap? RouteMap { get; init; }

    internal MouseButtonEventArgs(
        RoutedEvent routedEvent,
        object originalSource,
        InputMouseButton changedButton,
        float rootX,
        float rootY,
        int clickCount)
        : base(routedEvent, originalSource, rootX, rootY)
    {
        ChangedButton = changedButton;
        ClickCount = clickCount;
    }
}
