using System.Text;
using Cerneala.Drawing.Prism.Catalog;

namespace Cerneala.UI.Prism.Definitions;

public sealed class PrismFilterDefinition : IEquatable<PrismFilterDefinition>
{
    public PrismFilterDefinition(
        PrismFilterId filter,
        bool visible = PrismCatalogGenerated.FilterVisible,
        float opacity = PrismCatalogGenerated.FilterOpacity,
        PrismBlendMode blendMode = PrismCatalogGenerated.FilterBlendMode)
    {
        if (blendMode == PrismBlendMode.PassThrough)
        {
            throw new ArgumentException("PassThrough is valid only for Prism groups.", nameof(blendMode));
        }

        Filter = filter;
        Visible = visible;
        Opacity = PrismDefinitionValidation.UnitInterval(opacity, nameof(opacity));
        BlendMode = blendMode;
    }

    public PrismFilterId Filter { get; }

    public bool Visible { get; }

    public float Opacity { get; }

    public PrismBlendMode BlendMode { get; }

    public bool Equals(PrismFilterDefinition? other)
    {
        return other is not null &&
            Filter == other.Filter &&
            Visible == other.Visible &&
            Opacity.Equals(other.Opacity) &&
            BlendMode == other.BlendMode;
    }

    public override bool Equals(object? obj) => obj is PrismFilterDefinition other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(Filter, Visible, Opacity, BlendMode);

    internal void AppendDiagnostic(StringBuilder builder, int depth)
    {
        builder.Append(' ', depth * 2)
            .Append("Filter ")
            .Append(Filter)
            .Append(" visible=")
            .Append(Visible)
            .Append(" opacity=")
            .Append(Opacity.ToString("R", System.Globalization.CultureInfo.InvariantCulture))
            .Append(" blend=")
            .Append(BlendMode)
            .AppendLine();
    }
}
