using System.Xml.Linq;
using Cerneala.Drawing;
using Cerneala.Presentation;
using Cerneala.UI;
using Cerneala.UI.Automation;
using Cerneala.UI.Controls;
using Cerneala.UI.Controls.Primitives;
using Cerneala.UI.Core;
using Cerneala.UI.Elements;
using Cerneala.UI.Hosting;
using Cerneala.UI.Hosting.Windows;
using Cerneala.UI.Input;
using Cerneala.UI.Invalidation;
using Cerneala.UI.Layout;
using Cerneala.UI.Layout.Panels;
using Cerneala.UI.Media;
using Cerneala.UI.Text;
using Cerneala.Tests.UI.Hosting;
using SvgPath = Cerneala.UI.Controls.Shapes.SvgPath;

namespace Cerneala.Tests.Presentation;

[Collection(WindowRuntimeTestCollection.Name)]
public sealed class AspectChapterViewTests : IDisposable
{
    public AspectChapterViewTests()
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
    public void MarkupDefinesThreeElementsPreviewAndPropertyInspector()
    {
        XDocument markup = XDocument.Load(RepositoryFile("CernealaPresentation", "AspectChapterView.cui.xml"));
        string[] names = markup.Descendants()
            .Select(element => (string?)element.Attribute("Name"))
            .Where(name => name is not null)
            .Cast<string>()
            .ToArray();

        Assert.Contains("BorderElementButton", names);
        Assert.Contains("TextBlockElementButton", names);
        Assert.Contains("ButtonElementButton", names);
        Assert.Contains("PreviewStage", names);
        Assert.Contains("PropertySearch", names);
        Assert.Contains("PropertyHost", names);
        Assert.DoesNotContain("PackageHost", names);
        Assert.DoesNotContain("TraceHost", names);
    }

    [Fact]
    public void MarkupDeclaresPropertyInspectorItemsSource()
    {
        XDocument markup = XDocument.Load(RepositoryFile("CernealaPresentation", "AspectChapterView.cui.xml"));
        XElement propertyItems = Assert.Single(markup.Descendants()
            .Where(element => (string?)element.Attribute("Name") == "PropertyItems"));
        string codeBehind = File.ReadAllText(
            RepositoryFile("CernealaPresentation", "AspectChapterView.cui.xml.cs"));

        Assert.Equal("$root.PropertyRows", (string?)propertyItems.Attribute("ItemsSource"));
        Assert.DoesNotContain("PropertyItems.ItemsSource", codeBehind, StringComparison.Ordinal);
    }

    [Fact]
    public void PropertyInspectorUsesExplicitVirtualizingItemsPanel()
    {
        _ = AttachStudio(out AspectChapterView view);

        ItemsControl propertyItems = Assert.Single(Descendants(view)
            .OfType<ItemsControl>()
            .Where(items => items.ItemsSource?.Cast<object?>()
                .Any(item => item is AspectStudioPropertyRowModel) == true));

        Assert.IsType<VirtualizingStackPanel>(propertyItems.ItemsPanel);
    }

    [Fact]
    public void AttachedStudioBuildsBorderPreviewFromRegisteredEditableProperties()
    {
        UIRoot root = AttachStudio(out AspectChapterView view);

        Border preview = Assert.IsType<Border>(view.SelectedPreview);
        TextBlock label = Assert.IsType<TextBlock>(preview.Child);
        Assert.Same(preview, Assert.Single(Descendants(view).Where(element => ReferenceEquals(element, preview))));
        Assert.Equal("Segoe UI Variable Text", label.FontFamily);
        Assert.Equal(
            UiPropertyValueSource.ApplicationAspectBase,
            label.GetValueSource(Control.FontFamilyProperty));
        Assert.Contains(Control.BackgroundProperty, view.SelectedProperties);
        Assert.Contains(Control.BorderThicknessProperty, view.SelectedProperties);
        Assert.Contains(UIElement.OpacityProperty, view.SelectedProperties);
        Assert.DoesNotContain(UIElement.AspectProperty, view.SelectedProperties);
        Assert.True(PropertyModels(view).Count() >= 20);
        Assert.NotNull(root);
    }

