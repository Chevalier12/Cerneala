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
    private PrismGraph? previousSourceGraph;
    private PrismGraphExecutionPlan? previousPlan;
    private readonly Dictionary<PrismGraphNodeId, PrismGraphNode> originalNodes = [];
    private readonly Dictionary<int, PrismGraphScope> originalScopes = [];
    private readonly Dictionary<PrismGraphNodeId, PrismGraphNodeId> aliases = [];
    private readonly Dictionary<PrismGraphNodeId, PrismGraphNodeId> previousAliases = [];
    private readonly List<PrismGraphNode> orderedAliasNodes = [];
    private readonly PrismRetainedFingerprintBuilder fingerprintBuilder = new();
    private bool hasPreviousAliases;

    public PrismGraphExecutionPlan Optimize(PrismGraph graph)
    {
        ArgumentNullException.ThrowIfNull(graph);
        if (ReferenceEquals(previousSourceGraph, graph) &&
            previousPlan is PrismGraphExecutionPlan retainedPlan)
        {
            return retainedPlan;
        }

        return OptimizeChangedGraph(graph);
    }

    private PrismGraphExecutionPlan OptimizeChangedGraph(PrismGraph graph)
    {
        // Keep cold-path LINQ captures out of the cached-plan entry point.
        // C# creates their closure at method entry, even on an early cache hit.
        originalNodes.Clear();
        foreach (PrismGraphNode node in graph.Nodes)
        {
            originalNodes.Add(node.Id, node);
        }
        originalScopes.Clear();
        foreach (PrismGraphScope scope in graph.Scopes)
        {
            originalScopes.Add(scope.AnalysisScopeIndex, scope);
        }
        aliases.Clear();
        FindProvenAliases(
            graph,
            originalNodes,
            originalScopes,
            aliases,
            orderedAliasNodes);
        ImmutableDictionary<
            PrismGraphNodeId,
            ImmutableArray<PrismGraphDependency>> aliasedDependencies =
            CollectAliasedDependencies(aliases, originalNodes);
        ImmutableArray<PrismGraphScope> scopes =
            RewriteScopes(graph.Scopes, aliases);
        ValidateScopeHierarchy(scopes);
        if (previousSourceGraph is PrismGraph reusableSourceGraph &&
            previousPlan is PrismGraphExecutionPlan reusableTopologyPlan &&
            hasPreviousAliases &&
            CanReuseTopology(
                reusableSourceGraph,
                graph,
                reusableTopologyPlan,
                previousAliases,
                aliases,
                scopes))
        {
            ImmutableArray<PrismGraphNode>.Builder reusedNodes =
                ImmutableArray.CreateBuilder<PrismGraphNode>(
                    reusableTopologyPlan.ExecutionOrder.Length);
            foreach (PrismGraphNodeId nodeId in reusableTopologyPlan.ExecutionOrder)
            {
                reusedNodes.Add(originalNodes[nodeId]);
            }
            PrismGraph reusedOptimizedGraph = new(
                reusedNodes.MoveToImmutable(),
                reusableTopologyPlan.OptimizedGraph.Edges,
                scopes);
            ImmutableArray<PrismGraphNodePlan> reusedNodePlans =
                BuildNodePlans(
                    reusedOptimizedGraph,
                    aliasedDependencies,
                    reusableTopologyPlan);
            PrismGraphExecutionPlan reusedResult = new(
                reusedOptimizedGraph,
                reusedNodePlans,
                reusableTopologyPlan);
            previousSourceGraph = graph;
            previousPlan = reusedResult;
            return reusedResult;
        }
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
        PrismGraphExecutionPlan? reusableFingerprintPlan =
            previousPlan is not null &&
            HasSameFingerprintStructure(
                previousPlan.OptimizedGraph,
                optimizedGraph)
                ? previousPlan
                : null;
        ImmutableArray<PrismGraphNodePlan> nodePlans =
            BuildNodePlans(
                optimizedGraph,
                aliasedDependencies,
                reusableFingerprintPlan);
        ImmutableArray<PrismGraphSurfaceLifetime> lifetimes =
            BuildSurfaceLifetimes(optimizedGraph, executionIndices);
        int peakLiveSurfaces = CalculatePeakLiveSurfaces(lifetimes);
        ImmutableArray<PrismGraphNodeId> removedNodes =
            originalNodes.Keys
                .Where(id => !reachable.Contains(id))
                .OrderBy(id => id, PrismGraphNodeIdComparer.Instance)
                .ToImmutableArray();
        PrismGraphExecutionPlan result = new(
            optimizedGraph,
            executionNodes.Select(node => node.Id).ToImmutableArray(),
            nodePlans,
            lifetimes,
            removedNodes,
            peakLiveSurfaces);
        previousSourceGraph = graph;
        previousPlan = result;
        previousAliases.Clear();
        foreach ((PrismGraphNodeId alias, PrismGraphNodeId source) in aliases)
        {
            previousAliases.Add(alias, source);
        }
        hasPreviousAliases = true;
        return result;
    }

    private static bool CanReuseTopology(
        PrismGraph previousSource,
        PrismGraph currentSource,
        PrismGraphExecutionPlan previousPlan,
        IReadOnlyDictionary<PrismGraphNodeId, PrismGraphNodeId> previousAliases,
        IReadOnlyDictionary<PrismGraphNodeId, PrismGraphNodeId> currentAliases,
        ImmutableArray<PrismGraphScope> currentScopes)
    {
        if (previousSource.Nodes.Length != currentSource.Nodes.Length ||
            previousSource.Edges.Length != currentSource.Edges.Length ||
            previousAliases.Count != currentAliases.Count ||
            previousPlan.OptimizedGraph.Scopes.Length != currentScopes.Length)
        {
            return false;
        }

        for (int index = 0; index < currentSource.Nodes.Length; index++)
        {
            if (previousSource.Nodes[index].Id != currentSource.Nodes[index].Id)
            {
                return false;
            }
        }
        if (!previousSource.Edges.AsSpan().SequenceEqual(currentSource.Edges.AsSpan()))
        {
            return false;
        }
        foreach ((PrismGraphNodeId alias, PrismGraphNodeId source) in currentAliases)
        {
            if (!previousAliases.TryGetValue(alias, out PrismGraphNodeId previousSourceId) ||
                previousSourceId != source)
            {
                return false;
            }
        }

        ImmutableArray<PrismGraphScope> previousScopes =
            previousPlan.OptimizedGraph.Scopes;
        for (int index = 0; index < currentScopes.Length; index++)
        {
            PrismGraphScope previous = previousScopes[index];
            PrismGraphScope current = currentScopes[index];
            if (previous.AnalysisScopeIndex != current.AnalysisScopeIndex ||
                previous.BeginCommandIndex != current.BeginCommandIndex ||
                previous.EndCommandIndex != current.EndCommandIndex ||
                previous.Depth != current.Depth ||
                previous.ParentScopeIndex != current.ParentScopeIndex ||
                previous.Output != current.Output)
            {
                return false;
            }
        }
        return true;
    }

    private static bool HasSameFingerprintStructure(
        PrismGraph previous,
        PrismGraph current)
    {
        if (previous.Nodes.Length != current.Nodes.Length ||
            previous.Edges.Length != current.Edges.Length)
        {
            return false;
        }

        for (int index = 0; index < current.Nodes.Length; index++)
        {
            PrismGraphNode left = previous.Nodes[index];
            PrismGraphNode right = current.Nodes[index];
            if (left.Id != right.Id ||
                left.AnalysisScopeIndex != right.AnalysisScopeIndex ||
                left.DefinitionNodeId != right.DefinitionNodeId ||
                left.DefinitionOrder != right.DefinitionOrder ||
                left.IsIsolationBoundary != right.IsIsolationBoundary)
            {
                return false;
            }
        }

        return previous.Edges.AsSpan().SequenceEqual(
            current.Edges.AsSpan());
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

    private static void FindProvenAliases(
        PrismGraph graph,
        IReadOnlyDictionary<PrismGraphNodeId, PrismGraphNode> nodes,
        IReadOnlyDictionary<int, PrismGraphScope> scopes,
        Dictionary<PrismGraphNodeId, PrismGraphNodeId> aliases,
        List<PrismGraphNode> orderedNodes)
    {
        orderedNodes.Clear();
        orderedNodes.AddRange(graph.Nodes);
        orderedNodes.Sort(
            static (left, right) =>
                PrismGraphNodeIdComparer.Instance.Compare(left.Id, right.Id));
        bool changed;
        do
        {
            changed = false;
            foreach (PrismGraphNode node in orderedNodes)
            {
                if (aliases.ContainsKey(node.Id) ||
                    !TryGetAliasSource(
                        node,
                        graph.Edges,
                        aliases,
                        nodes,
                        scopes,
                        out PrismGraphNodeId source))
                {
                    continue;
                }

                aliases.Add(node.Id, source);
                changed = true;
            }
        }
        while (changed);
    }

    private static bool TryGetAliasSource(
        PrismGraphNode node,
        ImmutableArray<PrismGraphEdge> edges,
        IReadOnlyDictionary<PrismGraphNodeId, PrismGraphNodeId> aliases,
        IReadOnlyDictionary<PrismGraphNodeId, PrismGraphNode> nodes,
        IReadOnlyDictionary<int, PrismGraphScope> scopes,
        out PrismGraphNodeId source)
    {
        source = default;
        int pixelInputCount = 0;
        PrismGraphNodeId pixelInputSource = default;
        foreach (PrismGraphEdge edge in edges)
        {
            if (edge.Target == node.Id &&
                edge.Kind is PrismGraphEdgeKind.Content or PrismGraphEdgeKind.Backdrop)
            {
                pixelInputCount++;
                pixelInputSource = edge.Source;
            }
        }
        if (pixelInputCount != 1)
        {
            return false;
        }

        source = ResolveAlias(pixelInputSource, aliases);
        if (node.Kind is PrismGraphNodeKind.Fill or PrismGraphNodeKind.Opacity)
        {
            return node.Amount == 1f;
        }
        if (node.Kind == PrismGraphNodeKind.Filter &&
            IsProvenFilterNoOp(node, scopes))
        {
            return true;
        }
        if (node.Kind == PrismGraphNodeKind.Filter &&
            IsProvenFilterFusion(
                node,
                source,
                nodes,
                scopes))
        {
            return true;
        }
        if (node.Kind == PrismGraphNodeKind.Style)
        {
            if (!scopes.TryGetValue(
                    node.AnalysisScopeIndex,
                    out PrismGraphScope scope))
            {
                throw new InvalidOperationException(
                    $"Prism style node '{node.Id}' has no owning graph scope.");
            }

            return IsProvenStyleNoOp(node, scope);
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

    private static bool IsProvenFilterNoOp(
        PrismGraphNode node,
        IReadOnlyDictionary<int, PrismGraphScope> scopes)
    {
        if (node.Amount != 1f ||
            node.BlendMode != PrismBlendMode.Normal)
        {
            return false;
        }
        if (node.NeighborhoodPlan is PrismNeighborhoodPlan)
        {
            return GetNeighborhoodPass(node).IsNoOp;
        }
        if (node.ResamplingPlan is PrismResamplingPlan)
        {
            return GetResamplingPass(node).IsNoOp;
        }
        if (node.CatalogFilterPlan is PrismCatalogFilterPlan)
        {
            return GetCatalogFilterPass(node).IsNoOp;
        }
        if (node.Filter is PrismFilterId filter &&
            PrismAdjustmentPlanner.IsSupported(filter))
        {
            if (!scopes.TryGetValue(
                    node.AnalysisScopeIndex,
                    out PrismGraphScope scope))
            {
                throw new InvalidOperationException(
                    $"Prism adjustment node '{node.Id}' has no owning graph scope.");
            }

            return PrismAdjustmentPlanner.IsNoOp(
                PrismAdjustmentPlanner.Create(node, scope));
        }

        return false;
    }

    private static bool IsProvenFilterFusion(
        PrismGraphNode node,
        PrismGraphNodeId source,
        IReadOnlyDictionary<PrismGraphNodeId, PrismGraphNode> nodes,
        IReadOnlyDictionary<int, PrismGraphScope> scopes)
    {
        if (node.Amount != 1f ||
            node.BlendMode != PrismBlendMode.Normal ||
            node.Filter is not PrismFilterId filter ||
            !nodes.TryGetValue(source, out PrismGraphNode? sourceNode) ||
            sourceNode.Kind != PrismGraphNodeKind.Filter ||
            sourceNode.Filter != filter ||
            sourceNode.Amount != 1f ||
            sourceNode.BlendMode != PrismBlendMode.Normal ||
            sourceNode.AnalysisScopeIndex != node.AnalysisScopeIndex ||
            sourceNode.Id.ScopeOwnerToken != node.Id.ScopeOwnerToken ||
            PrismCatalogRuntime.GetEntry((int)filter).Fusion !=
                "same-parameters-idempotent")
        {
            return false;
        }

        if (!PrismAdjustmentPlanner.IsSupported(filter) ||
            !scopes.TryGetValue(
                node.AnalysisScopeIndex,
                out PrismGraphScope scope))
        {
            return false;
        }

        return PrismAdjustmentPlanner.Create(node, scope) ==
            PrismAdjustmentPlanner.Create(sourceNode, scope);
    }

    private static bool IsProvenStyleNoOp(
        PrismGraphNode node,
        PrismGraphScope scope)
    {
        PrismStylePlan plan = PrismStylePlanner.Create(node, scope);
        return plan.Style == PrismStyleId.BevelEmboss
            ? plan.Opacity == 0f &&
                plan.SecondaryOpacity == 0f
            : plan.Opacity == 0f;
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
                    scope.ControlBounds,
                    scope.EffectiveTransform,
                    scope.StyleTransform,
                    scope.PixelScale,
                    scope.DependencyStamp,
                    scope.LowerUiVersion,
                    scope.Resources,
                    scope.Output is PrismGraphNodeId output
                        ? ResolveAlias(output, aliases)
                        : null))
            .ToImmutableArray();
    }

    private static ImmutableArray<PrismGraphEdge> RewriteEdges(
        ImmutableArray<PrismGraphEdge> edges,
        IReadOnlyDictionary<PrismGraphNodeId, PrismGraphNodeId> aliases)
    {
        HashSet<PrismGraphEdge> rewritten = [];
        foreach (PrismGraphEdge edge in edges)
        {
            if (aliases.ContainsKey(edge.Target))
            {
                continue;
            }

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

    private ImmutableArray<PrismGraphNodePlan> BuildNodePlans(
        PrismGraph graph,
        IReadOnlyDictionary<
            PrismGraphNodeId,
            ImmutableArray<PrismGraphDependency>> aliasedDependencies,
        PrismGraphExecutionPlan? reusableFingerprintPlan)
    {
        Dictionary<int, PrismGraphScope> scopes = graph.Scopes
            .ToDictionary(scope => scope.AnalysisScopeIndex);
        Dictionary<PrismGraphNodeId, ImmutableArray<PrismGraphEdge>> incoming =
            IndexIncomingEdges(graph.Edges);
        Dictionary<PrismGraphNodeId, PrismGraphNodePlan> plans = [];
        fingerprintBuilder.Reset(graph);
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
                CalculateUncacheableReasons(
                    node,
                    scope,
                    inputs,
                    plans);
            PrismRetainedCacheCandidateKind cacheCandidateKind =
                SelectCacheCandidate(node, scope, reasons);
            PrismVerifiedFingerprint structuralFingerprint = default;
            PrismVerifiedFingerprint valueFingerprint = default;
            PrismVerifiedFingerprint dependencyFingerprint = default;
            if (cacheCandidateKind !=
                PrismRetainedCacheCandidateKind.None)
            {
                PrismGraphNodePlan? reusablePlan =
                    reusableFingerprintPlan?.GetNodePlan(node.Id);
                fingerprintBuilder.Create(
                    node.Id,
                    dependencies,
                    reusablePlan,
                    out structuralFingerprint,
                    out valueFingerprint,
                    out dependencyFingerprint);
            }
            PrismGraphNodePlan plan = new(
                node.Id,
                bounds.Bounds,
                bounds.Status,
                dependencies,
                reasons,
                cacheCandidateKind,
                structuralFingerprint,
                valueFingerprint,
                dependencyFingerprint);
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

    private static PrismNeighborhoodPass GetNeighborhoodPass(
        PrismGraphNode node)
    {
        if (node.NeighborhoodPlan is not PrismNeighborhoodPlan plan ||
            (uint)node.NeighborhoodPassIndex >=
                (uint)plan.Passes.Length)
        {
            throw new InvalidOperationException(
                $"Prism filter node '{node.Id}' has no prepared neighborhood pass.");
        }

        return plan.Passes[node.NeighborhoodPassIndex];
    }

    private static PrismResamplingPass GetResamplingPass(
        PrismGraphNode node)
    {
        if (node.ResamplingPlan is not PrismResamplingPlan plan ||
            (uint)node.ResamplingPassIndex >=
                (uint)plan.Passes.Length)
        {
            throw new InvalidOperationException(
                $"Prism filter node '{node.Id}' has no prepared resampling pass.");
        }

        return plan.Passes[node.ResamplingPassIndex];
    }

    private static PrismCatalogFilterPass GetCatalogFilterPass(
        PrismGraphNode node)
    {
        if (node.CatalogFilterPlan is not PrismCatalogFilterPlan plan ||
            (uint)node.CatalogFilterPassIndex >=
                (uint)plan.Passes.Length)
        {
            throw new InvalidOperationException(
                $"Prism filter node '{node.Id}' has no prepared catalog filter pass.");
        }

        return plan.Passes[node.CatalogFilterPassIndex];
    }

}
