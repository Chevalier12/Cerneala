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
