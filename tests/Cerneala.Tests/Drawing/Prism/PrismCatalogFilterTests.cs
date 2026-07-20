using System.Numerics;
using Cerneala.Drawing;
using Cerneala.Drawing.Prism;
using Cerneala.Drawing.Prism.Catalog;
using Cerneala.Drawing.Prism.ColorManagement;
using Cerneala.Drawing.Prism.Filters;
using Cerneala.Drawing.Prism.Graph;
using Cerneala.UI.Markup;
using Cerneala.UI.Prism.Definitions;
using Cerneala.UI.Prism.Runtime;

namespace Cerneala.Tests.Drawing.Prism;

public sealed class PrismCatalogFilterTests
{
    [Fact]
    public void CatalogDrivesEveryRemainingPlannerKernelTestAndDocumentation()
    {
        PrismCatalogEntryDescriptor[] entries = CatalogEntries();
        PrismGraph graph = BuildAllGraph(entries);
        PrismGraphNode[] filterNodes = graph.Nodes
            .Where(node =>
                node.Kind == PrismGraphNodeKind.Filter)
            .ToArray();

        Assert.Equal(74, entries.Length);
        Assert.Equal(
            entries.Select(entry => entry.StableId),
            entries
                .Select(entry => entry.StableId)
                .Distinct());
        foreach (PrismCatalogEntryDescriptor entry in entries)
        {
            PrismFilterId filter =
                (PrismFilterId)entry.StableId;
            PrismGraphNode[] nodes = filterNodes
                .Where(node => node.Filter == filter)
                .OrderBy(node =>
                    node.CatalogFilterPassIndex)
                .ToArray();

            Assert.NotEmpty(nodes);
            Assert.True(
                PrismCatalogFilterPlanner.IsSupported(filter));
            Assert.StartsWith(
                "generated:PrismGraphBuilder/CatalogEntry/",
                entry.Coverage.Planner,
                StringComparison.Ordinal);
            Assert.Equal(
                $"PrismKernelRegistry/{entry.Symbol}",
                entry.Coverage.Kernel);
            Assert.Equal(
                $"PrismCatalogFilterTests/{entry.Symbol}",
                entry.Coverage.Test);
            Assert.StartsWith(
                "generated:",
                entry.Coverage.Documentation,
                StringComparison.Ordinal);
            Assert.DoesNotContain(
                "planned:",
                string.Join(
                    '|',
                    entry.Coverage.Runtime,
                    entry.Coverage.Planner,
                    entry.Coverage.Kernel,
                    entry.Coverage.Test,
                    entry.Coverage.Documentation),
                StringComparison.Ordinal);
            Assert.True(entry.Deterministic);
            Assert.True(entry.Cacheable);
            Assert.NotNull(entry.Execution);
            Assert.NotEmpty(
                Assert.IsType<PrismCatalogExecutionDescriptor>(
                    entry.Execution)
                    .Primitive);
            Assert.InRange(entry.Properties.Length, 0, 9);
            Assert.Equal(
                Enumerable.Range(0, entry.Properties.Length),
                entry.Properties.Select(property =>
                    property.Slot));

            PrismGraphNode first = nodes[0];
            PrismCatalogFilterPlan prepared =
                Assert.IsType<PrismCatalogFilterPlan>(
                    first.CatalogFilterPlan);
            Assert.Equal(filter, prepared.Filter);
            Assert.Equal(
                entry.Properties.Length,
                first.Parameters.Length);
            Assert.All(
                nodes,
                node =>
                {
                    PrismCatalogFilterPlan nodePlan =
                        Assert.IsType<PrismCatalogFilterPlan>(
                            node.CatalogFilterPlan);
                    Assert.Equal(prepared, nodePlan);
                    Assert.InRange(
                        node.CatalogFilterPassIndex,
                        0,
                        prepared.Passes.Length - 1);
                    Assert.Null(node.NeighborhoodPlan);
                    Assert.Null(node.ResamplingPlan);
                });

            AssertParameterPacking(
                entry,
                first.Parameters,
                prepared);
        }
    }

