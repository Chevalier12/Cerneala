using Cerneala.Drawing;
using Cerneala.Drawing.Prism;
using Cerneala.Drawing.Prism.Catalog;
using Cerneala.Drawing.Prism.Filters;
using Cerneala.Drawing.Prism.Graph;
using Cerneala.UI.Prism.Definitions;

namespace Cerneala.Tests.Drawing.Prism;

public sealed class PrismRasterPlannerTests
{
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
