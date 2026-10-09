using Cerneala.Drawing;
using Cerneala.Drawing.Prism;
using Cerneala.Drawing.Prism.Catalog;
using Cerneala.Drawing.Prism.Filters;
using Cerneala.Drawing.Prism.Graph;
using Cerneala.UI.Markup;
using Cerneala.UI.Prism.Definitions;
using Cerneala.UI.Prism.Runtime;

namespace Cerneala.Tests.Drawing.Prism;

public sealed class PrismRasterPlannerTests
{
    [Theory]
    [InlineData(33, 2, false)]
    [InlineData(64, 2, false)]
    [InlineData(200, 7, false)]
    [InlineData(33, 2, true)]
    [InlineData(64, 2, true)]
    [InlineData(200, 7, true)]
    public void LargeShadowRadiiRespectKernelCapabilityWithoutChangingBounds(int radius, int factor, bool spread)
    {
        PrismGraphExecutionPlan semantic = ShadowPlan(spread ? 0 : (radius - 0.25f) / 1.5f,
            spread ? radius : 0);
        PrismRasterExecutionPlan raster = new PrismRasterPlanner().Prepare(semantic, 513, 257);
        PrismRasterPass[] passes = OrderedPasses(raster).ToArray();
        PrismRasterPass[] down = passes.Where(pass => pass.Kind == PrismRasterPassKind.MaskDownsample).ToArray();
        Assert.Equal(2, down.Length);
        Assert.All(down, pass => Assert.Equal(factor, pass.RadiusOrJump));
        Assert.True(down[0].Horizontal);
        Assert.False(down[1].Horizontal);
        int reducedWidth = (513 + factor - 1) / factor;
        int reducedHeight = (257 + factor - 1) / factor;
        Assert.Equal(new(reducedWidth, 257, PrismRasterSurfaceFormat.Rgba16Float), down[0].Surface);
        Assert.Equal(new(reducedWidth, reducedHeight, PrismRasterSurfaceFormat.Rgba16Float), down[1].Surface);
        PrismRasterPassKind kind = spread ? PrismRasterPassKind.ShadowSpread : PrismRasterPassKind.ShadowBlur;
        PrismRasterPass[] kernels = passes.Where(pass => pass.Kind == kind).ToArray();
        Assert.Equal(2, kernels.Length);
        Assert.All(kernels, pass =>
        {
            Assert.Equal(radius / (float)factor, pass.RadiusOrJump);
            Assert.InRange(pass.RadiusOrJump, 1, 32);
            Assert.Equal(down[1].Surface, pass.Surface);
        });
        PrismRasterPass up = Assert.Single(passes.Where(pass => pass.Kind == PrismRasterPassKind.MaskUpsample));
        Assert.Equal(factor, up.RadiusOrJump);
        Assert.Equal(new(513, 257, PrismRasterSurfaceFormat.Rgba16Float), up.Surface);
        foreach (PrismGraphNodePlan node in semantic.NodePlans)
            Assert.Equal(node, raster.GraphPlan.GetNodePlan(node.NodeId));
        PrismGraphNode style = Assert.Single(semantic.OptimizedGraph.Nodes.Where(node => node.Style == PrismStyleId.DropShadow));
        int support = radius + (spread ? 1 : 0);
        Assert.Equal(new DrawRect(-support, -support, 48 + support * 2, 32 + support * 2),
            semantic.GetNodePlan(style.Id).Bounds);
        AssertCompleteDependencies(raster);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(16)]
    [InlineData(32)]
    public void SmallShadowRadiiKeepTheOriginalPasses(int radius)
    {
        PrismRasterExecutionPlan raster = new PrismRasterPlanner().Prepare(
            ShadowPlan((radius - 0.25f) / 1.5f, radius), 513, 257);
        PrismRasterPass[] passes = OrderedPasses(raster).ToArray();
        Assert.Equal(new[] { PrismRasterPassKind.ShadowSpread, PrismRasterPassKind.ShadowSpread,
            PrismRasterPassKind.ShadowBlur, PrismRasterPassKind.ShadowBlur }, passes.Select(pass => pass.Kind));
        Assert.All(passes, pass =>
        {
            Assert.Equal(radius, pass.RadiusOrJump);
            Assert.Equal(new(513, 257, PrismRasterSurfaceFormat.Rgba16Float), pass.Surface);
        });
        AssertCompleteDependencies(raster);
    }

