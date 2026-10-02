using System.Collections.Immutable;
using System.Numerics;
using Cerneala.Drawing.Prism.Catalog;
using Cerneala.Drawing.Prism.Filters;
using Cerneala.Drawing.Prism.Styles;
using Cerneala.UI.Prism.Definitions;
using Cerneala.UI.Prism.Runtime;

namespace Cerneala.Drawing.Prism.Graph;

[Flags]
internal enum PrismGraphUncacheableReason
{
    None = 0,
    NonDeterministicOperation = 1 << 0,
    CatalogDisallowsCaching = 1 << 1,
    ResourceVersionUnavailable = 1 << 2,
    FrameBackdrop = 1 << 3,
    MissingRequiredDependency = 1 << 4,
    UncacheableInput = 1 << 5
}

internal enum PrismGraphBoundsStatus
{
    Unknown,
    Exact,
    Conservative
}

internal readonly record struct PrismGraphNodePlan
{
    internal PrismGraphNodePlan(
        PrismGraphNodeId nodeId,
        DrawRect bounds,
        PrismGraphBoundsStatus boundsStatus,
        ImmutableArray<PrismGraphDependency> cacheDependencies,
        PrismGraphUncacheableReason uncacheableReasons,
        PrismRetainedCacheCandidateKind cacheCandidateKind,
        PrismVerifiedFingerprint structuralFingerprint,
        PrismVerifiedFingerprint valueFingerprint,
        PrismVerifiedFingerprint dependencyFingerprint)
    {
        if (boundsStatus is not (PrismGraphBoundsStatus.Unknown or
            PrismGraphBoundsStatus.Exact or PrismGraphBoundsStatus.Conservative))
        {
            throw new ArgumentOutOfRangeException(
                nameof(boundsStatus),
                boundsStatus,
                "Unknown Prism graph bounds status.");
        }

        NodeId = nodeId;
        Bounds = bounds;
        BoundsStatus = boundsStatus;
        CacheDependencies = cacheDependencies.IsDefault
            ? ImmutableArray<PrismGraphDependency>.Empty
            : cacheDependencies;
        UncacheableReasons = uncacheableReasons;
        if (cacheCandidateKind !=
                PrismRetainedCacheCandidateKind.None &&
            (uncacheableReasons !=
                PrismGraphUncacheableReason.None ||
             !structuralFingerprint.IsInitialized ||
             !valueFingerprint.IsInitialized ||
             !dependencyFingerprint.IsInitialized))
        {
            throw new ArgumentException(
                "A retained cache candidate requires complete verified fingerprints and no uncacheable reason.",
                nameof(cacheCandidateKind));
        }
        CacheCandidateKind = cacheCandidateKind;
        StructuralFingerprint = structuralFingerprint;
        ValueFingerprint = valueFingerprint;
        DependencyFingerprint = dependencyFingerprint;
    }

    public PrismGraphNodeId NodeId { get; }

    public DrawRect Bounds { get; }

    public PrismGraphBoundsStatus BoundsStatus { get; }

    public ImmutableArray<PrismGraphDependency> CacheDependencies { get; }

    public PrismGraphUncacheableReason UncacheableReasons { get; }

    internal PrismRetainedCacheCandidateKind CacheCandidateKind { get; }

    internal PrismVerifiedFingerprint StructuralFingerprint { get; }

    internal PrismVerifiedFingerprint ValueFingerprint { get; }

    internal PrismVerifiedFingerprint DependencyFingerprint { get; }

    public bool IsCacheable =>
        NodeId.ScopeOwnerToken.Value > 0 &&
        UncacheableReasons == PrismGraphUncacheableReason.None;
}

internal readonly record struct PrismGraphSurfaceLifetime
{
    internal PrismGraphSurfaceLifetime(
        PrismGraphNodeId nodeId,
        int firstStep,
        int lastStep)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(firstStep);
        if (lastStep < firstStep)
        {
            throw new ArgumentOutOfRangeException(
                nameof(lastStep),
                lastStep,
                "A Prism surface cannot be released before it becomes live.");
        }

        NodeId = nodeId;
        FirstStep = firstStep;
        LastStep = lastStep;
    }

    public PrismGraphNodeId NodeId { get; }

    public int FirstStep { get; }

    public int LastStep { get; }
}

