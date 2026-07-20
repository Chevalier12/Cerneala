using System.Collections.Immutable;
using System.Numerics;
using Cerneala.Drawing.Prism.Catalog;
using Cerneala.Drawing.Prism.Filters;
using Cerneala.UI.Prism.Definitions;
using Cerneala.UI.Prism.Runtime;

namespace Cerneala.Drawing.Prism.Graph;

public sealed class PrismGraphBuilder
{
    public PrismGraph Build(PrismFrameAnalysis analysis)
    {
        ArgumentNullException.ThrowIfNull(analysis);
        EnsureCurrent(analysis);

        ImmutableArray<PrismGraphNode>.Builder nodes =
            ImmutableArray.CreateBuilder<PrismGraphNode>();
        ImmutableArray<PrismGraphEdge>.Builder edges =
            ImmutableArray.CreateBuilder<PrismGraphEdge>();
        ImmutableArray<PrismGraphScope>.Builder scopes =
            ImmutableArray.CreateBuilder<PrismGraphScope>(analysis.Scopes.Length);
        HashSet<PrismGraphNodeId> nodeIds = [];

        foreach (PrismAnalyzedScope analyzedScope in analysis.Scopes)
        {
            ScopeBuilder scopeBuilder = new(analyzedScope, nodes, edges, nodeIds);
            scopes.Add(scopeBuilder.Build());
        }

        EnsureCurrent(analysis);
        return new PrismGraph(nodes.ToImmutable(), edges.ToImmutable(), scopes.MoveToImmutable());
    }

    private static void EnsureCurrent(PrismFrameAnalysis analysis)
    {
        try
        {
            analysis.EnsureCurrent();
        }
        catch (InvalidOperationException exception)
        {
            PrismCompositionDefinition? definition = null;
            if (analysis.GetStaleScopeIndex() is int scopeIndex)
            {
                foreach (PrismAnalyzedScope analyzedScope in analysis.Scopes)
                {
                    if (analyzedScope.ScopeIndex == scopeIndex)
                    {
                        definition = analyzedScope.Scope.Definition;
                        break;
                    }
                }
            }
            PrismGraphDiagnostic diagnostic = new(
                definition?.Name ?? "<frame>",
                null,
                null,
                definition?.SourceSpan,
                exception.Message);
            throw new PrismGraphBuildException(diagnostic, exception);
        }
    }

    private sealed class ScopeBuilder
    {
        private const int MaskCompositeOrdinal = 0;
        private const int StackCompositeOrdinal = 1;
        private const int BackdropCompositeOrdinal = 2;

        private readonly PrismAnalyzedScope analyzedScope;
        private readonly PrismCompositionDefinition definition;
        private readonly PrismInstance instance;
        private readonly ImmutableArray<PrismGraphNode>.Builder nodes;
        private readonly ImmutableArray<PrismGraphEdge>.Builder edges;
        private readonly HashSet<PrismGraphNodeId> nodeIds;
        private readonly Dictionary<PrismNodeId, int> definitionOrders = [];
        private readonly Dictionary<PrismNodeId, string> diagnosticNames = [];
        private readonly ImmutableArray<PrismGraphDependency> scopeDependencies;
        private PrismGraphNodeId controlSource;

        public ScopeBuilder(
            PrismAnalyzedScope analyzedScope,
            ImmutableArray<PrismGraphNode>.Builder nodes,
            ImmutableArray<PrismGraphEdge>.Builder edges,
            HashSet<PrismGraphNodeId> nodeIds)
        {
            this.analyzedScope = analyzedScope;
            definition = analyzedScope.Scope.Definition;
            instance = analyzedScope.Scope.Instance;
            this.nodes = nodes;
            this.edges = edges;
            this.nodeIds = nodeIds;
            IndexDefinitions();
            scopeDependencies = CreateScopeDependencies();
        }

        public PrismGraphScope Build()
        {
            PrismGraphCompositionSettings compositionSettings =
                SnapshotCompositionSettings();
            if (analyzedScope.Bounds.Width <= 0 || analyzedScope.Bounds.Height <= 0)
            {
                return new PrismGraphScope(
                    analyzedScope.ScopeIndex,
                    analyzedScope.BeginCommandIndex,
                    analyzedScope.EndCommandIndex,
                    analyzedScope.Depth,
                    analyzedScope.ParentScopeIndex,
                    analyzedScope.DependencyStamp.CacheOwnerToken,
                    compositionSettings,
                    analyzedScope.Bounds,
                    analyzedScope.Scope.ControlBounds,
                    analyzedScope.Scope.EffectiveTransform,
                    analyzedScope.Scope.PixelScale,
                    analyzedScope.Scope.Resources,
                    null);
            }

            PrismColorProfile colorProfile = compositionSettings.WorkingColorProfile;

            PrismGraphNode capture = AddNode(
                PrismGraphNodeKind.ControlCapture,
                definitionNodeId: null,
                ordinal: 0,
                definitionOrder: -1,
                diagnosticName: $"{definition.Name}/control",
                dependencies: Dependencies(
                    new PrismGraphDependency(
                        PrismGraphDependencyKind.VisualContent,
                        analyzedScope.DependencyStamp.CacheOwnerToken.Value,
                        analyzedScope.DependencyStamp.VisualContentVersion),
                    new PrismGraphDependency(
                        PrismGraphDependencyKind.Bounds,
                        analyzedScope.DependencyStamp.CacheOwnerToken.Value,
                        StableBoundsHash(analyzedScope.Bounds)),
                    new PrismGraphDependency(
                        PrismGraphDependencyKind.PixelScale,
                        analyzedScope.DependencyStamp.CacheOwnerToken.Value,
                        BitConverter.SingleToInt32Bits(analyzedScope.Scope.PixelScale)),
                    new PrismGraphDependency(
                        PrismGraphDependencyKind.Transform,
                        analyzedScope.DependencyStamp.CacheOwnerToken.Value,
                        StableTransformHash(analyzedScope.Scope.EffectiveTransform))));
            PrismGraphNode conversion = AddNode(
                PrismGraphNodeKind.ColorConversion,
                definitionNodeId: null,
                ordinal: 0,
                definitionOrder: -1,
                diagnosticName: $"{definition.Name}/control-color",
                dependencies: Dependencies(
                    new PrismGraphDependency(
                        PrismGraphDependencyKind.ColorProfile,
                        analyzedScope.DependencyStamp.CacheOwnerToken.Value,
                        (int)colorProfile)),
                colorProfile: colorProfile);
            AddEdge(capture.Id, conversion.Id, PrismGraphEdgeKind.Content);
            controlSource = conversion.Id;

            PrismGraphNodeId? content =
                BuildStack(definition.Nodes, excludeBackdrop: true).Output;
            PrismGraphNodeId? backdrop = BuildBackdrop();
            PrismGraphNodeId? output = CombineBackdrop(backdrop, content);
            return new PrismGraphScope(
                analyzedScope.ScopeIndex,
                analyzedScope.BeginCommandIndex,
                analyzedScope.EndCommandIndex,
                analyzedScope.Depth,
                analyzedScope.ParentScopeIndex,
                analyzedScope.DependencyStamp.CacheOwnerToken,
                compositionSettings,
                analyzedScope.Bounds,
                analyzedScope.Scope.ControlBounds,
                analyzedScope.Scope.EffectiveTransform,
                analyzedScope.Scope.PixelScale,
                analyzedScope.Scope.Resources,
                output);
        }

