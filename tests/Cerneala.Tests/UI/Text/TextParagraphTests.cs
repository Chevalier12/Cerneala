using Cerneala.Drawing;
using Cerneala.UI.Layout;
using Cerneala.UI.Media;
using Cerneala.UI.Resources;
using Cerneala.UI.Text;

namespace Cerneala.Tests.UI.Text;

// Issue #683: TextMeasurer.MeasureParagraph, TextRenderer.RenderParagraph and the font identity of styled faces.
// RecordingFontSource returns fonts that are not Skia fonts: the engine measures 5 px per character at size 10 and a
// line is 12 px high with the baseline at 9.6 px.
public sealed class TextParagraphTests
{
    private const float Tolerance = 0.01f;

    private static readonly LayoutSize Unbounded = new(float.PositiveInfinity, float.PositiveInfinity);

    private readonly RecordingFontSource fontSource = new();
    private readonly TextLayoutCache cache = new();
    private readonly SolidColorBrush black = new(Color.Black);

    private TextMeasurer CreateMeasurer() => new(new FontResolver(fontSource), LineBreakService.Default, cache);

    private TextRenderer CreateRenderer(TextMeasurer measurer) => new(new FontResolver(fontSource), measurer);

    private TextAspect Aspect(
        TextWrapping wrapping = TextWrapping.NoWrap,
        TextTrimming trimming = TextTrimming.None,
        FontWeight weight = FontWeight.Normal,
        FontStyle style = FontStyle.Normal,
        Brush? foreground = null)
    {
        return new TextAspect(
            "Test",
            10,
            wrapping,
            trimming,
            foreground: foreground ?? black,
            fontWeight: weight,
            fontStyle: style);
    }

    // MeasureParagraph

    [Fact]
    public void MeasureParagraphReportsTheWidestLineAndTheLayoutHeight()
    {
        TextMeasurer measurer = CreateMeasurer();
        TextAspect aspect = Aspect();

        TextParagraphLayout paragraph = measurer.MeasureParagraph(
            [new TextSpan("abcd\nef", aspect)],
            aspect,
            new TextParagraphOptions(),
            Unbounded);

        Assert.Equal(20, paragraph.Size.Width, Tolerance);
        Assert.Equal(24, paragraph.Size.Height, Tolerance);
        Assert.Equal(2, paragraph.LineCount);
        Assert.Equal(2, paragraph.Layout.Lines.Count);
    }

    [Fact]
    public void MeasuredWidthIsTheLineContentWidthAndNotTheLayoutWidth()
    {
        TextMeasurer measurer = CreateMeasurer();
        TextAspect aspect = Aspect(TextWrapping.Wrap);

        TextParagraphLayout paragraph = measurer.MeasureParagraph(
            [new TextSpan("abcd", aspect)],
            aspect,
            new TextParagraphOptions(TextAlignment.Center),
            new LayoutSize(100, float.PositiveInfinity));

        Assert.Equal(100, paragraph.Layout.Bounds.Width, Tolerance);
        Assert.Equal(20, paragraph.Size.Width, Tolerance);
    }

    [Fact]
    public void MeasureParagraphWrapsOnlyWhenTheParagraphAspectWraps()
    {
        TextMeasurer measurer = CreateMeasurer();
        TextAspect noWrap = Aspect();
        TextAspect wrap = Aspect(TextWrapping.Wrap);
        TextSpan[] spans = [new TextSpan("aaa bbb ccc", noWrap)];

        TextParagraphLayout single = measurer.MeasureParagraph(spans, noWrap, new TextParagraphOptions(), new LayoutSize(20, float.PositiveInfinity));
        TextParagraphLayout wrapped = measurer.MeasureParagraph(spans, wrap, new TextParagraphOptions(), new LayoutSize(20, float.PositiveInfinity));

        Assert.Equal(1, single.LineCount);
        Assert.Equal(3, wrapped.LineCount);
    }

