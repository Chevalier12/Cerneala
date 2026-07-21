using System.Numerics;
using Cerneala.Drawing;
using Cerneala.Drawing.Prism;
using Cerneala.Drawing.Prism.Catalog;
using Cerneala.Drawing.Prism.Graph;
using Cerneala.Drawing.Prism.Styles;
using Cerneala.UI.Markup;
using Cerneala.UI.Prism.Definitions;
using Cerneala.UI.Prism.Runtime;

namespace Cerneala.Tests.Drawing.Prism;

public sealed class PrismStylePipelineTests
{
    [Fact]
    public void EveryCatalogStyleBuildsAPlanFromGeneratedTypedDefaults()
    {
        PrismStyleId[] styles =
            Enum.GetValues<PrismStyleId>();
        PrismLayerDefinition layer = new(
            new PrismNodeId(1),
            "All styles",
            styles: styles.Select(
                style => new PrismStyleDefinition(style)));
        PrismDrawScope drawScope = PrismTestData.Scope(
            PrismTestData.Composition("Style defaults", layer));

        PrismGraph graph = BuildGraph(drawScope);
        PrismGraphScope graphScope = Assert.Single(graph.Scopes);
        PrismGraphNode[] styleNodes = graph.Nodes
            .Where(node => node.Kind == PrismGraphNodeKind.Style)
            .OrderBy(node => node.Id.Ordinal)
            .ToArray();

        Assert.Equal(styles.Length, styleNodes.Length);
        Assert.Equal(
            Enumerable.Range(0, styles.Length),
            styleNodes
                .Select(node =>
                    PrismStylePlanner.Create(node, graphScope).Kind)
                .Order());

        PrismStyleState[] states = drawScope.Instance
            .GetLayerState(layer.Id)
            .Styles
            .ToArray();
        foreach (PrismGraphNode node in styleNodes)
        {
            PrismStyleId style = node.Style!.Value;
            PrismStyleState state =
                states.Single(candidate => candidate.Style == style);
            PrismCatalogEntryDescriptor entry =
                PrismCatalogRuntime.GetEntry((int)style);
            Assert.True(entry.Deterministic);
            Assert.True(entry.Cacheable);
            Assert.True(entry.DependencyVersion > 1);
            Assert.Equal(entry.Properties.Length, node.Parameters.Length);
            Assert.Contains(
                node.Dependencies,
                dependency =>
                    dependency.Kind ==
                        PrismGraphDependencyKind.CatalogEntry &&
                    dependency.Key == entry.StableId &&
                    dependency.Version ==
                        entry.DependencyVersion);

            for (int index = 0;
                index < entry.Properties.Length;
                index++)
            {
                AssertGeneratedValueFlowsToGraph(
                    state,
                    entry,
                    index,
                    node.Parameters[index]);
            }
        }
    }

    [Fact]
    public void TypedStyleSlotsAnimateEveryFamilyWithoutRebuildingDefinitions()
    {
        PrismStyleId[] styles =
            Enum.GetValues<PrismStyleId>();
        PrismLayerDefinition layer = new(
            new PrismNodeId(1),
            "Animated styles",
            styles: styles.Select(
                style => new PrismStyleDefinition(style)));
        PrismDrawScope scope = PrismTestData.Scope(
            PrismTestData.Composition("Typed style slots", layer));
        IReadOnlyList<PrismStyleState> states =
            scope.Instance.GetLayerState(layer.Id).Styles;

        foreach (PrismStyleState state in states)
        {
            PrismCatalogEntryDescriptor entry =
                PrismCatalogRuntime.GetEntry((int)state.Style);
            PrismCatalogPropertyDescriptor property =
                entry.Properties.First(candidate =>
                    candidate.ValueType ==
                        PrismCatalogValueType.Number);
            GeneratedMarkup.SetPrismStyleNumber(
                state,
                entry.StableId,
                property.TypeSlot,
                0.42f);
        }

        PrismGraph graph = BuildGraph(scope);
        foreach (PrismGraphNode node in graph.Nodes.Where(
            candidate =>
                candidate.Kind == PrismGraphNodeKind.Style))
        {
            PrismCatalogEntryDescriptor entry =
                PrismCatalogRuntime.GetEntry((int)node.Style!.Value);
            int propertyIndex = Array.FindIndex(
                entry.Properties,
                property => property.ValueType ==
                    PrismCatalogValueType.Number);
            Assert.Equal(
                0.42f,
                node.Parameters[propertyIndex].NumberValue);
        }
    }

