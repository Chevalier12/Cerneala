namespace Cerneala.UI.Controls.Selection;

public sealed class SelectionChangedEventArgs : EventArgs
{
    public SelectionChangedEventArgs(SelectionChangeResult change)
    {
        Change = change;
    }

    public SelectionChangeResult Change { get; }
}