    [Fact]
    public void MeasureParagraphWrapWithOverflowKeepsTheLongWordWhole()
    {
        TextMeasurer measurer = CreateMeasurer();
        TextAspect overflow = Aspect(TextWrapping.WrapWithOverflow);
        TextAspect wrap = Aspect(TextWrapping.Wrap);
        TextSpan[] spans = [new TextSpan("abcdefghij kl", overflow)];

        TextParagraphLayout kept = measurer.MeasureParagraph(spans, overflow, new TextParagraphOptions(), new LayoutSize(20, float.PositiveInfinity));
        TextParagraphLayout broken = measurer.MeasureParagraph(spans, wrap, new TextParagraphOptions(), new LayoutSize(20, float.PositiveInfinity));

        Assert.Equal(["abcdefghij", "kl"], kept.Layout.Lines.Select(static line => line.Text));
        Assert.Equal(4, broken.LineCount);
        Assert.Equal(50, kept.Size.Width, Tolerance);
    }

    [Fact]
    public void MeasureParagraphAppliesMaxLinesLineHeightAndStacking()
    {
        TextMeasurer measurer = CreateMeasurer();
        TextAspect aspect = Aspect();
        TextSpan[] spans = [new TextSpan("a\nb\nc\nd", aspect)];

        TextParagraphLayout limited = measurer.MeasureParagraph(spans, aspect, new TextParagraphOptions(MaxLines: 2), Unbounded);
        TextParagraphLayout block = measurer.MeasureParagraph(
            spans,
            aspect,
            new TextParagraphOptions(LineHeight: 30, LineStackingStrategy: LineStackingStrategy.BlockLineHeight),
            Unbounded);
        TextParagraphLayout max = measurer.MeasureParagraph(spans, aspect, new TextParagraphOptions(LineHeight: 5), Unbounded);

        Assert.Equal(24, limited.Size.Height, Tolerance);
        Assert.Equal(2, limited.LineCount);
        Assert.Equal(120, block.Size.Height, Tolerance);
        Assert.Equal(48, max.Size.Height, Tolerance);
    }

    // Issue #963: "aa bb" | "cc dd" | "ee" at width 30. "cc dd" ends neither the text nor a hard line break, so Justify
    // stretches it also when MaxLines hides "ee".
    [Theory]
    [InlineData(0)]
    [InlineData(2)]
    public void JustifyStretchesTheLastVisibleLineWhenMaxLinesHidesLaterSoftWrappedLines(int maxLines)
    {
        TextMeasurer measurer = CreateMeasurer();
        TextAspect aspect = Aspect(TextWrapping.Wrap);

        TextParagraphLayout paragraph = measurer.MeasureParagraph(
            [new TextSpan("aa bb cc dd ee", aspect)],
            aspect,
            new TextParagraphOptions(TextAlignment.Justify, MaxLines: maxLines),
            new LayoutSize(30, float.PositiveInfinity));

        Assert.True(
            paragraph.Layout.Lines.Count >= 2 && Math.Abs(paragraph.Layout.Lines[1].Bounds.Width - 30) < Tolerance,
            $"MaxLines={maxLines}: lines [{string.Join("|", paragraph.Layout.Lines.Select(line => line.Text))}] widths [{string.Join(", ", paragraph.Layout.Lines.Select(line => line.Bounds.Width))}] (expected line 1 width 30).");
    }

    // Issue #963: a line that ends before a hard line break stays unstretched when MaxLines makes it the last visible line.
    [Fact]
    public void JustifyKeepsTheLastVisibleLineUnstretchedWhenItEndsBeforeAHardLineBreak()
    {
        TextMeasurer measurer = CreateMeasurer();
        TextAspect aspect = Aspect(TextWrapping.Wrap);

        TextParagraphLayout paragraph = measurer.MeasureParagraph(
            [new TextSpan("aa bb cc dd\nee", aspect)],
            aspect,
            new TextParagraphOptions(TextAlignment.Justify, MaxLines: 2),
            new LayoutSize(30, float.PositiveInfinity));

        Assert.Equal(["aa bb", "cc dd"], paragraph.Layout.Lines.Select(static line => line.Text));
        Assert.Equal(30, paragraph.Layout.Lines[0].Bounds.Width, Tolerance);
        Assert.Equal(25, paragraph.Layout.Lines[1].Bounds.Width, Tolerance);
    }

