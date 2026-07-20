using Cerneala.Drawing;
using Cerneala.Drawing.Prism;
using Cerneala.Drawing.Prism.Catalog;
using Cerneala.Drawing.Prism.ColorManagement;
using Cerneala.Drawing.Prism.Graph;
using Cerneala.UI.Prism.Definitions;
using CernealaColor = Cerneala.Drawing.Color;

namespace Cerneala.Tests.Drawing.Prism;

public sealed class PrismColorPipelineTests
{
    private const double NumericTolerance = 0.000002;

    [Fact]
    public void PrismColorRoundTripsTransparentEdgeAndOpaquePixels()
    {
        PrismPremultipliedColor[] samples =
        [
            default,
            new PrismPremultipliedColor(0.8, 0.4, 0.2, 0),
            PrismPremultipliedColor.FromStraight(
                1,
                0.5,
                0.25,
                1d / byte.MaxValue),
            PrismPremultipliedColor.FromStraight(
                0.13,
                0.67,
                0.91,
                0.5),
            PrismPremultipliedColor.FromStraight(
                0.98,
                0.49,
                0.02,
                1)
        ];

        Assert.Equal(
            "premultiplied",
            PrismColorPipeline.AlphaConvention);
        foreach (PrismColorProfile profile in
            Enum.GetValues<PrismColorProfile>())
        {
            foreach (PrismPremultipliedColor sample in samples)
            {
                PrismPremultipliedColor actual =
                    PrismColorPipeline.ConvertWorkingToOutput(
                        PrismColorPipeline.ConvertInputToWorking(
                            sample,
                            profile),
                        profile);
                PrismPremultipliedColor expected =
                    sample.Alpha == 0 ? default : sample;
                AssertClose(expected, actual, profile.ToString());
            }
        }
    }

    [Fact]
    public void PrismColorNestedProfilesCrossTheSrgbBoundaryOnlyOnce()
    {
        PrismPremultipliedColor source =
            PrismPremultipliedColor.FromStraight(
                0.82,
                0.17,
                0.54,
                0.63);

        foreach (PrismColorProfile innerProfile in
            Enum.GetValues<PrismColorProfile>())
        {
            PrismPremultipliedColor innerWorking =
                PrismColorPipeline.ConvertInputToWorking(
                    source,
                    innerProfile);
            PrismPremultipliedColor boundary =
                PrismColorPipeline.ConvertWorkingToOutput(
                    innerWorking,
                    innerProfile);
            foreach (PrismColorProfile outerProfile in
                Enum.GetValues<PrismColorProfile>())
            {
                PrismPremultipliedColor outerWorking =
                    PrismColorPipeline.ConvertInputToWorking(
                        boundary,
                        outerProfile);
                PrismPremultipliedColor actual =
                    PrismColorPipeline.ConvertWorkingToOutput(
                        outerWorking,
                        outerProfile);
                AssertClose(
                    source,
                    actual,
                    $"{innerProfile}->{outerProfile}");
            }
        }
    }

    [Fact]
    public void PrismColorReferenceDetectsDoubleGamma()
    {
        PrismPremultipliedColor source =
            PrismPremultipliedColor.FromStraight(
                0.5,
                0.25,
                0.75,
                1);
        PrismPremultipliedColor working =
            PrismColorPipeline.ConvertInputToWorking(
                source,
                PrismColorProfile.LinearSrgb);
        PrismPremultipliedColor correct =
            PrismColorPipeline.ConvertWorkingToOutput(
                working,
                PrismColorProfile.LinearSrgb);
        PrismPremultipliedColor doubleDecoded =
            PrismColorPipeline.ConvertInputToWorking(
                working,
                PrismColorProfile.LinearSrgb);
        PrismPremultipliedColor incorrect =
            PrismColorPipeline.ConvertWorkingToOutput(
                doubleDecoded,
                PrismColorProfile.LinearSrgb);

        AssertClose(source, correct, "single gamma");
        Assert.True(
            Math.Abs(source.Red - incorrect.Red) > 0.2,
            "The double-gamma sentinel did not diverge.");
    }

