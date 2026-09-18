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
        bool affectsIntrinsicSize,
        ImageResourceLeaseSet? acquisitions = null,
        ImageResourceAccess access = ImageResourceAccess.Synchronous,
        Action<UIElement>? onPrepared = null)
    {
        ArgumentNullException.ThrowIfNull(owner);

        if (explicitProvider is not null)
        {
            ResourceDependencyTracker? tracker = explicitTracker ?? owner.Root?.ResourceDependencyTracker;
            tracker?.RecordDependency(owner, id, effects, affectsIntrinsicSize);
            long version = tracker?.GetDependencyVersion(owner) ?? GetProviderVersion(explicitProvider, id);
            return explicitProvider.TryGetResource(id, out ImageResource? resource)
                ? ResolveImage(owner, resource, acquisitions, access, version, effects, onPrepared)
                : new ImageResourceResolution(null, version);
        }

        for (UIElement? current = owner;
             current is not null;
             current = current.LogicalParent ?? current.VisualParent)
        {
            if (current.Resources.TryGetResource(id, out ImageResource? resource))
            {
                return ResolveImage(owner, resource, acquisitions, access,
                    current.Resources.Version, effects, onPrepared);
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
            ? ResolveImage(owner, rootResource, acquisitions, access, rootVersion, effects, onPrepared)
            : new ImageResourceResolution(null, rootVersion);
    }

    internal static ImageResourceResolution ResolveImage(UIElement owner, ImageResource resource,
        ImageResourceLeaseSet? acquisitions, ImageResourceAccess access, long version,
        InvalidationFlags effects, Action<UIElement>? onPrepared)
    {
        ImageResourceCache? cache = owner.Root?.ImageResourceCache;
        ImageResourceLeaseSet leases = acquisitions ?? owner.SourceImageLeases;
        if (access == ImageResourceAccess.Prepare)
        {
            ImageResourceLoadResult result = leases.Prepare(resource, cache, out ImageResourceLeaseSet.Acquisition? started);
            // Use the readiness snapshot that is returned to the consumer. The
            // task may finish between that snapshot and observer installation.
            if (result.IsPending && started?.Completion is Task completion && owner.Root is UIRoot root)
            {
                _ = RefreshOnCompletionAsync(completion, new(owner), new(root), started, effects, onPrepared);
            }
            return new(result.Image, version, result.IsPending, result.Error);
        }
        return new(access == ImageResourceAccess.ResidentOnly
            ? leases.TryAcquireResident(resource, cache) : leases.Acquire(resource, cache), version);
    }

    private static async Task RefreshOnCompletionAsync(Task completion, WeakReference<UIElement> ownerReference,
        WeakReference<UIRoot> rootReference, ImageResourceLeaseSet.Acquisition acquisition,
        InvalidationFlags effects, Action<UIElement>? onPrepared)
    {
        // Failures are exposed by the same acquisition on the next UI resolve.
        // No image/lease is transferred to or owned by this notification. A
        // shared load must not retain a detached tree until another consumer's
        // load finishes. Callbacks receive the live owner instead of capturing it.
        try { await completion.ConfigureAwait(false); }
        catch { }
        if (!acquisition.IsActive || !rootReference.TryGetTarget(out UIRoot? root)) { return; }
        root.Relay.Post(() =>
        {
            if (!acquisition.IsActive || !ownerReference.TryGetTarget(out UIElement? owner) ||
                !rootReference.TryGetTarget(out UIRoot? currentRoot) || !ReferenceEquals(owner.Root, currentRoot)) { return; }
            if (onPrepared is not null) { onPrepared(owner); }
            else
            {
                owner.IncrementRenderVersion();
                owner.Invalidate(effects, "Image preparation completed");
            }
        });
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
    long Version,
    bool IsPending = false,
    Exception? Error = null);

internal enum ImageResourceAccess { Synchronous, ResidentOnly, Prepare }