    [Fact]
    public void MeasureParagraphFitsTrimmedTextIntoTheAvailableHeight()
    {
        TextMeasurer measurer = CreateMeasurer();
        TextAspect aspect = Aspect(TextWrapping.Wrap, TextTrimming.CharacterEllipsis);
        TextSpan[] spans = [new TextSpan("aaa bbb ccc ddd", aspect)];

        TextParagraphLayout paragraph = measurer.MeasureParagraph(spans, aspect, new TextParagraphOptions(), new LayoutSize(20, 25));

        Assert.Equal(2, paragraph.LineCount);
        Assert.Equal(24, paragraph.Size.Height, Tolerance);
        Assert.True(paragraph.Layout.Lines[^1].IsTrimmed);
    }

    [Fact]
    public void MeasureParagraphAlwaysShowsTheFirstLineOfTrimmedText()
    {
        TextMeasurer measurer = CreateMeasurer();
        TextAspect aspect = Aspect(TextWrapping.Wrap, TextTrimming.CharacterEllipsis);
        TextSpan[] spans = [new TextSpan("aaa bbb ccc ddd", aspect)];

        TextParagraphLayout paragraph = measurer.MeasureParagraph(spans, aspect, new TextParagraphOptions(), new LayoutSize(20, 5));

        Assert.Equal(1, paragraph.LineCount);
        Assert.Equal(12, paragraph.Size.Height, Tolerance);
    }

    [Fact]
    public void MeasureParagraphIgnoresTheAvailableHeightWithoutTrimming()
    {
        TextMeasurer measurer = CreateMeasurer();
        TextAspect aspect = Aspect(TextWrapping.Wrap);
        TextSpan[] spans = [new TextSpan("aaa bbb ccc", aspect)];

        TextParagraphLayout tall = measurer.MeasureParagraph(spans, aspect, new TextParagraphOptions(), new LayoutSize(20, 1000));
        TextParagraphLayout flat = measurer.MeasureParagraph(spans, aspect, new TextParagraphOptions(), new LayoutSize(20, 5));

        Assert.Same(tall, flat);
        Assert.Equal(3, flat.LineCount);
    }

    [Fact]
    public void MeasureParagraphOfNoSpansIsEmpty()
    {
        TextMeasurer measurer = CreateMeasurer();

        TextParagraphLayout paragraph = measurer.MeasureParagraph([], Aspect(), new TextParagraphOptions(), Unbounded);

        Assert.Equal(LayoutSize.Zero, paragraph.Size);
        Assert.Equal(0, paragraph.LineCount);
    }

    [Fact]
    public void MeasureParagraphOfAnEmptySpanIsOneLine()
    {
        TextMeasurer measurer = CreateMeasurer();
        TextAspect aspect = Aspect();

        TextParagraphLayout paragraph = measurer.MeasureParagraph([new TextSpan(string.Empty, aspect)], aspect, new TextParagraphOptions(), Unbounded);

        Assert.Equal(0, paragraph.Size.Width);
        Assert.Equal(12, paragraph.Size.Height, Tolerance);
        Assert.Equal(1, paragraph.LineCount);
    }

    [Fact]
    public void MeasureParagraphUsesTheFontOfEachSpan()
    {
        TextMeasurer measurer = CreateMeasurer();
        TextAspect regular = Aspect();
        TextAspect bold = Aspect(weight: FontWeight.Bold);

        measurer.MeasureParagraph([new TextSpan("a", regular), new TextSpan("b", bold)], regular, new TextParagraphOptions(), Unbounded);

        // Only the bold span needs a styled face; the regular span loads through the two-argument overload.
        Assert.Equal([(FontWeight.Bold, FontStyle.Normal, FontStretch.Normal)], fontSource.StyledLoads);
    }

    // The paragraph cache

