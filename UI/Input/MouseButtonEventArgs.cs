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
