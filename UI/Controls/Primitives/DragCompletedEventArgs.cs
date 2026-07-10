namespace Cerneala.UI.Controls.Primitives;

using Cerneala.UI.Input;

public sealed class DragCompletedEventArgs : RoutedEventArgs
{
    public DragCompletedEventArgs(float horizontalChange, float verticalChange, bool canceled)
    {
        HorizontalChange = horizontalChange;
        VerticalChange = verticalChange;
        Canceled = canceled;
    }

    public DragCompletedEventArgs(RoutedEvent routedEvent, object source, float horizontalChange, float verticalChange, bool canceled)
        : base(routedEvent, source)
    {
        HorizontalChange = horizontalChange;
        VerticalChange = verticalChange;
        Canceled = canceled;
    }

    public float HorizontalChange { get; }

    public float VerticalChange { get; }

    public bool Canceled { get; }
}
