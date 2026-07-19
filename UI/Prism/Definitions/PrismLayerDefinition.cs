using System.Collections.Immutable;
using System.Text;
using Cerneala.Drawing.Prism.Catalog;

namespace Cerneala.UI.Prism.Definitions;

public sealed class PrismLayerDefinition : PrismNodeDefinition
{
    public PrismLayerDefinition(
        PrismNodeId id,
        string? name,
        IEnumerable<PrismFilterDefinition>? filters = null,
        IEnumerable<PrismStyleDefinition>? styles = null,
        PrismMaskDefinition? mask = null,
        bool visible = PrismCatalogGenerated.LayerVisible,
        float opacity = PrismCatalogGenerated.LayerOpacity,
        float fill = PrismCatalogGenerated.LayerFill,
        PrismBlendMode blendMode = PrismCatalogGenerated.LayerBlendMode,
        bool clipToBelow = PrismCatalogGenerated.LayerClipToBelow,
        PrismSourceSpan? sourceSpan = null)
        : base(id, name, sourceSpan)
    {
        if (blendMode == PrismBlendMode.PassThrough)
        {
            throw new ArgumentException("PassThrough is valid only for Prism groups.", nameof(blendMode));
        }

        Filters = PrismDefinitionValidation.ToImmutableArray(filters, nameof(filters));
        Styles = PrismDefinitionValidation.ToImmutableArray(styles, nameof(styles));
        if (Filters.IsEmpty && Styles.IsEmpty)
        {
            throw new ArgumentException("A Prism layer must contain at least one filter or style.");
        }

        Mask = mask;
        Visible = visible;
        Opacity = PrismDefinitionValidation.UnitInterval(opacity, nameof(opacity));
        Fill = PrismDefinitionValidation.UnitInterval(fill, nameof(fill));
        BlendMode = blendMode;
        ClipToBelow = clipToBelow;
    }

    public ImmutableArray<PrismFilterDefinition> Filters { get; }

    public ImmutableArray<PrismStyleDefinition> Styles { get; }

    public PrismMaskDefinition? Mask { get; }

    public bool Visible { get; }

    public float Opacity { get; }

    public float Fill { get; }

    public PrismBlendMode BlendMode { get; }

    public bool ClipToBelow { get; }

    public override bool Equals(PrismNodeDefinition? other)
    {
        return other is PrismLayerDefinition layer &&
            BaseEquals(layer) &&
            Filters.SequenceEqual(layer.Filters) &&
            Styles.SequenceEqual(layer.Styles) &&
            Equals(Mask, layer.Mask) &&
            Visible == layer.Visible &&
            Opacity.Equals(layer.Opacity) &&
            Fill.Equals(layer.Fill) &&
            BlendMode == layer.BlendMode &&
            ClipToBelow == layer.ClipToBelow;
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
        hash.Add(Fill);
        hash.Add(BlendMode);
        hash.Add(ClipToBelow);
        return hash.ToHashCode();
    }

    internal override void AppendDiagnostic(StringBuilder builder, int depth)
    {
        Indent(builder, depth);
        builder.Append("Layer #")
            .Append(Id.Value)
            .Append(" name=")
            .Append(Name ?? "<unnamed>")
            .Append(" visible=")
            .Append(Visible)
            .Append(" opacity=")
            .Append(Opacity.ToString("R", System.Globalization.CultureInfo.InvariantCulture))
            .Append(" fill=")
            .Append(Fill.ToString("R", System.Globalization.CultureInfo.InvariantCulture))
            .Append(" blend=")
            .Append(BlendMode)
            .Append(" clipToBelow=")
            .Append(ClipToBelow)
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
