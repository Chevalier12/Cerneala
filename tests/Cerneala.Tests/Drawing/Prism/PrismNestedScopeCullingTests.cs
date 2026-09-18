using System.Numerics;
using Cerneala.Drawing;
using Cerneala.Drawing.Prism;
using Cerneala.Drawing.Prism.Graph;
using Cerneala.UI.Prism.Definitions;

namespace Cerneala.Tests.Drawing.Prism;

public sealed class PrismNestedScopeCullingTests
{
    private static readonly DrawRect Viewport = new(0, 0, 64, 64);
    private static readonly DrawRect Content = new(0, 0, 16, 16);

    [Theory]
    [InlineData(-22, 8)]
    [InlineData(8, -22)]
    public void ClippedParentSuppressesExplicitInputChildBeforeLifetimePlanning(float x, float y)
    {
        PrismDrawScope parent = Scope(1);
        PrismDrawScope child = Scope(2) with { InputBounds = Content };
        DrawCommandList commands = NestedCommands(parent, child, x, y);
        PrismFrameAnalysis analysis = new PrismFrameAnalyzer().Analyze(commands);
        Assert.True(IsEmpty(analysis.Scopes[0].Bounds));

        PrismGraph graph = new PrismGraphBuilder().Build(analysis);
        PrismGraphExecutionPlan plan = new PrismGraphOptimizer().Optimize(graph);

        Assert.Equal(2, graph.Scopes.Length);
        Assert.All(graph.Scopes, scope => Assert.Null(scope.Output));
        Assert.Empty(graph.Nodes);
        Assert.Empty(plan.SurfaceLifetimes);
        Assert.All(analysis.Scopes, scope => Assert.True(IsEmpty(scope.Bounds)));
        Assert.Equal(PrismGraphCapabilities.None, analysis.RequiredCapabilities);
        Assert.Equal(0, analysis.RequiredSurfaceCount);
    }

    [Fact]
    public void EmptyExplicitAncestorSuppressesDeepBackdropWorkButNotItsVisibleSibling()
    {
        PrismDrawScope root = Scope(1);
        PrismDrawScope empty = Scope(2) with { InputBounds = new DrawRect(0, 0, 0, 16) };
        PrismDrawScope child = PrismTestData.Scope(
            PrismTestData.Composition("Backdrop child", PrismTestData.BackdropLayer(1, "Content")),
            ownerToken: 3, bounds: Content) with { InputBounds = Content };
        PrismDrawScope grandchild = Scope(4) with { InputBounds = Content };
        PrismDrawScope sibling = Scope(5);
        DrawCommandList commands = PrismTestData.Commands(
            DrawCommand.BeginPrism(root),
            DrawCommand.BeginPrism(empty),
            DrawCommand.BeginPrism(child),
            DrawCommand.BeginPrism(grandchild),
            DrawCommand.FillRectangle(Content, Color.White),
            DrawCommand.EndPrism(), DrawCommand.EndPrism(), DrawCommand.EndPrism(),
            DrawCommand.BeginPrism(sibling),
            DrawCommand.FillRectangle(Content, Color.White),
            DrawCommand.EndPrism(), DrawCommand.EndPrism());
        PrismFrameAnalyzer analyzer = new();
        PrismFrameAnalysis analysis = analyzer.Analyze(commands);

        Assert.False(analysis.RequiresBackdrop);
        Assert.Null(analysis.BackdropRequirement);
        for (int index = 1; index <= 3; index++)
        {
            Assert.True(IsEmpty(analysis.Scopes[index].Bounds));
            Assert.Equal(PrismGraphCapabilities.None, analysis.Scopes[index].RequiredCapabilities);
            Assert.Equal(0, analysis.Scopes[index].RequiredSurfaceCount);
            Assert.Equal(index - 1, analysis.Scopes[index].ParentScopeIndex);
        }
        Assert.Equal(0, analysis.Scopes[4].ParentScopeIndex);
        Assert.Equal(Content, analysis.Scopes[4].Bounds);
        Assert.Equal(analysis.Scopes[0].RequiredSurfaceCount + analysis.Scopes[4].RequiredSurfaceCount,
            analysis.RequiredSurfaceCount);

        PrismGraph graph = new PrismGraphBuilder().Build(analysis);
        _ = new PrismGraphOptimizer().Optimize(graph);
        Assert.Equal(2, graph.ControlCaptureCount);
        Assert.NotNull(graph.Scopes[0].Output);
        Assert.NotNull(graph.Scopes[4].Output);
        Assert.DoesNotContain(graph.Nodes, node => node.AnalysisScopeIndex is >= 1 and <= 3);

        // Culling work must not erase dependency tracking for a later re-entry.
        child.Instance.GetLayerState(new PrismNodeId(1)).Opacity = 0.5f;
        Assert.False(analysis.IsCurrent(commands));
        PrismFrameAnalysis changed = analyzer.Analyze(commands);
        Assert.NotEqual(analysis.Scopes[0].DependencyStamp, changed.Scopes[0].DependencyStamp);
        Assert.NotEqual(analysis.Scopes[1].DependencyStamp, changed.Scopes[1].DependencyStamp);
        Assert.False(changed.RequiresBackdrop);
    }