    [Fact]
    public void ParagraphLayoutIsCachedByContentFontAndOptions()
    {
        TextMeasurer measurer = CreateMeasurer();
        TextAspect aspect = Aspect(TextWrapping.Wrap);
        TextSpan[] spans = [new TextSpan("aaa bbb", aspect)];
        LayoutSize available = new(20, float.PositiveInfinity);

        TextParagraphLayout first = measurer.MeasureParagraph(spans, aspect, new TextParagraphOptions(), available);
        TextParagraphLayout second = measurer.MeasureParagraph(spans, aspect, new TextParagraphOptions(), available);

        Assert.Same(first, second);
        Assert.Equal(1, cache.Misses);
        Assert.Equal(1, cache.Hits);
    }

    [Fact]
    public void ForegroundAndDecorationsDoNotChangeTheCachedLayout()
    {
        TextMeasurer measurer = CreateMeasurer();
        TextAspect black = Aspect();
        TextAspect red = Aspect(foreground: new SolidColorBrush(Color.Red));

        TextParagraphLayout first = measurer.MeasureParagraph([new TextSpan("abc", black)], black, new TextParagraphOptions(), Unbounded);
        TextParagraphLayout second = measurer.MeasureParagraph(
            [new TextSpan("abc", red, TextDecorations.Underline)],
            red,
            new TextParagraphOptions(),
            Unbounded);

        Assert.Same(first, second);
        Assert.Equal(1, cache.Misses);
    }

    [Fact]
    public void WeightStyleAndSpanBoundariesChangeTheCachedLayout()
    {
        TextMeasurer measurer = CreateMeasurer();
        TextAspect regular = Aspect();
        TextAspect bold = Aspect(weight: FontWeight.Bold);
        TextAspect italic = Aspect(style: FontStyle.Italic);

        TextParagraphLayout a = measurer.MeasureParagraph([new TextSpan("abc", regular)], regular, new TextParagraphOptions(), Unbounded);
        TextParagraphLayout b = measurer.MeasureParagraph([new TextSpan("abc", bold)], regular, new TextParagraphOptions(), Unbounded);
        TextParagraphLayout c = measurer.MeasureParagraph([new TextSpan("abc", italic)], regular, new TextParagraphOptions(), Unbounded);
        TextParagraphLayout d = measurer.MeasureParagraph([new TextSpan("ab", regular), new TextSpan("c", regular)], regular, new TextParagraphOptions(), Unbounded);

        Assert.Equal(4, cache.Misses);
        Assert.Equal(4, new[] { a, b, c, d }.Distinct().Count());
        Assert.Equal(4, new[] { a, b, c, d }.Select(static paragraph => paragraph.RenderIdentity).Distinct().Count());
    }

    [Fact]
    public void OptionsAndWidthChangeTheCachedLayoutButAnUnwrappedLeftAlignedParagraphIgnoresTheWidth()
    {
        TextMeasurer measurer = CreateMeasurer();
        TextAspect aspect = Aspect();
        TextSpan[] spans = [new TextSpan("abc", aspect)];

        TextParagraphLayout narrow = measurer.MeasureParagraph(spans, aspect, new TextParagraphOptions(), new LayoutSize(10, 10));
        TextParagraphLayout wide = measurer.MeasureParagraph(spans, aspect, new TextParagraphOptions(), new LayoutSize(500, 10));
        TextParagraphLayout centeredNarrow = measurer.MeasureParagraph(spans, aspect, new TextParagraphOptions(TextAlignment.Center), new LayoutSize(100, 10));
        TextParagraphLayout centeredWide = measurer.MeasureParagraph(spans, aspect, new TextParagraphOptions(TextAlignment.Center), new LayoutSize(200, 10));

        Assert.Same(narrow, wide);
        Assert.NotSame(centeredNarrow, centeredWide);
        Assert.NotSame(narrow, centeredNarrow);
        Assert.Equal(3, cache.Misses);
    }

