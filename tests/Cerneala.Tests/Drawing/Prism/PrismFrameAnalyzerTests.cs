using Cerneala.Drawing;
using Cerneala.Drawing.Prism;
using Cerneala.Drawing.Prism.Catalog;
using Cerneala.Drawing.Prism.Graph;
using Cerneala.UI.Prism.Definitions;

namespace Cerneala.Tests.Drawing.Prism;

public sealed class PrismFrameAnalyzerTests
{
    [Theory]
    [InlineData(PrismBlendMode.Normal, false, 7)]
    [InlineData(PrismBlendMode.Multiply, true, 11)]
    public void RecursiveCapabilityEstimatesPreserveBackdropContribution(
        PrismBlendMode innerBlendMode,
        bool requiresBackdrop,
        int requiredSurfaceCount)
    {
        PrismLayerDefinition layer = new(
            new PrismNodeId(1),
            "Content",
            filters: [new PrismFilterDefinition(PrismFilterId.Invert)]);
        PrismGroupDefinition inner = new(
            new PrismNodeId(20),
            "Inner",
            [layer],
            blendMode: innerBlendMode);
        PrismGroupDefinition outer = new(
            new PrismNodeId(10),
            "Outer",
            [inner],
            blendMode: PrismBlendMode.PassThrough);
        PrismDrawScope scope = PrismTestData.Scope(
            PrismTestData.Composition("Recursive estimates", outer));
        DrawCommandList commands = PrismTestData.Commands(
            DrawCommand.BeginPrism(scope),
            DrawCommand.EndPrism());

        PrismFrameAnalysis analysis = new PrismFrameAnalyzer().Analyze(commands);
        PrismAnalyzedScope analyzedScope = Assert.Single(analysis.Scopes);
        PrismGraphCapabilities expectedCapabilities =
            PrismGraphCapabilities.ControlCapture |
            PrismGraphCapabilities.ColorConversion |
            PrismGraphCapabilities.FilterProcessing |
            PrismGraphCapabilities.GroupProcessing |
            PrismGraphCapabilities.GroupIsolation;
        if (requiresBackdrop)
        {
            expectedCapabilities |=
                PrismGraphCapabilities.AdvancedBlending |
                PrismGraphCapabilities.BackdropInput;
        }

        Assert.Equal(requiresBackdrop, analysis.RequiresBackdrop);
        Assert.Equal(expectedCapabilities, analysis.RequiredCapabilities);
        Assert.Equal(expectedCapabilities, analyzedScope.RequiredCapabilities);
        Assert.Equal(requiredSurfaceCount, analysis.RequiredSurfaceCount);
        Assert.Equal(requiredSurfaceCount, analyzedScope.RequiredSurfaceCount);
    }
}
