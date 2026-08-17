using System.Xml.Linq;
using Cerneala.Drawing;
using Cerneala.Drawing.Prism.Catalog;
using Cerneala.Presentation;
using Cerneala.UI;
using Cerneala.UI.Controls;
using Cerneala.UI.Core;
using Cerneala.UI.Elements;
using Cerneala.UI.Hosting.Windows;
using Cerneala.UI.Layout;
using Cerneala.UI.Layout.Panels;
using Cerneala.UI.Media;
using Cerneala.Tests.UI.Hosting;

namespace Cerneala.Tests.Presentation;

[Collection(WindowRuntimeTestCollection.Name)]
public sealed class PrismChapterViewTests : IDisposable
{
    public PrismChapterViewTests()
    {
        Application.ResetForTesting();
        WindowApplicationRuntime.ResetForTesting();
        WindowApplicationRuntime runtime = WindowApplicationRuntime.CurrentOrDefault;
        App app = new();
        app.Install(runtime);
    }

    public void Dispose()
    {
        WindowApplicationRuntime.ResetForTesting();
        Application.ResetForTesting();
    }

    [Fact]
    public void MarkupDefinesFourRenderOwningTargetsAndThreeEditorZones()
    {
        XDocument markup = XDocument.Load(RepositoryFile("CernealaPresentation", "PrismChapterView.crn"));
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

        string code = File.ReadAllText(RepositoryFile("CernealaPresentation", "PrismChapterView.crn.cs"));
        Assert.Contains("PrismStudioTarget.Mascot => PreviewMascotImage", code, StringComparison.Ordinal);
        Assert.DoesNotContain("internal sealed class PrismStudio", code, StringComparison.Ordinal);
    }

    [Fact]
    public void CatalogToolbarUsesCompactButtonsWithEnoughVerticalSpace()
    {
        XDocument markup = XDocument.Load(RepositoryFile("CernealaPresentation", "PrismChapterView.crn"));
        XElement[] buttons = [
            FindNamedElement(markup, "FilterTab"),
            FindNamedElement(markup, "StyleTab"),
            FindNamedElement(markup, "CategoryButton")];

        Assert.Equal("$PrismStudioSelectionButton", (string?)buttons[0].Attribute("Aspect"));
        Assert.Equal("$PrismStudioSelectionButton", (string?)buttons[1].Attribute("Aspect"));
        Assert.Equal("$PrismStudioCatalogButton", (string?)buttons[2].Attribute("Aspect"));

        XElement toolbar = Assert.IsType<XElement>(buttons[0].Parent);
        Assert.Equal("38", (string?)toolbar.Parent?.Elements().First(element => element.Name.LocalName == "Grid.RowDefinitions")
            .Elements().ElementAt(1).Attribute("Height"));
    }

    [Fact]
    public void PrismTextBoxesUseLightTextAndCaret()
    {
        XDocument markup = XDocument.Load(
            RepositoryFile("CernealaPresentation", "PrismChapterView.crn"));
        XElement searchBox = FindNamedElement(markup, "SearchBox");
        XElement textBoxAspect = Assert.Single(markup.Descendants("Aspect").Where(
            aspect => (string?)aspect.Attribute("Name") == "PrismStudioTextBox"));

        Assert.Equal("$PaperBrush", (string?)searchBox.Attribute("Foreground"));
        Assert.Equal("#FFFFFFFF", (string?)searchBox.Attribute("CaretBrush"));
        Assert.Equal("TextBox", (string?)textBoxAspect.Attribute("TargetType"));
        Assert.Contains("Foreground = \"#FFE8EBE8\"", textBoxAspect.Value, StringComparison.Ordinal);
        Assert.Contains("CaretBrush = \"#FFFFFFFF\"", textBoxAspect.Value, StringComparison.Ordinal);
    }

    [Fact]
    public void CatalogAndInspectorContractsCoverEveryOperationAndValueKind()
    {
        Assert.Equal(134, PrismCatalog.Filters.Length);
        Assert.Equal(10, PrismCatalog.Styles.Length);
        Assert.Equal(11, PrismCatalog.Filters.Count(operation => operation.RequiresResource));
        Assert.Single(PrismCatalog.Styles.Where(operation => operation.RequiresResource));

        string code = File.ReadAllText(RepositoryFile("CernealaPresentation", "PrismChapterView.crn.cs"));
        foreach (PrismCatalogValueKind kind in Enum.GetValues<PrismCatalogValueKind>())
        {
            Assert.Contains($"PrismCatalogValueKind.{kind}", code, StringComparison.Ordinal);
        }
        Assert.Contains("DetachPrism();", code, StringComparison.Ordinal);
        Assert.Contains("GeneratedMarkup.AttachPrism", code, StringComparison.Ordinal);
    }

