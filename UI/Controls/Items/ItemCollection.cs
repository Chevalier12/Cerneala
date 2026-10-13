using System.Collections;
using Cerneala.UI.Invalidation;

namespace Cerneala.UI.Controls.Items;

public sealed class ItemCollection : IList<object?>
{
    private readonly List<object?> items = [];

    public event EventHandler? Changed;

    internal bool IsResetNotification { get; private set; }

    internal int ChangeIndex { get; private set; } = -1;

    internal int ChangeRemovedCount { get; private set; }

    internal int ChangeAddedCount { get; private set; }

    public int Count => items.Count;

    public bool IsReadOnly => false;

    public object? this[int index]
    {
        get => items[index];
        set
        {
            if (Equals(items[index], value))
            {
                items[index] = value;
                return;
            }

            items[index] = value;
            OnChanged(index, removedCount: 1, addedCount: 1);
        }
    }

    public void Add(object? item)
    {
        items.Add(item);
        OnChanged(items.Count - 1, removedCount: 0, addedCount: 1);
    }

    public void Clear()
    {
        if (items.Count == 0)
        {
            return;
        }

        items.Clear();
        OnReset();
    }

    public bool Contains(object? item)
    {
        return items.Contains(item);
    }

    public void CopyTo(object?[] array, int arrayIndex)
    {
        items.CopyTo(array, arrayIndex);
    }

    public IEnumerator<object?> GetEnumerator()
    {
        return items.GetEnumerator();
    }

    public int IndexOf(object? item)
    {
        return items.IndexOf(item);
    }

    public void Insert(int index, object? item)
    {
        items.Insert(index, item);
        OnChanged(index, removedCount: 0, addedCount: 1);
    }

    public bool Remove(object? item)
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
        items.RemoveAt(index);
        OnChanged(index, removedCount: 1, addedCount: 0);
    }

    public void ReplaceWith(IEnumerable? source)
    {
        List<object?> next = source is null ? [] : [.. source.Cast<object?>()];
        if (items.SequenceEqual(next))
        {
            items.Clear();
            items.AddRange(next);
            return;
        }

        items.Clear();
        items.AddRange(next);
        OnReset();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    private void OnReset()
    {
        IsResetNotification = true;
        try
        {
            Changed?.Invoke(this, EventArgs.Empty);
        }
        finally
        {
            IsResetNotification = false;
        }
    }

    private void OnChanged(int index, int removedCount, int addedCount)
    {
        ChangeIndex = index;
        ChangeRemovedCount = removedCount;
        ChangeAddedCount = addedCount;
        try
        {
            Changed?.Invoke(this, EventArgs.Empty);
        }
        finally
        {
            ChangeIndex = -1;
            ChangeRemovedCount = 0;
            ChangeAddedCount = 0;
        }
    }
}
