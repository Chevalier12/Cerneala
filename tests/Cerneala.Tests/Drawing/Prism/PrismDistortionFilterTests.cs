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

public sealed class PrismDistortionFilterTests
{
    [Fact]
    public void CatalogDrivesEveryResamplingPlannerKernelTestAndDocumentation()
    {
        PrismCatalogEntryDescriptor[] entries =
            DistortionEntries();
        PrismFilterId[] filters = entries
            .Select(entry =>
                (PrismFilterId)entry.StableId)
            .ToArray();
        PrismLayerDefinition layer = new(
            new PrismNodeId(1),
            "All distortion filters",
            filters: filters.Select(
                filter =>
                    new PrismFilterDefinition(filter)));
        PrismDrawScope scope = PrismTestData.Scope(
            PrismTestData.Composition(
                "Distortion defaults",
                layer),
            bounds: new DrawRect(0, 0, 64, 48));
        PrismLayerState layerState =
            scope.Instance.GetLayerState(layer.Id);
        for (int index = 0; index < entries.Length; index++)
        {
            ConfigureRequiredResources(
                layerState.Filters[index],
                entries[index]);
        }

        PrismGraph graph = BuildGraph(scope);
        PrismGraphNode[] nodes = graph.Nodes
            .Where(node =>
                node.Kind == PrismGraphNodeKind.Filter)
            .ToArray();

        Assert.Equal(17, entries.Length);
        Assert.Equal(
            entries.Length,
            nodes
                .Select(node =>
                    Assert.IsType<PrismResamplingPlan>(
                        node.ResamplingPlan)
                        .Operation)
                .Distinct()
                .Count());
        Assert.Equal(
            2,
            nodes.Count(node =>
                node.Filter ==
                    PrismFilterId.DiffuseGlow));
        foreach (PrismCatalogEntryDescriptor entry in entries)
        {
            PrismFilterId filter =
                (PrismFilterId)entry.StableId;
            PrismGraphNode[] filterNodes = nodes
                .Where(node => node.Filter == filter)
                .ToArray();

            Assert.NotEmpty(filterNodes);
            Assert.True(
                PrismResamplingPlanner.IsSupported(filter));
            Assert.Equal(
                $"PrismKernelRegistry/{entry.Symbol}",
                entry.Coverage.Kernel);
            Assert.Equal(
                $"PrismDistortionFilterTests/{entry.Symbol}",
                entry.Coverage.Test);
            Assert.StartsWith(
                "generated:",
                entry.Coverage.Documentation,
                StringComparison.Ordinal);
            Assert.DoesNotContain(
                entry.Properties,
                property => property.Name is
                    "Source" or
                    "Shader" or
                    "ShaderFilename");
            Assert.All(
                filterNodes,
                node =>
                {
                    Assert.Equal(
                        entry.Properties.Length,
                        node.Parameters.Length);
                    PrismResamplingPlan prepared =
                        Assert.IsType<PrismResamplingPlan>(
                            node.ResamplingPlan);
                    Assert.Equal(filter, prepared.Filter);
                    Assert.InRange(
                        node.ResamplingPassIndex,
                        0,
                        prepared.Passes.Length - 1);
                });
            PrismCatalogExecutionDescriptor execution =
                Assert.IsType<PrismCatalogExecutionDescriptor>(
                    entry.Execution);
            Assert.Equal(
                "coordinate-map-morphology",
                execution.Primitive);
            Assert.Equal(
                "linear-premultiplied-rgba",
                execution.SurfaceFormat);
            Assert.Equal(
                "working-profile",
                execution.ColorSpace);
        }
    }

    [Fact]
    public void TransformMapsNegativeCoordinatesAndEveryEdgeMode()
    {
        PrismPremultipliedColor red =
            PrismPremultipliedColor.FromStraight(
                1,
                0,
                0,
                1);
        PrismPremultipliedColor green =
            PrismPremultipliedColor.FromStraight(
                0,
                1,
                0,
                0.5);
        PrismPremultipliedColor blue =
            PrismPremultipliedColor.FromStraight(
                0,
                0,
                1,
                1);
        PrismPremultipliedColor[] source =
            [red, green, blue];

        PrismPremultipliedColor[] transparent =
            ApplyTransform(
                source,
                translateX: 1,
                edgeMode: "Transparent");
        PrismPremultipliedColor[] clamp =
            ApplyTransform(
                source,
                translateX: 1,
                edgeMode: "Clamp");
        PrismPremultipliedColor[] wrap =
            ApplyTransform(
                source,
                translateX: 1,
                edgeMode: "Wrap");
        PrismPremultipliedColor[] mirror =
            ApplyTransform(
                source,
                translateX: 1,
                edgeMode: "Mirror");
        PrismPremultipliedColor[] negative =
            ApplyTransform(
                source,
                translateX: -1,
                edgeMode: "Transparent");

        Assert.Equal(0, transparent[0].Alpha);
        AssertColor(transparent[1], red);
        AssertColor(transparent[2], green);
        AssertColor(clamp[0], red);
        AssertColor(wrap[0], blue);
        Assert.True(mirror[0].Alpha > 0);
        Assert.Equal(0, negative[^1].Alpha);
        Assert.All(
            transparent
                .Concat(clamp)
                .Concat(wrap)
                .Concat(mirror)
                .Concat(negative),
            AssertFiniteAssociated);
    }