    [Fact]
    public void DeviceScaleAndLocalExtentDetermineIndependentSpreadAndBlurReductions()
    {
        // 2x DPI turns these logical radii into 64px spread and 200px blur.
        PrismGraphExecutionPlan semantic = ShadowPlan((200 - 0.25f) / 3f, 32, pixelScale: 2);
        int scope = Assert.Single(semantic.OptimizedGraph.Scopes).AnalysisScopeIndex;
        PrismRasterPlanner planner = new();
        Dictionary<int, PrismRasterExtent> extents = new() { [scope] = new(96, 80, 513, 257) };
        PrismRasterExecutionPlan raster = planner.Prepare(semantic, 1024, 1024, extents);
        Assert.Equal(new float[] { 2, 2, 7, 7 }, OrderedPasses(raster)
            .Where(pass => pass.Kind == PrismRasterPassKind.MaskDownsample).Select(pass => pass.RadiusOrJump));
        Assert.Equal(2, OrderedPasses(raster).Count(pass => pass.Kind == PrismRasterPassKind.MaskUpsample));
        Assert.All(OrderedPasses(raster).Where(pass => pass.Kind is PrismRasterPassKind.ShadowSpread or PrismRasterPassKind.ShadowBlur),
            pass => Assert.InRange(pass.RadiusOrJump, 1, PrismRasterPlanner.MaximumShadowKernelRadius));
        Assert.Same(raster, planner.Prepare(semantic, 1024, 1024, extents));
        extents[scope] = new(96, 80, 1, 1);
        PrismRasterExecutionPlan tiny = planner.Prepare(semantic, 1024, 1024, extents);
        Assert.All(tiny.AuxiliaryPasses.Values, pass => Assert.Equal(new(1, 1, PrismRasterSurfaceFormat.Rgba16Float), pass.Surface));
        AssertCompleteDependencies(raster);
        AssertCompleteDependencies(tiny);
    }

    private static PrismGraphExecutionPlan ShadowPlan(float size, float spread, float pixelScale = 1)
    {
        DrawRect bounds = new(0, 0, 48, 32);
        PrismLayerDefinition layer = new(new(1), "Shadow", styles: [new(PrismStyleId.DropShadow)]);
        PrismDrawScope scope = PrismTestData.Scope(new("Shadow", [layer]), bounds: bounds, pixelScale: pixelScale);
        var state = Assert.Single(scope.Instance.GetLayerState(layer.Id).Styles);
        PrismCatalogEntryDescriptor entry = PrismCatalogRuntime.GetEntry((int)PrismStyleId.DropShadow);
        Set("Size", size);
        Set("Spread", spread);
        Set("Distance", 0);
        DrawCommandList commands = PrismTestData.Commands(DrawCommand.BeginPrism(scope),
            DrawCommand.FillRectangle(bounds, Color.White), DrawCommand.EndPrism());
        return new PrismGraphOptimizer().Optimize(new PrismGraphBuilder().Build(new PrismFrameAnalyzer().Analyze(commands)));

        void Set(string name, float value) => GeneratedMarkup.SetPrismStyleNumber(state, entry.StableId,
            entry.Properties.Single(property => property.Name == name).TypeSlot, value);
    }

    [Fact]
    public void ThresholdPlansAnalysisSurfacesAndTheirConsumingDependencies()
    {
        PrismGraphExecutionPlan semantic = SemanticPlan(new(new(1), "Threshold",
            filters: [new(PrismFilterId.Threshold)]));
        PrismRasterExecutionPlan raster = new PrismRasterPlanner().Prepare(semantic, 48, 32);
        PrismGraphExecutionPlan plan = raster.GraphPlan;
        KeyValuePair<PrismGraphNodeId, PrismRasterPass> cdf = Assert.Single(
            raster.AuxiliaryPasses.Where(pair => pair.Value.Kind == PrismRasterPassKind.ThresholdCdf));
        KeyValuePair<PrismGraphNodeId, PrismRasterPass> selection = Assert.Single(
            raster.AuxiliaryPasses.Where(pair => pair.Value.Kind == PrismRasterPassKind.ThresholdSelection));

        Assert.Equal(2, raster.AuxiliaryPasses.Count);
        Assert.Equal(new(PrismThresholdAnalysis.BinCount, 1, PrismRasterSurfaceFormat.R32Float), cdf.Value.Surface);
        Assert.Equal(new(1, 1, PrismRasterSurfaceFormat.Rgba8Unorm), selection.Value.Surface);
        Assert.Contains(new(cdf.Key, selection.Key, PrismGraphEdgeKind.Content), plan.OptimizedGraph.Edges);
        Assert.Contains(new(selection.Key, selection.Value.Owner, PrismGraphEdgeKind.PreparedInput), plan.OptimizedGraph.Edges);
        Assert.Equal(plan.GetExecutionIndex(selection.Key), plan.SurfaceLifetimes[plan.GetExecutionIndex(cdf.Key)].LastStep);
        Assert.Equal(plan.GetExecutionIndex(selection.Value.Owner), plan.SurfaceLifetimes[plan.GetExecutionIndex(selection.Key)].LastStep);
        AssertCompleteDependencies(raster);
    }

