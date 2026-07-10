namespace Cerneala.UI.Controls.Selection;

using Cerneala.UI.Input;

public sealed class SelectionChangedEventArgs : RoutedEventArgs
{
    public SelectionChangedEventArgs(SelectionChangeResult change)
    {
        Change = change;
    }

    public SelectionChangedEventArgs(RoutedEvent routedEvent, object source, SelectionChangeResult change, IReadOnlyList<object?> removedItems, IReadOnlyList<object?> addedItems)
        : base(routedEvent, source)
    {
        Change = change;
        RemovedItems = removedItems;
        AddedItems = addedItems;
    }

    public SelectionChangeResult Change { get; }

    public IReadOnlyList<object?> RemovedItems { get; } = [];

    public IReadOnlyList<object?> AddedItems { get; } = [];
}
