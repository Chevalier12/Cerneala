using Cerneala.Drawing;
using Cerneala.Drawing.Prism;
using Cerneala.Drawing.Prism.Blending;
using Cerneala.Drawing.Prism.Catalog;
using Cerneala.Drawing.Prism.ColorManagement;
using Cerneala.Drawing.Prism.Graph;
using Cerneala.UI.Prism.Definitions;
using Cerneala.UI.Prism.Runtime;

namespace Cerneala.Tests.Drawing.Prism;

public sealed class PrismAdvancedBlendingKnockoutTests
{
    [Fact]
    public void KnockoutStillBlendsSourceAgainstItsOriginalBackdrop()
    {
        PrismPremultipliedColor source =
            PrismPremultipliedColor.FromStraight(0.2, 0.8, 0.5, 1);
        PrismPremultipliedColor originalBackdrop =
            PrismPremultipliedColor.FromStraight(0.5, 0.25, 0.75, 1);
        PrismBlendOptions options = PrismBlendOptions.Default with
        {
            Knockout = PrismKnockout.Shallow
        };

        PrismPremultipliedColor actual = PrismBlendMath.Composite(
            PrismBlendMode.Multiply,
            source,
            originalBackdrop,
            options);

        AssertClose(0.1, actual.Red);
        AssertClose(0.2, actual.Green);
        AssertClose(0.375, actual.Blue);
        AssertClose(1, actual.Alpha);
    }

    [Fact]
    public void DualBackdropRecurrenceKeepsShapeSeparateFromSourceAlpha()
    {
        PrismPremultipliedColor originalBackdrop =
            PrismPremultipliedColor.FromStraight(0.1, 0.2, 0.3, 0.4);
        PrismPremultipliedColor currentBackdrop = new(
            0.42,
            0.09,
            0.16,
            0.7);
        PrismPremultipliedColor source =
            PrismPremultipliedColor.FromStraight(0.2, 0.9, 0.5, 0.4);

        PrismPremultipliedColor actual = PrismBlendMath.CompositeKnockout(
            PrismBlendMode.Multiply,
            source,
            currentBackdrop,
            originalBackdrop,
            sourceShape: 0.8);

        AssertClose(0.1512, actual.Red);
        AssertClose(0.2948, actual.Green);
        AssertClose(0.224, actual.Blue);
        AssertClose(0.7, actual.Alpha);
    }

    [Fact]
    public void NestedGroupRoutesShallowAndDeepToDifferentOriginalBackdrops()
    {
        PrismGroupDefinition group = new(
            new PrismNodeId(10),
            "Isolated",
            [
                PrismTestData.Layer(11, "Shallow"),
                PrismTestData.Layer(12, "Deep")
            ],
            blendMode: PrismBlendMode.Normal);
        PrismCompositionDefinition definition = PrismTestData.Composition(
            "KnockoutScopes",
            group,
            PrismTestData.BackdropLayer(20, "Backdrop"));
        PrismDrawScope scope = PrismTestData.Scope(definition);
        scope.Instance.GetLayerState(new PrismNodeId(11)).Knockout =
            PrismKnockout.Shallow;
        scope.Instance.GetLayerState(new PrismNodeId(12)).Knockout =
            PrismKnockout.Deep;
        scope.Instance.GetLayerState(new PrismNodeId(11)).Opacity = 0.5f;
        scope.Instance.GetLayerState(new PrismNodeId(12)).Opacity = 0.5f;
        DrawCommandList commands = PrismTestData.Commands(
            DrawCommand.BeginPrism(scope),
            DrawCommand.FillRectangle(
                new DrawRect(0, 0, 5, 5),
                Color.White),
            DrawCommand.EndPrism());

        PrismGraph graph = new PrismGraphBuilder().Build(
            new PrismFrameAnalyzer().Analyze(commands));
        PrismGraphNode shallow = Composite(graph, 11);
        PrismGraphNode deep = Composite(graph, 12);

        Assert.DoesNotContain(
            graph.Edges,
            edge => edge.Target == shallow.Id &&
                edge.Kind == PrismGraphEdgeKind.KnockoutBackdrop);
        PrismGraphEdge deepBackdrop = Assert.Single(
            graph.Edges.Where(
                edge => edge.Target == deep.Id &&
                    edge.Kind == PrismGraphEdgeKind.KnockoutBackdrop));
        PrismGraphNode deepBackdropSource = graph.GetNode(deepBackdrop.Source);
        Assert.Null(deepBackdropSource.DefinitionNodeId);
        Assert.Equal(
            PrismGraphNodeKind.ColorConversion,
            deepBackdropSource.Kind);
        Assert.All(
            new[] { shallow, deep },
            composite =>
            {
                PrismGraphEdge shape = Assert.Single(
                    graph.Edges.Where(
                        edge => edge.Target == composite.Id &&
                            edge.Kind == PrismGraphEdgeKind.KnockoutShape));
                Assert.NotEqual(
                    PrismGraphNodeKind.Opacity,
                    graph.GetNode(shape.Source).Kind);
            });
    }

    private static PrismGraphNode Composite(PrismGraph graph, int id)
    {
        return Assert.Single(
            graph.Nodes.Where(
                node => node.Kind == PrismGraphNodeKind.Composite &&
                    node.DefinitionNodeId == new PrismNodeId(id)));
    }

    private static void AssertClose(double expected, double actual)
    {
        Assert.InRange(actual, expected - 1e-12, expected + 1e-12);
    }
}