        private StackBuildResult BuildStack(
            IReadOnlyList<PrismNodeDefinition> definitions,
            bool excludeBackdrop = false,
            PrismGraphNodeId? initialBackground = null)
        {
            PrismGraphNodeId? current = initialBackground;
            PrismGraphNodeId? clipBaseAlpha = null;
            bool hasClipBase = false;
            bool hasContent = false;
            int start = definitions.Count - 1;
            if (excludeBackdrop &&
                start >= 0 &&
                definitions[start] is PrismBackdropDefinition)
            {
                start--;
            }

            for (int index = start; index >= 0; index--)
            {
                PrismNodeDefinition nodeDefinition = definitions[index];
                NodeDisposition disposition = GetNodeDisposition(nodeDefinition);
                if (!disposition.IsRenderable)
                {
                    if (!disposition.ClipToBelow)
                    {
                        hasClipBase = true;
                        clipBaseAlpha = null;
                    }
                    continue;
                }
                if (disposition.ClipToBelow)
                {
                    if (!hasClipBase)
                    {
                        throw Failure(
                            nodeDefinition,
                            "A clipping layer has no non-clipping base below it.");
                    }
                    if (clipBaseAlpha is null)
                    {
                        continue;
                    }
                }

                NodeBuildResult? result = BuildNode(nodeDefinition, current);
                if (result is null)
                {
                    if (!disposition.ClipToBelow)
                    {
                        hasClipBase = true;
                        clipBaseAlpha = null;
                    }
                    continue;
                }

                NodeBuildResult built = result.Value;
                PrismGraphNodeId foreground = built.Output;
                if (built.ClipToBelow)
                {
                    if (clipBaseAlpha is not PrismGraphNodeId baseAlpha)
                    {
                        throw Failure(
                            nodeDefinition,
                            "A clipping layer has no renderable base alpha.");
                    }
                    PrismGraphNode clip = AddNode(
                        PrismGraphNodeKind.ClipToBelow,
                        nodeDefinition.Id,
                        ordinal: 0,
                        definitionOrders[nodeDefinition.Id],
                        $"{diagnosticNames[nodeDefinition.Id]}/clip",
                        Dependencies());
                    AddEdge(foreground, clip.Id, PrismGraphEdgeKind.Content);
                    AddEdge(baseAlpha, clip.Id, PrismGraphEdgeKind.ClipBaseAlpha);
                    foreground = clip.Id;
                }
                else
                {
                    clipBaseAlpha = built.Alpha;
                    hasClipBase = true;
                }

                if (built.ConsumesBackground)
                {
                    current = foreground;
                    hasContent = true;
                    continue;
                }

                PrismGraphNode composite = AddNode(
                    PrismGraphNodeKind.Composite,
                    nodeDefinition.Id,
                    StackCompositeOrdinal,
                    definitionOrders[nodeDefinition.Id],
                    $"{diagnosticNames[nodeDefinition.Id]}/composite",
                    Dependencies(),
                    blendMode: built.BlendMode,
                    layerSettings: built.LayerSettings);
                if (current is not null)
                {
                    AddEdge(
                        current.Value,
                        composite.Id,
                        PrismGraphEdgeKind.CompositeBackground);
                }
                AddEdge(
                    foreground,
                    composite.Id,
                    PrismGraphEdgeKind.CompositeForeground);
                current = composite.Id;
                hasContent = true;
            }

            return new StackBuildResult(current, hasContent);
        }

        private NodeBuildResult? BuildNode(
            PrismNodeDefinition nodeDefinition,
            PrismGraphNodeId? background)
        {
            try
            {
                return nodeDefinition switch
                {
                    PrismLayerDefinition layer => BuildLayer(layer),
                    PrismGroupDefinition group => BuildGroup(group, background),
                    PrismBackdropDefinition => throw new InvalidOperationException(
                        "A backdrop cannot appear inside a content stack."),
                    _ => throw new InvalidOperationException(
                        $"Unsupported definition type '{nodeDefinition.GetType().Name}'.")
                };
            }
            catch (PrismGraphBuildException)
            {
                throw;
            }
            catch (Exception exception) when (
                exception is InvalidOperationException or
                ArgumentException or
                KeyNotFoundException)
            {
                throw Failure(nodeDefinition, exception.Message, exception);
            }
        }

