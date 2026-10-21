using Cerneala.Drawing;
using Cerneala.Drawing.Text;
using Cerneala.UI.Media;
using SkiaSharp;

namespace Cerneala.Tests.Drawing;

// Issue #683: engine support for rich TextBlock text. TestFont is not a Skia font, so the engine measures 5 px per
// character at size 10 and a line is 12 px high with the baseline at 9.6 px.
public sealed class DrawingTextLayoutRichTextTests
{
    private const float Tolerance = 0.01f;

    private readonly TestFont font = new();
    private readonly SolidColorBrush black = new(Color.Black);

    // WordWithOverflow

    [Fact]
    public void WordWithOverflowKeepsAWordWiderThanTheLineWhole()
    {
        DrawTextLayout overflow = Layout("abcdefghij kl", new DrawTextLayoutOptions(maxWidth: 20, wrapping: DrawTextWrapping.WordWithOverflow));
        DrawTextLayout word = Layout("abcdefghij kl", new DrawTextLayoutOptions(maxWidth: 20, wrapping: DrawTextWrapping.Word));

        Assert.Equal(["abcdefghij", "kl"], overflow.Lines.Select(static line => line.Text));
        Assert.Equal(["abcd", "efgh", "ij", "kl"], word.Lines.Select(static line => line.Text));
        Assert.True(overflow.Lines[0].Bounds.Width > 20);
    }

    [Fact]
    public void WordWithOverflowBreaksAtAnOpportunityInTheFittingRangeLikeWord()
    {
        DrawTextLayoutOptions overflowOptions = new(maxWidth: 20, wrapping: DrawTextWrapping.WordWithOverflow);
        DrawTextLayoutOptions wordOptions = new(maxWidth: 20, wrapping: DrawTextWrapping.Word);

        DrawTextLayout overflow = Layout("ab cd ef", overflowOptions);
        DrawTextLayout word = Layout("ab cd ef", wordOptions);

        Assert.Equal(["ab", "cd", "ef"], word.Lines.Select(static line => line.Text));
        Assert.Equal(word.Lines.Select(static line => line.Text), overflow.Lines.Select(static line => line.Text));
    }

    [Fact]
    public void WordWithOverflowOfTextWithoutAnyOpportunityIsOneLine()
    {
        DrawTextLayout layout = Layout("abcdefghijkl", new DrawTextLayoutOptions(maxWidth: 20, wrapping: DrawTextWrapping.WordWithOverflow));

        Assert.Equal(["abcdefghijkl"], layout.Lines.Select(static line => line.Text));
    }

    [Fact]
    public void WordWithOverflowExtendsTheLineToTheNextHyphen()
    {
        DrawTextLayout layout = Layout("abcdefgh-ijkl mn", new DrawTextLayoutOptions(maxWidth: 20, wrapping: DrawTextWrapping.WordWithOverflow));

        Assert.Equal(["abcdefgh-", "ijkl", "mn"], layout.Lines.Select(static line => line.Text));
    }

    [Fact]
    public void WordWithOverflowWrapsEachParagraphSeparately()
    {
        DrawTextLayout layout = Layout("abcdefghij\nklmnopqrst uv", new DrawTextLayoutOptions(maxWidth: 20, wrapping: DrawTextWrapping.WordWithOverflow));

        Assert.Equal(["abcdefghij", "klmnopqrst", "uv"], layout.Lines.Select(static line => line.Text));
    }

    // Alignment

    [Fact]
    public void LeftAndRightAlignmentArePhysicalForLeftToRightText()
    {
        DrawTextLayout left = Layout("ab", new DrawTextLayoutOptions(maxWidth: 40, alignment: DrawTextAlignment.Left));
        DrawTextLayout right = Layout("ab", new DrawTextLayoutOptions(maxWidth: 40, alignment: DrawTextAlignment.Right));

        Assert.Equal(0, left.Lines[0].Bounds.X, Tolerance);
        Assert.Equal(30, right.Lines[0].Bounds.X, Tolerance);
        Assert.Equal(30, right.Lines[0].Runs[0].Position.X, Tolerance);
    }

