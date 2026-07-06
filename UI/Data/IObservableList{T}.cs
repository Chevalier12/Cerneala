using System.Collections;

namespace Cerneala.UI.Data;

public interface IObservableList : IEnumerable
{
    event EventHandler<ObservableListChangedEventArgs>? Changed;

    int Count { get; }

    object? this[int index] { get; }
}

public interface IObservableList<T> : IReadOnlyList<T>
{
    event EventHandler<ObservableListChangedEventArgs<T>>? Changed;
}

public enum ObservableListChangeKind
{
    Add,
    Remove,
    Replace,
    Move,
    Reset,
    Clear
}

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