    [Fact]
    public void ParagraphCacheIsBoundedAndClearedWithTheCache()
    {
        TextLayoutCache small = new(capacity: 2);
        TextMeasurer measurer = new(new FontResolver(fontSource), LineBreakService.Default, small);
        TextAspect aspect = Aspect();

        TextParagraphLayout first = measurer.MeasureParagraph([new TextSpan("one", aspect)], aspect, new TextParagraphOptions(), Unbounded);
        measurer.MeasureParagraph([new TextSpan("two", aspect)], aspect, new TextParagraphOptions(), Unbounded);
        measurer.MeasureParagraph([new TextSpan("three", aspect)], aspect, new TextParagraphOptions(), Unbounded);
        TextParagraphLayout firstAgain = measurer.MeasureParagraph([new TextSpan("one", aspect)], aspect, new TextParagraphOptions(), Unbounded);

        Assert.NotSame(first, firstAgain);
        Assert.Equal(2, small.Count);
        small.Clear();
        Assert.Equal(0, small.Count);
        Assert.Equal(0, small.Misses);
    }

    // RenderParagraph

    [Fact]
    public void RenderParagraphDrawsOneTextCommandPerRunWithTheBrushOfItsSpan()
    {
        SolidColorBrush red = new(Color.Red);
        TextMeasurer measurer = CreateMeasurer();
        TextRenderer renderer = CreateRenderer(measurer);
        TextAspect plain = Aspect();
        TextAspect marked = Aspect(foreground: red);
        DrawCommandList commands = new();

        TextParagraphLayout paragraph = renderer.RenderParagraph(
            new DrawingContext(commands),
            [new TextSpan("ab", plain), new TextSpan("cd", marked)],
            plain,
            new TextParagraphOptions(),
            Unbounded,
            new DrawPoint(2, 3));

        Assert.Equal(2, commands.Count);
        Assert.All(commands, static command => Assert.Equal(DrawCommandKind.DrawText, command.Kind));
        Assert.Equal("ab", commands[0].TextRun!.Text);
        Assert.Equal("cd", commands[1].TextRun!.Text);
        Assert.Same(black, commands[0].Brush);
        Assert.Same(red, commands[1].Brush);
        Assert.Equal(2, commands[0].Position.X, Tolerance);
        Assert.Equal(12, commands[1].Position.X, Tolerance);
        Assert.Equal(3 + 9.6f, commands[0].Position.Y, Tolerance);
        Assert.Equal(1, paragraph.LineCount);
    }

    [Fact]
    public void RenderParagraphSkipsSpansWithoutForeground()
    {
        TextRenderer renderer = CreateRenderer(CreateMeasurer());
        TextAspect plain = Aspect();
        TextAspect hidden = new("Test", 10, foreground: null);
        DrawCommandList commands = new();

        renderer.RenderParagraph(
            new DrawingContext(commands),
            [new TextSpan("ab", plain), new TextSpan("cd", hidden, TextDecorations.Underline)],
            plain,
            new TextParagraphOptions(),
            Unbounded,
            default);

        DrawCommand only = Assert.Single(commands);
        Assert.Equal("ab", only.TextRun!.Text);
    }

    [Fact]
    public void RenderParagraphOffsetsLinesByThePositionAndAlignment()
    {
        TextRenderer renderer = CreateRenderer(CreateMeasurer());
        TextAspect aspect = Aspect(TextWrapping.Wrap);
        DrawCommandList commands = new();

        renderer.RenderParagraph(
            new DrawingContext(commands),
            [new TextSpan("ab\ncd", aspect)],
            aspect,
            new TextParagraphOptions(TextAlignment.Right),
            new LayoutSize(50, float.PositiveInfinity),
            new DrawPoint(100, 200));

        Assert.Equal(2, commands.Count);
        Assert.Equal(140, commands[0].Position.X, Tolerance);
        Assert.Equal(140, commands[1].Position.X, Tolerance);
        Assert.Equal(200 + 9.6f, commands[0].Position.Y, Tolerance);
        Assert.Equal(200 + 12 + 9.6f, commands[1].Position.Y, Tolerance);
    }

