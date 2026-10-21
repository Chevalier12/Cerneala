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

// Issue #683, review round 2: regressions found while reviewing the TextBlock rich text implementation.
public sealed class TextBlockRichTextReviewTests
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

    private static DrawCommandList Render(TextBlock textBlock, float width, float height = 200)
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

    private static DrawCommand[] Rectangles(DrawCommandList commands)
    {
        return commands.Where(static command => command.Kind == DrawCommandKind.FillRectangle).ToArray();
    }

    // 1. A write or clear that keeps the effective value still changes which properties an inline sets.

    [Fact]
    public void WritingTheDefaultValueOnAnInlineMakesItStopInheritingTheTextBlockValue()
    {
        TextBlock textBlock = CreateTextBlock(string.Empty);
        textBlock.FontSize = 20;
        Run run = new("abcd");
        textBlock.Inlines.Add(run);
        LayoutSize inherited = Measure(textBlock, 1000);
        TextBlock reference = CreateTextBlock("abcd");
        LayoutSize atSixteen = Measure(reference, 1000);

        run.FontSize = 16;
        LayoutSize afterWrite = Measure(textBlock, 1000);

        Assert.True(inherited.Width > atSixteen.Width);
        Assert.Equal(atSixteen, afterWrite);
    }

    [Fact]
    public void ClearingALocalValueThatEqualsTheDefaultMakesTheInlineInheritAgain()
    {
        TextBlock textBlock = CreateTextBlock(string.Empty);
        textBlock.FontSize = 20;
        Run run = new("abcd") { FontSize = 16 };
        textBlock.Inlines.Add(run);
        LayoutSize local = Measure(textBlock, 1000);
        TextBlock reference = CreateTextBlock("abcd");
        reference.FontSize = 20;
        LayoutSize atTwenty = Measure(reference, 1000);

        run.ClearValue(TextElement.FontSizeProperty);
        LayoutSize afterClear = Measure(textBlock, 1000);

        Assert.True(local.Width < atTwenty.Width);
        Assert.Equal(atTwenty, afterClear);
    }

    [Fact]
    public void WritingTheDefaultWeightOnARunInsideBoldUnBoldsTheRun()
    {
        TextBlock textBlock = CreateTextBlock(string.Empty);
        Run run = new("ab");
        textBlock.Inlines.Add(new Bold(run));
        UIRoot root = new();
        root.VisualChildren.Add(textBlock);
        root.ProcessFrame();
        textBlock.Arrange(new ArrangeContext(new LayoutRect(0, 0, 400, 200)));
        root.Invalidate(InvalidationFlags.Render | InvalidationFlags.Subtree, "test");
        root.ProcessFrame();
        DrawCommand before = Assert.Single(TextCommands(root.RetainedRenderer.Commit(root)));
        Assert.Equal(700, ((SkiaFont)before.TextRun!.Font).Typeface.FontStyle.Weight);

        run.FontWeight = FontWeight.Normal;
        root.ProcessFrame();
        textBlock.Arrange(new ArrangeContext(new LayoutRect(0, 0, 400, 200)));
        root.Invalidate(InvalidationFlags.Render | InvalidationFlags.Subtree, "test");
        root.ProcessFrame();

        DrawCommand after = Assert.Single(TextCommands(root.RetainedRenderer.Commit(root)));
        Assert.Equal(400, ((SkiaFont)after.TextRun!.Font).Typeface.FontStyle.Weight);
    }

    [Fact]
    public void ClearingALocalValueOfAnInlineInvalidatesTheTextBlockWhenTheValueStaysTheSame()
    {
        TextBlock textBlock = CreateTextBlock("ab");
        Run run = new("cd") { FontSize = 16 };
        textBlock.Inlines.Add(run);
        Measure(textBlock, 1000);
        textBlock.DirtyState.ClearAll();

        run.ClearValue(TextElement.FontSizeProperty);

        Assert.True(textBlock.DirtyState.Has(InvalidationFlags.Measure));
        Assert.True(textBlock.DirtyState.Has(InvalidationFlags.Render));
    }

    // 5. Justify keeps the decoration of a run continuous over the justification gap.

    [Fact]
    public void JustifiedUnderlineCoversTheLineWithoutGapsAtTheSpaces()
    {
        TextBlock textBlock = CreateTextBlock("aaaa bbbb cccc dddd eeee ffff gggg hhhh");
        textBlock.TextWrapping = TextWrapping.Wrap;
        textBlock.TextAlignment = TextAlignment.Justify;
        textBlock.TextDecorations = TextDecorations.Underline;

        DrawCommand[] rectangles = Rectangles(Render(textBlock, 100));

        float firstLineY = rectangles.Min(static rectangle => rectangle.Rect.Y);
        DrawCommand[] firstLine = rectangles
            .Where(rectangle => Math.Abs(rectangle.Rect.Y - firstLineY) < 0.01f)
            .OrderBy(static rectangle => rectangle.Rect.X)
            .ToArray();
        Assert.Equal(2, firstLine[0].Rect.X, 0.01);
        for (int index = 1; index < firstLine.Length; index++)
        {
            Assert.True(
                firstLine[index].Rect.X <= firstLine[index - 1].Rect.Right + 0.01f,
                $"gap of {firstLine[index].Rect.X - firstLine[index - 1].Rect.Right} before rectangle {index}");
        }

        Assert.Equal(2 + 100, firstLine.Max(static rectangle => rectangle.Rect.Right), 0.05);
    }

    // 6. SKFontMetrics.StrikeoutPosition is the distance from the baseline to the BOTTOM of the strike stroke.

    [Fact]
    public void StrikethroughRectangleEndsAtTheBaselinePlusTheFontStrikeoutPosition()
    {
        TextBlock textBlock = CreateTextBlock("Hello");
        textBlock.TextDecorations = TextDecorations.Strikethrough;

        DrawCommandList commands = Render(textBlock, 200);

        DrawCommand text = Assert.Single(TextCommands(commands));
        DrawCommand strike = Assert.Single(Rectangles(commands));
        Assert.True(TextShaper.Default.TryMeasureDecorations(text.TextRun!, out TextDecorationMetrics metrics));
        Assert.Equal(text.Position.Y + metrics.StrikethroughOffset, strike.Rect.Bottom, 0.01);
        Assert.Equal(metrics.StrikethroughThickness, strike.Rect.Height, 0.01);
    }

    // 7. A line that ends a paragraph is not justified.

    [Fact]
    public void JustifyDoesNotSpreadTheLastLineOfAParagraph()
    {
        TextBlock textBlock = CreateTextBlock("aaaa bbbb cccc dddd eeee ffff gggg hhhh\nzz");
        textBlock.TextWrapping = TextWrapping.Wrap;
        textBlock.TextAlignment = TextAlignment.Justify;

        DrawCommand[] text = TextCommands(Render(textBlock, 100));

        float[] lineYs = text.Select(static command => command.Position.Y).Distinct().OrderBy(static y => y).ToArray();
        Assert.True(lineYs.Length >= 4, $"{lineYs.Length} lines");
        float RightEdge(float y) => text.Where(command => command.Position.Y == y).Max(static command => command.Position.X);
        for (int index = 0; index < lineYs.Length - 2; index++)
        {
            Assert.True(RightEdge(lineYs[index]) > 2 + 80, $"wrapped line {index} right edge {RightEdge(lineYs[index])}");
        }

        Assert.True(RightEdge(lineYs[^2]) < 2 + 80, $"last line of the first paragraph right edge {RightEdge(lineYs[^2])}");
        Assert.True(RightEdge(lineYs[^1]) < 2 + 40, $"second paragraph right edge {RightEdge(lineYs[^1])}");
    }

    [Fact]
    public void JustifiedLineThatEndsBeforeAHardBreakInsideSpansIsNotSpreadEither()
    {
        TextBlock textBlock = CreateTextBlock(string.Empty);
        textBlock.TextAlignment = TextAlignment.Justify;
        textBlock.Inlines.Add(new Run("aaaa bbbb"));
        textBlock.Inlines.Add(new LineBreak());
        textBlock.Inlines.Add(new Run("cccc"));

        DrawCommand[] text = TextCommands(Render(textBlock, 200));

        float firstLineY = text.Min(static command => command.Position.Y);
        float firstLineRight = text.Where(command => command.Position.Y == firstLineY).Max(static command => command.Position.X);
        Assert.True(firstLineRight < 2 + 90, $"first line right edge {firstLineRight}");
    }

    // 8. Text decorations of a parent add to the decorations of its children.

    [Fact]
    public void ChildDecorationsAddToTheDecorationsOfTheSpan()
    {
        TextBlock textBlock = CreateTextBlock(string.Empty);
        Span span = new() { TextDecorations = TextDecorations.Underline };
        span.Inlines.Add(new Run("ab"));
        span.Inlines.Add(new Run("cd") { TextDecorations = TextDecorations.Strikethrough });
        textBlock.Inlines.Add(span);

        DrawCommandList commands = Render(textBlock, 400);

        Assert.Equal(3, Rectangles(commands).Length);
    }

    [Fact]
    public void ExplicitNoneOnAChildDoesNotRemoveTheDecorationsOfTheSpan()
    {
        TextBlock textBlock = CreateTextBlock(string.Empty);
        Span span = new() { TextDecorations = TextDecorations.Underline };
        span.Inlines.Add(new Run("ab"));
        span.Inlines.Add(new Run("cd") { TextDecorations = TextDecorations.None });
        textBlock.Inlines.Add(span);

        DrawCommandList commands = Render(textBlock, 400);

        Assert.Equal(2, Rectangles(commands).Length);
    }

    [Fact]
    public void ChildDecorationsAddToTheDecorationsOfTheTextBlock()
    {
        TextBlock textBlock = CreateTextBlock(string.Empty);
        textBlock.TextDecorations = TextDecorations.Underline;
        textBlock.Inlines.Add(new Run("ab") { TextDecorations = TextDecorations.Overline });
        textBlock.Inlines.Add(new Run("cd") { TextDecorations = TextDecorations.None });

        DrawCommandList commands = Render(textBlock, 400);

        Assert.Equal(3, Rectangles(commands).Length);
    }

    // 9. The default font family takes the weight and the style too.

    [Fact]
    public void DefaultFamilyWithBoldDrawsWithABoldFace()
    {
        TextBlock textBlock = new() { Text = "Settings", FontWeight = FontWeight.Bold, Foreground = new SolidColorBrush(Color.Black) };

        SkiaFont font = Assert.IsType<SkiaFont>(Assert.Single(TextCommands(Render(textBlock, 200))).TextRun!.Font);

        Assert.Equal("Default", textBlock.FontFamily);
        Assert.Equal(700, font.Typeface.FontStyle.Weight);
    }

    [Fact]
    public void DefaultFamilyWithItalicDrawsWithAnItalicFace()
    {
        TextBlock textBlock = new() { Text = "Settings", FontStyle = FontStyle.Italic, Foreground = new SolidColorBrush(Color.Black) };

        SkiaFont font = Assert.IsType<SkiaFont>(Assert.Single(TextCommands(Render(textBlock, 200))).TextRun!.Font);

        Assert.Equal(SKFontStyleSlant.Italic, font.Typeface.FontStyle.Slant);
    }

    [Fact]
    public void BoldInsideATextBlockWithTheDefaultFamilyDrawsBold()
    {
        TextBlock textBlock = new() { Foreground = new SolidColorBrush(Color.Black) };
        textBlock.Inlines.Add(new Run("a"));
        textBlock.Inlines.Add(new Bold(new Run("b")));

        DrawCommand[] text = TextCommands(Render(textBlock, 200));

        Assert.Equal(400, ((SkiaFont)text[0].TextRun!.Font).Typeface.FontStyle.Weight);
        Assert.Equal(700, ((SkiaFont)text[1].TextRun!.Font).Typeface.FontStyle.Weight);
    }
}
