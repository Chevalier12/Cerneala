using Cerneala.UI.Controls;
using Cerneala.UI.Layout;
using Cerneala.UI.Text;

namespace Cerneala.Tests.UI.Text;

public sealed class BidiTextServiceTests
{
    [Fact]
    public void BaseDirectionUsesFirstStrongCharacter()
    {
        BidiTextService service = new();

        Assert.Equal(TextDirection.LeftToRight, service.GetBaseDirection("  hello"));
        Assert.Equal(TextDirection.RightToLeft, service.GetBaseDirection("  \u05E9\u05DC\u05D5\u05DD"));
    }

    [Theory]
    [InlineData("ب")]
    [InlineData("ـب")]
    [InlineData("ـ")]
    [InlineData("\U0001E900")]
    [InlineData("\U0001E922")]
    [InlineData("\U0001E94B")]
    [InlineData("\U00010C80")]
    public void BaseDirectionOfALetterInARightToLeftBlockIsRightToLeftForEveryLetterCategory(string text)
    {
        Assert.Equal(TextDirection.RightToLeft, BidiTextService.Default.GetBaseDirection(text));
    }

    [Theory]
    [InlineData("ʰ")]
    [InlineData("々")]
    [InlineData("A")]
    public void BaseDirectionOfALetterOutsideARightToLeftBlockIsLeftToRight(string text)
    {
        Assert.Equal(TextDirection.LeftToRight, BidiTextService.Default.GetBaseDirection(text));
    }

    [Fact]
    public void DirectionalRunsGroupAdjacentStrongText()
    {
        BidiTextService service = new();

        IReadOnlyList<BidiTextRun> runs = service.GetDirectionalRuns("abc \u05E9\u05DC\u05D5\u05DD");

        Assert.Equal(2, runs.Count);
        Assert.Equal(new BidiTextRun(0, 4, TextDirection.LeftToRight), runs[0]);
        Assert.Equal(new BidiTextRun(4, 4, TextDirection.RightToLeft), runs[1]);
        Assert.True(service.ContainsRightToLeft("abc \u05E9\u05DC\u05D5\u05DD"));
    }

    // Issue #1146: a lone surrogate is not a letter or a decimal digit, so it is neutral and the next letter decides. The
    // texts are built in the test: xUnit replaces a lone surrogate in InlineData with U+FFFD when it serializes the case.
    [Theory]
    [InlineData(0xD800)]
    [InlineData(0xDC00)]
    public void BaseDirectionSkipsALoneSurrogateAsNeutral(int surrogate)
    {
        string text = (char)surrogate + "a";

        Assert.Equal(TextDirection.LeftToRight, BidiTextService.Default.GetBaseDirection(text));
    }

    [Theory]
    [InlineData(0xD800, "", "a")]
    [InlineData(0xD800, "a", "b")]
    [InlineData(0xD800, "a", "")]
    [InlineData(0xDC00, "", "a")]
    [InlineData(0xDC00, "a", "b")]
    [InlineData(0xDC00, "a", "")]
    public void DirectionalRunsOfTextWithALoneSurrogateAreOneLeftToRightRun(int surrogate, string before, string after)
    {
        string text = before + (char)surrogate + after;

        BidiTextRun run = Assert.Single(BidiTextService.Default.GetDirectionalRuns(text));

        Assert.Equal(new BidiTextRun(0, text.Length, TextDirection.LeftToRight), run);
        Assert.False(BidiTextService.Default.ContainsRightToLeft(text));
    }

    [Theory]
    [InlineData(0xD800, "", "a")]
    [InlineData(0xDC00, "a", "")]
    public void TextBlockMeasuresTextWithALoneSurrogate(int surrogate, string before, string after)
    {
        TextBlock textBlock = new() { Text = before + (char)surrogate + after };

        Exception? exception = Record.Exception(() => textBlock.Measure(new MeasureContext(new LayoutSize(100, 100))));

        Assert.Null(exception);
    }
}
