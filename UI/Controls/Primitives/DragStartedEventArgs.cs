namespace Cerneala.UI.Controls.Primitives;

public sealed class DragStartedEventArgs : EventArgs
{
    public DragStartedEventArgs(float x, float y)
    {
        X = x;
        Y = y;
    }

    public float X { get; }

    public float Y { get; }
}