internal sealed class PrismGraphExecutionPlan
{
    private readonly ImmutableDictionary<PrismGraphNodeId, int> executionIndicesById;

    internal PrismGraphExecutionPlan(
        PrismGraph optimizedGraph,
        ImmutableArray<PrismGraphNodeId> executionOrder,
        ImmutableArray<PrismGraphNodePlan> nodePlans,
        ImmutableArray<PrismGraphSurfaceLifetime> surfaceLifetimes,
        ImmutableArray<PrismGraphNodeId> removedNodeIds,
        int peakLiveSurfaces)
    {
        ArgumentNullException.ThrowIfNull(optimizedGraph);
        if (executionOrder.IsDefault ||
            nodePlans.IsDefault ||
            surfaceLifetimes.IsDefault ||
            removedNodeIds.IsDefault)
        {
            throw new ArgumentException(
                "A Prism graph execution plan requires initialized immutable arrays.");
        }
        if (peakLiveSurfaces < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(peakLiveSurfaces),
                peakLiveSurfaces,
                "Peak live surfaces cannot be negative.");
        }
        if (executionOrder.Length != optimizedGraph.Nodes.Length ||
            nodePlans.Length != executionOrder.Length ||
            surfaceLifetimes.Length != executionOrder.Length)
        {
            throw new ArgumentException(
                "Execution order, node plans, lifetimes, and optimized graph nodes must have matching lengths.");
        }

        HashSet<PrismGraphNodeId> graphNodeIds =
            optimizedGraph.Nodes.Select(node => node.Id).ToHashSet();
        if (graphNodeIds.Count != optimizedGraph.Nodes.Length ||
            executionOrder.Distinct().Count() != executionOrder.Length)
        {
            throw new ArgumentException(
                "An optimized Prism graph execution plan cannot contain duplicate node identifiers.");
        }
        Dictionary<PrismGraphNodeId, int> executionIndices =
            executionOrder
                .Select((nodeId, index) => KeyValuePair.Create(nodeId, index))
                .ToDictionary();
        for (int index = 0; index < executionOrder.Length; index++)
        {
            PrismGraphNodeId nodeId = executionOrder[index];
            if (!graphNodeIds.Contains(nodeId) ||
                nodePlans[index].NodeId != nodeId ||
                surfaceLifetimes[index].NodeId != nodeId ||
                surfaceLifetimes[index].FirstStep > index ||
                surfaceLifetimes[index].LastStep < index)
            {
                throw new ArgumentException(
                    "A Prism graph execution plan must map each ordered node to one compatible plan and lifetime.");
            }
        }
        foreach (PrismGraphEdge edge in optimizedGraph.Edges)
        {
            if (executionIndices[edge.Source] >= executionIndices[edge.Target])
            {
                throw new ArgumentException(
                    "A Prism graph execution order must be topological.");
            }
        }
        if (removedNodeIds.Distinct().Count() != removedNodeIds.Length ||
            removedNodeIds.Any(graphNodeIds.Contains))
        {
            throw new ArgumentException(
                "Removed Prism graph node identifiers must be unique and disjoint from the optimized graph.");
        }

