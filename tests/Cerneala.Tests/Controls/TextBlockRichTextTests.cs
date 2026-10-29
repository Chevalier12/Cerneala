using Cerneala.Drawing;
using Cerneala.Drawing.Text;
using Cerneala.UI.Controls;
using Cerneala.UI.Documents;
using Cerneala.UI.Elements;
using Cerneala.UI.Invalidation;
using Cerneala.UI.Layout;
using Cerneala.UI.Media;
using Cerneala.UI.Text;
using SkiaSharp;

namespace Cerneala.Tests.Controls;

// Issue #683: TextBlock lays out Text and Inlines through the Drawing text layout engine.
public sealed class TextBlockRichTextTests
{
    private const string Family = "Segoe UI";

    private static TextBlock CreateTextBlock(string text)
    {
        return new TextBlock
        {
            Text = text,
            FontFamily = Family,
            FontSize = 16,
            Foreground = new SolidColorBrush(Color.Black)
        };
    }

    private static DrawCommandList Render(TextBlock textBlock, float width, float height = 80)
    {
        UIRoot root = new();
        root.VisualChildren.Add(textBlock);
        root.ProcessFrame();
        textBlock.Arrange(new ArrangeContext(new LayoutRect(2, 3, width, height)));
        root.Invalidate(InvalidationFlags.Render | InvalidationFlags.Subtree, "test");
        root.ProcessFrame();
        return root.RetainedRenderer.Commit(root);
    }

    private static LayoutSize Measure(TextBlock textBlock, float width, float height = 1000)
    {
        return textBlock.Measure(new MeasureContext(new LayoutSize(width, height)));
    }

    private static DrawCommand[] TextCommands(DrawCommandList commands)
    {
        return commands.Where(static command => command.Kind == DrawCommandKind.DrawText).ToArray();
    }

    // The right end of a drawn text run: its origin plus the advance width of its shaped glyphs. A justified line draws
    // a whole word as one run, so the start of the last run is not the right edge of the line.
    private static float RightOf(DrawCommand command)
    {
        Assert.True(TextShaper.Default.TryShape(command.TextRun!, out TextShapeResult shape));
        return command.Position.X + shape.AdvanceWidth;
    }

    private static float SingleLineHeight(string family = Family, float size = 16)
    {
        TextBlock probe = new() { Text = "Hg", FontFamily = family, FontSize = size };
        return Measure(probe, 1000).Height;
    }

    // TextAlignment

    [Fact]
    public void CenterAlignmentCentersTheLineInTheArrangedWidth()
    {
        TextBlock textBlock = CreateTextBlock("Settings");
        textBlock.TextAlignment = TextAlignment.Center;
        LayoutSize desired = Measure(textBlock, 200);

        DrawCommand[] text = TextCommands(Render(textBlock, 200));

        Assert.Single(text);
        Assert.Equal(2 + ((200 - desired.Width) / 2), text[0].Position.X, 0.01);
    }

    [Fact]
    public void RightAlignmentPlacesTheLineAtTheRightEdgeOfTheArrangedWidth()
    {
        TextBlock textBlock = CreateTextBlock("Settings");
        textBlock.TextAlignment = TextAlignment.Right;
        LayoutSize desired = Measure(textBlock, 200);

        DrawCommand[] text = TextCommands(Render(textBlock, 200));

        Assert.Single(text);
        Assert.Equal(2 + (200 - desired.Width), text[0].Position.X, 0.01);
    }

    [Fact]
    public void LeftAlignmentIsTheDefaultAndPlacesTheLineAtTheLeftEdge()
    {
        TextBlock textBlock = CreateTextBlock("Settings");

        DrawCommand[] text = TextCommands(Render(textBlock, 200));

        Assert.Equal(TextAlignment.Left, textBlock.TextAlignment);
        Assert.Equal(2, text[0].Position.X, 0.01);
    }

    [Fact]
    public void AlignmentDoesNotChangeTheMeasuredWidth()
    {
        TextBlock left = CreateTextBlock("Settings");
        TextBlock center = CreateTextBlock("Settings");
        center.TextAlignment = TextAlignment.Center;

        Assert.Equal(Measure(left, 200), Measure(center, 200));
    }