    [Fact]
    public void RenderParagraphDrawsDecorationRectanglesInTheBrushOfTheSpan()
    {
        TextRenderer renderer = CreateRenderer(CreateMeasurer());
        TextAspect aspect = Aspect();
        DrawCommandList commands = new();

        renderer.RenderParagraph(
            new DrawingContext(commands),
            [new TextSpan("abcd", aspect, TextDecorations.Underline | TextDecorations.Strikethrough | TextDecorations.Overline | TextDecorations.Baseline)],
            aspect,
            new TextParagraphOptions(),
            Unbounded,
            new DrawPoint(2, 3));

        Assert.Equal(5, commands.Count);
        Assert.Equal(DrawCommandKind.DrawText, commands[0].Kind);
        DrawCommand underline = commands[1];
        DrawCommand strike = commands[2];
        DrawCommand overline = commands[3];
        DrawCommand baseline = commands[4];
        Assert.All(new[] { underline, strike, overline, baseline }, command =>
        {
            Assert.Equal(DrawCommandKind.FillRectangle, command.Kind);
            Assert.Same(black, command.Brush);
            Assert.Equal(2, command.Rect.X, Tolerance);
            Assert.Equal(20, command.Rect.Width, Tolerance);
            Assert.Equal(1, command.Rect.Height, Tolerance);
        });
        float baselineY = 3 + 9.6f;
        Assert.Equal(baselineY + 1, underline.Rect.Y, Tolerance);
        // The strikethrough offset is the distance from the baseline to the bottom of the stroke.
        Assert.Equal(baselineY - 3, strike.Rect.Bottom, Tolerance);
        Assert.Equal(3, overline.Rect.Y, Tolerance);
        Assert.Equal(baselineY, baseline.Rect.Y, Tolerance);
    }

    [Fact]
    public void RenderParagraphUsesTheInjectedMeasurer()
    {
        CountingMeasurer measurer = new(fontSource);
        TextRenderer renderer = CreateRenderer(measurer);
        TextAspect aspect = Aspect();

        renderer.RenderParagraph(new DrawingContext(new DrawCommandList()), [new TextSpan("ab", aspect)], aspect, new TextParagraphOptions(), Unbounded, default);

        Assert.Equal(1, measurer.Calls);
    }

    // Font identity

    [Fact]
    public void FontIdentityOfANormalFaceIsUnchanged()
    {
        FontResolver resolver = new(fontSource);

        ResolvedTextFont plain = resolver.Resolve(new TextAspect("Test", 13));
        ResolvedTextFont viaOverload = resolver.Resolve("Test", 13, FontWeight.Normal, FontStyle.Normal, FontStretch.Normal);

        Assert.Equal("Test:13", plain.Identity);
        Assert.Equal("Test:13", viaOverload.Identity);
        Assert.Empty(fontSource.StyledLoads);
    }

    [Fact]
    public void FontIdentityOfAStyledFaceIncludesWeightStyleAndStretch()
    {
        FontResolver resolver = new(fontSource);

        ResolvedTextFont bold = resolver.Resolve(new TextAspect("Test", 13, fontWeight: FontWeight.Bold));
        ResolvedTextFont italic = resolver.Resolve(new TextAspect("Test", 13, fontStyle: FontStyle.Italic));
        ResolvedTextFont condensed = resolver.Resolve(new TextAspect("Test", 13, fontStretch: FontStretch.Condensed));
        ResolvedTextFont regular = resolver.Resolve(new TextAspect("Test", 13));

        Assert.Equal("Test:13:700:Normal:Normal", bold.Identity);
        Assert.Equal("Test:13:400:Italic:Normal", italic.Identity);
        Assert.Equal("Test:13:400:Normal:Condensed", condensed.Identity);
        Assert.Equal(4, new[] { bold.Identity, italic.Identity, condensed.Identity, regular.Identity }.Distinct().Count());
        Assert.Equal(
            [(FontWeight.Bold, FontStyle.Normal, FontStretch.Normal), (FontWeight.Normal, FontStyle.Italic, FontStretch.Normal), (FontWeight.Normal, FontStyle.Normal, FontStretch.Condensed)],
            fontSource.StyledLoads);
    }