    [Theory]
    [InlineData(1, 1, new int[] { 1 })]
    [InlineData(2, 1, new int[] { 1, 1 })]
    [InlineData(32, 16, new int[] { 16, 8, 4, 2, 1, 1 })]
    [InlineData(48, 32, new int[] { 32, 16, 8, 4, 2, 1, 1 })]
    public void DistanceFieldPlansTheExtentDependentJfaPlusOneSequence(int width, int height, int[] jumps)
    {
        PrismGraphExecutionPlan semantic = SemanticPlan(new(new(1), "Glow", styles: [new(PrismStyleId.OuterGlow)]));
        PrismRasterExecutionPlan raster = new PrismRasterPlanner().Prepare(semantic, width, height);
        Assert.Single(raster.AuxiliaryPasses.Values.Where(pass => pass.Kind == PrismRasterPassKind.DistanceSeed));
        Assert.Equal(jumps, OrderedPasses(raster)
            .Where(pass => pass.Kind == PrismRasterPassKind.DistanceFlood)
            .Select(pass => (int)pass.RadiusOrJump));
        Assert.All(raster.AuxiliaryPasses.Values, pass =>
            Assert.Equal(new(width, height, PrismRasterSurfaceFormat.Rgba32Float), pass.Surface));
        AssertCompleteDependencies(raster);
    }

    [Fact]
    public void BevelAndGlowShareTheFieldButStrokeKeepsDirectionalCoverageSeparate()
    {
        PrismGraphExecutionPlan semantic = SemanticPlan(new(new(1), "Styles", styles:
            [new(PrismStyleId.BevelEmboss), new(PrismStyleId.OuterGlow), new(PrismStyleId.Stroke)]));
        PrismRasterExecutionPlan raster = new PrismRasterPlanner().Prepare(semantic, 48, 32);
        PrismRasterPass[] seeds = raster.AuxiliaryPasses.Values
            .Where(pass => pass.Kind == PrismRasterPassKind.DistanceSeed).ToArray();
        Assert.Equal(2, seeds.Length);
        Assert.Single(seeds.Where(pass => pass.DirectionalCoverage));
        Assert.Single(seeds.Where(pass => !pass.DirectionalCoverage));
        Assert.Single(raster.AuxiliaryPasses.Values.Where(pass => pass.Kind == PrismRasterPassKind.BevelHeight));
        Assert.Single(raster.AuxiliaryPasses.Values.Where(pass => pass.Kind == PrismRasterPassKind.BevelLighting));

        PrismGraph graph = raster.GraphPlan.OptimizedGraph;
        PrismGraphNode glow = Assert.Single(graph.Nodes.Where(node => node.Style == PrismStyleId.OuterGlow));
        PrismGraphNodeId glowField = Assert.Single(graph.Edges.Where(edge =>
            edge.Target == glow.Id && edge.Kind == PrismGraphEdgeKind.PreparedInput)).Source;
        PrismGraphNodeId height = Assert.Single(raster.AuxiliaryPasses.Where(pair =>
            pair.Value.Kind == PrismRasterPassKind.BevelHeight)).Key;
        Assert.Contains(new(glowField, height, PrismGraphEdgeKind.Content), graph.Edges);
        AssertCompleteDependencies(raster);
    }

    [Fact]
    public void LoweringPreservesSemanticCacheKeysAndAuxiliariesAreNeverRetainedIndependently()
    {
        PrismGraphExecutionPlan semantic = SemanticPlan(new(new(1), "Bevel", styles: [new(PrismStyleId.BevelEmboss)]));
        PrismRasterExecutionPlan raster = new PrismRasterPlanner().Prepare(semantic, 48, 32);
        foreach (PrismGraphNodePlan node in semantic.NodePlans)
        {
            Assert.Equal(node, raster.GraphPlan.GetNodePlan(node.NodeId));
        }
        foreach (PrismGraphNodeId auxiliary in raster.AuxiliaryPasses.Keys)
        {
            Assert.Equal(PrismRetainedCacheCandidateKind.None,
                raster.GraphPlan.GetNodePlan(auxiliary).CacheCandidateKind);
        }
        AssertCompleteDependencies(raster);
    }

    [Fact]
    public void UnchangedPlansAreReusedButRasterExtentChangesReplanTheFloodSequence()
    {
        PrismGraphExecutionPlan semantic = SemanticPlan(new(new(1), "Glow", styles: [new(PrismStyleId.OuterGlow)]));
        PrismRasterPlanner planner = new();
        PrismRasterExecutionPlan first = planner.Prepare(semantic, 48, 32);
        Assert.Same(first, planner.Prepare(semantic, 48, 32));
        PrismRasterExecutionPlan resized = planner.Prepare(semantic, 96, 32);
        Assert.NotSame(first, resized);
        Assert.Equal(first.AuxiliaryPasses.Count + 1, resized.AuxiliaryPasses.Count);
        AssertCompleteDependencies(resized);
    }

