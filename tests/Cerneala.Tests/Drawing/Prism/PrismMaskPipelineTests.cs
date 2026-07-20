using System.Numerics;
using Cerneala.Drawing;
using Cerneala.Drawing.Prism;
using Cerneala.Drawing.Prism.ColorManagement;
using Cerneala.Drawing.Prism.Graph;
using Cerneala.Drawing.Prism.Masking;
using Cerneala.UI.Prism.Definitions;

namespace Cerneala.Tests.Drawing.Prism;

public sealed class PrismMaskPipelineTests
{
    [Fact]
    public void ScalarMaskAndClippingReferencesStayDistinctAtPartialAlpha()
    {
        PrismPremultipliedColor sample =
            PrismPremultipliedColor.FromStraight(
                0.8,
                0.2,
                0.1,
                0.4);

        Assert.Equal(
            0.4,
            PrismMaskMath.ResolveScalar(
                sample,
                PrismMaskChannel.Alpha,
                density: 1,
                invert: false),
            precision: 12);
        Assert.Equal(
            0.32034,
            PrismMaskMath.ResolveScalar(
                sample,
                PrismMaskChannel.Luminance,
                density: 1,
                invert: false),
            precision: 12);
        Assert.Equal(
            0.6,
            PrismMaskMath.ResolveScalar(
                sample,
                PrismMaskChannel.Alpha,
                density: 1,
                invert: true),
            precision: 12);
        Assert.Equal(
            0.7,
            PrismMaskMath.ResolveScalar(
                sample,
                PrismMaskChannel.Alpha,
                density: 0.5,
                invert: false),
            precision: 12);
        Assert.Equal(
            0.25,
            PrismMaskMath.FeatherNine(
                [0.25, 0.25, 0.25, 0.25, 0.25, 0.25, 0.25, 0.25, 0.25]),
            precision: 12);

        PrismPremultipliedColor content =
            PrismPremultipliedColor.FromStraight(
                0.7,
                0.3,
                0.1,
                0.5);
        PrismPremultipliedColor masked =
            PrismMaskMath.ApplyMask(content, 0.25);
        PrismPremultipliedColor clipped =
            PrismMaskMath.ApplyClip(
                content,
                PrismPremultipliedColor.FromStraight(
                    0.1,
                    0.2,
                    0.3,
                    0.75));

        Assert.Equal(0.125, masked.Alpha, precision: 12);
        Assert.Equal(0.375, clipped.Alpha, precision: 12);
        Assert.NotEqual(masked, clipped);
    }

    [Fact]
    public void ZeroDensityMaskAndAbsentClippingAreProvenIdentityPaths()
    {
        PrismLayerDefinition plainLayer =
            PrismTestData.Layer(1, "Layer");
        PrismLayerDefinition identityLayer =
            PrismTestData.Layer(
                1,
                "Layer",
                mask: new PrismMaskDefinition(
                    new PrismResourceId("IdentityMask"),
                    channel: PrismMaskChannel.Luminance,
                    feather: 40,
                    density: 0,
                    invert: true));

        PrismGraph plain = Build(
            PrismTestData.Composition("Identity", plainLayer)).Graph;
        PrismGraph identity = Build(
            PrismTestData.Composition("Identity", identityLayer)).Graph;

        Assert.Equal(
            plain.Nodes.Select(NodeShape),
            identity.Nodes.Select(NodeShape));
        Assert.Equal(
            plain.Edges.Select(EdgeShape),
            identity.Edges.Select(EdgeShape));
        Assert.DoesNotContain(
            identity.Nodes,
            node => node.Kind is
                PrismGraphNodeKind.Mask or
                PrismGraphNodeKind.ClipToBelow);
        Assert.DoesNotContain(
            identity.Edges,
            edge => edge.Kind is
                PrismGraphEdgeKind.MaskAlpha or
                PrismGraphEdgeKind.ClipBaseAlpha);
    }

