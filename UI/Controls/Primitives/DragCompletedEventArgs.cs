namespace Cerneala.UI.Controls.Primitives;

public sealed class DragCompletedEventArgs : EventArgs
{
    public DragCompletedEventArgs(float horizontalChange, float verticalChange, bool canceled)
    {
        HorizontalChange = horizontalChange;
        VerticalChange = verticalChange;
        Canceled = canceled;
    }

    public float HorizontalChange { get; }

    public float VerticalChange { get; }

    public bool Canceled { get; }
}
