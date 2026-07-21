using System.Collections.Immutable;
using System.Globalization;
using System.Numerics;
using System.Text;
using Cerneala.Drawing.Prism.Catalog;
using Cerneala.Drawing.Prism.Filters;
using Cerneala.UI.Prism.Definitions;
using Cerneala.UI.Prism.Runtime;

namespace Cerneala.Drawing.Prism.Graph;

internal enum PrismGraphNodeKind
{
    ControlCapture,
    BackdropInput,
    ColorConversion,
    Layer,
    Group,
    Filter,
    Style,
    Mask,
    Fill,
    Opacity,
    ClipToBelow,
    Composite,
    PassThroughComposite,
    BackdropCrop
}

internal enum PrismGraphEdgeKind
{
    Content,
    StyleSource,
    Control,
    Backdrop,
    GroupContent,
    MaskAlpha,
    ClipBaseAlpha,
    CompositeBackground,
    CompositeForeground
}

internal enum PrismGraphDependencyKind
{
    Structure,
    Values,
    VisualContent,
    Descendants,
    Bounds,
    PixelScale,
    Transform,
    ColorProfile,
    CatalogEntry,
    Resource,
    BackdropFrame
}

internal enum PrismGraphParameterValueKind
{
    Boolean,
    Integer,
    Number,
    Color,
    Vector,
    Symbol,
    Resource
}

internal readonly record struct PrismGraphNodeId
{
    public PrismGraphNodeId(
        PrismCacheOwnerToken scopeOwnerToken,
        int definitionNodeId,
        PrismGraphNodeKind kind,
        int ordinal)
    {
        if (scopeOwnerToken.Value <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(scopeOwnerToken),
                "A graph node requires a non-default scope owner token.");
        }
        ArgumentOutOfRangeException.ThrowIfNegative(definitionNodeId);
        ArgumentOutOfRangeException.ThrowIfNegative(ordinal);
        if (!Enum.IsDefined(typeof(PrismGraphNodeKind), kind))
        {
            throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown Prism graph node kind.");
        }

        ScopeOwnerToken = scopeOwnerToken;
        DefinitionNodeId = definitionNodeId;
        Kind = kind;
        Ordinal = ordinal;
    }

    public PrismCacheOwnerToken ScopeOwnerToken { get; }

    public int DefinitionNodeId { get; }

    public PrismGraphNodeKind Kind { get; }

    public int Ordinal { get; }

    public override string ToString()
    {
        return string.Create(
            CultureInfo.InvariantCulture,
            $"{ScopeOwnerToken.Value}:{DefinitionNodeId}:{Kind}:{Ordinal}");
    }
}

internal readonly record struct PrismGraphDependency(
    PrismGraphDependencyKind Kind,
    long Key,
    long Version);

internal readonly record struct PrismGraphParameter
{
    internal PrismGraphParameter(
        int index,
        PrismGraphParameterValueKind kind,
        bool booleanValue = default,
        int integerValue = default,
        float numberValue = default,
        Color colorValue = default,
        Vector4 vectorValue = default,
        PrismResourceId resourceValue = default)
    {
        Index = index;
        Kind = kind;
        BooleanValue = booleanValue;
        IntegerValue = integerValue;
        NumberValue = numberValue;
        ColorValue = colorValue;
        VectorValue = vectorValue;
        ResourceValue = resourceValue;
    }

    public int Index { get; }

    public PrismGraphParameterValueKind Kind { get; }

    public bool BooleanValue { get; }

    public int IntegerValue { get; }

    public float NumberValue { get; }

    public Color ColorValue { get; }

    public Vector4 VectorValue { get; }

    public PrismResourceId ResourceValue { get; }
}

internal readonly record struct PrismGraphEdge(
    PrismGraphNodeId Source,
    PrismGraphNodeId Target,
    PrismGraphEdgeKind Kind);

internal readonly record struct PrismGraphCompositionSettings(
    PrismColorProfile WorkingColorProfile,
    float GlobalLightAngle,
    float GlobalLightAltitude);

