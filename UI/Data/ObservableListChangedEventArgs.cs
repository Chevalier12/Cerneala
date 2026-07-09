namespace Cerneala.UI.Data;

public sealed class ObservableListChangedEventArgs : EventArgs
{
    public ObservableListChangedEventArgs(
        ObservableListChangeKind kind,
        int index = -1,
        int oldIndex = -1,
        object? item = null,
        object? oldItem = null,
        IReadOnlyList<object?>? items = null,
        IReadOnlyList<object?>? oldItems = null)
    {
        Kind = kind;
        Index = index;
        OldIndex = oldIndex;
        Item = item;
        OldItem = oldItem;
        Items = items ?? [];
        OldItems = oldItems ?? [];
    }

    public ObservableListChangeKind Kind { get; }

    public int Index { get; }

    public int OldIndex { get; }

    public object? Item { get; }

    public object? OldItem { get; }

    public IReadOnlyList<object?> Items { get; }

    public IReadOnlyList<object?> OldItems { get; }
}
