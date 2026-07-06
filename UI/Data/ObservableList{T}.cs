using System.Collections;

namespace Cerneala.UI.Data;

public sealed class ObservableList<T> : IObservableList<T>, IObservableList, IList<T>
{
    private readonly List<T> items = [];

    public ObservableList()
    {
    }

    public ObservableList(IEnumerable<T> items)
    {
        ArgumentNullException.ThrowIfNull(items);
        this.items.AddRange(items);
    }

    public event EventHandler<ObservableListChangedEventArgs<T>>? Changed;

    private event EventHandler<ObservableListChangedEventArgs>? UntypedChanged;

    event EventHandler<ObservableListChangedEventArgs>? IObservableList.Changed
    {
        add => UntypedChanged += value;
        remove => UntypedChanged -= value;
    }

    public int Count => items.Count;

    public bool IsReadOnly => false;

    public T this[int index]
    {
        get => items[index];
        set
        {
            T oldItem = items[index];
            if (EqualityComparer<T>.Default.Equals(oldItem, value))
            {
                return;
            }

            items[index] = value;
            Notify(new ObservableListChangedEventArgs<T>(
                ObservableListChangeKind.Replace,
                index,
                oldItem: oldItem,
                item: value,
                items: [value],
                oldItems: [oldItem]));
        }
    }

    object? IObservableList.this[int index] => items[index];

    public void Add(T item)
    {
        int index = items.Count;
        items.Add(item);
        Notify(new ObservableListChangedEventArgs<T>(
            ObservableListChangeKind.Add,
            index,
            item: item,
            items: [item]));
    }

    public void Insert(int index, T item)
    {
        items.Insert(index, item);
        Notify(new ObservableListChangedEventArgs<T>(
            ObservableListChangeKind.Add,
            index,
            item: item,
            items: [item]));
    }

    public bool Remove(T item)
    {
        int index = items.IndexOf(item);
        if (index < 0)
        {
            return false;
        }

        RemoveAt(index);
        return true;
    }

    public void RemoveAt(int index)
    {
        T item = items[index];
        items.RemoveAt(index);
        Notify(new ObservableListChangedEventArgs<T>(
            ObservableListChangeKind.Remove,
            index,
            item: item,
            oldItem: item,
            oldItems: [item]));
    }

    public void Move(int oldIndex, int newIndex)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(oldIndex);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(oldIndex, items.Count);
        ArgumentOutOfRangeException.ThrowIfNegative(newIndex);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(newIndex, items.Count);

        if (oldIndex == newIndex)
        {
            return;
        }

        T item = items[oldIndex];
        items.RemoveAt(oldIndex);
        items.Insert(newIndex, item);
        Notify(new ObservableListChangedEventArgs<T>(
            ObservableListChangeKind.Move,
            newIndex,
            oldIndex,
            item,
            item,
            [item],
            [item]));
    }

    public void Clear()
    {
        if (items.Count == 0)
        {
            return;
        }

        List<T> oldItems = [.. items];
        items.Clear();
        Notify(new ObservableListChangedEventArgs<T>(
            ObservableListChangeKind.Clear,
            oldItems: oldItems));
    }

    public void ReplaceWith(IEnumerable<T> newItems)
    {
        ArgumentNullException.ThrowIfNull(newItems);
        List<T> replacementItems = [.. newItems];
        List<T> oldItems = [.. items];
        items.Clear();
        items.AddRange(replacementItems);
        Notify(new ObservableListChangedEventArgs<T>(
            ObservableListChangeKind.Reset,
            items: [.. items],
            oldItems: oldItems));
    }

    public bool Contains(T item)
    {
        return items.Contains(item);
    }

    public void CopyTo(T[] array, int arrayIndex)
    {
        items.CopyTo(array, arrayIndex);
    }

    public int IndexOf(T item)
    {
        return items.IndexOf(item);
    }

    public IEnumerator<T> GetEnumerator()
    {
        return items.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    private void Notify(ObservableListChangedEventArgs<T> args)
    {
        Changed?.Invoke(this, args);
        UntypedChanged?.Invoke(this, new ObservableListChangedEventArgs(
            args.Kind,
            args.Index,
            args.OldIndex,
            args.Item,
            args.OldItem,
            [.. args.Items.Cast<object?>()],
            [.. args.OldItems.Cast<object?>()]));
    }
}