internal readonly record struct PrismGraphLayerSettings(
    PrismBlendChannels BlendChannels,
    PrismKnockout Knockout,
    bool BlendInteriorStylesAsGroup,
    bool BlendClippedLayersAsGroup,
    bool TransparencyShapesLayer,
    bool LayerMaskHidesStyles,
    bool VectorMaskHidesStyles,
    PrismBlendIfChannel BlendIfChannel,
    PrismBlendRange ThisLayerRange,
    PrismBlendRange UnderlyingRange,
    int DissolveSeed);

internal enum PrismMaskPass
{
    Extract,
    FeatherHorizontal,
    FeatherVertical
}

internal readonly record struct PrismGraphScope
{
    internal PrismGraphScope(
        int analysisScopeIndex,
        int beginCommandIndex,
        int endCommandIndex,
        int depth,
        int? parentScopeIndex,
        PrismCacheOwnerToken cacheOwnerToken,
        PrismGraphCompositionSettings compositionSettings,
        DrawRect bounds,
        DrawRect controlBounds,
        Matrix3x2 effectiveTransform,
        float pixelScale,
        PrismDependencyStamp dependencyStamp,
        long lowerUiVersion,
        PrismDrawResources resources,
        PrismGraphNodeId? output)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(analysisScopeIndex);
        ArgumentOutOfRangeException.ThrowIfNegative(beginCommandIndex);
        if (endCommandIndex <= beginCommandIndex)
        {
            throw new ArgumentOutOfRangeException(
                nameof(endCommandIndex),
                endCommandIndex,
                "A Prism scope must end after it begins.");
        }
        ArgumentOutOfRangeException.ThrowIfNegative(depth);
        if (parentScopeIndex is int parentIndex)
        {
            if (parentIndex < 0 ||
                parentIndex == analysisScopeIndex ||
                depth == 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(parentScopeIndex),
                    parentScopeIndex,
                    "A nested Prism scope requires a different non-negative parent and positive depth.");
            }
        }
        else if (depth != 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(depth),
                depth,
                "A root Prism scope must have depth zero.");
        }
        if (cacheOwnerToken.Value <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(cacheOwnerToken),
                "A graph scope requires a non-default cache owner token.");
        }
        if (!float.IsFinite(pixelScale) || pixelScale <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(pixelScale),
                pixelScale,
                "A graph scope requires a finite positive pixel scale.");
        }
        if (!IsFinite(effectiveTransform))
        {
            throw new ArgumentOutOfRangeException(
                nameof(effectiveTransform),
                "A graph scope requires a finite effective transform.");
        }
        if (dependencyStamp.CacheOwnerToken != cacheOwnerToken)
        {
            throw new ArgumentException(
                "A graph scope dependency stamp must belong to the same cache owner.",
                nameof(dependencyStamp));
        }
        if (lowerUiVersion < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(lowerUiVersion),
                lowerUiVersion,
                "A graph scope lower UI version cannot be negative.");
        }
        if (output is PrismGraphNodeId outputId &&
            outputId.ScopeOwnerToken != cacheOwnerToken)
        {
            throw new ArgumentException(
                "A graph scope output must belong to the same cache owner.",
                nameof(output));
        }

        AnalysisScopeIndex = analysisScopeIndex;
        BeginCommandIndex = beginCommandIndex;
        EndCommandIndex = endCommandIndex;
        Depth = depth;
        ParentScopeIndex = parentScopeIndex;
        CacheOwnerToken = cacheOwnerToken;
        CompositionSettings = compositionSettings;
        Bounds = bounds;
        ControlBounds = controlBounds;
        EffectiveTransform = effectiveTransform;
        PixelScale = pixelScale;
        DependencyStamp = dependencyStamp;
        LowerUiVersion = lowerUiVersion;
        Resources =
            resources ??
            throw new ArgumentNullException(nameof(resources));
        Output = output;
    }

    public int AnalysisScopeIndex { get; }

    public int BeginCommandIndex { get; }

    public int EndCommandIndex { get; }

    public int Depth { get; }

    public int? ParentScopeIndex { get; }

    public PrismCacheOwnerToken CacheOwnerToken { get; }

    public PrismGraphCompositionSettings CompositionSettings { get; }

    public DrawRect Bounds { get; }

    internal DrawRect ControlBounds { get; }

    public Matrix3x2 EffectiveTransform { get; }

    public float PixelScale { get; }

    internal PrismDependencyStamp DependencyStamp { get; }

    internal long LowerUiVersion { get; }

    internal PrismDrawResources Resources { get; }

    public PrismGraphNodeId? Output { get; }

    private static bool IsFinite(Matrix3x2 matrix) =>
        float.IsFinite(matrix.M11) &&
        float.IsFinite(matrix.M12) &&
        float.IsFinite(matrix.M21) &&
        float.IsFinite(matrix.M22) &&
        float.IsFinite(matrix.M31) &&
        float.IsFinite(matrix.M32);
}

