using Cerneala.Drawing;
using Cerneala.Drawing.Text;
using Cerneala.UI.Media;
using SkiaSharp;

namespace Cerneala.Tests.Drawing;

// Issue #683, review round 2: engine regressions found while reviewing the rich text layout.
// TestFont is not a Skia font, so the engine measures 5 px per character at size 10 and a line is 1.2 times the size high
// with the baseline at 0.8 of the line height.
public sealed class DrawingTextLayoutRichTextReviewTests
{
    private const float Tolerance = 0.01f;

    private readonly TestFont testFont = new();
    private readonly SolidColorBrush black = new(Color.Black);

    // 2. The baseline and the height of a line come from every span on the line.

    [Fact]
    public void MixedSizesOnOneLineShareTheLargestBaselineAndNoGlyphRisesAboveTheLine()
    {
        IDrawFont font = new SystemFontSource().LoadFont("Segoe UI", 16);
        DrawTextLayout layout = new DrawTextLayoutBuilder()
            .AddSpan(new DrawTextSpan("top", font, 16, black))
            .AddSpan(new DrawTextSpan("deep", font, 30, black))
            .Build(new DrawTextLayoutOptions());
        Metrics small = Measure(font, 16);
        Metrics large = Measure(font, 30);
        float expectedBaseline = MathF.Max(small.Baseline, large.Baseline);
        float expectedHeight = expectedBaseline + MathF.Max(small.Descent, large.Descent);

        DrawTextLayoutLine line = Assert.Single(layout.Lines);

        Assert.Equal(2, line.Runs.Count);
        Assert.Equal(expectedHeight, line.Bounds.Height, Tolerance);
        Assert.Equal(expectedBaseline, line.Baseline - line.Bounds.Y, Tolerance);
        Assert.Equal(line.Runs[0].Position.Y, line.Runs[1].Position.Y, Tolerance);
        Assert.True(line.Runs[1].Position.Y - line.Bounds.Y >= large.Baseline - Tolerance, "baseline above the 30 pt span baseline");
        Assert.True(line.Runs[0].Position.Y - small.Baseline >= line.Bounds.Y - Tolerance, "small glyphs rise above the line");
        Assert.True(line.Runs[1].Position.Y - large.Baseline >= line.Bounds.Y - Tolerance, "large glyphs rise above the line");
        Assert.True(line.Runs[0].Position.Y + small.Descent <= line.Bounds.Bottom + Tolerance, "small glyphs fall below the line");
        Assert.True(line.Runs[1].Position.Y + large.Descent <= line.Bounds.Bottom + Tolerance, "large glyphs fall below the line");
    }

    [Fact]
    public void ASingleSpanLineKeepsItsMeasuredHeightAndBaseline()
    {
        IDrawFont font = new SystemFontSource().LoadFont("Segoe UI", 16);
        DrawTextLayout layout = new DrawTextLayoutBuilder()
            .AddSpan(new DrawTextSpan("single", font, 16, black))
            .Build(new DrawTextLayoutOptions());
        Metrics metrics = Measure(font, 16);

        DrawTextLayoutLine line = Assert.Single(layout.Lines);

        Assert.Equal(metrics.Height, line.Bounds.Height);
        Assert.Equal(metrics.Baseline, line.Baseline);
    }

    [Fact]
    public void MixedSizesWithoutFontMetricsStillShareOneBaselineInsideTheLine()
    {
        DrawTextLayout layout = new DrawTextLayoutBuilder()
            .AddSpan(new DrawTextSpan("top", testFont, 10, black))
            .AddSpan(new DrawTextSpan("deep", testFont, 30, black))
            .Build(new DrawTextLayoutOptions());

        DrawTextLayoutLine line = Assert.Single(layout.Lines);

        Assert.Equal(36, line.Bounds.Height, Tolerance);
        Assert.Equal(28.8f, line.Baseline, Tolerance);
        Assert.Equal(line.Runs[0].Position.Y, line.Runs[1].Position.Y, Tolerance);
    }

    // 3. A span that ends exactly where the line starts does not count for the line.

    [Fact]
    public void LineAfterALineBreakDoesNotTakeTheHeightOfTheSpanThatEndedWithTheBreak()
    {
        DrawTextLayout layout = new DrawTextLayoutBuilder()
            .AddSpan(new DrawTextSpan("Title\n", testFont, 32, black))
            .AddSpan(new DrawTextSpan("body", testFont, 16, black))
            .Build(new DrawTextLayoutOptions());

        Assert.Equal(2, layout.Lines.Count);
        Assert.Equal(38.4f, layout.Lines[0].Bounds.Height, Tolerance);
        Assert.Equal(19.2f, layout.Lines[1].Bounds.Height, Tolerance);
    }

