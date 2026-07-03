using Cerneala.UI.Markup;

namespace Cerneala.Tests.UI.Markup;

public sealed class UiMarkupReaderTests
{
    [Fact]
    public void ReadPreservesAttributesTextAndChildOrder()
    {
        UiMarkupReader reader = new();

        MarkupResult<UiMarkupDocument> result = reader.Read("<StackPanel Name=\"Root\"><TextBlock Text=\"A\" />direct<Button>Click</Button></StackPanel>");

        Assert.False(result.HasErrors);
        Assert.NotNull(result.Value?.Root);
        UiMarkupNode root = result.Value.Root;
        Assert.Equal("StackPanel", root.Name);
        Assert.Equal("Name", root.Attributes[0].Name);
        Assert.Equal("Root", root.Attributes[0].Value);
        Assert.Equal("direct", root.Text);
        Assert.Equal(["TextBlock", "Button"], root.Children.Select(child => child.Name));
        Assert.Equal("Click", root.Children[1].Text);
    }

    [Fact]
    public void ReadMalformedXmlReturnsDiagnostic()
    {
        UiMarkupReader reader = new();

        MarkupResult<UiMarkupDocument> result = reader.Read("<StackPanel>");

        Assert.True(result.HasErrors);
        MarkupDiagnostic diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal(MarkupDiagnosticSeverity.Error, diagnostic.Severity);
        Assert.Equal("MARKUP002", diagnostic.Code);
        Assert.True(diagnostic.HasSourceLocation);
    }

    [Fact]
    public void ReadEmptyMarkupReportsMissingRoot()
    {
        UiMarkupReader reader = new();

        MarkupResult<UiMarkupDocument> result = reader.Read(" ");

        Assert.True(result.HasErrors);
        Assert.Equal("MARKUP001", Assert.Single(result.Diagnostics).Code);
        Assert.Null(result.Value?.Root);
    }
}