    [Fact]
    public void LeftAndRightAlignmentDoNotDependOnTheTextDirection()
    {
        DrawTextLayout start = Layout("אבג", new DrawTextLayoutOptions(maxWidth: 40, alignment: DrawTextAlignment.Start));
        DrawTextLayout left = Layout("אבג", new DrawTextLayoutOptions(maxWidth: 40, alignment: DrawTextAlignment.Left));
        DrawTextLayout right = Layout("אבג", new DrawTextLayoutOptions(maxWidth: 40, alignment: DrawTextAlignment.Right));

        Assert.Equal(DrawTextDirection.RightToLeft, left.Lines[0].Direction);
        Assert.Equal(25, start.Lines[0].Bounds.X, Tolerance);
        Assert.Equal(0, left.Lines[0].Bounds.X, Tolerance);
        Assert.Equal(25, right.Lines[0].Bounds.X, Tolerance);
    }

    // Line height

    [Fact]
    public void LineHeightIsNaNByDefaultAndKeepsTheContentHeight()
    {
        DrawTextLayoutOptions options = new();
        DrawTextLayout layout = Layout("a\nb", options);

        Assert.True(float.IsNaN(options.LineHeight));
        Assert.Equal(DrawLineStackingStrategy.MaxHeight, options.LineStackingStrategy);
        Assert.Equal(12, layout.Lines[0].Bounds.Height, Tolerance);
        Assert.Equal(9.6f, layout.Lines[0].Baseline, Tolerance);
        Assert.Equal(24, layout.Bounds.Height, Tolerance);
    }

    [Fact]
    public void BlockLineHeightMakesEveryLineThatHighAndScalesTheBaseline()
    {
        DrawTextLayout layout = Layout(
            "a\nb",
            new DrawTextLayoutOptions(lineHeight: 40, lineStackingStrategy: DrawLineStackingStrategy.BlockLineHeight));

        Assert.Equal(40, layout.Lines[0].Bounds.Height, Tolerance);
        Assert.Equal(32, layout.Lines[0].Baseline, Tolerance);
        Assert.Equal(40, layout.Lines[1].Bounds.Y, Tolerance);
        Assert.Equal(72, layout.Lines[1].Baseline, Tolerance);
        Assert.Equal(80, layout.Bounds.Height, Tolerance);
    }

    [Fact]
    public void BlockLineHeightSmallerThanTheContentShrinksTheLine()
    {
        DrawTextLayout layout = Layout(
            "a",
            new DrawTextLayoutOptions(lineHeight: 6, lineStackingStrategy: DrawLineStackingStrategy.BlockLineHeight));

        Assert.Equal(6, layout.Lines[0].Bounds.Height, Tolerance);
        Assert.Equal(4.8f, layout.Lines[0].Baseline, Tolerance);
    }

    [Fact]
    public void MaxHeightStackingIsTheLargerOfLineHeightAndContentHeight()
    {
        DrawTextLayout small = Layout("a", new DrawTextLayoutOptions(lineHeight: 8));
        DrawTextLayout large = Layout("a", new DrawTextLayoutOptions(lineHeight: 20));

        Assert.Equal(12, small.Lines[0].Bounds.Height, Tolerance);
        Assert.Equal(9.6f, small.Lines[0].Baseline, Tolerance);
        Assert.Equal(20, large.Lines[0].Bounds.Height, Tolerance);
        Assert.Equal(16, large.Lines[0].Baseline, Tolerance);
    }

    [Fact]
    public void LineSpacingMultipliesTheLineBoxHeightForTheAdvance()
    {
        DrawTextLayout layout = Layout(
            "a\nb",
            new DrawTextLayoutOptions(
                lineSpacing: 2,
                lineHeight: 40,
                lineStackingStrategy: DrawLineStackingStrategy.BlockLineHeight));

        Assert.Equal(40, layout.Lines[0].Bounds.Height, Tolerance);
        Assert.Equal(80, layout.Lines[1].Bounds.Y, Tolerance);
    }