    [Fact]
    public void EveryCatalogFilterIsDeterministicAndKeepsAssociatedAlpha()
    {
        PrismCatalogEntryDescriptor[] entries = CatalogEntries();
        PrismGraph graph = BuildAllGraph(entries);
        PrismPremultipliedColor[] source = SampleImage();
        Func<Vector2, Vector4> primary = uv =>
            new Vector4(
                uv.X,
                1 - uv.Y,
                0.5f,
                1);
        Func<Vector2, Vector4> auxiliary = uv =>
            new Vector4(
                1 - uv.X,
                uv.Y,
                0.25f,
                1);

        foreach (PrismCatalogEntryDescriptor entry in entries)
        {
            PrismFilterId filter =
                (PrismFilterId)entry.StableId;
            PrismCatalogFilterPlan plan =
                Assert.IsType<PrismCatalogFilterPlan>(
                    graph.Nodes.First(node =>
                        node.Kind ==
                            PrismGraphNodeKind.Filter &&
                        node.Filter == filter)
                        .CatalogFilterPlan);

            PrismPremultipliedColor[] first =
                PrismCatalogFilterMath.Apply(
                    plan,
                    source,
                    4,
                    4,
                    PrismColorProfile.LinearSrgb,
                    primaryResource: primary,
                    auxiliaryResource: auxiliary);
            PrismPremultipliedColor[] second =
                PrismCatalogFilterMath.Apply(
                    plan,
                    source,
                    4,
                    4,
                    PrismColorProfile.LinearSrgb,
                    primaryResource: primary,
                    auxiliaryResource: auxiliary);

            Assert.Equal(first, second);
            Assert.All(first, AssertFiniteAssociated);
        }
    }

    [Fact]
    public void ProceduralSeedRepeatsAndChangesThePattern()
    {
        PrismPremultipliedColor[] source = SampleImage();
        PrismCatalogFilterPlan seedSeven = CreatePlan(
            PrismFilterId.Clouds,
            configure: (state, entry) =>
                SetInteger(
                    state,
                    entry,
                    "Seed",
                    7));
        PrismCatalogFilterPlan seedEight = CreatePlan(
            PrismFilterId.Clouds,
            configure: (state, entry) =>
                SetInteger(
                    state,
                    entry,
                    "Seed",
                    8));

        PrismPremultipliedColor[] first =
            PrismCatalogFilterMath.Apply(
                seedSeven,
                source,
                4,
                4,
                PrismColorProfile.LinearSrgb);
        PrismPremultipliedColor[] repeated =
            PrismCatalogFilterMath.Apply(
                seedSeven,
                source,
                4,
                4,
                PrismColorProfile.LinearSrgb);
        PrismPremultipliedColor[] changed =
            PrismCatalogFilterMath.Apply(
                seedEight,
                source,
                4,
                4,
                PrismColorProfile.LinearSrgb);

        Assert.Equal(first, repeated);
        Assert.False(first.SequenceEqual(changed));
    }

    [Fact]
    public void MorphologyAndIterationFiltersPrepareRequiredPassesAndBounds()
    {
        PrismGraph morphology = CreateGraph(
            PrismFilterId.Maximum,
            new DrawRect(0, 0, 10, 8),
            (state, entry) =>
                SetNumber(
                    state,
                    entry,
                    "Radius",
                    3));
        PrismGraphNode[] morphologyNodes = morphology.Nodes
            .Where(node =>
                node.Kind == PrismGraphNodeKind.Filter &&
                node.Filter == PrismFilterId.Maximum)
            .OrderBy(node =>
                node.CatalogFilterPassIndex)
            .ToArray();
        PrismCatalogFilterPlan morphologyPlan =
            Assert.IsType<PrismCatalogFilterPlan>(
                morphologyNodes[0].CatalogFilterPlan);

        Assert.Equal(2, morphologyNodes.Length);
        Assert.Equal(2, morphologyPlan.Passes.Length);
        Assert.Equal(
            PrismCatalogFilterPassKind.Horizontal,
            morphologyPlan.Passes[0].Kind);
        Assert.Equal(3, morphologyPlan.Passes[0].RadiusX);
        Assert.Equal(3, morphologyPlan.Passes[0].BoundsRadiusX);
        Assert.Equal(
            PrismCatalogFilterPassKind.Vertical,
            morphologyPlan.Passes[1].Kind);
        Assert.Equal(3, morphologyPlan.Passes[1].RadiusY);
        Assert.Equal(3, morphologyPlan.Passes[1].BoundsRadiusY);

        PrismGraph facet = CreateGraph(
            PrismFilterId.Facet,
            new DrawRect(0, 0, 10, 8),
            (state, entry) =>
                SetNumber(
                    state,
                    entry,
                    "Iterations",
                    3));
        PrismGraphNode[] facetNodes = facet.Nodes
            .Where(node =>
                node.Kind == PrismGraphNodeKind.Filter &&
                node.Filter == PrismFilterId.Facet)
            .OrderBy(node =>
                node.CatalogFilterPassIndex)
            .ToArray();
        PrismCatalogFilterPlan facetPlan =
            Assert.IsType<PrismCatalogFilterPlan>(
                facetNodes[0].CatalogFilterPlan);

        Assert.Equal(3, facetNodes.Length);
        Assert.Equal([0, 1, 2], facetPlan.Passes
            .Select(pass => pass.Iteration));
        Assert.All(
            facetPlan.Passes,
            pass => Assert.Equal(
                PrismCatalogFilterPassKind.Iteration,
                pass.Kind));
    }

