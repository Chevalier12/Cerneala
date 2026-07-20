using System.Collections.Immutable;
using System.Globalization;
using System.Numerics;
using Cerneala.Drawing;
using Cerneala.Drawing.Prism;
using Cerneala.Drawing.Prism.Catalog;
using Cerneala.Drawing.Prism.ColorManagement;
using Cerneala.Drawing.Prism.Filters;
using Cerneala.Drawing.Prism.Graph;
using Cerneala.Drawing.Prism.Styles;
using Cerneala.UI.Markup;
using Cerneala.UI.Prism.Definitions;
using Cerneala.UI.Prism.Runtime;

namespace Cerneala.Tests.Drawing.Prism;

public sealed class PrismGraphOptimizerTests
{
    [Fact]
    public void OptimizerIsSeparateNonMutatingAndRemovesOnlyProvenNoOps()
    {
        PrismLayerDefinition layer = Layer(1, "Content");
        PrismCompositionDefinition definition =
            PrismTestData.Composition("NoOps", layer);
        (PrismGraph raw, PrismDrawScope scope) = BuildGraph(definition);
        PrismGraphNodeId[] rawIds = raw.Nodes.Select(node => node.Id).ToArray();
        PrismValueVersion valueVersion = scope.Instance.ValueVersion;

        PrismGraphExecutionPlan plan = new PrismGraphOptimizer().Optimize(raw);

        Assert.NotSame(raw, plan.OptimizedGraph);
        Assert.Equal(rawIds, raw.Nodes.Select(node => node.Id));
        Assert.Equal(valueVersion, scope.Instance.ValueVersion);
        Assert.Same(definition, scope.Instance.Definition);
        Assert.Contains(raw.Nodes, node => node.Kind == PrismGraphNodeKind.Fill);
        Assert.Contains(raw.Nodes, node => node.Kind == PrismGraphNodeKind.Opacity);
        Assert.DoesNotContain(
            plan.OptimizedGraph.Nodes,
            node => node.Kind == PrismGraphNodeKind.Fill);
        Assert.DoesNotContain(
            plan.OptimizedGraph.Nodes,
            node => node.Kind == PrismGraphNodeKind.Opacity);
        Assert.Equal(
            2,
            plan.RemovedNodeIds.Count(
                id => raw.GetNode(id).Kind is
                    PrismGraphNodeKind.Fill or PrismGraphNodeKind.Opacity));
    }

    [Fact]
    public void InvisibleCompositionPrunesOrphanedCaptureAndNeedsNoSurface()
    {
        PrismCompositionDefinition definition = PrismTestData.Composition(
            "Hidden",
            Layer(1, "HiddenLayer", visible: false));
        (PrismGraph raw, _) = BuildGraph(definition);

        PrismGraphExecutionPlan plan = new PrismGraphOptimizer().Optimize(raw);

        Assert.Equal(1, raw.ControlCaptureCount);
        Assert.Empty(plan.OptimizedGraph.Nodes);
        Assert.Empty(plan.OptimizedGraph.Edges);
        Assert.Null(Assert.Single(plan.OptimizedGraph.Scopes).Output);
        Assert.Equal(0, plan.OptimizedGraph.ControlCaptureCount);
        Assert.Equal(0, plan.PeakLiveSurfaces);
    }

    [Fact]
    public void CacheabilityIsExplicitConservativeAndPropagated()
    {
        PrismCompositionDefinition plainDefinition = PrismTestData.Composition(
            "Plain",
            Layer(1, "Content"));
        PrismGraphExecutionPlan plain = new PrismGraphOptimizer().Optimize(
            BuildGraph(plainDefinition).Graph);

        Assert.All(plain.NodePlans, node => Assert.True(node.IsCacheable));
        PrismGraphNode capture = Assert.Single(
            plain.OptimizedGraph.Nodes.Where(
                node => node.Kind == PrismGraphNodeKind.ControlCapture));
        Assert.Contains(
            capture.Dependencies,
            dependency => dependency.Kind == PrismGraphDependencyKind.Transform);
        PrismGraphNodePlan plainOutput = plain.GetNodePlan(
            Assert.Single(plain.OptimizedGraph.Scopes).Output!.Value);
        foreach (PrismGraphDependencyKind kind in new[]
        {
            PrismGraphDependencyKind.Structure,
            PrismGraphDependencyKind.Values,
            PrismGraphDependencyKind.VisualContent,
            PrismGraphDependencyKind.Descendants,
            PrismGraphDependencyKind.Bounds,
            PrismGraphDependencyKind.PixelScale,
            PrismGraphDependencyKind.Transform,
            PrismGraphDependencyKind.ColorProfile,
            PrismGraphDependencyKind.CatalogEntry
        })
        {
            Assert.Contains(
                plainOutput.CacheDependencies,
                dependency => dependency.Kind == kind);
        }

        PrismCompositionDefinition maskedDefinition = PrismTestData.Composition(
            "Masked",
            Layer(
                2,
                "MaskedContent",
                mask: new PrismMaskDefinition(new PrismResourceId(41))));
        PrismGraphExecutionPlan masked = new PrismGraphOptimizer().Optimize(
            BuildGraph(maskedDefinition).Graph);
        PrismGraphNode mask = Assert.Single(
            masked.OptimizedGraph.Nodes.Where(
                node => node.Kind == PrismGraphNodeKind.Mask));
        PrismGraphNodePlan maskPlan = masked.GetNodePlan(mask.Id);
        Assert.False(maskPlan.IsCacheable);
        Assert.True(
            maskPlan.UncacheableReasons.HasFlag(
                PrismGraphUncacheableReason.ResourceVersionUnavailable));
        PrismGraphNodePlan maskedOutput = masked.GetNodePlan(
            Assert.Single(masked.OptimizedGraph.Scopes).Output!.Value);
        Assert.False(maskedOutput.IsCacheable);
        Assert.True(
            maskedOutput.UncacheableReasons.HasFlag(
                PrismGraphUncacheableReason.UncacheableInput));
        Assert.Contains(
            maskedOutput.CacheDependencies,
            dependency => dependency.Kind == PrismGraphDependencyKind.Resource);

        PrismCompositionDefinition backdropDefinition = PrismTestData.Composition(
            "Backdrop",
            Layer(3, "Content"),
            new PrismBackdropDefinition(
                new PrismNodeId(4),
                "Backdrop",
                filters: [new PrismFilterDefinition(PrismFilterId.GaussianBlur)]));
        PrismGraphExecutionPlan backdrop = new PrismGraphOptimizer().Optimize(
            BuildGraph(backdropDefinition).Graph);
        PrismGraphNode backdropInput = Assert.Single(
            backdrop.OptimizedGraph.Nodes.Where(
                node => node.Kind == PrismGraphNodeKind.BackdropInput));
        Assert.True(
            backdrop.GetNodePlan(backdropInput.Id).UncacheableReasons.HasFlag(
                PrismGraphUncacheableReason.FrameBackdrop));
        Assert.False(
            backdrop.GetNodePlan(
                    Assert.Single(backdrop.OptimizedGraph.Scopes).Output!.Value)
                .IsCacheable);
    }

    [Fact]
    public void DefaultNodePlanIsUnknownAndNotCacheable()
    {
        PrismGraphNodePlan plan = default;

        Assert.Equal(PrismGraphBoundsStatus.Unknown, plan.BoundsStatus);
        Assert.False(plan.IsCacheable);
    }

