using System.Collections.Immutable;
using System.Numerics;
using Cerneala.Drawing.Prism.Catalog;
using Cerneala.Drawing.Prism.Styles;
using Cerneala.UI.Prism.Definitions;
using Cerneala.UI.Prism.Runtime;

namespace Cerneala.Drawing.Prism.Graph;

internal sealed class PrismFrameAnalyzer
{
    private const ulong DependencyOffset = 14695981039346656037UL;
    private const ulong DependencyPrime = 1099511628211UL;

    public PrismFrameAnalysis Analyze(DrawCommandList commands)
    {
        ArgumentNullException.ThrowIfNull(commands);

        long commandListVersion = commands.Version;
        int commandCount = commands.Count;
        DrawCommandStateAnalysis stateAnalysis =
            new DrawCommandStateAnalyzer().Analyze(commands);
        List<ScopeBuilder> scopes = [];
        List<OpenScope> openScopes = [];
        ImmutableArray<int>.Builder backdropScopeIndices = ImmutableArray.CreateBuilder<int>();
        PrismGraphCapabilities frameCapabilities = PrismGraphCapabilities.None;
        int frameSurfaceCount = 0;

        for (int commandIndex = 0; commandIndex < commandCount; commandIndex++)
        {
            DrawCommand command = commands[commandIndex];
            switch (command.Kind)
            {
                case DrawCommandKind.BeginPrism:
                {
                    PrismDrawScope scope = command.PrismScope ??
                        throw new InvalidOperationException(
                            $"BeginPrism at command index {commandIndex} has no scope payload.");
                    DrawCommandStateEntry state =
                        stateAnalysis.Entries[commandIndex];
                    Matrix3x2 effectiveTransform = Matrix3x2.Multiply(
                        scope.EffectiveTransform,
                        state.Transform);
                    DrawRect bounds = DrawCommandStateAnalyzer.TransformBounds(
                        scope.ControlBounds,
                        effectiveTransform);
                    if (state.ClipBounds is DrawRect clipBounds)
                    {
                        bounds = DrawCommandStateAnalyzer.Intersect(
                            bounds,
                            clipBounds);
                    }

                    bool requiresBackdrop = RequiresBackdrop(scope, bounds);
                    CapabilityEstimate estimate = EstimateCapabilities(
                        scope.Definition,
                        requiresBackdrop);
                    if (IsEmpty(bounds))
                    {
                        estimate = default;
                    }

                    int scopeIndex = scopes.Count;
                    int? parentScopeIndex = openScopes.Count == 0
                        ? null
                        : openScopes[^1].ScopeIndex;
                    scopes.Add(
                        new ScopeBuilder(
                            scopeIndex,
                            commandIndex,
                            openScopes.Count,
                            parentScopeIndex,
                            scope,
                            bounds,
                            estimate.Capabilities,
                            estimate.RequiredSurfaceCount));
                    openScopes.Add(new OpenScope(scopeIndex));
                    frameCapabilities |= estimate.Capabilities;
                    frameSurfaceCount = checked(frameSurfaceCount + estimate.RequiredSurfaceCount);

                    if (requiresBackdrop)
                    {
                        backdropScopeIndices.Add(scopeIndex);
                    }
                    break;
                }

                case DrawCommandKind.EndPrism:
                    if (openScopes.Count == 0)
                    {
                        throw new InvalidOperationException(
                            $"EndPrism at command index {commandIndex} has no matching BeginPrism.");
                    }

                    OpenScope completed = openScopes[^1];
                    openScopes.RemoveAt(openScopes.Count - 1);
                    ScopeBuilder completedScope = scopes[completed.ScopeIndex];
                    completedScope.EndCommandIndex = commandIndex;
                    completedScope.DescendantVersion = completed.DescendantVersion;
                    if (openScopes.Count > 0)
                    {
                        OpenScope parent = openScopes[^1];
                        parent.DescendantVersion = MixDependency(
                            parent.DescendantVersion,
                            completedScope.CreateDependencyStamp());
                    }
                    break;
            }
        }

        if (openScopes.Count > 0)
        {
            int beginCommandIndex = scopes[openScopes[^1].ScopeIndex].BeginCommandIndex;
            throw new InvalidOperationException(
                $"BeginPrism at command index {beginCommandIndex} has no matching EndPrism.");
        }

        if (commands.Version != commandListVersion || commands.Count != commandCount)
        {
            throw new InvalidOperationException(
                "The draw command list changed while its Prism frame analysis was being built.");
        }

        ImmutableArray<PrismAnalyzedScope>.Builder analyzedScopes =
            ImmutableArray.CreateBuilder<PrismAnalyzedScope>(scopes.Count);
        foreach (ScopeBuilder scope in scopes)
        {
            analyzedScopes.Add(scope.Build());
        }

        PrismBackdropRequirement? backdropRequirement = backdropScopeIndices.Count == 0
            ? null
            : new PrismBackdropRequirement(backdropScopeIndices.ToImmutable());
        PrismFrameAnalysis analysis = new(
            commands,
            commandListVersion,
            analyzedScopes.MoveToImmutable(),
            frameCapabilities,
            frameSurfaceCount,
            backdropRequirement,
            stateAnalysis);
        analysis.EnsureCurrent(commands);
        return analysis;
    }

