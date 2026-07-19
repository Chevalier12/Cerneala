using System.Collections.Immutable;
using System.Numerics;
using Cerneala.Drawing.Prism.Catalog;
using Cerneala.UI.Prism.Definitions;
using Cerneala.UI.Prism.Runtime;

namespace Cerneala.Drawing.Prism.Graph;

[Flags]
public enum PrismGraphUncacheableReason
{
    None = 0,
    NonDeterministicOperation = 1 << 0,
    CatalogDisallowsCaching = 1 << 1,
    ResourceVersionUnavailable = 1 << 2,
    FrameBackdrop = 1 << 3,
    MissingRequiredDependency = 1 << 4,
    UncacheableInput = 1 << 5
}

public enum PrismGraphBoundsStatus
{
    Unknown,
    Exact,
    Conservative
}

public readonly record struct PrismGraphNodePlan
{
    internal PrismGraphNodePlan(
        PrismGraphNodeId nodeId,
        DrawRect bounds,
        PrismGraphBoundsStatus boundsStatus,
        ImmutableArray<PrismGraphDependency> cacheDependencies,
        PrismGraphUncacheableReason uncacheableReasons)
    {
        if (!Enum.IsDefined(boundsStatus))
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
    }

    public PrismGraphNodeId NodeId { get; }

    public DrawRect Bounds { get; }

    public PrismGraphBoundsStatus BoundsStatus { get; }

    public ImmutableArray<PrismGraphDependency> CacheDependencies { get; }

    public PrismGraphUncacheableReason UncacheableReasons { get; }

    public bool IsCacheable =>
        NodeId.ScopeOwnerToken.Value > 0 &&
        UncacheableReasons == PrismGraphUncacheableReason.None;
}

public readonly record struct PrismGraphSurfaceLifetime
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

public sealed class PrismGraphExecutionPlan
{
    private readonly ImmutableDictionary<PrismGraphNodeId, PrismGraphNodePlan> nodesById;

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
        nodesById = nodePlans.ToImmutableDictionary(node => node.NodeId);
    }

    public PrismGraph OptimizedGraph { get; }

    public ImmutableArray<PrismGraphNodeId> ExecutionOrder { get; }

    public ImmutableArray<PrismGraphNodePlan> NodePlans { get; }

    public ImmutableArray<PrismGraphSurfaceLifetime> SurfaceLifetimes { get; }

    public ImmutableArray<PrismGraphNodeId> RemovedNodeIds { get; }

    public int PeakLiveSurfaces { get; }

    public PrismGraphNodePlan GetNodePlan(PrismGraphNodeId nodeId)
    {
        return nodesById.TryGetValue(nodeId, out PrismGraphNodePlan node)
            ? node
            : throw new KeyNotFoundException(
                $"Prism graph execution node '{nodeId}' does not exist.");
    }
}

public sealed class PrismGraphOptimizer
{
    public PrismGraphExecutionPlan Optimize(PrismGraph graph)
    {
        ArgumentNullException.ThrowIfNull(graph);

        Dictionary<PrismGraphNodeId, PrismGraphNode> originalNodes =
            graph.Nodes.ToDictionary(node => node.Id);
        Dictionary<PrismGraphNodeId, PrismGraphNodeId> aliases =
            FindProvenAliases(graph, originalNodes);
        ImmutableDictionary<
            PrismGraphNodeId,
            ImmutableArray<PrismGraphDependency>> aliasedDependencies =
            CollectAliasedDependencies(aliases, originalNodes);
        ImmutableArray<PrismGraphScope> scopes =
            RewriteScopes(graph.Scopes, aliases);
        ValidateScopeHierarchy(scopes);
        ImmutableArray<PrismGraphEdge> rewrittenEdges =
            RewriteEdges(graph.Edges, aliases);
        HashSet<PrismGraphNodeId> reachable =
            FindReachableNodes(scopes, rewrittenEdges);
        ImmutableArray<PrismGraphNode> executionNodes =
            SortTopologically(originalNodes, scopes, reachable, rewrittenEdges);
        ImmutableDictionary<PrismGraphNodeId, int> executionIndices =
            executionNodes
                .Select((node, index) => KeyValuePair.Create(node.Id, index))
                .ToImmutableDictionary();
        ImmutableArray<PrismGraphEdge> executionEdges =
            SortExecutionEdges(rewrittenEdges, reachable, executionIndices);
        PrismGraph optimizedGraph = new(executionNodes, executionEdges, scopes);
        ImmutableArray<PrismGraphNodePlan> nodePlans =
            BuildNodePlans(optimizedGraph, aliasedDependencies);
        ImmutableArray<PrismGraphSurfaceLifetime> lifetimes =
            BuildSurfaceLifetimes(optimizedGraph, executionIndices);
        int peakLiveSurfaces = CalculatePeakLiveSurfaces(lifetimes);
        ImmutableArray<PrismGraphNodeId> removedNodes =
            originalNodes.Keys
                .Where(id => !reachable.Contains(id))
                .OrderBy(id => id, PrismGraphNodeIdComparer.Instance)
                .ToImmutableArray();

        return new PrismGraphExecutionPlan(
            optimizedGraph,
            executionNodes.Select(node => node.Id).ToImmutableArray(),
            nodePlans,
            lifetimes,
            removedNodes,
            peakLiveSurfaces);
    }