    [Fact]
    public void ChainedCatalogFiltersPreserveDeclaredOrder()
    {
        PrismPremultipliedColor[] source = SampleImage();
        PrismCatalogFilterPlan halftone =
            CreatePlan(PrismFilterId.ColorHalftone);
        PrismCatalogFilterPlan solarize =
            CreatePlan(PrismFilterId.Solarize);

        PrismPremultipliedColor[] halftoneThenSolarize =
            PrismCatalogFilterMath.Apply(
                solarize,
                PrismCatalogFilterMath.Apply(
                    halftone,
                    source,
                    4,
                    4,
                    PrismColorProfile.LinearSrgb),
                4,
                4,
                PrismColorProfile.LinearSrgb);
        PrismPremultipliedColor[] solarizeThenHalftone =
            PrismCatalogFilterMath.Apply(
                halftone,
                PrismCatalogFilterMath.Apply(
                    solarize,
                    source,
                    4,
                    4,
                    PrismColorProfile.LinearSrgb),
                4,
                4,
                PrismColorProfile.LinearSrgb);

        Assert.False(
            halftoneThenSolarize.SequenceEqual(
                solarizeThenHalftone));
    }

    [Fact]
    public void CatalogFilterStaysInsideGroupMaskClippingAndBlendBoundaries()
    {
        PrismLayerDefinition clipped = new(
            new PrismNodeId(11),
            "Filtered clipped layer",
            filters:
            [
                new PrismFilterDefinition(
                    PrismFilterId.FindEdges)
            ],
            clipToBelow: true,
            blendMode: PrismBlendMode.Multiply);
        PrismLayerDefinition clipBase =
            PrismTestData.Layer(12, "Clip base");
        PrismGroupDefinition group = new(
            new PrismNodeId(10),
            "Isolated filtered group",
            [clipped, clipBase],
            mask: new PrismMaskDefinition(
                new PrismResourceId("catalog-mask")),
            blendMode: PrismBlendMode.Normal);
        PrismDrawScope scope = PrismTestData.Scope(
            PrismTestData.Composition(
                "Catalog filter boundaries",
                group),
            bounds: new DrawRect(0, 0, 20, 10));

        PrismGraph graph = BuildGraph(scope);

        PrismGraphNode filter = Assert.Single(
            graph.Nodes.Where(node =>
                node.Kind == PrismGraphNodeKind.Filter &&
                node.DefinitionNodeId == clipped.Id &&
                node.Filter == PrismFilterId.FindEdges));
        PrismGraphNode groupNode = Assert.Single(
            graph.Nodes.Where(node =>
                node.Kind == PrismGraphNodeKind.Group &&
                node.DefinitionNodeId == group.Id));
        PrismGraphNode mask = Assert.Single(
            graph.Nodes.Where(node =>
                node.Kind == PrismGraphNodeKind.Mask &&
                node.DefinitionNodeId == group.Id));
        PrismGraphNode maskComposite = Assert.Single(
            graph.Nodes.Where(node =>
                node.Kind == PrismGraphNodeKind.Composite &&
                node.DefinitionNodeId == group.Id &&
                graph.Edges.Any(edge =>
                    edge.Source == mask.Id &&
                    edge.Target == node.Id &&
                    edge.Kind ==
                        PrismGraphEdgeKind.MaskAlpha)));
        PrismGraphNode clipping = Assert.Single(
            graph.Nodes.Where(node =>
                node.Kind ==
                    PrismGraphNodeKind.ClipToBelow &&
                node.DefinitionNodeId == clipped.Id));
        PrismGraphNode composite = Assert.Single(
            graph.Nodes.Where(node =>
                node.Kind == PrismGraphNodeKind.Composite &&
                node.DefinitionNodeId == clipped.Id));

        Assert.NotNull(filter.CatalogFilterPlan);
        Assert.True(groupNode.IsIsolationBoundary);
        Assert.Equal(
            PrismBlendMode.Multiply,
            composite.BlendMode);
        Assert.True(
            HasDirectedPath(
                graph,
                filter.Id,
                clipping.Id));
        Assert.Contains(
            graph.Edges,
            edge =>
                edge.Source == groupNode.Id &&
                edge.Target == maskComposite.Id &&
                edge.Kind == PrismGraphEdgeKind.Content);
        Assert.Contains(
            graph.Edges,
            edge =>
                edge.Source == mask.Id &&
                edge.Target == maskComposite.Id &&
                edge.Kind == PrismGraphEdgeKind.MaskAlpha);
    }

