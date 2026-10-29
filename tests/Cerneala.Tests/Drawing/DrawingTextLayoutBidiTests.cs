using System.Globalization;
using Cerneala.Drawing;
using Cerneala.Drawing.Text;
using Cerneala.UI.Media;

namespace Cerneala.Tests.Drawing;

// Issue #1249 (bugs #1147, #1148, #1149, #1150, #1151): numbers in right-to-left text, the paragraph base direction of
// wrapped lines, the start edge of the unstretched lines of a justified paragraph, and the shaping of justified words.
// TestFont is not a Skia font, so the engine measures 5 px per grapheme at size 10.
public sealed class DrawingTextLayoutBidiTests
{
    private const float Tolerance = 0.01f;

    private readonly TestFont testFont = new();
    private readonly SolidColorBrush black = new(Color.Black);

    // #1147: an ASCII number is a European number (UAX #9). It does not split the right-to-left phrase around it, and
    // the spaces between it and the Hebrew words resolve as right-to-left (rule N1).
    [Theory]
    [InlineData(DrawTextDirection.LeftToRight, DrawTextAlignment.Start)]
    [InlineData(DrawTextDirection.RightToLeft, DrawTextAlignment.Start)]
    [InlineData(DrawTextDirection.LeftToRight, DrawTextAlignment.Justify)]
    [InlineData(DrawTextDirection.RightToLeft, DrawTextAlignment.Justify)]
    public void AnAsciiNumberInsideAHebrewPhraseKeepsThePhraseInRightToLeftOrder(DrawTextDirection direction, DrawTextAlignment alignment)
    {
        // "הו" wraps to the second line, so the first line is stretched when it is justified.
        DrawTextLayout layout = Build("אב 12 גד הו", new DrawTextLayoutOptions(
            maxWidth: 50,
            wrapping: DrawTextWrapping.Word,
            alignment: alignment,
            direction: direction));

        DrawTextLayoutLine line = layout.Lines[0];

        Assert.Equal("דג 12 בא", Visual(line));
        AssertRunsInVisualOrder(line);
    }

    [Fact]
    public void AutoStillResolvesALineThatStartsWithAnAsciiDigitAsLeftToRight()
    {
        DrawTextLayout layout = Build("123 שלום", new DrawTextLayoutOptions(direction: DrawTextDirection.Auto));

        Assert.Equal(DrawTextDirection.LeftToRight, Assert.Single(layout.Lines).Direction);
    }

    // #1148: Arabic-Indic digits are Arabic numbers (UAX #9). Rule I2 raises them above the right-to-left text, so the
    // digits of one number keep their order from left to right.
    [Fact]
    public void ArabicIndicDigitsOfAJustifiedRightToLeftLineKeepTheirLeftToRightOrder()
    {
        DrawTextLayout layout = Build("אב ١٢٣ גד הו", new DrawTextLayoutOptions(
            maxWidth: 50,
            wrapping: DrawTextWrapping.Word,
            alignment: DrawTextAlignment.Justify,
            direction: DrawTextDirection.RightToLeft));

        DrawTextLayoutLine line = layout.Lines[0];

        Assert.Equal("דג ١٢٣ בא", Visual(line));
        AssertRunsInVisualOrder(line);
        Assert.Equal(50, line.Bounds.Width, Tolerance);
    }

    [Fact]
    public void ArabicIndicDigitsOfARightToLeftLineThatIsNotJustifiedKeepTheirLeftToRightOrder()
    {
        DrawTextLayout layout = Build("אב ١٢٣ גד", new DrawTextLayoutOptions(direction: DrawTextDirection.RightToLeft));

        DrawTextLayoutLine line = Assert.Single(layout.Lines);

        Assert.Equal("דג ١٢٣ בא", Visual(line));
        DrawTextLayoutRun number = Assert.Single(line.Runs, static run => run.Text.Contains('١'));
        Assert.Equal(DrawTextDirection.LeftToRight, number.Direction);
    }

