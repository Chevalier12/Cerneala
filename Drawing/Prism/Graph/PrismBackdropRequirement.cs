using System.Collections.Immutable;

namespace Cerneala.Drawing.Prism.Graph;

internal sealed class PrismBackdropRequirement
{
    internal PrismBackdropRequirement(ImmutableArray<int> scopeIndices)
    {
        ScopeIndices = scopeIndices;
    }

    public ImmutableArray<int> ScopeIndices { get; }

    public int ScopeCount => ScopeIndices.Length;
}
