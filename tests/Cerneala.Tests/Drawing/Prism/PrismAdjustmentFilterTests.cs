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

public sealed class PrismAdjustmentFilterTests
{
    [Fact]
    public void CatalogDrivesEveryAdjustmentPlannerKernelAndTestBinding()
    {
        PrismCatalogEntryDescriptor[] entries =
            AdjustmentEntries();
        PrismFilterId[] filters = entries
            .Select(entry => (PrismFilterId)entry.StableId)
            .ToArray();
        PrismLayerDefinition layer = new(
            new PrismNodeId(1),
            "All adjustments",
            filters: filters.Select(
                filter => new PrismFilterDefinition(filter)));
        PrismDrawScope drawScope = PrismTestData.Scope(
            PrismTestData.Composition(
                "Adjustment defaults",
                layer));
        PrismGraph graph = BuildGraph(drawScope);
        PrismGraphScope graphScope = Assert.Single(graph.Scopes);
        PrismGraphNode[] nodes = graph.Nodes
            .Where(node =>
                node.Kind == PrismGraphNodeKind.Filter)
            .ToArray();

        Assert.Equal(entries.Length, nodes.Length);
        Assert.Equal(
            entries.Length,
            nodes
                .Select(node =>
                    PrismAdjustmentPlanner
                        .Create(node, graphScope)
                        .Operation)
                .Distinct()
                .Count());
        foreach (PrismCatalogEntryDescriptor entry in entries)
        {
            PrismFilterId filter =
                (PrismFilterId)entry.StableId;
            PrismGraphNode node = nodes.Single(
                candidate => candidate.Filter == filter);
            Assert.True(
                PrismAdjustmentPlanner.IsSupported(filter));
            Assert.Equal(
                $"PrismKernelRegistry/{entry.Symbol}",
                entry.Coverage.Kernel);
            Assert.Equal(
                $"PrismAdjustmentFilterTests/{entry.Symbol}",
                entry.Coverage.Test);
            Assert.StartsWith(
                "generated:",
                entry.Coverage.Documentation,
                StringComparison.Ordinal);
            Assert.Equal(
                entry.Properties.Length,
                node.Parameters.Length);
            Assert.Equal(
                Enumerable.Range(0, entry.Properties.Length),
                node.Parameters.Select(
                    parameter => parameter.Index));
        }
    }

    [Fact]
    public void RuntimeDomainValidationUsesGeneratedCatalogRanges()
    {
        (PrismFilterState brightness, PrismCatalogEntryDescriptor
            brightnessEntry) = CreateState(
                PrismFilterId.BrightnessContrast);
        PrismCatalogPropertyDescriptor brightnessProperty =
            Property(brightnessEntry, "Brightness");

        Assert.Throws<ArgumentOutOfRangeException>(
            () => GeneratedMarkup.SetPrismFilterNumber(
                brightness,
                brightnessEntry.StableId,
                brightnessProperty.TypeSlot,
                float.NaN));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => GeneratedMarkup.SetPrismFilterNumber(
                brightness,
                brightnessEntry.StableId,
                brightnessProperty.TypeSlot,
                1.0001f));
        GeneratedMarkup.SetPrismFilterNumber(
            brightness,
            brightnessEntry.StableId,
            brightnessProperty.TypeSlot,
            -1);
        GeneratedMarkup.SetPrismFilterNumber(
            brightness,
            brightnessEntry.StableId,
            brightnessProperty.TypeSlot,
            1);

        (PrismFilterState posterize, PrismCatalogEntryDescriptor
            posterizeEntry) = CreateState(
                PrismFilterId.Posterize);
        PrismCatalogPropertyDescriptor levels =
            Property(posterizeEntry, "Levels");
        Assert.Throws<ArgumentOutOfRangeException>(
            () => GeneratedMarkup.SetPrismFilterNumber(
                posterize,
                posterizeEntry.StableId,
                levels.TypeSlot,
                1));
        GeneratedMarkup.SetPrismFilterNumber(
            posterize,
            posterizeEntry.StableId,
            levels.TypeSlot,
            2);

        (PrismFilterState lookup, PrismCatalogEntryDescriptor
            lookupEntry) = CreateState(
                PrismFilterId.ColorLookup);
        PrismCatalogPropertyDescriptor lookupResource =
            Property(lookupEntry, "Lookup");
        Assert.Throws<ArgumentOutOfRangeException>(
            () => GeneratedMarkup.SetPrismFilterResource(
                lookup,
                lookupEntry.StableId,
                lookupResource.TypeSlot,
                default));