internal sealed class PrismGraphNode
{
    internal PrismGraphNode(
        PrismGraphNodeId id,
        PrismGraphNodeKind kind,
        int analysisScopeIndex,
        PrismNodeId? definitionNodeId,
        int definitionOrder,
        string diagnosticName,
        ImmutableArray<PrismGraphDependency> dependencies,
        ImmutableArray<PrismGraphParameter> parameters = default,
        bool isIsolationBoundary = false,
        PrismBlendMode? blendMode = null,
        float? amount = null,
        PrismFilterId? filter = null,
        PrismStyleId? style = null,
        PrismResourceId? resource = null,
        PrismColorProfile? colorProfile = null,
        PrismMaskChannel? maskChannel = null,
        float? feather = null,
        float? density = null,
        bool? invert = null,
        PrismMaskPass? maskPass = null,
        PrismGraphLayerSettings? layerSettings = null,
        PrismNeighborhoodPlan? neighborhoodPlan = null,
        int neighborhoodPassIndex = -1,
        PrismResamplingPlan? resamplingPlan = null,
        int resamplingPassIndex = -1,
        PrismCatalogFilterPlan? catalogFilterPlan = null,
        int catalogFilterPassIndex = -1,
        BackdropFrameMetadata? backdropMetadata = null,
        DrawRect? backdropSourceBounds = null)
    {
        Id = id;
        Kind = kind;
        AnalysisScopeIndex = analysisScopeIndex;
        DefinitionNodeId = definitionNodeId;
        DefinitionOrder = definitionOrder;
        DiagnosticName = diagnosticName;
        Dependencies = dependencies.IsDefault
            ? ImmutableArray<PrismGraphDependency>.Empty
            : dependencies;
        Parameters = parameters.IsDefault
            ? ImmutableArray<PrismGraphParameter>.Empty
            : parameters;
        IsIsolationBoundary = isIsolationBoundary;
        BlendMode = blendMode;
        Amount = amount;
        Filter = filter;
        Style = style;
        Resource = resource;
        ColorProfile = colorProfile;
        MaskChannel = maskChannel;
        Feather = feather;
        Density = density;
        Invert = invert;
        MaskPass = maskPass;
        LayerSettings = layerSettings;
        if (neighborhoodPlan is PrismNeighborhoodPlan prepared)
        {
            if ((uint)neighborhoodPassIndex >=
                (uint)prepared.Passes.Length)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(neighborhoodPassIndex),
                    neighborhoodPassIndex,
                    "A prepared filter pass must exist in its plan.");
            }
        }
        else if (neighborhoodPassIndex != -1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(neighborhoodPassIndex),
                neighborhoodPassIndex,
                "A filter pass index requires a prepared plan.");
        }
        if (resamplingPlan is PrismResamplingPlan resampling)
        {
            if ((uint)resamplingPassIndex >=
                (uint)resampling.Passes.Length)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(resamplingPassIndex),
                    resamplingPassIndex,
                    "A prepared resampling pass must exist in its plan.");
            }
        }
        else if (resamplingPassIndex != -1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(resamplingPassIndex),
                resamplingPassIndex,
                "A resampling pass index requires a prepared plan.");
        }
        if (catalogFilterPlan is PrismCatalogFilterPlan catalogFilter)
        {
            if ((uint)catalogFilterPassIndex >=
                (uint)catalogFilter.Passes.Length)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(catalogFilterPassIndex),
                    catalogFilterPassIndex,
                    "A prepared catalog filter pass must exist in its plan.");
            }
        }
        else if (catalogFilterPassIndex != -1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(catalogFilterPassIndex),
                catalogFilterPassIndex,
                "A catalog filter pass index requires a prepared plan.");
        }
        int preparedFamilyCount =
            (neighborhoodPlan is null ? 0 : 1) +
            (resamplingPlan is null ? 0 : 1) +
            (catalogFilterPlan is null ? 0 : 1);
        if (preparedFamilyCount > 1)
        {
            throw new ArgumentException(
                "A filter node cannot contain multiple prepared planner families.");
        }
        NeighborhoodPlan = neighborhoodPlan;
        NeighborhoodPassIndex = neighborhoodPassIndex;
        ResamplingPlan = resamplingPlan;
        ResamplingPassIndex = resamplingPassIndex;
        CatalogFilterPlan = catalogFilterPlan;
        CatalogFilterPassIndex = catalogFilterPassIndex;
        if (backdropMetadata is not null &&
            kind != PrismGraphNodeKind.ColorConversion)
        {
            throw new ArgumentException(
                "Backdrop raster metadata belongs on its color-conversion node.",
                nameof(backdropMetadata));
        }
        if (backdropSourceBounds is not null &&
            kind != PrismGraphNodeKind.BackdropCrop)
        {
            throw new ArgumentException(
                "Backdrop source bounds belong on a backdrop-crop node.",
                nameof(backdropSourceBounds));
        }
        BackdropMetadata = backdropMetadata;
        BackdropSourceBounds = backdropSourceBounds;
    }

    public PrismGraphNodeId Id { get; }

    public PrismGraphNodeKind Kind { get; }

    public int AnalysisScopeIndex { get; }

    public PrismNodeId? DefinitionNodeId { get; }

    public int DefinitionOrder { get; }

    public string DiagnosticName { get; }

    public ImmutableArray<PrismGraphDependency> Dependencies { get; }

    public ImmutableArray<PrismGraphParameter> Parameters { get; }

    public bool IsIsolationBoundary { get; }

    public PrismBlendMode? BlendMode { get; }

    public float? Amount { get; }

    public PrismFilterId? Filter { get; }

    public PrismStyleId? Style { get; }

    public PrismResourceId? Resource { get; }

    public PrismColorProfile? ColorProfile { get; }

    public PrismMaskChannel? MaskChannel { get; }

    public float? Feather { get; }

    public float? Density { get; }

    public bool? Invert { get; }

    internal PrismMaskPass? MaskPass { get; }

    public PrismGraphLayerSettings? LayerSettings { get; }

    internal PrismNeighborhoodPlan? NeighborhoodPlan { get; }

    internal int NeighborhoodPassIndex { get; }

    internal PrismResamplingPlan? ResamplingPlan { get; }

    internal int ResamplingPassIndex { get; }

    internal PrismCatalogFilterPlan? CatalogFilterPlan { get; }

    internal int CatalogFilterPassIndex { get; }

    public BackdropFrameMetadata? BackdropMetadata { get; }

    public DrawRect? BackdropSourceBounds { get; }
}

