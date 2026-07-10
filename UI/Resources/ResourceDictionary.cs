using System.Collections;

namespace Cerneala.UI.Resources;

public sealed class ResourceDictionary : IObservableResourceProvider, IEnumerable<KeyValuePair<object, object?>>
{
    private readonly Dictionary<object, object?> entries = new();
    private long version;

    public event EventHandler<ResourceChangedEventArgs>? ResourceChanged;

    public int Count => entries.Count;

    public ICollection<object> Keys => entries.Keys;

    public ICollection<object?> Values => entries.Values;

    public object? this[object key]
    {
        get => entries[ValidateKey(key)];
        set => Set(key, value, value?.GetType() ?? typeof(object));
    }

    public void Add(object key, object? resource)
    {
        key = ValidateKey(key);
        entries.Add(key, resource);
        RaiseChanged(key, resource?.GetType() ?? typeof(object), null, resource);
    }

    public void SetResource<T>(ResourceId<T> id, T resource)
    {
        Set(id.Key, resource, typeof(T));
    }

    public bool ContainsKey(object key)
    {
        return entries.ContainsKey(ValidateKey(key));
    }

    public bool TryGetValue(object key, out object? resource)
    {
        return entries.TryGetValue(ValidateKey(key), out resource);
    }

    public bool TryGetResource<T>(object key, out T resource)
    {
        if (TryGetValue(key, out object? value))
        {
            if (value is T typed)
            {
                resource = typed;
                return true;
            }

            if (value is null && default(T) is null)
            {
                resource = default!;
                return true;
            }
        }

        resource = default!;
        return false;
    }

    public bool TryGetResource<T>(ResourceId<T> id, out T resource)
    {
        return TryGetResource(id.Key, out resource);
    }

    public bool Remove(object key)
    {
        key = ValidateKey(key);
        if (!entries.Remove(key, out object? oldValue))
        {
            return false;
        }

        RaiseChanged(key, oldValue?.GetType() ?? typeof(object), oldValue, null);
        return true;
    }

    public void Clear()
    {
        KeyValuePair<object, object?>[] removed = entries.ToArray();
        entries.Clear();
        foreach ((object key, object? oldValue) in removed)
        {
            RaiseChanged(key, oldValue?.GetType() ?? typeof(object), oldValue, null);
        }
    }

    public IEnumerator<KeyValuePair<object, object?>> GetEnumerator()
    {
        return entries.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    private void Set(object key, object? resource, Type resourceType)
    {
        key = ValidateKey(key);
        entries.TryGetValue(key, out object? oldValue);
        if (entries.ContainsKey(key) && Equals(oldValue, resource))
        {
            return;
        }

        entries[key] = resource;
        RaiseChanged(key, resourceType, oldValue, resource);
    }

    private void RaiseChanged(object key, Type resourceType, object? oldValue, object? newValue)
    {
        ResourceChanged?.Invoke(
            this,
            new ResourceChangedEventArgs(resourceType, KeyText(key), oldValue, newValue, ++version));
    }

    private static object ValidateKey(object key)
    {
        ArgumentNullException.ThrowIfNull(key);
        if (key is string text && string.IsNullOrWhiteSpace(text))
        {
            throw new ArgumentException("Resource key cannot be empty.", nameof(key));
        }

        return key;
    }

    private static string KeyText(object key)
    {
        return key is Type type
            ? type.FullName ?? type.Name
            : key.ToString() ?? key.GetType().FullName ?? key.GetType().Name;
    }
}
