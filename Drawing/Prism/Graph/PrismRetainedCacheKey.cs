using System.Collections.Immutable;
using System.Numerics;
using Cerneala.Drawing.Prism.Catalog;
using Cerneala.Drawing.Prism.Filters;
using Cerneala.UI.Prism.Definitions;
using Cerneala.UI.Prism.Runtime;

namespace Cerneala.Drawing.Prism.Graph;

internal enum PrismRetainedCacheCandidateKind
{
    None,
    Capture,
    Intermediate,
    Final
}

internal readonly struct PrismVerifiedFingerprint :
    IEquatable<PrismVerifiedFingerprint>
{
    private readonly ImmutableArray<long> components;

    public PrismVerifiedFingerprint(
        ImmutableArray<long> components)
        : this(
            components,
            CalculateFastHash(components))
    {
    }

    internal PrismVerifiedFingerprint(
        ImmutableArray<long> components,
        ulong fastHash)
    {
        if (components.IsDefault)
        {
            throw new ArgumentException(
                "A verified fingerprint requires initialized components.",
                nameof(components));
        }

        this.components = components;
        FastHash = fastHash;
    }

    public ImmutableArray<long> Components =>
        components.IsDefault
            ? ImmutableArray<long>.Empty
            : components;

    public ulong FastHash { get; }

    public bool IsInitialized => !components.IsDefault;

    public bool Equals(PrismVerifiedFingerprint other)
    {
        ImmutableArray<long> left = Components;
        ImmutableArray<long> right = other.Components;
        return FastHash == other.FastHash &&
            left.Length == right.Length &&
            left.AsSpan().SequenceEqual(right.AsSpan());
    }

    public override bool Equals(object? obj) =>
        obj is PrismVerifiedFingerprint other &&
        Equals(other);

    public override int GetHashCode() =>
        unchecked((int)(FastHash ^ (FastHash >> 32)));

    public static bool operator ==(
        PrismVerifiedFingerprint left,
        PrismVerifiedFingerprint right) =>
        left.Equals(right);

    public static bool operator !=(
        PrismVerifiedFingerprint left,
        PrismVerifiedFingerprint right) =>
        !left.Equals(right);

    private static ulong CalculateFastHash(
        ImmutableArray<long> values)
    {
        if (values.IsDefault)
        {
            throw new ArgumentException(
                "A verified fingerprint requires initialized components.",
                nameof(values));
        }

        const ulong offset = 14695981039346656037UL;
        const ulong prime = 1099511628211UL;
        ulong hash = offset;
        foreach (long value in values)
        {
            ulong bits = unchecked((ulong)value);
            hash = unchecked((hash ^ (uint)bits) * prime);
            hash = unchecked((hash ^ (uint)(bits >> 32)) * prime);
        }
        hash = unchecked((hash ^ (uint)values.Length) * prime);
        return hash;
    }
}

internal readonly record struct PrismRetainedRasterBounds(
    int XBits,
    int YBits,
    int WidthBits,
    int HeightBits)
{
    public static PrismRetainedRasterBounds From(
        DrawRect bounds) =>
        new(
            BitConverter.SingleToInt32Bits(bounds.X),
            BitConverter.SingleToInt32Bits(bounds.Y),
            BitConverter.SingleToInt32Bits(bounds.Width),
            BitConverter.SingleToInt32Bits(bounds.Height));
}

internal readonly record struct PrismRetainedTransform(
    int M11Bits,
    int M12Bits,
    int M21Bits,
    int M22Bits,
    int M31Bits,
    int M32Bits)
{
    public static PrismRetainedTransform From(
        Matrix3x2 transform) =>
        new(
            BitConverter.SingleToInt32Bits(transform.M11),
            BitConverter.SingleToInt32Bits(transform.M12),
            BitConverter.SingleToInt32Bits(transform.M21),
            BitConverter.SingleToInt32Bits(transform.M22),
            BitConverter.SingleToInt32Bits(transform.M31),
            BitConverter.SingleToInt32Bits(transform.M32));
}

