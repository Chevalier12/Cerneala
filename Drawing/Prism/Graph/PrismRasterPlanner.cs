using System.Collections.Immutable;
using Cerneala.Drawing.Prism.Catalog;
using Cerneala.Drawing.Prism.Filters;
using Cerneala.Drawing.Prism.Styles;
using Cerneala.UI.Prism.Definitions;

namespace Cerneala.Drawing.Prism.Graph;

internal enum PrismRasterPassKind
{
    ThresholdCdf,
    ThresholdSelection,
    ShadowSpread,
    ShadowBlur,
    DistanceSeed,
    DistanceFlood,
    BevelHeight,
    BevelLighting
}

internal enum PrismRasterSurfaceFormat
{
    Rgba16Float,
    Rgba32Float,
    R32Float,
    Rgba8Unorm
}

internal readonly record struct PrismRasterSurface(
    int Width,
    int Height,
    PrismRasterSurfaceFormat Format);

internal readonly record struct PrismRasterPass(
    PrismGraphNodeId Owner,
    PrismRasterPassKind Kind,
    PrismRasterSurface Surface,
    float RadiusOrJump = 0,
    bool Horizontal = false,
    bool DirectionalCoverage = false);

internal sealed record PrismRasterExecutionPlan(
    PrismGraphExecutionPlan GraphPlan,
    ImmutableDictionary<PrismGraphNodeId, PrismRasterPass> AuxiliaryPasses,
    ImmutableDictionary<PrismGraphNodeId, PrismStylePlan> Styles,
    ImmutableDictionary<PrismGraphNodeId, PrismAdjustmentPlan> Adjustments);

// Lower semantic operations only after the raster extent is known. The result
// has explicit dependencies and lifetimes for every auxiliary render target;
// backends execute primitives, not hidden multipass algorithms.
internal sealed class PrismRasterPlanner
{
    private PrismGraphExecutionPlan? previousSource;
    private PrismRasterExecutionPlan? previousResult;
    private int previousWidth;
    private int previousHeight;

    internal PrismRasterExecutionPlan Prepare(
        PrismGraphExecutionPlan source,
        int width,
        int height)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(width);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(height);
        if (ReferenceEquals(source, previousSource) &&
            width == previousWidth && height == previousHeight)
        {
            return previousResult!;
        }

