using Cerneala.Drawing;
using Cerneala.UI.Controls;
using Microsoft.CodeAnalysis;

namespace Cerneala.Tests.SourceGen;

// SpriteAnimationFrame Column/Row address a grid declared on the SpriteAnimationSet by
// CellWidth, CellHeight, CellMargin, and CellSpacing. Rectangles are checked by running the
// generated factory.
public sealed partial class UiMarkupGeneratorTests
{
    private const string SpriteCellGrid32 = "CellWidth=\"32\" CellHeight=\"32\"";

    private static string SpriteCellMarkup(string setAttributes, string frames) =>
        $$"""
        <RenderSurface2D>
          <RenderSurface2D.Resources>
            <SpriteAnimationSet Name="HeroAnimations" {{setAttributes}}>
              <SpriteAnimationClip Name="WalkUp">{{frames}}</SpriteAnimationClip>
            </SpriteAnimationSet>
          </RenderSurface2D.Resources>
          <RenderSurface2D.Scene>
            <Scene2D>
              <Sprite2D Animations="$HeroAnimations" />
            </Scene2D>
          </RenderSurface2D.Scene>
        </RenderSurface2D>
        """;

    private static SpriteAnimationClip GenerateSpriteCellClip(string setAttributes, string frames)
    {
        GeneratorRunResult result = RunAseprite(SpriteCellMarkup(setAttributes, frames), null, out Compilation compilation);
        return Clip(CreateAsepriteSprite(result, compilation, out _).Animations!, "WalkUp");
    }

    [Theory]
    [InlineData(SpriteCellGrid32, "Column=\"1\" Row=\"1\"", 32f, 32f, 32f, 32f)]
    [InlineData(SpriteCellGrid32, "Column=\"0\" Row=\"2\"", 0f, 64f, 32f, 32f)]
    [InlineData(SpriteCellGrid32 + " CellMargin=\"2\" CellSpacing=\"1\"", "Column=\"1\" Row=\"0\"", 35f, 2f, 32f, 32f)]
    [InlineData("CellWidth=\"16\" CellHeight=\"24\" CellMargin=\"1\"", "Column=\"2\" Row=\"1\"", 33f, 25f, 16f, 24f)]
    [InlineData("CellWidth=\"16.5\" CellHeight=\"8\"", "Column=\"2\" Row=\"3\"", 33f, 24f, 16.5f, 8f)]
    public void SpriteCellFramesResolveTheirRectangleFromTheGrid(
        string setAttributes,
        string cell,
        float x,
        float y,
        float width,
        float height)
    {
        SpriteAnimationClip clip = GenerateSpriteCellClip(
            setAttributes,
            "<SpriteAnimationFrame " + cell + " Duration=\"150ms\" />");

        SpriteAnimationFrame frame = Assert.Single(clip.Frames);
        Assert.Equal(new DrawRect(x, y, width, height), frame.SourceRect);
        Assert.Equal(default, frame.Offset);
        Assert.Equal(new DrawSize(width, height), frame.FrameSize);
    }

    [Theory]
    [InlineData("100ms", "None")]
    [InlineData("150ms", "Horizontal")]
    [InlineData("0.25s", "Vertical")]
    public void SpriteCellFramesParseDurationAndFlipLikePixelFrames(string duration, string flip)
    {
        SpriteAnimationClip clip = GenerateSpriteCellClip(
            SpriteCellGrid32,
            $"<SpriteAnimationFrame Column=\"1\" Row=\"1\" Duration=\"{duration}\" Flip=\"{flip}\" />" +
            $"<SpriteAnimationFrame SourceX=\"32\" SourceY=\"32\" SourceWidth=\"32\" SourceHeight=\"32\" Duration=\"{duration}\" Flip=\"{flip}\" />");

        SpriteAnimationFrame cell = clip.Frames[0];
        SpriteAnimationFrame pixel = clip.Frames[1];
        Assert.Equal(pixel.SourceRect, cell.SourceRect);
        Assert.Equal(pixel.Duration, cell.Duration);
        Assert.Equal(pixel.Flip, cell.Flip);
        Assert.Equal(Enum.Parse<RenderSurface2DSpriteFlip>(flip), cell.Flip);
    }

