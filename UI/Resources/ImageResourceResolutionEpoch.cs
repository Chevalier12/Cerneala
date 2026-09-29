using Cerneala.UI.Elements;

namespace Cerneala.UI.Resources;

// Inputs to resource-backed image resolution under one root: its resource
// dictionaries and provider, the element ancestry resolution walks, and the
// root's image cache identities and residency. A consumer may reuse what it
// resolved only while the stamp it captured is still current. Values come
// from one process-wide sequence, so stamps of different roots never match.
internal readonly record struct ImageResolutionStamp(long RootEpoch, long CacheGeneration)
{
    internal bool IsSet => RootEpoch != 0;
}

internal static class ImageResourceResolutionEpoch
{
    private static long sequence;

    internal static long Next() => Interlocked.Increment(ref sequence);

    // An element outside any root resolves nothing through a cache and has no
    // owner to announce changes, so it never yields a reusable stamp. A root
    // with a non-observable provider likewise cannot vouch for unchanged
    // resolution: IResourceProvider does not promise immutable resources.
    internal static ImageResolutionStamp Capture(UIRoot? root) =>
        root is null || root.ResourceProvider is not (null or IObservableResourceProvider)
            ? default
            : new(root.ImageResolutionEpoch, root.ImageResourceCache?.ResolutionGeneration ?? 0);
}
