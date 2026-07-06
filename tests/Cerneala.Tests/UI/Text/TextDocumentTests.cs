using Cerneala.UI.Text;

namespace Cerneala.Tests.UI.Text;

public sealed class TextDocumentTests
{
    [Fact]
    public void ValidateRangeRejectsOverflowingLength()
    {
        TextDocument document = new("abc");

        Assert.Throws<ArgumentOutOfRangeException>(() => document.ValidateRange(1, int.MaxValue));
    }
}
