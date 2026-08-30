using Cerneala.Drawing;
using Cerneala.Drawing.Prism;
using Cerneala.Drawing.Prism.Catalog;
using Cerneala.Drawing.Prism.Graph;
using Cerneala.UI.Prism.Definitions;
using Cerneala.UI.Prism.Runtime;

namespace Cerneala.Tests.Drawing.Prism;

public sealed class PrismGraphPlanningAllocationTests
{
    [Fact]
    public void RepeatedCurrentFramePlanningAllocatesAtMost32BytesPerFrame()
    {
        PrismDrawScope scope = PrismTestData.Scope(
            PrismTestData.Composition(
                "RetainedPlanning",
                PrismTestData.Layer(1, "Content")));
        DrawCommandList commands = PrismTestData.Commands(
            DrawCommand.BeginPrism(scope),
            DrawCommand.EndPrism());
        PrismFrameAnalyzer analyzer = new();
        PrismGraphBuilder builder = new();
        PrismGraphOptimizer optimizer = new();

        PrismFrameAnalysis firstAnalysis = analyzer.Analyze(commands);
        PrismGraph firstGraph = builder.Build(firstAnalysis);
        PrismGraphExecutionPlan firstPlan = optimizer.Optimize(firstGraph);

        const int measuredFrames = 64;
        PrismFrameAnalysis lastAnalysis = firstAnalysis;
        PrismGraph lastGraph = firstGraph;
        PrismGraphExecutionPlan lastPlan = firstPlan;
        long before = GC.GetAllocatedBytesForCurrentThread();
        for (int frame = 0; frame < measuredFrames; frame++)
        {
            lastAnalysis = analyzer.Analyze(commands);
            lastGraph = builder.Build(lastAnalysis);
            lastPlan = optimizer.Optimize(lastGraph);
        }
        long allocatedBytes = GC.GetAllocatedBytesForCurrentThread() - before;

        Assert.True(
            allocatedBytes <= measuredFrames * 32,
            $"Repeated planning allocated {allocatedBytes:N0} bytes across {measuredFrames} unchanged frames.");
        Assert.Same(firstAnalysis, lastAnalysis);
        Assert.Same(firstGraph, lastGraph);
        Assert.Same(firstPlan, lastPlan);
    }

    [Fact]
    public void ValueChangeInvalidatesEveryPlanningSnapshot()
    {
        PrismDrawScope scope = PrismTestData.Scope(
            PrismTestData.Composition(
                "DynamicPlanning",
                PrismTestData.Layer(1, "Content")));
        DrawCommandList commands = PrismTestData.Commands(
            DrawCommand.BeginPrism(scope),
            DrawCommand.EndPrism());
        PrismFrameAnalyzer analyzer = new();
        PrismGraphBuilder builder = new();
        PrismGraphOptimizer optimizer = new();

        PrismFrameAnalysis firstAnalysis = analyzer.Analyze(commands);
        PrismGraph firstGraph = builder.Build(firstAnalysis);
        PrismGraphExecutionPlan firstPlan = optimizer.Optimize(firstGraph);

        scope.Instance.GetLayerState(new PrismNodeId(1)).Opacity = 0.4f;

        PrismFrameAnalysis secondAnalysis = analyzer.Analyze(commands);
        PrismGraph secondGraph = builder.Build(secondAnalysis);
        PrismGraphExecutionPlan secondPlan = optimizer.Optimize(secondGraph);
        PrismGraphNode secondOpacity = Assert.Single(
            secondGraph.Nodes.Where(
                node => node.Kind == PrismGraphNodeKind.Opacity &&
                    node.DefinitionNodeId == new PrismNodeId(1)));

        Assert.NotSame(firstAnalysis, secondAnalysis);
        Assert.NotSame(firstGraph, secondGraph);
        Assert.NotSame(firstPlan, secondPlan);
        Assert.Equal(0.4f, secondOpacity.Amount);
    }

    [Fact]
    public void AnimatedCompositePlanningStaysBelowThePerFrameAllocationBudget()
    {
        PrismLayerDefinition layer = new(
            new PrismNodeId(1),
            "AnimatedComposite",
            filters: [new PrismFilterDefinition(PrismFilterId.MotionBlur)],
            styles:
            [
                new PrismStyleDefinition(PrismStyleId.BevelEmboss),
                new PrismStyleDefinition(PrismStyleId.OuterGlow)
            ]);
        PrismInstance instance = new(
            new PrismCompositionDefinition("AnimatedComposite", [layer]));
        PrismDrawScope scope = new(
            instance,
            new PrismCacheOwnerToken(1),
            new DrawRect(0, 0, 96, 96),
            System.Numerics.Matrix3x2.Identity,
            pixelScale: 1,
            visualContentVersion: 1);
        DrawCommandList commands = PrismTestData.Commands(
            DrawCommand.BeginPrism(scope),
            DrawCommand.EndPrism());
        PrismFilterState motionBlur = instance.GetLayerState(layer.Id).Filters[0];
        PrismCatalogParameterInfo distance = PrismCatalog
            .GetFilter(PrismFilterId.MotionBlur)
            .Parameters
            .Single(parameter => parameter.Name == "Distance");
        PrismFrameAnalyzer analyzer = new();
        PrismGraphBuilder builder = new();
        PrismGraphOptimizer optimizer = new();

        motionBlur.SetValue(distance, 4f);
        _ = optimizer.Optimize(builder.Build(analyzer.Analyze(commands)));

        const int measuredFrames = 32;
        long allocatedBytes = 0;
        PrismGraphExecutionPlan? lastPlan = null;
        for (int frame = 0; frame < measuredFrames; frame++)
        {
            motionBlur.SetValue(distance, 5f + (frame % 8));
            PrismGraph graph = builder.Build(analyzer.Analyze(commands));
            long before = GC.GetAllocatedBytesForCurrentThread();
            lastPlan = optimizer.Optimize(graph);
            allocatedBytes += GC.GetAllocatedBytesForCurrentThread() - before;
        }

        GC.KeepAlive(lastPlan);
        Assert.True(
            allocatedBytes <= measuredFrames * 120_000,
            $"Animated composite optimization allocated {allocatedBytes / measuredFrames:N0} bytes per frame.");
    }
}