internal readonly record struct PrismRetainedRasterContext
{
    private const PrismGraphCapabilities KnownCapabilities =
        PrismGraphCapabilities.ControlCapture |
        PrismGraphCapabilities.FilterProcessing |
        PrismGraphCapabilities.StyleProcessing |
        PrismGraphCapabilities.MaskProcessing |
        PrismGraphCapabilities.GroupProcessing |
        PrismGraphCapabilities.GroupIsolation |
        PrismGraphCapabilities.Clipping |
        PrismGraphCapabilities.AdvancedBlending |
        PrismGraphCapabilities.ColorConversion |
        PrismGraphCapabilities.BackdropInput;

    public PrismRetainedRasterContext(
        int surfaceWidth,
        int surfaceHeight,
        PrismColorProfile outputColorProfile,
        BackdropPixelFormat surfaceFormat,
        PrismSampling sampling,
        PrismGraphCapabilities capabilitySet,
        long shaderPackageVersion)
    {
        SurfaceWidth = surfaceWidth;
        SurfaceHeight = surfaceHeight;
        OutputColorProfile = outputColorProfile;
        SurfaceFormat = surfaceFormat;
        Sampling = sampling;
        CapabilitySet = capabilitySet;
        ShaderPackageVersion = shaderPackageVersion;
        EnsureValid();
    }

    public int SurfaceWidth { get; }

    public int SurfaceHeight { get; }

    public PrismColorProfile OutputColorProfile { get; }

    public BackdropPixelFormat SurfaceFormat { get; }

    public PrismSampling Sampling { get; }

    public PrismGraphCapabilities CapabilitySet { get; }

    public long ShaderPackageVersion { get; }

    public void EnsureValid()
    {
        if (SurfaceWidth <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(SurfaceWidth),
                SurfaceWidth,
                "A retained surface width must be positive.");
        }
        if (SurfaceHeight <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(SurfaceHeight),
                SurfaceHeight,
                "A retained surface height must be positive.");
        }
        if (!Enum.IsDefined(OutputColorProfile))
        {
            throw new ArgumentOutOfRangeException(
                nameof(OutputColorProfile),
                OutputColorProfile,
                "Unknown Prism output color profile.");
        }
        if (!Enum.IsDefined(SurfaceFormat))
        {
            throw new ArgumentOutOfRangeException(
                nameof(SurfaceFormat),
                SurfaceFormat,
                "Unknown Prism retained surface format.");
        }
        if (!Enum.IsDefined(Sampling))
        {
            throw new ArgumentOutOfRangeException(
                nameof(Sampling),
                Sampling,
                "Unknown Prism retained sampling mode.");
        }
        if ((CapabilitySet & ~KnownCapabilities) != 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(CapabilitySet),
                CapabilitySet,
                "Unknown Prism graph capability.");
        }
        if (ShaderPackageVersion <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(ShaderPackageVersion),
                ShaderPackageVersion,
                "A retained cache key requires a positive shader package version.");
        }
    }
}

internal readonly record struct PrismRetainedCacheKey(
    PrismRetainedCacheCandidateKind CandidateKind,
    PrismGraphNodeId StableNodeId,
    PrismDependencyStamp DependencyStamp,
    PrismVerifiedFingerprint StructuralFingerprint,
    PrismVerifiedFingerprint ValueFingerprint,
    PrismVerifiedFingerprint DependencyFingerprint,
    PrismRetainedRasterBounds RasterBounds,
    int SurfaceWidth,
    int SurfaceHeight,
    long LowerUiVersion,
    int PixelScaleBits,
    PrismRetainedTransform EffectiveTransform,
    PrismColorProfile WorkingColorProfile,
    PrismColorProfile OutputColorProfile,
    BackdropPixelFormat SurfaceFormat,
    PrismSampling Sampling,
    PrismGraphCapabilities CapabilitySet,
    long ShaderPackageVersion)
{
    public static bool TryCreate(
        PrismGraphExecutionPlan executionPlan,
        PrismGraphNodeId nodeId,
        in PrismRetainedRasterContext rasterContext,
        out PrismRetainedCacheKey key)
    {
        ArgumentNullException.ThrowIfNull(executionPlan);
        rasterContext.EnsureValid();

        PrismGraphNodePlan plan =
            executionPlan.GetNodePlan(nodeId);
        if (!plan.IsCacheable ||
            plan.CacheCandidateKind ==
                PrismRetainedCacheCandidateKind.None)
        {
            key = default;
            return false;
        }

        PrismGraphNode node =
            executionPlan.OptimizedGraph.GetNode(nodeId);
        PrismGraphScope scope = default;
        bool foundScope = false;
        foreach (PrismGraphScope candidate in
            executionPlan.OptimizedGraph.Scopes)
        {
            if (candidate.AnalysisScopeIndex ==
                node.AnalysisScopeIndex)
            {
                scope = candidate;
                foundScope = true;
                break;
            }
        }
        if (!foundScope)
        {
            throw new InvalidOperationException(
                $"Prism graph node '{nodeId}' has no owning graph scope.");
        }

        long lowerUiVersion = 0;
        foreach (PrismGraphDependency dependency in
            plan.CacheDependencies)
        {
            if (dependency.Kind ==
                PrismGraphDependencyKind.BackdropFrame)
            {
                lowerUiVersion = scope.LowerUiVersion;
                break;
            }
        }

        key = new PrismRetainedCacheKey(
            plan.CacheCandidateKind,
            nodeId,
            scope.DependencyStamp,
            plan.StructuralFingerprint,
            plan.ValueFingerprint,
            plan.DependencyFingerprint,
            PrismRetainedRasterBounds.From(plan.Bounds),
            rasterContext.SurfaceWidth,
            rasterContext.SurfaceHeight,
            lowerUiVersion,
            BitConverter.SingleToInt32Bits(scope.PixelScale),
            PrismRetainedTransform.From(
                scope.EffectiveTransform),
            scope.CompositionSettings.WorkingColorProfile,
            rasterContext.OutputColorProfile,
            rasterContext.SurfaceFormat,
            rasterContext.Sampling,
            rasterContext.CapabilitySet,
            rasterContext.ShaderPackageVersion);
        return true;
    }
}