    [Fact]
    public void StylesComposeBottomUpAllowDuplicatesAndUsePreparedPreFillAlpha()
    {
        PrismLayerDefinition layer = new(
            new PrismNodeId(1),
            "Ordered styles",
            filters:
            [
                new PrismFilterDefinition(
                    PrismFilterId.Blur)
            ],
            styles:
            [
                new PrismStyleDefinition(
                    PrismStyleId.DropShadow),
                new PrismStyleDefinition(
                    PrismStyleId.ColorOverlay),
                new PrismStyleDefinition(
                    PrismStyleId.DropShadow),
                new PrismStyleDefinition(
                    PrismStyleId.Stroke)
            ],
            fill: 0);
        PrismGraph graph = BuildGraph(
            PrismTestData.Scope(
                PrismTestData.Composition(
                    "Style order",
                    layer)));
        PrismGraphNode[] styles = graph.Nodes
            .Where(node =>
                node.Kind == PrismGraphNodeKind.Style)
            .ToArray();

        Assert.Equal(
            [
                PrismStyleId.Stroke,
                PrismStyleId.DropShadow,
                PrismStyleId.ColorOverlay,
                PrismStyleId.DropShadow
            ],
            styles.Select(node => node.Style!.Value));
        Assert.Equal(
            [3, 2, 1, 0],
            styles.Select(node => node.Id.Ordinal));
        Assert.Equal(
            2,
            styles.Count(node =>
                node.Style == PrismStyleId.DropShadow));

        PrismGraphNode preparedSource = Assert.Single(
            graph.Nodes.Where(node =>
                node.Kind == PrismGraphNodeKind.Filter));
        PrismGraphNode fill = Assert.Single(
            graph.Nodes.Where(node =>
                node.Kind == PrismGraphNodeKind.Fill));
        Assert.Equal(0f, fill.Amount);
        Assert.All(
            styles,
            style => Assert.Contains(
                graph.Edges,
                edge =>
                    edge.Source == preparedSource.Id &&
                    edge.Target == style.Id &&
                    edge.Kind ==
                        PrismGraphEdgeKind.StyleSource));
        Assert.Contains(
            graph.Edges,
            edge =>
                edge.Source == fill.Id &&
                edge.Target == styles[0].Id &&
                edge.Kind == PrismGraphEdgeKind.Content);
        for (int index = 1; index < styles.Length; index++)
        {
            Assert.Contains(
                graph.Edges,
                edge =>
                    edge.Source == styles[index - 1].Id &&
                    edge.Target == styles[index].Id &&
                    edge.Kind == PrismGraphEdgeKind.Content);
        }
    }

    [Fact]
    public void OptimizerUsesTheSameStyleBoundsPrimitiveForEveryFamily()
    {
        foreach (PrismStyleId style in
            Enum.GetValues<PrismStyleId>())
        {
            PrismLayerDefinition layer = new(
                new PrismNodeId(1),
                style.ToString(),
                styles:
                [
                    new PrismStyleDefinition(style)
                ]);
            PrismDrawScope drawScope = PrismTestData.Scope(
                PrismTestData.Composition(
                    $"Bounds {style}",
                    layer),
                bounds: new DrawRect(10, 20, 40, 30),
                transform: Matrix3x2.CreateScale(1.5f),
                pixelScale: 1.25f);
            PrismGraph graph = BuildGraph(drawScope);
            PrismGraphExecutionPlan executionPlan =
                new PrismGraphOptimizer().Optimize(graph);
            PrismGraphNode styleNode = Assert.Single(
                executionPlan.OptimizedGraph.Nodes.Where(node =>
                    node.Kind == PrismGraphNodeKind.Style));
            PrismGraphScope graphScope =
                Assert.Single(executionPlan.OptimizedGraph.Scopes);
            PrismStylePlan stylePlan =
                PrismStylePlanner.Create(styleNode, graphScope);

            Assert.Equal(
                PrismStylePlanner.ExpandBounds(
                    stylePlan,
                    graphScope,
                    graphScope.Bounds),
                executionPlan.GetNodePlan(styleNode.Id).Bounds);
            Assert.NotEqual(
                PrismGraphBoundsStatus.Unknown,
                executionPlan.GetNodePlan(
                    styleNode.Id).BoundsStatus);
        }
    }