    private static bool RequiresBackdrop(PrismDrawScope scope, DrawRect bounds)
    {
        return !IsEmpty(bounds) && RequiresBackdrop(
            scope.Definition.Nodes,
            scope.Instance,
            canSeeHostBackdrop: true);
    }

    private static bool RequiresBackdrop(
        IReadOnlyList<PrismNodeDefinition> definitions,
        PrismInstance instance,
        bool canSeeHostBackdrop)
    {
        foreach (PrismNodeDefinition definition in definitions)
        {
            switch (definition)
            {
                case PrismLayerDefinition layer:
                {
                    PrismLayerState state = instance.GetLayerState(layer.Id);
                    if (!state.Visible || state.Opacity <= 0)
                    {
                        continue;
                    }
                    if (canSeeHostBackdrop &&
                        (state.BlendMode != PrismBlendMode.Normal ||
                            StylesRequireBackdrop(state.Styles)))
                    {
                        return true;
                    }
                    break;
                }

                case PrismGroupDefinition group:
                {
                    PrismGroupState state = instance.GetGroupState(group.Id);
                    if (!state.Visible || state.Opacity <= 0)
                    {
                        continue;
                    }

                    bool passThrough =
                        state.BlendMode == PrismBlendMode.PassThrough;
                    if (canSeeHostBackdrop &&
                        ((!passThrough &&
                            state.BlendMode != PrismBlendMode.Normal) ||
                            StylesRequireBackdrop(state.Styles)))
                    {
                        return true;
                    }
                    if (RequiresBackdrop(
                        group.Children,
                        instance,
                        canSeeHostBackdrop && passThrough))
                    {
                        return true;
                    }
                    break;
                }
            }
        }

        return false;
    }

