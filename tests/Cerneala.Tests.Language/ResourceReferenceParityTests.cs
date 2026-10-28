namespace Cerneala.Tests.Language;

// A bare '$Name' reference is a resource reference. The build converts it only into a Brush, a Color (from a brush
// that declares Color), an image, or a SpriteAnimationSet target, and resolves '$root', '$self' and '$owner' without
// a member as resource names (which they can never be). The editor must report the same diagnostics as the build.
public sealed class ResourceReferenceParityTests
{
    private const string Ink = "<SolidColorBrush Name=\"Ink\" Color=\"#112233\" />";

    [Theory]
    [InlineData("<StackPanel><StackPanel.Resources>" + Ink + "</StackPanel.Resources><Button Content=\"$Ink\" /></StackPanel>", "CERNEALAUI004")]
    [InlineData("<StackPanel><StackPanel.Resources>" + Ink + "</StackPanel.Resources><Button Width=\"$Ink\" /></StackPanel>", "CERNEALAUI004")]
    [InlineData("<StackPanel><StackPanel.Resources>" + Ink + "</StackPanel.Resources><Button Background=\"$Ink\" /></StackPanel>", "")]
    [InlineData("<StackPanel><StackPanel.Resources>" + Ink + "</StackPanel.Resources><TextBlock Foreground=\"$Ink\" /></StackPanel>", "")]
    [InlineData("<StackPanel><StackPanel.Resources>" + Ink + "</StackPanel.Resources><TextBox SelectionBackground=\"$Ink\" /></StackPanel>", "")]
    [InlineData("<StackPanel><StackPanel.Resources><Aspect Name=\"A\" TargetType=\"Button\">@default { Width = 10; }</Aspect></StackPanel.Resources><Button Background=\"$A\" /></StackPanel>", "CERNEALAUI004")]
    public void AttributeResourceReferencesAreTypeCheckedLikeTheBuild(string markup, string expectedIds) =>
        AssertEditorAndBuildReportTheSameDiagnostics(markup, expectedIds);

    [Theory]
    [InlineData("Button", "Width = $Ink;", "CERNEALAUI004")]
    [InlineData("Button", "Content = $Ink;", "CERNEALAUI004")]
    [InlineData("Button", "Width = abc;", "CERNEALAUI004")]
    [InlineData("Button", "Background = $Ink;", "")]
    [InlineData("TextBox", "SelectionBackground = $Ink;", "")]
    public void AspectResourceReferencesAreTypeCheckedLikeTheBuild(string targetType, string assignment, string expectedIds) =>
        AssertEditorAndBuildReportTheSameDiagnostics(
            "<StackPanel><StackPanel.Resources>" + Ink +
            "<Aspect Name=\"A\" TargetType=\"" + targetType + "\">@default { " + assignment + " }</Aspect>" +
            "</StackPanel.Resources><" + targetType + " Aspect=\"$A\" /></StackPanel>",
            expectedIds);

    [Theory]
    [InlineData("<StackPanel><StackPanel DataContext=\"$root\" /></StackPanel>", "CERNEALAUI004")]
    [InlineData("<StackPanel><StackPanel DataContext=\"$self\" /></StackPanel>", "CERNEALAUI004")]
    [InlineData("<StackPanel><TextBlock Text=\"$root\" /></StackPanel>", "CERNEALAUI004")]
    [InlineData("<StackPanel><Button Content=\"$owner\" /></StackPanel>", "CERNEALAUI004")]
    [InlineData("<Button>@template { <Border Name=\"Chrome\" DataContext=\"$owner\" /> }</Button>", "CERNEALAUI004")]
    [InlineData("<Button>@template { <Border Name=\"Chrome\" DataContext=\"$self\" /> }</Button>", "CERNEALAUI004")]
    public void ReservedSourcesWithoutAMemberAreRejectedLikeTheBuild(string markup, string expectedIds) =>
        AssertEditorAndBuildReportTheSameDiagnostics(markup, expectedIds);

    private static void AssertEditorAndBuildReportTheSameDiagnostics(string markup, string expectedIds)
    {
        LanguagePipelineResult result = LanguagePipelineHarness.Analyze("ResourceParity.crn", markup);

        Assert.Empty(result.Syntax.Diagnostics);
        Assert.Equal(expectedIds, string.Join(",", result.SourceGeneratorDiagnostics.Select(diagnostic => diagnostic.Id)));
        Assert.Equal(result.SourceGeneratorDiagnostics, result.SemanticDiagnostics);
    }
}
