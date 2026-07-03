namespace Cerneala.Tests.Playground;

public sealed class Game1SourceTests
{
    [Fact]
    public void Game1UsesRetainedSampleSelectorInsteadOfImmediateDemoElement()
    {
        string source = File.ReadAllText(Path.GetFullPath(
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "Playground", "Cerneala.Playground", "Game1.cs")));

        Assert.Contains("_resources = new ResourceStore()", source, StringComparison.Ordinal);
        Assert.Contains("new FontResource(_uiHost.ContentServices.LoadFont(\"Arial\", 16))", source, StringComparison.Ordinal);
        Assert.Contains("SampleSelector.CreateDefault(_resources, PlaygroundFontId)", source, StringComparison.Ordinal);
        Assert.Contains("uiRoot.VisualChildren.Add(_sampleSelector.Root)", source, StringComparison.Ordinal);
        Assert.Contains("RequireUiHost().Update(GetViewport(), gameTime.ElapsedGameTime)", source, StringComparison.Ordinal);
        Assert.Contains("RequireUiHost().Draw()", source, StringComparison.Ordinal);
        Assert.Contains("QueueTextInput(e.Character.ToString())", source, StringComparison.Ordinal);
        Assert.DoesNotContain("PlaygroundDemoElement", source, StringComparison.Ordinal);
        Assert.DoesNotContain("OnRender(RenderContext", source, StringComparison.Ordinal);
    }
}