        private NodeBuildResult? BuildLayer(PrismLayerDefinition layer)
        {
            PrismLayerState state = instance.GetLayerState(layer.Id);
            ValidateBlendMode(state.BlendMode, allowPassThrough: false);
            if (!state.Visible || state.Opacity <= 0)
            {
                return null;
            }

            PrismGraphLayerSettings layerSettings =
                SnapshotLayerSettings(state);
            PrismGraphNode layerNode = AddNode(
                PrismGraphNodeKind.Layer,
                layer.Id,
                ordinal: 0,
                definitionOrders[layer.Id],
                diagnosticNames[layer.Id],
                Dependencies(),
                blendMode: state.BlendMode,
                layerSettings: layerSettings);
            AddEdge(controlSource, layerNode.Id, PrismGraphEdgeKind.Control);

            PrismGraphNodeId preparedContent = ApplyFilters(
                layer.Id,
                state.Filters,
                layerNode.Id);
            PrismGraphNode fill = AddNode(
                PrismGraphNodeKind.Fill,
                layer.Id,
                ordinal: 0,
                definitionOrders[layer.Id],
                $"{diagnosticNames[layer.Id]}/fill",
                Dependencies(),
                amount: state.Fill);
            AddEdge(
                preparedContent,
                fill.Id,
                PrismGraphEdgeKind.Content);
            PrismGraphNodeId current = ApplyStyles(
                layer.Id,
                state.Styles,
                fill.Id,
                preparedContent);
            current = ApplyMask(layer, state.Mask, current);
            PrismGraphNode opacity = AddNode(
                PrismGraphNodeKind.Opacity,
                layer.Id,
                ordinal: 0,
                definitionOrders[layer.Id],
                $"{diagnosticNames[layer.Id]}/opacity",
                Dependencies(),
                amount: state.Opacity);
            AddEdge(current, opacity.Id, PrismGraphEdgeKind.Content);
            return new NodeBuildResult(
                opacity.Id,
                opacity.Id,
                state.BlendMode,
                state.ClipToBelow,
                ConsumesBackground: false,
                layerSettings);
        }

        private NodeBuildResult? BuildGroup(
            PrismGroupDefinition group,
            PrismGraphNodeId? background)
        {
            PrismGroupState state = instance.GetGroupState(group.Id);
            ValidateBlendMode(state.BlendMode, allowPassThrough: true);
            if (!state.Visible || state.Opacity <= 0)
            {
                return null;
            }

            bool isPassThrough = state.BlendMode == PrismBlendMode.PassThrough;
            StackBuildResult childStack = BuildStack(
                group.Children,
                initialBackground: isPassThrough ? background : null);
            if (!childStack.HasContent || childStack.Output is null)
            {
                return null;
            }

            PrismGraphNode groupNode = AddNode(
                PrismGraphNodeKind.Group,
                group.Id,
                ordinal: 0,
                definitionOrders[group.Id],
                diagnosticNames[group.Id],
                Dependencies(),
                isIsolationBoundary: !isPassThrough,
                blendMode: state.BlendMode);
            AddEdge(childStack.Output.Value, groupNode.Id, PrismGraphEdgeKind.GroupContent);
            if (isPassThrough && background is not null)
            {
                AddEdge(
                    background.Value,
                    groupNode.Id,
                    PrismGraphEdgeKind.CompositeBackground);
            }

            PrismGraphNodeId preparedContent =
                ApplyFilters(
                    group.Id,
                    state.Filters,
                    groupNode.Id);
            PrismGraphNodeId current = ApplyStyles(
                group.Id,
                state.Styles,
                preparedContent,
                preparedContent);
            current = ApplyMask(group, state.Mask, current);
            PrismGraphNode opacity = AddNode(
                PrismGraphNodeKind.Opacity,
                group.Id,
                ordinal: 0,
                definitionOrders[group.Id],
                $"{diagnosticNames[group.Id]}/opacity",
                Dependencies(),
                amount: state.Opacity);
            AddEdge(current, opacity.Id, PrismGraphEdgeKind.Content);
            PrismGraphNodeId output = opacity.Id;
            if (isPassThrough)
            {
                PrismGraphNode passThroughComposite = AddNode(
                    PrismGraphNodeKind.PassThroughComposite,
                    group.Id,
                    ordinal: 0,
                    definitionOrders[group.Id],
                    $"{diagnosticNames[group.Id]}/pass-through-composite",
                    Dependencies(),
                    blendMode: PrismBlendMode.PassThrough,
                    amount: state.Opacity);
                AddEdge(
                    groupNode.Id,
                    passThroughComposite.Id,
                    PrismGraphEdgeKind.GroupContent);
                AddEdge(
                    opacity.Id,
                    passThroughComposite.Id,
                    PrismGraphEdgeKind.CompositeForeground);
                if (background is not null)
                {
                    AddEdge(
                        background.Value,
                        passThroughComposite.Id,
                        PrismGraphEdgeKind.CompositeBackground);
                }
                output = passThroughComposite.Id;
            }

            return new NodeBuildResult(
                output,
                output,
                state.BlendMode,
                ClipToBelow: false,
                ConsumesBackground: isPassThrough,
                LayerSettings: null);
        }

        private PrismGraphNodeId? BuildBackdrop()
        {
            PrismBackdropDefinition? backdropDefinition = definition.Backdrop;
            PrismBackdropState? state = instance.Backdrop;
            if (backdropDefinition is null || state is null)
            {
                return null;
            }

            try
            {
                return BuildBackdrop(backdropDefinition, state);
            }
            catch (PrismGraphBuildException)
            {
                throw;
            }
            catch (Exception exception) when (
                exception is InvalidOperationException or
                ArgumentException or
                KeyNotFoundException)
            {
                throw Failure(backdropDefinition, exception.Message, exception);
            }
        }

