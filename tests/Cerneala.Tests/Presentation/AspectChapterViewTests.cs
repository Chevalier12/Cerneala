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
using Cerneala.UI.Layout;
using Cerneala.UI.Media;
using Cerneala.Tests.UI.Hosting;

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
    public void AttachedStudioBuildsBorderPreviewFromRegisteredEditableProperties()
    {
        UIRoot root = AttachStudio(out AspectChapterView view);

        Border preview = Assert.IsType<Border>(view.SelectedPreview);
        TextBlock label = Assert.IsType<TextBlock>(preview.Child);
        Assert.Same(preview, Assert.Single(Descendants(view).Where(element => ReferenceEquals(element, preview))));
        Assert.Equal(preview.FontFamily, label.FontFamily);
        Assert.Same(preview.Foreground, label.Foreground);
        Assert.Contains(Control.BackgroundProperty, view.SelectedProperties);
        Assert.Contains(Control.BorderThicknessProperty, view.SelectedProperties);
        Assert.Contains(UIElement.OpacityProperty, view.SelectedProperties);
        Assert.DoesNotContain(UIElement.AspectProperty, view.SelectedProperties);
        Assert.True(Descendants(view).OfType<AspectStudioPropertyRow>().Count() >= 20);
        Assert.NotNull(root);
    }

    [Fact]
    public void TextBlockValuesApplyThroughLocalAspectOnTheNextFrame()
    {
        UIRoot root = AttachStudio(out AspectChapterView view);
        view.SelectForTests(AspectStudioElementKind.TextBlock);

        Assert.True(view.TrySetPropertyForTests(TextBlock.TextProperty, "Editat live"));
        Assert.True(view.TrySetPropertyForTests(Control.FontSizeProperty, "42"));
        Assert.True(view.TrySetPropertyForTests(Control.ForegroundProperty, "#FFFF3EA5"));
        root.ProcessFrame();
        root.ProcessFrame();

        TextBlock preview = Assert.IsType<TextBlock>(view.SelectedPreview);
        Assert.Equal("Editat live", preview.Text);
        Assert.Equal(42, preview.FontSize);
        Assert.Equal(new Color(255, 62, 165), Assert.IsType<SolidColorBrush>(preview.Foreground).Color);
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
    public void EditingTextBlockForegroundTextBoxUpdatesPreview()
    {
        UIRoot root = AttachStudio(out AspectChapterView view);
        view.SelectForTests(AspectStudioElementKind.TextBlock);
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
        ComboBox editor = Assert.IsType<ComboBox>(PropertyRow(view, UIElement.CursorProperty).Editor);

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
        ComboBox comboBox = Assert.IsType<ComboBox>(PropertyRow(view, UIElement.CursorProperty).Editor);
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
        Assert.Equal(UiPropertyValueSource.Local, comboBox.GetValueSource(Control.BackgroundProperty));
        Assert.Equal(UiPropertyValueSource.Local, comboBox.GetValueSource(Control.ForegroundProperty));
        Assert.Equal(Color.Transparent, Assert.IsType<SolidColorBrush>(comboBox.Background).Color);
        Assert.Equal(paper, Assert.IsType<SolidColorBrush>(comboBox.Foreground).Color);
        Assert.Equal(line, Assert.IsType<SolidColorBrush>(comboBox.BorderBrush).Color);
        Assert.Equal(Color.Transparent, Assert.IsType<SolidColorBrush>(editor.Background).Color);
        Assert.Equal(paper, Assert.IsType<SolidColorBrush>(editor.Foreground).Color);
        Assert.Equal(paper, Assert.IsType<SolidColorBrush>(editor.CaretBrush).Color);
        Assert.Equal(Color.Transparent, Assert.IsType<SolidColorBrush>(toggle.Background).Color);
        Assert.Equal(paper, Assert.IsType<SolidColorBrush>(toggle.Foreground).Color);
        Assert.Equal(line, Assert.IsType<SolidColorBrush>(toggle.BorderBrush).Color);
        Assert.Equal(paper, Assert.IsType<SolidColorBrush>(glyph.Fill).Color);
        Assert.Equal(panel, Assert.IsType<SolidColorBrush>(dropDownBorder.Background).Color);
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

        session.FindByAutomationId("aspect-property-Cursor")
            .Click()
            .PressKey(InputKey.A, AutomationModifiers.Control)
            .SendText("a");

        ComboBox comboBox = Assert.IsType<ComboBox>(PropertyRow(view, UIElement.CursorProperty).Editor);
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

        for (int cycle = 0; cycle < 10; cycle++)
        {
            view.Deactivate();
            Assert.Empty(Descendants(view).OfType<AspectStudioPropertyRow>());
            view.Activate();
            root.ProcessFrame();
            Assert.Same(preview, view.SelectedPreview);
            Assert.Equal(view.SelectedProperties.Count, Descendants(view).OfType<AspectStudioPropertyRow>().Count());
        }
    }

    [Theory]
    [InlineData(1070, 726)]
    [InlineData(830, 586)]
    public void StudioInspectorArrangesInsideTourViewport(float width, float height)
    {
        AspectChapterView view = new();
        view.PrepareEditorForTests();

        Arrange(view, width, height);

        AspectStudioScrollHost inspector = Assert.Single(Descendants(view).OfType<AspectStudioScrollHost>());
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
        AspectStudioPropertyRow row = PropertyRow(view, property);
        return Assert.Single(Descendants(row.Editor).OfType<TextBox>());
    }

    private static AspectStudioPropertyRow PropertyRow(AspectChapterView view, UiProperty property)
    {
        return Assert.Single(Descendants(view)
            .OfType<AspectStudioPropertyRow>()
            .Where(candidate => ReferenceEquals(candidate.Property, property)));
    }

    private static void TypeInto(UIRoot root, TextBox input, string text)
    {
        ElementInputRouteMap routeMap = root.InputCache.EnsureCurrent(root);
        FocusManager focusManager = new();
        Assert.True(focusManager.Focus(input, routeMap));
        input.Select(0, input.Text.Length);
        new TextInputBridge().Dispatch([new TextInputSnapshotEvent(text)], focusManager, routeMap);
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
