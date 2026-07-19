using System.Collections.Immutable;
using System.Text;
using Cerneala.Drawing.Prism.Catalog;

namespace Cerneala.UI.Prism.Definitions;

public sealed class PrismBackdropDefinition : PrismNodeDefinition
{
    public PrismBackdropDefinition(
        PrismNodeId id,
        string? name,
        IEnumerable<PrismFilterDefinition>? filters = null,
        IEnumerable<PrismStyleDefinition>? styles = null,
        PrismMaskDefinition? mask = null,
        bool visible = PrismCatalogGenerated.BackdropVisible,
        float opacity = PrismCatalogGenerated.BackdropOpacity,
        PrismSourceSpan? sourceSpan = null)
        : base(id, name, sourceSpan)
    {
        Filters = PrismDefinitionValidation.ToImmutableArray(filters, nameof(filters));
        Styles = PrismDefinitionValidation.ToImmutableArray(styles, nameof(styles));
        if (Filters.IsEmpty && Styles.IsEmpty)
        {
            throw new ArgumentException("A Prism backdrop must contain at least one filter or style.");
        }

        Mask = mask;
        Visible = visible;
        Opacity = PrismDefinitionValidation.UnitInterval(opacity, nameof(opacity));
    }

    public ImmutableArray<PrismFilterDefinition> Filters { get; }

    public ImmutableArray<PrismStyleDefinition> Styles { get; }

    public PrismMaskDefinition? Mask { get; }

    public bool Visible { get; }

    public float Opacity { get; }

    public override bool Equals(PrismNodeDefinition? other)
    {
        return other is PrismBackdropDefinition backdrop &&
            BaseEquals(backdrop) &&
            Filters.SequenceEqual(backdrop.Filters) &&
            Styles.SequenceEqual(backdrop.Styles) &&
            Equals(Mask, backdrop.Mask) &&
            Visible == backdrop.Visible &&
            Opacity.Equals(backdrop.Opacity);
    }

    public override int GetHashCode()
    {
        HashCode hash = new();
        hash.Add(Id);
        hash.Add(Name, StringComparer.Ordinal);
        hash.Add(PrismDefinitionValidation.SequenceHash(Filters));
        hash.Add(PrismDefinitionValidation.SequenceHash(Styles));
        hash.Add(Mask);
        hash.Add(Visible);
        hash.Add(Opacity);
        return hash.ToHashCode();
    }

    internal override void AppendDiagnostic(StringBuilder builder, int depth)
    {
        Indent(builder, depth);
        builder.Append("Backdrop #")
            .Append(Id.Value)
            .Append(" name=")
            .Append(Name ?? "<unnamed>")
            .Append(" visible=")
            .Append(Visible)
            .Append(" opacity=")
            .Append(Opacity.ToString("R", System.Globalization.CultureInfo.InvariantCulture))
            .AppendLine();
        foreach (PrismFilterDefinition filter in Filters)
        {
            filter.AppendDiagnostic(builder, depth + 1);
        }
        foreach (PrismStyleDefinition style in Styles)
        {
            style.AppendDiagnostic(builder, depth + 1);
        }
    }
}