    [Fact]
    public void PropertyRowsInsetLabelsAndEditorsFromBothOuterBorders()
    {
        _ = AttachStudio(out AspectChapterView view);
        RenderedPropertyRow row = PropertyRow(view, Control.FontSizeProperty);
        Border chrome = row.Chrome;
        Cerneala.UI.Layout.Panels.Grid grid =
            Assert.IsType<Cerneala.UI.Layout.Panels.Grid>(chrome.Child);
        TextBlock label = Assert.IsType<TextBlock>(grid.VisualChildren[0]);

        Assert.Equal(new Thickness(8, 7, 8, 7), chrome.Padding);
        Assert.True(label.ArrangedBounds.X >= chrome.ArrangedBounds.X + chrome.BorderThickness.Left + 8);
        Assert.True(
            row.Editor.ArrangedBounds.X + row.Editor.ArrangedBounds.Width
            <= chrome.ArrangedBounds.X + chrome.ArrangedBounds.Width - chrome.BorderThickness.Right - 8);
    }

    [Fact]
    public void BooleanPropertyEditorsReceiveTheApplicationCheckBoxAspect()
    {
        UIRoot root = AttachStudio(out AspectChapterView view);
        CheckBox checkBox = Assert.IsAssignableFrom<CheckBox>(PropertyRow(view, UIElement.ClipToBoundsProperty).Editor);
        checkBox.ApplyTemplate();
        Border indicator = Assert.IsType<Border>(
            checkBox.ComponentTemplateInstance!.Parts["PART_Indicator"]);
        Border checkMark = Assert.IsType<Border>(
            checkBox.ComponentTemplateInstance.Parts["PART_CheckMark"]);
        SvgPath checkGlyph = Assert.IsType<SvgPath>(checkMark.Child);

        Assert.Equal(16, indicator.Width);
        Assert.Equal(16, indicator.Height);
        Assert.Equal(new Color(16, 18, 23), Assert.IsType<SolidColorBrush>(indicator.Background).Color);
        Assert.Equal(new Color(66, 71, 84), Assert.IsType<SolidColorBrush>(indicator.BorderBrush).Color);
        Assert.Equal(Visibility.Hidden, checkMark.Visibility);

        UiHost host = new(new UiHostOptions
        {
            Root = root,
            Viewport = new UiViewport(root.ViewportWidth, root.ViewportHeight)
        });
        new AutomationSession(root, new RetainedAutomationInputDriver(host))
            .FindByAutomationId("aspect-property-ClipToBounds")
            .Click();
        AspectStudioBooleanRow model = Assert.IsType<AspectStudioBooleanRow>(
            PropertyRow(view, UIElement.ClipToBoundsProperty).Model);
        HitTestResult? checkBoxHit = new HitTestService().HitTest(
            root,
            checkBox.ArrangedBounds.X + (checkBox.ArrangedBounds.Width / 2),
            checkBox.ArrangedBounds.Y + (checkBox.ArrangedBounds.Height / 2));
        Assert.True(
            checkBox.IsChecked,
            $"Automation click missed the generated CheckBox at {checkBox.ArrangedBounds}; " +
            $"hit {checkBoxHit?.Element.GetType().Name ?? "nothing"} at {checkBoxHit?.Element.ArrangedBounds}.");
        Assert.True(model.IsChecked, "The generated TwoWay binding did not update its data item.");
        root.ProcessFrame();
        root.ProcessFrame();

        Assert.True(checkBox.IsChecked);
        Assert.True(model.IsChecked);
        Assert.True(Assert.IsType<Border>(view.SelectedPreview).ClipToBounds);
        Assert.Equal(new Color(16, 18, 23), Assert.IsType<SolidColorBrush>(indicator.Background).Color);
        Assert.Equal(new Color(77, 240, 255), Assert.IsType<SolidColorBrush>(checkMark.Background).Color);
        Assert.False(string.IsNullOrWhiteSpace(checkGlyph.Data));
        Assert.False(string.IsNullOrWhiteSpace(checkGlyph.ViewBox));
        Assert.Equal(Color.Black, Assert.IsType<SolidColorBrush>(checkGlyph.Fill).Color);
        Assert.Equal(Visibility.Visible, checkMark.Visibility);
    }

    [Fact]
    public void ApplicationCheckBoxAspectUsesACenteredGeometryCheckMark()
    {
        _ = AttachStudio(out AspectChapterView view);
        CheckBox checkBox = Assert.IsAssignableFrom<CheckBox>(
            PropertyRow(view, UIElement.ClipToBoundsProperty).Editor);
        checkBox.ApplyTemplate();
        Border checkMark = Assert.IsType<Border>(
            checkBox.ComponentTemplateInstance!.Parts["PART_CheckMark"]);
        SvgPath glyph = Assert.IsType<SvgPath>(checkMark.Child);

        Assert.False(string.IsNullOrWhiteSpace(glyph.Data));
        Assert.Equal(
            checkMark.ArrangedBounds.X + (checkMark.ArrangedBounds.Width / 2),
            glyph.ArrangedBounds.X + (glyph.ArrangedBounds.Width / 2),
            3);
        Assert.Equal(
            checkMark.ArrangedBounds.Y + (checkMark.ArrangedBounds.Height / 2),
            glyph.ArrangedBounds.Y + (glyph.ArrangedBounds.Height / 2),
            3);
    }