internal sealed class PrismRetainedFingerprintBuilder
{
    private readonly PrismGraph graph;
    private readonly Dictionary<
        PrismGraphNodeId,
        ImmutableArray<PrismGraphEdge>> incoming;
    private readonly Dictionary<int, PrismGraphScope> scopes;

    public PrismRetainedFingerprintBuilder(
        PrismGraph graph)
    {
        this.graph = graph ??
            throw new ArgumentNullException(nameof(graph));
        incoming = graph.Edges
            .GroupBy(edge => edge.Target)
            .ToDictionary(
                group => group.Key,
                group => group
                    .OrderBy(edge => edge.Kind)
                    .ThenBy(
                        edge => edge.Source,
                        NodeIdComparer.Instance)
                    .ToImmutableArray());
        scopes = graph.Scopes.ToDictionary(
            scope => scope.AnalysisScopeIndex);
    }

    public void Create(
        PrismGraphNodeId target,
        ImmutableArray<PrismGraphDependency> dependencies,
        out PrismVerifiedFingerprint structural,
        out PrismVerifiedFingerprint values,
        out PrismVerifiedFingerprint dependency)
    {
        HashSet<PrismGraphNodeId> ancestors =
            CollectAncestors(target);
        PrismGraphNode[] orderedNodes = graph.Nodes
            .Where(node => ancestors.Contains(node.Id))
            .OrderBy(node => node.Id, NodeIdComparer.Instance)
            .ToArray();
        PrismGraphEdge[] orderedEdges = graph.Edges
            .Where(edge =>
                ancestors.Contains(edge.Source) &&
                ancestors.Contains(edge.Target))
            .OrderBy(edge => edge.Target, NodeIdComparer.Instance)
            .ThenBy(edge => edge.Kind)
            .ThenBy(edge => edge.Source, NodeIdComparer.Instance)
            .ToArray();

        List<long> structuralComponents = [1, orderedNodes.Length];
        foreach (PrismGraphNode node in orderedNodes)
        {
            AppendNodeId(structuralComponents, node.Id);
            structuralComponents.Add(node.AnalysisScopeIndex);
            structuralComponents.Add(
                node.DefinitionNodeId is PrismNodeId definitionId
                    ? 1
                    : 0);
            structuralComponents.Add(
                node.DefinitionNodeId?.Value ?? 0);
            structuralComponents.Add(node.DefinitionOrder);
            structuralComponents.Add(
                node.IsIsolationBoundary ? 1 : 0);
        }
        structuralComponents.Add(orderedEdges.Length);
        foreach (PrismGraphEdge edge in orderedEdges)
        {
            AppendNodeId(structuralComponents, edge.Source);
            AppendNodeId(structuralComponents, edge.Target);
            structuralComponents.Add((long)edge.Kind);
        }

        List<long> valueComponents = [1];
        int[] scopeIndexes = orderedNodes
            .Select(node => node.AnalysisScopeIndex)
            .Distinct()
            .OrderBy(index => index)
            .ToArray();
        valueComponents.Add(scopeIndexes.Length);
        foreach (int scopeIndex in scopeIndexes)
        {
            PrismGraphScope scope = scopes[scopeIndex];
            valueComponents.Add(scopeIndex);
            valueComponents.Add(
                (long)scope.CompositionSettings
                    .WorkingColorProfile);
            AppendFloat(
                valueComponents,
                scope.CompositionSettings.GlobalLightAngle);
            AppendFloat(
                valueComponents,
                scope.CompositionSettings.GlobalLightAltitude);
            AppendRect(valueComponents, scope.Bounds);
            AppendRect(valueComponents, scope.ControlBounds);
        }
        valueComponents.Add(orderedNodes.Length);
        foreach (PrismGraphNode node in orderedNodes)
        {
            AppendNodeValues(
                valueComponents,
                node,
                scopes[node.AnalysisScopeIndex]);
        }

        List<long> dependencyComponents =
            [1, dependencies.Length];
        foreach (PrismGraphDependency item in dependencies)
        {
            dependencyComponents.Add((long)item.Kind);
            dependencyComponents.Add(item.Key);
            dependencyComponents.Add(item.Version);
        }

        structural = new PrismVerifiedFingerprint(
            structuralComponents.ToImmutableArray());
        values = new PrismVerifiedFingerprint(
            valueComponents.ToImmutableArray());
        dependency = new PrismVerifiedFingerprint(
            dependencyComponents.ToImmutableArray());
    }

