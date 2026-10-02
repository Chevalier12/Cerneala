using System.Collections.Immutable;
using System.Numerics;
using Cerneala.Drawing.Prism.Catalog;
using Cerneala.Drawing.Prism.Filters;
using Cerneala.Drawing.Prism.Styles;
using Cerneala.UI.Prism.Definitions;
using Cerneala.UI.Prism.Runtime;

namespace Cerneala.Drawing.Prism.Graph;

internal sealed partial class PrismGraphOptimizer
{
    private static void ValidateScopeHierarchy(
        ImmutableArray<PrismGraphScope> scopes)
    {
        Stack<PrismGraphScope> active = [];
        foreach (PrismGraphScope scope in scopes
            .OrderBy(scope => scope.BeginCommandIndex)
            .ThenByDescending(scope => scope.EndCommandIndex)
            .ThenBy(scope => scope.AnalysisScopeIndex))
        {
            while (active.TryPeek(out PrismGraphScope candidate) &&
                scope.BeginCommandIndex >= candidate.EndCommandIndex)
            {
                active.Pop();
            }

            if (active.TryPeek(out PrismGraphScope parent))
            {
                if (scope.EndCommandIndex >= parent.EndCommandIndex ||
                    scope.ParentScopeIndex != parent.AnalysisScopeIndex ||
                    scope.Depth != parent.Depth + 1)
                {
                    throw new InvalidOperationException(
                        $"Prism graph scope '{scope.AnalysisScopeIndex}' has invalid nested command metadata.");
                }
            }
            else if (scope.ParentScopeIndex is not null || scope.Depth != 0)
            {
                throw new InvalidOperationException(
                    $"Prism graph scope '{scope.AnalysisScopeIndex}' has invalid root command metadata.");
            }

            active.Push(scope);
        }
    }

    private static HashSet<PrismGraphNodeId> FindReachableNodes(
        ImmutableArray<PrismGraphScope> scopes,
        ImmutableArray<PrismGraphEdge> edges)
    {
        Dictionary<PrismGraphNodeId, ImmutableArray<PrismGraphEdge>> incoming =
            IndexIncomingEdges(edges);
        HashSet<PrismGraphNodeId> reachable = [];
        Stack<PrismGraphNodeId> pending = new(
            scopes
                .Where(scope => scope.Output.HasValue)
                .Select(scope => scope.Output!.Value)
                .OrderByDescending(id => id, PrismGraphNodeIdComparer.Instance));
        while (pending.TryPop(out PrismGraphNodeId current))
        {
            if (!reachable.Add(current) ||
                !incoming.TryGetValue(current, out ImmutableArray<PrismGraphEdge> inputs))
            {
                continue;
            }

            foreach (PrismGraphEdge input in inputs
                .OrderByDescending(edge => edge.Kind)
                .ThenByDescending(edge => edge.Source, PrismGraphNodeIdComparer.Instance))
            {
                pending.Push(input.Source);
            }
        }

        return reachable;
    }

    private static ImmutableArray<PrismGraphNode> SortTopologically(
        IReadOnlyDictionary<PrismGraphNodeId, PrismGraphNode> nodes,
        ImmutableArray<PrismGraphScope> scopes,
        IReadOnlySet<PrismGraphNodeId> reachable,
        ImmutableArray<PrismGraphEdge> edges)
    {
        Dictionary<PrismGraphNodeId, int> indegrees = reachable
            .ToDictionary(id => id, _ => 0);
        Dictionary<PrismGraphNodeId, List<PrismGraphNodeId>> outgoing = [];
        foreach (PrismGraphEdge edge in edges)
        {
            if (!reachable.Contains(edge.Source) || !reachable.Contains(edge.Target))
            {
                continue;
            }

            indegrees[edge.Target]++;
            if (!outgoing.TryGetValue(edge.Source, out List<PrismGraphNodeId>? targets))
            {
                targets = [];
                outgoing.Add(edge.Source, targets);
            }
            targets.Add(edge.Target);
        }

        NodeExecutionComparer comparer = new(
            nodes,
            scopes.ToDictionary(scope => scope.AnalysisScopeIndex));
        SortedSet<PrismGraphNodeId> ready = new(comparer);
        foreach ((PrismGraphNodeId nodeId, int indegree) in indegrees)
        {
            if (indegree == 0)
            {
                ready.Add(nodeId);
            }
        }

        ImmutableArray<PrismGraphNode>.Builder result =
            ImmutableArray.CreateBuilder<PrismGraphNode>(reachable.Count);
        while (ready.Count > 0)
        {
            PrismGraphNodeId nodeId = ready.Min;
            ready.Remove(nodeId);
            result.Add(nodes[nodeId]);
            if (!outgoing.TryGetValue(nodeId, out List<PrismGraphNodeId>? targets))
            {
                continue;
            }

            foreach (PrismGraphNodeId target in targets
                .OrderBy(id => id, comparer))
            {
                indegrees[target]--;
                if (indegrees[target] == 0)
                {
                    ready.Add(target);
                }
            }
        }

        if (result.Count != reachable.Count)
        {
            throw new InvalidOperationException(
                "The Prism graph contains a cycle and cannot be optimized.");
        }

        return result.MoveToImmutable();
    }