    [Fact]
    public void TextBlockValuesApplyThroughLocalAspectOnTheNextFrame()
    {
        UIRoot root = AttachStudio(out AspectChapterView view);
        view.SelectForTests(AspectStudioElementKind.TextBlock);
        object? initialAspect = view.SelectedPreview.Aspect;
        Assert.NotNull(initialAspect);

        Assert.True(view.TrySetPropertyForTests(TextBlock.TextProperty, "Editat live"));
        Assert.True(view.TrySetPropertyForTests(Control.FontSizeProperty, "42"));
        Assert.True(view.TrySetPropertyForTests(Control.ForegroundProperty, "#FFFF3EA5"));
        root.ProcessFrame();
        root.ProcessFrame();

        TextBlock preview = Assert.IsType<TextBlock>(view.SelectedPreview);
        Assert.Equal("Editat live", preview.Text);
        Assert.Equal(42, preview.FontSize);
        Assert.Equal(UiPropertyValueSource.LocalAspectBase, preview.GetValueSource(Control.ForegroundProperty));
        Assert.Equal(new Color(255, 62, 165), Assert.IsType<SolidColorBrush>(preview.Foreground).Color);
        Assert.Same(initialAspect, preview.Aspect);
    }

    [Fact]
    public void EditingBorderBackgroundTextBoxUpdatesPreview()
    {
        UIRoot root = AttachStudio(out AspectChapterView view);
        TextBox input = PropertyInput(view, Control.BackgroundProperty);
        root.RetainedRenderer.Commit(root);

        Color expected = new(18, 52, 86);
        TypeInto(root, input, "#123456");
        root.ProcessFrame();
        root.ProcessFrame();
        root.RetainedRenderer.Commit(root);

        Border preview = Assert.IsType<Border>(view.SelectedPreview);
        Assert.Equal(expected, Assert.IsType<SolidColorBrush>(preview.Background).Color);
        Assert.Contains(root.RetainedRenderCache.RootCommands, command =>
            command.Brush is SolidColorBrush solid && solid.Color == expected);
    }

    [Fact]
    public void ColorSwatchOpensPickerOverlayAndSpectrumClickUpdatesPreviewAndHexText()
    {
        UIRoot root = AttachStudio(out AspectChapterView view, 1650, 1055);
        RenderedPropertyRow row = PropertyRow(view, Control.BackgroundProperty);
        ColorSwatch swatch = Assert.Single(row.Editor.VisualChildren.OfType<ColorSwatch>());
        Overlay overlay = Assert.IsType<Overlay>(
            swatch.ComponentTemplateInstance!.Parts["PART_PickerOverlay"]);
        Button swatchButton = Assert.IsType<Button>(
            swatch.ComponentTemplateInstance.Parts["PART_SwatchButton"]);
        ColorPicker picker = swatch.Picker;
        TextBox input = Assert.Single(row.Editor.VisualChildren.OfType<TextBox>());
        Border preview = Assert.IsType<Border>(view.SelectedPreview);
        Color initial = Assert.IsType<SolidColorBrush>(preview.Background).Color;
        UiHost host = new(new UiHostOptions
        {
            Root = root,
            Viewport = new UiViewport(root.ViewportWidth, root.ViewportHeight)
        });
        AutomationSession session = new(root, new RetainedAutomationInputDriver(host));

        PointerSnapshot pointer = PointerSnapshot.Empty;
        Assert.True(
            swatchButton.ArrangedBounds.Width > 0 && swatchButton.ArrangedBounds.Height > 0,
            $"The generated swatch template button was not arranged: {swatchButton.ArrangedBounds}.");
        ClickWithInputFrames(host, ref pointer, swatch);
        host.Update(
            new InputFrame(pointer, pointer, KeyboardSnapshot.Empty, KeyboardSnapshot.Empty, []),
            elapsedTime: TimeSpan.Zero);

        HitTestResult? swatchHit = new HitTestService().HitTest(
            root,
            swatch.ArrangedBounds.X + (swatch.ArrangedBounds.Width / 2),
            swatch.ArrangedBounds.Y + (swatch.ArrangedBounds.Height / 2));
        Assert.True(
            overlay.IsOpen,
            $"Swatch click hit {swatchHit?.Element.GetType().Name ?? "nothing"} at " +
            $"{swatchHit?.Element.ArrangedBounds} instead of opening the picker.");
        Assert.True(overlay.IsProjected);

        ColorSpectrum spectrum = Assert.IsType<ColorSpectrum>(
            picker.ComponentTemplateInstance!.Parts["PART_Spectrum"]);
        ClickWithInputFrames(host, ref pointer, spectrum);
        root.ProcessFrame();
        root.ProcessFrame();

        Color selected = picker.SelectedColor;
        Assert.NotEqual(initial, selected);
        Assert.Equal($"#{selected.A:X2}{selected.R:X2}{selected.G:X2}{selected.B:X2}", input.Text);
        Assert.Equal(selected, Assert.IsType<SolidColorBrush>(preview.Background).Color);
    }