        private PrismGraphNodeId? BuildBackdrop(
            PrismBackdropDefinition backdropDefinition,
            PrismBackdropState state)
        {
            if (!state.Visible || state.Opacity <= 0)
            {
                return null;
            }

            PrismGraphNode input = AddNode(
                PrismGraphNodeKind.BackdropInput,
                backdropDefinition.Id,
                ordinal: 0,
                definitionOrders[backdropDefinition.Id],
                diagnosticNames[backdropDefinition.Id],
                Dependencies());
            PrismColorProfile colorProfile = instance.Composition.WorkingColorProfile;
            PrismGraphNode conversion = AddNode(
                PrismGraphNodeKind.ColorConversion,
                backdropDefinition.Id,
                ordinal: 1,
                definitionOrders[backdropDefinition.Id],
                $"{diagnosticNames[backdropDefinition.Id]}/color",
                Dependencies(
                    new PrismGraphDependency(
                        PrismGraphDependencyKind.ColorProfile,
                        analyzedScope.DependencyStamp.CacheOwnerToken.Value,
                        (int)colorProfile)),
                colorProfile: colorProfile);
            AddEdge(input.Id, conversion.Id, PrismGraphEdgeKind.Backdrop);

            PrismGraphNodeId preparedContent = ApplyFilters(
                backdropDefinition.Id,
                state.Filters,
                conversion.Id);
            PrismGraphNodeId current = ApplyStyles(
                backdropDefinition.Id,
                state.Styles,
                preparedContent,
                preparedContent);
            current = ApplyMask(backdropDefinition, state.Mask, current);
            PrismGraphNode opacity = AddNode(
                PrismGraphNodeKind.Opacity,
                backdropDefinition.Id,
                ordinal: 0,
                definitionOrders[backdropDefinition.Id],
                $"{diagnosticNames[backdropDefinition.Id]}/opacity",
                Dependencies(),
                amount: state.Opacity);
            AddEdge(current, opacity.Id, PrismGraphEdgeKind.Content);
            return opacity.Id;
        }

        private PrismGraphNodeId? CombineBackdrop(
            PrismGraphNodeId? backdrop,
            PrismGraphNodeId? content)
        {
            if (backdrop is null)
            {
                return content;
            }
            if (content is null)
            {
                return backdrop;
            }

            PrismBackdropDefinition backdropDefinition = definition.Backdrop!;
            PrismGraphNode composite = AddNode(
                PrismGraphNodeKind.Composite,
                backdropDefinition.Id,
                BackdropCompositeOrdinal,
                definitionOrders[backdropDefinition.Id],
                $"{diagnosticNames[backdropDefinition.Id]}/content-composite",
                Dependencies(),
                blendMode: PrismBlendMode.Normal);
            AddEdge(
                backdrop.Value,
                composite.Id,
                PrismGraphEdgeKind.CompositeBackground);
            AddEdge(
                content.Value,
                composite.Id,
                PrismGraphEdgeKind.CompositeForeground);
            return composite.Id;
        }

        private PrismGraphNodeId ApplyFilters(
            PrismNodeId definitionNodeId,
            IReadOnlyList<PrismFilterState> states,
            PrismGraphNodeId input)
        {
            PrismNodeDefinition nodeDefinition = DefinitionNode(definitionNodeId);
            IReadOnlyList<PrismFilterDefinition> definitions = nodeDefinition switch
            {
                PrismLayerDefinition layer => layer.Filters,
                PrismGroupDefinition group => group.Filters,
                PrismBackdropDefinition backdrop => backdrop.Filters,
                _ => throw new InvalidOperationException("Unsupported filter owner.")
            };
            if (definitions.Count != states.Count)
            {
                throw new InvalidOperationException("Filter definition and runtime state counts differ.");
            }

            PrismGraphNodeId current = input;
            int nextIntermediateOrdinal = states.Count;
            for (int index = 0; index < states.Count; index++)
            {
                PrismFilterState state = states[index];
                ValidateBlendMode(state.BlendMode, allowPassThrough: false);
                if (!state.Visible || state.Opacity <= 0)
                {
                    continue;
                }

                ImmutableArray<PrismGraphParameter> parameters =
                    SnapshotParameters(state.Filter, state);
                PrismNeighborhoodPlan? neighborhoodPlan = null;
                PrismResamplingPlan? resamplingPlan = null;
                PrismCatalogFilterPlan? catalogFilterPlan = null;
                int passCount;
                if (PrismNeighborhoodPlanner.IsSupported(
                        state.Filter))
                {
                    neighborhoodPlan =
                        PrismNeighborhoodPlanner.Create(
                            state.Filter,
                            parameters,
                            state.BlendMode,
                            analyzedScope.Scope.PixelScale,
                            analyzedScope.Scope.EffectiveTransform,
                            analyzedScope.Bounds);
                    passCount =
                        neighborhoodPlan.Value.Passes.Length;
                }
                else if (PrismResamplingPlanner.IsSupported(
                    state.Filter))
                {
                    resamplingPlan =
                        PrismResamplingPlanner.Create(
                            state.Filter,
                            parameters,
                            state.BlendMode,
                            analyzedScope.Scope.PixelScale,
                            analyzedScope.Scope.EffectiveTransform,
                            analyzedScope.Bounds);
                    passCount =
                        resamplingPlan.Value.Passes.Length;
                }
                else if (PrismCatalogFilterPlanner.IsSupported(
                    state.Filter))
                {
                    catalogFilterPlan =
                        PrismCatalogFilterPlanner.Create(
                            state.Filter,
                            parameters,
                            state.BlendMode,
                            analyzedScope.Scope.PixelScale,
                            analyzedScope.Scope.EffectiveTransform,
                            analyzedScope.Bounds);
                    passCount =
                        catalogFilterPlan.Value.Passes.Length;
                }
                else
                {
                    PrismGraphNode filter = AddNode(
                        PrismGraphNodeKind.Filter,
                        definitionNodeId,
                        index,
                        definitionOrders[definitionNodeId],
                        $"{diagnosticNames[definitionNodeId]}/filter[{index}]",
                        DependenciesForCatalogEntry(
                            (int)state.Filter,
                            parameters),
                        parameters,
                        blendMode: state.BlendMode,
                        amount: state.Opacity,
                        filter: state.Filter);
                    AddEdge(
                        current,
                        filter.Id,
                        PrismGraphEdgeKind.Content);
                    current = filter.Id;
                    continue;
                }

                for (int passIndex = 0;
                    passIndex < passCount;
                    passIndex++)
                {
                    bool isFinal =
                        passIndex == passCount - 1;
                    int ordinal = isFinal
                        ? index
                        : nextIntermediateOrdinal++;
                    PrismGraphNode filter = AddNode(
                        PrismGraphNodeKind.Filter,
                        definitionNodeId,
                        ordinal,
                        definitionOrders[definitionNodeId],
                        $"{diagnosticNames[definitionNodeId]}/filter[{index}]/pass[{passIndex}]",
                        DependenciesForCatalogEntry(
                            (int)state.Filter,
                            parameters),
                        parameters,
                        blendMode: isFinal
                            ? state.BlendMode
                            : PrismBlendMode.Normal,
                        amount: isFinal
                            ? state.Opacity
                            : 1,
                        filter: state.Filter,
                        neighborhoodPlan: neighborhoodPlan,
                        neighborhoodPassIndex:
                            neighborhoodPlan is null
                                ? -1
                                : passIndex,
                        resamplingPlan: resamplingPlan,
                        resamplingPassIndex:
                            resamplingPlan is null
                                ? -1
                                : passIndex,
                        catalogFilterPlan: catalogFilterPlan,
                        catalogFilterPassIndex:
                            catalogFilterPlan is null
                                ? -1
                                : passIndex);
                    AddEdge(
                        current,
                        filter.Id,
                        PrismGraphEdgeKind.Content);
                    current = filter.Id;
                }
            }

            return current;
        }