    private HashSet<PrismGraphNodeId> CollectAncestors(
        PrismGraphNodeId target)
    {
        HashSet<PrismGraphNodeId> result = [];
        Stack<PrismGraphNodeId> pending = new();
        pending.Push(target);
        while (pending.TryPop(out PrismGraphNodeId nodeId))
        {
            if (!result.Add(nodeId) ||
                !incoming.TryGetValue(
                    nodeId,
                    out ImmutableArray<PrismGraphEdge> inputs))
            {
                continue;
            }

            foreach (PrismGraphEdge input in inputs)
            {
                pending.Push(input.Source);
            }
        }

        return result;
    }

    private static void AppendNodeValues(
        List<long> components,
        PrismGraphNode node,
        PrismGraphScope scope)
    {
        AppendNodeId(components, node.Id);
        AppendNullableEnum(components, node.BlendMode);
        AppendNullableFloat(components, node.Amount);
        AppendNullableEnum(components, node.Filter);
        AppendNullableEnum(components, node.Style);
        AppendResource(components, node.Resource, scope);
        AppendNullableEnum(components, node.ColorProfile);
        AppendNullableEnum(components, node.MaskChannel);
        AppendNullableFloat(components, node.Feather);
        AppendNullableFloat(components, node.Density);
        AppendNullableBoolean(components, node.Invert);
        AppendNullableEnum(components, node.MaskPass);
        AppendLayerSettings(components, node.LayerSettings);

        components.Add(node.Parameters.Length);
        foreach (PrismGraphParameter parameter in
            node.Parameters.OrderBy(parameter => parameter.Index))
        {
            components.Add(parameter.Index);
            components.Add((long)parameter.Kind);
            switch (parameter.Kind)
            {
                case PrismGraphParameterValueKind.Boolean:
                    components.Add(parameter.BooleanValue ? 1 : 0);
                    break;
                case PrismGraphParameterValueKind.Integer:
                case PrismGraphParameterValueKind.Symbol:
                    components.Add(parameter.IntegerValue);
                    break;
                case PrismGraphParameterValueKind.Number:
                    AppendFloat(components, parameter.NumberValue);
                    break;
                case PrismGraphParameterValueKind.Color:
                    components.Add(PackColor(parameter.ColorValue));
                    break;
                case PrismGraphParameterValueKind.Vector:
                    AppendVector(components, parameter.VectorValue);
                    break;
                case PrismGraphParameterValueKind.Resource:
                    AppendResource(
                        components,
                        parameter.ResourceValue,
                        scope);
                    break;
                default:
                    throw new InvalidOperationException(
                        $"Unknown Prism graph parameter kind '{parameter.Kind}'.");
            }
        }

        AppendPreparedPlan(components, node, scope);
        AppendBackdrop(components, node);
    }