    // The renderer draws the glyphs that the shaper gives for each run, so the digits must also be drawn in order.
    [Theory]
    [InlineData(DrawTextAlignment.Start)]
    [InlineData(DrawTextAlignment.Justify)]
    public void TheDrawnGlyphsOfArabicIndicDigitsInARightToLeftLineReadFromLeftToRight(DrawTextAlignment alignment)
    {
        IDrawFont font = new SystemFontSource().LoadFont("Segoe UI", 16);
        DrawTextLayout layout = new DrawTextLayoutBuilder()
            .AddSpan(new DrawTextSpan("אב ١٢٣ גד הו", font, 16, black))
            .Build(new DrawTextLayoutOptions(
                maxWidth: 70,
                wrapping: DrawTextWrapping.Word,
                alignment: alignment,
                direction: DrawTextDirection.RightToLeft));
        ushort[] digits = new[] { "١", "٢", "٣" }.Select(digit => Assert.Single(Shape(font, digit).GlyphIds)).ToArray();

        DrawTextLayoutLine line = layout.Lines[0];
        List<(float X, ushort Glyph)> drawn = [];
        foreach (DrawTextLayoutRun run in line.Runs)
        {
            TextShapeResult shape = Shape(run.TextRun);
            ushort[] glyphs = shape.GlyphIds;
            DrawPoint[] positions = shape.GlyphPositions;
            for (int index = 0; index < glyphs.Length; index++)
            {
                drawn.Add((run.Position.X + positions[index].X, glyphs[index]));
            }
        }

        ushort[] drawnDigits = drawn
            .Where(glyph => digits.Contains(glyph.Glyph))
            .OrderBy(static glyph => glyph.X)
            .Select(static glyph => glyph.Glyph)
            .ToArray();

        Assert.Equal(digits, drawnDigits);
    }

    [Theory]
    [InlineData("١٢٣")]
    [InlineData("٣ שלום")]
    public void AutoStillResolvesTextThatStartsWithAnArabicIndicDigitAsRightToLeft(string text)
    {
        DrawTextLayout layout = Build(text, new DrawTextLayoutOptions(direction: DrawTextDirection.Auto));

        Assert.Equal(DrawTextDirection.RightToLeft, Assert.Single(layout.Lines).Direction);
    }

    // #1149: Auto resolves the base direction once per paragraph (UAX #9 rules P2 and P3), and every wrapped line of the
    // paragraph uses it.
    [Fact]
    public void AutoGivesEveryWrappedLineOfAParagraphTheDirectionOfTheParagraph()
    {
        DrawTextLayout layout = Build("אבג דהו abc def זחט", new DrawTextLayoutOptions(
            maxWidth: 40,
            wrapping: DrawTextWrapping.Word,
            direction: DrawTextDirection.Auto));

        Assert.Equal(["אבג דהו", "abc def", "זחט"], layout.Lines.Select(static line => line.Text));
        Assert.All(layout.Lines, static line => Assert.Equal(DrawTextDirection.RightToLeft, line.Direction));
        Assert.Equal(5, layout.Lines[1].Bounds.X, Tolerance);
        Assert.Equal(5, layout.Lines[1].Runs[0].Bounds.X, Tolerance);
    }

    [Fact]
    public void AutoStillResolvesEachParagraphOfTheTextSeparately()
    {
        DrawTextLayout layout = Build("שלום\nabc", new DrawTextLayoutOptions(direction: DrawTextDirection.Auto));

        Assert.Equal(2, layout.Lines.Count);
        Assert.Equal(DrawTextDirection.RightToLeft, layout.Lines[0].Direction);
        Assert.Equal(DrawTextDirection.LeftToRight, layout.Lines[1].Direction);
    }

    // #1150: as in WPF, a line of a justified paragraph that is not stretched starts at the start edge of its direction.
    [Fact]
    public void TheUnstretchedLinesOfAJustifiedRightToLeftParagraphStartAtTheRightEdge()
    {
        DrawTextLayout layout = Build("אב גד הו זח\nטי", new DrawTextLayoutOptions(
            maxWidth: 30,
            wrapping: DrawTextWrapping.Word,
            alignment: DrawTextAlignment.Justify,
            direction: DrawTextDirection.RightToLeft));

        // Line.Text joins the runs in visual order, and the stretched first line has a run for each word and space.
        Assert.Equal(3, layout.Lines.Count);
        Assert.Equal(["הו זח", "טי"], layout.Lines.Skip(1).Select(static line => line.Text));
        Assert.Equal(0, layout.Lines[0].Bounds.X, Tolerance);
        Assert.Equal(30, layout.Lines[0].Bounds.Right, Tolerance);
        Assert.Equal(0, layout.Lines[0].Runs[0].Bounds.X, Tolerance);
        Assert.Equal(30, layout.Lines[0].Runs[^1].Bounds.Right, Tolerance);
        // "הו זח" ends the paragraph before the hard line break, and "טי" ends the text.
        Assert.Equal(5, layout.Lines[1].Bounds.X, Tolerance);
        Assert.Equal(5, layout.Lines[1].Runs[0].Bounds.X, Tolerance);
        Assert.Equal(20, layout.Lines[2].Bounds.X, Tolerance);
        Assert.Equal(20, layout.Lines[2].Runs[0].Bounds.X, Tolerance);
    }