        private PrismGraphNodeId ApplyStyles(
            PrismNodeId definitionNodeId,
            IReadOnlyList<PrismStyleState> states,
            PrismGraphNodeId input,
            PrismGraphNodeId styleSource)
        {
            PrismNodeDefinition nodeDefinition = DefinitionNode(definitionNodeId);
            IReadOnlyList<PrismStyleDefinition> definitions = nodeDefinition switch
            {
                PrismLayerDefinition layer => layer.Styles,
                PrismGroupDefinition group => group.Styles,
                PrismBackdropDefinition backdrop => backdrop.Styles,
                _ => throw new InvalidOperationException("Unsupported style owner.")
            };
            if (definitions.Count != states.Count)
            {
                throw new InvalidOperationException("Style definition and runtime state counts differ.");
            }

            PrismGraphNodeId current = input;
            for (int index = states.Count - 1; index >= 0; index--)
            {
                PrismStyleState state = states[index];
                if (!state.Visible)
                {
                    continue;
                }

                ImmutableArray<PrismGraphParameter> parameters =
                    SnapshotParameters(state.Style, state);
                PrismGraphNode style = AddNode(
                    PrismGraphNodeKind.Style,
                    definitionNodeId,
                    index,
                    definitionOrders[definitionNodeId],
                    $"{diagnosticNames[definitionNodeId]}/style[{index}]",
                    DependenciesForCatalogEntry((int)state.Style, parameters),
                    parameters,
                    style: state.Style);
                AddEdge(current, style.Id, PrismGraphEdgeKind.Content);
                AddEdge(
                    styleSource,
                    style.Id,
                    PrismGraphEdgeKind.StyleSource);
                current = style.Id;
            }

            return current;
        }

        private PrismGraphNodeId ApplyMask(
            PrismNodeDefinition definitionNode,
            PrismMaskState? state,
            PrismGraphNodeId input)
        {
            PrismMaskDefinition? maskDefinition = definitionNode switch
            {
                PrismLayerDefinition layer => layer.Mask,
                PrismGroupDefinition group => group.Mask,
                PrismBackdropDefinition backdrop => backdrop.Mask,
                _ => null
            };
            if (maskDefinition is null)
            {
                return input;
            }
            if (state is null)
            {
                throw new InvalidOperationException("Mask definition has no runtime state.");
            }
            if (!Enum.IsDefined(typeof(PrismMaskChannel), state.Channel))
            {
                throw new InvalidOperationException($"Unknown mask channel '{state.Channel}'.");
            }
            if (!float.IsFinite(state.Feather) || state.Feather < 0)
            {
                throw new InvalidOperationException(
                    "Mask feather must be finite and non-negative.");
            }
            if (!float.IsFinite(state.Density) ||
                state.Density is < 0 or > 1)
            {
                throw new InvalidOperationException(
                    "Mask density must be in [0, 1].");
            }
            if (state.Density == 0)
            {
                return input;
            }

            PrismGraphNode mask = AddNode(
                PrismGraphNodeKind.Mask,
                definitionNode.Id,
                ordinal: 0,
                definitionOrders[definitionNode.Id],
                $"{diagnosticNames[definitionNode.Id]}/mask",
                Dependencies(
                    new PrismGraphDependency(
                        PrismGraphDependencyKind.Resource,
                        definitionNode.Id.Value,
                        analyzedScope.Scope.Resources.TryGetVersion(
                            state.Image,
                            out long resourceVersion)
                            ? resourceVersion
                            : 0)),
                resource: state.Image,
                maskChannel: state.Channel,
                feather: state.Feather,
                density: state.Density,
                invert: state.Invert,
                maskPass: PrismMaskPass.Extract);
            PrismGraphNodeId maskOutput = mask.Id;
            if (state.Feather > 0)
            {
                PrismGraphNode horizontal = AddNode(
                    PrismGraphNodeKind.Mask,
                    definitionNode.Id,
                    ordinal: 1,
                    definitionOrders[definitionNode.Id],
                    $"{diagnosticNames[definitionNode.Id]}/mask-feather-x",
                    Dependencies(),
                    feather: state.Feather,
                    maskPass: PrismMaskPass.FeatherHorizontal);
                AddEdge(
                    maskOutput,
                    horizontal.Id,
                    PrismGraphEdgeKind.Content);
                PrismGraphNode vertical = AddNode(
                    PrismGraphNodeKind.Mask,
                    definitionNode.Id,
                    ordinal: 2,
                    definitionOrders[definitionNode.Id],
                    $"{diagnosticNames[definitionNode.Id]}/mask-feather-y",
                    Dependencies(),
                    feather: state.Feather,
                    density: state.Density,
                    maskPass: PrismMaskPass.FeatherVertical);
                AddEdge(
                    horizontal.Id,
                    vertical.Id,
                    PrismGraphEdgeKind.Content);
                maskOutput = vertical.Id;
            }
            PrismGraphNode composite = AddNode(
                PrismGraphNodeKind.Composite,
                definitionNode.Id,
                MaskCompositeOrdinal,
                definitionOrders[definitionNode.Id],
                $"{diagnosticNames[definitionNode.Id]}/mask-composite",
                Dependencies());
            AddEdge(input, composite.Id, PrismGraphEdgeKind.Content);
            AddEdge(
                maskOutput,
                composite.Id,
                PrismGraphEdgeKind.MaskAlpha);
            return composite.Id;
        }