    [Fact]
    public void FontIdentityOfAScaledStyledFaceUsesTheScaledSize()
    {
        FontResolver resolver = new(fontSource);

        ResolvedTextFont bold = resolver.Resolve(new TextAspect("Test", 10, scale: 2, fontWeight: FontWeight.Bold));

        Assert.Equal("Test:20:700:Normal:Normal", bold.Identity);
        Assert.Equal(20, bold.Font.Size);
    }

    [Fact]
    public void FontResourcesIgnoreWeightStyleAndStretch()
    {
        ResourceStore store = new();
        ResourceId<FontResource> id = new("Body");
        store.SetResource(id, new FontResource(new TestFont("Body", 16)));
        FontResolver resolver = new(store);
        TextAspect aspect = new("Fallback", 16, fontResourceId: id, fontWeight: FontWeight.Bold, fontStyle: FontStyle.Italic);

        ResolvedTextFont font = resolver.Resolve(aspect);

        Assert.StartsWith("resource:", font.Identity);
        Assert.DoesNotContain("700", font.Identity);
        Assert.Equal("Body", font.Font.FamilyName);
    }

    [Fact]
    public void FontSourceWithoutStyledFacesStillResolvesAStyledAspect()
    {
        FontResolver resolver = new(new PlainFontSource());

        ResolvedTextFont bold = resolver.Resolve(new TextAspect("Plain", 12, fontWeight: FontWeight.Bold));

        Assert.Equal("Plain", bold.Font.FamilyName);
        Assert.Equal("Plain:12:700:Normal:Normal", bold.Identity);
    }

    [Fact]
    public void TextAspectRejectsUndefinedWeightStyleAndStretch()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new TextAspect("Test", 10, fontWeight: (FontWeight)450));
        Assert.Throws<ArgumentOutOfRangeException>(() => new TextAspect("Test", 10, fontStyle: (FontStyle)5));
        Assert.Throws<ArgumentOutOfRangeException>(() => new TextAspect("Test", 10, fontStretch: (FontStretch)0));
        Assert.Equal(FontWeight.Normal, new TextAspect("Test", 10).FontWeight);
        Assert.Equal(FontStyle.Normal, new TextAspect("Test", 10).FontStyle);
        Assert.Equal(FontStretch.Normal, new TextAspect("Test", 10).FontStretch);
    }

    // Line breaking of the editing path

    [Fact]
    public void LineBreakServiceMapsWrapWithOverflowToTheOverflowingWordWrap()
    {
        LineBreakService service = LineBreakService.Default;
        FontResolver resolver = new(fontSource);
        TextAspect aspect = new("Test", 10, TextWrapping.WrapWithOverflow);
        ResolvedTextFont font = resolver.Resolve(aspect);

        IReadOnlyList<TextLine> lines = service.BreakLines("abcdefghij kl", aspect, font, 20);

        Assert.Equal(["abcdefghij", "kl"], lines.Select(static line => line.Text));
    }

    private sealed class RecordingFontSource : IFontSource
    {
        public List<(FontWeight Weight, FontStyle Style, FontStretch Stretch)> StyledLoads { get; } = [];

        public IDrawFont LoadFont(string familyName, float size) => new TestFont(familyName, size);

        public IDrawFont LoadFont(string familyName, float size, FontWeight weight, FontStyle style, FontStretch stretch)
        {
            StyledLoads.Add((weight, style, stretch));
            return new TestFont(familyName, size);
        }
    }

    private sealed class PlainFontSource : IFontSource
    {
        public IDrawFont LoadFont(string familyName, float size) => new TestFont(familyName, size);
    }

    private sealed record TestFont(string FamilyName, float Size) : IDrawFont;

    private sealed class CountingMeasurer : TextMeasurer
    {
        public CountingMeasurer(IFontSource fontSource)
            : base(new FontResolver(fontSource), LineBreakService.Default, new TextLayoutCache())
        {
        }

        public int Calls { get; private set; }

        public override TextParagraphLayout MeasureParagraph(
            IReadOnlyList<TextSpan> spans,
            TextAspect paragraphAspect,
            TextParagraphOptions options,
            LayoutSize availableSize)
        {
            Calls++;
            return base.MeasureParagraph(spans, paragraphAspect, options, availableSize);
        }
    }
}