    [Fact]
    public void JustifySpreadsTheWrappedLinesOfAParagraphButNotItsLastLine()
    {
        // Corrected in review round 2: the line before a hard break ends its paragraph, so it is not justified either.
        TextBlock textBlock = CreateTextBlock("aaaa bbbb cccc dddd eeee ffff gggg hhhh");
        textBlock.TextWrapping = TextWrapping.Wrap;
        textBlock.TextAlignment = TextAlignment.Justify;

        DrawCommand[] text = TextCommands(Render(textBlock, 100));

        float[] lineYs = text.Select(static command => command.Position.Y).Distinct().OrderBy(static y => y).ToArray();
        Assert.True(lineYs.Length >= 3, $"{lineYs.Length} lines");
        float RightEdge(float y) => text.Where(command => command.Position.Y == y).Max(static command => RightOf(command));
        for (int index = 0; index < lineYs.Length - 1; index++)
        {
            Assert.True(RightEdge(lineYs[index]) > 2 + 80, $"wrapped line {index} right edge {RightEdge(lineYs[index])}");
        }

        Assert.True(RightEdge(lineYs[^1]) < 2 + 80, $"last line right edge {RightEdge(lineYs[^1])}");
    }

    // FontWeight, FontStyle

    [Fact]
    public void FontWeightBoldSelectsTheBoldFaceOfTheSameFamily()
    {
        TextBlock regular = CreateTextBlock("Settings");
        TextBlock bold = CreateTextBlock("Settings");
        bold.FontWeight = FontWeight.Bold;

        SkiaFont regularFont = Assert.IsType<SkiaFont>(Assert.Single(TextCommands(Render(regular, 200))).TextRun!.Font);
        SkiaFont boldFont = Assert.IsType<SkiaFont>(Assert.Single(TextCommands(Render(bold, 200))).TextRun!.Font);

        Assert.Equal(400, regularFont.Typeface.FontStyle.Weight);
        Assert.Equal(700, boldFont.Typeface.FontStyle.Weight);
        Assert.Equal(Family, boldFont.FamilyName);
        Assert.Equal(Family, boldFont.Typeface.FamilyName);
    }

    [Fact]
    public void FontStyleItalicSelectsTheItalicFaceOfTheSameFamily()
    {
        TextBlock italic = CreateTextBlock("Settings");
        italic.FontStyle = FontStyle.Italic;

        SkiaFont font = Assert.IsType<SkiaFont>(Assert.Single(TextCommands(Render(italic, 200))).TextRun!.Font);

        Assert.Equal(SKFontStyleSlant.Italic, font.Typeface.FontStyle.Slant);
        Assert.Equal(Family, font.FamilyName);
        Assert.Equal(Family, font.Typeface.FamilyName);
    }

    [Fact]
    public void FontWeightAndStyleAreInheritedFromTheParentControl()
    {
        UIRoot root = new();
        Control parent = new()
        {
            FontWeight = FontWeight.Bold,
            FontStyle = FontStyle.Italic,
            FontStretch = FontStretch.Condensed
        };
        TextBlock textBlock = CreateTextBlock("Settings");
        root.VisualChildren.Add(parent);
        parent.VisualChildren.Add(textBlock);

        root.ProcessFrame();

        Assert.Equal(FontWeight.Bold, textBlock.FontWeight);
        Assert.Equal(FontStyle.Italic, textBlock.FontStyle);
        Assert.Equal(FontStretch.Condensed, textBlock.FontStretch);
    }

    [Fact]
    public void FontWeightRejectsUndefinedValues()
    {
        TextBlock textBlock = new();

        Assert.Throws<ArgumentException>(() => textBlock.FontWeight = (FontWeight)450);
        Assert.Throws<ArgumentException>(() => textBlock.FontStyle = (FontStyle)7);
        Assert.Throws<ArgumentException>(() => textBlock.FontStretch = (FontStretch)0);
    }

    [Fact]
    public void FontWeightChangeInvalidatesMeasureAndRender()
    {
        TextBlock textBlock = CreateTextBlock("Settings");
        textBlock.DirtyState.ClearAll();

        textBlock.FontWeight = FontWeight.Bold;

        Assert.True(textBlock.DirtyState.Has(InvalidationFlags.Measure));
        Assert.True(textBlock.DirtyState.Has(InvalidationFlags.Render));
    }