    [Fact]
    public void ExtremeScaleRotationAndTransparencyStayFiniteAssociated()
    {
        PrismPremultipliedColor[] source =
        [
            default,
            PrismPremultipliedColor.FromStraight(
                0.9,
                0.2,
                0.1,
                0.25),
            PrismPremultipliedColor.FromStraight(
                0.1,
                0.8,
                0.3,
                0.75),
            PrismPremultipliedColor.FromStraight(
                0.2,
                0.4,
                0.9,
                1)
        ];

        foreach (float scale in new[] { 0.001f, 1_000f })
        {
            PrismResamplingPlan plan = CreatePlan(
                PrismFilterId.Transform,
                new DrawRect(0, 0, 4, 1),
                (state, entry) =>
                {
                    SetVector(
                        state,
                        entry,
                        "Scale",
                        new Vector4(
                            scale,
                            scale,
                            0,
                            0));
                    SetNumber(
                        state,
                        entry,
                        "Rotation",
                        179.99f);
                    SetVector(
                        state,
                        entry,
                        "Skew",
                        new Vector4(
                            45,
                            -45,
                            0,
                            0));
                    SetSymbol(
                        state,
                        entry,
                        "EdgeMode",
                        "Transparent");
                });

            PrismPremultipliedColor[] result =
                PrismResamplingMath.Apply(
                    plan,
                    source,
                    4,
                    1,
                    PrismColorProfile.LinearSrgb);

            Assert.All(
                result,
                AssertFiniteAssociated);
        }
    }

    [Fact]
    public void TypedAuxiliaryResourcesAreValidatedWithoutGenericSource()
    {
        PrismResamplingPlan adaptive =
            CreatePlan(
                PrismFilterId.AdaptiveWideAngle);
        PrismResamplingPlan displace =
            CreatePlan(PrismFilterId.Displace);
        PrismResamplingPlan liquify =
            CreatePlan(PrismFilterId.Liquify);
        PrismPremultipliedColor[] source =
        [
            PrismPremultipliedColor.FromStraight(
                0.4,
                0.5,
                0.6,
                1)
        ];

        Assert.True(adaptive.PrimaryResourceRequired);
        Assert.True(displace.PrimaryResourceRequired);
        Assert.True(liquify.PrimaryResourceRequired);
        Assert.False(liquify.AuxiliaryResourceRequired);
        Assert.True(adaptive.PrimaryResource.Value > 0);
        Assert.True(displace.PrimaryResource.Value > 0);
        Assert.True(liquify.PrimaryResource.Value > 0);
        Assert.Throws<InvalidOperationException>(
            () => PrismResamplingMath.Apply(
                displace,
                source,
                1,
                1,
                PrismColorProfile.LinearSrgb));

        PrismPremultipliedColor[] result =
            PrismResamplingMath.Apply(
                liquify,
                source,
                1,
                1,
                PrismColorProfile.LinearSrgb,
                primaryResource:
                    _ => new Vector4(
                        0.5f,
                        0.5f,
                        0,
                        1));
        AssertColor(result[0], source[0]);
    }

