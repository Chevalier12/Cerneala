using Cerneala.Playground.Samples;

namespace Cerneala.Tests.Playground;

public sealed class Game1SourceTests
{
    [Fact]
    public void DefaultSampleSelectorExposesRetainedAppSample()
    {
        SampleSelector selector = SampleSelector.CreateDefault();

        Assert.Contains(selector.Samples, sample => sample.Name == "Retained App");
    }

    [Fact]
    public void Game1UsesRetainedSampleSelectorInsteadOfImmediateDemoElement()
    {
        string source = Game1Source();

        Assert.Contains("_resources = new ResourceStore()", source, StringComparison.Ordinal);
        Assert.Contains("new FontResource(_uiHost.ContentServices.LoadFont(\"Arial\", 16))", source, StringComparison.Ordinal);
        Assert.Contains("uiRoot.SetResourceProvider(_resources)", source, StringComparison.Ordinal);
        Assert.Contains("SampleSelector.CreateDefault(_resources, PlaygroundFontId, PlaygroundPreviewImageId)", source, StringComparison.Ordinal);
        Assert.Contains("uiRoot.VisualChildren.Add(_sampleSelector.Root)", source, StringComparison.Ordinal);
        Assert.Contains("host.Update(GetViewport(), gameTime.TotalGameTime)", source, StringComparison.Ordinal);
        Assert.Contains("RequireUiHost().Draw()", source, StringComparison.Ordinal);
        Assert.Contains("QueueTextInput(e.Character.ToString())", source, StringComparison.Ordinal);
        Assert.DoesNotContain("PlaygroundDemoElement", source, StringComparison.Ordinal);
        Assert.DoesNotContain("OnRender(RenderContext", source, StringComparison.Ordinal);
    }

    [Fact]
    public void Game1PrimesUiHostAfterBuildingSampleTreeBeforeFirstDraw()
    {
        string source = Game1Source();

        Assert.Contains("uiRoot.VisualChildren.Add(_sampleSelector.Root);", source, StringComparison.Ordinal);
        Assert.Contains("PrimeUiFrameForFirstDraw();", source, StringComparison.Ordinal);
        Assert.True(
            source.IndexOf("uiRoot.VisualChildren.Add(_sampleSelector.Root);", StringComparison.Ordinal) <
            source.IndexOf("PrimeUiFrameForFirstDraw();", StringComparison.Ordinal));
        Assert.Contains("private void PrimeUiFrameForFirstDraw()", source, StringComparison.Ordinal);
        Assert.Contains("RequireUiHost().Update(CreateEmptyInputFrame(), GetViewport(), TimeSpan.Zero)", source, StringComparison.Ordinal);
    }

    [Fact]
    public void Game1PublishesPreviousFrameStatsBeforeCurrentUiHostUpdate()
    {
        string source = Game1Source();

        Assert.Contains("_sampleSelector?.UpdateFrame(host.LastFrame);", source, StringComparison.Ordinal);
        Assert.Contains("host.Update(GetViewport(), gameTime.TotalGameTime);", source, StringComparison.Ordinal);
        Assert.True(
            source.IndexOf("_sampleSelector?.UpdateFrame(host.LastFrame);", StringComparison.Ordinal) <
            source.IndexOf("host.Update(GetViewport(), gameTime.TotalGameTime);", StringComparison.Ordinal));
    }

    [Fact]
    public void Game1DoesNotMutateSampleSelectorStatsAfterUiHostUpdate()
    {
        string updateBody = Game1UpdateBody();

        Assert.DoesNotContain("UiFrame frame = RequireUiHost().Update", updateBody, StringComparison.Ordinal);
        Assert.DoesNotContain("_sampleSelector?.UpdateFrame(frame)", updateBody, StringComparison.Ordinal);
    }

    [Fact]
    public void Game1SmokeModeExitsAfterFirstSuccessfulDraw()
    {
        string source = Game1Source();
        string drawBody = Game1DrawBody();

        Assert.Contains("Game1(bool exitAfterFirstSuccessfulDraw = false)", source, StringComparison.Ordinal);
        Assert.Contains("_exitAfterFirstSuccessfulDraw", source, StringComparison.Ordinal);
        Assert.Contains("_smokeDrawCompleted", source, StringComparison.Ordinal);
        Assert.Contains("Exit();", drawBody, StringComparison.Ordinal);
        Assert.True(
            drawBody.IndexOf("RequireUiHost().Draw();", StringComparison.Ordinal) <
            drawBody.IndexOf("Exit();", StringComparison.Ordinal));
    }

    [Fact]
    public void ProgramPassesSmokeOpenArgumentToGame1()
    {
        string source = File.ReadAllText(ProgramPath());

        Assert.Contains("--smoke-open", source, StringComparison.Ordinal);
        Assert.Contains("args.Contains(\"--smoke-open\", StringComparer.OrdinalIgnoreCase)", source, StringComparison.Ordinal);
        Assert.Contains("new Cerneala.Playground.Game1(", source, StringComparison.Ordinal);
    }

    private static string Game1UpdateBody()
    {
        string source = Game1Source();
        int start = source.IndexOf("protected override void Update(GameTime gameTime)", StringComparison.Ordinal);
        int end = source.IndexOf("protected override void Draw(GameTime gameTime)", StringComparison.Ordinal);
        return source[start..end];
    }

    private static string Game1DrawBody()
    {
        string source = Game1Source();
        int start = source.IndexOf("protected override void Draw(GameTime gameTime)", StringComparison.Ordinal);
        int end = source.IndexOf("private UiViewport GetViewport()", StringComparison.Ordinal);
        return source[start..end];
    }

    private static string Game1Source()
    {
        return File.ReadAllText(Game1Path());
    }

    private static string Game1Path()
    {
        return Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "Playground", "Cerneala.Playground", "Game1.cs"));
    }

    private static string ProgramPath()
    {
        return Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "Playground", "Cerneala.Playground", "Program.cs"));
    }
}