    [Fact]
    public void EmptyLastLineTakesTheHeightOfTheLastSpan()
    {
        DrawTextLayout layout = new DrawTextLayoutBuilder()
            .AddSpan(new DrawTextSpan("a", testFont, 10, black))
            .AddSpan(new DrawTextSpan("b\n", testFont, 30, black))
            .Build(new DrawTextLayoutOptions());

        Assert.Equal(2, layout.Lines.Count);
        Assert.Equal(36, layout.Lines[1].Bounds.Height, Tolerance);
    }

    // 5. A justified whitespace run includes the justification gap, so the runs of a line touch each other.

    [Fact]
    public void JustifiedRunsOfALineTouchAndFillTheLayoutWidth()
    {
        DrawTextLayout layout = new DrawTextLayoutBuilder()
            .AddSpan(new DrawTextSpan("ab cd ef gh\nij", testFont, 10, black))
            .Build(new DrawTextLayoutOptions(maxWidth: 30, wrapping: DrawTextWrapping.Word, alignment: DrawTextAlignment.Justify));

        IReadOnlyList<DrawTextLayoutRun> runs = layout.Lines[0].Runs;
        for (int index = 1; index < runs.Count; index++)
        {
            Assert.Equal(runs[index - 1].Bounds.Right, runs[index].Bounds.X, Tolerance);
        }

        Assert.Equal(0, runs[0].Bounds.X, Tolerance);
        Assert.Equal(30, runs[^1].Bounds.Right, Tolerance);
        Assert.Equal(30, layout.Lines[0].Bounds.Width, Tolerance);
    }

    // 7. A line that ends a paragraph is not justified.

    [Fact]
    public void JustifyLeavesTheLastLineOfEachParagraphAtItsNaturalWidth()
    {
        DrawTextLayout layout = new DrawTextLayoutBuilder()
            .AddSpan(new DrawTextSpan("ab cd ef gh\nij", testFont, 10, black))
            .Build(new DrawTextLayoutOptions(maxWidth: 30, wrapping: DrawTextWrapping.Word, alignment: DrawTextAlignment.Justify));

        Assert.Equal(["ab cd", "ef gh", "ij"], layout.Lines.Select(static line => line.Text));
        Assert.Equal(30, layout.Lines[0].Bounds.Width, Tolerance);
        Assert.Equal(25, layout.Lines[1].Bounds.Width, Tolerance);
        Assert.Equal(10, layout.Lines[2].Bounds.Width, Tolerance);
    }

    [Fact]
    public void JustifyLeavesALineBeforeAHardBreakAtItsNaturalWidth()
    {
        DrawTextLayout layout = new DrawTextLayoutBuilder()
            .AddSpan(new DrawTextSpan("ab cd\nef", testFont, 10, black))
            .Build(new DrawTextLayoutOptions(maxWidth: 40, wrapping: DrawTextWrapping.Word, alignment: DrawTextAlignment.Justify));

        Assert.Equal(25, layout.Lines[0].Bounds.Width, Tolerance);
    }

    // 9. A family name that the system does not know still gets the weight and the style.

    [Fact]
    public void UnknownFamilyTakesTheWeightOfTheDefaultTypeface()
    {
        SystemFontSource source = new();

        SkiaFont bold = Assert.IsType<SkiaFont>(source.LoadFont("Default", 16, FontWeight.Bold, FontStyle.Normal, FontStretch.Normal));

        Assert.Equal(700, bold.Typeface.FontStyle.Weight);
    }

    [Fact]
    public void UnknownFamilyTakesTheSlantOfTheDefaultTypeface()
    {
        SystemFontSource source = new();

        SkiaFont italic = Assert.IsType<SkiaFont>(source.LoadFont("Default", 16, FontWeight.Normal, FontStyle.Italic, FontStretch.Normal));

        Assert.Equal(SKFontStyleSlant.Italic, italic.Typeface.FontStyle.Slant);
    }

    [Fact]
    public void WeightedFamilyNameKeepsItsWeightWhenAnItalicIsRequested()
    {
        SystemFontSource source = new();

        SkiaFont italic = Assert.IsType<SkiaFont>(source.LoadFont("Segoe UI Bold", 16, FontWeight.Normal, FontStyle.Italic, FontStretch.Normal));

        Assert.Equal(SKFontStyleSlant.Italic, italic.Typeface.FontStyle.Slant);
        Assert.Equal(700, italic.Typeface.FontStyle.Weight);
    }

    private static Metrics Measure(IDrawFont font, float size)
    {
        DrawTextRun run = new(font, "Mg", size);
        Assert.True(TextShaper.Default.TryMeasureBaseline(run, out float baseline));
        Assert.True(TextShaper.Default.TryMeasureLineHeight(run, out float height));
        return new Metrics(baseline, height, height - baseline);
    }

    private readonly record struct Metrics(float Baseline, float Height, float Descent);

    private sealed class TestFont : IDrawFont
    {
        public string FamilyName => "RichTextReviewTest";

        public float Size => 10;
    }
}