    [Fact]
    public void PrismStudioSliderAspectLivesInChapterMarkupWithoutAWrapperControl()
    {
        XDocument markup = XDocument.Load(
            RepositoryFile("CernealaPresentation", "PrismChapterView.crn"));
        XElement[] aspects = markup.Descendants("Aspect")
            .Where(aspect => ((string?)aspect.Attribute("Name"))?.StartsWith(
                "PrismStudioSlider",
                StringComparison.Ordinal) == true)
            .ToArray();

        Assert.Equal(3, aspects.Length);
        Assert.Equal(
            "Track",
            Assert.Single(aspects.SelectMany(aspect => aspect.Descendants()).Where(
                element => (string?)element.Attribute("Name") == "PART_Track")).Name.LocalName);
        Assert.Equal(
            "Thumb",
            Assert.Single(aspects.SelectMany(aspect => aspect.Descendants()).Where(
                element => (string?)element.Attribute("Name") == "PART_Thumb")).Name.LocalName);

        Assert.NotEmpty(markup.Descendants("Slider").Where(
            slider => (string?)slider.Attribute("Aspect") == "$PrismStudioSlider"));
        string code = File.ReadAllText(
            RepositoryFile("CernealaPresentation", "PrismChapterView.crn.cs"));
        Assert.DoesNotContain("new Slider", code, StringComparison.Ordinal);
        Assert.DoesNotContain("PrismStudioSlider sliderView", code, StringComparison.Ordinal);
        Assert.DoesNotContain("ElementAspect", code, StringComparison.Ordinal);
        Assert.False(File.Exists(
            RepositoryFile("CernealaPresentation", "PrismStudioSlider.crn")));
        Assert.False(File.Exists(
            RepositoryFile("CernealaPresentation", "PrismStudioSlider.crn.cs")));
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
            Assert.DoesNotContain("Slider.Default", slider.ComponentTemplate?.Name, StringComparison.Ordinal);
            Assert.DoesNotContain("Track.Default", slider.Track.ComponentTemplate?.Name, StringComparison.Ordinal);
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
    public void InspectorAutoScrollbarAppearsWhenDynamicControlsOverflowAfterInitialLayout()
    {
        UIRoot root = new(487, 663);
        PrismChapterView view = new();
        root.VisualChildren.Add(view);
        root.ProcessFrame();
        ScrollViewer inspector = Descendants(view)
            .OfType<ScrollViewer>()
            .Last();
        Assert.False(inspector.IsVerticalScrollBarVisible);

        view.AddLayerForTests();
        PrismStudioLayer layer = Assert.Single(view.Model.Layers);
        Assert.True(view.Model.AddOperation(
            layer.Id,
            PrismCatalog.GetFilter(PrismFilterId.AccentedEdges)));
        view.Deactivate();
        view.Activate();
        root.ProcessFrame();

        Assert.True(inspector.Presenter.ExtentHeight > inspector.Presenter.ViewportHeight);
        Assert.True(inspector.IsVerticalScrollBarVisible);
        Assert.True(inspector.VerticalScrollBar.ArrangedBounds.Height > 0);
    }

    [Fact]
    public void EditorScrollHostsUseTheApplicationScrollViewerAspect()
    {
        UIRoot root = new(830, 586);
        root.SetResourceProvider(Application.Current!.Resources);
        PrismChapterView view = new();
        root.VisualChildren.Add(view);
        root.ProcessFrame();
        ScrollViewer[] hosts = Descendants(view)
            .OfType<ScrollViewer>()
            .ToArray();

        Assert.Equal(3, hosts.Length);
        Assert.All(hosts, host =>
        {
            Assert.NotNull(host.ComponentTemplate);
            Assert.NotSame(ScrollViewerTemplates.Default, host.ComponentTemplate);
        });
    }

    [Fact]
    public void InspectorUsesComboBoxesForEveryDiscreteChoice()
    {
        PrismChapterView view = new();
        PrismStudioLayer layer = view.Model.AddLayer();
        PrismCatalogOperationInfo filter = PrismCatalog.Filters.First(operation =>
            !operation.RequiresResource &&
            operation.Parameters.Any(parameter => parameter.ValueKind == PrismCatalogValueKind.Symbol));
        Assert.True(view.Model.AddOperation(layer.Id, filter));
        view.PrepareEditorForTests();

        Arrange(view, 830, 586);

        ComboBox[] comboBoxes = Descendants(view).OfType<ComboBox>().ToArray();
        int symbolParameterCount = filter.Parameters.Count(
            parameter => parameter.ValueKind == PrismCatalogValueKind.Symbol);
        Assert.Equal(2 + symbolParameterCount, comboBoxes.Length);
        Assert.DoesNotContain(
            "CreateCycleButton",
            File.ReadAllText(RepositoryFile("CernealaPresentation", "PrismChapterView.crn.cs")),
            StringComparison.Ordinal);

        PrismBlendMode nextBlendMode = Assert.IsType<PrismBlendMode>(
            Enumerable.Range(0, comboBoxes[0].ItemCount)
                .Select(comboBoxes[0].GetItemAt)
                .First(item => !Equals(item, layer.BlendMode)));
        comboBoxes[0].SelectedIndex = Enumerable.Range(0, comboBoxes[0].ItemCount)
            .First(index => Equals(comboBoxes[0].GetItemAt(index), nextBlendMode));

        Assert.Equal(nextBlendMode, layer.BlendMode);
    }

    [Fact]
    public void OpenInspectorComboBoxRendersItsBlendModeItems()
    {
        UIRoot root = new(1650, 1004);
        PrismChapterView view = new();
        root.VisualChildren.Add(view);
        root.ProcessFrame();
        view.AddLayerForTests();
        root.ProcessFrame();
        ComboBox comboBox = Assert.Single(Descendants(view).OfType<ComboBox>());
        Color paper = new(232, 235, 232);
        Color line = new(52, 60, 70);
        Color panel = new(20, 24, 30);
        Assert.Equal(
            UiPropertyValueSource.LocalAspectBase,
            comboBox.GetValueSource(Control.BackgroundProperty));
        Assert.Equal(
            UiPropertyValueSource.LocalAspectBase,
            comboBox.GetValueSource(Control.ForegroundProperty));
        Assert.Equal(
            panel,
            Assert.IsType<SolidColorBrush>(comboBox.Background).Color);
        Assert.Equal(
            paper,
            Assert.IsType<SolidColorBrush>(comboBox.Foreground).Color);
        Assert.Equal(
            line,
            Assert.IsType<SolidColorBrush>(comboBox.BorderBrush).Color);
        comboBox.IsDropDownOpen = true;
        root.ProcessFrame();

        Overlay overlay = Assert.IsType<Overlay>(
            comboBox.ComponentTemplateInstance!.Parts["PART_DropDownOverlay"]);
        Assert.True(overlay.ProjectedPresenter.ArrangedBounds.Height > 0);
        Border dropDownBorder = Assert.IsType<Border>(overlay.Content);
        Assert.Equal(
            panel,
            Assert.IsType<SolidColorBrush>(dropDownBorder.Background).Color);
        ScrollViewer scrollViewer = Assert.IsType<ScrollViewer>(dropDownBorder.Child);
        Assert.True(scrollViewer.Presenter.ExtentHeight > scrollViewer.Presenter.ViewportHeight);
        Assert.True(scrollViewer.IsVerticalScrollBarVisible);
        Assert.True(scrollViewer.VerticalScrollBar.ArrangedBounds.Width > 0);
        Assert.True(scrollViewer.VerticalScrollBar.ArrangedBounds.Height > 0);
        ItemsPresenter itemsPresenter = Assert.IsType<ItemsPresenter>(
            comboBox.ComponentTemplateInstance.Parts["PART_ItemsPresenter"]);
        Assert.NotNull(itemsPresenter.LayoutPanelRoot);
        Assert.NotEmpty(itemsPresenter.LayoutPanelRoot.VisualChildren);
        TextBlock[] itemText = Descendants(overlay.ProjectedPresenter).OfType<TextBlock>().ToArray();
        Assert.Contains(itemText, text => text.Text == nameof(PrismBlendMode.Multiply));
        TextBlock multiplyText = Assert.Single(
            itemText.Where(text => text.Text == nameof(PrismBlendMode.Multiply)));
        Assert.True(multiplyText.IsAttached);
        Assert.True(multiplyText.IsVisible);
        Assert.Equal(Visibility.Visible, multiplyText.Visibility);
        Assert.NotNull(multiplyText.Foreground);
        Assert.True(
            root.RetainedRenderCache.GetElementCache(multiplyText).IsValid,
            $"dirty={multiplyText.DirtyState}; bounds={multiplyText.ArrangedBounds}");
        Assert.Contains(
            root.RetainedRenderCache.GetElementCache(multiplyText).Commands,
            command => command.Kind == DrawCommandKind.DrawText &&
                command.Text == nameof(PrismBlendMode.Multiply));
        string[] renderedText = root.RetainedRenderer.Commit(root)
            .Where(command => command.Kind == DrawCommandKind.DrawText)
            .Select(command => command.Text)
            .Where(text => text is not null)
            .Cast<string>()
            .ToArray();
        Assert.Contains(nameof(PrismBlendMode.Multiply), renderedText);
        Assert.True(itemText.Length < comboBox.ItemCount);
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
        LayoutRect[] zones = elements.OfType<ScrollViewer>()
            .Select(host => host.ArrangedBounds)
            .ToArray();
        ScrollViewer layersHost = elements.OfType<ScrollViewer>().First();

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