    [Fact]
    public void SpriteCellSetMayMixCellAndPixelFrames()
    {
        SpriteAnimationClip clip = GenerateSpriteCellClip(
            "CellWidth=\"32\" CellHeight=\"32\" CellMargin=\"2\" CellSpacing=\"1\"",
            "<SpriteAnimationFrame Column=\"0\" Row=\"1\" Duration=\"150ms\" />" +
            "<SpriteAnimationFrame Column=\"1\" Row=\"1\" Duration=\"150ms\" Flip=\"Horizontal\" />" +
            "<SpriteAnimationFrame SourceX=\"64\" SourceY=\"0\" SourceWidth=\"48\" SourceHeight=\"32\" Duration=\"90ms\" />");

        Assert.Equal(
            [new DrawRect(2, 35, 32, 32), new DrawRect(35, 35, 32, 32), new DrawRect(64, 0, 48, 32)],
            clip.Frames.Select(frame => frame.SourceRect));
        Assert.Equal(
            [RenderSurface2DSpriteFlip.None, RenderSurface2DSpriteFlip.Horizontal, RenderSurface2DSpriteFlip.None],
            clip.Frames.Select(frame => frame.Flip));
        Assert.Equal(
            [150d, 150d, 90d],
            clip.Frames.Select(frame => frame.Duration.TotalMilliseconds));
    }

    [Fact]
    public void SpriteCellFramesEmitFromCellAndPixelFramesKeepTheirExactOutput()
    {
        GeneratorRunResult result = RunAseprite(
            SpriteCellMarkup(
                SpriteCellGrid32 + " CellMargin=\"2\" CellSpacing=\"1\"",
                "<SpriteAnimationFrame Column=\"1\" Row=\"0\" Duration=\"150ms\" Flip=\"Horizontal\" />" +
                "<SpriteAnimationFrame SourceX=\"64\" SourceY=\"0\" SourceWidth=\"48\" SourceHeight=\"32\" Duration=\"90ms\" />"),
            null,
            out _);

        string source = SingleGeneratedSource(result);
        Assert.Contains(
            "global::Cerneala.UI.Controls.SpriteAnimationFrame.FromCell(1, 0, 32f, 32f, " +
            "global::System.TimeSpan.FromMilliseconds(150), global::Cerneala.UI.Controls.RenderSurface2DSpriteFlip.Horizontal, 2f, 1f)",
            source,
            StringComparison.Ordinal);
        Assert.Contains(
            "new global::Cerneala.UI.Controls.SpriteAnimationFrame(new global::Cerneala.Drawing.DrawRect(64f, 0f, 48f, 32f), " +
            "global::System.TimeSpan.FromMilliseconds(90), global::Cerneala.UI.Controls.RenderSurface2DSpriteFlip.None)",
            source,
            StringComparison.Ordinal);
    }

    [Fact]
    public void SpriteCellPixelOnlySetsKeepTheirExactOutput()
    {
        GeneratorRunResult result = RunAseprite(
            SpriteCellMarkup(
                "Version=\"3\"",
                "<SpriteAnimationFrame SourceX=\"16\" SourceY=\"0\" SourceWidth=\"16\" SourceHeight=\"16\" Duration=\"100ms\" />"),
            null,
            out _);

        Assert.Contains(
            "new global::Cerneala.UI.Controls.SpriteAnimationClip(\"WalkUp\", new global::Cerneala.UI.Controls.SpriteAnimationFrame[] { " +
            "new global::Cerneala.UI.Controls.SpriteAnimationFrame(new global::Cerneala.Drawing.DrawRect(16f, 0f, 16f, 16f), " +
            "global::System.TimeSpan.FromMilliseconds(100), global::Cerneala.UI.Controls.RenderSurface2DSpriteFlip.None) }, true, 1L) }, 3L)",
            SingleGeneratedSource(result),
            StringComparison.Ordinal);
    }

    private const string SpriteCellFrame = "<SpriteAnimationFrame Column=\"0\" Row=\"0\" Duration=\"100ms\" />";
    private const string SpriteCellPixelFrame =
        "<SpriteAnimationFrame SourceX=\"0\" SourceY=\"0\" SourceWidth=\"16\" SourceHeight=\"16\" Duration=\"100ms\" />";

