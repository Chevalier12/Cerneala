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

    // On a release raised by the input bridge, the OriginalSource of the press that the release completes; null when
    // no press is known. A click owner below the command source that the click tracker resolves, such as the field of
    // a ComboBox inside a Button, uses it to check that the press and the release hit the same element.
    internal object? PressOriginalSource { get; init; }

    // The route map the input bridge resolved and routed this event with, taken before any handler ran. Handlers that
    // decide click ownership use it so that a tree change by an earlier handler does not change the decision (#501).
    internal ElementInputRouteMap? RouteMap { get; init; }

    // The modifier keys that the keyboard of the input frame holds down, as WPF's Keyboard.Modifiers when a mouse
    // button handler runs, so a ListBox can select by Ctrl+click and Shift+click. False on events raised elsewhere.
    internal bool IsControlDown { get; init; }

    internal bool IsShiftDown { get; init; }

    internal bool IsAltDown { get; init; }

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
