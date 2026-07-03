namespace Cerneala.UI.Resources;

public sealed class ResourceDependencyTracker
{
    private readonly Dictionary<ResourceKey, HashSet<object>> ownersByResource = new();
    private readonly Dictionary<object, long> ownerVersions = new(ReferenceEqualityComparer.Instance);
    private readonly Dictionary<ResourceKey, long> resourceVersions = new();
    private long nextOwnerVersion;

    public void Track(ResourceStore store)
    {
        ArgumentNullException.ThrowIfNull(store);
        store.ResourceChanged += OnResourceChanged;
    }

    public void RecordDependency<T>(object owner, ResourceId<T> id)
    {
        ArgumentNullException.ThrowIfNull(owner);
        ResourceKey key = ResourceKey.From(id);
        if (!ownersByResource.TryGetValue(key, out HashSet<object>? owners))
        {
            owners = new HashSet<object>(ReferenceEqualityComparer.Instance);
            ownersByResource.Add(key, owners);
        }

        owners.Add(owner);
        ownerVersions.TryAdd(owner, 0);
    }

    public long GetDependencyVersion(object owner)
    {
        ArgumentNullException.ThrowIfNull(owner);
        return ownerVersions.TryGetValue(owner, out long version) ? version : 0;
    }

    public long GetResourceVersion<T>(ResourceId<T> id)
    {
        return resourceVersions.TryGetValue(ResourceKey.From(id), out long version) ? version : 0;
    }

    public IReadOnlyCollection<object> GetDependents<T>(ResourceId<T> id)
    {
        return ownersByResource.TryGetValue(ResourceKey.From(id), out HashSet<object>? owners)
            ? owners.ToArray()
            : Array.Empty<object>();
    }

    public void NotifyResourceChanged(ResourceChangedEventArgs args)
    {
        ArgumentNullException.ThrowIfNull(args);
        ResourceKey key = new(args.ResourceType, args.Key);
        resourceVersions[key] = args.Version;
        if (!ownersByResource.TryGetValue(key, out HashSet<object>? owners))
        {
            return;
        }

        foreach (object owner in owners)
        {
            ownerVersions[owner] = ++nextOwnerVersion;
        }
    }

    private void OnResourceChanged(object? sender, ResourceChangedEventArgs args)
    {
        NotifyResourceChanged(args);
    }

    private readonly record struct ResourceKey(Type Type, string Key)
    {
        public static ResourceKey From<T>(ResourceId<T> id)
        {
            return new ResourceKey(typeof(T), id.Key);
        }
    }

    private sealed class ReferenceEqualityComparer : IEqualityComparer<object>
    {
        public static ReferenceEqualityComparer Instance { get; } = new();

        public new bool Equals(object? x, object? y)
        {
            return ReferenceEquals(x, y);
        }

        public int GetHashCode(object obj)
        {
            return System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(obj);
        }
    }
}