    private static ImmutableDictionary<
        PrismGraphNodeId,
        ImmutableArray<PrismGraphDependency>> CollectAliasedDependencies(
            IReadOnlyDictionary<PrismGraphNodeId, PrismGraphNodeId> aliases,
            IReadOnlyDictionary<PrismGraphNodeId, PrismGraphNode> nodes)
    {
        Dictionary<PrismGraphNodeId, HashSet<PrismGraphDependency>> collected = [];
        foreach (PrismGraphNodeId alias in aliases.Keys)
        {
            PrismGraphNodeId target = ResolveAlias(alias, aliases);
            if (!collected.TryGetValue(
                    target,
                    out HashSet<PrismGraphDependency>? dependencies))
            {
                dependencies = [];
                collected.Add(target, dependencies);
            }
            dependencies.UnionWith(nodes[alias].Dependencies);
        }

        return collected.ToImmutableDictionary(
            pair => pair.Key,
            pair => pair.Value
                .OrderBy(dependency => dependency.Kind)
                .ThenBy(dependency => dependency.Key)
                .ThenBy(dependency => dependency.Version)
                .ToImmutableArray());
    }

    private static Dictionary<PrismGraphNodeId, PrismGraphNodeId> FindProvenAliases(
        PrismGraph graph,
        IReadOnlyDictionary<PrismGraphNodeId, PrismGraphNode> nodes)
    {
        Dictionary<PrismGraphNodeId, ImmutableArray<PrismGraphEdge>> incoming =
            IndexIncomingEdges(graph.Edges);
        Dictionary<PrismGraphNodeId, PrismGraphNodeId> aliases = [];
        PrismGraphNode[] orderedNodes = graph.Nodes
            .OrderBy(node => node.Id, PrismGraphNodeIdComparer.Instance)
            .ToArray();
        bool changed;
        do
        {
            changed = false;
            foreach (PrismGraphNode node in orderedNodes)
            {
                if (aliases.ContainsKey(node.Id) ||
                    !TryGetAliasSource(node, incoming, aliases, nodes, out PrismGraphNodeId source))
                {
                    continue;
                }

                aliases.Add(node.Id, source);
                changed = true;
            }
        }
        while (changed);

        return aliases;
    }

    private static bool TryGetAliasSource(
        PrismGraphNode node,
        IReadOnlyDictionary<PrismGraphNodeId, ImmutableArray<PrismGraphEdge>> incoming,
        IReadOnlyDictionary<PrismGraphNodeId, PrismGraphNodeId> aliases,
        IReadOnlyDictionary<PrismGraphNodeId, PrismGraphNode> nodes,
        out PrismGraphNodeId source)
    {
        source = default;
        if (!incoming.TryGetValue(node.Id, out ImmutableArray<PrismGraphEdge> nodeInputs))
        {
            return false;
        }

        PrismGraphEdge[] pixelInputs = nodeInputs
            .Where(edge => edge.Kind is PrismGraphEdgeKind.Content or PrismGraphEdgeKind.Backdrop)
            .ToArray();
        if (pixelInputs.Length != 1)
        {
            return false;
        }

        source = ResolveAlias(pixelInputs[0].Source, aliases);
        if (node.Kind is PrismGraphNodeKind.Fill or PrismGraphNodeKind.Opacity)
        {
            return node.Amount == 1f;
        }
        if (node.Kind == PrismGraphNodeKind.Filter &&
            IsProvenFilterNoOp(node))
        {
            return true;
        }

        if (node.Kind != PrismGraphNodeKind.ColorConversion ||
            node.ColorProfile is not PrismColorProfile profile ||
            !nodes.TryGetValue(source, out PrismGraphNode? sourceNode))
        {
            return false;
        }

        return sourceNode.Kind == PrismGraphNodeKind.ColorConversion &&
            sourceNode.ColorProfile == profile;
    }

