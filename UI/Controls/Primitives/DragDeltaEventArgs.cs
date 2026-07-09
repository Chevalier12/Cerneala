namespace Cerneala.UI.Controls.Primitives;

public sealed class DragDeltaEventArgs : EventArgs
{
    public DragDeltaEventArgs(float horizontalChange, float verticalChange, float totalHorizontalChange, float totalVerticalChange)
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