    [Fact]
    public void BoldTextMeasuresWiderThanRegularText()
    {
        TextBlock regular = CreateTextBlock("Settings Settings");
        TextBlock bold = CreateTextBlock("Settings Settings");
        bold.FontWeight = FontWeight.Bold;

        Assert.True(Measure(bold, 1000).Width > Measure(regular, 1000).Width);
    }

    // TextDecorations

    [Fact]
    public void UnderlineIsDrawnUnderTheBaselineOverTheWidthOfTheRun()
    {
        TextBlock textBlock = CreateTextBlock("Hello");
        textBlock.TextDecorations = TextDecorations.Underline;
        LayoutSize desired = Measure(textBlock, 200);

        DrawCommandList commands = Render(textBlock, 200);

        DrawCommand text = Assert.Single(TextCommands(commands));
        DrawCommand underline = Assert.Single(commands, static command => command.Kind == DrawCommandKind.FillRectangle);
        Assert.True(underline.Rect.Y > text.Position.Y, $"underline {underline.Rect.Y} baseline {text.Position.Y}");
        Assert.True(underline.Rect.Bottom <= 3 + desired.Height, $"underline bottom {underline.Rect.Bottom} line bottom {3 + desired.Height}");
        Assert.Equal(text.Position.X, underline.Rect.X, 0.01);
        Assert.Equal(desired.Width, underline.Rect.Width, 0.01);
        // The thickness is the one the font reports, which can be below one pixel.
        Assert.True(underline.Rect.Height > 0 && underline.Rect.Height < 4, $"thickness {underline.Rect.Height}");
    }

    [Fact]
    public void StrikethroughIsDrawnAboveTheBaselineInsideTheLine()
    {
        TextBlock textBlock = CreateTextBlock("Hello");
        textBlock.TextDecorations = TextDecorations.Strikethrough;

        DrawCommandList commands = Render(textBlock, 200);

        DrawCommand text = Assert.Single(TextCommands(commands));
        DrawCommand strike = Assert.Single(commands, static command => command.Kind == DrawCommandKind.FillRectangle);
        Assert.True(strike.Rect.Y < text.Position.Y, $"strike {strike.Rect.Y} baseline {text.Position.Y}");
        Assert.True(strike.Rect.Y > 3);
    }

    [Fact]
    public void OverlineIsDrawnAtTheTopOfTheLine()
    {
        TextBlock textBlock = CreateTextBlock("Hello");
        textBlock.TextDecorations = TextDecorations.Overline;

        DrawCommandList commands = Render(textBlock, 200);

        DrawCommand overline = Assert.Single(commands, static command => command.Kind == DrawCommandKind.FillRectangle);
        Assert.Equal(3, overline.Rect.Y, 0.01);
    }

    [Fact]
    public void BaselineDecorationIsDrawnAtTheBaseline()
    {
        TextBlock textBlock = CreateTextBlock("Hello");
        textBlock.TextDecorations = TextDecorations.Baseline;

        DrawCommandList commands = Render(textBlock, 200);

        DrawCommand text = Assert.Single(TextCommands(commands));
        DrawCommand line = Assert.Single(commands, static command => command.Kind == DrawCommandKind.FillRectangle);
        Assert.Equal(text.Position.Y, line.Rect.Y, 0.01);
    }

    [Fact]
    public void CombinedDecorationsDrawOneRectangleEach()
    {
        TextBlock textBlock = CreateTextBlock("Hello");
        textBlock.TextDecorations = TextDecorations.Underline | TextDecorations.Strikethrough;

        DrawCommandList commands = Render(textBlock, 200);

        Assert.Equal(2, commands.Count(static command => command.Kind == DrawCommandKind.FillRectangle));
    }

    [Fact]
    public void DecorationsUseTheForegroundBrushOfTheirSpan()
    {
        SolidColorBrush red = new(Color.Red);
        TextBlock textBlock = CreateTextBlock(string.Empty);
        textBlock.Inlines.Add(new Run("plain "));
        textBlock.Inlines.Add(new Run("marked") { Foreground = red, TextDecorations = TextDecorations.Underline });

        DrawCommandList commands = Render(textBlock, 300);

        DrawCommand underline = Assert.Single(commands, static command => command.Kind == DrawCommandKind.FillRectangle);
        Assert.Same(red, underline.Brush);
    }