        PrismRasterExecutionPlan result = Build(source, width, height);
        previousSource = source;
        previousWidth = width;
        previousHeight = height;
        previousResult = result;
        return result;
    }

    private static PrismRasterExecutionPlan Build(
        PrismGraphExecutionPlan source,
        int width,
        int height)
    {
        PrismGraph graph = source.OptimizedGraph;
        Dictionary<int, PrismGraphScope> scopes = graph.Scopes
            .ToDictionary(scope => scope.AnalysisScopeIndex);
        Dictionary<PrismGraphNodeId, PrismGraphEdge[]> incoming = graph.Edges
            .GroupBy(edge => edge.Target)
            .ToDictionary(group => group.Key, group => group.ToArray());
        List<PrismGraphNode> nodes = [];
        List<PrismGraphNodePlan> nodePlans = [];
        List<PrismGraphEdge> edges = [.. graph.Edges];
        Dictionary<PrismGraphNodeId, PrismRasterPass> passes = [];
        Dictionary<PrismGraphNodeId, PrismStylePlan> styles = [];
        Dictionary<PrismGraphNodeId, PrismAdjustmentPlan> adjustments = [];
        Dictionary<(PrismGraphNodeId Source, bool Directional), PrismGraphNodeId> distanceFields = [];
        int ordinal = 0;

        foreach (PrismGraphNodeId id in source.ExecutionOrder)
        {
            PrismGraphNode node = graph.GetNode(id);
            PrismGraphScope scope = scopes[node.AnalysisScopeIndex];
            if (node.Kind == PrismGraphNodeKind.Filter &&
                node.Filter is PrismFilterId filter &&
                PrismAdjustmentPlanner.IsSupported(filter))
            {
                PrismAdjustmentPlan adjustment = PrismAdjustmentPlanner.Create(node, scope);
                adjustments.Add(id, adjustment);
                if (filter == PrismFilterId.Threshold)
                {
                    PrismGraphNodeId input = Input(node, PrismGraphEdgeKind.Content);
                    PrismGraphNodeId cdf = Add(node, input,
                        PrismRasterPassKind.ThresholdCdf,
                        new(PrismThresholdAnalysis.BinCount, 1, PrismRasterSurfaceFormat.R32Float));
                    PrismGraphNodeId threshold = Add(node, cdf,
                        PrismRasterPassKind.ThresholdSelection,
                        new(1, 1, PrismRasterSurfaceFormat.Rgba8Unorm),
                        adjustment.Parameters0.X);
                    edges.Add(new(threshold, id, PrismGraphEdgeKind.PreparedInput));
                }
            }
            else if (node.Kind == PrismGraphNodeKind.Style)
            {
                PrismStylePlan style = PrismStylePlanner.Create(node, scope);
                styles.Add(id, style);
                PrismGraphNodeId input = Input(node, PrismGraphEdgeKind.StyleSource);
                PrismGraphNodeId prepared = input;
                PrismRasterSurface maskSurface = new(width, height, PrismRasterSurfaceFormat.Rgba16Float);
                PrismRasterSurface fieldSurface = new(width, height, PrismRasterSurfaceFormat.Rgba32Float);
                if (style.Style == PrismStyleId.DropShadow)
                {
                    PrismStyleSamplingGeometry geometry = PrismStylePlanner.ResolveSamplingGeometry(style, scope);
                    if (geometry.Spread >= 0.5f)
                    {
                        prepared = Add(node, prepared, PrismRasterPassKind.ShadowSpread,
                            maskSurface, MathF.Ceiling(geometry.Spread), horizontal: true);
                        prepared = Add(node, prepared, PrismRasterPassKind.ShadowSpread,
                            maskSurface, MathF.Ceiling(geometry.Spread));
                    }
                    float techniqueScale = style.Technique == 0 ? 1f : style.Technique == 1 ? 0.65f : 0.8f;
                    float radius = MathF.Max(MathF.Ceiling(geometry.Size * techniqueScale * 1.5f), 1f);
                    prepared = Add(node, prepared, PrismRasterPassKind.ShadowBlur, maskSurface, radius, horizontal: true);
                    prepared = Add(node, prepared, PrismRasterPassKind.ShadowBlur, maskSurface, radius);
                }
                else if (style.Style is PrismStyleId.OuterGlow or PrismStyleId.BevelEmboss or PrismStyleId.Stroke)
                {
                    bool directional = style.Style == PrismStyleId.Stroke;
                    if (!distanceFields.TryGetValue((input, directional), out prepared))
                    {
                        prepared = Add(node, input, PrismRasterPassKind.DistanceSeed,
                            fieldSurface, directional: directional);
                        int extent = Math.Max(width, height);
                        // Largest power of two below extent, without overflowing
                        // at the upper end of the positive integer domain.
                        int jump = 1;
                        while (jump <= (extent - 1) / 2)
                        {
                            jump <<= 1;
                        }
                        for (; extent > 1 && jump >= 1; jump >>= 1)
                        {
                            prepared = Add(node, prepared, PrismRasterPassKind.DistanceFlood, fieldSurface, jump);
                        }
                        prepared = Add(node, prepared, PrismRasterPassKind.DistanceFlood, fieldSurface, 1);
                        distanceFields.Add((input, directional), prepared);
                    }
                    if (style.Style == PrismStyleId.BevelEmboss)
                    {
                        prepared = Add(node, prepared, PrismRasterPassKind.BevelHeight, fieldSurface);
                        prepared = Add(node, prepared, PrismRasterPassKind.BevelLighting, fieldSurface);
                    }
                }
                if (prepared != input)
                {
                    edges.Add(new(prepared, id, PrismGraphEdgeKind.PreparedInput));
                }
            }
            nodes.Add(node);
            // Semantic cache fingerprints already cover these deterministic
            // auxiliaries' inputs and parameters. Only semantic outputs are
            // retained; a hit prunes the complete lowered dependency subgraph.
            nodePlans.Add(source.GetNodePlan(id));
        }

        PrismGraph lowered = new([.. nodes], [.. edges], graph.Scopes);
        Dictionary<PrismGraphNodeId, int> indices = nodes
            .Select((node, index) => KeyValuePair.Create(node.Id, index)).ToDictionary();
        ImmutableArray<PrismGraphSurfaceLifetime> lifetimes =
            PrismGraphOptimizer.BuildSurfaceLifetimes(lowered, indices);
        PrismGraphExecutionPlan plan = new(lowered,
            [.. nodes.Select(node => node.Id)], [.. nodePlans], lifetimes,
            source.RemovedNodeIds, PrismGraphOptimizer.CalculatePeakLiveSurfaces(lifetimes));
        return new(plan, passes.ToImmutableDictionary(), styles.ToImmutableDictionary(), adjustments.ToImmutableDictionary());

        PrismGraphNodeId Input(PrismGraphNode owner, PrismGraphEdgeKind kind) =>
            incoming[owner.Id].Single(edge => edge.Kind == kind).Source;

        PrismGraphNodeId Add(
            PrismGraphNode owner,
            PrismGraphNodeId input,
            PrismRasterPassKind kind,
            PrismRasterSurface surface,
            float radiusOrJump = 0,
            bool horizontal = false,
            bool directional = false)
        {
            PrismGraphNodeId auxiliaryId = new(owner.Id.ScopeOwnerToken,
                owner.Id.DefinitionNodeId, PrismGraphNodeKind.RasterAuxiliary,
                ordinal++, owner.AnalysisScopeIndex);
            nodes.Add(new(auxiliaryId, PrismGraphNodeKind.RasterAuxiliary,
                owner.AnalysisScopeIndex, owner.DefinitionNodeId, owner.DefinitionOrder,
                $"{owner.DiagnosticName}/{kind}", owner.Dependencies));
            PrismGraphNodePlan ownerPlan = source.GetNodePlan(owner.Id);
            nodePlans.Add(new(auxiliaryId, ownerPlan.Bounds, ownerPlan.BoundsStatus,
                ownerPlan.CacheDependencies, ownerPlan.UncacheableReasons,
                PrismRetainedCacheCandidateKind.None, default, default, default));
            passes.Add(auxiliaryId, new(owner.Id, kind, surface, radiusOrJump, horizontal, directional));
            edges.Add(new(input, auxiliaryId, PrismGraphEdgeKind.Content));
            if (kind is PrismRasterPassKind.BevelHeight or PrismRasterPassKind.BevelLighting)
            {
                edges.Add(new(Input(owner, PrismGraphEdgeKind.StyleSource), auxiliaryId, PrismGraphEdgeKind.StyleSource));
                foreach (PrismGraphEdge background in incoming[owner.Id].Where(edge =>
                    edge.Kind == PrismGraphEdgeKind.CompositeBackground))
                {
                    edges.Add(new(background.Source, auxiliaryId, background.Kind));
                }
            }
            return auxiliaryId;
        }
    }
}
