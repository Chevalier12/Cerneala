using Cerneala.Drawing;
using Cerneala.Drawing.Prism;
using Cerneala.Drawing.Prism.Catalog;
using Cerneala.Drawing.Prism.Graph;
using Cerneala.UI.Prism.Definitions;
using Cerneala.UI.Prism.Runtime;

namespace Cerneala.Tests.Drawing.Prism;

public sealed class PrismGraphContractTests
{
    [Fact]
    public void GraphProcessesVisibleLayersBottomUpAndCapturesControlOnce()
    {
        PrismCompositionDefinition definition = PrismTestData.Composition(
            "LayerStack",
            PrismTestData.Layer(1, "Front"),
            PrismTestData.Layer(2, "Hidden", visible: false),
            PrismTestData.Layer(3, "Back"));

        PrismGraph graph = BuildGraph(definition);

        Assert.Single(
            graph.Nodes.Where(node => node.Kind == PrismGraphNodeKind.ControlCapture));
        Assert.Equal(
            [new PrismNodeId(3), new PrismNodeId(1)],
            graph.Nodes
                .Where(node => node.Kind == PrismGraphNodeKind.Layer)
                .Select(node => node.DefinitionNodeId));
        Assert.DoesNotContain(
            graph.Nodes,
            node => node.DefinitionNodeId == new PrismNodeId(2));
        Assert.Equal(1, graph.ControlCaptureCount);
        Assert.Contains(graph.Nodes, node => node.Kind == PrismGraphNodeKind.Fill);
        Assert.Contains(graph.Nodes, node => node.Kind == PrismGraphNodeKind.Opacity);
    }

    [Fact]
    public void GraphKeepsGroupsBottomUpAndOnlyNonPassThroughGroupsAreIsolated()
    {
        PrismGroupDefinition isolated = new(
            new PrismNodeId(10),
            "Isolated",
            [PrismTestData.Layer(11, "Front"), PrismTestData.Layer(12, "Back")],
            blendMode: PrismBlendMode.Normal);
        PrismGroupDefinition passThrough = new(
            new PrismNodeId(20),
            "PassThrough",
            [PrismTestData.Layer(21, "Front"), PrismTestData.Layer(22, "Back")],
            blendMode: PrismBlendMode.PassThrough);
        PrismCompositionDefinition definition = PrismTestData.Composition(
            "Groups",
            isolated,
            passThrough);

        PrismGraph graph = BuildGraph(definition);

        PrismGraphNode passThroughNode = Assert.Single(
            graph.Nodes.Where(
                node => node.Kind == PrismGraphNodeKind.Group &&
                    node.DefinitionNodeId == new PrismNodeId(20)));
        PrismGraphNode isolatedNode = Assert.Single(
            graph.Nodes.Where(
                node => node.Kind == PrismGraphNodeKind.Group &&
                    node.DefinitionNodeId == new PrismNodeId(10)));
        Assert.False(passThroughNode.IsIsolationBoundary);
        Assert.True(isolatedNode.IsIsolationBoundary);
        Assert.True(
            IndexOfLayer(graph, 22) < IndexOfLayer(graph, 21),
            "PassThrough children must still be processed bottom-up.");
        Assert.True(
            IndexOfLayer(graph, 12) < IndexOfLayer(graph, 11),
            "Isolated group children must be processed bottom-up.");
    }

