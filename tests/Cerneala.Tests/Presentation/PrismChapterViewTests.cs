using System.Xml.Linq;
using Cerneala.Drawing;
using Cerneala.Drawing.Prism.Catalog;
using Cerneala.Presentation;
using Cerneala.UI.Controls;
using Cerneala.UI.Core;
using Cerneala.UI.Elements;
using Cerneala.UI.Layout;
using Cerneala.UI.Layout.Panels;
using Cerneala.UI.Media;

namespace Cerneala.Tests.Presentation;

public sealed class PrismChapterViewTests
{
    [Fact]
    public void MarkupDefinesFourRenderOwningTargetsAndThreeEditorZones()
    {
        XDocument markup = XDocument.Load(RepositoryFile("CernealaPresentation", "PrismChapterView.cui.xml"));
        string[] names = markup.Descendants()
            .Select(element => (string?)element.Attribute("Name"))
            .Where(name => name is not null)
            .Cast<string>()
            .ToArray();

        Assert.Contains("PreviewMascot", names);
        Assert.Contains("PreviewMascotImage", names);
        Assert.Contains("PreviewTypography", names);
        Assert.Contains("PreviewBadge", names);
        Assert.Contains("PreviewCard", names);
        Assert.Contains("LayersHost", names);
        Assert.Contains("CatalogHost", names);
        Assert.Contains("InspectorHost", names);
        Assert.Contains("StatusFallback", names);

        string code = File.ReadAllText(RepositoryFile("CernealaPresentation", "PrismChapterView.cui.xml.cs"));
        Assert.Contains("PrismStudioTarget.Mascot => PreviewMascotImage", code, StringComparison.Ordinal);
    }

    [Fact]
    public void CatalogToolbarUsesCompactButtonsWithEnoughVerticalSpace()
    {
        XDocument markup = XDocument.Load(RepositoryFile("CernealaPresentation", "PrismChapterView.cui.xml"));
        XElement[] buttons = [
            FindNamedElement(markup, "FilterTab"),
            FindNamedElement(markup, "StyleTab"),
            FindNamedElement(markup, "CategoryButton")];

        Assert.All(buttons, button =>
            Assert.Equal("$PrismStudioCatalogButton", (string?)button.Attribute("Aspect")));

        XElement toolbar = Assert.IsType<XElement>(buttons[0].Parent);
        Assert.Equal("38", (string?)toolbar.Parent?.Elements().First(element => element.Name.LocalName == "Grid.RowDefinitions")
            .Elements().ElementAt(1).Attribute("Height"));
    }

    [Fact]
    public void PrismTextBoxesUseLightTextAndCaret()
    {
        XDocument markup = XDocument.Load(
            RepositoryFile("CernealaPresentation", "PrismChapterView.cui.xml"));
        XElement searchBox = FindNamedElement(markup, "SearchBox");
        string code = File.ReadAllText(
            RepositoryFile("CernealaPresentation", "PrismChapterView.cui.xml.cs"));

        Assert.Equal("$PaperBrush", (string?)searchBox.Attribute("Foreground"));
        Assert.Equal("#FFFFFFFF", (string?)searchBox.Attribute("CaretBrush"));
        Assert.Contains("Foreground = PaperBrush", code, StringComparison.Ordinal);
        Assert.Contains("CaretBrush = new SolidColorBrush(Color.White)", code, StringComparison.Ordinal);
    }

    [Fact]
    public void CatalogAndInspectorContractsCoverEveryOperationAndValueKind()
    {
        Assert.Equal(134, PrismCatalog.Filters.Length);
        Assert.Equal(10, PrismCatalog.Styles.Length);
        Assert.Equal(9, PrismCatalog.Filters.Count(operation => operation.RequiresResource));
        Assert.Single(PrismCatalog.Styles.Where(operation => operation.RequiresResource));

        string code = File.ReadAllText(RepositoryFile("CernealaPresentation", "PrismChapterView.cui.xml.cs"));
        foreach (PrismCatalogValueKind kind in Enum.GetValues<PrismCatalogValueKind>())
        {
            Assert.Contains($"case PrismCatalogValueKind.{kind}", code, StringComparison.Ordinal);
        }
        Assert.Contains("DetachPrism();", code, StringComparison.Ordinal);
        Assert.Contains("GeneratedMarkup.AttachPrism", code, StringComparison.Ordinal);
    }