    private static ImmutableArray<PrismGraphEdge> SortExecutionEdges(
        ImmutableArray<PrismGraphEdge> edges,
        IReadOnlySet<PrismGraphNodeId> reachable,
        IReadOnlyDictionary<PrismGraphNodeId, int> executionIndices)
    {
        return edges
            .Where(edge => reachable.Contains(edge.Source) && reachable.Contains(edge.Target))
            .OrderBy(edge => executionIndices[edge.Target])
            .ThenBy(edge => edge.Kind)
            .ThenBy(edge => executionIndices[edge.Source])
            .ToImmutableArray();
    }

    internal static ImmutableArray<PrismGraphSurfaceLifetime> BuildSurfaceLifetimes(
        PrismGraph graph,
        IReadOnlyDictionary<PrismGraphNodeId, int> executionIndices)
    {
        Dictionary<int, int> firstScopeSteps = graph.Nodes
            .GroupBy(node => node.AnalysisScopeIndex)
            .ToDictionary(
                group => group.Key,
                group => group.Min(node => executionIndices[node.Id]));
        Dictionary<int, PrismGraphScope> scopes = graph.Scopes
            .ToDictionary(scope => scope.AnalysisScopeIndex);
        Dictionary<PrismGraphNodeId, int> firstUses = graph.Nodes
            .ToDictionary(node => node.Id, node => executionIndices[node.Id]);
        foreach (PrismGraphNode capture in graph.Nodes.Where(
            node => node.Kind == PrismGraphNodeKind.ControlCapture))
        {
            PrismGraphScope captureScope = scopes[capture.AnalysisScopeIndex];
            foreach ((int candidateScopeIndex, int firstStep) in firstScopeSteps)
            {
                PrismGraphScope candidate = scopes[candidateScopeIndex];
                if (captureScope.BeginCommandIndex < candidate.BeginCommandIndex &&
                    candidate.EndCommandIndex < captureScope.EndCommandIndex)
                {
                    firstUses[capture.Id] = Math.Min(
                        firstUses[capture.Id],
                        firstStep);
                }
            }
        }

        Dictionary<PrismGraphNodeId, int> lastUses = graph.Nodes
            .ToDictionary(node => node.Id, node => executionIndices[node.Id]);
        foreach (PrismGraphEdge edge in graph.Edges)
        {
            lastUses[edge.Source] = Math.Max(
                lastUses[edge.Source],
                executionIndices[edge.Target]);
        }
        Dictionary<int, int> captureSteps = graph.Nodes
            .Where(node => node.Kind == PrismGraphNodeKind.ControlCapture)
            .ToDictionary(node => node.AnalysisScopeIndex, node => executionIndices[node.Id]);
        foreach (PrismGraphScope scope in graph.Scopes)
        {
            if (scope.Output is not PrismGraphNodeId output)
            {
                continue;
            }

            int finalScopeStep = graph.Nodes
                .Where(node => node.AnalysisScopeIndex == scope.AnalysisScopeIndex)
                .Select(node => executionIndices[node.Id])
                .DefaultIfEmpty(executionIndices[output])
                .Max();
            lastUses[output] = Math.Max(lastUses[output], finalScopeStep);
            if (scope.ParentScopeIndex is int parentScopeIndex)
            {
                // Nested presentation is an implicit pixel dependency: the
                // parent capture consumes this output after the child finishes.
                // Every executor must receive the same complete lifetime.
                if (!captureSteps.TryGetValue(parentScopeIndex, out int captureStep))
                {
                    throw new InvalidOperationException(
                        $"Prism scope '{parentScopeIndex}' has no control-capture node.");
                }
                lastUses[output] = Math.Max(lastUses[output], captureStep);
            }
        }

        return graph.Nodes
            .Select(
                node => new PrismGraphSurfaceLifetime(
                    node.Id,
                    firstUses[node.Id],
                    lastUses[node.Id]))
            .ToImmutableArray();
    }