    [Fact]
    public void PassThroughChildrenReceiveTheParentStackBackgroundDirectly()
    {
        PrismLayerDefinition multiply = new(
            new PrismNodeId(21),
            "Multiply",
            filters: [new PrismFilterDefinition(PrismFilterId.Blur)],
            blendMode: PrismBlendMode.Multiply);
        PrismGroupDefinition passThrough = new(
            new PrismNodeId(20),
            "PassThrough",
            [multiply],
            blendMode: PrismBlendMode.PassThrough);
        PrismCompositionDefinition definition = PrismTestData.Composition(
            "PassThroughBackground",
            passThrough,
            PrismTestData.Layer(99, "Base"));

        PrismGraph graph = BuildGraph(definition);

        PrismGraphNode baseComposite = Assert.Single(
            graph.Nodes.Where(
                node => node.Kind == PrismGraphNodeKind.Composite &&
                    node.DefinitionNodeId == new PrismNodeId(99)));
        PrismGraphNode childComposite = Assert.Single(
            graph.Nodes.Where(
                node => node.Kind == PrismGraphNodeKind.Composite &&
                    node.DefinitionNodeId == multiply.Id));
        PrismGraphNode group = Assert.Single(
            graph.Nodes.Where(
                node => node.Kind == PrismGraphNodeKind.Group &&
                    node.DefinitionNodeId == passThrough.Id));
        PrismGraphNode passThroughComposite = Assert.Single(
            graph.Nodes.Where(
                node => node.Kind == PrismGraphNodeKind.PassThroughComposite &&
                    node.DefinitionNodeId == passThrough.Id));
        Assert.Contains(
            graph.Edges,
            edge => edge.Source == baseComposite.Id &&
                edge.Target == childComposite.Id &&
                edge.Kind == PrismGraphEdgeKind.CompositeBackground);
        Assert.Contains(
            graph.Edges,
            edge => edge.Source == baseComposite.Id &&
                edge.Target == group.Id &&
                edge.Kind == PrismGraphEdgeKind.CompositeBackground);
        Assert.Contains(
            graph.Edges,
            edge => edge.Source == baseComposite.Id &&
                edge.Target == passThroughComposite.Id &&
                edge.Kind == PrismGraphEdgeKind.CompositeBackground);
        Assert.DoesNotContain(
            graph.Nodes,
            node => node.Kind == PrismGraphNodeKind.Composite &&
                node.BlendMode == PrismBlendMode.PassThrough);
    }

    [Fact]
    public void PassThroughCompositeKeepsGroupOperationsAndClippingAlphaLocal()
    {
        PrismGroupDefinition passThrough = new(
            new PrismNodeId(20),
            "PassThrough",
            [PrismTestData.Layer(21, "Child")],
            filters: [new PrismFilterDefinition(PrismFilterId.Blur)],
            mask: new PrismMaskDefinition(new PrismResourceId(73)),
            opacity: 0.5f,
            blendMode: PrismBlendMode.PassThrough);
        PrismCompositionDefinition definition = PrismTestData.Composition(
            "PassThroughBoundary",
            PrismTestData.Layer(1, "Clipped", clipToBelow: true),
            passThrough,
            PrismTestData.Layer(99, "Base"));

        PrismGraph graph = BuildGraph(definition);

        PrismGraphNode group = Assert.Single(
            graph.Nodes.Where(
                node => node.Kind == PrismGraphNodeKind.Group &&
                    node.DefinitionNodeId == passThrough.Id));
        PrismGraphNode opacity = Assert.Single(
            graph.Nodes.Where(
                node => node.Kind == PrismGraphNodeKind.Opacity &&
                    node.DefinitionNodeId == passThrough.Id));
        PrismGraphNode passThroughComposite = Assert.Single(
            graph.Nodes.Where(
                node => node.Kind == PrismGraphNodeKind.PassThroughComposite &&
                    node.DefinitionNodeId == passThrough.Id));
        PrismGraphNode baseComposite = Assert.Single(
            graph.Nodes.Where(
                node => node.Kind == PrismGraphNodeKind.Composite &&
                    node.DefinitionNodeId == new PrismNodeId(99)));
        Assert.Equal(0.5f, passThroughComposite.Amount);
        Assert.Contains(
            graph.Edges,
            edge => edge.Source == group.Id &&
                edge.Target == passThroughComposite.Id &&
                edge.Kind == PrismGraphEdgeKind.GroupContent);
        Assert.Contains(
            graph.Edges,
            edge => edge.Source == opacity.Id &&
                edge.Target == passThroughComposite.Id &&
                edge.Kind == PrismGraphEdgeKind.CompositeForeground);
        Assert.Contains(
            graph.Edges,
            edge => edge.Source == baseComposite.Id &&
                edge.Target == passThroughComposite.Id &&
                edge.Kind == PrismGraphEdgeKind.CompositeBackground);
        PrismGraphEdge clipEdge = Assert.Single(
            graph.Edges.Where(
                edge => edge.Kind == PrismGraphEdgeKind.ClipBaseAlpha));
        Assert.Equal(passThroughComposite.Id, clipEdge.Source);
        Assert.Equal(
            new PrismNodeId(1),
            graph.GetNode(clipEdge.Target).DefinitionNodeId);
    }

