using Cerneala.UI.Controls;
using Cerneala.UI.Controls.Templates;
using Cerneala.Drawing;
using Cerneala.UI.Media;
using Cerneala.UI.Elements;
using Cerneala.UI.Input;
using Cerneala.UI.Layout;
using ToggleButton = Cerneala.UI.Controls.Primitives.ToggleButton;
using LayoutCanvas = Cerneala.UI.Layout.Panels.Canvas;
using LayoutGrid = Cerneala.UI.Layout.Panels.Grid;
using LayoutStackPanel = Cerneala.UI.Layout.Panels.StackPanel;
using DirectionPath = Cerneala.UI.Controls.Shapes.Path;

namespace Cerneala.Tests.Controls;

public sealed class ComboBoxTests
{
    [Fact]
    public void ComboBoxUsesSharedSelectorSelectionState()
    {
        ComboBox comboBox = new();
        comboBox.SetItems(new[] { "one", "two" });

        comboBox.SelectedIndex = 1;

        Assert.Equal(1, comboBox.SelectedIndex);
        Assert.Equal("two", comboBox.SelectedItem);
        Assert.Equal("two", comboBox.Text);
        Assert.True(comboBox.SelectionModel.IsSelected(1));
    }

    [Fact]
    public void ComboBoxItemsHaveDefaultPadding()
    {
        ComboBox comboBox = new();
        comboBox.SetItems(new[] { "one", "two" });
        comboBox.IsDropDownOpen = true;
        UIRoot root = new(200, 120);
        LayoutCanvas canvas = new();
        canvas.VisualChildren.Add(comboBox);
        root.VisualChildren.Add(canvas);

        root.ProcessFrame();

        ComboBoxItem[] containers = comboBox.ItemContainerGenerator.RealizedContainers.Values
            .Cast<ComboBoxItem>()
            .ToArray();
        Assert.Equal(2, containers.Length);
        Assert.All(containers, container =>
            Assert.Equal(new Thickness(6), container.Padding));
    }

    [Fact]
    public void ConstrainedDropDownShowsVerticalScrollBarForOverflowingItems()
    {
        UIRoot root = new(220, 140);
        ComboBox comboBox = new()
        {
            Width = 180,
            MaxDropDownHeight = 80,
            IsDropDownOpen = true
        };
        comboBox.SetItems(Enumerable.Range(1, 12).Select(index => $"item {index}"));
        LayoutCanvas canvas = new();
        canvas.VisualChildren.Add(comboBox);
        root.VisualChildren.Add(canvas);

        root.ProcessFrame();

        Overlay overlay = Assert.IsType<Overlay>(
            comboBox.ComponentTemplateInstance!.Parts["PART_DropDownOverlay"]);
        Border border = Assert.IsType<Border>(overlay.Content);
        ScrollViewer scrollViewer = Assert.IsType<ScrollViewer>(border.Child);
        Assert.True(scrollViewer.Presenter.ExtentHeight > scrollViewer.Presenter.ViewportHeight);
        Assert.True(scrollViewer.IsVerticalScrollBarVisible);
        Assert.True(scrollViewer.VerticalScrollBar.ArrangedBounds.Width > 0);
    }

    [Fact]
    public void DefaultDropDownAutoSizesToContentUpToThreeHundredPixels()
    {
        UIRoot root = new(400, 500);
        ComboBox comboBox = new()
        {
            Width = 180,
            IsDropDownOpen = true
        };
        comboBox.SetItems(new[] { "one", "two" });
        LayoutCanvas canvas = new();
        canvas.VisualChildren.Add(comboBox);
        root.VisualChildren.Add(canvas);

        root.ProcessFrame();

        Overlay overlay = Assert.IsType<Overlay>(
            comboBox.ComponentTemplateInstance!.Parts["PART_DropDownOverlay"]);
        Border border = Assert.IsType<Border>(overlay.Content);
        ScrollViewer scrollViewer = Assert.IsType<ScrollViewer>(border.Child);
        Assert.Equal(
            scrollViewer.Presenter.ExtentHeight + border.BorderThickness.Vertical,
            overlay.ProjectedPresenter.ArrangedBounds.Height);
        Assert.True(overlay.ProjectedPresenter.ArrangedBounds.Height < 300);
        Assert.False(scrollViewer.IsVerticalScrollBarVisible);
        Assert.Equal(300, comboBox.MaxDropDownHeight);
    }