    [Fact]
    public void TextDecorationsChangeInvalidatesRenderOnly()
    {
        TextBlock textBlock = CreateTextBlock("Hello");
        textBlock.DirtyState.ClearAll();

        textBlock.TextDecorations = TextDecorations.Underline;

        Assert.False(textBlock.DirtyState.Has(InvalidationFlags.Measure));
        Assert.True(textBlock.DirtyState.Has(InvalidationFlags.Render));
    }

    // MaxLines

    [Fact]
    public void MaxLinesLimitsTheMeasuredHeightToThatManyLines()
    {
        string text = string.Join(' ', Enumerable.Repeat("word", 60));
        TextBlock all = CreateTextBlock(text);
        all.TextWrapping = TextWrapping.Wrap;
        TextBlock limited = CreateTextBlock(text);
        limited.TextWrapping = TextWrapping.Wrap;
        limited.MaxLines = 2;
        float lineHeight = SingleLineHeight();

        LayoutSize allSize = Measure(all, 120);
        LayoutSize limitedSize = Measure(limited, 120);

        Assert.Equal(2 * lineHeight, limitedSize.Height, 0.01);
        Assert.True(allSize.Height > 2 * lineHeight);
        Assert.Equal(0, all.MaxLines);
    }

    [Fact]
    public void MaxLinesRendersOnlyTheFirstLines()
    {
        string text = string.Join(' ', Enumerable.Repeat("word", 60));
        TextBlock textBlock = CreateTextBlock(text);
        textBlock.TextWrapping = TextWrapping.Wrap;
        textBlock.MaxLines = 2;

        DrawCommand[] lines = TextCommands(Render(textBlock, 120, 300));

        Assert.Equal(2, lines.Select(static command => command.Position.Y).Distinct().Count());
    }

    [Fact]
    public void MaxLinesRejectsNegativeValues()
    {
        TextBlock textBlock = new();

        Assert.Throws<ArgumentException>(() => textBlock.MaxLines = -1);
    }

    // Wrapping

    [Fact]
    public void WrapWithOverflowKeepsAWordWiderThanTheLineOnOneLine()
    {
        TextBlock wrap = CreateTextBlock("Supercalifragilisticexpialidocious");
        wrap.TextWrapping = TextWrapping.Wrap;
        TextBlock overflow = CreateTextBlock("Supercalifragilisticexpialidocious");
        overflow.TextWrapping = TextWrapping.WrapWithOverflow;
        float lineHeight = SingleLineHeight();

        LayoutSize wrapped = Measure(wrap, 60);
        LayoutSize overflowed = Measure(overflow, 60);

        Assert.True(wrapped.Height > lineHeight * 1.5f, $"Wrap height {wrapped.Height}");
        Assert.Equal(lineHeight, overflowed.Height, 0.01);
        Assert.True(overflowed.Width > 60, $"WrapWithOverflow width {overflowed.Width}");
    }

    [Fact]
    public void WrapWithOverflowStillBreaksBetweenWords()
    {
        TextBlock overflow = CreateTextBlock("Supercalifragilisticexpialidocious and more words");
        overflow.TextWrapping = TextWrapping.WrapWithOverflow;
        float lineHeight = SingleLineHeight();

        LayoutSize size = Measure(overflow, 60);

        Assert.True(size.Height >= lineHeight * 2 - 0.01f);
    }

    // LineHeight, LineStackingStrategy

    [Fact]
    public void BlockLineHeightMakesEveryLineExactlyThatHigh()
    {
        TextBlock textBlock = CreateTextBlock("a\nb\nc");
        textBlock.LineHeight = 40;
        textBlock.LineStackingStrategy = LineStackingStrategy.BlockLineHeight;

        Assert.Equal(120, Measure(textBlock, 200).Height, 0.01);
    }

    [Fact]
    public void BlockLineHeightCanMakeLinesLowerThanTheirContent()
    {
        TextBlock textBlock = CreateTextBlock("a\nb\nc");
        textBlock.LineHeight = 5;
        textBlock.LineStackingStrategy = LineStackingStrategy.BlockLineHeight;

        Assert.Equal(15, Measure(textBlock, 200).Height, 0.01);
    }