internal sealed class PrismGraph
{
    private readonly ImmutableDictionary<PrismGraphNodeId, PrismGraphNode> nodesById;

    internal PrismGraph(
        ImmutableArray<PrismGraphNode> nodes,
        ImmutableArray<PrismGraphEdge> edges,
        ImmutableArray<PrismGraphScope> scopes)
    {
        ImmutableDictionary<PrismGraphNodeId, PrismGraphNode>.Builder index =
            ImmutableDictionary.CreateBuilder<PrismGraphNodeId, PrismGraphNode>();
        foreach (PrismGraphNode node in nodes)
        {
            if (!index.TryAdd(node.Id, node))
            {
                throw new InvalidOperationException($"Duplicate Prism graph node identifier '{node.Id}'.");
            }
        }

        foreach (PrismGraphEdge edge in edges)
        {
            if (!index.ContainsKey(edge.Source) || !index.ContainsKey(edge.Target))
            {
                throw new InvalidOperationException("A Prism graph edge references an unknown node.");
            }
        }
        ValidateAcyclic(index.Keys, edges);

        Nodes = nodes;
        Edges = edges;
        Scopes = scopes;
        nodesById = index.ToImmutable();
    }

    public ImmutableArray<PrismGraphNode> Nodes { get; }

    public ImmutableArray<PrismGraphEdge> Edges { get; }