    [Fact]
    public void GraphModelsMaskAndClippingChainWithSeparateBaseAlphaEdges()
    {
        PrismMaskDefinition mask = new(new PrismResourceId(17));
        PrismCompositionDefinition definition = PrismTestData.Composition(
            "Clipping",
            PrismTestData.Layer(1, "Top", clipToBelow: true, mask: mask),
            PrismTestData.Layer(2, "Middle", clipToBelow: true),
            PrismTestData.Layer(3, "Base"));

        PrismGraph graph = BuildGraph(definition);

        Assert.Contains(
            graph.Nodes,
            node => node.Kind == PrismGraphNodeKind.Mask &&
                node.DefinitionNodeId == new PrismNodeId(1));
        PrismGraphEdge maskEdge = Assert.Single(
            graph.Edges.Where(edge => edge.Kind == PrismGraphEdgeKind.MaskAlpha));
        Assert.Equal(PrismGraphNodeKind.Mask, graph.GetNode(maskEdge.Source).Kind);

        PrismGraphEdge[] clipEdges = graph.Edges
            .Where(edge => edge.Kind == PrismGraphEdgeKind.ClipBaseAlpha)
            .ToArray();
        Assert.Equal(2, clipEdges.Length);
        Assert.All(
            clipEdges,
            edge =>
            {
                PrismGraphNode baseAlphaSource = graph.GetNode(edge.Source);
                Assert.Equal(PrismGraphNodeKind.Opacity, baseAlphaSource.Kind);
                Assert.Equal(new PrismNodeId(3), baseAlphaSource.DefinitionNodeId);
                Assert.Equal(PrismGraphNodeKind.ClipToBelow, graph.GetNode(edge.Target).Kind);
            });
    }

    [Fact]
    public void InvisibleClippingBaseBreaksTheChainInsteadOfReusingALowerLayer()
    {
        PrismCompositionDefinition definition = PrismTestData.Composition(
            "InvisibleClippingBase",
            PrismTestData.Layer(1, "Clipped", clipToBelow: true),
            PrismTestData.Layer(2, "InvisibleBase", visible: false),
            PrismTestData.Layer(3, "Lower"));

        PrismGraph graph = BuildGraph(definition);

        Assert.DoesNotContain(
            graph.Nodes,
            node => node.DefinitionNodeId == new PrismNodeId(1) ||
                node.DefinitionNodeId == new PrismNodeId(2));
        Assert.Contains(
            graph.Nodes,
            node => node.DefinitionNodeId == new PrismNodeId(3));
        Assert.DoesNotContain(
            graph.Nodes,
            node => node.Kind == PrismGraphNodeKind.ClipToBelow);
    }

    [Fact]
    public void BackdropIsASeparateInputBranchRatherThanAnotherControlCapture()
    {
        PrismCompositionDefinition definition = PrismTestData.Composition(
            "Backdrop",
            PrismTestData.Layer(1, "Content"),
            PrismTestData.Backdrop(2, "Glass"));

        PrismGraph graph = BuildGraph(definition);

        PrismGraphNode capture = Assert.Single(
            graph.Nodes.Where(node => node.Kind == PrismGraphNodeKind.ControlCapture));
        PrismGraphNode backdrop = Assert.Single(
            graph.Nodes.Where(node => node.Kind == PrismGraphNodeKind.BackdropInput));
        Assert.NotEqual(capture.Id, backdrop.Id);
        Assert.DoesNotContain(
            graph.Edges,
            edge => edge.Source == capture.Id &&
                edge.Target == backdrop.Id);
        Assert.Contains(
            graph.Edges,
            edge => edge.Source == backdrop.Id &&
                edge.Kind == PrismGraphEdgeKind.Backdrop);
        Assert.Equal(1, graph.ControlCaptureCount);
        Assert.Equal(1, graph.BackdropInputCount);
    }