    [Fact]
    public void MaxHeightStackingKeepsTheContentHeightWhenLineHeightIsSmaller()
    {
        TextBlock textBlock = CreateTextBlock("a\nb\nc");
        textBlock.LineHeight = 5;
        float lineHeight = SingleLineHeight();

        Assert.Equal(LineStackingStrategy.MaxHeight, textBlock.LineStackingStrategy);
        Assert.Equal(3 * lineHeight, Measure(textBlock, 200).Height, 0.01);
    }

    [Fact]
    public void MaxHeightStackingUsesLineHeightWhenItIsLarger()
    {
        TextBlock textBlock = CreateTextBlock("a\nb\nc");
        textBlock.LineHeight = 40;

        Assert.Equal(120, Measure(textBlock, 200).Height, 0.01);
    }

    [Fact]
    public void NaNLineHeightKeepsTheContentHeight()
    {
        TextBlock textBlock = CreateTextBlock("a\nb\nc");
        float lineHeight = SingleLineHeight();

        Assert.True(float.IsNaN(textBlock.LineHeight));
        Assert.Equal(3 * lineHeight, Measure(textBlock, 200).Height, 0.01);
    }

    [Fact]
    public void BlockLineHeightMovesTheBaselineWithTheLine()
    {
        TextBlock textBlock = CreateTextBlock("a\nb");
        textBlock.LineHeight = 40;
        textBlock.LineStackingStrategy = LineStackingStrategy.BlockLineHeight;

        DrawCommand[] lines = TextCommands(Render(textBlock, 200));

        Assert.Equal(2, lines.Length);
        Assert.Equal(40, lines[1].Position.Y - lines[0].Position.Y, 0.01);
        Assert.True(lines[0].Position.Y > 3 && lines[0].Position.Y <= 3 + 40);
    }

    [Theory]
    [InlineData(0f)]
    [InlineData(-1f)]
    [InlineData(float.PositiveInfinity)]
    public void LineHeightRejectsNonPositiveAndInfiniteValues(float value)
    {
        TextBlock textBlock = new();

        Assert.Throws<ArgumentException>(() => textBlock.LineHeight = value);
    }

    // Inlines

    [Fact]
    public void RunAndBoldRunDrawAsTwoTextCommandsWithRegularAndBoldFaces()
    {
        TextBlock textBlock = CreateTextBlock(string.Empty);
        textBlock.Inlines.Add(new Run("a"));
        textBlock.Inlines.Add(new Bold(new Run("b")));

        DrawCommand[] text = TextCommands(Render(textBlock, 200));

        Assert.Equal(2, text.Length);
        Assert.Equal("a", text[0].TextRun!.Text);
        Assert.Equal("b", text[1].TextRun!.Text);
        SkiaFont regular = Assert.IsType<SkiaFont>(text[0].TextRun!.Font);
        SkiaFont bold = Assert.IsType<SkiaFont>(text[1].TextRun!.Font);
        Assert.Equal(400, regular.Typeface.FontStyle.Weight);
        Assert.Equal(700, bold.Typeface.FontStyle.Weight);
        Assert.True(text[1].Position.X > text[0].Position.X);
    }

    [Fact]
    public void ItalicRunDrawsWithTheItalicFace()
    {
        TextBlock textBlock = CreateTextBlock(string.Empty);
        textBlock.Inlines.Add(new Run("a"));
        textBlock.Inlines.Add(new Italic(new Run("b")));

        DrawCommand[] text = TextCommands(Render(textBlock, 200));

        SkiaFont italic = Assert.IsType<SkiaFont>(text[1].TextRun!.Font);
        Assert.Equal(SKFontStyleSlant.Italic, italic.Typeface.FontStyle.Slant);
    }

    [Fact]
    public void RunsWithTheSameStyleStayOneRunPerSpan()
    {
        TextBlock textBlock = CreateTextBlock(string.Empty);
        textBlock.Inlines.Add(new Run("a"));
        textBlock.Inlines.Add(new Run("b"));

        DrawCommand[] text = TextCommands(Render(textBlock, 200));

        Assert.Equal(["a", "b"], text.Select(static command => command.TextRun!.Text));
    }