    [Fact]
    public void ExplicitOverlayHeightOverridesComboBoxDropDownAutoSize()
    {
        UIRoot root = new(400, 500);
        ComboBox comboBox = new()
        {
            Width = 180
        };
        comboBox.SetItems(new[] { "one", "two" });
        comboBox.ApplyTemplate();
        Overlay overlay = Assert.IsType<Overlay>(
            comboBox.ComponentTemplateInstance!.Parts["PART_DropDownOverlay"]);
        overlay.Height = 360;
        comboBox.IsDropDownOpen = true;
        LayoutCanvas canvas = new();
        canvas.VisualChildren.Add(comboBox);
        root.VisualChildren.Add(canvas);

        root.ProcessFrame();

        Assert.Equal(360, overlay.ProjectedPresenter.ArrangedBounds.Height);
    }

    [Fact]
    public void DefaultDropDownCapsOverflowAtThreeHundredPixels()
    {
        UIRoot root = new(400, 500);
        ComboBox comboBox = new()
        {
            Width = 180,
            IsDropDownOpen = true
        };
        comboBox.SetItems(Enumerable.Range(1, 40).Select(index => $"item {index}"));
        LayoutCanvas canvas = new();
        canvas.VisualChildren.Add(comboBox);
        root.VisualChildren.Add(canvas);

        root.ProcessFrame();

        Overlay overlay = Assert.IsType<Overlay>(
            comboBox.ComponentTemplateInstance!.Parts["PART_DropDownOverlay"]);
        Border border = Assert.IsType<Border>(overlay.Content);
        ScrollViewer scrollViewer = Assert.IsType<ScrollViewer>(border.Child);
        Assert.Equal(300, overlay.ProjectedPresenter.ArrangedBounds.Height);
        Assert.True(scrollViewer.IsVerticalScrollBarVisible);
    }

    [Fact]
    public void ComboBoxRealizedItemsParticipateInRetainedInputRouting()
    {
        UIRoot root = new(100, 100);
        ComboBox comboBox = new()
        {
            ItemsPanel = new ItemsPanelTemplate(() => new LayoutStackPanel()),
            IsDropDownOpen = true
        };
        comboBox.SetItems(new UIElement[] { new FixedElement(), new FixedElement() });
        LayoutCanvas canvas = new();
        canvas.VisualChildren.Add(comboBox);
        root.VisualChildren.Add(canvas);
        root.ProcessFrame();

        ElementInputBridge bridge = new();
        UIElement second = comboBox.ItemContainerGenerator.RealizedContainers[1];
        float x = second.ArrangedBounds.X + 2;
        float y = second.ArrangedBounds.Y + 2;
        Assert.NotNull(new HitTestService().HitTest(root, x, y));
        bridge.Dispatch(root, PointerFrame(x, y, currentDown: true));
        bridge.Dispatch(root, PointerFrame(x, y, previousDown: true));

        Assert.Equal(1, comboBox.SelectedIndex);
        Assert.False(comboBox.IsDropDownOpen);
    }

    [Fact]
    public void EditableTextThatDiffersFromSelectionClearsSelection()
    {
        ComboBox comboBox = new()
        {
            IsEditable = true
        };
        comboBox.SetItems(new[] { "one", "two" });
        comboBox.SelectedIndex = 1;

        comboBox.Text = "custom";

        Assert.Equal(-1, comboBox.SelectedIndex);
        Assert.Null(comboBox.SelectedItem);
        Assert.Equal("custom", comboBox.Text);
    }

    [Fact]
    public void EditableTemplatePartDrivesIndependentText()
    {
        ComboBox comboBox = new()
        {
            IsEditable = true
        };
        comboBox.SetItems(new[] { "one", "two" });
        comboBox.SelectedIndex = 0;
        comboBox.ApplyTemplate();
        TextBox editor = Assert.IsType<TextBox>(
            comboBox.ComponentTemplateInstance!.Parts["PART_EditableTextBox"]);

        editor.Text = "typed";

        Assert.Equal("typed", comboBox.Text);
        Assert.Equal(-1, comboBox.SelectedIndex);
    }

    [Fact]
    public void DisplayMemberPathResolvesDottedPropertiesAndNullIntermediates()
    {
        ComboBox comboBox = new()
        {
            DisplayMemberPath = "Address.City"
        };
        comboBox.SetItems(new[]
        {
            new Person(new Address("Iasi")),
            new Person(null)
        });

        comboBox.SelectedIndex = 0;
        Assert.Equal("Iasi", comboBox.Text);

        comboBox.SelectedIndex = 1;
        Assert.Equal(string.Empty, comboBox.Text);
    }