    [Fact]
    public void MaxHeightCountsTheLineHeightOption()
    {
        DrawTextLayout layout = Layout(
            "a\nb\nc",
            new DrawTextLayoutOptions(
                maxHeight: 85,
                lineHeight: 40,
                lineStackingStrategy: DrawLineStackingStrategy.BlockLineHeight));

        Assert.Equal(2, layout.Lines.Count);
    }

    [Theory]
    [InlineData(0f)]
    [InlineData(-1f)]
    [InlineData(float.PositiveInfinity)]
    [InlineData(float.NegativeInfinity)]
    public void LineHeightMustBeNaNOrPositiveAndFinite(float lineHeight)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new DrawTextLayoutOptions(lineHeight: lineHeight));
    }

    [Fact]
    public void LineStackingStrategyMustBeDefined()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new DrawTextLayoutOptions(lineStackingStrategy: (DrawLineStackingStrategy)9));
    }

    [Fact]
    public void BuilderCacheSeparatesLayoutsByLineHeightAndStrategy()
    {
        DrawTextLayoutBuilder builder = new();
        builder.AddSpan(new DrawTextSpan("cached", font, 10, black));

        DrawTextLayout natural = builder.Build(new DrawTextLayoutOptions());
        DrawTextLayout tall = builder.Build(new DrawTextLayoutOptions(lineHeight: 30));
        DrawTextLayout block = builder.Build(new DrawTextLayoutOptions(lineHeight: 30, lineStackingStrategy: DrawLineStackingStrategy.BlockLineHeight));
        DrawTextLayout naturalAgain = builder.Build(new DrawTextLayoutOptions());

        Assert.NotSame(natural, tall);
        Assert.NotSame(tall, block);
        Assert.Same(natural, naturalAgain);
    }

    // Span index

    [Fact]
    public void RunsKnowTheSpanTheyCameFromAndSpansOfTheSameStyleAreNotMerged()
    {
        DrawTextLayout layout = new DrawTextLayoutBuilder()
            .AddSpan(new DrawTextSpan("ab", font, 10, black))
            .AddSpan(new DrawTextSpan("cd", font, 10, black))
            .Build();

        IReadOnlyList<DrawTextLayoutRun> runs = layout.Lines.Single().Runs;
        Assert.Equal(["ab", "cd"], runs.Select(static run => run.Text));
        Assert.Equal([0, 1], runs.Select(static run => run.SpanIndex));
    }

    [Fact]
    public void FragmentsOfOneSpanStillMergeIntoOneRun()
    {
        DrawTextLayout layout = new DrawTextLayoutBuilder()
            .AddSpan(new DrawTextSpan("abcdef", font, 10, black))
            .Build();

        DrawTextLayoutRun run = Assert.Single(layout.Lines.Single().Runs);
        Assert.Equal("abcdef", run.Text);
        Assert.Equal(0, run.SpanIndex);
    }

    [Fact]
    public void WrappedSpansKeepTheirSpanIndexOnEveryLine()
    {
        DrawTextLayout layout = new DrawTextLayoutBuilder()
            .AddSpan(new DrawTextSpan("ab ", font, 10, black))
            .AddSpan(new DrawTextSpan("cd ef", font, 10, black))
            .Build(new DrawTextLayoutOptions(maxWidth: 12, wrapping: DrawTextWrapping.Word));

        Assert.Equal(["ab", "cd", "ef"], layout.Lines.Select(static line => line.Text));
        Assert.Equal([0, 1, 1], layout.Lines.Select(static line => line.Runs.Single().SpanIndex));
    }

    [Fact]
    public void EllipsisTakesTheSpanIndexOfTheSpanItStyles()
    {
        DrawTextLayout layout = new DrawTextLayoutBuilder()
            .AddSpan(new DrawTextSpan("aaaa", font, 10, black))
            .AddSpan(new DrawTextSpan("bbbb", font, 10, black))
            .Build(new DrawTextLayoutOptions(maxWidth: 30, trimming: DrawTextTrimming.CharacterEllipsis));

        DrawTextLayoutLine line = Assert.Single(layout.Lines);
        Assert.True(line.IsTrimmed);
        Assert.Equal(["aaaa", "b…"], line.Runs.Select(static run => run.Text));
        Assert.Equal([0, 1], line.Runs.Select(static run => run.SpanIndex));
    }

    // Decoration metrics

    [Fact]
    public void SkiaFontReportsDecorationMetricsBelowAndAboveTheBaseline()
    {
        IDrawFont skia = new SystemFontSource().LoadFont("Segoe UI", 16);
        DrawTextRun run = new(skia, "Hello", 16);

        bool measured = TextShaper.Default.TryMeasureDecorations(run, out TextDecorationMetrics metrics);

        Assert.True(measured);
        Assert.True(metrics.UnderlineOffset > 0, $"underline offset {metrics.UnderlineOffset}");
        Assert.True(metrics.UnderlineThickness > 0);
        Assert.True(metrics.StrikethroughOffset < 0, $"strikethrough offset {metrics.StrikethroughOffset}");
        Assert.True(metrics.StrikethroughThickness > 0);
        Assert.True(metrics.UnderlineOffset < 16);
        Assert.True(-metrics.StrikethroughOffset < 16);
    }

    [Fact]
    public void DecorationMetricsScaleWithTheRunSize()
    {
        IDrawFont skia = new SystemFontSource().LoadFont("Segoe UI", 16);

        TextShaper.Default.TryMeasureDecorations(new DrawTextRun(skia, "H", 16), out TextDecorationMetrics small);
        TextShaper.Default.TryMeasureDecorations(new DrawTextRun(skia, "H", 64), out TextDecorationMetrics large);

        Assert.True(large.UnderlineOffset > small.UnderlineOffset);
        Assert.True(large.StrikethroughOffset < small.StrikethroughOffset);
    }

    [Fact]
    public void NonSkiaFontHasNoDecorationMetrics()
    {
        bool measured = TextShaper.Default.TryMeasureDecorations(new DrawTextRun(font, "Hello", 10), out TextDecorationMetrics metrics);

        Assert.False(measured);
        Assert.Equal(default, metrics);
    }

    [Fact]
    public void FallbackDecorationMetricsFollowTheFontSize()
    {
        TextDecorationMetrics metrics = TextDecorationMetrics.CreateFallback(28);

        Assert.Equal(2.8f, metrics.UnderlineOffset, Tolerance);
        Assert.Equal(2, metrics.UnderlineThickness, Tolerance);
        Assert.Equal(-8.4f, metrics.StrikethroughOffset, Tolerance);
        Assert.Equal(2, metrics.StrikethroughThickness, Tolerance);
        Assert.Equal(1, TextDecorationMetrics.CreateFallback(10).UnderlineThickness, Tolerance);
    }

    // Font faces

    [Fact]
    public void SystemFontSourceResolvesBoldAndItalicFacesOfTheFamily()
    {
        SystemFontSource source = new();

        SkiaFont regular = Assert.IsType<SkiaFont>(source.LoadFont("Segoe UI", 16));
        SkiaFont bold = Assert.IsType<SkiaFont>(source.LoadFont("Segoe UI", 16, FontWeight.Bold, FontStyle.Normal, FontStretch.Normal));
        SkiaFont italic = Assert.IsType<SkiaFont>(source.LoadFont("Segoe UI", 16, FontWeight.Normal, FontStyle.Italic, FontStretch.Normal));
        SkiaFont oblique = Assert.IsType<SkiaFont>(source.LoadFont("Segoe UI", 16, FontWeight.Normal, FontStyle.Oblique, FontStretch.Normal));

        Assert.Equal(400, regular.Typeface.FontStyle.Weight);
        Assert.Equal(700, bold.Typeface.FontStyle.Weight);
        Assert.Equal(SKFontStyleSlant.Upright, bold.Typeface.FontStyle.Slant);
        Assert.Equal(SKFontStyleSlant.Italic, italic.Typeface.FontStyle.Slant);
        Assert.Equal(SKFontStyleSlant.Oblique, oblique.Typeface.FontStyle.Slant);
        Assert.Equal("Segoe UI", bold.FamilyName);
        Assert.Equal("Segoe UI", bold.Typeface.FamilyName);
        Assert.Equal(16, bold.Size);
    }

    [Fact]
    public void SystemFontSourceKeepsTheUnchangedPathForNormalWeightStyleAndStretch()
    {
        SystemFontSource source = new();

        SkiaFont plain = Assert.IsType<SkiaFont>(source.LoadFont("Segoe UI", 16));
        SkiaFont explicitNormal = Assert.IsType<SkiaFont>(source.LoadFont("Segoe UI", 16, FontWeight.Normal, FontStyle.Normal, FontStretch.Normal));

        Assert.Same(plain.Typeface, explicitNormal.Typeface);
    }

    [Fact]
    public void SystemFontSourceCachesTheStyledTypeface()
    {
        SystemFontSource source = new();

        SkiaFont first = Assert.IsType<SkiaFont>(source.LoadFont("Segoe UI", 12, FontWeight.Bold, FontStyle.Italic, FontStretch.Normal));
        SkiaFont second = Assert.IsType<SkiaFont>(source.LoadFont("Segoe UI", 20, FontWeight.Bold, FontStyle.Italic, FontStretch.Normal));

        Assert.Same(first.Typeface, second.Typeface);
        Assert.Equal(700, first.Typeface.FontStyle.Weight);
        Assert.Equal(SKFontStyleSlant.Italic, first.Typeface.FontStyle.Slant);
    }

    [Fact]
    public void SystemFontSourceFallsBackToTheDefaultTypefaceForAnUnknownFamily()
    {
        SystemFontSource source = new();

        SkiaFont font = Assert.IsType<SkiaFont>(source.LoadFont("No Such Family 683", 16, FontWeight.Bold, FontStyle.Normal, FontStretch.Normal));

        Assert.NotNull(font.Typeface);
        Assert.Equal("No Such Family 683", font.FamilyName);
    }

    [Fact]
    public void SystemFontSourceRejectsUndefinedWeightStyleAndStretch()
    {
        SystemFontSource source = new();

        Assert.Throws<ArgumentOutOfRangeException>(() => source.LoadFont("Segoe UI", 16, (FontWeight)450, FontStyle.Normal, FontStretch.Normal));
        Assert.Throws<ArgumentOutOfRangeException>(() => source.LoadFont("Segoe UI", 16, FontWeight.Normal, (FontStyle)5, FontStretch.Normal));
        Assert.Throws<ArgumentOutOfRangeException>(() => source.LoadFont("Segoe UI", 16, FontWeight.Normal, FontStyle.Normal, (FontStretch)0));
    }

    [Fact]
    public void FontSourceWithoutStyledFacesReturnsTheFontOfTheTwoArgumentOverload()
    {
        IFontSource source = new PlainFontSource();

        IDrawFont bold = source.LoadFont("Plain", 12, FontWeight.Bold, FontStyle.Italic, FontStretch.Condensed);

        Assert.Equal("Plain", bold.FamilyName);
        Assert.Equal(12, bold.Size);
    }

    private DrawTextLayout Layout(string text, DrawTextLayoutOptions options) =>
        new DrawTextLayoutBuilder()
            .AddSpan(new DrawTextSpan(text, font, 10, black))
            .Build(options);

    private sealed class TestFont : IDrawFont
    {
        public string FamilyName => "RichTextTest";

        public float Size => 10;
    }

    private sealed class PlainFontSource : IFontSource
    {
        public IDrawFont LoadFont(string familyName, float size) => new PlainFont(familyName, size);
    }

    private sealed record PlainFont(string FamilyName, float Size) : IDrawFont;
}