    [Fact]
    public void TheUnstretchedLinesOfAJustifiedLeftToRightParagraphStayAtTheLeftEdge()
    {
        DrawTextLayout layout = Build("ab cd ef gh\nij", new DrawTextLayoutOptions(
            maxWidth: 30,
            wrapping: DrawTextWrapping.Word,
            alignment: DrawTextAlignment.Justify,
            direction: DrawTextDirection.LeftToRight));

        Assert.Equal(["ab cd", "ef gh", "ij"], layout.Lines.Select(static line => line.Text));
        Assert.Equal(30, layout.Lines[0].Bounds.Width, Tolerance);
        Assert.All(layout.Lines, static line => Assert.Equal(0, line.Bounds.X, Tolerance));
    }

    // #1151: justification changes the width of the spaces, not the shapes of the letters, so the letters of one word
    // are shaped together.
    [Fact]
    public void AJustifiedArabicWordIsDrawnWithTheSameJoinedGlyphsAsOnAStartLine()
    {
        IDrawFont font = new SystemFontSource().LoadFont("Segoe UI", 16);
        DrawTextLayoutOptions justify = new(
            maxWidth: 110,
            wrapping: DrawTextWrapping.Word,
            alignment: DrawTextAlignment.Justify,
            direction: DrawTextDirection.RightToLeft);
        DrawTextLayout layout = new DrawTextLayoutBuilder()
            .AddSpan(new DrawTextSpan("مرحبا بكم جميعا هنا", font, 16, black))
            .Build(justify);

        DrawTextLayoutLine line = layout.Lines[0];
        DrawTextLayoutRun? word = line.Runs.FirstOrDefault(static run => run.Text == "مرحبا");

        Assert.True(word is not null, "runs from left to right: " + string.Join("|", line.Runs.Select(static run => run.Text)));
        Assert.Equal(Shape(font, "مرحبا").GlyphIds, Shape(word.TextRun).GlyphIds);
        Assert.Equal(110, line.Bounds.Width, 0.05f);
        Assert.Equal(110, line.Runs[^1].Bounds.Right, 0.05f);
    }

    private DrawTextLayout Build(string text, DrawTextLayoutOptions options) =>
        new DrawTextLayoutBuilder()
            .AddSpan(new DrawTextSpan(text, testFont, 10, black))
            .Build(options);

    // The text of the line as it is seen from left to right: the graphemes of a right-to-left run are drawn from right to
    // left.
    private static string Visual(DrawTextLayoutLine line) =>
        string.Concat(line.Runs.Select(static run => run.Direction == DrawTextDirection.RightToLeft
            ? Reverse(run.Text)
            : run.Text));

    private static string Reverse(string text)
    {
        List<string> elements = [];
        TextElementEnumerator enumerator = StringInfo.GetTextElementEnumerator(text);
        while (enumerator.MoveNext())
        {
            elements.Add(enumerator.GetTextElement());
        }

        elements.Reverse();
        return string.Concat(elements);
    }

    private static void AssertRunsInVisualOrder(DrawTextLayoutLine line)
    {
        for (int index = 1; index < line.Runs.Count; index++)
        {
            Assert.Equal(line.Runs[index - 1].Bounds.Right, line.Runs[index].Bounds.X, Tolerance);
        }
    }

    private static TextShapeResult Shape(IDrawFont font, string text) => Shape(new DrawTextRun(font, text, 16));

    private static TextShapeResult Shape(DrawTextRun run)
    {
        Assert.True(TextShaper.Default.TryShape(run, out TextShapeResult result));
        return result;
    }

    private sealed class TestFont : IDrawFont
    {
        public string FamilyName => "BidiTest";

        public float Size => 10;
    }
}
