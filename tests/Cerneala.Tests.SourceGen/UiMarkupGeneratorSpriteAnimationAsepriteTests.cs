using System.Reflection;
using Cerneala.Drawing;
using Cerneala.SourceGen;
using Cerneala.UI.Controls;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Cerneala.Tests.SourceGen;

// SpriteAnimationSet Source="...json" reads an Aseprite JSON export at build time.
// The JSON fixtures in Fixtures/Aseprite are real Aseprite 1.3.18 exports (see make-hero.lua).
// The JSON strings built in this file are hand-written: they are the invalid
// documents that a real export cannot produce.
public sealed partial class UiMarkupGeneratorTests
{
    private const string AsepriteGameHeroPath = "C:/Game/Assets/hero.json";

    private static string AsepriteFixture(string name) =>
        File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "Aseprite", name));

    private static string AsepriteMarkup(
        string setAttributes = "Name=\"HeroAnimations\" Source=\"Assets/hero.json\"",
        string setChildren = "",
        string state = "WalkUp") =>
        $$"""
        <RenderSurface2D>
          <RenderSurface2D.Resources>
            <SpriteAnimationSet {{setAttributes}}>{{setChildren}}</SpriteAnimationSet>
          </RenderSurface2D.Resources>
          <RenderSurface2D.Scene>
            <Scene2D>
              <Sprite2D Animations="$HeroAnimations" AnimationState="{{state}}" />
            </Scene2D>
          </RenderSurface2D.Scene>
        </RenderSurface2D>
        """;

    private static GeneratorRunResult RunAseprite(
        string markup,
        string? jsonText,
        out Compilation compilation,
        string jsonPath = AsepriteGameHeroPath)
    {
        List<MarkupFile> files = [new MarkupFile("C:/Game/Hero.crn", markup)];
        if (jsonText is not null)
        {
            files.Add(new MarkupFile(jsonPath, jsonText));
        }

        return RunGenerator(files.ToArray(), out compilation);
    }

    private static Sprite2D CreateAsepriteSprite(GeneratorRunResult result, Compilation compilation, out RenderSurface2D surface)
    {
        Assert.DoesNotContain(result.Diagnostics, diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        using MemoryStream stream = new();
        var emitted = compilation.Emit(stream);
        Assert.True(emitted.Success, string.Join(Environment.NewLine, emitted.Diagnostics));
        surface = Assert.IsType<RenderSurface2D>(InvokeCreate(stream, "Cerneala.GeneratedUi.HeroFactory"));
        return Assert.IsType<Sprite2D>(Assert.Single(surface.Scene!.Children));
    }

    private static SpriteAnimationSet GenerateAsepriteSet(string jsonText)
    {
        GeneratorRunResult result = RunAseprite(AsepriteMarkup(), jsonText, out Compilation compilation);
        return CreateAsepriteSprite(result, compilation, out _).Animations!;
    }

    private static (float X, float Y, float Width, float Height, double Milliseconds)[] AsepriteRects(SpriteAnimationClip clip) =>
        clip.Frames
            .Select(frame => (
                frame.SourceRect.X,
                frame.SourceRect.Y,
                frame.SourceRect.Width,
                frame.SourceRect.Height,
                frame.Duration.TotalMilliseconds))
            .ToArray();

    private static (float, float, float, float, double) AsepriteCell(int index, double milliseconds) =>
        (index * 32f, 0f, 32f, 32f, milliseconds);

    [Theory]
    [Trait("SpriteAnimationAseprite", "1")]
    [InlineData("hero-hash.json")]
    [InlineData("hero-array.json")]
    public void AsepriteHashAndArrayExportsProduceTheSameClips(string fixture)
    {
        SpriteAnimationSet set = GenerateAsepriteSet(AsepriteFixture(fixture));

        Assert.Equal(
            new[] { "Bob", "BobBack", "Once", "Spin", "WalkUp" },
            set.Clips.Select(clip => clip.Name).OrderBy(name => name, StringComparer.Ordinal));

        SpriteAnimationClip walkUp = Clip(set, "WalkUp");
        Assert.Equal(
            [(64f, 0f, 32f, 32f, 150d), (96f, 0f, 32f, 32f, 150d)],
            AsepriteRects(walkUp));
        Assert.True(walkUp.IsLooping);

        // forward 0-3 pingpong: 0,1,2,3,2,1
        SpriteAnimationClip bob = Clip(set, "Bob");
        Assert.Equal(
            [AsepriteCell(0, 100), AsepriteCell(1, 100), AsepriteCell(2, 150), AsepriteCell(3, 150), AsepriteCell(2, 150), AsepriteCell(1, 100)],
            AsepriteRects(bob));
        Assert.True(bob.IsLooping);

        // 0-2 pingpong_reverse: 2,1,0,1
        Assert.Equal(
            [AsepriteCell(2, 150), AsepriteCell(1, 100), AsepriteCell(0, 100), AsepriteCell(1, 100)],
            AsepriteRects(Clip(set, "BobBack")));

        // 0-1 reverse: 1,0
        Assert.Equal([AsepriteCell(1, 100), AsepriteCell(0, 100)], AsepriteRects(Clip(set, "Spin")));

        // 1-2 forward, repeat "1": one pass, not looping
        SpriteAnimationClip once = Clip(set, "Once");
        Assert.Equal([AsepriteCell(1, 100), AsepriteCell(2, 150)], AsepriteRects(once));
        Assert.False(once.IsLooping);

        Assert.All(set.Clips, clip => Assert.All(clip.Frames, frame =>
        {
            Assert.Equal(default, frame.Offset);
            Assert.Equal(new DrawSize(32, 32), frame.FrameSize);
        }));
    }

    [Fact]
    [Trait("SpriteAnimationAseprite", "1")]
    public void AsepriteFramesOutsideEveryTagAreIgnored()
    {
        SpriteAnimationSet set = GenerateAsepriteSet(AsepriteFixture("hero-hash.json"));

        // Frames 4 and 5 (x = 128 and 160) are untagged.
        Assert.DoesNotContain(
            set.Clips.SelectMany(clip => clip.Frames),
            frame => frame.SourceRect.X >= 128f);
    }

    [Fact]
    [Trait("SpriteAnimationAseprite", "1")]
    public void AsepriteUntrimmedFramesUseTheThreeArgumentConstructorAndTrimmedFramesTheFiveArgumentOne()
    {
        GeneratorRunResult untrimmed = RunAseprite(AsepriteMarkup(), AsepriteFixture("hero-hash.json"), out _);
        string untrimmedSource = SingleGeneratedSource(untrimmed);
        Assert.Contains(
            "new global::Cerneala.UI.Controls.SpriteAnimationFrame(new global::Cerneala.Drawing.DrawRect(64f, 0f, 32f, 32f), " +
            "global::System.TimeSpan.FromMilliseconds(150), global::Cerneala.UI.Controls.RenderSurface2DSpriteFlip.None)",
            untrimmedSource,
            StringComparison.Ordinal);
        Assert.DoesNotContain("DrawPoint", untrimmedSource, StringComparison.Ordinal);

        GeneratorRunResult trimmed = RunAseprite(AsepriteMarkup(), AsepriteFixture("hero-trimmed.json"), out _);
        Assert.Contains("global::Cerneala.Drawing.DrawPoint", SingleGeneratedSource(trimmed), StringComparison.Ordinal);
    }

    [Fact]
    [Trait("SpriteAnimationAseprite", "2")]
    public void AsepriteTrimmedFramesCarryOffsetAndLogicalFrameSize()
    {
        SpriteAnimationSet set = GenerateAsepriteSet(AsepriteFixture("hero-trimmed.json"));

        // Sprite frame 2 is not trimmed.
        SpriteAnimationFrame walkUp = Clip(set, "WalkUp").Frames[0];
        Assert.Equal(new DrawRect(38, 0, 32, 32), walkUp.SourceRect);
        Assert.Equal(new DrawPoint(0, 0), walkUp.Offset);
        Assert.Equal(new DrawSize(32, 32), walkUp.FrameSize);

        // Sprite frame 0 is trimmed to 10x12 pixels that sat at (8, 4) in the 32x32 frame.
        SpriteAnimationFrame bob = Clip(set, "Bob").Frames[0];
        Assert.Equal(new DrawRect(0, 0, 10, 12), bob.SourceRect);
        Assert.Equal(new DrawPoint(8, 4), bob.Offset);
        Assert.Equal(new DrawSize(32, 32), bob.FrameSize);
    }

    [Fact]
    [Trait("SpriteAnimationAseprite", "3")]
    public void AsepriteAnimationStateShowsTheTaggedFramesOverTime()
    {
        GeneratorRunResult result = RunAseprite(AsepriteMarkup(), AsepriteFixture("hero-hash.json"), out Compilation compilation);
        Sprite2D sprite = CreateAsepriteSprite(result, compilation, out RenderSurface2D surface);
        sprite.Image = new(new AsepriteSheetImage());
        sprite.X = 0;
        sprite.Y = 0;
        sprite.Width = 32;
        sprite.Height = 32;
        Assert.Equal("WalkUp", sprite.AnimationState);

        MethodInfo record = typeof(RenderSurface2D).GetInterface("IRenderSurface2DFrameSource")!.GetMethod("RecordFrame")!;
        MethodInfo advance = typeof(Sprite2D).GetMethod("AdvanceAnimation", BindingFlags.Instance | BindingFlags.NonPublic)!;
        DrawRect Recorded()
        {
            DrawCommandList commands = new();
            record.Invoke(surface, [commands, new DrawRect(0, 0, 64, 64)]);
            return Assert.Single(commands.Where(command => command.Kind == DrawCommandKind.DrawImage)).ImageSource!.Value;
        }

        Assert.Equal(new DrawRect(64, 0, 32, 32), Recorded());
        advance.Invoke(sprite, [TimeSpan.FromMilliseconds(150)]);
        Assert.Equal(new DrawRect(96, 0, 32, 32), Recorded());
    }

    [Fact]
    [Trait("SpriteAnimationAseprite", "4")]
    public void AsepriteClipNamesFeedStaticAnimationStateValidation()
    {
        GeneratorRunResult result = RunAseprite(AsepriteMarkup(state: "Walkup"), AsepriteFixture("hero-hash.json"), out _);

        Diagnostic diagnostic = Assert.Single(result.Diagnostics, candidate => candidate.Id == "CERNEALAUI016");
        Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity);
        Assert.Contains("Walkup", diagnostic.GetMessage(), StringComparison.Ordinal);
        Assert.Contains("HeroAnimations", diagnostic.GetMessage(), StringComparison.Ordinal);
    }

    [Fact]
    [Trait("SpriteAnimationAseprite", "5")]
    public void AsepriteSourceAcceptsVersionAndKeepsItOnTheSet()
    {
        GeneratorRunResult result = RunAseprite(
            AsepriteMarkup("Name=\"HeroAnimations\" Source=\"Assets/hero.json\" Version=\"7\""),
            AsepriteFixture("hero-hash.json"),
            out Compilation compilation);

        SpriteAnimationSet set = CreateAsepriteSprite(result, compilation, out _).Animations!;
        Assert.Equal(7L, set.Version);
    }

    [Theory]
    [Trait("SpriteAnimationAseprite", "6")]
    [InlineData("C:/Game/Assets/hero.json", "Assets/hero.json")]
    [InlineData("C:/Game/ASSETS/Hero.JSON", "assets/hero.json")]
    [InlineData("C:\\Game\\Assets\\hero.json", "Assets/hero.json")]
    [InlineData("C:/Game/Assets/hero.json", "Assets\\hero.json")]
    [InlineData("C:/Game/Assets/hero.json", "hero.json")]
    public void AsepriteSourceMatchesAtASegmentBoundaryIgnoringCaseAndSlashStyle(string additionalFilePath, string source)
    {
        GeneratorRunResult result = RunAseprite(
            AsepriteMarkup("Name=\"HeroAnimations\" Source=\"" + source + "\""),
            AsepriteFixture("hero-hash.json"),
            out Compilation compilation,
            additionalFilePath);

        Assert.Equal(5, CreateAsepriteSprite(result, compilation, out _).Animations!.Clips.Count);
    }

    [Fact]
    [Trait("SpriteAnimationAseprite", "6")]
    public void AsepriteSourceDoesNotMatchInTheMiddleOfAPathSegment()
    {
        GeneratorRunResult result = RunAseprite(
            AsepriteMarkup(),
            AsepriteFixture("hero-hash.json"),
            out _,
            "C:/Game/MyAssets/hero.json");

        Diagnostic diagnostic = Assert.Single(result.Diagnostics, candidate => candidate.Id == "CERNEALAUI016");
        Assert.Contains("AdditionalFiles", diagnostic.GetMessage(), StringComparison.Ordinal);
    }

    [Fact]
    [Trait("SpriteAnimationAseprite", "6")]
    public void AsepriteMissingAdditionalFileTellsTheUserWhatToAdd()
    {
        GeneratorRunResult result = RunAseprite(AsepriteMarkup(), jsonText: null, out _);

        Diagnostic diagnostic = Assert.Single(result.Diagnostics, candidate => candidate.Id == "CERNEALAUI016");
        Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity);
        Assert.Contains("<AdditionalFiles Include=\"Assets\\hero.json\" />", diagnostic.GetMessage(), StringComparison.Ordinal);
        Assert.Contains("HeroAnimations", diagnostic.GetMessage(), StringComparison.Ordinal);
        AssertAsepriteLocation(diagnostic, "Source=\"Assets/hero.json\"");
    }

    [Fact]
    [Trait("SpriteAnimationAseprite", "6")]
    public void AsepriteAmbiguousAdditionalFilesAreListed()
    {
        string markup = AsepriteMarkup();
        GeneratorRunResult result = RunGenerator(
            [
                new MarkupFile("C:/Game/Hero.crn", markup),
                new MarkupFile("C:/One/Assets/hero.json", AsepriteFixture("hero-hash.json")),
                new MarkupFile("C:/Two/Assets/hero.json", AsepriteFixture("hero-hash.json"))
            ],
            out _);

        Diagnostic diagnostic = Assert.Single(result.Diagnostics, candidate => candidate.Id == "CERNEALAUI016");
        Assert.Contains("more than one", diagnostic.GetMessage(), StringComparison.Ordinal);
        Assert.Contains("C:/One/Assets/hero.json", diagnostic.GetMessage(), StringComparison.Ordinal);
        Assert.Contains("C:/Two/Assets/hero.json", diagnostic.GetMessage(), StringComparison.Ordinal);
        AssertAsepriteLocation(diagnostic, "Source=\"Assets/hero.json\"");
    }

    [Theory]
    [Trait("SpriteAnimationAseprite", "6")]
    [InlineData("")]
    [InlineData("   ")]
    public void AsepriteEmptySourceIsRejected(string source)
    {
        string markup = AsepriteMarkup("Name=\"HeroAnimations\" Source=\"" + source + "\"");
        GeneratorRunResult result = RunAseprite(markup, AsepriteFixture("hero-hash.json"), out _);

        Diagnostic diagnostic = Assert.Single(result.Diagnostics, candidate => candidate.Id == "CERNEALAUI016");
        Assert.Contains("Source cannot be empty", diagnostic.GetMessage(), StringComparison.Ordinal);
        AssertAsepriteLocation(diagnostic, "Source=", markup);
    }

    [Fact]
    [Trait("SpriteAnimationAseprite", "6")]
    public void AsepriteSourceTogetherWithClipChildrenIsRejectedNotMerged()
    {
        string markup = AsepriteMarkup(
            setChildren: "<SpriteAnimationClip Name=\"Extra\"><SpriteAnimationFrame SourceX=\"0\" SourceY=\"0\" SourceWidth=\"16\" SourceHeight=\"16\" Duration=\"100ms\" /></SpriteAnimationClip>");
        GeneratorRunResult result = RunAseprite(markup, AsepriteFixture("hero-hash.json"), out _);

        Diagnostic diagnostic = Assert.Single(result.Diagnostics, candidate => candidate.Id == "CERNEALAUI016");
        Assert.Contains("cannot combine Source with SpriteAnimationClip children", diagnostic.GetMessage(), StringComparison.Ordinal);
        AssertAsepriteLocation(diagnostic, "Source=", markup);
    }

    [Fact]
    [Trait("SpriteAnimationAseprite", "6")]
    public void AsepriteRepeatOtherThanOneNamesTheTag()
    {
        GeneratorRunResult result = RunAseprite(AsepriteMarkup(), AsepriteFixture("hero-repeat3.json"), out _);

        Diagnostic diagnostic = Assert.Single(result.Diagnostics, candidate => candidate.Id == "CERNEALAUI016");
        Assert.Contains("Thrice", diagnostic.GetMessage(), StringComparison.Ordinal);
        Assert.Contains("repeat '3'", diagnostic.GetMessage(), StringComparison.Ordinal);
        Assert.Contains("hero.json", diagnostic.GetMessage(), StringComparison.Ordinal);
        AssertAsepriteLocation(diagnostic, "Source=");
    }

    private const string AsepriteFrameJson =
        """{ "frame": { "x": 0, "y": 0, "w": 32, "h": 32 }, "rotated": false, "trimmed": false, "spriteSourceSize": { "x": 0, "y": 0, "w": 32, "h": 32 }, "sourceSize": { "w": 32, "h": 32 }, "duration": 100 }""";

    private const string AsepriteIdleTagJson = """{ "name": "Idle", "from": 0, "to": 1, "direction": "forward" }""";

    private static string AsepriteDocument(string frames, string tags) =>
        "{ \"frames\": [" + frames + "], \"meta\": { \"frameTags\": [" + tags + "] } }";

    private static string AsepriteTwoFrames(string frame) => frame + ", " + AsepriteFrameJson;

    public static IEnumerable<object[]> AsepriteInvalidDocuments()
    {
        yield return ["invalid json", "{ not json", new[] { "hero.json", "not valid JSON" }];
        yield return [
            "missing frames",
            """{ "meta": { "frameTags": [ { "name": "Idle", "from": 0, "to": 0, "direction": "forward" } ] } }""",
            new[] { "hero.json", "'frames'" }];
        yield return [
            "missing frameTags",
            """{ "frames": [], "meta": { "app": "hand-written" } }""",
            new[] { "hero.json", "'meta.frameTags'" }];
        yield return [
            "no tags",
            AsepriteDocument(AsepriteTwoFrames(AsepriteFrameJson), string.Empty),
            new[] { "hero.json", "no frame tags" }];
        yield return [
            "tag to outside frames",
            AsepriteDocument(AsepriteTwoFrames(AsepriteFrameJson), """{ "name": "Idle", "from": 0, "to": 9, "direction": "forward" }"""),
            new[] { "hero.json", "'Idle'", "frames 0-9", "2 frames" }];
        yield return [
            "tag from greater than to",
            AsepriteDocument(AsepriteTwoFrames(AsepriteFrameJson), """{ "name": "Idle", "from": 1, "to": 0, "direction": "forward" }"""),
            new[] { "hero.json", "'Idle'", "from 1 is greater than to 0" }];
        yield return [
            "duplicate tag name",
            AsepriteDocument(AsepriteTwoFrames(AsepriteFrameJson), AsepriteIdleTagJson + ", " + AsepriteIdleTagJson),
            new[] { "hero.json", "'Idle'", "duplicated" }];
        yield return [
            "unknown direction",
            AsepriteDocument(AsepriteTwoFrames(AsepriteFrameJson), """{ "name": "Idle", "from": 0, "to": 1, "direction": "sideways" }"""),
            new[] { "hero.json", "'Idle'", "unknown direction 'sideways'" }];
        yield return [
            "repeat zero",
            AsepriteDocument(AsepriteTwoFrames(AsepriteFrameJson), """{ "name": "Idle", "from": 0, "to": 1, "direction": "forward", "repeat": "0" }"""),
            new[] { "hero.json", "'Idle'", "repeat '0'" }];
        yield return [
            "rotated frame",
            AsepriteDocument(
                AsepriteTwoFrames(AsepriteFrameJson.Replace("\"rotated\": false", "\"rotated\": true")),
                AsepriteIdleTagJson),
            new[] { "hero.json", "Frame 0", "rotated" }];
        yield return [
            "frame missing a number",
            AsepriteDocument(
                AsepriteTwoFrames(AsepriteFrameJson.Replace("\"w\": 32, \"h\": 32 }, \"rotated\"", "\"h\": 32 }, \"rotated\"")),
                AsepriteIdleTagJson),
            new[] { "hero.json", "Frame 0", "frame.w" }];
        yield return [
            "frame width zero",
            AsepriteDocument(
                AsepriteTwoFrames(AsepriteFrameJson.Replace("\"x\": 0, \"y\": 0, \"w\": 32, \"h\": 32 }, \"rotated\"", "\"x\": 0, \"y\": 0, \"w\": 0, \"h\": 32 }, \"rotated\"")),
                AsepriteIdleTagJson),
            new[] { "hero.json", "Frame 0", "positive frame.w and frame.h" }];
        yield return [
            "frame duration zero",
            AsepriteDocument(
                AsepriteTwoFrames(AsepriteFrameJson.Replace("\"duration\": 100", "\"duration\": 0")),
                AsepriteIdleTagJson),
            new[] { "hero.json", "Frame 0", "positive duration" }];
        yield return [
            "trimmed frame without spriteSourceSize",
            AsepriteDocument(
                AsepriteTwoFrames(AsepriteFrameJson.Replace("\"trimmed\": false", "\"trimmed\": true")
                    .Replace("\"spriteSourceSize\": { \"x\": 0, \"y\": 0, \"w\": 32, \"h\": 32 }, ", string.Empty)),
                AsepriteIdleTagJson),
            new[] { "hero.json", "Frame 0", "spriteSourceSize" }];
        yield return [
            "trimmed frame outside its logical frame",
            AsepriteDocument(
                AsepriteTwoFrames(AsepriteFrameJson.Replace("\"trimmed\": false", "\"trimmed\": true")
                    .Replace("\"spriteSourceSize\": { \"x\": 0,", "\"spriteSourceSize\": { \"x\": 30,")),
                AsepriteIdleTagJson),
            new[] { "hero.json", "Frame 0", "does not fit" }];
    }

    [Theory]
    [Trait("SpriteAnimationAseprite", "6")]
    [MemberData(nameof(AsepriteInvalidDocuments))]
    public void AsepriteInvalidDocumentsProduceLocatedDiagnosticsNamingTheFile(
        string caseName,
        string json,
        string[] expectedParts)
    {
        Assert.False(string.IsNullOrEmpty(caseName));
        GeneratorRunResult result = RunAseprite(AsepriteMarkup(), json, out _);

        Diagnostic diagnostic = Assert.Single(result.Diagnostics, candidate => candidate.Id == "CERNEALAUI016");
        Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity);
        foreach (string part in expectedParts)
        {
            Assert.Contains(part, diagnostic.GetMessage(), StringComparison.Ordinal);
        }

        AssertAsepriteLocation(diagnostic, "Source=");
    }

    [Fact]
    [Trait("SpriteAnimationAseprite", "7")]
    public void EditingTheAsepriteJsonRegeneratesTheFactory()
    {
        SyntaxTree tree = CSharpSyntaxTree.ParseText("namespace TestInput { public static class Anchor { } }");
        CSharpCompilation compilation = CSharpCompilation.Create(
            "GeneratorTests",
            [tree],
            References(),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        InMemoryAdditionalText markup = new("C:/Game/Hero.crn", AsepriteMarkup());
        InMemoryAdditionalText original = new(AsepriteGameHeroPath, AsepriteFixture("hero-hash.json"));
        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            new ISourceGenerator[] { new UiMarkupGenerator().AsSourceGenerator() },
            new AdditionalText[] { markup, original },
            parseOptions: CSharpParseOptions.Default);

        driver = driver.RunGenerators(compilation);
        string before = SingleGeneratedSource(driver.GetRunResult().Results.Single());
        Assert.DoesNotContain("DrawPoint", before, StringComparison.Ordinal);

        InMemoryAdditionalText edited = new(AsepriteGameHeroPath, AsepriteFixture("hero-trimmed.json"));
        driver = driver.ReplaceAdditionalText(original, edited).RunGenerators(compilation);
        string after = SingleGeneratedSource(driver.GetRunResult().Results.Single());

        Assert.Contains("DrawPoint", after, StringComparison.Ordinal);
    }

    private static SpriteAnimationClip Clip(SpriteAnimationSet set, string name)
    {
        Assert.True(set.TryGetClip(name, out SpriteAnimationClip? clip));
        return clip!;
    }

    private static void AssertAsepriteLocation(Diagnostic diagnostic, string expectedSpanPart, string? markup = null)
    {
        Assert.Equal(LocationKind.ExternalFile, diagnostic.Location.Kind);
        Assert.Equal("C:/Game/Hero.crn", diagnostic.Location.GetLineSpan().Path);
        Assert.True(diagnostic.Location.SourceSpan.Length > 0, "The diagnostic span is empty.");
        string located = (markup ?? AsepriteMarkup()).Substring(
            diagnostic.Location.SourceSpan.Start,
            diagnostic.Location.SourceSpan.Length);
        Assert.Contains(expectedSpanPart, located, StringComparison.Ordinal);
    }

    private sealed class AsepriteSheetImage : IDrawImage
    {
        public int Width => 192;

        public int Height => 32;
    }
}
