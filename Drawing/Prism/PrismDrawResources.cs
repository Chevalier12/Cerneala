using Cerneala.UI.Prism.Definitions;

namespace Cerneala.Drawing.Prism;

internal readonly record struct PrismDrawImageResource(
    PrismResourceId Id,
    IDrawImage Image,
    long Version = 0,
    long Identity = 0);

internal sealed class PrismDrawResources
{
    private readonly Dictionary<PrismResourceId, ResolvedImage> images;

    private PrismDrawResources(
        Dictionary<PrismResourceId, ResolvedImage> images,
        bool hasStableVersions)
    {
        this.images = images;
        HasStableVersions = hasStableVersions;
    }

    public static PrismDrawResources Empty { get; } =
        new([], hasStableVersions: true);

    public bool HasStableVersions { get; }

    public static PrismDrawResources Create(
        IEnumerable<PrismDrawImageResource> resources)
    {
        ArgumentNullException.ThrowIfNull(resources);
        Dictionary<PrismResourceId, ResolvedImage> images = [];
        foreach (PrismDrawImageResource resource in resources)
        {
            ArgumentNullException.ThrowIfNull(resource.Image);
            if (resource.Version < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(resources),
                    resource.Version,
                    "Prism resource versions cannot be negative.");
            }
            if (resource.Identity < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(resources),
                    resource.Identity,
                    "Prism resource identities cannot be negative.");
            }

            images[resource.Id] =
                new ResolvedImage(
                    resource.Image,
                    resource.Identity,
                    resource.Version);
        }

        if (images.Count == 0)
        {
            return Empty;
        }

        bool hasStableVersions = true;
        foreach (ResolvedImage image in images.Values)
        {
            if (image.Version <= 0)
            {
                hasStableVersions = false;
                break;
            }
        }

        return new PrismDrawResources(images, hasStableVersions);
    }

    public bool TryGetImage(
        PrismResourceId id,
        out IDrawImage image)
    {
        if (images.TryGetValue(id, out ResolvedImage resource))
        {
            image = resource.Image;
            return true;
        }

        image = null!;
        return false;
    }

    public bool TryGetVersion(
        PrismResourceId id,
        out long version)
    {
        if (images.TryGetValue(id, out ResolvedImage resource))
        {
            version = resource.Version;
            return true;
        }

        version = 0;
        return false;
    }

    public bool TryGetDependency(
        PrismResourceId id,
        out long identity,
        out long version)
    {
        if (images.TryGetValue(id, out ResolvedImage resource))
        {
            identity = resource.Identity;
            version = resource.Version;
            return true;
        }

        identity = 0;
        version = 0;
        return false;
    }

    private readonly record struct ResolvedImage(
        IDrawImage Image,
        long Identity,
        long Version);
}
