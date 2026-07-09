namespace Cerneala.UI.Data;

public sealed class ObservableListChangedEventArgs<T> : EventArgs
{
    public ObservableListChangedEventArgs(
        ObservableListChangeKind kind,
        int index = -1,
        int oldIndex = -1,
        T item = default!,
        T oldItem = default!,
        IReadOnlyList<T>? items = null,
        IReadOnlyList<T>? oldItems = null)
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

    public T Item { get; }

    public T OldItem { get; }

    public IReadOnlyList<T> Items { get; }

    public IReadOnlyList<T> OldItems { get; }
}