    [Fact]
    public void InvalidDisplayMemberPathThrowsDescriptiveException()
    {
        ComboBox comboBox = new()
        {
            DisplayMemberPath = "Address.Missing"
        };
        comboBox.SetItems(new[] { new Person(new Address("Iasi")) });

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
            () => comboBox.SelectedIndex = 0);

        Assert.Contains("Address.Missing", exception.Message);
        Assert.Contains("Missing", exception.Message);
    }

    [Fact]
    public void DefaultTemplateProvidesAllRequiredPartsAndClosedListIsNotHitTested()
    {
        UIRoot root = new(100, 100);
        ComboBox comboBox = new();
        comboBox.SetItems(new[] { "one", "two" });
        root.VisualChildren.Add(comboBox);
        root.ProcessFrame();

        ComponentTemplateInstance instance = Assert.IsType<ComponentTemplateInstance>(comboBox.ComponentTemplateInstance);
        Assert.IsType<ContentPresenter>(instance.Parts["PART_SelectionPresenter"]);
        Assert.IsType<TextBox>(instance.Parts["PART_EditableTextBox"]);
        Assert.IsType<Cerneala.UI.Controls.Primitives.ToggleButton>(instance.Parts["PART_DropDownToggle"]);
        Assert.IsType<Overlay>(instance.Parts["PART_DropDownOverlay"]);
        Assert.IsType<ItemsPresenter>(instance.Parts["PART_ItemsPresenter"]);
        Assert.DoesNotContain(root.VisualChildren, child => child.GetType().Name.Contains("OverlayLayer", StringComparison.Ordinal));
    }

    [Fact]
    public void DefaultToggleUsesTheScrollBarDownPathAndFill()
    {
        SolidColorBrush foreground = new(new Cerneala.Drawing.Color(12, 34, 56));
        ComboBox comboBox = new()
        {
            Foreground = foreground
        };
        comboBox.ApplyTemplate();
        ToggleButton toggle = Assert.IsType<ToggleButton>(
            comboBox.ComponentTemplateInstance!.Parts["PART_DropDownToggle"]);
        DirectionPath comboGlyph = Assert.IsType<DirectionPath>(toggle.Content);
        Cerneala.UI.Controls.Primitives.ScrollBar scrollBar = new()
        {
            Orientation = Orientation.Vertical
        };
        scrollBar.ApplyTemplate();
        Cerneala.UI.Controls.Primitives.RepeatButton increaseButton =
            Assert.IsType<Cerneala.UI.Controls.Primitives.RepeatButton>(
                scrollBar.ComponentTemplateInstance!.Parts["PART_IncreaseButton"]);
        DirectionPath scrollBarGlyph = Assert.IsType<DirectionPath>(increaseButton.Content);

        Assert.Same(scrollBarGlyph.Geometry, comboGlyph.Geometry);
        Assert.Same(scrollBarGlyph.Fill, comboGlyph.Fill);
    }

    [Fact]
    public void ClickingDefaultToggleProjectsANonEmptyDropDown()
    {
        UIRoot root = new(300, 200);
        ComboBox comboBox = new()
        {
            Width = 200,
            Background = new SolidColorBrush(new Cerneala.Drawing.Color(20, 24, 30)),
            Foreground = new SolidColorBrush(Cerneala.Drawing.Color.White)
        };
        comboBox.SetItems(new[] { "Normal", "Multiply", "Screen" });
        comboBox.SelectedIndex = 0;
        LayoutCanvas canvas = new();
        canvas.VisualChildren.Add(comboBox);
        root.VisualChildren.Add(canvas);
        root.ProcessFrame();
        ToggleButton toggle = Assert.IsType<ToggleButton>(
            comboBox.ComponentTemplateInstance!.Parts["PART_DropDownToggle"]);
        Overlay overlay = Assert.IsType<Overlay>(
            comboBox.ComponentTemplateInstance.Parts["PART_DropDownOverlay"]);
        ElementInputBridge bridge = new();
        float x = toggle.ArrangedBounds.X + toggle.ArrangedBounds.Width / 2;
        float y = toggle.ArrangedBounds.Y + toggle.ArrangedBounds.Height / 2;
        Assert.Same(toggle, new HitTestService().HitTest(root, x, y)?.Element);

        bridge.Dispatch(root, PointerFrame(x, y, currentDown: true));
        bridge.Dispatch(root, PointerFrame(x, y, previousDown: true));
        root.ProcessFrame();

        Assert.True(comboBox.IsDropDownOpen);
        Assert.True(overlay.IsProjected);
        Assert.Equal(comboBox.ArrangedBounds.Width, overlay.ProjectedPresenter.ArrangedBounds.Width);
        Assert.True(overlay.ProjectedPresenter.ArrangedBounds.Height > 0);
        Border dropDownBorder = Assert.IsType<Border>(overlay.Content);
        Assert.Same(comboBox.Background, dropDownBorder.Background);
        TextBlock[] itemText = Descendants(dropDownBorder).OfType<TextBlock>().ToArray();
        Assert.Equal(3, itemText.Length);
        Assert.All(itemText, text => Assert.Same(comboBox.Foreground, text.Foreground));
        DrawCommandList commands = root.RetainedRenderer.Commit(root);
        string[] renderedText = commands
            .Where(command => command.Kind == DrawCommandKind.DrawText)
            .Select(command => command.Text)
            .Where(text => text is not null)
            .Cast<string>()
            .ToArray();
        Assert.Contains("Normal", renderedText);
        Assert.Contains("Multiply", renderedText);
        Assert.Contains("Screen", renderedText);
    }

    [Fact]
    public void MissingRequiredTemplatePartFailsWhenTemplateIsApplied()
    {
        ComboBox comboBox = new();
        ComponentTemplate<ComboBox> invalid = new("invalid", _ => new LayoutGrid());

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
            () => comboBox.ComponentTemplate = invalid);

        Assert.Contains("PART_SelectionPresenter", exception.Message);
    }

    [Fact]
    public void KeyboardNavigationUpdatesSelectionAndEscapeOnlyCloses()
    {
        UIRoot root = new(100, 100);
        ComboBox comboBox = new();
        comboBox.SetItems(new[] { "one", "two", "three" });
        root.VisualChildren.Add(comboBox);
        root.ProcessFrame();
        ElementInputBridge bridge = new();
        bridge.FocusManager.Focus(comboBox, root.InputCache.EnsureCurrent(root));

        DispatchKey(root, bridge, InputKey.F4);
        DispatchKey(root, bridge, InputKey.Down);
        DispatchKey(root, bridge, InputKey.End);
        Assert.True(comboBox.IsDropDownOpen);
        Assert.Equal(2, comboBox.SelectedIndex);

        DispatchKey(root, bridge, InputKey.Escape);
        Assert.False(comboBox.IsDropDownOpen);
        Assert.Equal(2, comboBox.SelectedIndex);
    }

    [Fact]
    public void AltAndEnterCommandsControlDropDownWithoutDeferredSelection()
    {
        UIRoot root = new(100, 100);
        ComboBox comboBox = new();
        comboBox.SetItems(new[] { "one", "two" });
        root.VisualChildren.Add(comboBox);
        root.ProcessFrame();
        ElementInputBridge bridge = new();
        bridge.FocusManager.Focus(comboBox, root.InputCache.EnsureCurrent(root));

        DispatchKey(root, bridge, InputKey.Down, InputKey.LeftAlt);
        DispatchKey(root, bridge, InputKey.Home);
        Assert.True(comboBox.IsDropDownOpen);
        Assert.Equal(0, comboBox.SelectedIndex);

        DispatchKey(root, bridge, InputKey.Enter);
        Assert.False(comboBox.IsDropDownOpen);
        Assert.Equal(0, comboBox.SelectedIndex);

        DispatchKey(root, bridge, InputKey.Down, InputKey.LeftAlt);
        DispatchKey(root, bridge, InputKey.Up, InputKey.LeftAlt);
        Assert.False(comboBox.IsDropDownOpen);
    }

    [Fact]
    public void DropDownEventsTrackActualProjectionWithoutDuplicates()
    {
        UIRoot root = new(100, 100);
        ComboBox comboBox = new();
        LayoutCanvas canvas = new();
        canvas.VisualChildren.Add(comboBox);
        root.VisualChildren.Add(canvas);
        root.ProcessFrame();
        int opened = 0;
        int closed = 0;
        comboBox.DropDownOpened += (_, _) => opened++;
        comboBox.DropDownClosed += (_, _) => closed++;

        comboBox.IsDropDownOpen = true;
        comboBox.IsDropDownOpen = true;
        comboBox.IsDropDownOpen = false;

        Assert.Equal(1, opened);
        Assert.Equal(1, closed);

        comboBox.IsDropDownOpen = true;
        comboBox.IsEnabled = false;
        Assert.False(comboBox.IsDropDownOpen);
        Assert.Equal(2, opened);
        Assert.Equal(2, closed);
    }

    [Fact]
    public void RetemplatingWithdrawsOldOverlayAndDetachesOldHandlers()
    {
        UIRoot root = new(100, 100);
        ComboBox comboBox = new();
        LayoutCanvas canvas = new();
        canvas.VisualChildren.Add(comboBox);
        root.VisualChildren.Add(canvas);
        root.ProcessFrame();
        comboBox.IsDropDownOpen = true;
        ComponentTemplateInstance oldInstance = comboBox.ComponentTemplateInstance!;
        Overlay oldOverlay = Assert.IsType<Overlay>(oldInstance.Parts["PART_DropDownOverlay"]);
        ToggleButton oldToggle = Assert.IsType<ToggleButton>(oldInstance.Parts["PART_DropDownToggle"]);

        comboBox.ComponentTemplate = CreateComboBoxTemplate("replacement");

        Overlay newOverlay = Assert.IsType<Overlay>(
            comboBox.ComponentTemplateInstance!.Parts["PART_DropDownOverlay"]);
        Assert.False(oldOverlay.IsOpen);
        Assert.True(newOverlay.IsOpen);

        comboBox.IsDropDownOpen = false;
        oldToggle.RaiseEvent(new RoutedEventArgs(ToggleButton.CheckedEvent, oldToggle));
        Assert.False(comboBox.IsDropDownOpen);

        comboBox.IsDropDownOpen = true;
        Assert.True(newOverlay.IsOpen);
    }

    private static InputFrame PointerFrame(float x, float y, bool previousDown = false, bool currentDown = false)
    {
        PointerSnapshot previous = PointerSnapshot.Empty.WithPosition(x, y);
        PointerSnapshot current = PointerSnapshot.Empty.WithPosition(x, y);
        if (previousDown)
        {
            previous = previous.WithButton(InputMouseButton.Left, true);
        }

        if (currentDown)
        {
            current = current.WithButton(InputMouseButton.Left, true);
        }

        return new InputFrame(previous, current, KeyboardSnapshot.Empty, KeyboardSnapshot.Empty, []);
    }

    private static void DispatchKey(
        UIRoot root,
        ElementInputBridge bridge,
        InputKey key,
        params InputKey[] modifiers)
    {
        InputKey[] downKeys = [key, .. modifiers];
        bridge.Dispatch(
            root,
            new InputFrame(
                PointerSnapshot.Empty,
                PointerSnapshot.Empty,
                KeyboardSnapshot.Empty,
                KeyboardSnapshot.FromDownKeys(downKeys),
                []));
        bridge.Dispatch(
            root,
            new InputFrame(
                PointerSnapshot.Empty,
                PointerSnapshot.Empty,
                KeyboardSnapshot.FromDownKeys(downKeys),
                KeyboardSnapshot.Empty,
                []));
    }

    private static ComponentTemplate<ComboBox> CreateComboBoxTemplate(string key)
    {
        return new ComponentTemplate<ComboBox>(key, context =>
        {
            ContentPresenter selection = new();
            TextBox editor = new();
            ToggleButton toggle = new();
            ItemsPresenter items = new();
            Overlay overlay = new()
            {
                Content = items
            };
            LayoutGrid root = new();
            root.VisualChildren.Add(selection);
            root.VisualChildren.Add(editor);
            root.VisualChildren.Add(toggle);
            root.VisualChildren.Add(overlay);
            context.RequirePart("PART_SelectionPresenter", selection);
            context.RequirePart("PART_EditableTextBox", editor);
            context.RequirePart("PART_DropDownToggle", toggle);
            context.RequirePart("PART_DropDownOverlay", overlay);
            context.RequirePart("PART_ItemsPresenter", items);
            return root;
        });
    }

    private sealed record Person(Address? Address);

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

    private sealed record Address(string City);

    private sealed class FixedElement : UIElement
    {
        protected override LayoutSize MeasureCore(MeasureContext context)
        {
            return new LayoutSize(20, 10);
        }
    }
}
