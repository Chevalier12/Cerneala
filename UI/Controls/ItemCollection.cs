using System.Collections;
using Cerneala.UI.Invalidation;

namespace Cerneala.UI.Controls;

public sealed class ItemCollection : IList<object?>
{
    private readonly List<object?> items = [];

    public event EventHandler? Changed;

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
            OnChanged();
        }
    }

    public void Add(object? item)
    {
        items.Add(item);
        OnChanged();
    }

    public void Clear()
    {
        if (items.Count == 0)
        {
            return;
        }

        items.Clear();
        OnChanged();
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
        OnChanged();
    }

    public bool Remove(object? item)
    {
        bool removed = items.Remove(item);
        if (removed)
        {
            OnChanged();
        }

        return removed;
    }

    public void RemoveAt(int index)
    {
        items.RemoveAt(index);
        OnChanged();
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
        OnChanged();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    private void OnChanged()
    {
        Changed?.Invoke(this, EventArgs.Empty);
    }
}
