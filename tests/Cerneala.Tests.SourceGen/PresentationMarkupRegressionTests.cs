using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Xunit;

namespace Cerneala.Tests.SourceGen;

public sealed class PresentationMarkupRegressionTests
{
    [Fact]
    public void NavigationTemplateAnimatesItsHoverLineAndOverlayText()
    {
        string repositoryRoot = FindRepositoryRoot();
        XDocument document = XDocument.Load(Path.Combine(
            repositoryRoot,
            "CernealaPresentation",
            "PresentationWindow.cui.xml"), LoadOptions.PreserveWhitespace);

        XElement navigationAspect = Assert.Single(document
            .Descendants("Aspect")
            .Where(element => string.Equals(
                element.Attribute("Name")?.Value,
                "NavButton",
                StringComparison.Ordinal)));
        XElement hoverLine = Assert.Single(navigationAspect
            .Descendants()
            .Where(element => string.Equals(
                element.Attribute("Name")?.Value,
                "PART_HoverLine",
                StringComparison.Ordinal)));
        XElement hoverText = Assert.Single(navigationAspect
            .Descendants()
            .Where(element => string.Equals(
                element.Attribute("Name")?.Value,
                "PART_HoverText",
                StringComparison.Ordinal)));
        Assert.Null(hoverLine.Attribute("Aspect"));
        Assert.Null(hoverText.Attribute("Aspect"));
        Assert.Contains(
            "$self.parts.$PART_HoverLine.ScaleX",
            navigationAspect.Value,
            StringComparison.Ordinal);
        Assert.Contains(
            "$self.parts.$PART_HoverText.Opacity",
            navigationAspect.Value,
            StringComparison.Ordinal);
    }

    private static string FindRepositoryRoot()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Cerneala.slnx")))
        {
            directory = directory.Parent;
        }

        return Assert.IsType<DirectoryInfo>(directory).FullName;
    }
}