    private static bool IsProvenFilterNoOp(PrismGraphNode node)
    {
        if (node.Amount != 1f ||
            node.BlendMode != PrismBlendMode.Normal)
        {
            return false;
        }

        return node.Filter switch
        {
            PrismFilterId.Blur =>
                GetNumber(node, BoundsParameters.BlurRadius) == 0f,
            PrismFilterId.BlurMore =>
                GetNumber(node, BoundsParameters.BlurMoreRadius) == 0f,
            PrismFilterId.GaussianBlur =>
                GetNumber(node, BoundsParameters.GaussianBlurRadius) == 0f,
            PrismFilterId.BoxBlur =>
                GetNumber(node, BoundsParameters.BoxBlurRadius) == 0f ||
                GetNumber(node, BoundsParameters.BoxBlurIterations) == 0f,
            PrismFilterId.Transform => IsIdentityTransform(node),
            _ => false
        };
    }

    private static bool IsIdentityTransform(PrismGraphNode node)
    {
        Vector4 translate = GetVector(node, BoundsParameters.TransformTranslate);
        Vector4 scale = GetVector(node, BoundsParameters.TransformScale);
        Vector4 skew = GetVector(node, BoundsParameters.TransformSkew);
        return translate.X == 0f &&
            translate.Y == 0f &&
            scale.X == 1f &&
            scale.Y == 1f &&
            GetNumber(node, BoundsParameters.TransformRotation) == 0f &&
            skew.X == 0f &&
            skew.Y == 0f;
    }

    private static ImmutableArray<PrismGraphScope> RewriteScopes(
        ImmutableArray<PrismGraphScope> scopes,
        IReadOnlyDictionary<PrismGraphNodeId, PrismGraphNodeId> aliases)
    {
        return scopes
            .OrderBy(scope => scope.AnalysisScopeIndex)
            .Select(
                scope => new PrismGraphScope(
                    scope.AnalysisScopeIndex,
                    scope.BeginCommandIndex,
                    scope.EndCommandIndex,
                    scope.Depth,
                    scope.ParentScopeIndex,
                    scope.CacheOwnerToken,
                    scope.CompositionSettings,
                    scope.Bounds,
                    scope.EffectiveTransform,
                    scope.PixelScale,
                    scope.Output is PrismGraphNodeId output
                        ? ResolveAlias(output, aliases)
                        : null))
            .ToImmutableArray();
    }

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

