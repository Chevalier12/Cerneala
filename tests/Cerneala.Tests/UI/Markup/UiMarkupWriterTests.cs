using Cerneala.UI.Markup;

namespace Cerneala.Tests.UI.Markup;

public sealed class UiMarkupWriterTests
{
    [Fact]
    public void WriteSerializesDocumentDeterministically()
    {
        UiMarkupDocument document = UiMarkupDocument.FromRoot(new UiMarkupNode(
            "StackPanel",
            [new UiMarkupAttribute("Name", "Root"), new UiMarkupAttribute("Tag", "One")],
            [
                new UiMarkupNode("TextBlock", [new UiMarkupAttribute("Text", "Hello")]),
                new UiMarkupNode("Button", text: "Click")
            ]));
        UiMarkupWriter writer = new();

        MarkupResult<string> first = writer.Write(document);
        MarkupResult<string> second = writer.Write(document);

        Assert.False(first.HasErrors);
        Assert.Equal(first.Value, second.Value);
        Assert.Equal("<StackPanel Name=\"Root\" Tag=\"One\"><TextBlock Text=\"Hello\" /><Button>Click</Button></StackPanel>", first.Value);
    }

    [Fact]
    public void WriteMissingRootReturnsDiagnostic()
    {
        UiMarkupWriter writer = new();

        MarkupResult<string> result = writer.Write(new UiMarkupDocument(null));

        Assert.True(result.HasErrors);
        Assert.Null(result.Value);
        Assert.Equal("MARKUP010", Assert.Single(result.Diagnostics).Code);
    }

    [Fact]
    public void WritePreservesReaderTextOrderAfterLeadingChild()
    {
        UiMarkupReader reader = new();
        UiMarkupWriter writer = new();
        MarkupResult<UiMarkupDocument> read = reader.Read("<StackPanel><TextBlock />direct<Button /></StackPanel>");

        MarkupResult<string> written = writer.Write(read.Value!);

        Assert.False(written.HasErrors);
        Assert.Equal("<StackPanel><TextBlock />direct<Button /></StackPanel>", written.Value);
    }
}