    [Fact]
    public void BoundsExpandForBlurShadowStrokeAndTransformWithoutChangingScopeBounds()
    {
        DrawRect scopeBounds = new(0, 0, 20, 10);

        PrismLayerDefinition blurLayer = new(
            new PrismNodeId(1),
            "Blur",
            filters: [new PrismFilterDefinition(PrismFilterId.Blur)]);
        PrismGraphExecutionPlan blur = OptimizeConfigured(
            PrismTestData.Composition("BlurBounds", blurLayer),
            instance => GeneratedMarkup.SetPrismFilterNumber(
                instance.GetLayerState(blurLayer.Id).Filters[0],
                (int)PrismFilterId.Blur,
                slot: 0,
                value: 3),
            scopeBounds);
        PrismGraphNodePlan blurPlan =
            PlanForOperation(blur, PrismGraphNodeKind.Filter);
        AssertRect(
            new DrawRect(-3, -3, 26, 16),
            blurPlan.Bounds);
        Assert.Equal(PrismGraphBoundsStatus.Conservative, blurPlan.BoundsStatus);

        PrismLayerDefinition shadowLayer = new(
            new PrismNodeId(2),
            "Shadow",
            styles: [new PrismStyleDefinition(PrismStyleId.DropShadow)]);
        PrismGraphExecutionPlan shadow = OptimizeConfigured(
            PrismTestData.Composition("ShadowBounds", shadowLayer),
            instance =>
            {
                PrismStyleState state =
                    instance.GetLayerState(shadowLayer.Id).Styles[0];
                GeneratedMarkup.SetPrismStyleBoolean(
                    state,
                    (int)PrismStyleId.DropShadow,
                    slot: 2,
                    value: false);
                GeneratedMarkup.SetPrismStyleNumber(
                    state,
                    (int)PrismStyleId.DropShadow,
                    slot: 0,
                    value: 0);
                GeneratedMarkup.SetPrismStyleNumber(
                    state,
                    (int)PrismStyleId.DropShadow,
                    slot: 1,
                    value: 5);
                GeneratedMarkup.SetPrismStyleNumber(
                    state,
                    (int)PrismStyleId.DropShadow,
                    slot: 5,
                    value: 2);
                GeneratedMarkup.SetPrismStyleNumber(
                    state,
                    (int)PrismStyleId.DropShadow,
                    slot: 4,
                    value: 3);
            },
            scopeBounds);
        PrismGraphNodePlan shadowPlan =
            PlanForOperation(shadow, PrismGraphNodeKind.Style);
        AssertRect(
            new DrawRect(0, -5, 30, 20),
            shadowPlan.Bounds);
        Assert.Equal(PrismGraphBoundsStatus.Conservative, shadowPlan.BoundsStatus);

        PrismLayerDefinition strokeLayer = new(
            new PrismNodeId(3),
            "Stroke",
            styles: [new PrismStyleDefinition(PrismStyleId.Stroke)]);
        PrismGraphExecutionPlan stroke = OptimizeConfigured(
            PrismTestData.Composition("StrokeBounds", strokeLayer),
            instance => GeneratedMarkup.SetPrismStyleNumber(
                instance.GetLayerState(strokeLayer.Id).Styles[0],
                (int)PrismStyleId.Stroke,
                slot: 4,
                value: 4),
            scopeBounds);
        PrismGraphNodePlan strokePlan =
            PlanForOperation(stroke, PrismGraphNodeKind.Style);
        AssertRect(
            new DrawRect(-4, -4, 28, 18),
            strokePlan.Bounds);
        Assert.Equal(PrismGraphBoundsStatus.Conservative, strokePlan.BoundsStatus);

        PrismLayerDefinition transformLayer = new(
            new PrismNodeId(4),
            "Transform",
            filters: [new PrismFilterDefinition(PrismFilterId.Transform)]);
        PrismGraphExecutionPlan transform = OptimizeConfigured(
            PrismTestData.Composition("TransformBounds", transformLayer),
            instance =>
            {
                PrismFilterState state =
                    instance.GetLayerState(transformLayer.Id).Filters[0];
                GeneratedMarkup.SetPrismFilterVector(
                    state,
                    (int)PrismFilterId.Transform,
                    slot: 3,
                    value: new Vector4(5, -2, 0, 0));
                GeneratedMarkup.SetPrismFilterVector(
                    state,
                    (int)PrismFilterId.Transform,
                    slot: 1,
                    value: new Vector4(2, 1, 0, 0));
                GeneratedMarkup.SetPrismFilterVector(
                    state,
                    (int)PrismFilterId.Transform,
                    slot: 2,
                    value: new Vector4(0.5f, 0, 0, 0));
                GeneratedMarkup.SetPrismFilterVector(
                    state,
                    (int)PrismFilterId.Transform,
                    slot: 0,
                    value: Vector4.Zero);
            },
            scopeBounds);
        PrismGraphNodePlan transformPlan =
            PlanForOperation(transform, PrismGraphNodeKind.Filter);
        AssertRect(
            new DrawRect(
                5,
                -2,
                40 +
                    (10 * MathF.Tan(
                        0.5f *
                        (MathF.PI / 180f))),
                10),
            transformPlan.Bounds);
        Assert.Equal(PrismGraphBoundsStatus.Exact, transformPlan.BoundsStatus);

        Assert.All(
            new[] { blur, shadow, stroke, transform },
            plan => AssertRect(
                scopeBounds,
                Assert.Single(plan.OptimizedGraph.Scopes).Bounds));
    }

    [Fact]
    public void PartialTransformKeepsSourceAndTransformedPixelBounds()
    {
        DrawRect scopeBounds = new(0, 0, 20, 10);
        PrismLayerDefinition layer = new(
            new PrismNodeId(1),
            "PartialTransform",
            filters: [new PrismFilterDefinition(PrismFilterId.Transform)]);
        PrismGraphExecutionPlan plan = OptimizeConfigured(
            PrismTestData.Composition("PartialTransformBounds", layer),
            instance =>
            {
                PrismFilterState state =
                    instance.GetLayerState(layer.Id).Filters[0];
                state.Opacity = 0.5f;
                GeneratedMarkup.SetPrismFilterVector(
                    state,
                    (int)PrismFilterId.Transform,
                    slot: 3,
                    value: new Vector4(20, 0, 0, 0));
                GeneratedMarkup.SetPrismFilterVector(
                    state,
                    (int)PrismFilterId.Transform,
                    slot: 1,
                    value: new Vector4(1, 1, 0, 0));
                GeneratedMarkup.SetPrismFilterNumber(
                    state,
                    (int)PrismFilterId.Transform,
                    slot: 0,
                    value: 0);
                GeneratedMarkup.SetPrismFilterVector(
                    state,
                    (int)PrismFilterId.Transform,
                    slot: 2,
                    value: Vector4.Zero);
                GeneratedMarkup.SetPrismFilterVector(
                    state,
                    (int)PrismFilterId.Transform,
                    slot: 0,
                    value: Vector4.Zero);
            },
            scopeBounds);

        PrismGraphNodePlan transform =
            PlanForOperation(plan, PrismGraphNodeKind.Filter);

        AssertRect(new DrawRect(0, 0, 40, 10), transform.Bounds);
        Assert.Equal(
            PrismGraphBoundsStatus.Conservative,
            transform.BoundsStatus);
    }

    [Fact]
    public void BoundsStayInTransformedLogicalCoordinatesUntilSurfaceAllocation()
    {
        PrismLayerDefinition layer = new(
            new PrismNodeId(1),
            "LogicalBlur",
            filters: [new PrismFilterDefinition(PrismFilterId.Blur)]);
        PrismCompositionDefinition definition =
            PrismTestData.Composition("LogicalBounds", layer);
        Matrix3x2 effectiveTransform = Matrix3x2.CreateScale(2);
        PrismDrawScope scope = PrismTestData.Scope(
            definition,
            bounds: new DrawRect(0, 0, 20, 10),
            transform: effectiveTransform,
            pixelScale: 2);
        GeneratedMarkup.SetPrismFilterNumber(
            scope.Instance.GetLayerState(layer.Id).Filters[0],
            (int)PrismFilterId.Blur,
            slot: 0,
            value: 3);

        PrismGraphExecutionPlan plan =
            new PrismGraphOptimizer().Optimize(BuildGraph(scope));
        PrismGraphScope graphScope = Assert.Single(plan.OptimizedGraph.Scopes);
        PrismGraphNodePlan blur =
            PlanForOperation(plan, PrismGraphNodeKind.Filter);

        AssertRect(new DrawRect(0, 0, 40, 20), graphScope.Bounds);
        Assert.Equal(effectiveTransform, graphScope.EffectiveTransform);
        Assert.Equal(2, graphScope.PixelScale);
        AssertRect(new DrawRect(-6, -6, 52, 32), blur.Bounds);
    }

    [Fact]
    public void PreparedNeighborhoodOperationClaimsConservativeBounds()
    {
        PrismLayerDefinition layer = new(
            new PrismNodeId(1),
            "Lens",
            filters: [new PrismFilterDefinition(PrismFilterId.LensBlur)]);
        PrismGraphExecutionPlan plan = new PrismGraphOptimizer().Optimize(
            BuildGraph(PrismTestData.Composition("UnknownBounds", layer)).Graph);
        PrismGraphNodePlan filter =
            PlanForOperation(plan, PrismGraphNodeKind.Filter);
        PrismGraphNodePlan output = plan.GetNodePlan(
            Assert.Single(plan.OptimizedGraph.Scopes).Output!.Value);

        Assert.Equal(
            PrismGraphBoundsStatus.Conservative,
            filter.BoundsStatus);
        Assert.Equal(
            PrismGraphBoundsStatus.Conservative,
            output.BoundsStatus);
        Assert.True(filter.Bounds.Width > 20);
        Assert.True(filter.Bounds.Height > 10);
    }

