namespace Cerneala.UI.Resources;

public sealed class ResourceStore : IResourceProvider
{
    private readonly Dictionary<ResourceKey, Entry> entries = new();

    public event EventHandler<ResourceChangedEventArgs>? ResourceChanged;

    public void SetResource<T>(ResourceId<T> id, T resource)
    {
        ResourceKey key = ResourceKey.From(id);
        entries.TryGetValue(key, out Entry? oldEntry);
        object? oldValue = oldEntry?.Value;
        if (oldEntry is not null && EqualityComparer<T>.Default.Equals((T?)oldValue, resource))
        {
            return;
        }

        long version = (oldEntry?.Version ?? 0) + 1;
        entries[key] = new Entry(resource, version);
        ResourceChanged?.Invoke(this, new ResourceChangedEventArgs(typeof(T), id.Key, oldValue, resource, version));
    }

    public bool TryGetResource<T>(ResourceId<T> id, out T resource)
    {
        if (entries.TryGetValue(ResourceKey.From(id), out Entry? entry))
        {
            if (entry.Value is T typed)
            {
                resource = typed;
                return true;
            }

            if (entry.Value is null && default(T) is null)
            {
                resource = default!;
                return true;
            }
        }

        resource = default!;
        return false;
    }

    public T GetResource<T>(ResourceId<T> id)
    {
        return TryGetResource(id, out T? resource)
            ? resource
            : throw new KeyNotFoundException($"Resource '{id}' was not found.");
    }

    public long GetVersion<T>(ResourceId<T> id)
    {
        return entries.TryGetValue(ResourceKey.From(id), out Entry? entry) ? entry.Version : 0;
    }

    private sealed record Entry(object? Value, long Version);

    private readonly record struct ResourceKey(Type Type, string Key)
    {
        public static ResourceKey From<T>(ResourceId<T> id)
        {
            return new ResourceKey(typeof(T), id.Key);
        }
    }
}
