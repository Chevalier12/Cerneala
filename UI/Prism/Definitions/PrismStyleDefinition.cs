using System.Text;
using Cerneala.Drawing.Prism.Catalog;

namespace Cerneala.UI.Prism.Definitions;

public sealed class PrismStyleDefinition : IEquatable<PrismStyleDefinition>
{
    public PrismStyleDefinition(
        PrismStyleId style,
        bool visible = PrismCatalogGenerated.StyleVisible)
    {
        Style = style;
        Visible = visible;
    }

    public PrismStyleId Style { get; }

    public bool Visible { get; }

    public bool Equals(PrismStyleDefinition? other)
    {
        return other is not null && Style == other.Style && Visible == other.Visible;
    }

    public override bool Equals(object? obj) => obj is PrismStyleDefinition other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(Style, Visible);

    internal void AppendDiagnostic(StringBuilder builder, int depth)
    {
        builder.Append(' ', depth * 2)
            .Append("Style ")
            .Append(Style)
            .Append(" visible=")
            .Append(Visible)
            .AppendLine();
    }
}
