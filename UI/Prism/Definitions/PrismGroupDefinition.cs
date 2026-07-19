using System.Collections.Immutable;
using System.Text;
using Cerneala.Drawing.Prism.Catalog;

namespace Cerneala.UI.Prism.Definitions;

public sealed class PrismGroupDefinition : PrismNodeDefinition
{
    public PrismGroupDefinition(
        PrismNodeId id,
        string? name,
        IEnumerable<PrismNodeDefinition> children,
        IEnumerable<PrismFilterDefinition>? filters = null,
        IEnumerable<PrismStyleDefinition>? styles = null,
        PrismMaskDefinition? mask = null,
        bool visible = PrismCatalogGenerated.GroupVisible,
        float opacity = PrismCatalogGenerated.GroupOpacity,
        PrismBlendMode blendMode = PrismCatalogGenerated.GroupBlendMode)
        : base(id, name)
    {
        Children = PrismDefinitionValidation.ToImmutableArray(children, nameof(children));
        if (Children.IsEmpty)
        {
            throw new ArgumentException("A Prism group must contain at least one layer or nested group.", nameof(children));
        }
        if (Children.Any(child => child is not PrismLayerDefinition and not PrismGroupDefinition))
        {
            throw new ArgumentException("A Prism group can contain only layers and nested groups.", nameof(children));
        }

        Filters = PrismDefinitionValidation.ToImmutableArray(filters, nameof(filters));
        Styles = PrismDefinitionValidation.ToImmutableArray(styles, nameof(styles));
        Mask = mask;
        Visible = visible;
        Opacity = PrismDefinitionValidation.UnitInterval(opacity, nameof(opacity));
        BlendMode = blendMode;
    }

    public ImmutableArray<PrismNodeDefinition> Children { get; }

    public ImmutableArray<PrismFilterDefinition> Filters { get; }

    public ImmutableArray<PrismStyleDefinition> Styles { get; }

    public PrismMaskDefinition? Mask { get; }

    public bool Visible { get; }

    public float Opacity { get; }

    public PrismBlendMode BlendMode { get; }

    public IEnumerable<PrismNodeDefinition> EnumerateChildrenBottomUp()
    {
        for (int index = Children.Length - 1; index >= 0; index--)
        {
            yield return Children[index];
        }
    }

    public override bool Equals(PrismNodeDefinition? other)
    {
        return other is PrismGroupDefinition group &&
            BaseEquals(group) &&
            Children.SequenceEqual(group.Children) &&
            Filters.SequenceEqual(group.Filters) &&
            Styles.SequenceEqual(group.Styles) &&
            Equals(Mask, group.Mask) &&
            Visible == group.Visible &&
            Opacity.Equals(group.Opacity) &&
            BlendMode == group.BlendMode;
    }

    public override int GetHashCode()
    {
        HashCode hash = new();
        hash.Add(Id);
        hash.Add(Name, StringComparer.Ordinal);
        hash.Add(PrismDefinitionValidation.SequenceHash(Children));
        hash.Add(PrismDefinitionValidation.SequenceHash(Filters));
        hash.Add(PrismDefinitionValidation.SequenceHash(Styles));
        hash.Add(Mask);
        hash.Add(Visible);
        hash.Add(Opacity);
        hash.Add(BlendMode);
        return hash.ToHashCode();
    }

    internal override void AppendDiagnostic(StringBuilder builder, int depth)
    {
        Indent(builder, depth);
        builder.Append("Group #")
            .Append(Id.Value)
            .Append(" name=")
            .Append(Name ?? "<unnamed>")
            .Append(" visible=")
            .Append(Visible)
            .Append(" opacity=")
            .Append(Opacity.ToString("R", System.Globalization.CultureInfo.InvariantCulture))
            .Append(" blend=")
            .Append(BlendMode)
            .AppendLine();
        foreach (PrismNodeDefinition child in Children)
        {
            child.AppendDiagnostic(builder, depth + 1);
        }
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