    private static ImmutableArray<PrismGraphEdge> RewriteEdges(
        ImmutableArray<PrismGraphEdge> edges,
        IReadOnlyDictionary<PrismGraphNodeId, PrismGraphNodeId> aliases)
    {
        HashSet<PrismGraphEdge> rewritten = [];
        foreach (PrismGraphEdge edge in edges)
        {
            PrismGraphNodeId source = ResolveAlias(edge.Source, aliases);
            PrismGraphNodeId target = ResolveAlias(edge.Target, aliases);
            if (source == target)
            {
                continue;
            }

            rewritten.Add(new PrismGraphEdge(source, target, edge.Kind));
        }

        return rewritten
            .OrderBy(edge => edge.Target, PrismGraphNodeIdComparer.Instance)
            .ThenBy(edge => edge.Kind)
            .ThenBy(edge => edge.Source, PrismGraphNodeIdComparer.Instance)
            .ToImmutableArray();
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

    private static ImmutableArray<PrismGraphNodePlan> BuildNodePlans(
        PrismGraph graph,
        IReadOnlyDictionary<
            PrismGraphNodeId,
            ImmutableArray<PrismGraphDependency>> aliasedDependencies)
    {
        Dictionary<int, PrismGraphScope> scopes = graph.Scopes
            .ToDictionary(scope => scope.AnalysisScopeIndex);
        Dictionary<PrismGraphNodeId, ImmutableArray<PrismGraphEdge>> incoming =
            IndexIncomingEdges(graph.Edges);
        Dictionary<PrismGraphNodeId, PrismGraphNodePlan> plans = [];
        ImmutableArray<PrismGraphNodePlan>.Builder result =
            ImmutableArray.CreateBuilder<PrismGraphNodePlan>(graph.Nodes.Length);
        foreach (PrismGraphNode node in graph.Nodes)
        {
            if (!scopes.TryGetValue(node.AnalysisScopeIndex, out PrismGraphScope scope))
            {
                throw new InvalidOperationException(
                    $"Prism graph node '{node.Id}' refers to an unknown analysis scope.");
            }

            ImmutableArray<PrismGraphEdge> inputs = incoming.TryGetValue(
                node.Id,
                out ImmutableArray<PrismGraphEdge> indexedInputs)
                ? indexedInputs
                : ImmutableArray<PrismGraphEdge>.Empty;
            BoundsCalculation bounds = CalculateBounds(node, scope, inputs, plans);
            ImmutableArray<PrismGraphDependency> dependencies =
                CollectDependencies(
                    node,
                    inputs,
                    plans,
                    aliasedDependencies);
            PrismGraphUncacheableReason reasons =
                CalculateUncacheableReasons(node, inputs, plans);
            PrismGraphNodePlan plan = new(
                node.Id,
                bounds.Bounds,
                bounds.Status,
                dependencies,
                reasons);
            plans.Add(node.Id, plan);
            result.Add(plan);
        }

        return result.MoveToImmutable();
    }

    private static ImmutableArray<PrismGraphDependency> CollectDependencies(
        PrismGraphNode node,
        ImmutableArray<PrismGraphEdge> inputs,
        IReadOnlyDictionary<PrismGraphNodeId, PrismGraphNodePlan> plans,
        IReadOnlyDictionary<
            PrismGraphNodeId,
            ImmutableArray<PrismGraphDependency>> aliasedDependencies)
    {
        HashSet<PrismGraphDependency> dependencies = [.. node.Dependencies];
        if (aliasedDependencies.TryGetValue(
                node.Id,
                out ImmutableArray<PrismGraphDependency> elided))
        {
            dependencies.UnionWith(elided);
        }
        foreach (PrismGraphEdge input in inputs)
        {
            dependencies.UnionWith(plans[input.Source].CacheDependencies);
        }

        return dependencies
            .OrderBy(dependency => dependency.Kind)
            .ThenBy(dependency => dependency.Key)
            .ThenBy(dependency => dependency.Version)
            .ToImmutableArray();
    }

    private static BoundsCalculation CalculateBounds(
        PrismGraphNode node,
        PrismGraphScope scope,
        ImmutableArray<PrismGraphEdge> inputs,
        IReadOnlyDictionary<PrismGraphNodeId, PrismGraphNodePlan> plans)
    {
        DrawRect bounds = scope.Bounds;
        PrismGraphBoundsStatus status = PrismGraphBoundsStatus.Exact;
        bool hasInput = false;
        foreach (PrismGraphEdge input in inputs)
        {
            if (!plans.TryGetValue(input.Source, out PrismGraphNodePlan sourcePlan))
            {
                throw new InvalidOperationException(
                    $"Prism graph node '{node.Id}' is not in topological order.");
            }

            bounds = hasInput
                ? Union(bounds, sourcePlan.Bounds)
                : sourcePlan.Bounds;
            status = WorstBoundsStatus(status, sourcePlan.BoundsStatus);
            hasInput = true;
        }

        return node.Kind switch
        {
            PrismGraphNodeKind.Filter => ExpandFilterBounds(node, bounds, status),
            PrismGraphNodeKind.Style => ExpandStyleBounds(node, scope, bounds, status),
            PrismGraphNodeKind.ClipToBelow or
            PrismGraphNodeKind.PassThroughComposite =>
                ConservativeBounds(bounds, status),
            PrismGraphNodeKind.Composite
                when inputs.Any(input => input.Kind == PrismGraphEdgeKind.MaskAlpha) =>
                ConservativeBounds(bounds, status),
            _ => new BoundsCalculation(bounds, status)
        };
    }

    private static BoundsCalculation ExpandFilterBounds(
        PrismGraphNode node,
        DrawRect bounds,
        PrismGraphBoundsStatus inputStatus)
    {
        return node.Filter switch
        {
            PrismFilterId.Blur => ConservativeBounds(
                Inflate(bounds, GetNumber(node, BoundsParameters.BlurRadius)),
                inputStatus),
            PrismFilterId.BlurMore => ConservativeBounds(
                Inflate(bounds, GetNumber(node, BoundsParameters.BlurMoreRadius)),
                inputStatus),
            PrismFilterId.GaussianBlur => ConservativeBounds(
                Inflate(bounds, GetNumber(node, BoundsParameters.GaussianBlurRadius)),
                inputStatus),
            PrismFilterId.BoxBlur => ConservativeBounds(
                Inflate(
                    bounds,
                    checked(
                        GetNumber(node, BoundsParameters.BoxBlurRadius) *
                        GetNumber(node, BoundsParameters.BoxBlurIterations))),
                inputStatus),
            PrismFilterId.Transform => ExpandTransformBounds(
                node,
                bounds,
                inputStatus),
            _ => new BoundsCalculation(
                bounds,
                PrismGraphBoundsStatus.Unknown)
        };
    }

    private static BoundsCalculation ExpandStyleBounds(
        PrismGraphNode node,
        PrismGraphScope scope,
        DrawRect bounds,
        PrismGraphBoundsStatus inputStatus)
    {
        return node.Style switch
        {
            PrismStyleId.DropShadow => ConservativeBounds(
                ExpandDropShadow(node, scope, bounds),
                inputStatus),
            PrismStyleId.Stroke => ConservativeBounds(
                Inflate(bounds, GetNumber(node, BoundsParameters.StrokeSize)),
                inputStatus),
            _ => new BoundsCalculation(
                bounds,
                PrismGraphBoundsStatus.Unknown)
        };
    }

    private static BoundsCalculation ConservativeBounds(
        DrawRect bounds,
        PrismGraphBoundsStatus inputStatus) =>
        new(
            bounds,
            WorstBoundsStatus(
                inputStatus,
                PrismGraphBoundsStatus.Conservative));

    private static PrismGraphBoundsStatus WorstBoundsStatus(
        PrismGraphBoundsStatus left,
        PrismGraphBoundsStatus right)
    {
        if (left == PrismGraphBoundsStatus.Unknown ||
            right == PrismGraphBoundsStatus.Unknown)
        {
            return PrismGraphBoundsStatus.Unknown;
        }
        return left == PrismGraphBoundsStatus.Conservative ||
            right == PrismGraphBoundsStatus.Conservative
            ? PrismGraphBoundsStatus.Conservative
            : PrismGraphBoundsStatus.Exact;
    }

    private static DrawRect ExpandDropShadow(
        PrismGraphNode node,
        PrismGraphScope scope,
        DrawRect bounds)
    {
        bool useGlobalLight = GetBoolean(
            node,
            BoundsParameters.DropShadowUseGlobalLight);
        float angle = useGlobalLight
            ? scope.CompositionSettings.GlobalLightAngle
            : GetNumber(node, BoundsParameters.DropShadowAngle);
        float distance = GetNumber(node, BoundsParameters.DropShadowDistance);
        float support =
            GetNumber(node, BoundsParameters.DropShadowSpread) +
            GetNumber(node, BoundsParameters.DropShadowSize);
        float radians = angle * (MathF.PI / 180f);
        float offsetX = MathF.Cos(radians) * distance;
        float offsetY = -MathF.Sin(radians) * distance;
        DrawRect shadow = Translate(Inflate(bounds, support), offsetX, offsetY);
        return Union(bounds, shadow);
    }

    private static DrawRect TransformBounds(
        PrismGraphNode node,
        DrawRect bounds)
    {
        Vector4 translate = GetVector(node, BoundsParameters.TransformTranslate);
        Vector4 scale = GetVector(node, BoundsParameters.TransformScale);
        float rotation = GetNumber(node, BoundsParameters.TransformRotation);
        Vector4 skew = GetVector(node, BoundsParameters.TransformSkew);
        Vector4 origin = GetVector(node, BoundsParameters.TransformOrigin);
        Vector2 pivot = new(
            bounds.X + (bounds.Width * origin.X),
            bounds.Y + (bounds.Height * origin.Y));
        Matrix3x2 skewMatrix = new(
            1f,
            skew.Y,
            skew.X,
            1f,
            0f,
            0f);
        Matrix3x2 transform =
            Matrix3x2.CreateTranslation(-pivot) *
            Matrix3x2.CreateScale(scale.X, scale.Y) *
            skewMatrix *
            Matrix3x2.CreateRotation(rotation * (MathF.PI / 180f)) *
            Matrix3x2.CreateTranslation(
                pivot + new Vector2(translate.X, translate.Y));
        return Transform(bounds, transform);
    }

    private static BoundsCalculation ExpandTransformBounds(
        PrismGraphNode node,
        DrawRect bounds,
        PrismGraphBoundsStatus inputStatus)
    {
        DrawRect transformed = TransformBounds(node, bounds);
        if (node.Amount == 1f &&
            node.BlendMode == PrismBlendMode.Normal)
        {
            return new BoundsCalculation(transformed, inputStatus);
        }

        return ConservativeBounds(Union(bounds, transformed), inputStatus);
    }

    private static PrismGraphUncacheableReason CalculateUncacheableReasons(
        PrismGraphNode node,
        ImmutableArray<PrismGraphEdge> inputs,
        IReadOnlyDictionary<PrismGraphNodeId, PrismGraphNodePlan> plans)
    {
        PrismGraphUncacheableReason reasons = OwnUncacheableReasons(node);
        foreach (PrismGraphEdge input in inputs)
        {
            PrismGraphNodePlan source = plans[input.Source];
            if (source.IsCacheable)
            {
                continue;
            }

            reasons |= source.UncacheableReasons |
                PrismGraphUncacheableReason.UncacheableInput;
        }
        return reasons;
    }

    private static PrismGraphUncacheableReason OwnUncacheableReasons(
        PrismGraphNode node)
    {
        PrismGraphUncacheableReason reasons = PrismGraphUncacheableReason.None;
        if (node.Kind == PrismGraphNodeKind.BackdropInput)
        {
            reasons |= PrismGraphUncacheableReason.FrameBackdrop;
        }

        if ((node.Resource is PrismResourceId resource && resource.Value > 0) ||
            node.Parameters.Any(
                parameter => parameter.Kind == PrismGraphParameterValueKind.Resource &&
                    parameter.ResourceValue.Value > 0))
        {
            reasons |= PrismGraphUncacheableReason.ResourceVersionUnavailable;
        }

        if (node.Filter is PrismFilterId filter)
        {
            reasons |= CatalogReasons((int)filter);
        }
        if (node.Style is PrismStyleId style)
        {
            reasons |= CatalogReasons((int)style);
        }
        if (!HasRequiredDependencies(node))
        {
            reasons |= PrismGraphUncacheableReason.MissingRequiredDependency;
        }

        return reasons;
    }

    private static PrismGraphUncacheableReason CatalogReasons(int stableId)
    {
        PrismCatalogEntryDescriptor entry = PrismCatalogRuntime.GetEntry(stableId);
        PrismGraphUncacheableReason reasons = PrismGraphUncacheableReason.None;
        if (!entry.Deterministic)
        {
            reasons |= PrismGraphUncacheableReason.NonDeterministicOperation;
        }
        if (!entry.Cacheable)
        {
            reasons |= PrismGraphUncacheableReason.CatalogDisallowsCaching;
        }
        return reasons;
    }

    private static bool HasRequiredDependencies(PrismGraphNode node)
    {
        bool Has(PrismGraphDependencyKind kind) =>
            node.Dependencies.Any(dependency => dependency.Kind == kind);

        if (!Has(PrismGraphDependencyKind.Structure) ||
            !Has(PrismGraphDependencyKind.Values) ||
            !Has(PrismGraphDependencyKind.Descendants))
        {
            return false;
        }

        return node.Kind switch
        {
            PrismGraphNodeKind.ControlCapture =>
                Has(PrismGraphDependencyKind.VisualContent) &&
                Has(PrismGraphDependencyKind.Bounds) &&
                Has(PrismGraphDependencyKind.PixelScale) &&
                Has(PrismGraphDependencyKind.Transform),
            PrismGraphNodeKind.ColorConversion =>
                Has(PrismGraphDependencyKind.ColorProfile),
            PrismGraphNodeKind.Filter or PrismGraphNodeKind.Style =>
                Has(PrismGraphDependencyKind.CatalogEntry),
            _ => true
        };
    }

    private static ImmutableArray<PrismGraphSurfaceLifetime> BuildSurfaceLifetimes(
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
        }

        return graph.Nodes
            .Select(
                node => new PrismGraphSurfaceLifetime(
                    node.Id,
                    firstUses[node.Id],
                    lastUses[node.Id]))
            .ToImmutableArray();
    }

