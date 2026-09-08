using Cerneala.Drawing;
using Cerneala.Drawing.Prism;
using Cerneala.Drawing.Prism.Catalog;
using Cerneala.Drawing.Prism.Graph;
using Cerneala.UI.Prism.Definitions;
using Cerneala.UI.Prism.Runtime;

namespace Cerneala.Tests.Drawing.Prism;

public sealed class PrismStyleBackdropAnalysisTests
{
    [Theory]
    [InlineData("Screen", 1f, "Normal", 0f, true, true)]
    [InlineData("Normal", 0f, "Multiply", 1f, true, true)]
    [InlineData("Screen", 0f, "Multiply", 0f, true, false)]
    [InlineData("Normal", 1f, "Normal", 1f, true, false)]
    [InlineData("Screen", 1f, "Multiply", 1f, false, false)]
    public void BevelBackdropDependsOnBothVisibleBlendContributions(
        string highlightMode, float highlightOpacity,
        string shadowMode, float shadowOpacity,
        bool visible, bool expected)
    {
        (DrawCommandList commands, PrismStyleState state) = CreateBevel();
        Set(state, "HighlightMode", highlightMode);
        Set(state, "HighlightOpacity", highlightOpacity);
        Set(state, "ShadowMode", shadowMode);
        Set(state, "ShadowOpacity", shadowOpacity);
        state.Visible = visible;

        Assert.Equal(expected, new PrismFrameAnalyzer().Analyze(commands).RequiresBackdrop);
    }

    [Fact]
    public void ReusedAnalyzerReevaluatesChangedBevelContributionValues()
    {
        (DrawCommandList commands, PrismStyleState state) = CreateBevel();
        Set(state, "HighlightMode", "Screen");
        Set(state, "HighlightOpacity", 0f);
        Set(state, "ShadowOpacity", 0f);
        PrismFrameAnalyzer analyzer = new();
        Assert.False(analyzer.Analyze(commands).RequiresBackdrop);

        Set(state, "HighlightOpacity", 1f);
        Assert.True(analyzer.Analyze(commands).RequiresBackdrop);

        Set(state, "HighlightOpacity", 0f);
        Assert.False(analyzer.Analyze(commands).RequiresBackdrop);
    }

    private static void Set<T>(PrismStyleState state, string name, T value) =>
        state.SetValue(PrismCatalog.GetStyle(state.Style).Parameters.Single(parameter => parameter.Name == name), value);

    private static (DrawCommandList, PrismStyleState) CreateBevel()
    {
        PrismLayerDefinition layer = new(new(1), "Bevel", styles: [new(PrismStyleId.BevelEmboss)]);
        PrismDrawScope scope = PrismTestData.Scope(new("Bevel backdrop", [layer]));
        return (PrismTestData.Commands(DrawCommand.BeginPrism(scope),
            DrawCommand.FillRectangle(scope.ControlBounds, Color.Coral), DrawCommand.EndPrism()),
            scope.Instance.GetLayerState(layer.Id).Styles.Single());
    }
}
