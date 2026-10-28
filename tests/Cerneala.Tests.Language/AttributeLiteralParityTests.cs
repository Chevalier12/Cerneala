namespace Cerneala.Tests.Language;

// The editor (CernealaSemanticModel) must reject exactly the attribute literals the build (UiMarkupGenerator
// ParseLiteralValue) rejects with CERNEALAUI004, and accept the ones the build accepts (#867).
public sealed class AttributeLiteralParityTests
{
    [Theory]
    [InlineData("<Button Margin=\"abc\" />")]
    [InlineData("<Button Margin=\"1,2\" />")]
    [InlineData("<Button Margin=\"1,2,3,x\" />")]
    [InlineData("<Button Padding=\"-1\" />")]
    [InlineData("<Button Padding=\"0,0,-1,0\" />")]
    [InlineData("<Button Background=\"notacolor\" />")]
    [InlineData("<Button Background=\"#12345\" />")]
    [InlineData("<Button Background=\"300,0,0\" />")]
    [InlineData("<Button Width=\"-5\" />")]
    [InlineData("<Button FontSize=\"0\" />")]
    [InlineData("<Border CornerRadius=\"1,2\" />")]
    [InlineData("<Border CornerRadius=\"-1\" />")]
    [InlineData("<Button RenderTransformOrigin=\"0.5\" />")]
    [InlineData("<Line StartPoint=\"1,x\" />")]
    [InlineData("<Polyline Points=\"0,0 10\" />")]
    [InlineData("<Window xmlns:c=\"clr-namespace:CorpusInput\"><c:Knob Small=\"256\" /></Window>")]
    [InlineData("<Window xmlns:c=\"clr-namespace:CorpusInput\"><c:Knob Big=\"x\" /></Window>")]
    [InlineData("<Window xmlns:c=\"clr-namespace:CorpusInput\"><c:Knob Price=\"abc\" /></Window>")]
    [InlineData("<Window><Window.Resources><Aspect TargetType=\"Button\">@default { Margin = 1,2; }</Aspect></Window.Resources></Window>")]
    [InlineData("<Window><Window.Resources><Aspect TargetType=\"Button\">@default { Background = notacolor; }</Aspect></Window.Resources></Window>")]
    public void InvalidAttributeLiteralsAreReportedLikeTheBuild(string markup) =>
        AssertParity(markup, "CERNEALAUI004");

    [Theory]
    [InlineData("<Button Margin=\"4\" />")]
    [InlineData("<Button Margin=\"-4\" />")]
    [InlineData("<Button Margin=\" 1, 2 ,3,4 \" />")]
    [InlineData("<Button Padding=\"0,1,2,3\" />")]
    [InlineData("<Button Background=\"Red\" />")]
    [InlineData("<Button Background=\"cornflowerblue\" />")]
    [InlineData("<Button Background=\"#FF0000\" />")]
    [InlineData("<Button Background=\"#80FF0000\" />")]
    [InlineData("<Button Background=\"10, 20, 30\" />")]
    [InlineData("<Button Background=\"10,20,30,40\" />")]
    [InlineData("<Button Width=\"0\" />")]
    [InlineData("<Button Width=\"5.5\" />")]
    [InlineData("<Button FontSize=\"12\" />")]
    [InlineData("<Border CornerRadius=\"4\" />")]
    [InlineData("<Border CornerRadius=\"1,2,3,4\" />")]
    [InlineData("<Button RenderTransformOrigin=\"0.5,0.5\" />")]
    [InlineData("<Line StartPoint=\"1,2\" />")]
    [InlineData("<Polyline Points=\"0,0 10,10 20,0\" />")]
    [InlineData("<Window xmlns:c=\"clr-namespace:CorpusInput\"><c:Knob Small=\"255\" Big=\"-1\" Price=\"1.5\" /></Window>")]
    [InlineData("<Window><Window.Resources><Aspect TargetType=\"Button\">@default { Margin = 1,2,3,4; }</Aspect></Window.Resources></Window>")]
    [InlineData("<Window><Window.Resources><Aspect TargetType=\"Button\">@default { Background = \"#FF10282D\"; }</Aspect></Window.Resources></Window>")]
    [InlineData("<Window><Window.Resources><SolidColorBrush Name=\"Accent\" Color=\" Red \" /></Window.Resources></Window>")]
    public void ValidAttributeLiteralsAreAcceptedLikeTheBuild(string markup) =>
        AssertParity(markup, string.Empty);

    private const string KnobSource = """
        namespace CorpusInput;

        public sealed class Knob : Cerneala.UI.Controls.Control
        {
            public byte Small { get; set; }

            public long Big { get; set; }

            public decimal Price { get; set; }
        }
        """;

    private static void AssertParity(string markup, string generatorIds)
    {
        LanguagePipelineResult result = LanguagePipelineHarness.Analyze("Parity.crn", markup, "Knob.cs", KnobSource);

        string[] generator = result.SourceGeneratorDiagnostics.Select(diagnostic => diagnostic.Id).ToArray();
        string[] editor = result.SemanticDiagnostics.Select(diagnostic => diagnostic.Id).ToArray();
        string[] expected = generatorIds.Length == 0 ? [] : generatorIds.Split(',');
        string detail = "generator=[" + string.Join(",", generator) + "] editor=[" + string.Join(",", editor) +
            "] markup=" + markup;

        Assert.True(generator.SequenceEqual(expected), "Premise failed: " + detail);
        Assert.True(editor.SequenceEqual(generator), "Parity failed: " + detail);
    }
}
