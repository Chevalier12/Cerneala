using Cerneala.Language.Text;

namespace Cerneala.Tests.Language;

public sealed class SourceTextTests
{
    [Fact]
    public void LineMapUsesUtf16OffsetsAndRecognizesAllLineBreakForms()
    {
        SourceText text = SourceText.From("a\r\nb\nc\rd\U0001F600");

        Assert.Equal(4, text.LineCount);
        Assert.Equal(new LinePosition(0, 1), text.GetLinePosition(1));
        Assert.Equal(new LinePosition(1, 0), text.GetLinePosition(3));
        Assert.Equal(new LinePosition(3, 3), text.GetLinePosition(text.Length));
        Assert.Equal(text.Length, text.GetOffset(new LinePosition(3, 3)));
    }

    [Fact]
    public void IncrementalChangesAreImmutableVersionedAndRejectOverlap()
    {
        SourceText original = SourceText.From("abcdef", version: 7);
        SourceText changed = original.WithChanges(
        [
            new TextChange(new TextSpan(1, 2), "B"),
            new TextChange(new TextSpan(4, 1), "E")
        ]);

        Assert.Equal("abcdef", original.ToString());
        Assert.Equal("aBdEf", changed.ToString());
        Assert.Equal(8, changed.Version);
        Assert.Throws<ArgumentException>(() => original.WithChanges(
        [
            new TextChange(new TextSpan(1, 3), "x"),
            new TextChange(new TextSpan(2, 1), "y")
        ]));
    }
}