        private PrismGraphNode AddNode(
            PrismGraphNodeKind kind,
            PrismNodeId? definitionNodeId,
            int ordinal,
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
            int catalogFilterPassIndex = -1)
        {
            PrismGraphNodeId id = new(
                analyzedScope.DependencyStamp.CacheOwnerToken,
                definitionNodeId?.Value ?? 0,
                kind,
                ordinal);
            if (!nodeIds.Add(id))
            {
                throw Failure(
                    definitionNodeId is null ? null : DefinitionNode(definitionNodeId.Value),
                    $"Structural node identifier '{id}' is duplicated.");
            }

            PrismGraphNode node = new(
                id,
                kind,
                analyzedScope.ScopeIndex,
                definitionNodeId,
                definitionOrder,
                diagnosticName,
                dependencies,
                parameters,
                isIsolationBoundary,
                blendMode,
                amount,
                filter,
                style,
                resource,
                colorProfile,
                maskChannel,
                feather,
                density,
                invert,
                maskPass,
                layerSettings,
                neighborhoodPlan,
                neighborhoodPassIndex,
                resamplingPlan,
                resamplingPassIndex,
                catalogFilterPlan,
                catalogFilterPassIndex);
            nodes.Add(node);
            return node;
        }

        private NodeDisposition GetNodeDisposition(PrismNodeDefinition nodeDefinition)
        {
            try
            {
                return nodeDefinition switch
                {
                    PrismLayerDefinition layer => LayerDisposition(layer),
                    PrismGroupDefinition group => GroupDisposition(group),
                    PrismBackdropDefinition => throw new InvalidOperationException(
                        "A backdrop cannot appear inside a content stack."),
                    _ => throw new InvalidOperationException(
                        $"Unsupported definition type '{nodeDefinition.GetType().Name}'.")
                };
            }
            catch (PrismGraphBuildException)
            {
                throw;
            }
            catch (Exception exception) when (
                exception is InvalidOperationException or
                ArgumentException or
                KeyNotFoundException)
            {
                throw Failure(nodeDefinition, exception.Message, exception);
            }
        }

        private NodeDisposition LayerDisposition(PrismLayerDefinition layer)
        {
            PrismLayerState state = instance.GetLayerState(layer.Id);
            ValidateBlendMode(state.BlendMode, allowPassThrough: false);
            return new NodeDisposition(
                state.Visible && state.Opacity > 0,
                state.ClipToBelow);
        }

        private NodeDisposition GroupDisposition(PrismGroupDefinition group)
        {
            PrismGroupState state = instance.GetGroupState(group.Id);
            ValidateBlendMode(state.BlendMode, allowPassThrough: true);
            return new NodeDisposition(
                state.Visible && state.Opacity > 0,
                ClipToBelow: false);
        }

        private void AddEdge(
            PrismGraphNodeId source,
            PrismGraphNodeId target,
            PrismGraphEdgeKind kind)
        {
            edges.Add(new PrismGraphEdge(source, target, kind));
        }

        private ImmutableArray<PrismGraphDependency> Dependencies(
            params PrismGraphDependency[] additional)
        {
            if (additional.Length == 0)
            {
                return scopeDependencies;
            }

            ImmutableArray<PrismGraphDependency>.Builder builder =
                ImmutableArray.CreateBuilder<PrismGraphDependency>(
                    scopeDependencies.Length + additional.Length);
            builder.AddRange(scopeDependencies);
            builder.AddRange(additional);
            return builder.MoveToImmutable();
        }

        private ImmutableArray<PrismGraphDependency> DependenciesForCatalogEntry(
            int stableId,
            ImmutableArray<PrismGraphParameter> parameters)
        {
            PrismCatalogEntryDescriptor entry =
                PrismCatalogRuntime.GetEntry(stableId);
            List<PrismGraphDependency> additional =
            [
                new PrismGraphDependency(
                    PrismGraphDependencyKind.CatalogEntry,
                    stableId,
                    entry.DependencyVersion)
            ];
            foreach (PrismGraphParameter parameter in parameters)
            {
                if (parameter.Kind == PrismGraphParameterValueKind.Resource &&
                    parameter.ResourceValue.Value > 0)
                {
                    additional.Add(
                        new PrismGraphDependency(
                            PrismGraphDependencyKind.Resource,
                            ((long)stableId << 32) | (uint)parameter.Index,
                            analyzedScope.Scope.Resources.TryGetVersion(
                                parameter.ResourceValue,
                                out long resourceVersion)
                                ? resourceVersion
                                : 0));
                }
            }
            return Dependencies(additional.ToArray());
        }

        private ImmutableArray<PrismGraphDependency> CreateScopeDependencies()
        {
            PrismDependencyStamp stamp = analyzedScope.DependencyStamp;
            return
            [
                new PrismGraphDependency(
                    PrismGraphDependencyKind.Structure,
                    stamp.CacheOwnerToken.Value,
                    stamp.StructuralVersion.Value),
                new PrismGraphDependency(
                    PrismGraphDependencyKind.Values,
                    stamp.CacheOwnerToken.Value,
                    stamp.ValueVersion.Value),
                new PrismGraphDependency(
                    PrismGraphDependencyKind.Descendants,
                    stamp.CacheOwnerToken.Value,
                    stamp.DescendantVersion)
            ];
        }