    [Fact]
    public void TypedCatalogNoOpsAreRemovedOnlyWithNeutralBlendSettings()
    {
        PrismLayerDefinition layer = new(
            new PrismNodeId(1),
            "NoOps",
            filters:
            [
                new PrismFilterDefinition(PrismFilterId.Blur),
                new PrismFilterDefinition(PrismFilterId.Transform),
                new PrismFilterDefinition(PrismFilterId.Blur)
            ]);
        (PrismGraph raw, _) = BuildGraph(
            PrismTestData.Composition("TypedNoOps", layer),
            instance =>
            {
                PrismLayerState state = instance.GetLayerState(layer.Id);
                GeneratedMarkup.SetPrismFilterNumber(
                    state.Filters[0],
                    (int)PrismFilterId.Blur,
                    slot: 0,
                    value: 0);
                GeneratedMarkup.SetPrismFilterVector(
                    state.Filters[1],
                    (int)PrismFilterId.Transform,
                    slot: 3,
                    value: Vector4.Zero);
                GeneratedMarkup.SetPrismFilterVector(
                    state.Filters[1],
                    (int)PrismFilterId.Transform,
                    slot: 1,
                    value: new Vector4(1, 1, 0, 0));
                GeneratedMarkup.SetPrismFilterNumber(
                    state.Filters[1],
                    (int)PrismFilterId.Transform,
                    slot: 0,
                    value: 0);
                GeneratedMarkup.SetPrismFilterVector(
                    state.Filters[1],
                    (int)PrismFilterId.Transform,
                    slot: 2,
                    value: Vector4.Zero);
                GeneratedMarkup.SetPrismFilterNumber(
                    state.Filters[2],
                    (int)PrismFilterId.Blur,
                    slot: 0,
                    value: 0);
                state.Filters[2].BlendMode = PrismBlendMode.Multiply;
            });

        PrismGraphExecutionPlan plan = new PrismGraphOptimizer().Optimize(raw);

        Assert.Equal(
            3,
            raw.Nodes.Count(node => node.Kind == PrismGraphNodeKind.Filter));
        PrismGraphNode remainingFilter = Assert.Single(
            plan.OptimizedGraph.Nodes.Where(
                node => node.Kind == PrismGraphNodeKind.Filter));
        Assert.Equal(PrismBlendMode.Multiply, remainingFilter.BlendMode);
        Assert.Equal(
            2,
            plan.RemovedNodeIds.Count(
                id => raw.GetNode(id).Kind == PrismGraphNodeKind.Filter));
        PrismGraphNodePlan output = plan.GetNodePlan(
            Assert.Single(plan.OptimizedGraph.Scopes).Output!.Value);
        Assert.Contains(
            output.CacheDependencies,
            dependency =>
                dependency.Kind == PrismGraphDependencyKind.CatalogEntry &&
                dependency.Key == (int)PrismFilterId.Blur);
        Assert.Contains(
            output.CacheDependencies,
            dependency =>
                dependency.Kind == PrismGraphDependencyKind.CatalogEntry &&
                dependency.Key == (int)PrismFilterId.Transform);
    }

    [Fact]
    public void ZeroOpacityStyleNoOpsAreRemovedForEveryCatalogFamily()
    {
        foreach (PrismStyleId style in
            Enum.GetValues<PrismStyleId>())
        {
            PrismLayerDefinition layer = new(
                new PrismNodeId(1),
                style.ToString(),
                styles: [new PrismStyleDefinition(style)]);
            PrismGraph raw = BuildGraph(
                PrismTestData.Composition(
                    $"NoOp{style}",
                    layer),
                instance => SetStyleNoOp(
                    instance.GetLayerState(layer.Id).Styles[0]))
                .Graph;
            PrismGraphNode rawStyle = Assert.Single(
                raw.Nodes.Where(
                    node => node.Kind == PrismGraphNodeKind.Style));

            PrismGraphExecutionPlan plan =
                new PrismGraphOptimizer().Optimize(raw);

            Assert.Contains(rawStyle.Id, plan.RemovedNodeIds);
            Assert.DoesNotContain(
                plan.OptimizedGraph.Nodes,
                node => node.Kind == PrismGraphNodeKind.Style);
            Assert.DoesNotContain(
                plan.OptimizedGraph.Edges,
                edge => edge.Kind == PrismGraphEdgeKind.StyleSource);
            Assert.Equal(
                SemanticSnapshot(raw),
                SemanticSnapshot(plan.OptimizedGraph));
        }
    }

    [Fact]
    public void InvisibleAndNoOpStylesPreserveOrderMaskClipBlendAndAlpha()
    {
        PrismLayerDefinition styled = new(
            new PrismNodeId(1),
            "Styled",
            styles:
            [
                new PrismStyleDefinition(PrismStyleId.ColorOverlay),
                new PrismStyleDefinition(PrismStyleId.DropShadow),
                new PrismStyleDefinition(PrismStyleId.Stroke),
                new PrismStyleDefinition(PrismStyleId.ColorOverlay)
            ],
            mask: new PrismMaskDefinition(new PrismResourceId(71)),
            opacity: 0.63f,
            fill: 0.47f,
            blendMode: PrismBlendMode.Screen,
            clipToBelow: true);
        PrismLayerDefinition background = Layer(
            2,
            "Background",
            opacity: 0.71f,
            fill: 0.82f,
            blendMode: PrismBlendMode.Multiply);
        PrismGraph raw = BuildGraph(
            PrismTestData.Composition(
                "StyleNoOpComposition",
                styled,
                background),
            instance =>
            {
                IReadOnlyList<PrismStyleState> styles =
                    instance.GetLayerState(styled.Id).Styles;
                styles[0].Visible = false;
                SetStyleNoOp(styles[1]);
            }).Graph;
        PrismGraphNode[] rawStyles = raw.Nodes
            .Where(
                node =>
                    node.Kind == PrismGraphNodeKind.Style &&
                    node.DefinitionNodeId == styled.Id)
            .ToArray();
        Assert.Equal(3, rawStyles.Length);
        PrismGraphNode noOp = Assert.Single(
            rawStyles.Where(
                node => node.Style == PrismStyleId.DropShadow));
        PrismGraphNodeId[] expectedOrder = rawStyles
            .Where(node => node.Id != noOp.Id)
            .Select(node => node.Id)
            .ToArray();

        PrismGraphExecutionPlan plan =
            new PrismGraphOptimizer().Optimize(raw);
        PrismGraph optimized = plan.OptimizedGraph;
        PrismGraphNodeId[] actualOrder = plan.ExecutionOrder
            .Where(
                id =>
                {
                    PrismGraphNode node = optimized.GetNode(id);
                    return node.Kind == PrismGraphNodeKind.Style &&
                        node.DefinitionNodeId == styled.Id;
                })
            .ToArray();

        Assert.Contains(noOp.Id, plan.RemovedNodeIds);
        Assert.Equal(expectedOrder, actualOrder);
        Assert.DoesNotContain(
            optimized.Edges,
            edge =>
                edge.Kind == PrismGraphEdgeKind.StyleSource &&
                optimized.GetNode(edge.Target).Kind !=
                    PrismGraphNodeKind.Style);
        Assert.Equal(
            SemanticSnapshot(raw),
            SemanticSnapshot(optimized));
        Assert.Equal(
            raw.Nodes
                .Where(
                    node => node.Kind is
                        PrismGraphNodeKind.Fill or
                        PrismGraphNodeKind.Opacity or
                        PrismGraphNodeKind.ClipToBelow)
                .OrderBy(
                    node => node.Id.ToString(),
                    StringComparer.Ordinal)
                .Select(node => (node.Id, node.Kind, node.Amount)),
            optimized.Nodes
                .Where(
                    node => node.Kind is
                        PrismGraphNodeKind.Fill or
                        PrismGraphNodeKind.Opacity or
                        PrismGraphNodeKind.ClipToBelow)
                .OrderBy(
                    node => node.Id.ToString(),
                    StringComparer.Ordinal)
                .Select(node => (node.Id, node.Kind, node.Amount)));
    }