        OptimizedGraph = optimizedGraph;
        ExecutionOrder = executionOrder;
        NodePlans = nodePlans;
        SurfaceLifetimes = surfaceLifetimes;
        RemovedNodeIds = removedNodeIds;
        PeakLiveSurfaces = peakLiveSurfaces;
        executionIndicesById = executionIndices.ToImmutableDictionary();
        CacheInputExecutionIndices = BuildCacheInputExecutionIndices(
            optimizedGraph,
            executionIndices);
        RootOutputExecutionIndices = optimizedGraph.Scopes
            .Where(scope =>
                scope.Depth == 0 &&
                scope.Output.HasValue)
            .OrderBy(scope => scope.BeginCommandIndex)
            .Select(scope => executionIndices[scope.Output!.Value])
            .Distinct()
            .ToImmutableArray();
    }

    internal PrismGraphExecutionPlan(
        PrismGraph optimizedGraph,
        ImmutableArray<PrismGraphNodePlan> nodePlans,
        PrismGraphExecutionPlan reusableTopology)
    {
        ArgumentNullException.ThrowIfNull(optimizedGraph);
        ArgumentNullException.ThrowIfNull(reusableTopology);
        if (nodePlans.IsDefault ||
            optimizedGraph.Nodes.Length != reusableTopology.ExecutionOrder.Length ||
            nodePlans.Length != reusableTopology.ExecutionOrder.Length ||
            !optimizedGraph.Edges.AsSpan().SequenceEqual(
                reusableTopology.OptimizedGraph.Edges.AsSpan()))
        {
            throw new ArgumentException(
                "A reused Prism execution topology requires matching nodes, edges, and node plans.");
        }
        for (int index = 0; index < nodePlans.Length; index++)
        {
            PrismGraphNodeId expected = reusableTopology.ExecutionOrder[index];
            if (optimizedGraph.Nodes[index].Id != expected ||
                nodePlans[index].NodeId != expected)
            {
                throw new ArgumentException(
                    "A reused Prism execution topology must preserve execution order.");
            }
        }

        OptimizedGraph = optimizedGraph;
        ExecutionOrder = reusableTopology.ExecutionOrder;
        NodePlans = nodePlans;
        SurfaceLifetimes = reusableTopology.SurfaceLifetimes;
        RemovedNodeIds = reusableTopology.RemovedNodeIds;
        PeakLiveSurfaces = reusableTopology.PeakLiveSurfaces;
        executionIndicesById = reusableTopology.executionIndicesById;
        CacheInputExecutionIndices = reusableTopology.CacheInputExecutionIndices;
        RootOutputExecutionIndices = reusableTopology.RootOutputExecutionIndices;
    }

    public PrismGraph OptimizedGraph { get; }

    public ImmutableArray<PrismGraphNodeId> ExecutionOrder { get; }

    public ImmutableArray<PrismGraphNodePlan> NodePlans { get; }

    public ImmutableArray<PrismGraphSurfaceLifetime> SurfaceLifetimes { get; }

    public ImmutableArray<PrismGraphNodeId> RemovedNodeIds { get; }

    public int PeakLiveSurfaces { get; }

    internal ImmutableArray<ImmutableArray<int>>
        CacheInputExecutionIndices { get; }

    internal ImmutableArray<int> RootOutputExecutionIndices { get; }

    public PrismGraphNodePlan GetNodePlan(PrismGraphNodeId nodeId)
    {
        return executionIndicesById.TryGetValue(nodeId, out int index)
            ? NodePlans[index]
            : throw new KeyNotFoundException(
                $"Prism graph execution node '{nodeId}' does not exist.");
    }

    internal int GetExecutionIndex(PrismGraphNodeId nodeId)
    {
        return executionIndicesById.TryGetValue(nodeId, out int index)
            ? index
            : throw new KeyNotFoundException(
                $"Prism graph execution node '{nodeId}' does not exist.");
    }

    private static ImmutableArray<ImmutableArray<int>>
        BuildCacheInputExecutionIndices(
            PrismGraph graph,
            IReadOnlyDictionary<PrismGraphNodeId, int> executionIndices)
    {
        List<int>[] inputs = new List<int>[graph.Nodes.Length];
        for (int index = 0; index < inputs.Length; index++)
        {
            inputs[index] = [];
        }

        foreach (PrismGraphEdge edge in graph.Edges)
        {
            inputs[executionIndices[edge.Target]].Add(
                executionIndices[edge.Source]);
        }

        foreach (PrismGraphNode node in graph.Nodes)
        {
            if (node.Kind != PrismGraphNodeKind.ControlCapture)
            {
                continue;
            }

            int captureIndex = executionIndices[node.Id];
            foreach (PrismGraphScope child in graph.Scopes)
            {
                if (child.ParentScopeIndex ==
                        node.AnalysisScopeIndex &&
                    child.Output is PrismGraphNodeId output)
                {
                    inputs[captureIndex].Add(
                        executionIndices[output]);
                }
            }
        }

        ImmutableArray<ImmutableArray<int>> result = inputs
            .Select((nodeInputs, targetIndex) =>
            {
                ImmutableArray<int> ordered = nodeInputs
                    .Distinct()
                    .Order()
                    .ToImmutableArray();
                if (ordered.Any(sourceIndex =>
                        sourceIndex >= targetIndex))
                {
                    throw new ArgumentException(
                        "Prism cache-pruning inputs must precede their consuming node.");
                }

                return ordered;
            })
            .ToImmutableArray();
        return result;
    }
}