        private PrismGraphCompositionSettings SnapshotCompositionSettings()
        {
            PrismCompositionState state = instance.Composition;
            PrismColorProfile colorProfile = state.WorkingColorProfile;
            float globalLightAngle = state.GlobalLightAngle;
            float globalLightAltitude = state.GlobalLightAltitude;
            if (!Enum.IsDefined(typeof(PrismColorProfile), colorProfile))
            {
                throw Failure(null, $"Unknown working color profile '{colorProfile}'.");
            }
            if (!float.IsFinite(globalLightAngle))
            {
                throw Failure(null, "The global light angle must be finite.");
            }
            if (!float.IsFinite(globalLightAltitude) ||
                globalLightAltitude is < 0 or > 90)
            {
                throw Failure(null, "The global light altitude must be from zero through 90.");
            }

            return new PrismGraphCompositionSettings(
                colorProfile,
                globalLightAngle,
                globalLightAltitude);
        }

        private static PrismGraphLayerSettings SnapshotLayerSettings(PrismLayerState state)
        {
            PrismBlendChannels blendChannels = state.BlendChannels;
            PrismKnockout knockout = state.Knockout;
            PrismBlendIfChannel blendIfChannel = state.BlendIfChannel;
            int dissolveSeed = state.DissolveSeed;
            if ((blendChannels & ~PrismBlendChannels.Rgba) != 0)
            {
                throw new InvalidOperationException(
                    $"Unknown blend channels '{blendChannels}'.");
            }
            if (!Enum.IsDefined(typeof(PrismKnockout), knockout))
            {
                throw new InvalidOperationException($"Unknown knockout mode '{knockout}'.");
            }
            if (!Enum.IsDefined(typeof(PrismBlendIfChannel), blendIfChannel))
            {
                throw new InvalidOperationException(
                    $"Unknown Blend If channel '{blendIfChannel}'.");
            }
            if (dissolveSeed < 0)
            {
                throw new InvalidOperationException("The dissolve seed cannot be negative.");
            }

            return new PrismGraphLayerSettings(
                blendChannels,
                knockout,
                state.BlendInteriorStylesAsGroup,
                state.BlendClippedLayersAsGroup,
                state.TransparencyShapesLayer,
                state.LayerMaskHidesStyles,
                state.VectorMaskHidesStyles,
                blendIfChannel,
                state.ThisLayerRange,
                state.UnderlyingRange,
                dissolveSeed);
        }

        private ImmutableArray<PrismGraphParameter> SnapshotParameters(
            PrismFilterId filter,
            PrismFilterState state)
        {
            int stableId = (int)filter;
            PrismCatalogEntryDescriptor entry = PrismCatalogRuntime.GetEntry(stableId);
            ImmutableArray<PrismGraphParameter>.Builder parameters =
                ImmutableArray.CreateBuilder<PrismGraphParameter>(entry.Properties.Length);
            for (int index = 0; index < entry.Properties.Length; index++)
            {
                PrismCatalogPropertyDescriptor property = entry.Properties[index];
                parameters.Add(SnapshotParameter(stableId, index, property, state));
            }
            return parameters.MoveToImmutable();
        }

        private ImmutableArray<PrismGraphParameter> SnapshotParameters(
            PrismStyleId style,
            PrismStyleState state)
        {
            int stableId = (int)style;
            PrismCatalogEntryDescriptor entry = PrismCatalogRuntime.GetEntry(stableId);
            ImmutableArray<PrismGraphParameter>.Builder parameters =
                ImmutableArray.CreateBuilder<PrismGraphParameter>(entry.Properties.Length);
            for (int index = 0; index < entry.Properties.Length; index++)
            {
                PrismCatalogPropertyDescriptor property = entry.Properties[index];
                parameters.Add(SnapshotParameter(stableId, index, property, state));
            }
            return parameters.MoveToImmutable();
        }

        private static PrismGraphParameter SnapshotParameter(
            int stableId,
            int index,
            PrismCatalogPropertyDescriptor property,
            PrismFilterState state)
        {
            return property.ValueType switch
            {
                PrismCatalogValueType.Boolean => new PrismGraphParameter(
                    index,
                    PrismGraphParameterValueKind.Boolean,
                    booleanValue: state.GetValue(new PrismParameterKey<bool>(stableId, property.TypeSlot))),
                PrismCatalogValueType.Integer => new PrismGraphParameter(
                    index,
                    PrismGraphParameterValueKind.Integer,
                    integerValue: state.GetValue(new PrismParameterKey<int>(stableId, property.TypeSlot))),
                PrismCatalogValueType.Number => new PrismGraphParameter(
                    index,
                    PrismGraphParameterValueKind.Number,
                    numberValue: state.GetValue(new PrismParameterKey<float>(stableId, property.TypeSlot))),
                PrismCatalogValueType.Color => new PrismGraphParameter(
                    index,
                    PrismGraphParameterValueKind.Color,
                    colorValue: state.GetValue(new PrismParameterKey<Color>(stableId, property.TypeSlot))),
                PrismCatalogValueType.Vector => new PrismGraphParameter(
                    index,
                    PrismGraphParameterValueKind.Vector,
                    vectorValue: state.GetValue(new PrismParameterKey<Vector4>(stableId, property.TypeSlot))),
                PrismCatalogValueType.Symbol => new PrismGraphParameter(
                    index,
                    PrismGraphParameterValueKind.Symbol,
                    integerValue: state.GetValue(new PrismParameterKey<int>(stableId, property.TypeSlot))),
                PrismCatalogValueType.Resource => new PrismGraphParameter(
                    index,
                    PrismGraphParameterValueKind.Resource,
                    resourceValue: state.GetValue(
                        new PrismParameterKey<PrismResourceId>(stableId, property.TypeSlot))),
                _ => throw new InvalidOperationException(
                    $"Unsupported Prism catalog value type '{property.ValueType}'.")
            };
        }

