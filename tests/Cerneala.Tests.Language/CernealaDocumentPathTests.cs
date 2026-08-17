using Cerneala.Language;

namespace Cerneala.Tests.Language;

public sealed class CernealaDocumentPathTests
{
    [Theory]
    [InlineData("View.crn", true)]
    [InlineData("Views/VIEW.CRN", true)]
    [InlineData("View.cui.xml", false)]
    [InlineData("View.crn.cs", false)]
    public void MarkupDetectionUsesOnlyTheCrnExtension(string path, bool expected)
    {
        Assert.Equal(expected, CernealaDocumentPath.IsMarkupFile(path));
    }

    [Theory]
    [InlineData("View.crn", "View")]
    [InlineData("Views/MainWindow.CRN", "MainWindow")]
    public void LogicalNameRemovesExactlyTheCrnExtension(string path, string expected)
    {
        Assert.Equal(expected, CernealaDocumentPath.GetLogicalName(path));
    }

    [Fact]
    public void CompanionPathAppendsCsToTheCrnDocumentPath()
    {
        Assert.Equal("Views/View.crn.cs", CernealaDocumentPath.GetCompanionPath("Views/View.crn"));
    }
}
