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

        XElement cardComposition = Assert.Single(document
            .Descendants("PrismComposition")
            .Where(element => string.Equals(
                element.Attribute("Name")?.Value,
                "PlanetCardPrism",
                StringComparison.Ordinal)));
        string cardCompositionBody = cardComposition.Value;
        Assert.DoesNotContain("@layer SignalPulse", cardCompositionBody, StringComparison.Ordinal);
        Assert.Contains("@group CardTreatment", cardCompositionBody, StringComparison.Ordinal);
        Assert.Contains("@filter BrightnessContrast", cardCompositionBody, StringComparison.Ordinal);
        Assert.DoesNotContain("@style OuterGlow", cardCompositionBody, StringComparison.Ordinal);
        Assert.Contains("@backdrop SpaceGlass", cardCompositionBody, StringComparison.Ordinal);
        Assert.True(
            cardCompositionBody.IndexOf("@backdrop SpaceGlass", StringComparison.Ordinal) >
            cardCompositionBody.IndexOf("@group CardTreatment", StringComparison.Ordinal));

        XElement pulseComposition = Assert.Single(document
            .Descendants("PrismComposition")
            .Where(element => string.Equals(
                element.Attribute("Name")?.Value,
                "PlanetCardPulsePrism",
                StringComparison.Ordinal)));
        Assert.Contains("@layer SignalPulse", pulseComposition.Value, StringComparison.Ordinal);
        Assert.Contains("@style OuterGlow", pulseComposition.Value, StringComparison.Ordinal);

        XElement planetCard = Assert.Single(document
            .Descendants("Border")
            .Where(element => string.Equals(
                element.Attribute("Name")?.Value,
                "PlanetInfoCard",
                StringComparison.Ordinal)));
        Assert.Contains("@prism $PlanetCardPrism;", planetCard.Value, StringComparison.Ordinal);

        XElement pulseFrame = Assert.Single(document
            .Descendants("Border")
            .Where(element => string.Equals(
                element.Attribute("Name")?.Value,
                "PlanetInfoPulseFrame",
                StringComparison.Ordinal)));
        Assert.Empty(pulseFrame.Elements());
        Assert.Contains("@prism $PlanetCardPulsePrism;", pulseFrame.Value, StringComparison.Ordinal);

        XElement motionClip = Assert.Single(document
            .Descendants("MotionClip")
            .Where(element => string.Equals(
                element.Attribute("Name")?.Value,
                "PlanetCardPulse",
                StringComparison.Ordinal)));
        Assert.Contains(
            "$PlanetInfoPulseFrame.prism.SignalPulse.Opacity",
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