    [Fact]
    public void GraphUsesStableIdsAndExplicitPixelDependenciesAcrossValueFrames()
    {
        PrismDrawScope scope = PrismTestData.Scope(
            PrismTestData.Composition(
                "Stable",
                PrismTestData.Layer(1, "Content")),
            ownerToken: 91,
            pixelScale: 2,
            visualContentVersion: 7);
        DrawCommandList commands = PrismTestData.Commands(
            DrawCommand.BeginPrism(scope),
            DrawCommand.EndPrism());
        PrismFrameAnalyzer analyzer = new();
        PrismGraphBuilder builder = new();

        PrismGraph first = builder.Build(analyzer.Analyze(commands));
        scope.Instance.GetLayerState(new PrismNodeId(1)).Opacity = 0.4f;
        PrismGraph second = builder.Build(analyzer.Analyze(commands));

        Assert.Equal(
            first.Nodes.Select(node => node.Id),
            second.Nodes.Select(node => node.Id));
        PrismGraphNode firstOpacity = Assert.Single(
            first.Nodes.Where(
                node => node.Kind == PrismGraphNodeKind.Opacity &&
                    node.DefinitionNodeId == new PrismNodeId(1)));
        PrismGraphNode secondOpacity = second.GetNode(firstOpacity.Id);
        Assert.Equal(1, firstOpacity.Amount);
        Assert.Equal(0.4f, secondOpacity.Amount);
        Assert.NotEqual(
            Dependency(firstOpacity, PrismGraphDependencyKind.Values).Version,
            Dependency(secondOpacity, PrismGraphDependencyKind.Values).Version);

        PrismGraphNode capture = Assert.Single(
            first.Nodes.Where(node => node.Kind == PrismGraphNodeKind.ControlCapture));
        Assert.Contains(
            capture.Dependencies,
            dependency => dependency.Kind == PrismGraphDependencyKind.VisualContent &&
                dependency.Version == 7);
        Assert.Contains(
            capture.Dependencies,
            dependency => dependency.Kind == PrismGraphDependencyKind.Bounds);
        Assert.Contains(
            capture.Dependencies,
            dependency => dependency.Kind == PrismGraphDependencyKind.PixelScale);
        Assert.Contains(
            capture.Dependencies,
            dependency => dependency.Kind == PrismGraphDependencyKind.Transform);

        PrismGraphNode layer = Assert.Single(
            first.Nodes.Where(node => node.Kind == PrismGraphNodeKind.Layer));
        PrismGraphEdge controlEdge = Assert.Single(
            first.Edges.Where(
                edge => edge.Target == layer.Id &&
                    edge.Kind == PrismGraphEdgeKind.Control));
        Assert.Equal(
            PrismGraphNodeKind.ColorConversion,
            first.GetNode(controlEdge.Source).Kind);
        PrismGraphNode filter = Assert.Single(
            first.Nodes.Where(node => node.Kind == PrismGraphNodeKind.Filter));
        Assert.NotEmpty(filter.Parameters);
        Assert.Contains(
            filter.Dependencies,
            dependency => dependency.Kind == PrismGraphDependencyKind.CatalogEntry);
    }

    [Fact]
    public void GraphSnapshotsTypedFilterAndStyleOperations()
    {
        PrismLayerDefinition layer = new(
            new PrismNodeId(1),
            "Styled",
            filters: [new PrismFilterDefinition(PrismFilterId.Blur)],
            styles: [new PrismStyleDefinition(PrismStyleId.DropShadow)]);

        PrismGraph graph = BuildGraph(
            PrismTestData.Composition("Operations", layer));

        PrismGraphNode filter = Assert.Single(
            graph.Nodes.Where(node => node.Kind == PrismGraphNodeKind.Filter));
        PrismGraphNode style = Assert.Single(
            graph.Nodes.Where(node => node.Kind == PrismGraphNodeKind.Style));
        Assert.Equal(PrismFilterId.Blur, filter.Filter);
        Assert.Equal(PrismStyleId.DropShadow, style.Style);
        Assert.NotEmpty(filter.Parameters);
        Assert.NotEmpty(style.Parameters);
        Assert.All(
            filter.Parameters,
            parameter => Assert.True(parameter.Index >= 0));
        Assert.All(
            style.Parameters,
            parameter => Assert.True(parameter.Index >= 0));
    }