    [Fact]
    public void NonemptyExplicitParentKeepsRequiredOffscreenChildInput()
    {
        DrawRect offscreen = new(-16, 0, 16, 16);
        PrismDrawScope parent = Scope(1, new(-16, 0, 32, 16)) with { InputBounds = new DrawRect(-16, 0, 32, 16) };
        PrismDrawScope child = PrismTestData.Scope(
            PrismTestData.Composition("Offscreen source", PrismTestData.Layer(1, "Content")),
            ownerToken: 2, bounds: offscreen) with { InputBounds = offscreen };
        PrismFrameAnalysis analysis = new PrismFrameAnalyzer().Analyze(NestedCommands(parent, child, 0, 0));
        PrismGraph graph = new PrismGraphBuilder().Build(analysis);
        _ = new PrismGraphOptimizer().Optimize(graph);

        Assert.Equal(offscreen, analysis.Scopes[1].Bounds);
        Assert.Equal(2, graph.ControlCaptureCount);
        Assert.All(graph.Scopes, scope => Assert.NotNull(scope.Output));
    }

    [Fact]
    public void RetainedPlanningRestoresNestedOutputAcrossRepeatedCameraReentry()
    {
        PrismDrawScope parent = Scope(1);
        PrismDrawScope child = Scope(2) with { InputBounds = Content };
        PrismFrameAnalyzer analyzer = new();
        PrismGraphBuilder builder = new();
        PrismGraphOptimizer optimizer = new();

        for (int cycle = 0; cycle < 16; cycle++)
        {
            foreach (float x in new float[] { 8, -22, 8 })
            {
                PrismFrameAnalysis analysis = analyzer.Analyze(NestedCommands(parent, child, x, 8));
                PrismGraph graph = builder.Build(analysis);
                _ = optimizer.Optimize(graph);
                bool visible = x == 8;
                Assert.Equal(visible ? 2 : 0, graph.ControlCaptureCount);
                Assert.All(graph.Scopes, scope => Assert.Equal(visible, scope.Output.HasValue));
                Assert.Equal(visible, !IsEmpty(analysis.Scopes[1].Bounds));
                Assert.Equal(child.CacheOwnerToken, analysis.Scopes[1].DependencyStamp.CacheOwnerToken);
            }
        }
    }

    private static PrismDrawScope Scope(long owner, DrawRect? bounds = null) => PrismTestData.Scope(
        PrismTestData.Composition("Nested culling", PrismTestData.Layer(1, "Content")),
        ownerToken: owner, bounds: bounds ?? Content);

    private static DrawCommandList NestedCommands(PrismDrawScope parent, PrismDrawScope child, float x, float y) =>
        PrismTestData.Commands(
            DrawCommand.PushClip(Viewport),
            DrawCommand.PushTransform(Matrix3x2.CreateTranslation(x, y)),
            DrawCommand.BeginPrism(parent), DrawCommand.BeginPrism(child),
            DrawCommand.FillRectangle(Content, Color.White),
            DrawCommand.EndPrism(), DrawCommand.EndPrism(),
            DrawCommand.PopTransform(), DrawCommand.PopClip());

    private static bool IsEmpty(DrawRect bounds) => bounds.Width <= 0 || bounds.Height <= 0;
}