    [Fact]
    public void EveryDistortionIsDeterministicAndFiniteOnSmallImages()
    {
        PrismPremultipliedColor[] source =
        [
            default,
            PrismPremultipliedColor.FromStraight(
                0.9,
                0.1,
                0.2,
                0.4),
            PrismPremultipliedColor.FromStraight(
                0.2,
                0.8,
                0.3,
                1),
            PrismPremultipliedColor.FromStraight(
                0.7,
                0.6,
                0.1,
                0.8),
            PrismPremultipliedColor.FromStraight(
                0.3,
                0.5,
                0.9,
                0.6),
            PrismPremultipliedColor.FromStraight(
                0.1,
                0.2,
                0.4,
                1),
            default,
            PrismPremultipliedColor.FromStraight(
                0.4,
                0.2,
                0.7,
                0.5),
            PrismPremultipliedColor.FromStraight(
                0.8,
                0.8,
                0.8,
                1)
        ];
        Func<Vector2, Vector4> primary =
            uv => new Vector4(
                uv.X,
                uv.Y,
                (uv.X + uv.Y) * 0.5f,
                1);
        Func<Vector2, Vector4> auxiliary =
            uv => new Vector4(
                0,
                0,
                0,
                uv.X);

        foreach (PrismCatalogEntryDescriptor entry in
            DistortionEntries())
        {
            PrismResamplingPlan plan = CreatePlan(
                (PrismFilterId)entry.StableId,
                new DrawRect(0, 0, 3, 3));
            PrismPremultipliedColor[] first =
                PrismResamplingMath.Apply(
                    plan,
                    source,
                    3,
                    3,
                    PrismColorProfile.LinearSrgb,
                    primaryResource: primary,
                    auxiliaryResource: auxiliary);
            PrismPremultipliedColor[] repeated =
                PrismResamplingMath.Apply(
                    plan,
                    source,
                    3,
                    3,
                    PrismColorProfile.LinearSrgb,
                    primaryResource: primary,
                    auxiliaryResource: auxiliary);

            Assert.Equal(first, repeated);
            Assert.All(
                first,
                AssertFiniteAssociated);
        }
    }

    [Fact]
    public void NestedTransformsPreserveConstantPixelsAcrossWorkingProfiles()
    {
        PrismResamplingPlan firstPlan = CreatePlan(
            PrismFilterId.Transform,
            new DrawRect(0, 0, 3, 3),
            (state, entry) =>
            {
                SetVector(
                    state,
                    entry,
                    "Translate",
                    new Vector4(
                        -2,
                        1,
                        0,
                        0));
                SetNumber(
                    state,
                    entry,
                    "Rotation",
                    37);
                SetSymbol(
                    state,
                    entry,
                    "EdgeMode",
                    "Clamp");
            });
        PrismResamplingPlan secondPlan = CreatePlan(
            PrismFilterId.Transform,
            new DrawRect(0, 0, 3, 3),
            (state, entry) =>
            {
                SetVector(
                    state,
                    entry,
                    "Scale",
                    new Vector4(
                        1.5f,
                        0.75f,
                        0,
                        0));
                SetSymbol(
                    state,
                    entry,
                    "EdgeMode",
                    "Clamp");
            });
        PrismPremultipliedColor input =
            PrismPremultipliedColor.FromStraight(
                0.31,
                0.57,
                0.83,
                0.73);

        foreach (PrismColorProfile profile in
            Enum.GetValues<PrismColorProfile>())
        {
            PrismPremultipliedColor working =
                PrismColorPipeline.ConvertInputToWorking(
                    input,
                    profile);
            PrismPremultipliedColor[] source =
                Enumerable.Repeat(working, 9).ToArray();
            PrismPremultipliedColor[] first =
                PrismResamplingMath.Apply(
                    firstPlan,
                    source,
                    3,
                    3,
                    profile);
            PrismPremultipliedColor[] second =
                PrismResamplingMath.Apply(
                    secondPlan,
                    first,
                    3,
                    3,
                    profile);

            Assert.All(
                second,
                pixel => AssertColor(
                    pixel,
                    working,
                    tolerance: 0.00001));
        }
    }