    [Fact]
    public void LifetimePlanKeepsFanOutInputsUntilTheirLastConsumer()
    {
        PrismGroupDefinition passThrough = new(
            new PrismNodeId(20),
            "PassThrough",
            [Layer(21, "Child")],
            filters: [new PrismFilterDefinition(PrismFilterId.Blur)],
            opacity: 0.5f,
            blendMode: PrismBlendMode.PassThrough);
        PrismCompositionDefinition definition = PrismTestData.Composition(
            "Lifetime",
            Layer(
                1,
                "Clipped",
                mask: new PrismMaskDefinition(new PrismResourceId(31)),
                clipToBelow: true),
            passThrough,
            Layer(99, "Base"));
        PrismGraphExecutionPlan plan = new PrismGraphOptimizer().Optimize(
            BuildGraph(definition).Graph);
        Dictionary<PrismGraphNodeId, PrismGraphSurfaceLifetime> lifetimes =
            plan.SurfaceLifetimes.ToDictionary(lifetime => lifetime.NodeId);
        Dictionary<PrismGraphNodeId, int> steps = plan.ExecutionOrder
            .Select((id, index) => KeyValuePair.Create(id, index))
            .ToDictionary();

        Assert.All(
            plan.OptimizedGraph.Edges,
            edge => Assert.True(
                lifetimes[edge.Source].LastStep >= steps[edge.Target],
                $"{edge.Source} was released before {edge.Target}."));
        Assert.All(
            plan.SurfaceLifetimes,
            lifetime => Assert.InRange(
                lifetime.LastStep,
                lifetime.FirstStep,
                plan.ExecutionOrder.Length - 1));

        PrismGraphNodeId clipBase = Assert.Single(
            plan.OptimizedGraph.Edges.Where(
                edge => edge.Kind == PrismGraphEdgeKind.ClipBaseAlpha)).Source;
        int finalClipUse = plan.OptimizedGraph.Edges
            .Where(
                edge => edge.Source == clipBase &&
                    edge.Kind == PrismGraphEdgeKind.ClipBaseAlpha)
            .Max(edge => steps[edge.Target]);
        int finalUse = plan.OptimizedGraph.Edges
            .Where(edge => edge.Source == clipBase)
            .Max(edge => steps[edge.Target]);
        Assert.True(lifetimes[clipBase].LastStep >= finalClipUse);
        Assert.Equal(finalUse, lifetimes[clipBase].LastStep);
        PrismGraphNode clip = Assert.Single(
            plan.OptimizedGraph.Nodes.Where(
                node => node.Kind == PrismGraphNodeKind.ClipToBelow));
        PrismGraphNode maskComposite = Assert.Single(
            plan.OptimizedGraph.Nodes.Where(
                node =>
                    node.Kind == PrismGraphNodeKind.Composite &&
                    plan.OptimizedGraph.Edges.Any(
                        edge =>
                            edge.Target == node.Id &&
                            edge.Kind == PrismGraphEdgeKind.MaskAlpha)));
        Assert.Equal(
            PrismGraphBoundsStatus.Conservative,
            plan.GetNodePlan(clip.Id).BoundsStatus);
        Assert.Equal(
            PrismGraphBoundsStatus.Conservative,
            plan.GetNodePlan(maskComposite.Id).BoundsStatus);
        Assert.True(plan.PeakLiveSurfaces >= 3);
    }

    [Fact]
    public void PeakSurfacesAcrossIndependentScopesIsMaximumRatherThanSum()
    {
        PrismCompositionDefinition firstDefinition = PrismTestData.Composition(
            "First",
            new PrismLayerDefinition(
                new PrismNodeId(1),
                "FirstLayer",
                filters: [new PrismFilterDefinition(PrismFilterId.Blur)]));
        PrismCompositionDefinition secondDefinition = PrismTestData.Composition(
            "Second",
            Layer(
                2,
                "SecondLayer",
                mask: new PrismMaskDefinition(new PrismResourceId(52))));
        PrismDrawScope first = PrismTestData.Scope(firstDefinition, ownerToken: 101);
        PrismDrawScope second = PrismTestData.Scope(secondDefinition, ownerToken: 102);
        PrismGraph combinedRaw = BuildGraph(first, second);
        PrismGraphExecutionPlan combined =
            new PrismGraphOptimizer().Optimize(combinedRaw);
        int firstPeak = new PrismGraphOptimizer()
            .Optimize(BuildGraph(firstDefinition).Graph)
            .PeakLiveSurfaces;
        int secondPeak = new PrismGraphOptimizer()
            .Optimize(BuildGraph(secondDefinition).Graph)
            .PeakLiveSurfaces;

        Assert.Equal(Math.Max(firstPeak, secondPeak), combined.PeakLiveSurfaces);
    }

    [Fact]
    public void NestedScopeExecutesInnerFirstAndCountsAncestorCapture()
    {
        PrismCompositionDefinition outerDefinition = PrismTestData.Composition(
            "Outer",
            Layer(1, "OuterLayer"));
        PrismCompositionDefinition innerDefinition = PrismTestData.Composition(
            "Inner",
            Layer(
                2,
                "InnerLayer",
                mask: new PrismMaskDefinition(new PrismResourceId(59))));
        PrismDrawScope outer =
            PrismTestData.Scope(outerDefinition, ownerToken: 301);
        PrismDrawScope inner =
            PrismTestData.Scope(innerDefinition, ownerToken: 302);
        PrismGraphExecutionPlan combined = new PrismGraphOptimizer().Optimize(
            BuildNestedGraph(outer, inner));
        int outerPeak = new PrismGraphOptimizer()
            .Optimize(BuildGraph(outerDefinition).Graph)
            .PeakLiveSurfaces;
        int innerPeak = new PrismGraphOptimizer()
            .Optimize(BuildGraph(innerDefinition).Graph)
            .PeakLiveSurfaces;
        PrismGraphScope outerScope = combined.OptimizedGraph.Scopes.Single(
            scope => scope.CacheOwnerToken == outer.CacheOwnerToken);
        PrismGraphScope innerScope = combined.OptimizedGraph.Scopes.Single(
            scope => scope.CacheOwnerToken == inner.CacheOwnerToken);
        Dictionary<PrismGraphNodeId, int> steps = combined.ExecutionOrder
            .Select((id, index) => KeyValuePair.Create(id, index))
            .ToDictionary();
        int finalInnerStep = combined.OptimizedGraph.Nodes
            .Where(node => node.AnalysisScopeIndex == innerScope.AnalysisScopeIndex)
            .Max(node => steps[node.Id]);
        int firstInnerStep = combined.OptimizedGraph.Nodes
            .Where(node => node.AnalysisScopeIndex == innerScope.AnalysisScopeIndex)
            .Min(node => steps[node.Id]);
        int firstOuterStep = combined.OptimizedGraph.Nodes
            .Where(node => node.AnalysisScopeIndex == outerScope.AnalysisScopeIndex)
            .Min(node => steps[node.Id]);
        PrismGraphNode outerCapture = Assert.Single(
            combined.OptimizedGraph.Nodes.Where(
                node =>
                    node.AnalysisScopeIndex == outerScope.AnalysisScopeIndex &&
                    node.Kind == PrismGraphNodeKind.ControlCapture));
        PrismGraphSurfaceLifetime outerCaptureLifetime =
            combined.SurfaceLifetimes.Single(
                lifetime => lifetime.NodeId == outerCapture.Id);
        int derivedPeak = Enumerable.Range(0, combined.ExecutionOrder.Length)
            .Max(
                step => combined.SurfaceLifetimes.Count(
                    lifetime =>
                        lifetime.FirstStep <= step &&
                        lifetime.LastStep >= step));

        Assert.Equal(outerScope.AnalysisScopeIndex, innerScope.ParentScopeIndex);
        Assert.Equal(outerScope.Depth + 1, innerScope.Depth);
        Assert.True(finalInnerStep < firstOuterStep);
        Assert.True(outerCaptureLifetime.FirstStep <= firstInnerStep);
        Assert.Equal(derivedPeak, combined.PeakLiveSurfaces);
        Assert.Equal(
            Math.Max(outerPeak, innerPeak + 1),
            combined.PeakLiveSurfaces);
    }

    [Fact]
    public void OptimizationAndLifetimeAreIndependentOfCollectionOrder()
    {
        PrismCompositionDefinition firstDefinition = PrismTestData.Composition(
            "First",
            new PrismLayerDefinition(
                new PrismNodeId(1),
                "Masked",
                filters: [new PrismFilterDefinition(PrismFilterId.Blur)],
                mask: new PrismMaskDefinition(new PrismResourceId(61))));
        PrismCompositionDefinition secondDefinition = PrismTestData.Composition(
            "Second",
            Layer(2, "Content"));
        PrismGraph raw = BuildGraph(
            PrismTestData.Scope(firstDefinition, ownerToken: 201),
            PrismTestData.Scope(secondDefinition, ownerToken: 202));
        PrismGraph permuted = new(
            raw.Nodes.Reverse().ToImmutableArray(),
            raw.Edges.Reverse().ToImmutableArray(),
            raw.Scopes.Reverse().ToImmutableArray());
        PrismGraphOptimizer optimizer = new();

        PrismGraphExecutionPlan normal = optimizer.Optimize(raw);
        PrismGraphExecutionPlan reordered = optimizer.Optimize(permuted);
        PrismGraphExecutionPlan secondPass =
            optimizer.Optimize(normal.OptimizedGraph);

        Assert.Equal(PlanSnapshot(normal), PlanSnapshot(reordered));
        Assert.Equal(
            PlanSnapshot(normal, includeRemoved: false),
            PlanSnapshot(secondPass, includeRemoved: false));
    }