    [Fact]
    public void PrismColorGraphKeepsFillBeforeStylesAndOpacityAfterStyles()
    {
        PrismLayerDefinition layer = new(
            new PrismNodeId(1),
            "Styled",
            styles:
            [
                new PrismStyleDefinition(PrismStyleId.DropShadow)
            ],
            opacity: 0.4f,
            fill: 0.2f);
        PrismGraph graph = BuildGraph(
            new PrismCompositionDefinition(
                "Fill and opacity",
                [layer]));
        PrismGraphNode fill = Assert.Single(
            graph.Nodes.Where(
                node => node.Kind == PrismGraphNodeKind.Fill));
        PrismGraphNode style = Assert.Single(
            graph.Nodes.Where(
                node => node.Kind == PrismGraphNodeKind.Style));
        PrismGraphNode opacity = Assert.Single(
            graph.Nodes.Where(
                node => node.Kind == PrismGraphNodeKind.Opacity));

        Assert.Contains(
            graph.Edges,
            edge => edge.Source == fill.Id &&
                edge.Target == style.Id &&
                edge.Kind == PrismGraphEdgeKind.Content);
        Assert.Contains(
            graph.Edges,
            edge => edge.Source == style.Id &&
                edge.Target == opacity.Id &&
                edge.Kind == PrismGraphEdgeKind.Content);
    }

    [Fact]
    public void PrismColorNestedGraphAddsOneInputConversionPerScope()
    {
        PrismDrawScope outer = PrismTestData.Scope(
            new PrismCompositionDefinition(
                "Outer",
                [PrismTestData.Layer(1, "Outer layer")],
                workingColorProfile: PrismColorProfile.Srgb),
            ownerToken: 8101);
        PrismDrawScope inner = PrismTestData.Scope(
            new PrismCompositionDefinition(
                "Inner",
                [PrismTestData.Layer(2, "Inner layer")],
                workingColorProfile:
                    PrismColorProfile.LinearDisplayP3),
            ownerToken: 8102);
        DrawCommandList commands = PrismTestData.Commands(
            DrawCommand.BeginPrism(outer),
            DrawCommand.BeginPrism(inner),
            DrawCommand.FillRectangle(
                new DrawRect(0, 0, 10, 10),
                CernealaColor.White),
            DrawCommand.EndPrism(),
            DrawCommand.EndPrism());
        PrismGraph graph = new PrismGraphBuilder().Build(
            new PrismFrameAnalyzer().Analyze(commands));

        Assert.Equal(2, graph.Scopes.Length);
        foreach (PrismGraphScope scope in graph.Scopes)
        {
            PrismGraphNode conversion = Assert.Single(
                graph.Nodes.Where(node =>
                    node.AnalysisScopeIndex ==
                        scope.AnalysisScopeIndex &&
                    node.Kind ==
                        PrismGraphNodeKind.ColorConversion));
            Assert.Equal(
                scope.CompositionSettings.WorkingColorProfile,
                conversion.ColorProfile);
        }
    }

    private static PrismGraph BuildGraph(
        PrismCompositionDefinition composition)
    {
        PrismDrawScope scope =
            PrismTestData.Scope(composition);
        DrawCommandList commands = PrismTestData.Commands(
            DrawCommand.BeginPrism(scope),
            DrawCommand.FillRectangle(
                new DrawRect(0, 0, 10, 10),
                CernealaColor.White),
            DrawCommand.EndPrism());
        return new PrismGraphBuilder().Build(
            new PrismFrameAnalyzer().Analyze(commands));
    }

    private static void AssertClose(
        PrismPremultipliedColor expected,
        PrismPremultipliedColor actual,
        string context)
    {
        AssertChannelClose(
            expected.Red,
            actual.Red,
            context,
            "red");
        AssertChannelClose(
            expected.Green,
            actual.Green,
            context,
            "green");
        AssertChannelClose(
            expected.Blue,
            actual.Blue,
            context,
            "blue");
        AssertChannelClose(
            expected.Alpha,
            actual.Alpha,
            context,
            "alpha");
    }

    private static void AssertChannelClose(
        double expected,
        double actual,
        string context,
        string channel)
    {
        Assert.True(
            Math.Abs(expected - actual) <= NumericTolerance,
            $"{context} {channel} was {actual:R}, expected " +
            $"{expected:R} within {NumericTolerance:R}.");
    }
}