    internal static int CalculatePeakLiveSurfaces(
        ImmutableArray<PrismGraphSurfaceLifetime> lifetimes)
    {
        if (lifetimes.IsEmpty)
        {
            return 0;
        }

        int peak = 0;
        int firstStep = lifetimes.Min(lifetime => lifetime.FirstStep);
        int lastStep = lifetimes.Max(lifetime => lifetime.LastStep);
        for (int step = firstStep; step <= lastStep; step++)
        {
            int live = lifetimes.Count(
                lifetime => lifetime.FirstStep <= step &&
                    lifetime.LastStep >= step);
            peak = Math.Max(peak, live);
        }
        return peak;
    }

    private static Dictionary<PrismGraphNodeId, ImmutableArray<PrismGraphEdge>>
        IndexIncomingEdges(IEnumerable<PrismGraphEdge> edges)
    {
        return edges
            .GroupBy(edge => edge.Target)
            .ToDictionary(
                group => group.Key,
                group => group
                    .OrderBy(edge => edge.Kind)
                    .ThenBy(edge => edge.Source, PrismGraphNodeIdComparer.Instance)
                    .ToImmutableArray());
    }

    private sealed class NodeExecutionComparer : IComparer<PrismGraphNodeId>
    {
        private readonly IReadOnlyDictionary<PrismGraphNodeId, PrismGraphNode> nodes;
        private readonly IReadOnlyDictionary<int, PrismGraphScope> scopes;

        public NodeExecutionComparer(
            IReadOnlyDictionary<PrismGraphNodeId, PrismGraphNode> nodes,
            IReadOnlyDictionary<int, PrismGraphScope> scopes)
        {
            this.nodes = nodes;
            this.scopes = scopes;
        }

        public int Compare(PrismGraphNodeId left, PrismGraphNodeId right)
        {
            if (left == right)
            {
                return 0;
            }

            PrismGraphNode leftNode = nodes[left];
            PrismGraphNode rightNode = nodes[right];
            PrismGraphScope leftScope = scopes[leftNode.AnalysisScopeIndex];
            PrismGraphScope rightScope = scopes[rightNode.AnalysisScopeIndex];
            int result = leftScope.EndCommandIndex.CompareTo(
                rightScope.EndCommandIndex);
            if (result == 0)
            {
                result = rightScope.Depth.CompareTo(leftScope.Depth);
            }
            if (result == 0)
            {
                result = rightScope.BeginCommandIndex.CompareTo(
                    leftScope.BeginCommandIndex);
            }
            if (result == 0)
            {
                result = leftScope.AnalysisScopeIndex.CompareTo(
                    rightScope.AnalysisScopeIndex);
            }
            return result != 0
                ? result
                : PrismGraphNodeIdComparer.Instance.Compare(left, right);
        }
    }

    private sealed class PrismGraphNodeIdComparer : IComparer<PrismGraphNodeId>
    {
        public static PrismGraphNodeIdComparer Instance { get; } = new();

        public int Compare(PrismGraphNodeId left, PrismGraphNodeId right)
        {
            int result = left.ScopeOwnerToken.Value.CompareTo(
                right.ScopeOwnerToken.Value);
            if (result != 0)
            {
                return result;
            }

            result = left.DefinitionNodeId.CompareTo(right.DefinitionNodeId);
            if (result != 0)
            {
                return result;
            }

            result = left.Kind.CompareTo(right.Kind);
            return result != 0
                ? result
                : left.Ordinal.CompareTo(right.Ordinal);
        }
    }
}