    public static IEnumerable<object[]> SpriteCellInvalidDefinitions()
    {
        // case name, set attributes, frames, message parts, text the diagnostic span must contain
        yield return ["cell frame without grid", "", SpriteCellFrame, new[] { "CellWidth and CellHeight" }, "Column"];
        yield return ["cell width only", "CellWidth=\"32\"", SpriteCellPixelFrame, new[] { "CellWidth requires CellHeight" }, "CellWidth"];
        yield return ["cell height only", "CellHeight=\"32\"", SpriteCellPixelFrame, new[] { "CellHeight requires CellWidth" }, "CellHeight"];
        yield return [
            "margin without cell size", "CellMargin=\"1\"", SpriteCellPixelFrame,
            new[] { "CellMargin requires CellWidth and CellHeight" }, "CellMargin"];
        yield return [
            "spacing without cell size", "CellSpacing=\"1\"", SpriteCellPixelFrame,
            new[] { "CellSpacing requires CellWidth and CellHeight" }, "CellSpacing"];
        yield return [
            "column without row", SpriteCellGrid32, "<SpriteAnimationFrame Column=\"0\" Duration=\"100ms\" />",
            new[] { "Column requires Row" }, "Column"];
        yield return [
            "row without column", SpriteCellGrid32, "<SpriteAnimationFrame Row=\"0\" Duration=\"100ms\" />",
            new[] { "Row requires Column" }, "Row"];
        yield return [
            "fractional column", SpriteCellGrid32, "<SpriteAnimationFrame Column=\"1.5\" Row=\"0\" Duration=\"100ms\" />",
            new[] { "Column must be a non-negative integer" }, "Column="];
        yield return [
            "negative column", SpriteCellGrid32, "<SpriteAnimationFrame Column=\"-1\" Row=\"0\" Duration=\"100ms\" />",
            new[] { "Column must be a non-negative integer" }, "Column="];
        yield return [
            "non numeric column", SpriteCellGrid32, "<SpriteAnimationFrame Column=\"a\" Row=\"0\" Duration=\"100ms\" />",
            new[] { "Column must be a non-negative integer" }, "Column="];
        yield return [
            "fractional row", SpriteCellGrid32, "<SpriteAnimationFrame Column=\"0\" Row=\"2.5\" Duration=\"100ms\" />",
            new[] { "Row must be a non-negative integer" }, "Row="];
        yield return [
            "negative row", SpriteCellGrid32, "<SpriteAnimationFrame Column=\"0\" Row=\"-1\" Duration=\"100ms\" />",
            new[] { "Row must be a non-negative integer" }, "Row="];
        yield return [
            "non numeric row", SpriteCellGrid32, "<SpriteAnimationFrame Column=\"0\" Row=\"b\" Duration=\"100ms\" />",
            new[] { "Row must be a non-negative integer" }, "Row="];
        yield return [
            "zero cell width", "CellWidth=\"0\" CellHeight=\"32\"", SpriteCellFrame,
            new[] { "CellWidth must be a finite number greater than zero" }, "CellWidth="];
        yield return [
            "negative cell width", "CellWidth=\"-4\" CellHeight=\"32\"", SpriteCellFrame,
            new[] { "CellWidth must be a finite number greater than zero" }, "CellWidth="];
        yield return [
            "non numeric cell width", "CellWidth=\"wide\" CellHeight=\"32\"", SpriteCellFrame,
            new[] { "CellWidth must be a finite number greater than zero" }, "CellWidth="];
        yield return [
            "zero cell height", "CellWidth=\"32\" CellHeight=\"0\"", SpriteCellFrame,
            new[] { "CellHeight must be a finite number greater than zero" }, "CellHeight="];
        yield return [
            "negative cell height", "CellWidth=\"32\" CellHeight=\"-1\"", SpriteCellFrame,
            new[] { "CellHeight must be a finite number greater than zero" }, "CellHeight="];
        yield return [
            "non numeric cell height", "CellWidth=\"32\" CellHeight=\"tall\"", SpriteCellFrame,
            new[] { "CellHeight must be a finite number greater than zero" }, "CellHeight="];
        yield return [
            "negative margin", SpriteCellGrid32 + " CellMargin=\"-1\"", SpriteCellFrame,
            new[] { "CellMargin must be a finite number of zero or more" }, "CellMargin="];
        yield return [
            "non numeric margin", SpriteCellGrid32 + " CellMargin=\"big\"", SpriteCellFrame,
            new[] { "CellMargin must be a finite number of zero or more" }, "CellMargin="];
        yield return [
            "negative spacing", SpriteCellGrid32 + " CellSpacing=\"-0.5\"", SpriteCellFrame,
            new[] { "CellSpacing must be a finite number of zero or more" }, "CellSpacing="];
        yield return [
            "non numeric spacing", SpriteCellGrid32 + " CellSpacing=\"x\"", SpriteCellFrame,
            new[] { "CellSpacing must be a finite number of zero or more" }, "CellSpacing="];
        yield return [
            "column row with SourceX", SpriteCellGrid32,
            "<SpriteAnimationFrame Column=\"0\" Row=\"0\" SourceX=\"4\" Duration=\"100ms\" />",
            new[] { "cannot combine Column and Row with SourceX" }, "SourceX"];
        yield return [
            "column row with SourceHeight", SpriteCellGrid32,
            "<SpriteAnimationFrame Column=\"0\" Row=\"0\" SourceHeight=\"8\" Duration=\"100ms\" />",
            new[] { "cannot combine Column and Row with SourceHeight" }, "SourceHeight"];
        yield return [
            "frame with neither form in a cell set", SpriteCellGrid32, "<SpriteAnimationFrame Duration=\"100ms\" />",
            new[] { "needs Column and Row (or SourceX, SourceY, SourceWidth, and SourceHeight)" }, "SpriteAnimationFrame"];
        yield return [
            "partial pixel frame in a cell set", SpriteCellGrid32,
            "<SpriteAnimationFrame SourceX=\"0\" Duration=\"100ms\" />",
            new[] { "SpriteAnimationFrame requires SourceY" }, "SpriteAnimationFrame"];
        yield return [
            "frame with neither form without a grid", "", "<SpriteAnimationFrame Duration=\"100ms\" />",
            new[] { "SpriteAnimationFrame requires SourceX" }, "SpriteAnimationFrame"];
        yield return [
            "cell frame without duration", SpriteCellGrid32, "<SpriteAnimationFrame Column=\"0\" Row=\"0\" />",
            new[] { "SpriteAnimationFrame requires Duration" }, "SpriteAnimationFrame"];
        yield return [
            "cell frame with bad duration", SpriteCellGrid32, "<SpriteAnimationFrame Column=\"0\" Row=\"0\" Duration=\"0ms\" />",
            new[] { "Frame duration must be positive" }, "Duration"];
        yield return [
            "cell frame with bad flip", SpriteCellGrid32,
            "<SpriteAnimationFrame Column=\"0\" Row=\"0\" Duration=\"10ms\" Flip=\"Sideways\" />",
            new[] { "Frame Flip must be None, Horizontal, or Vertical" }, "Flip"];
    }

