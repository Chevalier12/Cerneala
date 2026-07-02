namespace Cerneala.UI.Input;

public class MouseEventArgs : RoutedEventArgs
{
    public MouseEventArgs(RoutedEvent routedEvent, object originalSource, int x, int y)
        : base(routedEvent, originalSource)
    {
        X = x;
        Y = y;
    }

    public int X { get; }

    public int Y { get; }
}