    [Fact]
    public void PrismStudioSliderAspectLivesInMarkup()
    {
        XDocument markup = XDocument.Load(
            RepositoryFile("CernealaPresentation", "PrismStudioSlider.cui.xml"));
        XElement slider = Assert.Single(markup.Descendants("Slider"));
        XElement[] aspects = markup.Descendants("Aspect").ToArray();

        Assert.Equal("$PrismStudioSlider", (string?)slider.Attribute("Aspect"));
        Assert.Equal(3, aspects.Length);
        Assert.Equal(
            "Track",
            Assert.Single(aspects.SelectMany(aspect => aspect.Descendants()).Where(
                element => (string?)element.Attribute("Name") == "PART_Track")).Name.LocalName);
        Assert.Equal(
            "Thumb",
            Assert.Single(aspects.SelectMany(aspect => aspect.Descendants()).Where(
                element => (string?)element.Attribute("Name") == "PART_Thumb")).Name.LocalName);

        string code = File.ReadAllText(
            RepositoryFile("CernealaPresentation", "PrismChapterView.cui.xml.cs"));
        Assert.DoesNotContain("ElementAspect", code, StringComparison.Ordinal);
    }

    [Fact]
    public void DynamicSlidersUseTheMarkupComponentTemplate()
    {
        UIRoot root = new(830, 586);
        PrismChapterView view = new();
        root.VisualChildren.Add(view);
        root.ProcessFrame();
        view.AddLayerForTests();
        root.ProcessFrame();
        Slider[] sliders = Descendants(view).OfType<Slider>().ToArray();

        Assert.Equal(2, sliders.Length);
        Assert.All(sliders, slider =>
        {
            Assert.NotNull(slider.Aspect);
            Assert.Contains("PrismStudioSlider", slider.ComponentTemplate?.Name, StringComparison.Ordinal);
            Assert.Contains(
                "PrismStudioSlider",
                slider.Track.ComponentTemplate?.Name,
                StringComparison.Ordinal);
            Assert.NotNull(slider.Track.Thumb.Aspect);
            Assert.Equal(
                new Color(33, 39, 47),
                Assert.IsType<SolidColorBrush>(slider.Track.Background).Color);
            Assert.Equal(
                new Color(77, 240, 255),
                Assert.IsType<SolidColorBrush>(slider.Track.Thumb.Background).Color);
            Assert.Equal(
                UiPropertyValueSource.LocalAspectBase,
                slider.Track.GetValueSource(Control.BackgroundProperty));
            Assert.Equal(
                UiPropertyValueSource.LocalAspectBase,
                slider.Track.Thumb.GetValueSource(Control.BackgroundProperty));
            Assert.Equal(10, slider.Track.Thumb.ArrangedBounds.Width);
        });
    }

    [Fact]
    public void ActivateSwitchAndDeactivateOwnTheAttachmentLifetime()
    {
        PrismChapterView view = new();

        view.Activate();
        view.SelectTargetForTests(PrismStudioTarget.Card);
        Arrange(view, 830, 586);

        Assert.Empty(view.Model.Layers);
        Assert.False(view.HasPrismAttachment);
        Assert.Equal(134, view.VisibleCatalogCount);

        view.AddLayerForTests();
        Arrange(view, 830, 586);

        Assert.False(view.HasPrismAttachment);
        Assert.Single(view.Model.Layers);
        Assert.Empty(view.Model.Layers[0].Operations);

        view.Deactivate();

        Assert.False(view.HasPrismAttachment);
        Assert.Equal(0, view.VisibleCatalogCount);
    }

    [Fact]
    public void RepeatedTourActivationDoesNotRetainAttachmentsOrDynamicControls()
    {
        PrismChapterView view = new();
        Assert.Empty(view.Model.Layers);
        view.AddLayerForTests();
        int[] layerIds = view.Model.Layers.Select(layer => layer.Id).ToArray();

        for (int cycle = 0; cycle < 50; cycle++)
        {
            view.Activate();
            view.SelectTargetForTests((PrismStudioTarget)(cycle % 4));
            Arrange(view, 830, 586);
            Assert.False(view.HasPrismAttachment);
            view.Deactivate();
            Assert.False(view.HasPrismAttachment);
            Assert.Equal(0, view.VisibleCatalogCount);
        }

        Assert.Equal(layerIds, view.Model.Layers.Select(layer => layer.Id));
    }