    private static void AppendPreparedPlan(
        List<long> components,
        PrismGraphNode node,
        PrismGraphScope scope)
    {
        if (node.NeighborhoodPlan is PrismNeighborhoodPlan neighborhood)
        {
            components.Add(1);
            components.Add((long)neighborhood.Filter);
            components.Add((long)neighborhood.Operation);
            components.Add((long)neighborhood.BlendMode);
            components.Add(node.NeighborhoodPassIndex);
            PrismNeighborhoodPass pass =
                neighborhood.Passes[node.NeighborhoodPassIndex];
            components.Add((long)pass.Kind);
            AppendFloat(components, pass.RadiusX);
            AppendFloat(components, pass.RadiusY);
            AppendFloat(components, pass.BoundsRadiusX);
            AppendFloat(components, pass.BoundsRadiusY);
            components.Add(pass.SampleCount);
            components.Add(pass.IsNoOp ? 1 : 0);
            AppendVector(components, neighborhood.Options0);
            AppendVector(components, neighborhood.Options1);
            AppendVector(components, neighborhood.Options2);
            AppendVector(components, neighborhood.Options3);
            AppendResource(
                components,
                neighborhood.Resource,
                scope);
            components.Add(
                neighborhood.ResourceRequired ? 1 : 0);
            return;
        }

        if (node.ResamplingPlan is PrismResamplingPlan resampling)
        {
            components.Add(2);
            components.Add((long)resampling.Filter);
            components.Add((long)resampling.Operation);
            components.Add((long)resampling.BlendMode);
            components.Add(node.ResamplingPassIndex);
            PrismResamplingPass pass =
                resampling.Passes[node.ResamplingPassIndex];
            components.Add((long)pass.Kind);
            components.Add(pass.IsNoOp ? 1 : 0);
            AppendVector(components, resampling.Options0);
            AppendVector(components, resampling.Options1);
            AppendVector(components, resampling.Options2);
            AppendVector(components, resampling.Options3);
            AppendVector(components, resampling.Options4);
            AppendVector(components, resampling.Options5);
            AppendResource(
                components,
                resampling.PrimaryResource,
                scope);
            components.Add(
                resampling.PrimaryResourceRequired ? 1 : 0);
            AppendResource(
                components,
                resampling.AuxiliaryResource,
                scope);
            components.Add(
                resampling.AuxiliaryResourceRequired ? 1 : 0);
            components.Add(resampling.TransformsBounds ? 1 : 0);
            AppendVector(components, resampling.BoundsTranslation);
            AppendVector(components, resampling.BoundsScale);
            AppendFloat(components, resampling.BoundsRotation);
            AppendVector(components, resampling.BoundsSkew);
            AppendVector(components, resampling.BoundsOrigin);
            return;
        }

        if (node.CatalogFilterPlan is PrismCatalogFilterPlan catalog)
        {
            components.Add(3);
            components.Add((long)catalog.Filter);
            components.Add((long)catalog.Primitive);
            components.Add((long)catalog.BlendMode);
            components.Add(node.CatalogFilterPassIndex);
            PrismCatalogFilterPass pass =
                catalog.Passes[node.CatalogFilterPassIndex];
            components.Add((long)pass.Kind);
            AppendFloat(components, pass.RadiusX);
            AppendFloat(components, pass.RadiusY);
            AppendFloat(components, pass.BoundsRadiusX);
            AppendFloat(components, pass.BoundsRadiusY);
            components.Add(pass.Iteration);
            components.Add(pass.IsNoOp ? 1 : 0);
            for (int index = 0; index < 9; index++)
            {
                AppendVector(
                    components,
                    catalog.GetOption(index));
            }
            AppendResource(
                components,
                catalog.PrimaryResource,
                scope);
            components.Add(
                catalog.PrimaryResourceRequired ? 1 : 0);
            AppendResource(
                components,
                catalog.AuxiliaryResource,
                scope);
            components.Add(
                catalog.AuxiliaryResourceRequired ? 1 : 0);
            return;
        }

        components.Add(0);
    }

    private static void AppendLayerSettings(
        List<long> components,
        PrismGraphLayerSettings? settings)
    {
        if (settings is not PrismGraphLayerSettings value)
        {
            components.Add(0);
            return;
        }

        components.Add(1);
        components.Add((long)value.BlendChannels);
        components.Add((long)value.Knockout);
        components.Add(
            value.BlendInteriorStylesAsGroup ? 1 : 0);
        components.Add(
            value.BlendClippedLayersAsGroup ? 1 : 0);
        components.Add(
            value.TransparencyShapesLayer ? 1 : 0);
        components.Add(value.LayerMaskHidesStyles ? 1 : 0);
        components.Add(value.VectorMaskHidesStyles ? 1 : 0);
        components.Add((long)value.BlendIfChannel);
        AppendBlendRange(components, value.ThisLayerRange);
        AppendBlendRange(components, value.UnderlyingRange);
        components.Add(value.DissolveSeed);
    }