    [Fact]
    public void GraphSnapshotsAdvancedLayerAndCompositionPixelSettings()
    {
        PrismDrawScope scope = PrismTestData.Scope(
            PrismTestData.Composition(
                "AdvancedSettings",
                PrismTestData.Layer(1, "Content")));
        PrismLayerState state = scope.Instance.GetLayerState(new PrismNodeId(1));
        state.BlendChannels = PrismBlendChannels.Red | PrismBlendChannels.Alpha;
        state.Knockout = PrismKnockout.Deep;
        state.BlendInteriorStylesAsGroup = true;
        state.BlendClippedLayersAsGroup = false;
        state.TransparencyShapesLayer = false;
        state.LayerMaskHidesStyles = true;
        state.VectorMaskHidesStyles = true;
        state.BlendIfChannel = PrismBlendIfChannel.Blue;
        state.ThisLayerRange = new PrismBlendRange(0.1f, 0.2f, 0.7f, 0.9f);
        state.UnderlyingRange = new PrismBlendRange(0.05f, 0.3f, 0.8f, 1f);
        state.DissolveSeed = 47;
        scope.Instance.Composition.GlobalLightAngle = 123;
        scope.Instance.Composition.GlobalLightAltitude = 37;
        DrawCommandList commands = PrismTestData.Commands(
            DrawCommand.BeginPrism(scope),
            DrawCommand.EndPrism());

        PrismGraph graph = new PrismGraphBuilder().Build(
            new PrismFrameAnalyzer().Analyze(commands));

        PrismGraphNode layer = Assert.Single(
            graph.Nodes.Where(node => node.Kind == PrismGraphNodeKind.Layer));
        Assert.True(layer.LayerSettings.HasValue);
        Assert.Equal(
            new PrismGraphLayerSettings(
                PrismBlendChannels.Red | PrismBlendChannels.Alpha,
                PrismKnockout.Deep,
                BlendInteriorStylesAsGroup: true,
                BlendClippedLayersAsGroup: false,
                TransparencyShapesLayer: false,
                LayerMaskHidesStyles: true,
                VectorMaskHidesStyles: true,
                PrismBlendIfChannel.Blue,
                new PrismBlendRange(0.1f, 0.2f, 0.7f, 0.9f),
                new PrismBlendRange(0.05f, 0.3f, 0.8f, 1f),
                DissolveSeed: 47),
            layer.LayerSettings.Value);
        Assert.Equal(
            new PrismGraphCompositionSettings(
                PrismColorProfile.LinearSrgb,
                GlobalLightAngle: 123,
                GlobalLightAltitude: 37),
            Assert.Single(graph.Scopes).CompositionSettings);
    }