        (PrismFilterState balance, PrismCatalogEntryDescriptor
            balanceEntry) = CreateState(
                PrismFilterId.ColorBalance);
        PrismCatalogPropertyDescriptor shadows =
            Property(balanceEntry, "Shadows");
        Assert.Throws<ArgumentOutOfRangeException>(
            () => GeneratedMarkup.SetPrismFilterVector(
                balance,
                balanceEntry.StableId,
                shadows.TypeSlot,
                new Vector4(float.PositiveInfinity)));
    }

    [Fact]
    public void AnalyticVectorsPreserveAlphaAndHandleTransparentPixels()
    {
        PrismAdjustmentPlan invert =
            CreatePlan(PrismFilterId.Invert);
        PrismPremultipliedColor source =
            PrismPremultipliedColor.FromStraight(
                0.2,
                0.4,
                0.8,
                0.5);
        PrismPremultipliedColor result =
            PrismAdjustmentMath.Apply(
                invert,
                source,
                PrismColorProfile.LinearSrgb);

        AssertColor(
            result,
            red: 0.4,
            green: 0.3,
            blue: 0.1,
            alpha: 0.5);
        Assert.Equal(
            default,
            PrismAdjustmentMath.Apply(
                invert,
                default,
                PrismColorProfile.LinearSrgb));

        foreach (PrismCatalogEntryDescriptor entry in
            AdjustmentEntries())
        {
            PrismAdjustmentPlan plan =
                CreatePlan(
                    (PrismFilterId)entry.StableId);
            PrismPremultipliedColor adjusted =
                PrismAdjustmentMath.Apply(
                    plan,
                    source,
                    PrismColorProfile.LinearSrgb,
                    lookup: color => color);
            Assert.Equal(
                source.Alpha,
                adjusted.Alpha,
                precision: 8);
            AssertFiniteAssociated(adjusted);
        }
    }

    [Fact]
    public void ThresholdPosterizeAndLevelsHaveAnalyticBoundariesAndChannels()
    {
        PrismAdjustmentPlan threshold =
            CreatePlan(
                PrismFilterId.Threshold,
                (state, entry) => SetNumber(
                    state,
                    entry,
                    "Level",
                    0.5f));
        AssertStraight(
            PrismAdjustmentMath.Apply(
                threshold,
                PrismPremultipliedColor.FromStraight(
                    0.49,
                    0.49,
                    0.49,
                    1),
                PrismColorProfile.LinearSrgb),
            0,
            0,
            0);
        AssertStraight(
            PrismAdjustmentMath.Apply(
                threshold,
                PrismPremultipliedColor.FromStraight(
                    0.5,
                    0.5,
                    0.5,
                    1),
                PrismColorProfile.LinearSrgb),
            1,
            1,
            1);

        PrismAdjustmentPlan posterize =
            CreatePlan(
                PrismFilterId.Posterize,
                (state, entry) => SetNumber(
                    state,
                    entry,
                    "Levels",
                    2));
        AssertStraight(
            PrismAdjustmentMath.Apply(
                posterize,
                PrismPremultipliedColor.FromStraight(
                    0.49,
                    0.5,
                    0.51,
                    1),
                PrismColorProfile.LinearSrgb),
            0,
            0,
            1);

        PrismAdjustmentPlan levels =
            CreatePlan(
                PrismFilterId.Levels,
                (state, entry) =>
                {
                    SetSymbol(
                        state,
                        entry,
                        "Channel",
                        "Red");
                    SetNumber(
                        state,
                        entry,
                        "InputBlack",
                        0.25f);
                });
        AssertStraight(
            PrismAdjustmentMath.Apply(
                levels,
                PrismPremultipliedColor.FromStraight(
                    0.25,
                    0.4,
                    0.8,
                    1),
                PrismColorProfile.LinearSrgb),
            0,
            0.4,
            0.8);
    }

    [Fact]
    public void ChannelMixerAndLookupUseSharedMatrixAndLutPrimitives()
    {
        PrismAdjustmentPlan mixer =
            CreatePlan(
                PrismFilterId.ChannelMixer,
                (state, entry) =>
                {
                    SetVector(
                        state,
                        entry,
                        "Red",
                        new Vector4(0, 1, 0, 0));
                    SetVector(
                        state,
                        entry,
                        "Green",
                        new Vector4(0, 0, 1, 0));
                    SetVector(
                        state,
                        entry,
                        "Blue",
                        new Vector4(1, 0, 0, 0));
                });
        AssertStraight(
            PrismAdjustmentMath.Apply(
                mixer,
                PrismPremultipliedColor.FromStraight(
                    0.2,
                    0.4,
                    0.8,
                    1),
                PrismColorProfile.LinearSrgb),
            0.4,
            0.8,
            0.2);

        PrismAdjustmentPlan lookup =
            CreatePlan(
                PrismFilterId.ColorLookup,
                (state, entry) =>
                    GeneratedMarkup.SetPrismFilterResource(
                        state,
                        entry.StableId,
                        Property(entry, "Lookup").TypeSlot,
                        new PrismResourceId("analytic-lut")));
        AssertStraight(
            PrismAdjustmentMath.Apply(
                lookup,
                PrismPremultipliedColor.FromStraight(
                    0.2,
                    0.4,
                    0.8,
                    1),
                PrismColorProfile.LinearSrgb,
                lookup: color => Vector3.One - color),
            0.8,
            0.6,
            0.2);
    }

    [Fact]
    public void NeutralAdjustmentIsStableInEverySelectableColorProfile()
    {
        PrismAdjustmentPlan neutral =
            CreatePlan(
                PrismFilterId.BrightnessContrast);
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
            PrismPremultipliedColor result =
                PrismAdjustmentMath.Apply(
                    neutral,
                    working,
                    profile);
            AssertColor(
                result,
                working.Red,
                working.Green,
                working.Blue,
                working.Alpha,
                tolerance: 0.00001);
        }
    }

    [Fact]
    public void OptimizerKeepsExactSourceBoundsForEveryAdjustment()
    {
        foreach (PrismCatalogEntryDescriptor entry in
            AdjustmentEntries())
        {
            PrismFilterId filter =
                (PrismFilterId)entry.StableId;
            PrismLayerDefinition layer = new(
                new PrismNodeId(1),
                filter.ToString(),
                filters:
                [
                    new PrismFilterDefinition(filter)
                ]);
            DrawRect bounds =
                new(10, 20, 40, 30);
            PrismDrawScope scope = PrismTestData.Scope(
                PrismTestData.Composition(
                    $"Bounds {filter}",
                    layer),
                bounds: bounds);
            Assert.Single(
                scope.Instance.GetLayerState(layer.Id).Filters)
                .Opacity = 0.5f;
            PrismGraph graph = BuildGraph(scope);
            PrismGraphExecutionPlan plan =
                new PrismGraphOptimizer().Optimize(graph);
            PrismGraphNode node = Assert.Single(
                plan.OptimizedGraph.Nodes.Where(candidate =>
                    candidate.Kind ==
                        PrismGraphNodeKind.Filter));
            PrismGraphNodePlan nodePlan =
                plan.GetNodePlan(node.Id);

            Assert.Equal(bounds, nodePlan.Bounds);
            Assert.Equal(
                PrismGraphBoundsStatus.Exact,
                nodePlan.BoundsStatus);
        }
    }

    private static PrismAdjustmentPlan CreatePlan(
        PrismFilterId filter,
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
                layer));
        PrismFilterState state = Assert.Single(
            scope.Instance.GetLayerState(layer.Id).Filters);
        PrismCatalogEntryDescriptor entry =
            PrismCatalogRuntime.GetEntry((int)filter);
        configure?.Invoke(state, entry);

        PrismGraph graph = BuildGraph(scope);
        PrismGraphNode node = Assert.Single(
            graph.Nodes.Where(candidate =>
                candidate.Kind ==
                    PrismGraphNodeKind.Filter));
        return PrismAdjustmentPlanner.Create(
            node,
            Assert.Single(graph.Scopes));
    }

    private static (
        PrismFilterState State,
        PrismCatalogEntryDescriptor Entry) CreateState(
        PrismFilterId filter)
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
                $"State {filter}",
                layer));
        return (
            Assert.Single(
                scope.Instance.GetLayerState(layer.Id).Filters),
            PrismCatalogRuntime.GetEntry((int)filter));
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

    private static PrismCatalogPropertyDescriptor Property(
        PrismCatalogEntryDescriptor entry,
        string name) =>
        entry.Properties.Single(property =>
            property.Name == name);

    private static PrismCatalogEntryDescriptor[]
        AdjustmentEntries() =>
        PrismCatalogGenerated.Entries
            .Where(entry =>
                entry.Kind == "filter" &&
                entry.Category ==
                    "color-and-adjustment" &&
                entry.Execution is
                    PrismCatalogExecutionDescriptor execution &&
                execution.Primitive ==
                    "matrix-curve-lut")
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

    private static void AssertStraight(
        PrismPremultipliedColor color,
        double red,
        double green,
        double blue,
        double tolerance = 0.00001) =>
        AssertColor(
            color,
            red,
            green,
            blue,
            1,
            tolerance);

    private static void AssertColor(
        PrismPremultipliedColor color,
        double red,
        double green,
        double blue,
        double alpha,
        double tolerance = 0.00001)
    {
        Assert.InRange(
            Math.Abs(color.Red - red),
            0,
            tolerance);
        Assert.InRange(
            Math.Abs(color.Green - green),
            0,
            tolerance);
        Assert.InRange(
            Math.Abs(color.Blue - blue),
            0,
            tolerance);
        Assert.InRange(
            Math.Abs(color.Alpha - alpha),
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
