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
}
