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

public sealed class PrismNeighborhoodFilterTests
{
    [Fact]
    public void CatalogDrivesEveryNeighborhoodPlannerKernelTestAndDocumentation()
    {
        PrismCatalogEntryDescriptor[] entries =
            NeighborhoodEntries();
        PrismFilterId[] filters = entries
            .Select(entry => (PrismFilterId)entry.StableId)
            .ToArray();
        PrismLayerDefinition layer = new(
            new PrismNodeId(1),
            "All neighborhood filters",
            filters: filters.Select(
                filter => new PrismFilterDefinition(filter)));
        PrismDrawScope scope = PrismTestData.Scope(
            PrismTestData.Composition(
                "Neighborhood defaults",
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

        Assert.Equal(27, entries.Length);
        Assert.Equal(
            entries.Length,
            nodes
                .Select(node =>
                    Assert.IsType<PrismNeighborhoodPlan>(
                        node.NeighborhoodPlan)
                        .Operation)
                .Distinct()
                .Count());
        foreach (PrismCatalogEntryDescriptor entry in entries)
        {
            PrismFilterId filter =
                (PrismFilterId)entry.StableId;
            PrismGraphNode[] filterNodes = nodes
                .Where(node => node.Filter == filter)
                .ToArray();

            Assert.NotEmpty(filterNodes);
            Assert.True(
                PrismNeighborhoodPlanner.IsSupported(filter));
            Assert.Equal(
                $"PrismKernelRegistry/{entry.Symbol}",
                entry.Coverage.Kernel);
            Assert.Equal(
                $"PrismNeighborhoodFilterTests/{entry.Symbol}",
                entry.Coverage.Test);
            Assert.StartsWith(
                "generated:",
                entry.Coverage.Documentation,
                StringComparison.Ordinal);
            Assert.All(
                filterNodes,
                node =>
                {
                    Assert.Equal(
                        entry.Properties.Length,
                        node.Parameters.Length);
                    Assert.Equal(
                        Enumerable.Range(
                            0,
                            entry.Properties.Length),
                        node.Parameters.Select(
                            parameter => parameter.Index));
                    PrismNeighborhoodPlan prepared =
                        Assert.IsType<PrismNeighborhoodPlan>(
                            node.NeighborhoodPlan);
                    Assert.Equal(filter, prepared.Filter);
                    Assert.InRange(
                        node.NeighborhoodPassIndex,
                        0,
                        prepared.Passes.Length - 1);
                });
            PrismCatalogExecutionDescriptor execution =
                Assert.IsType<PrismCatalogExecutionDescriptor>(
                    entry.Execution);
            Assert.Contains(
                execution.Primitive,
                new[]
                {
                    "convolution-neighborhood",
                    "noise-quantization-procedural"
                });
            Assert.Equal(
                "linear-premultiplied-rgba",
                execution.SurfaceFormat);
            Assert.Equal(
                "working-profile",
                execution.ColorSpace);
        }
    }

    [Fact]
    public void PlannerPreparesPassesRadiiBoundsAndQualityOnlyOnce()
    {
        PrismNeighborhoodPlan gaussian = CreatePlan(
            PrismFilterId.GaussianBlur,
            new DrawRect(0, 0, 40, 30),
            (state, entry) =>
            {
                SetNumber(state, entry, "Radius", 3);
                SetSymbol(state, entry, "Quality", "Best");
            });

        Assert.Collection(
            gaussian.Passes,
            horizontal =>
            {
                Assert.Equal(
                    PrismNeighborhoodPassKind.Horizontal,
                    horizontal.Kind);
                Assert.Equal(3, horizontal.RadiusX);
                Assert.Equal(0, horizontal.RadiusY);
                Assert.Equal(3, horizontal.BoundsRadiusX);
                Assert.Equal(0, horizontal.BoundsRadiusY);
                Assert.Equal(17, horizontal.SampleCount);
                Assert.False(horizontal.IsNoOp);
            },
            vertical =>
            {
                Assert.Equal(
                    PrismNeighborhoodPassKind.Vertical,
                    vertical.Kind);
                Assert.Equal(0, vertical.RadiusX);
                Assert.Equal(3, vertical.RadiusY);
                Assert.Equal(0, vertical.BoundsRadiusX);
                Assert.Equal(3, vertical.BoundsRadiusY);
                Assert.Equal(17, vertical.SampleCount);
                Assert.False(vertical.IsNoOp);
            });

        PrismNeighborhoodPlan tiny = CreatePlan(
            PrismFilterId.GaussianBlur,
            new DrawRect(0, 0, 1, 1));
        PrismNeighborhoodPass tinyPass =
            Assert.Single(tiny.Passes);
        Assert.True(tinyPass.IsNoOp);
        Assert.Equal(
            PrismNeighborhoodPassKind.Direct,
            tinyPass.Kind);

        Assert.Equal(
            5,
            Assert.Single(
                CreatePlan(
                    PrismFilterId.Blur,
                    configure: (state, entry) =>
                        SetSymbol(
                            state,
                            entry,
                            "Quality",
                            "Draft"))
                    .Passes)
                .SampleCount);
        Assert.Equal(
            9,
            Assert.Single(
                CreatePlan(PrismFilterId.Blur).Passes)
                .SampleCount);
    }

    [Fact]
    public void EdgeModesAndAlphaEdgesUseAssociatedLinearSamples()
    {
        PrismPremultipliedColor[] pixels =
        [
            default,
            PrismPremultipliedColor.FromStraight(
                1,
                0,
                0,
                1),
            default
        ];
        PrismNeighborhoodPlan clamp = CreatePlan(
            PrismFilterId.Blur,
            configure: (state, entry) =>
            {
                SetNumber(state, entry, "Radius", 2);
                SetSymbol(state, entry, "EdgeMode", "Clamp");
            });
        PrismNeighborhoodPlan transparent = CreatePlan(
            PrismFilterId.Blur,
            configure: (state, entry) =>
            {
                SetNumber(state, entry, "Radius", 2);
                SetSymbol(
                    state,
                    entry,
                    "EdgeMode",
                    "Transparent");
            });

        PrismPremultipliedColor[] clamped =
            PrismNeighborhoodMath.Apply(
                clamp,
                pixels,
                3,
                1,
                PrismColorProfile.LinearSrgb);
        PrismPremultipliedColor[] transparentResult =
            PrismNeighborhoodMath.Apply(
                transparent,
                pixels,
                3,
                1,
                PrismColorProfile.LinearSrgb);

        Assert.True(
            clamped[1].Alpha >
            transparentResult[1].Alpha);
        Assert.True(clamped[0].Alpha > 0);
        Assert.True(clamped[2].Alpha > 0);
        Assert.All(clamped, AssertFiniteAssociated);
        Assert.All(transparentResult, AssertFiniteAssociated);
    }

    [Fact]
    public void ZeroAndLargeRadiiHaveExactNoOpAndConservativeBounds()
    {
        DrawRect sourceBounds = new(0, 0, 3, 2);
        PrismGraph zero = CreateGraph(
            PrismFilterId.GaussianBlur,
            sourceBounds,
            (state, entry) =>
                SetNumber(state, entry, "Radius", 0));
        PrismGraphExecutionPlan zeroPlan =
            new PrismGraphOptimizer().Optimize(zero);
        Assert.DoesNotContain(
            zeroPlan.OptimizedGraph.Nodes,
            node => node.Kind == PrismGraphNodeKind.Filter);

        PrismGraph large = CreateGraph(
            PrismFilterId.GaussianBlur,
            sourceBounds,
            (state, entry) =>
                SetNumber(state, entry, "Radius", 100_000));
        PrismGraphExecutionPlan largePlan =
            new PrismGraphOptimizer().Optimize(large);
        PrismGraphNode[] passes = largePlan.OptimizedGraph.Nodes
            .Where(node =>
                node.Kind == PrismGraphNodeKind.Filter)
            .ToArray();

        Assert.Equal(2, passes.Length);
        Assert.All(
            passes,
            node => Assert.False(
                Assert.IsType<PrismNeighborhoodPlan>(
                    node.NeighborhoodPlan)
                    .Passes[node.NeighborhoodPassIndex]
                    .IsNoOp));
        PrismGraphNodePlan final =
            largePlan.GetNodePlan(passes[^1].Id);
        Assert.Equal(
            new DrawRect(
                -100_000,
                -100_000,
                200_003,
                200_002),
            final.Bounds);
        Assert.Equal(
            PrismGraphBoundsStatus.Conservative,
            final.BoundsStatus);
    }

    [Fact]
    public void AddNoiseIsDeterministicSeededAndMonochromatic()
    {
        PrismPremultipliedColor[] source =
            Enumerable.Repeat(
                PrismPremultipliedColor.FromStraight(
                    0.5,
                    0.5,
                    0.5,
                    1),
                32)
            .ToArray();
        PrismNeighborhoodPlan plan = NoisePlan(seed: 42);

        PrismPremultipliedColor[] first =
            PrismNeighborhoodMath.Apply(
                plan,
                source,
                source.Length,
                1,
                PrismColorProfile.LinearSrgb);
        PrismPremultipliedColor[] repeated =
            PrismNeighborhoodMath.Apply(
                plan,
                source,
                source.Length,
                1,
                PrismColorProfile.LinearSrgb);
        PrismPremultipliedColor[] changed =
            PrismNeighborhoodMath.Apply(
                NoisePlan(seed: 43),
                source,
                source.Length,
                1,
                PrismColorProfile.LinearSrgb);

        Assert.Equal(first, repeated);
        Assert.False(first.SequenceEqual(changed));
        Assert.All(
            first,
            pixel =>
            {
                Assert.Equal(
                    pixel.Red,
                    pixel.Green,
                    precision: 7);
                Assert.Equal(
                    pixel.Red,
                    pixel.Blue,
                    precision: 7);
                AssertFiniteAssociated(pixel);
            });

        PrismNeighborhoodPlan NoisePlan(int seed) =>
            CreatePlan(
                PrismFilterId.AddNoise,
                configure: (state, entry) =>
                {
                    SetNumber(state, entry, "Amount", 0.2f);
                    SetBoolean(
                        state,
                        entry,
                        "Monochromatic",
                        true);
                    SetInteger(state, entry, "Seed", seed);
                });
    }

    [Fact]
    public void EveryNeighborhoodFilterIsFiniteAndRepeatableOnSmallImages()
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
        Func<Vector2, Vector4> resource =
            uv => new Vector4(
                uv.X,
                uv.Y,
                (uv.X + uv.Y) * 0.5f,
                1);

        foreach (PrismCatalogEntryDescriptor entry in
            NeighborhoodEntries())
        {
            PrismNeighborhoodPlan plan = CreatePlan(
                (PrismFilterId)entry.StableId);
            PrismPremultipliedColor[] first =
                PrismNeighborhoodMath.Apply(
                    plan,
                    source,
                    3,
                    3,
                    PrismColorProfile.LinearSrgb,
                    resource: resource);
            PrismPremultipliedColor[] repeated =
                PrismNeighborhoodMath.Apply(
                    plan,
                    source,
                    3,
                    3,
                    PrismColorProfile.LinearSrgb,
                    resource: resource);

            Assert.Equal(first, repeated);
            Assert.All(first, AssertFiniteAssociated);
        }
    }

    [Fact]
    public void ConstantPixelsRemainStableAcrossNestedWorkingProfiles()
    {
        PrismNeighborhoodPlan blur =
            CreatePlan(PrismFilterId.GaussianBlur);
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
            PrismPremultipliedColor[] result =
                PrismNeighborhoodMath.Apply(
                    blur,
                    source,
                    3,
                    3,
                    profile);

            Assert.All(
                result,
                pixel => AssertColor(
                    pixel,
                    working,
                    tolerance: 0.00001));
        }
    }