    [Fact]
    public void HueInteractionDoesNotReapplyUnchangedPreviewAspectValues()
    {
        UIRoot root = AttachStudio(out AspectChapterView view, 1650, 1055);
        UiHost host = new(new UiHostOptions
        {
            Root = root,
            Viewport = new UiViewport(root.ViewportWidth, root.ViewportHeight)
        });
        PointerSnapshot pointer = PointerSnapshot.Empty;
        ColorSwatch swatch = Assert.Single(
            PropertyRow(view, Control.BorderBrushProperty).Editor.VisualChildren.OfType<ColorSwatch>());

        ClickWithInputFrames(host, ref pointer, swatch);
        Slider hueSlider = Assert.IsType<Slider>(
            swatch.Picker.ComponentTemplateInstance!.Parts["PART_HueSlider"]);

        UiFrame[] frames = DragWithInputFrames(host, ref pointer, hueSlider, steps: 12);

        Assert.All(frames, frame => Assert.Equal(0, frame.Stats.InheritedElements));
        Assert.All(frames, frame => Assert.InRange(frame.Stats.AspectElements, 0, 1));
        Assert.InRange(frames[0].Stats.MeasuredElements, 0, 5);
        Assert.InRange(frames[0].Stats.ArrangedElements, 0, 6);
        Assert.All(frames.Skip(1), frame => Assert.Equal(0, frame.Stats.MeasuredElements));
        Assert.All(frames.Skip(1), frame => Assert.Equal(0, frame.Stats.ArrangedElements));
    }

    [Fact]
    public void EditingTextBlockForegroundTextBoxUpdatesPreview()
    {
        UIRoot root = AttachStudio(out AspectChapterView view);
        view.SelectForTests(AspectStudioElementKind.TextBlock);
        root.ProcessFrame();
        TextBox input = PropertyInput(view, Control.ForegroundProperty);
        root.ProcessFrame();
        root.RetainedRenderer.Commit(root);

        Color expected = new(101, 67, 33);
        TypeInto(root, input, "#654321");
        root.ProcessFrame();
        root.ProcessFrame();
        root.RetainedRenderer.Commit(root);

        TextBlock preview = Assert.IsType<TextBlock>(view.SelectedPreview);
        Assert.Equal(expected, Assert.IsType<SolidColorBrush>(preview.Foreground).Color);
        Assert.Contains(root.RetainedRenderCache.RootCommands, command =>
            command.Brush is SolidColorBrush solid && solid.Color == expected);
    }

    [Fact]
    public void ButtonSupportsContentBooleanEnumAndThicknessValues()
    {
        UIRoot root = AttachStudio(out AspectChapterView view);
        view.SelectForTests(AspectStudioElementKind.Button);

        Assert.Contains(ContentControl.ContentProperty, view.SelectedProperties);
        Assert.True(view.TrySetPropertyForTests(ContentControl.ContentProperty, "SALVEAZA"));
        Assert.True(view.TrySetPropertyForTests(UIElement.IsEnabledProperty, "false"));
        Assert.True(view.TrySetPropertyForTests(UIElement.HorizontalAlignmentProperty, "Right"));
        Assert.True(view.TrySetPropertyForTests(Control.PaddingProperty, "8,12"));
        root.ProcessFrame();
        root.ProcessFrame();

        Button preview = Assert.IsType<Button>(view.SelectedPreview);
        Assert.Equal("SALVEAZA", preview.Content);
        Assert.False(preview.IsEnabled);
        Assert.Equal(HorizontalAlignment.Right, preview.HorizontalAlignment);
        Assert.Equal(new Thickness(8, 12, 8, 12), preview.Padding);
    }