    [Fact]
    public void DifferentialRawAndOptimizedGraphsPreservePhotoshopSemantics()
    {
        PrismCompositionDefinition[] definitions =
        [
            PrismTestData.Composition(
                "Alpha",
                Layer(
                    1,
                    "AlphaLayer",
                    opacity: 0.6f,
                    fill: 0.4f)),
            PrismTestData.Composition(
                "BlendOrder",
                Layer(
                    2,
                    "Screen",
                    opacity: 0.65f,
                    fill: 0.35f,
                    blendMode: PrismBlendMode.Screen),
                Layer(
                    3,
                    "Multiply",
                    opacity: 0.72f,
                    fill: 0.8f,
                    blendMode: PrismBlendMode.Multiply)),
            PrismTestData.Composition(
                "MaskAndClip",
                Layer(
                    4,
                    "ClippedMask",
                    opacity: 0.8f,
                    fill: 0.6f,
                    mask: new PrismMaskDefinition(new PrismResourceId(71)),
                    clipToBelow: true),
                Layer(5, "Base", opacity: 0.55f, fill: 0.85f)),
            PrismTestData.Composition(
                "IsolatedGroup",
                new PrismGroupDefinition(
                    new PrismNodeId(10),
                    "Isolated",
                    [
                        Layer(11, "Front", opacity: 0.8f, fill: 0.45f),
                        Layer(12, "Back", opacity: 0.65f, fill: 0.9f)
                    ],
                    opacity: 0.7f,
                    blendMode: PrismBlendMode.Normal)),
            PrismTestData.Composition(
                "PassThroughGroup",
                new PrismGroupDefinition(
                    new PrismNodeId(20),
                    "PassThrough",
                    [Layer(21, "Child", opacity: 0.75f, fill: 0.4f)],
                    filters: [new PrismFilterDefinition(PrismFilterId.Blur)],
                    opacity: 0.5f,
                    blendMode: PrismBlendMode.PassThrough),
                Layer(22, "Background", opacity: 0.62f, fill: 0.82f))
        ];

        foreach (PrismCompositionDefinition definition in definitions)
        {
            PrismGraph raw = BuildGraph(definition).Graph;
            PrismGraph optimized =
                new PrismGraphOptimizer().Optimize(raw).OptimizedGraph;

            AssertNumericallyEquivalent(raw, optimized);
            Assert.Equal(
                SemanticSnapshot(raw),
                SemanticSnapshot(optimized));
        }
    }

    [Fact]
    public void CatalogOperationsAreNotFusedWithoutDeclaredEquivalence()
    {
        PrismLayerDefinition layer = new(
            new PrismNodeId(1),
            "TwoBlurs",
            filters:
            [
                new PrismFilterDefinition(PrismFilterId.Blur),
                new PrismFilterDefinition(PrismFilterId.Blur)
            ]);
        PrismGraph raw = BuildGraph(
            PrismTestData.Composition("NoInventedFusion", layer)).Graph;

        PrismGraphExecutionPlan plan = new PrismGraphOptimizer().Optimize(raw);

        Assert.Equal(
            2,
            plan.OptimizedGraph.Nodes.Count(
                node => node.Kind == PrismGraphNodeKind.Filter));
        Assert.Equal(
            SemanticSnapshot(raw),
            SemanticSnapshot(plan.OptimizedGraph));
    }

    [Fact]
    public void DeclaredThresholdFusionUsesTypedValuesAndPreservesOutput()
    {
        PrismLayerDefinition layer = new(
            new PrismNodeId(1),
            "TwoThresholds",
            filters:
            [
                new PrismFilterDefinition(PrismFilterId.Threshold),
                new PrismFilterDefinition(PrismFilterId.Threshold)
            ]);
        PrismGraph raw = BuildGraph(
            PrismTestData.Composition("TypedFusion", layer),
            instance => SetFilterNumber(
                instance.GetLayerState(layer.Id).Filters[0],
                "Level",
                0.5f)).Graph;

        PrismGraphExecutionPlan plan = new PrismGraphOptimizer().Optimize(raw);

        Assert.Equal(
            2,
            raw.Nodes.Count(node => node.Kind == PrismGraphNodeKind.Filter));
        Assert.Equal(
            PrismFilterId.Threshold,
            Assert.Single(
                plan.OptimizedGraph.Nodes.Where(
                    node => node.Kind == PrismGraphNodeKind.Filter)).Filter);
        Assert.Single(
            plan.RemovedNodeIds.Where(
                id => raw.GetNode(id).Kind == PrismGraphNodeKind.Filter));
        AssertNumericallyEquivalent(raw, plan.OptimizedGraph);
    }

    [Fact]
    public void FusionRequiresEqualTypedValuesAndPreservesNonCommutativeOrder()
    {
        PrismLayerDefinition unequalLayer = new(
            new PrismNodeId(1),
            "UnequalThresholds",
            filters:
            [
                new PrismFilterDefinition(PrismFilterId.Threshold),
                new PrismFilterDefinition(PrismFilterId.Threshold)
            ]);
        PrismGraph unequal = BuildGraph(
            PrismTestData.Composition("UnequalFusion", unequalLayer),
            instance =>
            {
                PrismLayerState state = instance.GetLayerState(unequalLayer.Id);
                SetFilterNumber(state.Filters[0], "Level", 0.4f);
                SetFilterNumber(state.Filters[1], "Level", 0.6f);
            }).Graph;

        PrismGraphExecutionPlan unequalPlan =
            new PrismGraphOptimizer().Optimize(unequal);
        Assert.Equal(
            2,
            unequalPlan.OptimizedGraph.Nodes.Count(
                node => node.Kind == PrismGraphNodeKind.Filter));

        PrismLayerDefinition orderedLayer = new(
            new PrismNodeId(2),
            "Ordered",
            filters:
            [
                new PrismFilterDefinition(PrismFilterId.Threshold),
                new PrismFilterDefinition(PrismFilterId.Invert),
                new PrismFilterDefinition(PrismFilterId.Threshold)
            ]);
        PrismGraph ordered = BuildGraph(
            PrismTestData.Composition("NonCommutativeFusion", orderedLayer),
            instance =>
            {
                PrismLayerState state = instance.GetLayerState(orderedLayer.Id);
                SetFilterNumber(state.Filters[0], "Level", 0.5f);
                SetFilterNumber(state.Filters[2], "Level", 0.5f);
            }).Graph;

        PrismGraphExecutionPlan orderedPlan =
            new PrismGraphOptimizer().Optimize(ordered);
        Assert.Equal(
            new[]
            {
                PrismFilterId.Threshold,
                PrismFilterId.Invert,
                PrismFilterId.Threshold
            },
            orderedPlan.OptimizedGraph.Nodes
                .Where(node => node.Kind == PrismGraphNodeKind.Filter)
                .OrderBy(node => node.DefinitionOrder)
                .Select(node => node.Filter!.Value));
        AssertNumericallyEquivalent(ordered, orderedPlan.OptimizedGraph);
    }

    [Fact]
    public void TypedAdjustmentNoOpsAreRemovedWithoutReorderingActiveFilters()
    {
        PrismLayerDefinition layer = new(
            new PrismNodeId(1),
            "AdjustmentNoOps",
            filters:
            [
                new PrismFilterDefinition(PrismFilterId.ChannelMixer),
                new PrismFilterDefinition(PrismFilterId.BrightnessContrast),
                new PrismFilterDefinition(PrismFilterId.Threshold),
                new PrismFilterDefinition(PrismFilterId.SelectiveColor),
                new PrismFilterDefinition(PrismFilterId.Exposure)
            ]);
        PrismGraph raw = BuildGraph(
            PrismTestData.Composition("TypedAdjustmentNoOps", layer),
            instance =>
            {
                PrismLayerState state = instance.GetLayerState(layer.Id);
                SetFilterNumber(state.Filters[4], "Exposure", 0.25f);
            }).Graph;

        PrismGraphExecutionPlan plan = new PrismGraphOptimizer().Optimize(raw);

        Assert.Equal(
            new[]
            {
                PrismFilterId.Threshold,
                PrismFilterId.Exposure
            },
            plan.OptimizedGraph.Nodes
                .Where(node => node.Kind == PrismGraphNodeKind.Filter)
                .OrderBy(node => node.DefinitionOrder)
                .Select(node => node.Filter!.Value));
        Assert.Equal(
            3,
            plan.RemovedNodeIds.Count(
                id => raw.GetNode(id).Kind == PrismGraphNodeKind.Filter));
    }

    private static PrismGraphExecutionPlan OptimizeConfigured(
        PrismCompositionDefinition definition,
        Action<PrismInstance> configure,
        DrawRect bounds)
    {
        (PrismGraph graph, _) = BuildGraph(definition, configure, bounds);
        return new PrismGraphOptimizer().Optimize(graph);
    }

    private static void SetStyleNoOp(PrismStyleState state)
    {
        string[] opacityNames =
            state.Style == PrismStyleId.BevelEmboss
                ? ["HighlightOpacity", "ShadowOpacity"]
                : ["Opacity"];
        PrismCatalogEntryDescriptor entry =
            PrismCatalogRuntime.GetEntry((int)state.Style);
        foreach (string opacityName in opacityNames)
        {
            PrismCatalogPropertyDescriptor property = Assert.Single(
                entry.Properties.Where(
                    candidate =>
                        string.Equals(
                            candidate.Name,
                            opacityName,
                            StringComparison.Ordinal)));
            GeneratedMarkup.SetPrismStyleNumber(
                state,
                (int)state.Style,
                property.TypeSlot,
                0);
        }
    }