    private static PrismNeighborhoodPlan CreatePlan(
        PrismFilterId filter,
        DrawRect? bounds = null,
        Action<PrismFilterState, PrismCatalogEntryDescriptor>?
            configure = null)
    {
        PrismGraph graph = CreateGraph(
            filter,
            bounds ?? new DrawRect(0, 0, 20, 10),
            configure);
        PrismGraphNode node = graph.Nodes.First(candidate =>
            candidate.Kind == PrismGraphNodeKind.Filter);
        return Assert.IsType<PrismNeighborhoodPlan>(
            node.NeighborhoodPlan);
    }

    private static PrismGraph CreateGraph(
        PrismFilterId filter,
        DrawRect bounds,
        Action<PrismFilterState, PrismCatalogEntryDescriptor>?
            configure = null)
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
            scope.Instance.GetLayerState(layer.Id).Filters);
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
                    $"neighborhood-{entry.Symbol}-{property.Name}"));
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

    private static void SetBoolean(
        PrismFilterState state,
        PrismCatalogEntryDescriptor entry,
        string name,
        bool value) =>
        GeneratedMarkup.SetPrismFilterBoolean(
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
            PrismCatalogRuntime.ResolveSymbol(name, value));

    private static PrismCatalogPropertyDescriptor Property(
        PrismCatalogEntryDescriptor entry,
        string name) =>
        entry.Properties.Single(property =>
            property.Name == name);

    private static PrismCatalogEntryDescriptor[]
        NeighborhoodEntries() =>
        PrismCatalogGenerated.Entries
            .Where(entry =>
                entry.Kind == "filter" &&
                entry.Coverage.Test.StartsWith(
                    "PrismNeighborhoodFilterTests/",
                    StringComparison.Ordinal))
            .ToArray();

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

    private static void AssertColor(
        PrismPremultipliedColor actual,
        PrismPremultipliedColor expected,
        double tolerance)
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