    [Fact]
    public void EditingButtonCursorChangesCursorResolvedOverPreview()
    {
        UIRoot root = AttachStudio(out AspectChapterView view);
        view.SelectForTests(AspectStudioElementKind.Button);
        root.ProcessFrame();
        root.ProcessFrame();
        ComboBox editor = Assert.IsAssignableFrom<ComboBox>(PropertyRow(view, UIElement.CursorProperty).Editor);

        editor.SelectedIndex = 4;
        root.ProcessFrame();
        root.ProcessFrame();

        Button preview = Assert.IsType<Button>(view.SelectedPreview);
        Assert.Equal(Cursor.Crosshair, preview.Cursor);
        LayoutRect bounds = preview.ArrangedBounds;
        Cursor resolved = new CursorService().Resolve(
            root,
            bounds.X + (bounds.Width / 2),
            bounds.Y + (bounds.Height / 2));
        Assert.Equal(Cursor.Crosshair, resolved);
    }

    [Fact]
    public void ComboBoxPropertyEditorsUsePresentationPalette()
    {
        UIRoot root = AttachStudio(out AspectChapterView view);
        view.SelectForTests(AspectStudioElementKind.Button);
        root.ProcessFrame();
        root.ProcessFrame();
        ComboBox comboBox = Assert.IsAssignableFrom<ComboBox>(PropertyRow(view, UIElement.CursorProperty).Editor);
        TextBox editor = Assert.IsType<TextBox>(
            comboBox.ComponentTemplateInstance!.Parts["PART_EditableTextBox"]);
        ToggleButton toggle = Assert.IsType<ToggleButton>(
            comboBox.ComponentTemplateInstance.Parts["PART_DropDownToggle"]);
        Cerneala.UI.Controls.Shapes.Shape glyph =
            Assert.IsAssignableFrom<Cerneala.UI.Controls.Shapes.Shape>(toggle.Content);
        Overlay overlay = Assert.IsType<Overlay>(
            comboBox.ComponentTemplateInstance.Parts["PART_DropDownOverlay"]);
        Border dropDownBorder = Assert.IsType<Border>(overlay.Content);

        Color paper = new(237, 239, 243);
        Color line = new(52, 60, 70);
        Color panel = new(20, 24, 30);
        Color slate = new(148, 163, 184);
        Assert.Equal(UiPropertyValueSource.LocalAspectBase, comboBox.GetValueSource(Control.BackgroundProperty));
        Assert.Equal(UiPropertyValueSource.LocalAspectBase, comboBox.GetValueSource(Control.ForegroundProperty));
        Assert.Equal(panel, Assert.IsType<SolidColorBrush>(comboBox.Background).Color);
        Assert.Equal(paper, Assert.IsType<SolidColorBrush>(comboBox.Foreground).Color);
        Assert.Equal(line, Assert.IsType<SolidColorBrush>(comboBox.BorderBrush).Color);
        Assert.Equal(panel, Assert.IsType<SolidColorBrush>(editor.Background).Color);
        Assert.Equal(paper, Assert.IsType<SolidColorBrush>(editor.Foreground).Color);
        Assert.Equal(paper, Assert.IsType<SolidColorBrush>(editor.CaretBrush).Color);
        Assert.Equal(panel, Assert.IsType<SolidColorBrush>(toggle.Background).Color);
        Assert.Equal(paper, Assert.IsType<SolidColorBrush>(toggle.Foreground).Color);
        Assert.Equal(slate, Assert.IsType<SolidColorBrush>(toggle.BorderBrush).Color);
        Assert.Equal(paper, Assert.IsType<SolidColorBrush>(glyph.Fill).Color);
        Assert.Equal(panel, Assert.IsType<SolidColorBrush>(dropDownBorder.Background).Color);
    }

    [Fact]
    public void TextTrimmingEditorExposesTheCompleteWpfEquivalentContract()
    {
        UIRoot root = AttachStudio(out AspectChapterView view);
        view.SelectForTests(AspectStudioElementKind.TextBlock);
        root.ProcessFrame();

        ComboBox editor = Assert.IsAssignableFrom<ComboBox>(PropertyRow(view, TextBlock.TextTrimmingProperty).Editor);

        Assert.Equal(
            [TextTrimming.None, TextTrimming.CharacterEllipsis, TextTrimming.WordEllipsis],
            editor.ItemsSource!.Cast<TextTrimming>().ToArray());
    }

    [Fact]
    public void PropertyEditorsExposeStableAutomationIds()
    {
        UIRoot root = AttachStudio(out AspectChapterView view);
        view.SelectForTests(AspectStudioElementKind.Button);
        root.ProcessFrame();

        UIElement cursorEditor = PropertyRow(view, UIElement.CursorProperty).Editor;

        Assert.Equal(
            "aspect-property-Cursor",
            AutomationProperties.GetAutomationId(cursorEditor));
        Assert.Equal(
            "aspect-element-textblock",
            AutomationProperties.GetAutomationId(
                Assert.Single(Descendants(view).OfType<Button>().Where(button => Equals(button.Content, "TEXTBLOCK")))));
    }