    private static bool StylesRequireBackdrop(
        IReadOnlyList<PrismStyleState> styles)
    {
        foreach (PrismStyleState state in styles)
        {
            if (!state.Visible)
            {
                continue;
            }

            PrismCatalogEntryDescriptor entry =
                PrismCatalogRuntime.GetEntry((int)state.Style);
            foreach (PrismCatalogPropertyDescriptor property in
                entry.Properties)
            {
                if (property.ValueType != PrismCatalogValueType.Symbol ||
                    !property.Name.EndsWith(
                        "BlendMode",
                        StringComparison.Ordinal))
                {
                    continue;
                }

                string opacityName = property.Name[
                    ..^"BlendMode".Length] + "Opacity";
                PrismCatalogPropertyDescriptor? opacityProperty =
                    entry.Properties.FirstOrDefault(candidate =>
                        candidate.ValueType == PrismCatalogValueType.Number &&
                        string.Equals(
                            candidate.Name,
                            opacityName,
                            StringComparison.Ordinal));
                if (opacityProperty is PrismCatalogPropertyDescriptor visibleOpacity &&
                    state.GetValue(
                        new PrismParameterKey<float>(
                            entry.StableId,
                            visibleOpacity.TypeSlot)) <= 0)
                {
                    continue;
                }

                int value = state.GetValue(
                    new PrismParameterKey<int>(
                        entry.StableId,
                        property.TypeSlot));
                if (PrismStylePlanner.ResolveBlendMode(
                        value,
                        property.Name) != PrismBlendMode.Normal)
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static CapabilityEstimate EstimateCapabilities(
        PrismCompositionDefinition definition,
        bool includeBackdrop)
    {
        PrismGraphCapabilities capabilities =
            PrismGraphCapabilities.ControlCapture |
            PrismGraphCapabilities.ColorConversion;
        int surfaceCount = 1;
        if (includeBackdrop)
        {
            capabilities |= PrismGraphCapabilities.BackdropInput;
            surfaceCount = checked(surfaceCount + 4);
        }
        foreach (PrismNodeDefinition node in definition.Nodes)
        {
            EstimateNode(
                node,
                includeBackdrop,
                ref capabilities,
                ref surfaceCount);
        }

        return new CapabilityEstimate(capabilities, surfaceCount);
    }

    private static void EstimateNode(
        PrismNodeDefinition node,
        bool includeBackdrop,
        ref PrismGraphCapabilities capabilities,
        ref int surfaceCount)
    {
        switch (node)
        {
            case PrismLayerDefinition layer:
                surfaceCount = checked(surfaceCount + 2);
                EstimateOperations(
                    layer.Filters.Length,
                    layer.Styles.Length,
                    layer.Mask is not null,
                    ref capabilities,
                    ref surfaceCount);
                if (layer.ClipToBelow)
                {
                    capabilities |= PrismGraphCapabilities.Clipping;
                    surfaceCount = checked(surfaceCount + 1);
                }
                if (layer.BlendMode != PrismBlendMode.Normal)
                {
                    capabilities |= PrismGraphCapabilities.AdvancedBlending;
                }
                break;

            case PrismGroupDefinition group:
                capabilities |= PrismGraphCapabilities.GroupProcessing;
                surfaceCount = checked(surfaceCount + 1);
                if (group.BlendMode != PrismBlendMode.PassThrough)
                {
                    capabilities |= PrismGraphCapabilities.GroupIsolation;
                    surfaceCount = checked(surfaceCount + 1);
                }
                if (group.BlendMode is not PrismBlendMode.Normal and not PrismBlendMode.PassThrough)
                {
                    capabilities |= PrismGraphCapabilities.AdvancedBlending;
                }
                foreach (PrismNodeDefinition child in group.Children)
                {
                    EstimateNode(
                        child,
                        includeBackdrop,
                        ref capabilities,
                        ref surfaceCount);
                }
                EstimateOperations(
                    group.Filters.Length,
                    group.Styles.Length,
                    group.Mask is not null,
                    ref capabilities,
                    ref surfaceCount);
                break;

        }
    }

    private static void EstimateOperations(
        int filterCount,
        int styleCount,
        bool hasMask,
        ref PrismGraphCapabilities capabilities,
        ref int surfaceCount)
    {
        if (filterCount > 0)
        {
            capabilities |= PrismGraphCapabilities.FilterProcessing;
            surfaceCount = checked(surfaceCount + filterCount);
        }
        if (styleCount > 0)
        {
            capabilities |= PrismGraphCapabilities.StyleProcessing;
            surfaceCount = checked(surfaceCount + styleCount);
        }
        if (hasMask)
        {
            capabilities |= PrismGraphCapabilities.MaskProcessing;
            surfaceCount = checked(surfaceCount + 1);
        }
    }

    private static bool IsEmpty(DrawRect bounds) =>
        bounds.Width <= 0 || bounds.Height <= 0;

    private static long MixDependency(long aggregate, PrismDependencyStamp stamp)
    {
        ulong hash = aggregate == 0 ? DependencyOffset : unchecked((ulong)aggregate);
        hash = Mix(hash, stamp.CacheOwnerToken.Value);
        hash = Mix(hash, stamp.StructuralVersion.Value);
        hash = Mix(hash, stamp.ValueVersion.Value);
        hash = Mix(hash, stamp.VisualContentVersion);
        hash = Mix(hash, stamp.DescendantVersion);
        return unchecked((long)hash);
    }

    private static ulong Mix(ulong hash, long value) =>
        unchecked((hash ^ (ulong)value) * DependencyPrime);

    private readonly record struct CapabilityEstimate(
        PrismGraphCapabilities Capabilities,
        int RequiredSurfaceCount);

    private sealed class OpenScope
    {
        public OpenScope(int scopeIndex)
        {
            ScopeIndex = scopeIndex;
        }

        public int ScopeIndex { get; }

        public long DescendantVersion { get; set; }
    }

    private sealed class ScopeBuilder
    {
        public ScopeBuilder(
            int scopeIndex,
            int beginCommandIndex,
            int depth,
            int? parentScopeIndex,
            PrismDrawScope scope,
            DrawRect bounds,
            PrismGraphCapabilities requiredCapabilities,
            int requiredSurfaceCount)
        {
            ScopeIndex = scopeIndex;
            BeginCommandIndex = beginCommandIndex;
            Depth = depth;
            ParentScopeIndex = parentScopeIndex;
            Scope = scope;
            Bounds = bounds;
            RequiredCapabilities = requiredCapabilities;
            RequiredSurfaceCount = requiredSurfaceCount;
        }

        public int ScopeIndex { get; }

        public int BeginCommandIndex { get; }

        public int EndCommandIndex { get; set; } = -1;

        public int Depth { get; }

        public int? ParentScopeIndex { get; }

        public PrismDrawScope Scope { get; }

        public DrawRect Bounds { get; }

        public PrismGraphCapabilities RequiredCapabilities { get; }

        public int RequiredSurfaceCount { get; }

        public long DescendantVersion { get; set; }

        public PrismDependencyStamp CreateDependencyStamp() =>
            new(
                Scope.CacheOwnerToken,
                Scope.StructuralVersion,
                Scope.ValueVersion,
                Scope.VisualContentVersion,
                DescendantVersion);

        public PrismAnalyzedScope Build() =>
            new(
                ScopeIndex,
                BeginCommandIndex,
                EndCommandIndex,
                Depth,
                ParentScopeIndex,
                Scope,
                Bounds,
                CreateDependencyStamp(),
                RequiredCapabilities,
                RequiredSurfaceCount);
    }
}