    public ImmutableArray<PrismGraphScope> Scopes { get; }

    public int ControlCaptureCount =>
        Nodes.Count(node => node.Kind == PrismGraphNodeKind.ControlCapture);

    public int BackdropInputCount =>
        Nodes.Count(node => node.Kind == PrismGraphNodeKind.BackdropInput);

    public PrismGraphNode GetNode(PrismGraphNodeId id)
    {
        return nodesById.TryGetValue(id, out PrismGraphNode? node)
            ? node
            : throw new KeyNotFoundException($"Prism graph node '{id}' does not exist.");
    }

    public string ToDiagnosticString()
    {
        StringBuilder builder = new();
        foreach (PrismGraphScope scope in Scopes)
        {
            builder.Append("scope ")
                .Append(scope.AnalysisScopeIndex)
                .Append(" owner=")
                .Append(scope.CacheOwnerToken.Value)
                .Append(" output=")
                .Append(scope.Output?.ToString() ?? "<none>")
                .AppendLine();
        }
        foreach (PrismGraphNode node in Nodes)
        {
            builder.Append("node ")
                .Append(node.Id)
                .Append(' ')
                .Append(node.Kind)
                .Append(" definition=")
                .Append(node.DefinitionNodeId?.Value.ToString(CultureInfo.InvariantCulture) ?? "<none>")
                .Append(" order=")
                .Append(node.DefinitionOrder)
                .Append(" name=")
                .Append(node.DiagnosticName)
                .AppendLine();
            foreach (PrismGraphDependency dependency in node.Dependencies)
            {
                builder.Append("  dependency ")
                    .Append(dependency.Kind)
                    .Append(' ')
                    .Append(dependency.Key)
                    .Append('=')
                    .Append(dependency.Version)
                    .AppendLine();
            }
        }
        foreach (PrismGraphEdge edge in Edges)
        {
            builder.Append("edge ")
                .Append(edge.Source)
                .Append(" -> ")
                .Append(edge.Target)
                .Append(' ')
                .Append(edge.Kind)
                .AppendLine();
        }

        return builder.ToString().TrimEnd();
    }

    private static void ValidateAcyclic(
        IEnumerable<PrismGraphNodeId> nodeIds,
        ImmutableArray<PrismGraphEdge> edges)
    {
        Dictionary<PrismGraphNodeId, int> indegrees = [];
        Dictionary<PrismGraphNodeId, List<PrismGraphNodeId>> outgoing = [];
        foreach (PrismGraphNodeId nodeId in nodeIds)
        {
            indegrees.Add(nodeId, 0);
            outgoing.Add(nodeId, []);
        }
        foreach (PrismGraphEdge edge in edges)
        {
            indegrees[edge.Target]++;
            outgoing[edge.Source].Add(edge.Target);
        }

        Queue<PrismGraphNodeId> ready = new(
            indegrees
                .Where(pair => pair.Value == 0)
                .Select(pair => pair.Key));
        int visited = 0;
        while (ready.TryDequeue(out PrismGraphNodeId nodeId))
        {
            visited++;
            foreach (PrismGraphNodeId target in outgoing[nodeId])
            {
                indegrees[target]--;
                if (indegrees[target] == 0)
                {
                    ready.Enqueue(target);
                }
            }
        }

        if (visited != indegrees.Count)
        {
            throw new InvalidOperationException(
                "The Prism graph contains a cycle.");
        }
    }
}