    [Fact]
    public void FeatherExpandsSamplingBoundsWithoutChangingOutputOrLayoutBounds()
    {
        const float feather = 2;
        PrismCompositionDefinition definition =
            PrismTestData.Composition(
                "Feather",
                PrismTestData.Layer(
                    1,
                    "Layer",
                    mask: new PrismMaskDefinition(
                        new PrismResourceId("FeatherMask"),
                        feather: feather,
                        density: 0.6f)));
        DrawRect controlBounds = new(0, 0, 10, 10);
        BuildResult result = Build(
            definition,
            controlBounds,
            Matrix3x2.CreateScale(2, 3));

        PrismGraphNode[] maskPasses = result.Graph.Nodes
            .Where(node => node.Kind == PrismGraphNodeKind.Mask)
            .OrderBy(node => node.Id.Ordinal)
            .ToArray();
        Assert.Equal(3, maskPasses.Length);
        Assert.Equal(
            [
                PrismMaskPass.Extract,
                PrismMaskPass.FeatherHorizontal,
                PrismMaskPass.FeatherVertical
            ],
            maskPasses.Select(node => node.MaskPass));
        Assert.Equal(0.6f, maskPasses[2].Density);

        PrismGraphNodePlan extract =
            result.Plan.GetNodePlan(maskPasses[0].Id);
        PrismGraphNodePlan horizontal =
            result.Plan.GetNodePlan(maskPasses[1].Id);
        PrismGraphNodePlan vertical =
            result.Plan.GetNodePlan(maskPasses[2].Id);
        Assert.Equal(new DrawRect(0, 0, 20, 30), extract.Bounds);
        Assert.Equal(new DrawRect(-6, 0, 32, 30), horizontal.Bounds);
        Assert.Equal(new DrawRect(-6, -6, 32, 42), vertical.Bounds);

        PrismGraph optimized = result.Plan.OptimizedGraph;
        PrismGraphNode maskComposite = optimized.Nodes.Single(
            node =>
                node.Kind == PrismGraphNodeKind.Composite &&
                optimized.Edges.Any(
                    edge =>
                        edge.Target == node.Id &&
                        edge.Kind == PrismGraphEdgeKind.MaskAlpha));
        PrismGraphEdge content = optimized.Edges.Single(
            edge =>
                edge.Target == maskComposite.Id &&
                edge.Kind == PrismGraphEdgeKind.Content);
        Assert.Equal(
            result.Plan.GetNodePlan(content.Source).Bounds,
            result.Plan.GetNodePlan(maskComposite.Id).Bounds);
        Assert.Equal(controlBounds, result.Analysis.Scopes[0].Scope.ControlBounds);
        Assert.Equal(
            new DrawRect(0, 0, 20, 30),
            result.Analysis.Scopes[0].Bounds);
    }

    [Fact]
    public void EveryClippedSiblingUsesTheNearestUnclippedBaseAlpha()
    {
        PrismMaskDefinition mask = new(
            new PrismResourceId("ClippedMask"),
            density: 0.8f);
        PrismCompositionDefinition definition =
            PrismTestData.Composition(
                "Clipping chain",
                PrismTestData.Layer(
                    3,
                    "Top clip",
                    clipToBelow: true),
                PrismTestData.Layer(
                    2,
                    "Masked clip",
                    clipToBelow: true,
                    mask: mask),
                PrismTestData.Layer(
                    1,
                    "Base",
                    opacity: 0.65f));
        PrismGraph graph = Build(definition).Graph;

        PrismGraphNode baseAlpha = graph.Nodes.Single(
            node =>
                node.Kind == PrismGraphNodeKind.Opacity &&
                node.DefinitionNodeId == new PrismNodeId(1));
        PrismGraphNode[] clips = graph.Nodes
            .Where(node => node.Kind == PrismGraphNodeKind.ClipToBelow)
            .ToArray();
        Assert.Equal(2, clips.Length);
        foreach (PrismGraphNode clip in clips)
        {
            PrismGraphEdge alphaEdge = graph.Edges.Single(
                edge =>
                    edge.Target == clip.Id &&
                    edge.Kind == PrismGraphEdgeKind.ClipBaseAlpha);
            Assert.Equal(baseAlpha.Id, alphaEdge.Source);
        }

        PrismGraphNode maskedClip = Assert.Single(
            clips,
            node => node.DefinitionNodeId == new PrismNodeId(2));
        PrismGraphEdge maskedContent = graph.Edges.Single(
            edge =>
                edge.Target == maskedClip.Id &&
                edge.Kind == PrismGraphEdgeKind.Content);
        Assert.Equal(
            PrismGraphNodeKind.Opacity,
            graph.GetNode(maskedContent.Source).Kind);
        Assert.Contains(
            graph.Nodes,
            node =>
                node.Kind == PrismGraphNodeKind.Mask &&
                node.DefinitionNodeId == new PrismNodeId(2));
        Assert.DoesNotContain(
            graph.Nodes,
            node => node.Kind == PrismGraphNodeKind.Group);
    }

    private static BuildResult Build(
        PrismCompositionDefinition definition,
        DrawRect bounds = default,
        Matrix3x2 transform = default)
    {
        PrismDrawScope scope = PrismTestData.Scope(
            definition,
            bounds: bounds,
            transform: transform);
        DrawCommandList commands = PrismTestData.Commands(
            DrawCommand.BeginPrism(scope),
            DrawCommand.FillRectangle(
                scope.ControlBounds,
                Color.White),
            DrawCommand.EndPrism());
        PrismFrameAnalysis analysis =
            new PrismFrameAnalyzer().Analyze(commands);
        PrismGraph graph = new PrismGraphBuilder().Build(analysis);
        PrismGraphExecutionPlan plan =
            new PrismGraphOptimizer().Optimize(graph);
        return new BuildResult(analysis, graph, plan);
    }

    private static object NodeShape(PrismGraphNode node) =>
        new
        {
            node.Id,
            node.Kind,
            node.DefinitionNodeId,
            node.DefinitionOrder
        };

    private static object EdgeShape(PrismGraphEdge edge) =>
        new
        {
            edge.Source,
            edge.Target,
            edge.Kind
        };

    private readonly record struct BuildResult(
        PrismFrameAnalysis Analysis,
        PrismGraph Graph,
        PrismGraphExecutionPlan Plan);
}