    private static void AppendBackdrop(
        List<long> components,
        PrismGraphNode node)
    {
        if (node.BackdropMetadata is BackdropFrameMetadata metadata)
        {
            components.Add(1);
            components.Add(metadata.PixelWidth);
            components.Add(metadata.PixelHeight);
            AppendFloat(components, metadata.PixelScale);
            components.Add((long)metadata.ColorProfile);
            components.Add((long)metadata.PixelFormat);
            components.Add((long)metadata.AlphaMode);
            AppendMatrix(
                components,
                metadata.CoordinateTransform);
            components.Add(metadata.ContentVersion);
        }
        else
        {
            components.Add(0);
        }

        if (node.BackdropSourceBounds is DrawRect sourceBounds)
        {
            components.Add(1);
            AppendRect(components, sourceBounds);
        }
        else
        {
            components.Add(0);
        }
    }

    private static void AppendResource(
        List<long> components,
        PrismResourceId? resource,
        PrismGraphScope scope)
    {
        if (resource is not PrismResourceId id ||
            id.Value <= 0 ||
            !scope.Resources.TryGetDependency(
                id,
                out long identity,
                out long version))
        {
            components.Add(0);
            components.Add(0);
            return;
        }

        components.Add(identity);
        components.Add(version);
    }

    private static void AppendNodeId(
        List<long> components,
        PrismGraphNodeId nodeId)
    {
        components.Add(nodeId.ScopeOwnerToken.Value);
        components.Add(nodeId.DefinitionNodeId);
        components.Add((long)nodeId.Kind);
        components.Add(nodeId.Ordinal);
    }

    private static void AppendRect(
        List<long> components,
        DrawRect rect)
    {
        AppendFloat(components, rect.X);
        AppendFloat(components, rect.Y);
        AppendFloat(components, rect.Width);
        AppendFloat(components, rect.Height);
    }

    private static void AppendMatrix(
        List<long> components,
        Matrix3x2 matrix)
    {
        AppendFloat(components, matrix.M11);
        AppendFloat(components, matrix.M12);
        AppendFloat(components, matrix.M21);
        AppendFloat(components, matrix.M22);
        AppendFloat(components, matrix.M31);
        AppendFloat(components, matrix.M32);
    }

    private static void AppendVector(
        List<long> components,
        Vector2 vector)
    {
        AppendFloat(components, vector.X);
        AppendFloat(components, vector.Y);
    }

    private static void AppendVector(
        List<long> components,
        Vector4 vector)
    {
        AppendFloat(components, vector.X);
        AppendFloat(components, vector.Y);
        AppendFloat(components, vector.Z);
        AppendFloat(components, vector.W);
    }

    private static void AppendBlendRange(
        List<long> components,
        PrismBlendRange range)
    {
        AppendFloat(components, range.BlackStart);
        AppendFloat(components, range.BlackEnd);
        AppendFloat(components, range.WhiteStart);
        AppendFloat(components, range.WhiteEnd);
    }

    private static void AppendNullableFloat(
        List<long> components,
        float? value)
    {
        components.Add(value.HasValue ? 1 : 0);
        if (value.HasValue)
        {
            AppendFloat(components, value.Value);
        }
    }

    private static void AppendNullableBoolean(
        List<long> components,
        bool? value)
    {
        components.Add(value.HasValue ? 1 : 0);
        if (value.HasValue)
        {
            components.Add(value.Value ? 1 : 0);
        }
    }

    private static void AppendNullableEnum<TEnum>(
        List<long> components,
        TEnum? value)
        where TEnum : struct, Enum
    {
        components.Add(value.HasValue ? 1 : 0);
        if (value.HasValue)
        {
            components.Add(Convert.ToInt64(value.Value));
        }
    }

    private static void AppendFloat(
        List<long> components,
        float value)
    {
        components.Add(
            BitConverter.SingleToUInt32Bits(value));
    }

    private static long PackColor(Color color) =>
        color.R |
        ((long)color.G << 8) |
        ((long)color.B << 16) |
        ((long)color.A << 24);

    private sealed class NodeIdComparer :
        IComparer<PrismGraphNodeId>
    {
        public static NodeIdComparer Instance { get; } = new();

        public int Compare(
            PrismGraphNodeId left,
            PrismGraphNodeId right)
        {
            int result = left.ScopeOwnerToken.Value.CompareTo(
                right.ScopeOwnerToken.Value);
            if (result != 0)
            {
                return result;
            }

            result = left.DefinitionNodeId.CompareTo(
                right.DefinitionNodeId);
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
