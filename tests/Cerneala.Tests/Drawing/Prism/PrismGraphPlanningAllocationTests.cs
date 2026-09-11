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
    public void RerecordedEquivalentScopesReuseGraphAndPlanWithoutRebuilding()
    {
        PrismDrawScope scope = PrismTestData.Scope(
            PrismTestData.Composition("RerecordedPlanning", PrismTestData.Layer(1, "Content")));
        DrawCommandList commands = new();
        PrismFrameAnalyzer analyzer = new();
        PrismGraphBuilder builder = new();
        PrismGraphOptimizer optimizer = new();
        PrismGraph? firstGraph = null;
        PrismGraphExecutionPlan? firstPlan = null;
        long allocatedBytes = 0;
        const int warmupFrames = 8;
        const int measuredFrames = 64;

        for (int frame = 0; frame < warmupFrames + measuredFrames; frame++)
        {
            commands.Clear();
            commands.Add(DrawCommand.BeginPrism(scope));
            commands.Add(DrawCommand.FillRectangle(new DrawRect(0, 0, 10, 10), Color.White));
            commands.Add(DrawCommand.EndPrism());
            // Work outside the scope may change without changing the graph's inputs.
            commands.Add(DrawCommand.FillRectangle(new DrawRect(30, 0, frame + 1, 10), Color.White));
            PrismFrameAnalysis analysis = analyzer.Analyze(commands);
            long before = GC.GetAllocatedBytesForCurrentThread();
            PrismGraph graph = builder.Build(analysis);
            PrismGraphExecutionPlan plan = optimizer.Optimize(graph);
            if (frame >= warmupFrames)
            {
                allocatedBytes += GC.GetAllocatedBytesForCurrentThread() - before;
            }
            firstGraph ??= graph;
            firstPlan ??= plan;
            if (frame == warmupFrames + measuredFrames - 1)
            {
                Assert.True(allocatedBytes <= measuredFrames * 32,
                    $"Equivalent scope planning allocated {allocatedBytes:N0} bytes across {measuredFrames} rerecorded frames.");
                Assert.Same(firstGraph, graph);
                Assert.Same(firstPlan, plan);
            }
        }
    }

    [Theory]
    [InlineData("instance")]
    [InlineData("owner")]
    [InlineData("bounds")]
    [InlineData("transform")]
    [InlineData("scale")]
    [InlineData("visual")]
    [InlineData("draw")]
    [InlineData("lower-ui")]
    [InlineData("values")]
    [InlineData("command-indices")]
    public void RerecordedChangedScopeInputsInvalidateTheGraph(string change)
    {
        PrismDrawScope scope = PrismTestData.Scope(
            PrismTestData.Composition("ChangedPlanning", PrismTestData.Layer(1, "Content")));
        DrawCommandList commands = PrismTestData.Commands(
            DrawCommand.BeginPrism(scope), DrawCommand.EndPrism());
        PrismFrameAnalyzer analyzer = new();
        PrismGraphBuilder builder = new();
        PrismGraph firstGraph = builder.Build(analyzer.Analyze(commands));
        if (change == "values")
        {
            scope.Instance.GetLayerState(new PrismNodeId(1)).Opacity = 0.4f;
        }
        PrismDrawScope changed = new(
            change == "instance" ? new PrismInstance(scope.Definition) : scope.Instance,
            change == "owner" ? new PrismCacheOwnerToken(2) : scope.CacheOwnerToken,
            change == "bounds" ? new DrawRect(1, 2, 30, 40) : scope.ControlBounds,
            change == "transform" ? System.Numerics.Matrix3x2.CreateTranslation(1, 2) : scope.EffectiveTransform,
            change == "scale" ? 2 : scope.PixelScale,
            change == "visual" ? 2 : scope.VisualContentVersion,
            scope.Resources,
            lowerUiVersion: change == "lower-ui" ? 1 : scope.LowerUiVersion,
            drawContentVersion: change == "draw" ? 1 : scope.DrawContentVersion);
        commands.Clear();
        if (change == "command-indices")
        {
            commands.Add(DrawCommand.FillRectangle(new DrawRect(30, 0, 10, 10), Color.White));
        }
        commands.Add(DrawCommand.BeginPrism(changed));
        commands.Add(DrawCommand.EndPrism());

        Assert.NotSame(firstGraph, builder.Build(analyzer.Analyze(commands)));
    }

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
