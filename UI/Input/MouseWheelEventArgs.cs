namespace Cerneala.UI.Input;

public sealed class MouseWheelEventArgs : MouseEventArgs
{
    public MouseWheelEventArgs(RoutedEvent routedEvent, object originalSource, int x, int y, int delta)
        : base(routedEvent, originalSource, x, y)
    {
        Delta = delta;
    }

    public int Delta { get; }
}