    [Fact]
    public void TransformChangesOnlyVisualBoundsAndSurvivesMaskAndClipComposition()
    {
        DrawRect sourceBounds =
            new(-8, -4, 20, 10);
        PrismLayerDefinition baseLayer = new(
            new PrismNodeId(1),
            "Masked transform",
            filters:
            [
                new PrismFilterDefinition(
                    PrismFilterId.Transform)
            ],
            mask: new PrismMaskDefinition(
                new PrismResourceId(71)));
        PrismLayerDefinition clippedLayer = new(
            new PrismNodeId(2),
            "Clipped transform",
            filters:
            [
                new PrismFilterDefinition(
                    PrismFilterId.Transform)
            ],
            clipToBelow: true);
        PrismDrawScope scope = PrismTestData.Scope(
            PrismTestData.Composition(
                "Transform composition",
                clippedLayer,
                baseLayer),
            bounds: sourceBounds);
        ConfigureTransform(
            scope.Instance
                .GetLayerState(baseLayer.Id)
                .Filters[0]);
        ConfigureTransform(
            scope.Instance
                .GetLayerState(clippedLayer.Id)
                .Filters[0]);
        PrismGraph graph = BuildGraph(scope);
        PrismGraphExecutionPlan plan =
            new PrismGraphOptimizer().Optimize(graph);

        Assert.All(
            graph.Scopes,
            graphScope =>
            {
                Assert.Equal(
                    sourceBounds,
                    graphScope.ControlBounds);
                Assert.Equal(
                    sourceBounds,
                    graphScope.Bounds);
            });
        Assert.Contains(
            graph.Nodes,
            node => node.Kind ==
                PrismGraphNodeKind.Mask);
        Assert.Contains(
            graph.Nodes,
            node => node.Kind ==
                PrismGraphNodeKind.ClipToBelow);
        PrismGraphNode[] filters = graph.Nodes
            .Where(node =>
                node.Kind ==
                    PrismGraphNodeKind.Filter)
            .ToArray();
        Assert.Equal(2, filters.Length);
        Assert.All(
            filters,
            node =>
            {
                Assert.IsType<PrismResamplingPlan>(
                    node.ResamplingPlan);
                int pixelInputs = graph.Edges.Count(edge =>
                    edge.Target == node.Id &&
                    edge.Kind is
                        PrismGraphEdgeKind.Content or
                        PrismGraphEdgeKind.Backdrop);
                Assert.Equal(1, pixelInputs);
                Assert.NotEqual(
                    sourceBounds,
                    plan.GetNodePlan(node.Id).Bounds);
            });

        void ConfigureTransform(
            PrismFilterState state)
        {
            PrismCatalogEntryDescriptor entry =
                PrismCatalogRuntime.GetEntry(
                    (int)PrismFilterId.Transform);
            SetVector(
                state,
                entry,
                "Translate",
                new Vector4(
                    3,
                    -2,
                    0,
                    0));
            SetVector(
                state,
                entry,
                "Scale",
                new Vector4(
                    1.25f,
                    0.75f,
                    0,
                    0));
            SetNumber(
                state,
                entry,
                "Rotation",
                15);
        }
    }

    private static PrismPremultipliedColor[] ApplyTransform(
        PrismPremultipliedColor[] source,
        float translateX,
        string edgeMode)
    {
        PrismResamplingPlan plan = CreatePlan(
            PrismFilterId.Transform,
            new DrawRect(
                0,
                0,
                source.Length,
                1),
            (state, entry) =>
            {
                SetVector(
                    state,
                    entry,
                    "Translate",
                    new Vector4(
                        translateX,
                        0,
                        0,
                        0));
                SetSymbol(
                    state,
                    entry,
                    "EdgeMode",
                    edgeMode);
            });
        return PrismResamplingMath.Apply(
            plan,
            source,
            source.Length,
            1,
            PrismColorProfile.LinearSrgb);
    }

    private static PrismResamplingPlan CreatePlan(
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
        PrismGraphNode node = graph.Nodes.First(
            candidate =>
                candidate.Kind ==
                    PrismGraphNodeKind.Filter);
        return Assert.IsType<PrismResamplingPlan>(
            node.ResamplingPlan);
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
                    $"distortion-{entry.Symbol}-{property.Name}"));
        }
    }

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

    private static void SetVector(
        PrismFilterState state,
        PrismCatalogEntryDescriptor entry,
        string name,
        Vector4 value) =>
        GeneratedMarkup.SetPrismFilterVector(
            state,
            entry.StableId,
            Property(entry, name).TypeSlot,
            value);

    private static void SetSymbol(
        PrismFilterState state,
        PrismCatalogEntryDescriptor entry,
        string name,
        string value) =>
        GeneratedMarkup.SetPrismFilterInteger(
            state,
            entry.StableId,
            Property(entry, name).TypeSlot,
            PrismCatalogRuntime.ResolveSymbol(
                name,
                value));

    private static PrismCatalogPropertyDescriptor Property(
        PrismCatalogEntryDescriptor entry,
        string name) =>
        entry.Properties.Single(property =>
            property.Name == name);

    private static PrismCatalogEntryDescriptor[]
        DistortionEntries() =>
        PrismCatalogGenerated.Entries
            .Where(entry =>
                entry.Kind == "filter" &&
                entry.Coverage.Test.StartsWith(
                    "PrismDistortionFilterTests/",
                    StringComparison.Ordinal))
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

    private static void AssertColor(
        PrismPremultipliedColor actual,
        PrismPremultipliedColor expected,
        double tolerance = 0.000001)
    {
        Assert.InRange(
            Math.Abs(actual.Red - expected.Red),
            0,
            tolerance);
        Assert.InRange(
            Math.Abs(actual.Green - expected.Green),
            0,
            tolerance);
        Assert.InRange(
            Math.Abs(actual.Blue - expected.Blue),
            0,
            tolerance);
        Assert.InRange(
            Math.Abs(actual.Alpha - expected.Alpha),
            0,
            tolerance);
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