    [Fact]
    public void FilteredCursorDropDownAutoSizesToItsVisibleItems()
    {
        UIRoot root = AttachStudio(out AspectChapterView view, 1650, 1055);
        view.SelectForTests(AspectStudioElementKind.Button);
        root.ProcessFrame();
        UiHost host = new(new UiHostOptions
        {
            Root = root,
            Viewport = new UiViewport(root.ViewportWidth, root.ViewportHeight)
        });
        AutomationSession session = new(root, new RetainedAutomationInputDriver(host));

        ComboBox comboBox = Assert.IsAssignableFrom<ComboBox>(PropertyRow(view, UIElement.CursorProperty).Editor);
        TextBox editableTextBox = Assert.IsType<TextBox>(
            comboBox.ComponentTemplateInstance!.Parts["PART_EditableTextBox"]);
        TypeInto(root, editableTextBox, "a");
        root.ProcessFrame();

        Overlay overlay = Assert.IsType<Overlay>(
            comboBox.ComponentTemplateInstance!.Parts["PART_DropDownOverlay"]);
        Border border = Assert.IsType<Border>(overlay.Content);
        ScrollViewer scrollViewer = Assert.IsType<ScrollViewer>(border.Child);
        Assert.True(comboBox.IsDropDownOpen);
        Assert.Equal(
            [1, 2, 0, 3, 4],
            comboBox.ItemsPresenter.LayoutPanelRoot!.VisualChildren
                .Select(Cerneala.UI.Controls.Items.ItemContainerGenerator.GetItemIndex)
                .ToArray());
        Assert.Equal(
            scrollViewer.Presenter.ExtentHeight + border.BorderThickness.Vertical,
            overlay.ProjectedPresenter.ArrangedBounds.Height);
        Assert.True(overlay.ProjectedPresenter.ArrangedBounds.Height < comboBox.MaxDropDownHeight);
        DrawCommand[] renderedCommands = root.RetainedRenderer.Commit(root)
            .Where(command => command.Kind == DrawCommandKind.DrawText)
            .ToArray();
        string[] renderedText = renderedCommands
            .Where(command => command.Kind == DrawCommandKind.DrawText)
            .Select(command => command.Text)
            .Where(text => text is not null)
            .Cast<string>()
            .ToArray();
        Assert.Contains("IBEAM", renderedText);
        Assert.Contains("CROSSHAIR", renderedText);
        float dropDownTop = overlay.ProjectedPresenter.ArrangedBounds.Y;
        float dropDownBottom = dropDownTop + overlay.ProjectedPresenter.ArrangedBounds.Height;
        Assert.InRange(Assert.Single(renderedCommands, command => command.Text == "IBEAM").Position.Y, dropDownTop, dropDownBottom);
        Assert.InRange(Assert.Single(renderedCommands, command => command.Text == "CROSSHAIR").Position.Y, dropDownTop, dropDownBottom);
    }

    [Fact]
    public void InvalidValuesAreRejectedWithoutMutatingPreview()
    {
        UIRoot root = AttachStudio(out AspectChapterView view);
        view.SelectForTests(AspectStudioElementKind.TextBlock);
        root.ProcessFrame();
        float initial = Assert.IsType<TextBlock>(view.SelectedPreview).FontSize;

        Assert.False(view.TrySetPropertyForTests(Control.FontSizeProperty, "-4"));
        Assert.False(view.TrySetPropertyForTests(UIElement.OpacityProperty, "2"));
        root.ProcessFrame();

        TextBlock preview = Assert.IsType<TextBlock>(view.SelectedPreview);
        Assert.Equal(initial, preview.FontSize);
        Assert.Equal(1, preview.Opacity);
    }