    [Fact]
    public void ScopeExtentsControlDistanceWorkAndMutableExtentInputsCannotStaleThePlan()
    {
        PrismGraphExecutionPlan semantic = SemanticPlan(new(new(1), "Glow", styles: [new(PrismStyleId.OuterGlow)]));
        int scope = Assert.Single(semantic.OptimizedGraph.Scopes).AnalysisScopeIndex;
        Dictionary<int, PrismRasterExtent> extents = new() { [scope] = new(96, 80, 32, 16) };
        PrismRasterPlanner planner = new();
        PrismRasterExecutionPlan first = planner.Prepare(semantic, 256, 256, extents);

        Assert.All(first.AuxiliaryPasses.Values, pass =>
            Assert.Equal(new(32, 16, PrismRasterSurfaceFormat.Rgba32Float), pass.Surface));
        Assert.Equal(new[] { 16, 8, 4, 2, 1, 1 }, OrderedPasses(first)
            .Where(pass => pass.Kind == PrismRasterPassKind.DistanceFlood)
            .Select(pass => (int)pass.RadiusOrJump));
        Assert.Same(first, planner.Prepare(semantic, 256, 256, new Dictionary<int, PrismRasterExtent>(extents)));

        extents[scope] = new(96, 80, 64, 32);
        PrismRasterExecutionPlan resized = planner.Prepare(semantic, 256, 256, extents);
        Assert.NotSame(first, resized);
        Assert.All(resized.AuxiliaryPasses.Values, pass =>
            Assert.Equal(new(64, 32, PrismRasterSurfaceFormat.Rgba32Float), pass.Surface));
        Assert.Equal(first.AuxiliaryPasses.Count + 1, resized.AuxiliaryPasses.Count);
        AssertCompleteDependencies(resized);
        Assert.NotSame(resized, planner.Prepare(semantic, 256, 256));
    }

    [Fact]
    public void ScopeExtentsRejectEmptyRasterDimensions()
    {
        PrismGraphExecutionPlan semantic = SemanticPlan(new(new(1), "Glow", styles: [new(PrismStyleId.OuterGlow)]));
        int scope = Assert.Single(semantic.OptimizedGraph.Scopes).AnalysisScopeIndex;
        Assert.Throws<ArgumentOutOfRangeException>(() => new PrismRasterPlanner().Prepare(semantic, 256, 256,
            new Dictionary<int, PrismRasterExtent> { [scope] = new(0, 0, 0, 32) }));
    }

    private static IEnumerable<PrismRasterPass> OrderedPasses(PrismRasterExecutionPlan raster) =>
        raster.GraphPlan.ExecutionOrder.Where(raster.AuxiliaryPasses.ContainsKey)
            .Select(id => raster.AuxiliaryPasses[id]);

    private static void AssertCompleteDependencies(PrismRasterExecutionPlan raster)
    {
        PrismGraphExecutionPlan plan = raster.GraphPlan;
        foreach (PrismGraphEdge edge in plan.OptimizedGraph.Edges)
        {
            int source = plan.GetExecutionIndex(edge.Source);
            int target = plan.GetExecutionIndex(edge.Target);
            Assert.True(source < target);
            Assert.Contains(source, plan.CacheInputExecutionIndices[target]);
            Assert.True(plan.SurfaceLifetimes[source].LastStep >= target);
        }
        HashSet<int> reachable = [];
        Stack<int> pending = new(plan.RootOutputExecutionIndices);
        while (pending.TryPop(out int step))
        {
            if (!reachable.Add(step)) continue;
            foreach (int input in plan.CacheInputExecutionIndices[step]) pending.Push(input);
        }
        Assert.Equal(plan.ExecutionOrder.Length, reachable.Count);
    }

    private static PrismGraphExecutionPlan SemanticPlan(PrismLayerDefinition layer)
    {
        DrawRect bounds = new(0, 0, 48, 32);
        PrismDrawScope scope = PrismTestData.Scope(new("RasterPlan", [layer]), bounds: bounds);
        DrawCommandList commands = PrismTestData.Commands(DrawCommand.BeginPrism(scope),
            DrawCommand.FillRectangle(bounds, Color.CornflowerBlue), DrawCommand.EndPrism());
        return new PrismGraphOptimizer().Optimize(
            new PrismGraphBuilder().Build(new PrismFrameAnalyzer().Analyze(commands)));
    }
}