    private static void SetFilterNumber(
        PrismFilterState state,
        string propertyName,
        float value)
    {
        PrismCatalogPropertyDescriptor property = Assert.Single(
            PrismCatalogRuntime.GetEntry((int)state.Filter).Properties.Where(
                candidate => string.Equals(
                    candidate.Name,
                    propertyName,
                    StringComparison.Ordinal)));
        GeneratedMarkup.SetPrismFilterNumber(
            state,
            (int)state.Filter,
            property.TypeSlot,
            value);
    }

    private static PrismLayerDefinition Layer(
        int id,
        string name,
        bool visible = true,
        float opacity = 1,
        float fill = 1,
        PrismBlendMode blendMode = PrismBlendMode.Normal,
        PrismMaskDefinition? mask = null,
        bool clipToBelow = false)
    {
        return new PrismLayerDefinition(
            new PrismNodeId(id),
            name,
            filters: [new PrismFilterDefinition(PrismFilterId.Blur)],
            mask: mask,
            visible: visible,
            opacity: opacity,
            fill: fill,
            blendMode: blendMode,
            clipToBelow: clipToBelow);
    }

    private static (PrismGraph Graph, PrismDrawScope Scope) BuildGraph(
        PrismCompositionDefinition definition,
        Action<PrismInstance>? configure = null,
        DrawRect bounds = default)
    {
        PrismDrawScope scope = PrismTestData.Scope(definition, bounds: bounds);
        configure?.Invoke(scope.Instance);
        DrawCommandList commands = PrismTestData.Commands(
            DrawCommand.BeginPrism(scope),
            DrawCommand.FillRectangle(new DrawRect(0, 0, 5, 5), Color.White),
            DrawCommand.EndPrism());
        PrismGraph graph = new PrismGraphBuilder().Build(
            new PrismFrameAnalyzer().Analyze(commands));
        return (graph, scope);
    }

    private static PrismGraph BuildGraph(params PrismDrawScope[] scopes)
    {
        DrawCommandList commands = new();
        foreach (PrismDrawScope scope in scopes)
        {
            commands.Add(DrawCommand.BeginPrism(scope));
            commands.Add(
                DrawCommand.FillRectangle(
                    new DrawRect(0, 0, 5, 5),
                    Color.White));
            commands.Add(DrawCommand.EndPrism());
        }
        return new PrismGraphBuilder().Build(
            new PrismFrameAnalyzer().Analyze(commands));
    }

    private static PrismGraph BuildNestedGraph(
        PrismDrawScope outer,
        PrismDrawScope inner)
    {
        DrawCommandList commands = PrismTestData.Commands(
            DrawCommand.BeginPrism(outer),
            DrawCommand.FillRectangle(new DrawRect(0, 0, 5, 5), Color.White),
            DrawCommand.BeginPrism(inner),
            DrawCommand.FillRectangle(new DrawRect(1, 1, 3, 3), Color.White),
            DrawCommand.EndPrism(),
            DrawCommand.FillRectangle(new DrawRect(0, 0, 2, 2), Color.White),
            DrawCommand.EndPrism());
        return new PrismGraphBuilder().Build(
            new PrismFrameAnalyzer().Analyze(commands));
    }

    private static PrismGraphNodePlan PlanForOperation(
        PrismGraphExecutionPlan plan,
        PrismGraphNodeKind kind)
    {
        PrismGraphNode node = Assert.Single(
            plan.OptimizedGraph.Nodes.Where(
                candidate => candidate.Kind == kind));
        return plan.GetNodePlan(node.Id);
    }

    private static void AssertRect(DrawRect expected, DrawRect actual)
    {
        Assert.Equal(expected.X, actual.X, precision: 4);
        Assert.Equal(expected.Y, actual.Y, precision: 4);
        Assert.Equal(expected.Width, actual.Width, precision: 4);
        Assert.Equal(expected.Height, actual.Height, precision: 4);
    }

    private static string PlanSnapshot(
        PrismGraphExecutionPlan plan,
        bool includeRemoved = true)
    {
        IEnumerable<string> lines =
            plan.ExecutionOrder.Select(id => $"E:{id}")
                .Concat(
                    plan.NodePlans.Select(
                        node =>
                        {
                            string dependencies = string.Join(
                                ",",
                                node.CacheDependencies.Select(
                                    dependency =>
                                        $"{dependency.Kind}/{dependency.Key}/" +
                                        $"{dependency.Version}"));
                            return string.Create(
                                CultureInfo.InvariantCulture,
                                $"N:{node.NodeId}:{node.Bounds.X},{node.Bounds.Y}," +
                                $"{node.Bounds.Width},{node.Bounds.Height}:" +
                                $"{node.BoundsStatus}:{node.IsCacheable}:" +
                                $"{node.UncacheableReasons}:{dependencies}");
                        }))
                .Concat(
                    plan.SurfaceLifetimes.Select(
                        lifetime =>
                            $"L:{lifetime.NodeId}:{lifetime.FirstStep}-{lifetime.LastStep}"))
                .Append($"P:{plan.PeakLiveSurfaces}");
        if (includeRemoved)
        {
            lines = lines.Concat(
                plan.RemovedNodeIds.Select(id => $"R:{id}"));
        }
        return string.Join("\n", lines);
    }

    private static void AssertNumericallyEquivalent(
        PrismGraph raw,
        PrismGraph optimized)
    {
        NumericGraphEvaluator rawEvaluator = new(raw);
        NumericGraphEvaluator optimizedEvaluator = new(optimized);
        PrismGraphScope[] rawScopes = raw.Scopes
            .OrderBy(scope => scope.AnalysisScopeIndex)
            .ToArray();
        PrismGraphScope[] optimizedScopes = optimized.Scopes
            .OrderBy(scope => scope.AnalysisScopeIndex)
            .ToArray();
        Assert.Equal(rawScopes.Length, optimizedScopes.Length);
        for (int index = 0; index < rawScopes.Length; index++)
        {
            Assert.Equal(
                rawScopes[index].AnalysisScopeIndex,
                optimizedScopes[index].AnalysisScopeIndex);
            Assert.Equal(rawScopes[index].Output.HasValue, optimizedScopes[index].Output.HasValue);
            if (rawScopes[index].Output is not PrismGraphNodeId rawOutput ||
                optimizedScopes[index].Output is not PrismGraphNodeId optimizedOutput)
            {
                continue;
            }

            AssertPixel(
                rawEvaluator.Evaluate(rawOutput),
                optimizedEvaluator.Evaluate(optimizedOutput));
        }
    }

    private static void AssertPixel(Pixel expected, Pixel actual)
    {
        const float tolerance = 0.00001f;
        Assert.InRange(MathF.Abs(expected.R - actual.R), 0, tolerance);
        Assert.InRange(MathF.Abs(expected.G - actual.G), 0, tolerance);
        Assert.InRange(MathF.Abs(expected.B - actual.B), 0, tolerance);
        Assert.InRange(MathF.Abs(expected.A - actual.A), 0, tolerance);
    }