    [Theory]
    [MemberData(nameof(SpriteCellInvalidDefinitions))]
    public void SpriteCellInvalidDefinitionsProduceLocatedDiagnostics(
        string caseName,
        string setAttributes,
        string frames,
        string[] expectedParts,
        string expectedSpanPart)
    {
        Assert.False(string.IsNullOrEmpty(caseName));
        string markup = SpriteCellMarkup(setAttributes, frames);
        GeneratorRunResult result = RunAseprite(markup, null, out _);

        Diagnostic diagnostic = Assert.Single(result.Diagnostics, candidate => candidate.Id == "CERNEALAUI016");
        Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity);
        foreach (string part in expectedParts)
        {
            Assert.Contains(part, diagnostic.GetMessage(), StringComparison.Ordinal);
        }

        AssertAsepriteLocation(diagnostic, expectedSpanPart, markup);
    }

    [Theory]
    [InlineData("CellWidth=\"32\"", "CellWidth")]
    [InlineData("CellHeight=\"32\"", "CellHeight")]
    [InlineData("CellMargin=\"1\"", "CellMargin")]
    [InlineData("CellSpacing=\"1\"", "CellSpacing")]
    public void SpriteCellSourceTogetherWithACellAttributeIsRejected(string cellAttribute, string attributeName)
    {
        string markup = $$"""
            <RenderSurface2D>
              <RenderSurface2D.Resources>
                <SpriteAnimationSet Name="HeroAnimations" Source="Assets/hero.json" {{cellAttribute}} />
              </RenderSurface2D.Resources>
              <RenderSurface2D.Scene>
                <Scene2D><Sprite2D Animations="$HeroAnimations" /></Scene2D>
              </RenderSurface2D.Scene>
            </RenderSurface2D>
            """;
        GeneratorRunResult result = RunAseprite(markup, AsepriteFixture("hero-hash.json"), out _);

        Diagnostic diagnostic = Assert.Single(result.Diagnostics, candidate => candidate.Id == "CERNEALAUI016");
        Assert.Contains("cannot combine Source with " + attributeName, diagnostic.GetMessage(), StringComparison.Ordinal);
        AssertAsepriteLocation(diagnostic, attributeName + "=", markup);
    }

    [Fact]
    public void SpriteCellUnsupportedSetAttributeMessageListsTheCellAttributes()
    {
        string markup = SpriteCellMarkup("Bogus=\"1\"", SpriteCellPixelFrame);
        GeneratorRunResult result = RunAseprite(markup, null, out _);

        Diagnostic diagnostic = Assert.Single(result.Diagnostics, candidate => candidate.Id == "CERNEALAUI016");
        Assert.Contains(
            "supports only Name, Version, Source, CellWidth, CellHeight, CellMargin, and CellSpacing attributes",
            diagnostic.GetMessage(),
            StringComparison.Ordinal);
    }
}