    [Fact]
    public void RunTakesTheForegroundOfItsOwnOrOfTheTextBlock()
    {
        SolidColorBrush blockBrush = new(Color.Black);
        SolidColorBrush runBrush = new(Color.Red);
        TextBlock textBlock = CreateTextBlock(string.Empty);
        textBlock.Foreground = blockBrush;
        textBlock.Inlines.Add(new Run("a"));
        textBlock.Inlines.Add(new Run("b") { Foreground = runBrush });

        DrawCommand[] text = TextCommands(Render(textBlock, 200));

        Assert.Same(blockBrush, text[0].Brush);
        Assert.Same(runBrush, text[1].Brush);
    }

    [Fact]
    public void InlinePropertiesOverrideDownTheChainOfSpans()
    {
        TextBlock textBlock = CreateTextBlock(string.Empty);
        Span outer = new() { FontSize = 30 };
        Span inner = new() { FontWeight = FontWeight.Bold };
        inner.Inlines.Add(new Run("deep"));
        outer.Inlines.Add(inner);
        outer.Inlines.Add(new Run("flat") { FontSize = 20 });
        textBlock.Inlines.Add(new Run("top"));
        textBlock.Inlines.Add(outer);

        DrawCommand[] text = TextCommands(Render(textBlock, 400));

        Assert.Equal(["top", "deep", "flat"], text.Select(static command => command.TextRun!.Text));
        Assert.Equal(16, text[0].TextRun!.Size);
        Assert.Equal(30, text[1].TextRun!.Size);
        Assert.Equal(700, ((SkiaFont)text[1].TextRun!.Font).Typeface.FontStyle.Weight);
        Assert.Equal(20, text[2].TextRun!.Size);
        Assert.Equal(400, ((SkiaFont)text[2].TextRun!.Font).Typeface.FontStyle.Weight);
    }

    [Fact]
    public void ExplicitWeightOnBoldOverridesItsFrameworkDefault()
    {
        Bold bold = new();
        Assert.Equal(FontWeight.Bold, bold.FontWeight);

        bold.FontWeight = FontWeight.Normal;

        Assert.Equal(FontWeight.Normal, bold.FontWeight);
        Assert.Equal(FontStyle.Italic, new Italic().FontStyle);
    }

    [Fact]
    public void LineBreakStartsANewLine()
    {
        TextBlock textBlock = CreateTextBlock(string.Empty);
        textBlock.Inlines.Add(new Run("a"));
        textBlock.Inlines.Add(new LineBreak());
        textBlock.Inlines.Add(new Run("b"));

        DrawCommand[] text = TextCommands(Render(textBlock, 200));

        Assert.Equal(["a", "b"], text.Select(static command => command.TextRun!.Text));
        Assert.True(text[1].Position.Y > text[0].Position.Y);
        Assert.Equal("a\nb", textBlock.Text);
    }

    [Fact]
    public void InlineTextWrapsAcrossSpans()
    {
        TextBlock textBlock = CreateTextBlock(string.Empty);
        textBlock.TextWrapping = TextWrapping.Wrap;
        textBlock.Inlines.Add(new Run("one two "));
        textBlock.Inlines.Add(new Bold(new Run("three four")));
        float lineHeight = SingleLineHeight();

        LayoutSize size = Measure(textBlock, 90);

        Assert.True(size.Height >= 2 * lineHeight - 0.01f);
    }

    [Fact]
    public void ForegroundOfAnInlineChangesRenderOnly()
    {
        Run run = new("a");
        TextBlock textBlock = CreateTextBlock(string.Empty);
        textBlock.Inlines.Add(run);
        Measure(textBlock, 200);
        textBlock.DirtyState.ClearAll();

        run.Foreground = new SolidColorBrush(Color.Red);

        Assert.False(textBlock.DirtyState.Has(InvalidationFlags.Measure));
        Assert.True(textBlock.DirtyState.Has(InvalidationFlags.Render));
        Assert.Equal("a", textBlock.Text);
    }

    [Fact]
    public void FontSizeOfAnInlineInvalidatesMeasureWithoutChangingText()
    {
        Run run = new("a");
        TextBlock textBlock = CreateTextBlock(string.Empty);
        textBlock.Inlines.Add(run);
        LayoutSize before = Measure(textBlock, 200);
        textBlock.DirtyState.ClearAll();

        run.FontSize = 40;

        Assert.True(textBlock.DirtyState.Has(InvalidationFlags.Measure));
        Assert.True(textBlock.DirtyState.Has(InvalidationFlags.Render));
        Assert.True(Measure(textBlock, 200).Height > before.Height);
        Assert.Equal("a", textBlock.Text);
    }

