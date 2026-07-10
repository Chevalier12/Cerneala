namespace Cerneala.UI.Controls.Primitives;

using Cerneala.UI.Input;

public sealed class DragStartedEventArgs : RoutedEventArgs
{
    public DragStartedEventArgs(float x, float y)
    {
        X = x;
        Y = y;
    }

    public DragStartedEventArgs(RoutedEvent routedEvent, object source, float x, float y) : base(routedEvent, source)
    {
        X = x;
        Y = y;
    }

    public float X { get; }

    public float Y { get; }
}