    [Fact]
    public void GraphBuildFailureCarriesCompositionNodeAndSourceSpan()
    {
        PrismLayerDefinition layer = new(
            new PrismNodeId(1),
            "Broken",
            filters: [new PrismFilterDefinition(PrismFilterId.Blur)],
            sourceSpan: new PrismSourceSpan(12, 6, "Card.cui.xml"));
        PrismCompositionDefinition definition =
            PrismTestData.Composition("DiagnosticCard", layer);
        PrismDrawScope scope = PrismTestData.Scope(definition);
        scope.Instance.GetLayerState(layer.Id).BlendMode = (PrismBlendMode)int.MaxValue;
        DrawCommandList commands = PrismTestData.Commands(
            DrawCommand.BeginPrism(scope),
            DrawCommand.EndPrism());
        PrismFrameAnalysis analysis = new PrismFrameAnalyzer().Analyze(commands);

        PrismGraphBuildException exception = Assert.Throws<PrismGraphBuildException>(
            () => new PrismGraphBuilder().Build(analysis));

        Assert.Equal("DiagnosticCard", exception.Diagnostic.CompositionName);
        Assert.Equal(layer.Id, exception.Diagnostic.NodeId);
        Assert.Equal("Broken", exception.Diagnostic.NodeName);
        Assert.Equal(layer.SourceSpan, exception.Diagnostic.SourceSpan);
        Assert.Contains("Card.cui.xml@12+6", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void BackdropBuildFailureCarriesBackdropDiagnosticContext()
    {
        PrismBackdropDefinition backdrop = new(
            new PrismNodeId(2),
            "BrokenBackdrop",
            filters: [new PrismFilterDefinition(PrismFilterId.GaussianBlur)],
            mask: new PrismMaskDefinition(new PrismResourceId(71)),
            sourceSpan: new PrismSourceSpan(33, 8, "Window.cui.xml"));
        PrismDrawScope scope = PrismTestData.Scope(
            PrismTestData.Composition(
                "BackdropDiagnostic",
                PrismTestData.Layer(1, "Content"),
                backdrop));
        scope.Instance.Backdrop!.Mask!.Channel = (PrismMaskChannel)int.MaxValue;
        DrawCommandList commands = PrismTestData.Commands(
            DrawCommand.BeginPrism(scope),
            DrawCommand.EndPrism());

        PrismGraphBuildException exception = Assert.Throws<PrismGraphBuildException>(
            () => new PrismGraphBuilder().Build(
                new PrismFrameAnalyzer().Analyze(commands)));

        Assert.Equal("BackdropDiagnostic", exception.Diagnostic.CompositionName);
        Assert.Equal(backdrop.Id, exception.Diagnostic.NodeId);
        Assert.Equal(backdrop.Name, exception.Diagnostic.NodeName);
        Assert.Equal(backdrop.SourceSpan, exception.Diagnostic.SourceSpan);
    }

    [Fact]
    public void StaleMultiScopeDiagnosticNamesTheScopeThatActuallyChanged()
    {
        PrismDrawScope first = PrismTestData.Scope(
            PrismTestData.Composition(
                "First",
                PrismTestData.Layer(1, "Content")),
            ownerToken: 81);
        PrismSourceSpan secondSpan = new(44, 5, "Second.cui.xml");
        PrismCompositionDefinition secondDefinition = new(
            "Second",
            [PrismTestData.Layer(2, "Content")],
            sourceSpan: secondSpan);
        PrismDrawScope second = PrismTestData.Scope(secondDefinition, ownerToken: 82);
        DrawCommandList commands = PrismTestData.Commands(
            DrawCommand.BeginPrism(first),
            DrawCommand.EndPrism(),
            DrawCommand.BeginPrism(second),
            DrawCommand.EndPrism());
        PrismFrameAnalysis analysis = new PrismFrameAnalyzer().Analyze(commands);
        second.Instance.GetLayerState(new PrismNodeId(2)).Opacity = 0.5f;

        PrismGraphBuildException exception = Assert.Throws<PrismGraphBuildException>(
            () => new PrismGraphBuilder().Build(analysis));

        Assert.Equal("Second", exception.Diagnostic.CompositionName);
        Assert.Equal(secondSpan, exception.Diagnostic.SourceSpan);
    }

    [Fact]
    public void GoldenSnapshotsCoverSimpleNestedMaskedAndClippedGraphs()
    {
        PrismCompositionDefinition simple = PrismTestData.Composition(
            "Simple",
            PrismTestData.Layer(1, "Front"),
            PrismTestData.Layer(2, "Hidden", visible: false),
            PrismTestData.Layer(3, "Back"));
        PrismGroupDefinition group = new(
            new PrismNodeId(10),
            "Group",
            [PrismTestData.Layer(11, "Front"), PrismTestData.Layer(12, "Back")],
            blendMode: PrismBlendMode.Normal);
        PrismCompositionDefinition nested = PrismTestData.Composition("Nested", group);
        PrismCompositionDefinition masked = PrismTestData.Composition(
            "Masked",
            PrismTestData.Layer(
                1,
                "Mask",
                mask: new PrismMaskDefinition(new PrismResourceId(17))));
        PrismCompositionDefinition clipped = PrismTestData.Composition(
            "Clipped",
            PrismTestData.Layer(1, "Top", clipToBelow: true),
            PrismTestData.Layer(2, "Middle", clipToBelow: true),
            PrismTestData.Layer(3, "Base"));

        AssertSnapshot(
            simple,
            """
            captures=1;backdrops=0
            N:ControlCapture:0:-1:Structure,Values,Descendants,VisualContent,Bounds,PixelScale,Transform
            N:ColorConversion:0:-1:Structure,Values,Descendants,ColorProfile
            N:Layer:3:2:Structure,Values,Descendants
            N:Filter:3:2:Structure,Values,Descendants,CatalogEntry
            N:Fill:3:2:Structure,Values,Descendants
            N:Opacity:3:2:Structure,Values,Descendants
            N:Composite:3:2:Structure,Values,Descendants
            N:Layer:1:0:Structure,Values,Descendants
            N:Filter:1:0:Structure,Values,Descendants,CatalogEntry
            N:Fill:1:0:Structure,Values,Descendants
            N:Opacity:1:0:Structure,Values,Descendants
            N:Composite:1:0:Structure,Values,Descendants
            E:ControlCapture/0->ColorConversion/0:Content
            E:ColorConversion/0->Layer/3:Control
            E:Layer/3->Filter/3:Content
            E:Filter/3->Fill/3:Content
            E:Fill/3->Opacity/3:Content
            E:Opacity/3->Composite/3:CompositeForeground
            E:ColorConversion/0->Layer/1:Control
            E:Layer/1->Filter/1:Content
            E:Filter/1->Fill/1:Content
            E:Fill/1->Opacity/1:Content
            E:Composite/3->Composite/1:CompositeBackground
            E:Opacity/1->Composite/1:CompositeForeground
            """);
        AssertSnapshot(
            nested,
            """
            captures=1;backdrops=0
            N:ControlCapture:0:-1:Structure,Values,Descendants,VisualContent,Bounds,PixelScale,Transform
            N:ColorConversion:0:-1:Structure,Values,Descendants,ColorProfile
            N:Layer:12:2:Structure,Values,Descendants
            N:Filter:12:2:Structure,Values,Descendants,CatalogEntry
            N:Fill:12:2:Structure,Values,Descendants
            N:Opacity:12:2:Structure,Values,Descendants
            N:Composite:12:2:Structure,Values,Descendants
            N:Layer:11:1:Structure,Values,Descendants
            N:Filter:11:1:Structure,Values,Descendants,CatalogEntry
            N:Fill:11:1:Structure,Values,Descendants
            N:Opacity:11:1:Structure,Values,Descendants
            N:Composite:11:1:Structure,Values,Descendants
            N:Group:10:0:Structure,Values,Descendants
            N:Opacity:10:0:Structure,Values,Descendants
            N:Composite:10:0:Structure,Values,Descendants
            E:ControlCapture/0->ColorConversion/0:Content
            E:ColorConversion/0->Layer/12:Control
            E:Layer/12->Filter/12:Content
            E:Filter/12->Fill/12:Content
            E:Fill/12->Opacity/12:Content
            E:Opacity/12->Composite/12:CompositeForeground
            E:ColorConversion/0->Layer/11:Control
            E:Layer/11->Filter/11:Content
            E:Filter/11->Fill/11:Content
            E:Fill/11->Opacity/11:Content
            E:Composite/12->Composite/11:CompositeBackground
            E:Opacity/11->Composite/11:CompositeForeground
            E:Composite/11->Group/10:GroupContent
            E:Group/10->Opacity/10:Content
            E:Opacity/10->Composite/10:CompositeForeground
            """);
        AssertSnapshot(
            masked,
            """
            captures=1;backdrops=0
            N:ControlCapture:0:-1:Structure,Values,Descendants,VisualContent,Bounds,PixelScale,Transform
            N:ColorConversion:0:-1:Structure,Values,Descendants,ColorProfile
            N:Layer:1:0:Structure,Values,Descendants
            N:Filter:1:0:Structure,Values,Descendants,CatalogEntry
            N:Fill:1:0:Structure,Values,Descendants
            N:Mask:1:0:Structure,Values,Descendants,Resource
            N:Composite:1:0:Structure,Values,Descendants
            N:Opacity:1:0:Structure,Values,Descendants
            N:Composite:1:0:Structure,Values,Descendants
            E:ControlCapture/0->ColorConversion/0:Content
            E:ColorConversion/0->Layer/1:Control
            E:Layer/1->Filter/1:Content
            E:Filter/1->Fill/1:Content
            E:Fill/1->Composite/1:Content
            E:Mask/1->Composite/1:MaskAlpha
            E:Composite/1->Opacity/1:Content
            E:Opacity/1->Composite/1:CompositeForeground
            """);
        AssertSnapshot(
            clipped,
            """
            captures=1;backdrops=0
            N:ControlCapture:0:-1:Structure,Values,Descendants,VisualContent,Bounds,PixelScale,Transform
            N:ColorConversion:0:-1:Structure,Values,Descendants,ColorProfile
            N:Layer:3:2:Structure,Values,Descendants
            N:Filter:3:2:Structure,Values,Descendants,CatalogEntry
            N:Fill:3:2:Structure,Values,Descendants
            N:Opacity:3:2:Structure,Values,Descendants
            N:Composite:3:2:Structure,Values,Descendants
            N:Layer:2:1:Structure,Values,Descendants
            N:Filter:2:1:Structure,Values,Descendants,CatalogEntry
            N:Fill:2:1:Structure,Values,Descendants
            N:Opacity:2:1:Structure,Values,Descendants
            N:ClipToBelow:2:1:Structure,Values,Descendants
            N:Composite:2:1:Structure,Values,Descendants
            N:Layer:1:0:Structure,Values,Descendants
            N:Filter:1:0:Structure,Values,Descendants,CatalogEntry
            N:Fill:1:0:Structure,Values,Descendants
            N:Opacity:1:0:Structure,Values,Descendants
            N:ClipToBelow:1:0:Structure,Values,Descendants
            N:Composite:1:0:Structure,Values,Descendants
            E:ControlCapture/0->ColorConversion/0:Content
            E:ColorConversion/0->Layer/3:Control
            E:Layer/3->Filter/3:Content
            E:Filter/3->Fill/3:Content
            E:Fill/3->Opacity/3:Content
            E:Opacity/3->Composite/3:CompositeForeground
            E:ColorConversion/0->Layer/2:Control
            E:Layer/2->Filter/2:Content
            E:Filter/2->Fill/2:Content
            E:Fill/2->Opacity/2:Content
            E:Opacity/2->ClipToBelow/2:Content
            E:Opacity/3->ClipToBelow/2:ClipBaseAlpha
            E:Composite/3->Composite/2:CompositeBackground
            E:ClipToBelow/2->Composite/2:CompositeForeground
            E:ColorConversion/0->Layer/1:Control
            E:Layer/1->Filter/1:Content
            E:Filter/1->Fill/1:Content
            E:Fill/1->Opacity/1:Content
            E:Opacity/1->ClipToBelow/1:Content
            E:Opacity/3->ClipToBelow/1:ClipBaseAlpha
            E:Composite/2->Composite/1:CompositeBackground
            E:ClipToBelow/1->Composite/1:CompositeForeground
            """);
    }

    private static PrismGraph BuildGraph(PrismCompositionDefinition definition)
    {
        PrismDrawScope scope = PrismTestData.Scope(definition);
        DrawCommandList commands = PrismTestData.Commands(
            DrawCommand.BeginPrism(scope),
            DrawCommand.FillRectangle(new DrawRect(0, 0, 5, 5), Color.White),
            DrawCommand.EndPrism());
        PrismFrameAnalysis analysis = new PrismFrameAnalyzer().Analyze(commands);
        return new PrismGraphBuilder().Build(analysis);
    }

    private static int IndexOfLayer(PrismGraph graph, int id)
    {
        return graph.Nodes
            .Select((node, index) => (Node: node, Index: index))
            .First(
                item => item.Node.Kind == PrismGraphNodeKind.Layer &&
                    item.Node.DefinitionNodeId == new PrismNodeId(id))
            .Index;
    }

    private static PrismGraphDependency Dependency(
        PrismGraphNode node,
        PrismGraphDependencyKind kind)
    {
        return Assert.Single(node.Dependencies.Where(dependency => dependency.Kind == kind));
    }

    private static void AssertSnapshot(
        PrismCompositionDefinition definition,
        string expected)
    {
        Assert.Equal(
            expected.ReplaceLineEndings("\n"),
            Snapshot(BuildGraph(definition)));
    }

    private static string Snapshot(PrismGraph graph)
    {
        List<string> lines =
        [
            $"captures={graph.ControlCaptureCount};backdrops={graph.BackdropInputCount}"
        ];
        lines.AddRange(
            graph.Nodes.Select(
                node =>
                    $"N:{node.Kind}:{node.DefinitionNodeId?.Value ?? 0}:{node.DefinitionOrder}:" +
                    string.Join(",", node.Dependencies.Select(dependency => dependency.Kind))));
        lines.AddRange(
            graph.Edges.Select(
                edge =>
                {
                    PrismGraphNode source = graph.GetNode(edge.Source);
                    PrismGraphNode target = graph.GetNode(edge.Target);
                    return $"E:{source.Kind}/{source.DefinitionNodeId?.Value ?? 0}->" +
                        $"{target.Kind}/{target.DefinitionNodeId?.Value ?? 0}:{edge.Kind}";
                }));
        return string.Join("\n", lines);
    }
}
