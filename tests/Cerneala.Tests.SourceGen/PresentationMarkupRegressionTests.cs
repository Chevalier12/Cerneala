using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Cerneala.UI.Invalidation;
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

    [Fact]
    public void RuntimeDiagnosticsLiveInTheHeaderInsteadOfATourChapter()
    {
        string repositoryRoot = FindRepositoryRoot();
        string presentationRoot = Path.Combine(repositoryRoot, "CernealaPresentation");
        XDocument document = XDocument.Load(
            Path.Combine(presentationRoot, "PresentationWindow.cui.xml"),
            LoadOptions.PreserveWhitespace);
        string[] names = document.Descendants()
            .Select(element => element.Attribute("Name")?.Value)
            .Where(name => name is not null)
            .Cast<string>()
            .ToArray();

        Assert.False(File.Exists(Path.Combine(presentationRoot, "DiagnosticsChapterView.cui.xml")));
        Assert.False(File.Exists(Path.Combine(presentationRoot, "DiagnosticsChapterView.cui.xml.cs")));
        Assert.DoesNotContain("NavDiagnostics", names);
        Assert.DoesNotContain("PageDiagnostics", names);
        Assert.Contains("HeaderDiagFrame", names);
        Assert.Contains("HeaderDiagPhases", names);
        Assert.Contains("HeaderDiagLayout", names);
        Assert.Contains("HeaderDiagRender", names);
        Assert.Contains("HeaderDiagMotion", names);
        Assert.Contains("HeaderDiagRelay", names);

        XElement chapterCounter = Assert.Single(document.Descendants().Where(
            element => element.Attribute("Name")?.Value == "ChapterCounter"));
        Assert.Equal("CHAPTER 01 / 07", chapterCounter.Attribute("Text")?.Value);

        string code = File.ReadAllText(Path.Combine(
            presentationRoot,
            "PresentationWindow.cui.xml.cs"));
        Assert.DoesNotContain("PresentationChapter.Diagnostics", code, StringComparison.Ordinal);
        foreach (string counter in typeof(FrameStats)
            .GetProperties()
            .Where(property => property.PropertyType == typeof(int))
            .Select(property => property.Name))
        {
            Assert.Contains($"frame.Stats.{counter}", code, StringComparison.Ordinal);
        }
        Assert.Contains("frame.Stats.HasWork", code, StringComparison.Ordinal);
        Assert.Contains("frame.ProcessingTime", code, StringComparison.Ordinal);
    }

    [Fact]
    public void PresentationScrollViewersUseTheApplicationScrollAspect()
    {
        string repositoryRoot = FindRepositoryRoot();
        XDocument application = XDocument.Load(Path.Combine(
            repositoryRoot,
            "CernealaPresentation",
            "App.cui.xml"), LoadOptions.PreserveWhitespace);
        XElement[] aspects = application.Descendants("Aspect").ToArray();
        XElement scrollViewerAspect = Assert.Single(aspects.Where(element =>
            element.Attribute("Name") is null &&
            element.Attribute("Target")?.Value == "ScrollViewer"));
        XElement scrollBarAspect = Assert.Single(aspects.Where(element =>
            element.Attribute("Name")?.Value == "PresentationScrollBar"));
        XElement scrollTrackAspect = Assert.Single(aspects.Where(element =>
            element.Attribute("Name")?.Value == "PresentationScrollTrack"));
        XElement scrollThumbAspect = Assert.Single(aspects.Where(element =>
            element.Attribute("Name")?.Value == "PresentationScrollThumb"));

        string[] viewerParts = scrollViewerAspect.Descendants()
            .Select(element => element.Attribute("Name")?.Value)
            .Where(name => name is not null)
            .Cast<string>()
            .ToArray();
        Assert.Contains("PART_ScrollContentPresenter", viewerParts);
        Assert.Contains("PART_VerticalScrollBar", viewerParts);
        Assert.Contains("PART_HorizontalScrollBar", viewerParts);
        Assert.Equal("Track", Assert.Single(scrollBarAspect.Descendants("Track")).Name.LocalName);
        Assert.Equal("Thumb", Assert.Single(scrollTrackAspect.Descendants("Thumb")).Name.LocalName);
        Assert.Contains("$CyanBrush", scrollThumbAspect.Value, StringComparison.Ordinal);
        Assert.Empty(scrollBarAspect.Descendants("RepeatButton"));
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