    [Fact]
    public void ConformanceGalleryComesFromTheCatalogList()
    {
        PrismCatalogEntryDescriptor[] entries = CatalogEntries();
        PrismFilterConformanceGalleryEntry[] gallery =
            PrismFilterConformanceGallery.Entries.ToArray();

        Assert.Equal(entries.Length, gallery.Length);
        Assert.Equal(
            entries.Select(entry => entry.Symbol),
            gallery.Select(entry => entry.Symbol));
        foreach (PrismFilterConformanceGalleryEntry item in
            gallery)
        {
            PrismLayerDefinition layer =
                Assert.IsType<PrismLayerDefinition>(
                    Assert.Single(
                        item.Composition.Nodes));
            PrismFilterDefinition filter =
                Assert.Single(layer.Filters);

            Assert.Equal(item.Filter, filter.Filter);
            Assert.Equal(
                $"PrismFilterConformance.{item.Symbol}",
                item.Composition.Name);
        }
    }

    private static void AssertParameterPacking(
        PrismCatalogEntryDescriptor entry,
        System.Collections.Immutable.ImmutableArray<
            PrismGraphParameter> parameters,
        PrismCatalogFilterPlan plan)
    {
        PrismFilterId filter =
            (PrismFilterId)entry.StableId;
        PrismFilterParameterReader reader =
            new(filter, parameters);
        PrismCatalogPropertyDescriptor[] resources =
            entry.Properties
                .Where(property =>
                    property.ValueType ==
                        PrismCatalogValueType.Resource)
                .ToArray();

        foreach (PrismCatalogPropertyDescriptor property in
            entry.Properties)
        {
            PrismGraphParameter parameter =
                parameters[property.Slot];
            Assert.Equal(
                ExpectedKind(property.ValueType),
                parameter.Kind);
            Assert.Equal(property.Slot, parameter.Index);
            if (property.ValueType ==
                PrismCatalogValueType.Resource)
            {
                continue;
            }

            Vector4 expected = property.ValueType switch
            {
                PrismCatalogValueType.Boolean =>
                    new Vector4(
                        parameter.BooleanValue ? 1 : 0,
                        0,
                        0,
                        0),
                PrismCatalogValueType.Integer or
                    PrismCatalogValueType.Symbol =>
                    PackInteger(parameter.IntegerValue),
                PrismCatalogValueType.Number =>
                    new Vector4(
                        parameter.NumberValue,
                        0,
                        0,
                        0),
                PrismCatalogValueType.Color =>
                    reader.Color(property.Name),
                PrismCatalogValueType.Vector =>
                    parameter.VectorValue,
                _ => throw new InvalidOperationException(
                    $"Unexpected catalog type {property.ValueType}.")
            };
            Assert.Equal(
                expected,
                plan.GetOption(property.Slot));
        }

        if (resources.Length > 0)
        {
            Assert.Equal(
                parameters[resources[0].Slot]
                    .ResourceValue,
                plan.PrimaryResource);
            Assert.Equal(
                resources[0].Required,
                plan.PrimaryResourceRequired);
        }
        if (resources.Length > 1)
        {
            Assert.Equal(
                parameters[resources[1].Slot]
                    .ResourceValue,
                plan.AuxiliaryResource);
            Assert.Equal(
                resources[1].Required,
                plan.AuxiliaryResourceRequired);
        }
    }