    // Text <-> Inlines

    [Fact]
    public void SettingTextReplacesTheInlinesWithOneRun()
    {
        TextBlock textBlock = new();
        textBlock.Inlines.Add(new Bold(new Run("old")));
        Inline oldInline = textBlock.Inlines[0];

        textBlock.Text = "Hello";

        Run run = Assert.IsType<Run>(Assert.Single(textBlock.Inlines));
        Assert.Equal("Hello", run.Text);
        Assert.Equal("Hello", textBlock.Text);
        Assert.NotSame(oldInline, run);
        textBlock.Inlines.Add(oldInline);
        Assert.Equal("Helloold", textBlock.Text);
    }

    [Fact]
    public void SettingEmptyTextClearsTheInlines()
    {
        TextBlock textBlock = new() { Text = "Hello" };

        textBlock.Text = string.Empty;

        Assert.Empty(textBlock.Inlines);
        textBlock.SetValue(TextBlock.TextProperty, "again");
        textBlock.SetValue(TextBlock.TextProperty, null!);
        Assert.Empty(textBlock.Inlines);
        Assert.Equal(string.Empty, textBlock.Text);
    }

    [Fact]
    public void AddingInlinesSetsTextToTheirConcatenation()
    {
        TextBlock textBlock = new();

        textBlock.Inlines.Add(new Run("a"));
        textBlock.Inlines.Add(new Bold(new Run("b")));
        textBlock.Inlines.Add(new LineBreak());
        textBlock.Inlines.Add(new Italic(new Span(new Run("c"))));

        Assert.Equal("ab\nc", textBlock.Text);
        Assert.Equal(4, textBlock.Inlines.Count);
    }

    [Fact]
    public void ChangingInlinesUnderASpanSetsText()
    {
        TextBlock textBlock = new();
        Span span = new();
        textBlock.Inlines.Add(span);

        span.Inlines.Add(new Run("x"));
        span.Inlines.Add(new Run("y"));
        span.Inlines.RemoveAt(0);

        Assert.Equal("y", textBlock.Text);
    }

    [Fact]
    public void RemovingAndClearingInlinesUpdatesText()
    {
        TextBlock textBlock = new();
        Run first = new("a");
        textBlock.Inlines.Add(first);
        textBlock.Inlines.Add(new Run("b"));

        textBlock.Inlines.Remove(first);
        Assert.Equal("b", textBlock.Text);
        textBlock.Inlines.Clear();

        Assert.Equal(string.Empty, textBlock.Text);
        textBlock.Inlines.Add(first);
        Assert.Equal("a", textBlock.Text);
    }

    [Fact]
    public void ReplacingAnInlineUpdatesText()
    {
        TextBlock textBlock = new();
        textBlock.Inlines.Add(new Run("a"));

        textBlock.Inlines[0] = new Run("z");

        Assert.Equal("z", textBlock.Text);
    }

    [Fact]
    public void ChangingRunTextInvalidatesMeasureAndUpdatesText()
    {
        Run run = new("a");
        TextBlock textBlock = CreateTextBlock(string.Empty);
        textBlock.Inlines.Add(run);
        textBlock.Inlines.Add(new Bold(new Run("b")));
        LayoutSize before = Measure(textBlock, 400);
        textBlock.DirtyState.ClearAll();

        run.Text = "aaaaaaaa";

        Assert.True(textBlock.DirtyState.Has(InvalidationFlags.Measure));
        Assert.True(textBlock.DirtyState.Has(InvalidationFlags.Render));
        Assert.Equal("aaaaaaaab", textBlock.Text);
        Assert.True(Measure(textBlock, 400).Width > before.Width);
    }

    [Fact]
    public void ChangingRunTextToNullCoercesToEmpty()
    {
        Run run = new("a");
        TextBlock textBlock = new();
        textBlock.Inlines.Add(run);

        run.Text = null!;

        Assert.Equal(string.Empty, run.Text);
        Assert.Equal(string.Empty, textBlock.Text);
    }