    [Fact]
    public void ResourceVersionsFlowIntoStyleDependencyStampAndCacheability()
    {
        PrismResourceId pattern = new("StylePattern");
        PrismDrawResources resources =
            PrismDrawResources.Create(
            [
                new PrismDrawImageResource(
                    pattern,
                    new TestImage(),
                    Version: 73,
                    Identity: 7_301)
            ]);
        PrismDrawScope resolved = CreatePatternScope(
            pattern,
            resources,
            ownerToken: 10);
        PrismGraph resolvedGraph = BuildGraph(resolved);
        PrismGraphNode resolvedStyle = Assert.Single(
            resolvedGraph.Nodes.Where(node =>
                node.Kind == PrismGraphNodeKind.Style));
        PrismGraphDependency resourceDependency =
            Assert.Single(
                resolvedStyle.Dependencies.Where(
                    dependency =>
                        dependency.Kind ==
                            PrismGraphDependencyKind.Resource));
        Assert.Equal(73, resourceDependency.Version);
        Assert.True(
            new PrismGraphOptimizer()
                .Optimize(resolvedGraph)
                .GetNodePlan(resolvedStyle.Id)
                .IsCacheable);

        PrismDrawScope missing = CreatePatternScope(
            pattern,
            PrismDrawResources.Empty,
            ownerToken: 20);
        PrismGraph missingGraph = BuildGraph(missing);
        PrismGraphNode missingStyle = Assert.Single(
            missingGraph.Nodes.Where(node =>
                node.Kind == PrismGraphNodeKind.Style));
        PrismGraphNodePlan missingPlan =
            new PrismGraphOptimizer()
                .Optimize(missingGraph)
                .GetNodePlan(missingStyle.Id);
        Assert.Contains(
            missingStyle.Dependencies,
            dependency =>
                dependency.Kind ==
                    PrismGraphDependencyKind.Resource &&
                dependency.Version == 0);
        Assert.True(
            missingPlan.UncacheableReasons.HasFlag(
                PrismGraphUncacheableReason
                    .ResourceVersionUnavailable));
    }

    private static PrismDrawScope CreatePatternScope(
        PrismResourceId pattern,
        PrismDrawResources resources,
        long ownerToken)
    {
        PrismLayerDefinition layer = new(
            new PrismNodeId(1),
            "Pattern",
            styles:
            [
                new PrismStyleDefinition(
                    PrismStyleId.PatternOverlay)
            ]);
        PrismDrawScope scope = PrismTestData.Scope(
            PrismTestData.Composition(
                "Pattern resource",
                layer),
            ownerToken: ownerToken,
            resources: resources);
        PrismStyleState state = Assert.Single(
            scope.Instance.GetLayerState(layer.Id).Styles);
        PrismCatalogEntryDescriptor entry =
            PrismCatalogRuntime.GetEntry(
                (int)PrismStyleId.PatternOverlay);
        PrismCatalogPropertyDescriptor property =
            entry.Properties.Single(candidate =>
                candidate.Name == "Pattern");
        GeneratedMarkup.SetPrismStyleResource(
            state,
            entry.StableId,
            property.TypeSlot,
            pattern);
        return scope;
    }

    private static void AssertGeneratedValueFlowsToGraph(
        PrismStyleState state,
        PrismCatalogEntryDescriptor entry,
        int index,
        PrismGraphParameter parameter)
    {
        PrismCatalogPropertyDescriptor property =
            entry.Properties[index];
        Assert.Equal(index, parameter.Index);
        switch (property.ValueType)
        {
            case PrismCatalogValueType.Boolean:
                Assert.Equal(
                    GeneratedMarkup.GetPrismStyleBoolean(
                        state,
                        entry.StableId,
                        property.TypeSlot),
                    parameter.BooleanValue);
                break;
            case PrismCatalogValueType.Integer:
            case PrismCatalogValueType.Symbol:
                Assert.Equal(
                    GeneratedMarkup.GetPrismStyleInteger(
                        state,
                        entry.StableId,
                        property.TypeSlot),
                    parameter.IntegerValue);
                break;
            case PrismCatalogValueType.Number:
                Assert.Equal(
                    GeneratedMarkup.GetPrismStyleNumber(
                        state,
                        entry.StableId,
                        property.TypeSlot),
                    parameter.NumberValue);
                break;
            case PrismCatalogValueType.Color:
                Assert.Equal(
                    GeneratedMarkup.GetPrismStyleColor(
                        state,
                        entry.StableId,
                        property.TypeSlot),
                    parameter.ColorValue);
                break;
            case PrismCatalogValueType.Vector:
                Assert.Equal(
                    GeneratedMarkup.GetPrismStyleVector(
                        state,
                        entry.StableId,
                        property.TypeSlot),
                    parameter.VectorValue);
                break;
            case PrismCatalogValueType.Resource:
                Assert.Equal(
                    GeneratedMarkup.GetPrismStyleResource(
                        state,
                        entry.StableId,
                        property.TypeSlot),
                    parameter.ResourceValue);
                break;
            default:
                throw new InvalidOperationException(
                    $"Unhandled catalog value type '{property.ValueType}'.");
        }
    }

    private static PrismGraph BuildGraph(
        PrismDrawScope scope)
    {
        DrawCommandList commands = PrismTestData.Commands(
            DrawCommand.BeginPrism(scope),
            DrawCommand.FillRectangle(
                new DrawRect(0, 0, 20, 10),
                new Color(255, 255, 255)),
            DrawCommand.EndPrism());
        return new PrismGraphBuilder().Build(
            new PrismFrameAnalyzer().Analyze(commands));
    }

    private sealed class TestImage : IDrawImage
    {
        public int Width => 1;

        public int Height => 1;
    }
}