    private static string SemanticSnapshot(PrismGraph graph)
    {
        Dictionary<PrismGraphNodeId, List<PrismGraphEdge>> incoming = graph.Edges
            .GroupBy(edge => edge.Target)
            .ToDictionary(group => group.Key, group => group.ToList());
        Dictionary<int, PrismGraphScope> scopes = graph.Scopes
            .ToDictionary(scope => scope.AnalysisScopeIndex);
        Dictionary<PrismGraphNodeId, string> memo = [];

        string Evaluate(PrismGraphNodeId id)
        {
            if (memo.TryGetValue(id, out string? cached))
            {
                return cached;
            }

            PrismGraphNode node = graph.GetNode(id);
            PrismGraphEdge[] inputs = incoming.TryGetValue(
                id,
                out List<PrismGraphEdge>? indexed)
                ? indexed
                    .OrderBy(edge => edge.Kind)
                    .ThenBy(edge => edge.Source.ToString(), StringComparer.Ordinal)
                    .ToArray()
                : [];
            if (node.Kind is PrismGraphNodeKind.Fill or PrismGraphNodeKind.Opacity &&
                node.Amount == 1f)
            {
                PrismGraphEdge content = Assert.Single(
                    inputs.Where(edge => edge.Kind == PrismGraphEdgeKind.Content));
                return memo[id] = Evaluate(content.Source);
            }
            if (node.Kind == PrismGraphNodeKind.Style)
            {
                PrismStylePlan stylePlan = PrismStylePlanner.Create(
                    node,
                    scopes[node.AnalysisScopeIndex]);
                bool noOp = stylePlan.Style == PrismStyleId.BevelEmboss
                    ? stylePlan.Opacity == 0f &&
                        stylePlan.SecondaryOpacity == 0f
                    : stylePlan.Opacity == 0f;
                if (noOp)
                {
                    PrismGraphEdge content = Assert.Single(
                        inputs.Where(
                            edge =>
                                edge.Kind ==
                                PrismGraphEdgeKind.Content));
                    return memo[id] = Evaluate(content.Source);
                }
            }
            if (node.Kind == PrismGraphNodeKind.ColorConversion &&
                inputs.Length == 1)
            {
                PrismGraphNode source = graph.GetNode(inputs[0].Source);
                if (source.Kind == PrismGraphNodeKind.ColorConversion &&
                    source.ColorProfile == node.ColorProfile)
                {
                    return memo[id] = Evaluate(source.Id);
                }
            }

            string parameters = string.Join(
                ",",
                node.Parameters.Select(
                    parameter => string.Create(
                        CultureInfo.InvariantCulture,
                        $"{parameter.Index}:{parameter.Kind}:" +
                        $"{parameter.BooleanValue}:{parameter.IntegerValue}:" +
                        $"{parameter.NumberValue}:{parameter.ColorValue}:" +
                        $"{parameter.VectorValue}:{parameter.ResourceValue.Value}")));
            string inputExpression = string.Join(
                ",",
                inputs.Select(edge => $"{edge.Kind}={Evaluate(edge.Source)}"));
            string expression = string.Create(
                CultureInfo.InvariantCulture,
                $"{node.Kind}[{node.DefinitionNodeId?.Value ?? 0};" +
                $"{node.BlendMode};{node.Amount};{node.Filter};{node.Style};" +
                $"{node.Resource?.Value};{node.ColorProfile};{node.MaskChannel};" +
                $"{node.Feather};{node.Density};{node.Invert};" +
                $"{node.IsIsolationBoundary};{node.LayerSettings};{parameters}]" +
                $"({inputExpression})");
            memo.Add(id, expression);
            return expression;
        }

        return string.Join(
            "\n",
            graph.Scopes
                .OrderBy(scope => scope.AnalysisScopeIndex)
                .Select(
                    scope => scope.Output is PrismGraphNodeId output
                        ? Evaluate(output)
                        : "<none>"));
    }

    private readonly record struct Pixel(float R, float G, float B, float A)
    {
        public static Pixel Transparent => default;

        public static Pixel FromStraight(float red, float green, float blue, float alpha) =>
            new(red * alpha, green * alpha, blue * alpha, alpha);

        public Pixel Scale(float value) =>
            new(R * value, G * value, B * value, A * value);

        public Pixel ScaleRgb(float value) =>
            new(R * value, G * value, B * value, A);

        public static Pixel operator +(Pixel left, Pixel right) =>
            new(
                left.R + right.R,
                left.G + right.G,
                left.B + right.B,
                left.A + right.A);

        public static Pixel operator -(Pixel left, Pixel right) =>
            new(
                left.R - right.R,
                left.G - right.G,
                left.B - right.B,
                left.A - right.A);
    }

    private sealed class NumericGraphEvaluator
    {
        private readonly IReadOnlyDictionary<PrismGraphNodeId, PrismGraphNode> nodes;
        private readonly IReadOnlyDictionary<int, PrismGraphScope> scopes;
        private readonly IReadOnlyDictionary<
            (PrismGraphNodeId Target, PrismGraphEdgeKind Kind),
            PrismGraphNodeId> inputs;

        public NumericGraphEvaluator(PrismGraph graph)
        {
            nodes = graph.Nodes.ToDictionary(node => node.Id);
            scopes = graph.Scopes.ToDictionary(
                scope => scope.AnalysisScopeIndex);
            inputs = graph.Edges
                .GroupBy(edge => (edge.Target, edge.Kind))
                .ToDictionary(
                    group => group.Key,
                    group => Assert.Single(group).Source);
        }

        public Pixel Evaluate(PrismGraphNodeId output)
        {
            return EvaluateRoot(output, ImmutableDictionary<PrismGraphNodeId, Pixel>.Empty);
        }

        private Pixel EvaluateRoot(
            PrismGraphNodeId output,
            IReadOnlyDictionary<PrismGraphNodeId, Pixel> overrides)
        {
            return EvaluateCore(
                output,
                overrides,
                new Dictionary<PrismGraphNodeId, Pixel>(),
                []);
        }

        private Pixel EvaluateCore(
            PrismGraphNodeId id,
            IReadOnlyDictionary<PrismGraphNodeId, Pixel> overrides,
            Dictionary<PrismGraphNodeId, Pixel> memo,
            HashSet<PrismGraphNodeId> visiting)
        {
            if (overrides.TryGetValue(id, out Pixel replacement))
            {
                return replacement;
            }
            if (memo.TryGetValue(id, out Pixel cached))
            {
                return cached;
            }
            Assert.True(visiting.Add(id), $"Cycle at {id}.");

            PrismGraphNode node = nodes[id];
            Pixel result = node.Kind switch
            {
                PrismGraphNodeKind.ControlCapture => ControlSource(node),
                PrismGraphNodeKind.BackdropInput => BackdropSource(node),
                PrismGraphNodeKind.ColorConversion =>
                    EvaluateColorConversion(node, overrides, memo, visiting),
                PrismGraphNodeKind.Layer =>
                    Input(node, PrismGraphEdgeKind.Control, overrides, memo, visiting),
                PrismGraphNodeKind.Group =>
                    Input(node, PrismGraphEdgeKind.GroupContent, overrides, memo, visiting),
                PrismGraphNodeKind.Filter =>
                    EvaluateUniformFilter(node, overrides, memo, visiting),
                PrismGraphNodeKind.Style =>
                    throw new InvalidOperationException(
                        "The numeric differential probe has no uniform style model."),
                PrismGraphNodeKind.Mask => MaskSource(node),
                PrismGraphNodeKind.Fill =>
                    Input(node, PrismGraphEdgeKind.Content, overrides, memo, visiting)
                        .ScaleRgb(RequiredAmount(node)),
                PrismGraphNodeKind.Opacity =>
                    Input(node, PrismGraphEdgeKind.Content, overrides, memo, visiting)
                        .Scale(RequiredAmount(node)),
                PrismGraphNodeKind.ClipToBelow =>
                    EvaluateClip(node, overrides, memo, visiting),
                PrismGraphNodeKind.Composite =>
                    EvaluateComposite(node, overrides, memo, visiting),
                PrismGraphNodeKind.PassThroughComposite =>
                    EvaluatePassThrough(node, overrides, memo, visiting),
                _ => throw new InvalidOperationException(
                    $"Unsupported numeric Prism node kind '{node.Kind}'.")
            };

            Assert.True(
                float.IsFinite(result.R) &&
                float.IsFinite(result.G) &&
                float.IsFinite(result.B) &&
                float.IsFinite(result.A),
                $"Node {node.Id} produced a non-finite pixel.");
            visiting.Remove(id);
            memo.Add(id, result);
            return result;
        }

        private Pixel EvaluateColorConversion(
            PrismGraphNode node,
            IReadOnlyDictionary<PrismGraphNodeId, Pixel> overrides,
            Dictionary<PrismGraphNodeId, Pixel> memo,
            HashSet<PrismGraphNodeId> visiting)
        {
            PrismGraphEdgeKind inputKind = HasInput(node, PrismGraphEdgeKind.Content)
                ? PrismGraphEdgeKind.Content
                : PrismGraphEdgeKind.Backdrop;
            return Input(node, inputKind, overrides, memo, visiting);
        }

        private Pixel EvaluateUniformFilter(
            PrismGraphNode node,
            IReadOnlyDictionary<PrismGraphNodeId, Pixel> overrides,
            Dictionary<PrismGraphNodeId, Pixel> memo,
            HashSet<PrismGraphNodeId> visiting)
        {
            Pixel input = Input(
                node,
                PrismGraphEdgeKind.Content,
                overrides,
                memo,
                visiting);
            if (node.Filter is PrismFilterId adjustment &&
                PrismAdjustmentPlanner.IsSupported(adjustment) &&
                node.BlendMode == PrismBlendMode.Normal &&
                node.Amount == 1f)
            {
                PrismGraphScope scope = scopes[node.AnalysisScopeIndex];
                PrismPremultipliedColor adjusted =
                    PrismAdjustmentMath.Apply(
                        PrismAdjustmentPlanner.Create(node, scope),
                        new PrismPremultipliedColor(
                            input.R,
                            input.G,
                            input.B,
                            input.A),
                        scope.CompositionSettings.WorkingColorProfile);
                return new Pixel(
                    (float)adjusted.Red,
                    (float)adjusted.Green,
                    (float)adjusted.Blue,
                    (float)adjusted.Alpha);
            }

            if (node.Filter is not (
                    PrismFilterId.Blur or
                    PrismFilterId.BlurMore or
                    PrismFilterId.BoxBlur or
                    PrismFilterId.GaussianBlur or
                    PrismFilterId.Transform) ||
                node.BlendMode != PrismBlendMode.Normal ||
                node.Amount != 1f)
            {
                throw new InvalidOperationException(
                    $"The numeric differential probe cannot model filter '{node.Filter}'.");
            }

            return input;
        }

