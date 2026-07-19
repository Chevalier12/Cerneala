using System.Text;

namespace Cerneala.UI.Prism.Definitions;

public abstract class PrismNodeDefinition : IEquatable<PrismNodeDefinition>
{
    protected PrismNodeDefinition(PrismNodeId id, string? name)
    {
        Id = id;
        Name = PrismDefinitionValidation.ValidateOptionalName(name, nameof(name));
    }

    public PrismNodeId Id { get; }

    public string? Name { get; }

    public abstract bool Equals(PrismNodeDefinition? other);

    public sealed override bool Equals(object? obj) => obj is PrismNodeDefinition other && Equals(other);

    public abstract override int GetHashCode();

    internal abstract void AppendDiagnostic(StringBuilder builder, int depth);

    protected bool BaseEquals(PrismNodeDefinition other)
    {
        return Id == other.Id && string.Equals(Name, other.Name, StringComparison.Ordinal);
    }

    protected static void Indent(StringBuilder builder, int depth)
    {
        builder.Append(' ', depth * 2);
    }
}