    private static bool HasDirectedPath(
        PrismGraph graph,
        PrismGraphNodeId source,
        PrismGraphNodeId target)
    {
        Queue<PrismGraphNodeId> pending = new([source]);
        HashSet<PrismGraphNodeId> visited = [source];
        while (pending.TryDequeue(out PrismGraphNodeId current))
        {
            foreach (PrismGraphEdge edge in graph.Edges)
            {
                if (edge.Source != current ||
                    !visited.Add(edge.Target))
                {
                    continue;
                }
                if (edge.Target == target)
                {
                    return true;
                }
                pending.Enqueue(edge.Target);
            }
        }
        return false;
    }

    private static PrismGraphParameterValueKind ExpectedKind(
        PrismCatalogValueType valueType) =>
        valueType switch
        {
            PrismCatalogValueType.Boolean =>
                PrismGraphParameterValueKind.Boolean,
            PrismCatalogValueType.Integer =>
                PrismGraphParameterValueKind.Integer,
            PrismCatalogValueType.Number =>
                PrismGraphParameterValueKind.Number,
            PrismCatalogValueType.Color =>
                PrismGraphParameterValueKind.Color,
            PrismCatalogValueType.Vector =>
                PrismGraphParameterValueKind.Vector,
            PrismCatalogValueType.Symbol =>
                PrismGraphParameterValueKind.Symbol,
            PrismCatalogValueType.Resource =>
                PrismGraphParameterValueKind.Resource,
            _ => throw new InvalidOperationException(
                $"Unexpected catalog type {valueType}.")
        };

    private static Vector4 PackInteger(int value)
    {
        uint bits = unchecked((uint)value);
        return new Vector4(
            bits & 0xffffu,
            bits >> 16,
            0,
            0);
    }

    private static PrismPremultipliedColor[] SampleImage()
    {
        PrismPremultipliedColor[] result =
            new PrismPremultipliedColor[16];
        for (int y = 0; y < 4; y++)
        {
            for (int x = 0; x < 4; x++)
            {
                double alpha =
                    ((x + y) % 4) / 3d;
                result[(y * 4) + x] =
                    PrismPremultipliedColor.FromStraight(
                        x / 3d,
                        y / 3d,
                        (x + y) / 6d,
                        alpha);
            }
        }
        return result;
    }

    private static PrismGraph BuildAllGraph(
        PrismCatalogEntryDescriptor[] entries)
    {
        PrismLayerDefinition layer = new(
            new PrismNodeId(1),
            "All remaining catalog filters",
            filters: entries.Select(entry =>
                new PrismFilterDefinition(
                    (PrismFilterId)entry.StableId)));
        PrismDrawScope scope = PrismTestData.Scope(
            PrismTestData.Composition(
                "All remaining catalog filters",
                layer),
            bounds: new DrawRect(0, 0, 64, 48));
        PrismLayerState state =
            scope.Instance.GetLayerState(layer.Id);
        for (int index = 0; index < entries.Length; index++)
        {
            ConfigureRequiredResources(
                state.Filters[index],
                entries[index]);
        }
        return BuildGraph(scope);
    }