        private Pixel EvaluateClip(
            PrismGraphNode node,
            IReadOnlyDictionary<PrismGraphNodeId, Pixel> overrides,
            Dictionary<PrismGraphNodeId, Pixel> memo,
            HashSet<PrismGraphNodeId> visiting)
        {
            Pixel content = Input(
                node,
                PrismGraphEdgeKind.Content,
                overrides,
                memo,
                visiting);
            Pixel clipBase = Input(
                node,
                PrismGraphEdgeKind.ClipBaseAlpha,
                overrides,
                memo,
                visiting);
            return content.Scale(clipBase.A);
        }

        private Pixel EvaluateComposite(
            PrismGraphNode node,
            IReadOnlyDictionary<PrismGraphNodeId, Pixel> overrides,
            Dictionary<PrismGraphNodeId, Pixel> memo,
            HashSet<PrismGraphNodeId> visiting)
        {
            if (HasInput(node, PrismGraphEdgeKind.MaskAlpha))
            {
                Pixel content = Input(
                    node,
                    PrismGraphEdgeKind.Content,
                    overrides,
                    memo,
                    visiting);
                PrismGraphNodeId maskId = InputId(
                    node,
                    PrismGraphEdgeKind.MaskAlpha);
                Pixel mask = EvaluateCore(maskId, overrides, memo, visiting);
                PrismGraphNode maskNode = nodes[maskId];
                if (maskNode.Feather is float feather && feather != 0f)
                {
                    throw new InvalidOperationException(
                        "A one-pixel numeric probe cannot model mask feathering.");
                }

                float coverage = maskNode.MaskChannel switch
                {
                    PrismMaskChannel.Alpha => mask.A,
                    PrismMaskChannel.Luminance =>
                        (0.2126f * Unpremultiply(mask.R, mask.A)) +
                        (0.7152f * Unpremultiply(mask.G, mask.A)) +
                        (0.0722f * Unpremultiply(mask.B, mask.A)),
                    _ => throw new InvalidOperationException(
                        $"Unsupported mask channel '{maskNode.MaskChannel}'.")
                };
                if (maskNode.Invert == true)
                {
                    coverage = 1f - coverage;
                }
                float density = maskNode.Density ?? 1f;
                coverage = 1f - (density * (1f - coverage));
                return content.Scale(coverage);
            }

            Pixel background = TryInput(
                node,
                PrismGraphEdgeKind.CompositeBackground,
                overrides,
                memo,
                visiting,
                out Pixel indexedBackground)
                ? indexedBackground
                : Pixel.Transparent;
            Pixel foreground = Input(
                node,
                PrismGraphEdgeKind.CompositeForeground,
                overrides,
                memo,
                visiting);
            return Blend(
                background,
                foreground,
                node.BlendMode ?? PrismBlendMode.Normal);
        }

        private Pixel EvaluatePassThrough(
            PrismGraphNode node,
            IReadOnlyDictionary<PrismGraphNodeId, Pixel> overrides,
            Dictionary<PrismGraphNodeId, Pixel> memo,
            HashSet<PrismGraphNodeId> visiting)
        {
            PrismGraphNodeId groupId = InputId(
                node,
                PrismGraphEdgeKind.GroupContent);
            PrismGraphNodeId postId = InputId(
                node,
                PrismGraphEdgeKind.CompositeForeground);
            Pixel background = TryInput(
                node,
                PrismGraphEdgeKind.CompositeBackground,
                overrides,
                memo,
                visiting,
                out Pixel indexedBackground)
                ? indexedBackground
                : Pixel.Transparent;
            Pixel postProcessedGroup =
                EvaluateCore(postId, overrides, memo, visiting);
            Dictionary<PrismGraphNodeId, Pixel> replayOverrides =
                new(overrides)
                {
                    [groupId] = background
                };
            Pixel processedBackdrop = EvaluateRoot(postId, replayOverrides);
            return background + (postProcessedGroup - processedBackdrop);
        }

        private Pixel Input(
            PrismGraphNode node,
            PrismGraphEdgeKind kind,
            IReadOnlyDictionary<PrismGraphNodeId, Pixel> overrides,
            Dictionary<PrismGraphNodeId, Pixel> memo,
            HashSet<PrismGraphNodeId> visiting)
        {
            return EvaluateCore(InputId(node, kind), overrides, memo, visiting);
        }

        private bool TryInput(
            PrismGraphNode node,
            PrismGraphEdgeKind kind,
            IReadOnlyDictionary<PrismGraphNodeId, Pixel> overrides,
            Dictionary<PrismGraphNodeId, Pixel> memo,
            HashSet<PrismGraphNodeId> visiting,
            out Pixel value)
        {
            if (!inputs.TryGetValue((node.Id, kind), out PrismGraphNodeId inputId))
            {
                value = default;
                return false;
            }

            value = EvaluateCore(inputId, overrides, memo, visiting);
            return true;
        }

        private PrismGraphNodeId InputId(
            PrismGraphNode node,
            PrismGraphEdgeKind kind)
        {
            return inputs.TryGetValue((node.Id, kind), out PrismGraphNodeId input)
                ? input
                : throw new InvalidOperationException(
                    $"Node '{node.Id}' has no '{kind}' input.");
        }

        private bool HasInput(
            PrismGraphNode node,
            PrismGraphEdgeKind kind) =>
            inputs.ContainsKey((node.Id, kind));

        private static float RequiredAmount(PrismGraphNode node)
        {
            return node.Amount is float amount
                ? amount
                : throw new InvalidOperationException(
                    $"Node '{node.Id}' has no amount.");
        }

        private static Pixel ControlSource(PrismGraphNode node)
        {
            float seed = node.Id.ScopeOwnerToken.Value % 7;
            return Pixel.FromStraight(
                0.18f + (seed * 0.04f),
                0.61f - (seed * 0.025f),
                0.32f + (seed * 0.03f),
                0.73f);
        }

        private static Pixel BackdropSource(PrismGraphNode node)
        {
            float seed = node.Id.ScopeOwnerToken.Value % 5;
            return Pixel.FromStraight(
                0.62f - (seed * 0.035f),
                0.21f + (seed * 0.045f),
                0.49f,
                0.81f);
        }

        private static Pixel MaskSource(PrismGraphNode node)
        {
            float seed = (node.Resource?.Value ?? 1) % 5;
            return Pixel.FromStraight(
                0.25f + (seed * 0.08f),
                0.72f - (seed * 0.06f),
                0.41f,
                0.28f + (seed * 0.11f));
        }

        private static Pixel Blend(
            Pixel backdrop,
            Pixel source,
            PrismBlendMode mode)
        {
            float backdropAlpha = backdrop.A;
            float sourceAlpha = source.A;
            float backdropRed = Unpremultiply(backdrop.R, backdropAlpha);
            float backdropGreen = Unpremultiply(backdrop.G, backdropAlpha);
            float backdropBlue = Unpremultiply(backdrop.B, backdropAlpha);
            float sourceRed = Unpremultiply(source.R, sourceAlpha);
            float sourceGreen = Unpremultiply(source.G, sourceAlpha);
            float sourceBlue = Unpremultiply(source.B, sourceAlpha);
            return new Pixel(
                BlendChannel(
                    backdrop.R,
                    source.R,
                    backdropRed,
                    sourceRed,
                    backdropAlpha,
                    sourceAlpha,
                    mode),
                BlendChannel(
                    backdrop.G,
                    source.G,
                    backdropGreen,
                    sourceGreen,
                    backdropAlpha,
                    sourceAlpha,
                    mode),
                BlendChannel(
                    backdrop.B,
                    source.B,
                    backdropBlue,
                    sourceBlue,
                    backdropAlpha,
                    sourceAlpha,
                    mode),
                sourceAlpha + backdropAlpha - (sourceAlpha * backdropAlpha));
        }

        private static float BlendChannel(
            float backdropPremultiplied,
            float sourcePremultiplied,
            float backdropStraight,
            float sourceStraight,
            float backdropAlpha,
            float sourceAlpha,
            PrismBlendMode mode)
        {
            float blended = mode switch
            {
                PrismBlendMode.Normal => sourceStraight,
                PrismBlendMode.Multiply => backdropStraight * sourceStraight,
                PrismBlendMode.Screen =>
                    backdropStraight +
                    sourceStraight -
                    (backdropStraight * sourceStraight),
                _ => throw new InvalidOperationException(
                    $"Unsupported numeric blend mode '{mode}'.")
            };

            return
                ((1f - sourceAlpha) * backdropPremultiplied) +
                ((1f - backdropAlpha) * sourcePremultiplied) +
                (backdropAlpha * sourceAlpha * blended);
        }

        private static float Unpremultiply(float value, float alpha) =>
            alpha <= 0.0000001f ? 0f : value / alpha;
    }
}
