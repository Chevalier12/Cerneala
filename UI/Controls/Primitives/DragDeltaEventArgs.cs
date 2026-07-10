namespace Cerneala.UI.Controls.Primitives;

using Cerneala.UI.Input;

public sealed class DragDeltaEventArgs : RoutedEventArgs
{
    public DragDeltaEventArgs(float horizontalChange, float verticalChange, float totalHorizontalChange, float totalVerticalChange)
    {
        HorizontalChange = horizontalChange;
        VerticalChange = verticalChange;
        TotalHorizontalChange = totalHorizontalChange;
        TotalVerticalChange = totalVerticalChange;
    }

    public DragDeltaEventArgs(RoutedEvent routedEvent, object source, float horizontalChange, float verticalChange, float totalHorizontalChange, float totalVerticalChange)
        : base(routedEvent, source)
    {
        HorizontalChange = horizontalChange;
        VerticalChange = verticalChange;
        TotalHorizontalChange = totalHorizontalChange;
        TotalVerticalChange = totalVerticalChange;
    }

    public float HorizontalChange { get; }

    public float VerticalChange { get; }

    public float TotalHorizontalChange { get; }

    public float TotalVerticalChange { get; }
}
