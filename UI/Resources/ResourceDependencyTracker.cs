using Cerneala.UI.Elements;
using Cerneala.UI.Invalidation;

namespace Cerneala.UI.Resources;

public sealed class ResourceDependencyTracker
{
    private readonly Dictionary<ResourceKey, Dictionary<UIElement, ResourceDependency>> dependenciesByResource = new();
    private readonly Dictionary<UIElement, long> ownerVersions = new(ReferenceEqualityComparer.Instance);
    private readonly Dictionary<ResourceKey, long> resourceVersions = new();

    // Every type a key was read as or changed as, so a change that applies to every type reaches them all.
    private readonly Dictionary<string, HashSet<Type>> resourceTypesByKey = new(StringComparer.Ordinal);
    private readonly HashSet<IObservableResourceProvider> trackedProviders = new(ReferenceEqualityComparer.Instance);
    private long nextOwnerVersion;

    public void Track(IObservableResourceProvider provider)
    {
        ArgumentNullException.ThrowIfNull(provider);
        if (!trackedProviders.Add(provider))
        {
            return;
        }

        provider.ResourceChanged += OnResourceChanged;
    }

    public void RecordDependency<T>(UIElement owner, ResourceId<T> id)
    {
        RecordDependency(owner, id, InvalidationFlags.Render);
    }

    public void RecordDependency<T>(
        UIElement owner,
        ResourceId<T> id,
        InvalidationFlags effects,
        bool affectsIntrinsicSize = true)
    {
        ArgumentNullException.ThrowIfNull(owner);
        ResourceKey key = ResourceKey.From(id);
        if (!dependenciesByResource.TryGetValue(key, out Dictionary<UIElement, ResourceDependency>? dependencies))
        {
            dependencies = new Dictionary<UIElement, ResourceDependency>(ReferenceEqualityComparer.Instance);
            dependenciesByResource.Add(key, dependencies);
        }

        dependencies[owner] = new ResourceDependency(owner, key, effects, affectsIntrinsicSize);
        ownerVersions.TryAdd(owner, 0);
        GetResourceTypes(key.Key).Add(key.Type);
    }

    public long GetDependencyVersion(UIElement owner)
    {
        ArgumentNullException.ThrowIfNull(owner);
        return ownerVersions.TryGetValue(owner, out long version) ? version : 0;
    }

    public long GetResourceVersion<T>(ResourceId<T> id)
    {
        return resourceVersions.TryGetValue(ResourceKey.From(id), out long version) ? version : 0;
    }

    public IReadOnlyCollection<UIElement> GetDependents<T>(ResourceId<T> id)
    {
        if (!dependenciesByResource.TryGetValue(ResourceKey.From(id), out Dictionary<UIElement, ResourceDependency>? dependencies))
        {
            return Array.Empty<UIElement>();
        }

        return dependencies.Keys.ToArray();
    }

    public void RemoveOwner(UIElement owner)
    {
        ArgumentNullException.ThrowIfNull(owner);
        ownerVersions.Remove(owner);
        foreach (ResourceKey key in dependenciesByResource.Keys.ToArray())
        {
            Dictionary<UIElement, ResourceDependency> dependencies = dependenciesByResource[key];
            dependencies.Remove(owner);
            if (dependencies.Count == 0)
            {
                dependenciesByResource.Remove(key);
            }
        }
    }

    public IReadOnlyList<ResourceDependencyChange> NotifyResourceChanged(ResourceChangedEventArgs args)
    {
        ArgumentNullException.ThrowIfNull(args);
        HashSet<Type> types = GetResourceTypes(args.Key);
        types.Add(args.ResourceType);
        if (!args.AppliesToEveryResourceType || types.Count == 1)
        {
            return NotifyResourceChanged(new ResourceKey(args.ResourceType, args.Key), args.Version);
        }

        // One change record per owner, also when the owner read the key as several types.
        Dictionary<UIElement, ResourceDependencyChange> changesByOwner = new(ReferenceEqualityComparer.Instance);
        foreach (Type type in types)
        {
            foreach (ResourceDependencyChange change in NotifyResourceChanged(new ResourceKey(type, args.Key), args.Version))
            {
                changesByOwner[change.Owner] = changesByOwner.TryGetValue(change.Owner, out ResourceDependencyChange? earlier)
                    ? new ResourceDependencyChange(
                        change.Owner,
                        earlier.Effects | change.Effects,
                        earlier.AffectsIntrinsicSize || change.AffectsIntrinsicSize)
                    : change;
            }
        }

        return changesByOwner.Count == 0 ? Array.Empty<ResourceDependencyChange>() : changesByOwner.Values.ToArray();
    }

    private ResourceDependencyChange[] NotifyResourceChanged(ResourceKey key, long version)
    {
        resourceVersions[key] = version;
        if (!dependenciesByResource.TryGetValue(key, out Dictionary<UIElement, ResourceDependency>? dependencies))
        {
            return Array.Empty<ResourceDependencyChange>();
        }

        CleanupDetachedOwners(dependencies);
        if (dependencies.Count == 0)
        {
            return Array.Empty<ResourceDependencyChange>();
        }

        ResourceDependencyChange[] changes = new ResourceDependencyChange[dependencies.Count];
        int index = 0;
        foreach (ResourceDependency dependency in dependencies.Values)
        {
            ownerVersions[dependency.Owner] = ++nextOwnerVersion;
            changes[index++] = new ResourceDependencyChange(
                dependency.Owner,
                dependency.Effects,
                dependency.AffectsIntrinsicSize);
        }

        return changes;
    }

    private HashSet<Type> GetResourceTypes(string key)
    {
        if (!resourceTypesByKey.TryGetValue(key, out HashSet<Type>? types))
        {
            types = new HashSet<Type>();
            resourceTypesByKey.Add(key, types);
        }

        return types;
    }

    private void OnResourceChanged(object? sender, ResourceChangedEventArgs args)
    {
        NotifyResourceChanged(args);
    }

    private static void CleanupDetachedOwners(Dictionary<UIElement, ResourceDependency> dependencies)
    {
        foreach (UIElement owner in dependencies.Keys.Where(owner => !owner.IsAttached).ToArray())
        {
            dependencies.Remove(owner);
        }
    }

    private sealed record ResourceDependency(
        UIElement Owner,
        ResourceKey Key,
        InvalidationFlags Effects,
        bool AffectsIntrinsicSize);

    private readonly record struct ResourceKey(Type Type, string Key)
    {
        public static ResourceKey From<T>(ResourceId<T> id)
        {
            return new ResourceKey(typeof(T), id.Key);
        }
    }
}

public sealed record ResourceDependencyChange(
    UIElement Owner,
    InvalidationFlags Effects,
    bool AffectsIntrinsicSize);
