using System.Collections.Immutable;
using System.Text;
using Cerneala.Drawing.Prism.Catalog;

namespace Cerneala.UI.Prism.Definitions;

public sealed class PrismCompositionDefinition : IEquatable<PrismCompositionDefinition>
{
    private readonly ImmutableDictionary<string, PrismNodeId> namedNodes;

    public PrismCompositionDefinition(
        string name,
        IEnumerable<PrismNodeDefinition> nodes,
        PrismColorProfile workingColorProfile = PrismCatalogGenerated.CompositionWorkingColorProfile,
        float globalLightAngle = PrismCatalogGenerated.CompositionGlobalLightAngle,
        float globalLightAltitude = PrismCatalogGenerated.CompositionGlobalLightAltitude)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("A Prism composition name is required.", nameof(name));
        }

        Name = name;
        Nodes = PrismDefinitionValidation.ToImmutableArray(nodes, nameof(nodes));
        if (Nodes.IsEmpty)
        {
            throw new ArgumentException("A Prism composition must contain at least one layer, group, or backdrop.", nameof(nodes));
        }

        WorkingColorProfile = workingColorProfile;
        GlobalLightAngle = PrismDefinitionValidation.Finite(globalLightAngle, nameof(globalLightAngle));
        GlobalLightAltitude = PrismDefinitionValidation.Finite(globalLightAltitude, nameof(globalLightAltitude));
        namedNodes = ValidateAndIndex(Nodes);
    }

    public string Name { get; }

    public ImmutableArray<PrismNodeDefinition> Nodes { get; }

    public PrismColorProfile WorkingColorProfile { get; }

    public float GlobalLightAngle { get; }

    public float GlobalLightAltitude { get; }

    public PrismBackdropDefinition? Backdrop =>
        Nodes[^1] as PrismBackdropDefinition;

    public IEnumerable<PrismNodeDefinition> EnumerateContentBottomUp()
    {
        int start = Backdrop is null ? Nodes.Length - 1 : Nodes.Length - 2;
        for (int index = start; index >= 0; index--)
        {
            yield return Nodes[index];
        }
    }

    public bool TryGetNamedNode(string path, out PrismNodeId nodeId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        return namedNodes.TryGetValue(path, out nodeId);
    }

    public string ToDiagnosticString()
    {
        StringBuilder builder = new();
        builder.Append("Prism ")
            .Append(Name)
            .Append(" profile=")
            .Append(WorkingColorProfile)
            .Append(" light=")
            .Append(GlobalLightAngle.ToString("R", System.Globalization.CultureInfo.InvariantCulture))
            .Append('/')
            .Append(GlobalLightAltitude.ToString("R", System.Globalization.CultureInfo.InvariantCulture))
            .AppendLine();
        foreach (PrismNodeDefinition node in Nodes)
        {
            node.AppendDiagnostic(builder, 1);
        }

        return builder.ToString().TrimEnd();
    }

    public bool Equals(PrismCompositionDefinition? other)
    {
        return other is not null &&
            string.Equals(Name, other.Name, StringComparison.Ordinal) &&
            Nodes.SequenceEqual(other.Nodes) &&
            WorkingColorProfile == other.WorkingColorProfile &&
            GlobalLightAngle.Equals(other.GlobalLightAngle) &&
            GlobalLightAltitude.Equals(other.GlobalLightAltitude);
    }

    public override bool Equals(object? obj) => obj is PrismCompositionDefinition other && Equals(other);

    public override int GetHashCode()
    {
        return HashCode.Combine(
            Name,
            PrismDefinitionValidation.SequenceHash(Nodes),
            WorkingColorProfile,
            GlobalLightAngle,
            GlobalLightAltitude);
    }

    public override string ToString() => ToDiagnosticString();

    private static ImmutableDictionary<string, PrismNodeId> ValidateAndIndex(
        ImmutableArray<PrismNodeDefinition> nodes)
    {
        HashSet<PrismNodeId> ids = new();
        ImmutableDictionary<string, PrismNodeId>.Builder names =
            ImmutableDictionary.CreateBuilder<string, PrismNodeId>(StringComparer.Ordinal);
        int backdropCount = 0;
        for (int index = 0; index < nodes.Length; index++)
        {
            PrismNodeDefinition node = nodes[index];
            if (node is PrismBackdropDefinition)
            {
                backdropCount++;
                if (index != nodes.Length - 1)
                {
                    throw new ArgumentException("A Prism backdrop must be the last direct composition child.", nameof(nodes));
                }
            }

            ValidateScope(node, prefix: null, ids, names);
        }

        if (backdropCount > 1)
        {
            throw new ArgumentException("A Prism composition can contain at most one backdrop.", nameof(nodes));
        }

        return names.ToImmutable();
    }

    private static void ValidateScope(
        PrismNodeDefinition node,
        string? prefix,
        HashSet<PrismNodeId> ids,
        ImmutableDictionary<string, PrismNodeId>.Builder names)
    {
        if (!ids.Add(node.Id))
        {
            throw new ArgumentException($"Prism node identifier '{node.Id.Value}' is duplicated.");
        }

        string? path = null;
        if (node.Name is not null)
        {
            path = prefix is null ? node.Name : $"{prefix}.{node.Name}";
            if (!names.TryAdd(path, node.Id))
            {
                throw new ArgumentException($"Prism node name '{node.Name}' is duplicated in address scope '{prefix ?? "<root>"}'.");
            }
        }

        if (node is not PrismGroupDefinition group)
        {
            return;
        }

        HashSet<string> siblingNames = new(StringComparer.Ordinal);
        foreach (PrismNodeDefinition child in group.Children)
        {
            if (child.Name is not null && !siblingNames.Add(child.Name))
            {
                throw new ArgumentException($"Prism node name '{child.Name}' is duplicated in address scope '{path ?? "<unnamed-group>"}'.");
            }

            ValidateScope(child, path ?? prefix, ids, names);
        }
    }
}