    private static PrismCatalogFilterPlan CreatePlan(
        PrismFilterId filter,
        DrawRect? bounds = null,
        Action<
            PrismFilterState,
            PrismCatalogEntryDescriptor>? configure = null)
    {
        PrismGraph graph = CreateGraph(
            filter,
            bounds ?? new DrawRect(
                0,
                0,
                20,
                10),
            configure);
        return Assert.IsType<PrismCatalogFilterPlan>(
            graph.Nodes.First(node =>
                node.Kind == PrismGraphNodeKind.Filter &&
                node.Filter == filter)
                .CatalogFilterPlan);
    }

    private static PrismGraph CreateGraph(
        PrismFilterId filter,
        DrawRect bounds,
        Action<
            PrismFilterState,
            PrismCatalogEntryDescriptor>? configure = null)
    {
        PrismLayerDefinition layer = new(
            new PrismNodeId(1),
            filter.ToString(),
            filters:
            [
                new PrismFilterDefinition(filter)
            ]);
        PrismDrawScope scope = PrismTestData.Scope(
            PrismTestData.Composition(
                $"Plan {filter}",
                layer),
            bounds: bounds);
        PrismFilterState state = Assert.Single(
            scope.Instance
                .GetLayerState(layer.Id)
                .Filters);
        PrismCatalogEntryDescriptor entry =
            PrismCatalogRuntime.GetEntry((int)filter);
        ConfigureRequiredResources(state, entry);
        configure?.Invoke(state, entry);
        return BuildGraph(scope);
    }

    private static void ConfigureRequiredResources(
        PrismFilterState state,
        PrismCatalogEntryDescriptor entry)
    {
        foreach (PrismCatalogPropertyDescriptor property in
            entry.Properties.Where(property =>
                property.Required &&
                property.ValueType ==
                    PrismCatalogValueType.Resource))
        {
            GeneratedMarkup.SetPrismFilterResource(
                state,
                entry.StableId,
                property.TypeSlot,
                new PrismResourceId(
                    $"catalog-{entry.Symbol}-{property.Name}"));
        }
    }

    private static void SetInteger(
        PrismFilterState state,
        PrismCatalogEntryDescriptor entry,
        string name,
        int value) =>
        GeneratedMarkup.SetPrismFilterInteger(
            state,
            entry.StableId,
            Property(entry, name).TypeSlot,
            value);

    private static void SetNumber(
        PrismFilterState state,
        PrismCatalogEntryDescriptor entry,
        string name,
        float value) =>
        GeneratedMarkup.SetPrismFilterNumber(
            state,
            entry.StableId,
            Property(entry, name).TypeSlot,
            value);

    private static PrismCatalogPropertyDescriptor Property(
        PrismCatalogEntryDescriptor entry,
        string name) =>
        entry.Properties.Single(property =>
            property.Name == name);

    private static PrismCatalogEntryDescriptor[]
        CatalogEntries() =>
        PrismCatalogGenerated.Entries
            .Where(entry =>
            {
                if (entry.Kind != "filter")
                {
                    return false;
                }

                PrismFilterId filter =
                    (PrismFilterId)entry.StableId;
                return
                    !PrismAdjustmentPlanner.IsSupported(filter) &&
                    !PrismNeighborhoodPlanner.IsSupported(filter) &&
                    !PrismResamplingPlanner.IsSupported(filter);
            })
            .ToArray();

    private static PrismGraph BuildGraph(
        PrismDrawScope scope)
    {
        DrawCommandList commands = PrismTestData.Commands(
            DrawCommand.BeginPrism(scope),
            DrawCommand.FillRectangle(
                new DrawRect(0, 0, 20, 10),
                new Color(
                    255,
                    255,
                    255)),
            DrawCommand.EndPrism());
        return new PrismGraphBuilder().Build(
            new PrismFrameAnalyzer()
                .Analyze(commands));
    }

    private static void AssertFiniteAssociated(
        PrismPremultipliedColor color)
    {
        Assert.True(double.IsFinite(color.Red));
        Assert.True(double.IsFinite(color.Green));
        Assert.True(double.IsFinite(color.Blue));
        Assert.True(double.IsFinite(color.Alpha));
        Assert.InRange(color.Alpha, 0, 1);
        Assert.InRange(color.Red, 0, color.Alpha);
        Assert.InRange(color.Green, 0, color.Alpha);
        Assert.InRange(color.Blue, 0, color.Alpha);
    }
}