    [Fact]
    public void ResetCurrentRestoresInitialAspectValues()
    {
        UIRoot root = AttachStudio(out AspectChapterView view);
        view.SelectForTests(AspectStudioElementKind.Button);
        Assert.True(view.TrySetPropertyForTests(Control.FontSizeProperty, "27"));
        root.ProcessFrame();
        Assert.Equal(27, Assert.IsType<Button>(view.SelectedPreview).FontSize);

        Button reset = Assert.Single(Descendants(view).OfType<Button>().Where(button => Equals(button.Content, "RESET CURRENT")));
        reset.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent, reset));
        root.ProcessFrame();
        root.ProcessFrame();

        Assert.Equal(12, Assert.IsType<Button>(view.SelectedPreview).FontSize);
    }

    [Fact]
    public void RepeatedActivationReusesTargetsWithoutDuplicatingEditors()
    {
        UIRoot root = AttachStudio(out AspectChapterView view);
        UIElement preview = view.SelectedPreview;
        UIElement[] editors = RealizedPropertyRows(view).Select(row => (UIElement)row.Chrome).ToArray();

        for (int cycle = 0; cycle < 10; cycle++)
        {
            view.Deactivate();
            Assert.Equal(editors, RealizedPropertyRows(view).Select(row => row.Chrome));
            view.Activate();
            root.ProcessFrame();
            Assert.Same(preview, view.SelectedPreview);
            Assert.Equal(editors, RealizedPropertyRows(view).Select(row => row.Chrome));
        }
    }

    [Fact]
    public void RepeatedActivationStaysWithinLayoutMeasureBudget()
    {
        UIRoot root = AttachStudio(out AspectChapterView view);
        view.Deactivate();
        root.ProcessFrame();

        view.Activate();
        FrameStats activation = root.ProcessFrame();
        int visualElementCount = Descendants(view).Count() + 1;
        int measureCallBudget = visualElementCount * 4;

        Assert.True(
            activation.MeasureCalls <= measureCallBudget,
            $"Expected at most {measureCallBudget} measure calls for {visualElementCount} elements, " +
            $"but activation performed {activation.MeasureCalls}.");
    }

    [Theory]
    [InlineData(1070, 726)]
    [InlineData(830, 586)]
    public void StudioInspectorArrangesInsideTourViewport(float width, float height)
    {
        AspectChapterView view = new();
        view.PrepareEditorForTests();

        Arrange(view, width, height);

        ScrollViewer inspector = Assert.Single(Descendants(view)
            .OfType<ScrollViewer>()
            .Where(scrollViewer => Descendants(scrollViewer).OfType<ItemsControl>().Any()));
        LayoutRect bounds = inspector.ArrangedBounds;
        Assert.True(bounds.Width > 0 && bounds.Height > 0);
        Assert.True(bounds.X >= 0 && bounds.Y >= 0);
        Assert.True(bounds.X + bounds.Width <= width);
        Assert.True(bounds.Y + bounds.Height <= height);
    }

    private static UIRoot AttachStudio(
        out AspectChapterView view,
        float width = 1070,
        float height = 726)
    {
        UIRoot root = new(width, height);
        root.SetResourceProvider(Application.Current!.Resources);
        view = new AspectChapterView();
        root.VisualChildren.Add(view);
        root.ProcessFrame();
        root.ProcessFrame();
        return root;
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

    private static void Arrange(AspectChapterView view, float width, float height)
    {
        view.Measure(new MeasureContext(new LayoutSize(width, height)));
        view.Arrange(new ArrangeContext(new LayoutRect(0, 0, width, height)));
    }

    private static TextBox PropertyInput(AspectChapterView view, UiProperty property)
    {
        RenderedPropertyRow row = PropertyRow(view, property);
        return Assert.Single(Descendants(row.Editor).OfType<TextBox>());
    }

    private static RenderedPropertyRow PropertyRow(AspectChapterView view, UiProperty property)
    {
        AspectStudioPropertyRowModel model = Assert.Single(
            PropertyModels(view).Where(candidate => candidate.Label == property.Name));
        Border chrome = FindChrome(view, model);
        Cerneala.UI.Layout.Panels.Grid grid = Assert.IsType<Cerneala.UI.Layout.Panels.Grid>(chrome.Child);
        return new RenderedPropertyRow(model, chrome, grid.VisualChildren[1]);
    }

    private static RenderedPropertyRow[] RealizedPropertyRows(AspectChapterView view) =>
        Descendants(view)
            .OfType<Border>()
            .Where(chrome =>
                chrome.DataContext is AspectStudioPropertyRowModel and not AspectStudioHeaderRow &&
                chrome.Child is Cerneala.UI.Layout.Panels.Grid grid &&
                grid.VisualChildren.Count == 2 &&
                grid.VisualChildren[0] is TextBlock)
            .Select(chrome =>
            {
                Cerneala.UI.Layout.Panels.Grid grid =
                    Assert.IsType<Cerneala.UI.Layout.Panels.Grid>(chrome.Child);
                return new RenderedPropertyRow(
                    (AspectStudioPropertyRowModel)chrome.DataContext!,
                    chrome,
                    grid.VisualChildren[1]);
            })
            .ToArray();

    private static AspectStudioPropertyRowModel[] PropertyModels(AspectChapterView view)
    {
        ItemsControl propertyItems = Assert.Single(Descendants(view)
            .OfType<ItemsControl>()
            .Where(items => items.ItemsSource?.Cast<object?>().Any(item => item is AspectStudioPropertyRowModel) == true));
        return propertyItems.ItemsSource!.Cast<AspectStudioPropertyRowModel>().ToArray();
    }

    private static Border FindChrome(
        AspectChapterView view,
        AspectStudioPropertyRowModel model)
    {
        return Assert.Single(Descendants(view)
            .OfType<Border>()
            .Where(candidate =>
                candidate.Child is Cerneala.UI.Layout.Panels.Grid grid &&
                grid.VisualChildren.Count == 2 &&
                grid.VisualChildren[0] is TextBlock label &&
                ReferenceEquals(candidate.DataContext, model) &&
                label.Text == model.Label));
    }

    private static void TypeInto(UIRoot root, TextBox input, string text)
    {
        ElementInputRouteMap routeMap = root.InputCache.EnsureCurrent(root);
        FocusManager focusManager = new();
        Assert.True(focusManager.Focus(input, routeMap));
        input.Select(0, input.Text.Length);
        new TextInputBridge().Dispatch([new TextInputSnapshotEvent(text)], focusManager, routeMap);
    }

    private static UiFrame[] ClickWithInputFrames(
        UiHost host,
        ref PointerSnapshot pointer,
        UIElement target)
    {
        LayoutRect bounds = target.ArrangedBounds;
        PointerSnapshot moved = pointer.WithPosition(
            bounds.X + (bounds.Width / 2),
            bounds.Y + (bounds.Height / 2));
        UiFrame moveFrame = host.Update(
            new InputFrame(pointer, moved, KeyboardSnapshot.Empty, KeyboardSnapshot.Empty, []),
            elapsedTime: TimeSpan.Zero);
        PointerSnapshot pressed = moved.WithButton(InputMouseButton.Left, true);
        UiFrame pressFrame = host.Update(
            new InputFrame(moved, pressed, KeyboardSnapshot.Empty, KeyboardSnapshot.Empty, []),
            elapsedTime: TimeSpan.Zero);
        PointerSnapshot released = pressed.WithButton(InputMouseButton.Left, false);
        UiFrame releaseFrame = host.Update(
            new InputFrame(pressed, released, KeyboardSnapshot.Empty, KeyboardSnapshot.Empty, []),
            elapsedTime: TimeSpan.Zero);
        pointer = released;
        return [moveFrame, pressFrame, releaseFrame];
    }

    private static UiFrame[] DragWithInputFrames(
        UiHost host,
        ref PointerSnapshot pointer,
        UIElement target,
        int steps)
    {
        LayoutRect bounds = target.ArrangedBounds;
        float y = bounds.Y + (bounds.Height / 2);
        float startX = bounds.X + (bounds.Width * 0.1f);
        float endX = bounds.X + (bounds.Width * 0.9f);
        PointerSnapshot moved = pointer.WithPosition(startX, y);
        host.Update(
            new InputFrame(pointer, moved, KeyboardSnapshot.Empty, KeyboardSnapshot.Empty, []),
            elapsedTime: TimeSpan.Zero);

        List<UiFrame> frames = new(steps + 2);
        PointerSnapshot pressed = moved.WithButton(InputMouseButton.Left, true);
        frames.Add(host.Update(
            new InputFrame(moved, pressed, KeyboardSnapshot.Empty, KeyboardSnapshot.Empty, []),
            elapsedTime: TimeSpan.Zero));
        PointerSnapshot current = pressed;
        for (int step = 1; step <= steps; step++)
        {
            float progress = step / (float)steps;
            PointerSnapshot next = current.WithPosition(startX + ((endX - startX) * progress), y);
            frames.Add(host.Update(
                new InputFrame(current, next, KeyboardSnapshot.Empty, KeyboardSnapshot.Empty, []),
                elapsedTime: TimeSpan.Zero));
            current = next;
        }

        PointerSnapshot released = current.WithButton(InputMouseButton.Left, false);
        frames.Add(host.Update(
            new InputFrame(current, released, KeyboardSnapshot.Empty, KeyboardSnapshot.Empty, []),
            elapsedTime: TimeSpan.Zero));
        pointer = released;
        return frames.ToArray();
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

    private sealed record RenderedPropertyRow(
        AspectStudioPropertyRowModel Model,
        Border Chrome,
        UIElement Editor);
}