    [Theory]
    [InlineData(1070, 726)]
    [InlineData(830, 586)]
    public void EditorZonesArrangeWithoutOverlapAtTourSizes(float width, float height)
    {
        PrismChapterView view = new();
        view.PrepareEditorForTests();

        Arrange(view, width, height);

        UIElement[] elements = Descendants(view).ToArray();
        LayoutRect preview = Assert.Single(elements.OfType<SvgImage>()).ArrangedBounds;
        LayoutRect[] zones = elements.OfType<PrismStudioScrollHost>()
            .Select(host => host.ArrangedBounds)
            .ToArray();
        PrismStudioScrollHost layersHost = elements.OfType<PrismStudioScrollHost>().First();

        Assert.True(preview.Width > 0 && preview.Height > 0);
        Assert.Equal(3, zones.Length);
        Assert.All(zones, zone => Assert.True(zone.Width > 0 && zone.Height > 0));
        for (int left = 0; left < zones.Length; left++)
        {
            for (int right = left + 1; right < zones.Length; right++)
            {
                Assert.False(Overlaps(zones[left], zones[right]));
            }
        }
        Assert.All(zones, zone => Assert.True(zone.X + zone.Width <= width && zone.Y + zone.Height <= height));
        Assert.All(
            Descendants(layersHost).OfType<Button>(),
            button => Assert.True(button.ArrangedBounds.X + button.ArrangedBounds.Width <= layersHost.ArrangedBounds.X + layersHost.ArrangedBounds.Width));
    }

    [Fact]
    public void EditorContentFollowsANonZeroHostOrigin()
    {
        PrismChapterView view = new();
        view.PrepareEditorForTests();
        const float originX = 313;
        const float originY = 86;

        view.Measure(new MeasureContext(new LayoutSize(1070, 726)));
        view.Arrange(new ArrangeContext(new LayoutRect(originX, originY, 1070, 726)));

        SvgImage mascot = Assert.Single(Descendants(view).OfType<SvgImage>());
        Border target = Assert.IsType<Border>(mascot.VisualParent?.VisualParent);
        Assert.True(target.ArrangedBounds.X >= originX);
        Assert.True(target.ArrangedBounds.Y >= originY);
    }

    [Fact]
    public void ExpandingTheEditorFromCollapsedRepositionsItsVisualSubtree()
    {
        UIRoot root = new(1650, 1024);
        Grid host = new();
        host.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Pixels(313)));
        host.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Stars(1)));
        PrismChapterView view = new() { Visibility = Visibility.Collapsed };
        Grid.SetColumn(view, 1);
        root.VisualChildren.Add(host);
        host.VisualChildren.Add(view);
        root.ProcessFrame();

        view.Visibility = Visibility.Visible;
        view.PrepareEditorForTests();
        root.ProcessFrame();

        SvgImage mascot = Assert.Single(Descendants(view).OfType<SvgImage>());
        Border target = Assert.IsType<Border>(mascot.VisualParent?.VisualParent);
        Assert.True(
            target.ArrangedBounds.X >= 313,
            $"host={host.ArrangedBounds}; view={view.ArrangedBounds}; template={view.VisualChildren[0].ArrangedBounds}; target={target.ArrangedBounds}; mascot={mascot.ArrangedBounds}");
    }

    private static string RepositoryFile(params string[] path)
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Cerneala.slnx")))
        {
            directory = directory.Parent;
        }

        Assert.NotNull(directory);
        return Path.Combine([directory.FullName, .. path]);
    }

    private static XElement FindNamedElement(XDocument document, string name) =>
        Assert.Single(document.Descendants().Where(element => (string?)element.Attribute("Name") == name));

    private static bool Overlaps(LayoutRect left, LayoutRect right) =>
        left.X < right.X + right.Width &&
        left.X + left.Width > right.X &&
        left.Y < right.Y + right.Height &&
        left.Y + left.Height > right.Y;

    private static void Arrange(PrismChapterView view, float width, float height)
    {
        view.Measure(new MeasureContext(new LayoutSize(width, height)));
        view.Arrange(new ArrangeContext(new LayoutRect(0, 0, width, height)));
    }

    private static IEnumerable<UIElement> Descendants(UIElement element)
    {
        foreach (UIElement child in element.VisualChildren)
        {
            yield return child;
            foreach (UIElement descendant in Descendants(child))
            {
                yield return descendant;
            }
        }
    }
}
