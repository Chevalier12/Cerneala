using System.Runtime.CompilerServices;
using Cerneala.Drawing;

namespace Cerneala.UI.Resources;

/// <summary>A direct image or a typed image resource reference, never both.</summary>
public sealed class ImageReference : IEquatable<ImageReference>
{
    public ImageReference(IDrawImage image)
    {
        ArgumentNullException.ThrowIfNull(image);
        DirectImage = image;
    }

    public ImageReference(ResourceId<ImageResource> resourceId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(resourceId.Key);
        ResourceId = resourceId;
        ResourceIdentity = resourceId.ToString();
    }

    public IDrawImage? DirectImage { get; }

    public ResourceId<ImageResource>? ResourceId { get; }

    internal string ResourceIdentity { get; } = string.Empty;

    public bool Equals(ImageReference? other) =>
        other is not null && ReferenceEquals(DirectImage, other.DirectImage) && ResourceId == other.ResourceId;

    public override bool Equals(object? obj) => obj is ImageReference other && Equals(other);

    public override int GetHashCode() => DirectImage is not null
        ? RuntimeHelpers.GetHashCode(DirectImage)
        : ResourceId.GetHashCode();
}
