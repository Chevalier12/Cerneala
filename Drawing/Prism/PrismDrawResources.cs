using System.Runtime.CompilerServices;
using Cerneala.UI.Prism.Definitions;

namespace Cerneala.Drawing.Prism;

internal readonly record struct PrismDrawImageResource(
    PrismResourceId Id,
    IDrawImage Image,
    long Version = 0);

internal sealed class PrismDrawResources
{
    private readonly Dictionary<PrismResourceId, ResolvedImage> images;

    private PrismDrawResources(
        Dictionary<PrismResourceId, ResolvedImage> images)
    {
        this.images = images;
    }

    public static PrismDrawResources Empty { get; } =
        new([]);

    public static PrismDrawResources Create(
        IEnumerable<PrismDrawImageResource> resources)
    {
        ArgumentNullException.ThrowIfNull(resources);
        Dictionary<PrismResourceId, ResolvedImage> images = [];
        foreach (PrismDrawImageResource resource in resources)
        {
            ArgumentNullException.ThrowIfNull(resource.Image);
            long version = resource.Version > 0
                ? resource.Version
                : checked(
                    (long)(uint)RuntimeHelpers.GetHashCode(
                        resource.Image) + 1);
            images[resource.Id] =
                new ResolvedImage(resource.Image, version);
        }

        return images.Count == 0
            ? Empty
            : new PrismDrawResources(images);
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

    private readonly record struct ResolvedImage(
        IDrawImage Image,
        long Version);
}