    private static int CalculatePeakLiveSurfaces(
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

    private static PrismGraphNodeId ResolveAlias(
        PrismGraphNodeId nodeId,
        IReadOnlyDictionary<PrismGraphNodeId, PrismGraphNodeId> aliases)
    {
        HashSet<PrismGraphNodeId>? visited = null;
        while (aliases.TryGetValue(nodeId, out PrismGraphNodeId source))
        {
            visited ??= [];
            if (!visited.Add(nodeId))
            {
                throw new InvalidOperationException(
                    "The Prism graph optimizer produced an alias cycle.");
            }
            nodeId = source;
        }
        return nodeId;
    }

    private static float GetNumber(PrismGraphNode node, int parameterIndex)
    {
        PrismGraphParameter parameter = GetParameter(node, parameterIndex);
        if (parameter.Kind != PrismGraphParameterValueKind.Number)
        {
            throw InvalidParameter(node, parameterIndex);
        }
        return parameter.NumberValue;
    }

    private static bool GetBoolean(PrismGraphNode node, int parameterIndex)
    {
        PrismGraphParameter parameter = GetParameter(node, parameterIndex);
        if (parameter.Kind != PrismGraphParameterValueKind.Boolean)
        {
            throw InvalidParameter(node, parameterIndex);
        }
        return parameter.BooleanValue;
    }

    private static Vector4 GetVector(PrismGraphNode node, int parameterIndex)
    {
        PrismGraphParameter parameter = GetParameter(node, parameterIndex);
        if (parameter.Kind != PrismGraphParameterValueKind.Vector)
        {
            throw InvalidParameter(node, parameterIndex);
        }
        return parameter.VectorValue;
    }

    private static PrismGraphParameter GetParameter(
        PrismGraphNode node,
        int parameterIndex)
    {
        foreach (PrismGraphParameter parameter in node.Parameters)
        {
            if (parameter.Index == parameterIndex)
            {
                return parameter;
            }
        }
        throw InvalidParameter(node, parameterIndex);
    }

    private static InvalidOperationException InvalidParameter(
        PrismGraphNode node,
        int parameterIndex) =>
        new(
            $"Prism graph node '{node.Id}' has no compatible catalog parameter " +
            $"at index {parameterIndex}.");

    private static DrawRect Inflate(DrawRect bounds, float amount)
    {
        if (!float.IsFinite(amount) || amount < 0)
        {
            throw new InvalidOperationException(
                "Prism bounds expansion must be finite and non-negative.");
        }
        return CreateBounds(
            bounds.X - amount,
            bounds.Y - amount,
            bounds.Right + amount,
            bounds.Bottom + amount);
    }

    private static DrawRect Translate(DrawRect bounds, float x, float y)
    {
        return CreateBounds(
            bounds.X + x,
            bounds.Y + y,
            bounds.Right + x,
            bounds.Bottom + y);
    }

    private static DrawRect Union(DrawRect left, DrawRect right)
    {
        return CreateBounds(
            MathF.Min(left.X, right.X),
            MathF.Min(left.Y, right.Y),
            MathF.Max(left.Right, right.Right),
            MathF.Max(left.Bottom, right.Bottom));
    }

    private static DrawRect Transform(DrawRect bounds, Matrix3x2 transform)
    {
        Vector2 topLeft = Vector2.Transform(new Vector2(bounds.X, bounds.Y), transform);
        Vector2 topRight = Vector2.Transform(new Vector2(bounds.Right, bounds.Y), transform);
        Vector2 bottomLeft = Vector2.Transform(new Vector2(bounds.X, bounds.Bottom), transform);
        Vector2 bottomRight = Vector2.Transform(new Vector2(bounds.Right, bounds.Bottom), transform);
        return CreateBounds(
            MathF.Min(MathF.Min(topLeft.X, topRight.X), MathF.Min(bottomLeft.X, bottomRight.X)),
            MathF.Min(MathF.Min(topLeft.Y, topRight.Y), MathF.Min(bottomLeft.Y, bottomRight.Y)),
            MathF.Max(MathF.Max(topLeft.X, topRight.X), MathF.Max(bottomLeft.X, bottomRight.X)),
            MathF.Max(MathF.Max(topLeft.Y, topRight.Y), MathF.Max(bottomLeft.Y, bottomRight.Y)));
    }

    private static DrawRect CreateBounds(float left, float top, float right, float bottom)
    {
        try
        {
            return new DrawRect(
                left,
                top,
                MathF.Max(0, right - left),
                MathF.Max(0, bottom - top));
        }
        catch (ArgumentOutOfRangeException exception)
        {
            throw new InvalidOperationException(
                "A Prism operation produced non-finite or unsupported expanded bounds.",
                exception);
        }
    }

    private static class BoundsParameters
    {
        public static readonly int BlurRadius =
            ParameterIndex((int)PrismFilterId.Blur, "Radius");
        public static readonly int BlurMoreRadius =
            ParameterIndex((int)PrismFilterId.BlurMore, "Radius");
        public static readonly int BoxBlurRadius =
            ParameterIndex((int)PrismFilterId.BoxBlur, "Radius");
        public static readonly int BoxBlurIterations =
            ParameterIndex((int)PrismFilterId.BoxBlur, "Iterations");
        public static readonly int GaussianBlurRadius =
            ParameterIndex((int)PrismFilterId.GaussianBlur, "Radius");
        public static readonly int TransformTranslate =
            ParameterIndex((int)PrismFilterId.Transform, "Translate");
        public static readonly int TransformScale =
            ParameterIndex((int)PrismFilterId.Transform, "Scale");
        public static readonly int TransformRotation =
            ParameterIndex((int)PrismFilterId.Transform, "Rotation");
        public static readonly int TransformSkew =
            ParameterIndex((int)PrismFilterId.Transform, "Skew");
        public static readonly int TransformOrigin =
            ParameterIndex((int)PrismFilterId.Transform, "Origin");
        public static readonly int DropShadowUseGlobalLight =
            ParameterIndex((int)PrismStyleId.DropShadow, "UseGlobalLight");
        public static readonly int DropShadowAngle =
            ParameterIndex((int)PrismStyleId.DropShadow, "Angle");
        public static readonly int DropShadowDistance =
            ParameterIndex((int)PrismStyleId.DropShadow, "Distance");
        public static readonly int DropShadowSpread =
            ParameterIndex((int)PrismStyleId.DropShadow, "Spread");
        public static readonly int DropShadowSize =
            ParameterIndex((int)PrismStyleId.DropShadow, "Size");
        public static readonly int StrokeSize =
            ParameterIndex((int)PrismStyleId.Stroke, "Size");

        private static int ParameterIndex(int stableId, string name)
        {
            PrismCatalogPropertyDescriptor[] properties =
                PrismCatalogRuntime.GetEntry(stableId).Properties;
            for (int index = 0; index < properties.Length; index++)
            {
                if (string.Equals(properties[index].Name, name, StringComparison.Ordinal))
                {
                    return properties[index].Slot;
                }
            }
            throw new InvalidOperationException(
                $"Prism catalog entry '{stableId}' has no '{name}' property.");
        }
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

    private readonly record struct BoundsCalculation(
        DrawRect Bounds,
        PrismGraphBoundsStatus Status);

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
