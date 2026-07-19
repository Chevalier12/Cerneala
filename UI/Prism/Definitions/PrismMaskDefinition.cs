using Cerneala.Drawing.Prism.Catalog;

namespace Cerneala.UI.Prism.Definitions;

public enum PrismMaskChannel
{
    Alpha,
    Luminance
}

public sealed class PrismMaskDefinition : IEquatable<PrismMaskDefinition>
{
    public PrismMaskDefinition(
        PrismResourceId image,
        PrismMaskChannel channel = PrismCatalogGenerated.MaskChannel,
        float feather = PrismCatalogGenerated.MaskFeather,
        float density = PrismCatalogGenerated.MaskDensity,
        bool invert = PrismCatalogGenerated.MaskInvert)
    {
        Image = image;
        Channel = channel;
        Feather = feather >= 0f
            ? PrismDefinitionValidation.Finite(feather, nameof(feather))
            : throw new ArgumentOutOfRangeException(nameof(feather), feather, "Mask feather cannot be negative.");
        Density = PrismDefinitionValidation.UnitInterval(density, nameof(density));
        Invert = invert;
    }

    public PrismResourceId Image { get; }

    public PrismMaskChannel Channel { get; }

    public float Feather { get; }

    public float Density { get; }

    public bool Invert { get; }

    public bool Equals(PrismMaskDefinition? other)
    {
        return other is not null &&
            Image == other.Image &&
            Channel == other.Channel &&
            Feather.Equals(other.Feather) &&
            Density.Equals(other.Density) &&
            Invert == other.Invert;
    }

    public override bool Equals(object? obj) => obj is PrismMaskDefinition other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(Image, Channel, Feather, Density, Invert);
}
