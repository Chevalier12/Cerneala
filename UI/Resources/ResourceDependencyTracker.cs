using Cerneala.UI.Elements;
using Cerneala.UI.Invalidation;

namespace Cerneala.UI.Resources;

public sealed class ResourceDependencyTracker
{
    private readonly Dictionary<ResourceKey, Dictionary<UIElement, ResourceDependency>> dependenciesByResource = new();
    private readonly Dictionary<UIElement, long> ownerVersions = new(ReferenceEqualityComparer.Instance);
    private readonly Dictionary<ResourceKey, long> resourceVersions = new();
    private long nextOwnerVersion;

    public void Track(IObservableResourceProvider provider)
    {
        ArgumentNullException.ThrowIfNull(provider);
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

    public IReadOnlyList<ResourceDependencyChange> NotifyResourceChanged(ResourceChangedEventArgs args)
    {
        ArgumentNullException.ThrowIfNull(args);
        ResourceKey key = new(args.ResourceType, args.Key);
        resourceVersions[key] = args.Version;
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

    private sealed class ReferenceEqualityComparer : IEqualityComparer<UIElement>
    {
        public static ReferenceEqualityComparer Instance { get; } = new();

        public bool Equals(UIElement? x, UIElement? y)
        {
            return ReferenceEquals(x, y);
        }

        public int GetHashCode(UIElement obj)
        {
            return System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(obj);
        }
    }
}

public sealed record ResourceDependencyChange(
    UIElement Owner,
    InvalidationFlags Effects,
    bool AffectsIntrinsicSize);