    [Fact]
    public void AddingInlinesInvalidatesMeasureRenderAndSemantics()
    {
        TextBlock textBlock = new();
        textBlock.DirtyState.ClearAll();

        textBlock.Inlines.Add(new Run("a"));

        Assert.True(textBlock.DirtyState.Has(InvalidationFlags.Measure));
        Assert.True(textBlock.DirtyState.Has(InvalidationFlags.Render));
        Assert.True(textBlock.DirtyState.Has(InvalidationFlags.Semantics));
    }

    [Fact]
    public void EmptyTextBlockStillMeasuresOneLine()
    {
        TextBlock textBlock = CreateTextBlock(string.Empty);

        LayoutSize size = Measure(textBlock, 100);

        Assert.Equal(0, size.Width);
        Assert.Equal(SingleLineHeight(), size.Height, 0.01);
    }

    [Fact]
    public void InlineCollectionRejectsNullAndInlinesThatHaveAParent()
    {
        TextBlock first = new();
        TextBlock second = new();
        Run run = new("a");
        first.Inlines.Add(run);

        Assert.Throws<ArgumentNullException>(() => first.Inlines.Add(null!));
        Assert.Throws<InvalidOperationException>(() => second.Inlines.Add(run));
        Assert.Throws<InvalidOperationException>(() => first.Inlines.Add(run));
        Assert.Equal("a", first.Text);
        Assert.Equal(string.Empty, second.Text);
    }

    [Fact]
    public void InlineCollectionRejectsAnInlineInsideItsOwnSubtree()
    {
        Span outer = new();
        Span inner = new();
        outer.Inlines.Add(inner);

        Assert.Throws<InvalidOperationException>(() => inner.Inlines.Add(outer));
        Assert.Throws<InvalidOperationException>(() => outer.Inlines.Add(outer));
    }

    [Fact]
    public void RemovedInlineCanBeAddedToAnotherParent()
    {
        TextBlock first = new();
        TextBlock second = new();
        Run run = new("a");
        first.Inlines.Add(run);
        first.Inlines.Remove(run);

        second.Inlines.Add(run);

        Assert.Equal(string.Empty, first.Text);
        Assert.Equal("a", second.Text);
        run.Text = "b";
        Assert.Equal(string.Empty, first.Text);
        Assert.Equal("b", second.Text);
    }

    // The measurer

    [Fact]
    public void MeasureUsesTheParagraphMeasurerOfTheTextBlock()
    {
        TextBlock textBlock = CreateTextBlock(string.Empty);
        textBlock.TextWrapping = TextWrapping.Wrap;
        textBlock.TextAlignment = TextAlignment.Center;
        textBlock.MaxLines = 3;
        textBlock.Inlines.Add(new Run("a"));
        textBlock.Inlines.Add(new Bold(new Run("b")));
        ParagraphRecordingMeasurer measurer = new();
        textBlock.TextMeasurer = measurer;

        Measure(textBlock, 100, 50);

        Assert.Equal(["a", "b"], measurer.Spans!.Select(static span => span.Text));
        Assert.Equal(FontWeight.Normal, measurer.Spans![0].Aspect.FontWeight);
        Assert.Equal(FontWeight.Bold, measurer.Spans![1].Aspect.FontWeight);
        Assert.Equal(TextWrapping.Wrap, measurer.ParagraphAspect.Wrapping);
        Assert.Equal(new TextParagraphOptions(TextAlignment.Center, 3), measurer.Options);
        Assert.Equal(new LayoutSize(100, 50), measurer.AvailableSize);
    }

    private sealed class ParagraphRecordingMeasurer : TextMeasurer
    {
        public IReadOnlyList<TextSpan>? Spans { get; private set; }

        public TextAspect ParagraphAspect { get; private set; }

        public TextParagraphOptions Options { get; private set; }

        public LayoutSize AvailableSize { get; private set; }

        public override TextParagraphLayout MeasureParagraph(
            IReadOnlyList<TextSpan> spans,
            TextAspect paragraphAspect,
            TextParagraphOptions options,
            LayoutSize availableSize)
        {
            Spans = spans.ToArray();
            ParagraphAspect = paragraphAspect;
            Options = options;
            AvailableSize = availableSize;
            return base.MeasureParagraph(spans, paragraphAspect, options, availableSize);
        }
    }
}
