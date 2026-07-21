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

    [Fact]
    public void SolarSystemDogfoodUsesThePublicReusablePrismMarkupContract()
    {
        string repositoryRoot = FindRepositoryRoot();
        string markupPath = Path.Combine(
            repositoryRoot,
            "CernealaPresentation",
            "SolarSystemChapterView.cui.xml");
        XDocument document = XDocument.Load(markupPath, LoadOptions.PreserveWhitespace);

        XElement composition = Assert.Single(document
            .Descendants("PrismComposition")
            .Where(element => string.Equals(
                element.Attribute("Name")?.Value,
                "PlanetCardPrism",
                StringComparison.Ordinal)));
        string compositionBody = composition.Value;
        Assert.Contains("@layer SignalPulse", compositionBody, StringComparison.Ordinal);
        Assert.Contains("@group CardTreatment", compositionBody, StringComparison.Ordinal);
        Assert.Contains("@filter BrightnessContrast", compositionBody, StringComparison.Ordinal);
        Assert.Contains("@style OuterGlow", compositionBody, StringComparison.Ordinal);
        Assert.Contains("@mask", compositionBody, StringComparison.Ordinal);
        Assert.Contains("Image = $PlanetCardMask", compositionBody, StringComparison.Ordinal);
        Assert.Contains("@backdrop SpaceGlass", compositionBody, StringComparison.Ordinal);
        Assert.True(
            compositionBody.IndexOf("@backdrop SpaceGlass", StringComparison.Ordinal) >
            compositionBody.IndexOf("@group CardTreatment", StringComparison.Ordinal));

        XElement maskBrush = Assert.Single(document
            .Descendants("ImageBrush")
            .Where(element => string.Equals(
                element.Attribute("Name")?.Value,
                "PlanetCardMask",
                StringComparison.Ordinal)));
        Assert.EndsWith(
            "cerneala-mascot-void-suction-well.png",
            maskBrush.Attribute("Source")?.Value,
            StringComparison.Ordinal);

        XElement planetCard = Assert.Single(document
            .Descendants("Border")
            .Where(element => string.Equals(
                element.Attribute("Name")?.Value,
                "PlanetInfoCard",
                StringComparison.Ordinal)));
        Assert.Contains("@prism $PlanetCardPrism;", planetCard.Value, StringComparison.Ordinal);

        XElement motionClip = Assert.Single(document
            .Descendants("MotionClip")
            .Where(element => string.Equals(
                element.Attribute("Name")?.Value,
                "PlanetCardPulse",
                StringComparison.Ordinal)));
        Assert.Contains(
            "$PlanetInfoCard.prism.SignalPulse.Opacity",
            motionClip.Value,
            StringComparison.Ordinal);

        string codeBehind = File.ReadAllText(markupPath + ".cs");
        Assert.DoesNotContain("OnRender", codeBehind, StringComparison.Ordinal);
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
