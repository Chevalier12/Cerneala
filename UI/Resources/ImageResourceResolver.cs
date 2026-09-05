using Cerneala.Drawing;
using Cerneala.UI.Elements;
using Cerneala.UI.Invalidation;

namespace Cerneala.UI.Resources;

internal static class ImageResourceResolver
{
    internal static ImageResourceResolution Resolve(
        UIElement owner,
        ResourceId<ImageResource> id,
        IResourceProvider? explicitProvider,
        ResourceDependencyTracker? explicitTracker,
        InvalidationFlags effects,
        bool affectsIntrinsicSize)
    {
        ArgumentNullException.ThrowIfNull(owner);

        if (explicitProvider is not null)
        {
            ResourceDependencyTracker? tracker = explicitTracker ?? owner.Root?.ResourceDependencyTracker;
            tracker?.RecordDependency(owner, id, effects, affectsIntrinsicSize);
            long version = tracker?.GetDependencyVersion(owner) ?? GetProviderVersion(explicitProvider, id);
            return explicitProvider.TryGetResource(id, out ImageResource? resource)
                ? new ImageResourceResolution(ResolveImage(owner, resource), version)
                : new ImageResourceResolution(null, version);
        }

        for (UIElement? current = owner;
             current is not null;
             current = current.LogicalParent ?? current.VisualParent)
        {
            if (current.Resources.TryGetResource(id, out ImageResource? resource))
            {
                return new ImageResourceResolution(
                    ResolveImage(owner, resource),
                    current.Resources.Version);
            }

            if (current.Resources.ContainsKey(id.Key))
            {
                return new ImageResourceResolution(null, current.Resources.Version);
            }
        }

        IResourceProvider? rootProvider = owner.Root?.ResourceProvider;
        ResourceDependencyTracker? rootTracker = explicitTracker ?? owner.Root?.ResourceDependencyTracker;
        rootTracker?.RecordDependency(owner, id, effects, affectsIntrinsicSize);
        long rootVersion = rootTracker?.GetDependencyVersion(owner) ??
            GetProviderVersion(rootProvider, id);
        return rootProvider?.TryGetResource(id, out ImageResource? rootResource) == true
            ? new ImageResourceResolution(ResolveImage(owner, rootResource), rootVersion)
            : new ImageResourceResolution(null, rootVersion);
    }

    private static IDrawImage ResolveImage(UIElement owner, ImageResource resource)
    {
        ImageResourceCache? cache = owner.Root?.ImageResourceCache;
        return cache is null ? resource.Resolve() : cache.Resolve(resource);
    }

    private static long GetProviderVersion(
        IResourceProvider? provider,
        ResourceId<ImageResource> id)
    {
        return provider switch
        {
            ResourceStore store => store.GetVersion(id),
            ResourceDictionary dictionary => dictionary.Version,
            _ => 0
        };
    }
}

internal readonly record struct ImageResourceResolution(
    IDrawImage? Image,
    long Version);
