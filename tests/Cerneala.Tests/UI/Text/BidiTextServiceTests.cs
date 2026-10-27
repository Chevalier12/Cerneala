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
}