        private static PrismGraphParameter SnapshotParameter(
            int stableId,
            int index,
            PrismCatalogPropertyDescriptor property,
            PrismStyleState state)
        {
            return property.ValueType switch
            {
                PrismCatalogValueType.Boolean => new PrismGraphParameter(
                    index,
                    PrismGraphParameterValueKind.Boolean,
                    booleanValue: state.GetValue(new PrismParameterKey<bool>(stableId, property.TypeSlot))),
                PrismCatalogValueType.Integer => new PrismGraphParameter(
                    index,
                    PrismGraphParameterValueKind.Integer,
                    integerValue: state.GetValue(new PrismParameterKey<int>(stableId, property.TypeSlot))),
                PrismCatalogValueType.Number => new PrismGraphParameter(
                    index,
                    PrismGraphParameterValueKind.Number,
                    numberValue: state.GetValue(new PrismParameterKey<float>(stableId, property.TypeSlot))),
                PrismCatalogValueType.Color => new PrismGraphParameter(
                    index,
                    PrismGraphParameterValueKind.Color,
                    colorValue: state.GetValue(new PrismParameterKey<Color>(stableId, property.TypeSlot))),
                PrismCatalogValueType.Vector => new PrismGraphParameter(
                    index,
                    PrismGraphParameterValueKind.Vector,
                    vectorValue: state.GetValue(new PrismParameterKey<Vector4>(stableId, property.TypeSlot))),
                PrismCatalogValueType.Symbol => new PrismGraphParameter(
                    index,
                    PrismGraphParameterValueKind.Symbol,
                    integerValue: state.GetValue(new PrismParameterKey<int>(stableId, property.TypeSlot))),
                PrismCatalogValueType.Resource => new PrismGraphParameter(
                    index,
                    PrismGraphParameterValueKind.Resource,
                    resourceValue: state.GetValue(
                        new PrismParameterKey<PrismResourceId>(stableId, property.TypeSlot))),
                _ => throw new InvalidOperationException(
                    $"Unsupported Prism catalog value type '{property.ValueType}'.")
            };
        }

        private PrismNodeDefinition DefinitionNode(PrismNodeId id)
        {
            return FindNode(definition.Nodes, id)
                ?? throw new KeyNotFoundException($"Prism definition node '{id.Value}' does not exist.");
        }

        private static PrismNodeDefinition? FindNode(
            IReadOnlyList<PrismNodeDefinition> definitions,
            PrismNodeId id)
        {
            foreach (PrismNodeDefinition definition in definitions)
            {
                if (definition.Id == id)
                {
                    return definition;
                }
                if (definition is PrismGroupDefinition group)
                {
                    PrismNodeDefinition? child = FindNode(group.Children, id);
                    if (child is not null)
                    {
                        return child;
                    }
                }
            }
            return null;
        }

        private void IndexDefinitions()
        {
            int order = 0;
            foreach (PrismNodeDefinition node in definition.Nodes)
            {
                IndexDefinition(node, definition.Name, ref order);
            }
        }

        private void IndexDefinition(
            PrismNodeDefinition node,
            string parentName,
            ref int order)
        {
            definitionOrders.Add(node.Id, order++);
            string name = $"{parentName}/{node.Name ?? $"#{node.Id.Value}"}";
            diagnosticNames.Add(node.Id, name);
            if (node is PrismGroupDefinition group)
            {
                foreach (PrismNodeDefinition child in group.Children)
                {
                    IndexDefinition(child, name, ref order);
                }
            }
        }

        private PrismGraphBuildException Failure(
            PrismNodeDefinition? node,
            string message,
            Exception? innerException = null)
        {
            PrismGraphDiagnostic diagnostic = new(
                definition.Name,
                node?.Id,
                node?.Name,
                node?.SourceSpan ?? definition.SourceSpan,
                message);
            return new PrismGraphBuildException(diagnostic, innerException);
        }

        private static void ValidateBlendMode(
            PrismBlendMode blendMode,
            bool allowPassThrough)
        {
            if (!Enum.IsDefined(typeof(PrismBlendMode), blendMode) ||
                (!allowPassThrough && blendMode == PrismBlendMode.PassThrough))
            {
                throw new InvalidOperationException($"Unsupported blend mode '{blendMode}'.");
            }
        }

        private static long StableBoundsHash(DrawRect bounds)
        {
            const ulong offset = 14695981039346656037;
            const ulong prime = 1099511628211;
            ulong hash = offset;
            hash = (hash ^ BitConverter.SingleToUInt32Bits(bounds.X)) * prime;
            hash = (hash ^ BitConverter.SingleToUInt32Bits(bounds.Y)) * prime;
            hash = (hash ^ BitConverter.SingleToUInt32Bits(bounds.Width)) * prime;
            hash = (hash ^ BitConverter.SingleToUInt32Bits(bounds.Height)) * prime;
            return unchecked((long)hash);
        }

        private static long StableTransformHash(Matrix3x2 transform)
        {
            const ulong offset = 14695981039346656037;
            const ulong prime = 1099511628211;
            ulong hash = offset;
            hash = (hash ^ BitConverter.SingleToUInt32Bits(transform.M11)) * prime;
            hash = (hash ^ BitConverter.SingleToUInt32Bits(transform.M12)) * prime;
            hash = (hash ^ BitConverter.SingleToUInt32Bits(transform.M21)) * prime;
            hash = (hash ^ BitConverter.SingleToUInt32Bits(transform.M22)) * prime;
            hash = (hash ^ BitConverter.SingleToUInt32Bits(transform.M31)) * prime;
            hash = (hash ^ BitConverter.SingleToUInt32Bits(transform.M32)) * prime;
            return unchecked((long)hash);
        }

        private readonly record struct NodeDisposition(
            bool IsRenderable,
            bool ClipToBelow);

        private readonly record struct StackBuildResult(
            PrismGraphNodeId? Output,
            bool HasContent);

        private readonly record struct NodeBuildResult(
            PrismGraphNodeId Output,
            PrismGraphNodeId Alpha,
            PrismBlendMode BlendMode,
            bool ClipToBelow,
            bool ConsumesBackground,
            PrismGraphLayerSettings? LayerSettings);
    }
}
