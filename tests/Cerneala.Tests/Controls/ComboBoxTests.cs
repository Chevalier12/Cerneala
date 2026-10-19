using System.Collections.ObjectModel;
using System.Collections.Specialized;
using Cerneala.UI.Controls;
using Cerneala.UI.Controls.Items;
using Cerneala.UI.Controls.Templates;
using Cerneala.Drawing;
using Cerneala.UI.Core;
using Cerneala.UI.Data;
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
    public void EditableFilteringDefaultsAreEnabled()
    {
        ComboBox comboBox = new();

        Assert.True(comboBox.IsEditable);
        Assert.False(comboBox.IsReadOnly);
        Assert.True(comboBox.IsTextSearchEnabled);
        Assert.False(comboBox.IsTextSearchCaseSensitive);
        Assert.False(comboBox.ShouldPreserveUserEnteredPrefix);
        Assert.True(comboBox.IsTextFilterEnabled);
        Assert.Equal(UiPropertyValueSource.Default, comboBox.GetValueSource(Control.BackgroundProperty));
        Assert.Equal(UiPropertyValueSource.Default, comboBox.GetValueSource(Control.ForegroundProperty));
        Assert.Equal(Color.White, Assert.IsType<SolidColorBrush>(comboBox.Background).Color);
        Assert.Equal(Color.Black, Assert.IsType<SolidColorBrush>(comboBox.Foreground).Color);
    }

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
    public void FilteringAnOpenDropDownShrinksItsHeightToTheFilteredItems()
    {
        UIRoot root = new(400, 500);
        ComboBox comboBox = new()
        {
            Width = 180,
            MaxDropDownHeight = 180
        };
        comboBox.SetItems(new[] { "DEFAULT", "ARROW", "HAND", "IBEAM", "CROSSHAIR" });
        LayoutCanvas canvas = new();
        canvas.VisualChildren.Add(comboBox);
        root.VisualChildren.Add(canvas);
        root.ProcessFrame();
        TextBox editor = Assert.IsType<TextBox>(
            comboBox.ComponentTemplateInstance!.Parts["PART_EditableTextBox"]);
        ElementInputBridge bridge = new();
        bridge.FocusManager.Focus(editor, root.InputCache.EnsureCurrent(root));

        bridge.Dispatch(root, TextInputFrame("a"));
        root.ProcessFrame();
        root.ProcessFrame();

        Overlay overlay = Assert.IsType<Overlay>(
            comboBox.ComponentTemplateInstance.Parts["PART_DropDownOverlay"]);
        Border border = Assert.IsType<Border>(overlay.Content);
        ScrollViewer scrollViewer = Assert.IsType<ScrollViewer>(border.Child);
        Assert.True(comboBox.IsDropDownOpen);
        Assert.Equal([1, 2, 0, 3, 4], RealizedSourceIndices(comboBox));
        Assert.Equal(
            scrollViewer.Presenter.ExtentHeight + border.BorderThickness.Vertical,
            overlay.ProjectedPresenter.ArrangedBounds.Height);
        Assert.True(overlay.ProjectedPresenter.ArrangedBounds.Height < comboBox.MaxDropDownHeight);
    }

    [Fact]
    public void DeletingFilterTextRealizesTheExpandedAutoSizedDropDown()
    {
        UIRoot root = new(400, 500);
        ComboBox comboBox = new()
        {
            Width = 180
        };
        comboBox.SetItems(new[] { "DEFAULT", "ARROW", "HAND", "IBEAM", "CROSSHAIR" });
        comboBox.SelectedIndex = 0;
        LayoutCanvas canvas = new();
        canvas.VisualChildren.Add(comboBox);
        root.VisualChildren.Add(canvas);
        root.ProcessFrame();
        TextBox editor = Assert.IsType<TextBox>(
            comboBox.ComponentTemplateInstance!.Parts["PART_EditableTextBox"]);
        ElementInputBridge bridge = new();
        bridge.FocusManager.Focus(editor, root.InputCache.EnsureCurrent(root));

        for (int index = 0; index < "DEFAULT".Length; index++)
        {
            DispatchKey(root, bridge, InputKey.Back);
            root.ProcessFrame();
        }

        Overlay overlay = Assert.IsType<Overlay>(
            comboBox.ComponentTemplateInstance.Parts["PART_DropDownOverlay"]);
        Border border = Assert.IsType<Border>(overlay.Content);
        ScrollViewer scrollViewer = Assert.IsType<ScrollViewer>(border.Child);
        Assert.Equal(string.Empty, editor.Text);
        Assert.True(comboBox.IsDropDownOpen);
        Assert.Equal([0, 1, 2, 3, 4], RealizedSourceIndices(comboBox));
        Assert.Equal(
            scrollViewer.Presenter.ExtentHeight + border.BorderThickness.Vertical,
            overlay.ProjectedPresenter.ArrangedBounds.Height);
        Assert.True(overlay.ProjectedPresenter.ArrangedBounds.Height < comboBox.MaxDropDownHeight);
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
            ItemsPanel = new LayoutStackPanel(),
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
    public void ClickingItemAfterRemovingEarlierItemBeforeLayoutSelectsClickedItem()
    {
        UIRoot root = new(100, 100);
        ComboBox comboBox = new()
        {
            ItemsPanel = new LayoutStackPanel(),
            IsDropDownOpen = true
        };
        UIElement[] items = [new FixedElement(), new FixedElement(), new FixedElement(), new FixedElement()];
        comboBox.SetItems(items);
        LayoutCanvas canvas = new();
        canvas.VisualChildren.Add(comboBox);
        root.VisualChildren.Add(canvas);
        root.ProcessFrame();
        UIElement clicked = comboBox.ItemContainerGenerator.RealizedContainers[2];
        float x = clicked.ArrangedBounds.X + 2;
        float y = clicked.ArrangedBounds.Y + 2;

        comboBox.Items.RemoveAt(0);
        ElementInputBridge bridge = new();
        bridge.Dispatch(root, PointerFrame(x, y, currentDown: true));
        bridge.Dispatch(root, PointerFrame(x, y, previousDown: true));

        Assert.Equal(1, comboBox.SelectedIndex);
        Assert.Same(items[2], comboBox.SelectedItem);
        Assert.False(comboBox.IsDropDownOpen);
    }

    [Fact]
    public void ClickingItemAfterReplacingItsItemBeforeLayoutSelectsNothing()
    {
        UIRoot root = new(100, 100);
        ComboBox comboBox = new()
        {
            ItemsPanel = new LayoutStackPanel(),
            IsDropDownOpen = true
        };
        comboBox.SetItems(new[] { "a", "b", "c" });
        LayoutCanvas canvas = new();
        canvas.VisualChildren.Add(comboBox);
        root.VisualChildren.Add(canvas);
        root.ProcessFrame();
        // Text rows measure 0 px tall without a font, so each row gets a fixed height.
        foreach (UIElement container in comboBox.ItemContainerGenerator.RealizedContainers.Values)
        {
            container.Height = 10;
        }

        root.ProcessFrame();
        UIElement clicked = comboBox.ItemContainerGenerator.RealizedContainers[1];
        float x = clicked.ArrangedBounds.X + 2;
        float y = clicked.ArrangedBounds.Y + 2;

        comboBox.Items[1] = "x";
        ElementInputBridge bridge = new();
        bridge.Dispatch(root, PointerFrame(x, y, currentDown: true));
        bridge.Dispatch(root, PointerFrame(x, y, previousDown: true));

        Assert.Equal("b", ItemContainerGenerator.GetItem(clicked));
        Assert.Equal(-1, comboBox.SelectedIndex);
        Assert.Null(comboBox.SelectedItem);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ClickingItemWhoseReplacedItemPreparationThrewSelectsNothing(bool keepFailing)
    {
        UIRoot root = new(100, 100);
        FailingContentComboBox comboBox = new()
        {
            ItemsPanel = new LayoutStackPanel(),
            IsDropDownOpen = true
        };
        comboBox.SetItems(new[] { "a", "b", "c" });
        LayoutCanvas canvas = new();
        canvas.VisualChildren.Add(comboBox);
        root.VisualChildren.Add(canvas);
        root.ProcessFrame();
        // Text rows measure 0 px tall without a font, so each row gets a fixed height.
        foreach (UIElement container in comboBox.ItemContainerGenerator.RealizedContainers.Values)
        {
            container.Height = 10;
        }

        root.ProcessFrame();
        UIElement clicked = comboBox.ItemContainerGenerator.RealizedContainers[1];
        float x = clicked.ArrangedBounds.X + 2;
        float y = clicked.ArrangedBounds.Y + 2;
        int selectionChanges = 0;
        comboBox.SelectionChanged += (_, _) => selectionChanges++;

        comboBox.Items[1] = "x";
        comboBox.FailFor = "x";
        Assert.Throws<InvalidOperationException>(() => root.ProcessFrame());
        if (!keepFailing)
        {
            comboBox.FailFor = null;
        }

        ElementInputBridge bridge = new();
        bridge.Dispatch(root, PointerFrame(x, y, currentDown: true));
        bridge.Dispatch(root, PointerFrame(x, y, previousDown: true));

        Assert.Same(clicked, comboBox.ItemContainerGenerator.RealizedContainers[1]);
        Assert.Equal(-1, comboBox.SelectedIndex);
        Assert.Null(comboBox.SelectedItem);
        Assert.Equal(0, selectionChanges);
    }

    [Fact]
    public void EditableTextThatDiffersFromSelectionClearsSelection()
    {
        ComboBox comboBox = new()
        {
            IsEditable = true,
            IsTextFilterEnabled = false
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
            IsEditable = true,
            IsTextFilterEnabled = false
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
    public void EditableTextSearchCompletesFirstPrefixMatchAndSelectsSuffix()
    {
        ComboBox comboBox = new()
        {
            IsEditable = true,
            IsTextFilterEnabled = false
        };
        comboBox.SetItems(new[] { "Alpha", "Alpine", "Beta" });
        comboBox.ApplyTemplate();
        TextBox editor = Assert.IsType<TextBox>(
            comboBox.ComponentTemplateInstance!.Parts["PART_EditableTextBox"]);

        editor.ReceiveTextInput("a");

        Assert.Equal(0, comboBox.SelectedIndex);
        Assert.Equal("Alpha", comboBox.Text);
        Assert.Equal("Alpha", editor.Text);
        Assert.Equal(1, editor.Selection.Start);
        Assert.Equal(5, editor.Selection.End);

        editor.ReceiveTextInput("l");

        Assert.Equal("Alpha", comboBox.Text);
        Assert.Equal(2, editor.Selection.Start);
        Assert.Equal(5, editor.Selection.End);
    }

    [Fact]
    public void EditableEditorReceivesSelectAllChordAfterPointerFocus()
    {
        UIRoot root = new(300, 120);
        ComboBox comboBox = new()
        {
            IsEditable = true,
            Width = 180
        };
        comboBox.SetItems(new[] { "Apricot", "Apple" });
        comboBox.SelectedIndex = 0;
        root.VisualChildren.Add(comboBox);
        root.ProcessFrame();
        TextBox editor = Assert.IsType<TextBox>(
            comboBox.ComponentTemplateInstance!.Parts["PART_EditableTextBox"]);
        ElementInputBridge bridge = new();
        float x = editor.ArrangedBounds.X + 8;
        float y = editor.ArrangedBounds.Y + (editor.ArrangedBounds.Height / 2);

        bridge.Dispatch(root, PointerFrame(x, y, currentDown: true));
        bridge.Dispatch(root, PointerFrame(x, y, previousDown: true));
        DispatchKey(root, bridge, InputKey.A, InputKey.LeftCtrl);
        bridge.Dispatch(root, TextInputFrame("aplpe"));

        Assert.Same(editor, bridge.FocusManager.FocusedElement);
        Assert.Equal("aplpe", editor.Text);
    }

    [Fact]
    public void EditableFieldClickFocusesEditorAcrossTheFullFieldHeight()
    {
        UIRoot root = new(300, 120);
        ComboBox comboBox = new()
        {
            Height = 40,
            Width = 180
        };
        comboBox.SetItems(new[] { "Apricot", "Apple" });
        comboBox.SelectedIndex = 0;
        root.VisualChildren.Add(comboBox);
        root.ProcessFrame();
        TextBox editor = Assert.IsType<TextBox>(
            comboBox.ComponentTemplateInstance!.Parts["PART_EditableTextBox"]);
        ElementInputBridge bridge = new();
        float x = editor.ArrangedBounds.X + 8;
        float y = comboBox.ArrangedBounds.Y + 2;

        bridge.Dispatch(root, PointerFrame(x, y, currentDown: true));
        bridge.Dispatch(root, PointerFrame(x, y, previousDown: true));
        DispatchKey(root, bridge, InputKey.A, InputKey.LeftCtrl);

        Assert.Same(editor, bridge.FocusManager.FocusedElement);
        Assert.Equal(0, editor.Selection.Start);
        Assert.Equal(editor.Text.Length, editor.Selection.End);
        Assert.False(comboBox.IsDropDownOpen);
    }

    [Theory]
    [InlineData("middle")]
    [InlineData("left")]
    [InlineData("toggle")]
    public void NonEditableFieldClickTogglesDropDown(string where)
    {
        (UIRoot root, ComboBox comboBox) = CreateNonEditableField();
        ToggleButton toggle = Assert.IsType<ToggleButton>(
            comboBox.ComponentTemplateInstance!.Parts["PART_DropDownToggle"]);
        Overlay overlay = Assert.IsType<Overlay>(
            comboBox.ComponentTemplateInstance.Parts["PART_DropDownOverlay"]);
        LayoutRect bounds = comboBox.ArrangedBounds;
        float y = bounds.Y + bounds.Height / 2;
        float x = where switch
        {
            "middle" => bounds.X + bounds.Width / 2,
            "left" => bounds.X + 8,
            _ => toggle.ArrangedBounds.X + toggle.ArrangedBounds.Width / 2
        };
        UIElement? hit = new HitTestService().HitTest(root, x, y)?.Element;
        Assert.Equal(where == "toggle", ReferenceEquals(hit, toggle));
        ElementInputBridge bridge = new();

        bridge.Dispatch(root, PointerFrame(x, y, currentDown: true));
        bridge.Dispatch(root, PointerFrame(x, y, previousDown: true));
        root.ProcessFrame();

        Assert.True(comboBox.IsDropDownOpen);
        Assert.True(overlay.IsOpen);
        Assert.True(overlay.IsProjected);
        Assert.True(toggle.IsChecked);

        bridge.Dispatch(root, PointerFrame(x, y, currentDown: true));
        bridge.Dispatch(root, PointerFrame(x, y, previousDown: true));
        root.ProcessFrame();

        Assert.False(comboBox.IsDropDownOpen);
        Assert.False(overlay.IsOpen);
        Assert.False(overlay.IsProjected);
        Assert.False(toggle.IsChecked);
        Assert.Equal(0, comboBox.SelectedIndex);
    }

    // The non-hit-testable selection box lets a press on the selected text hit the field itself, so a press there
    // and a release on the empty part of the field make one click. A press on the field and a release on
    // PART_DropDownToggle hit different elements and make no click.
    [Theory]
    [InlineData("text", "empty", true)]
    [InlineData("empty", "toggle", false)]
    [InlineData("toggle", "empty", false)]
    public void NonEditableFieldPressAndReleaseOnDifferentPointsTogglesOnlyOnSameHitElement(
        string pressAt,
        string releaseAt,
        bool expectedOpen)
    {
        (UIRoot root, ComboBox comboBox) = CreateNonEditableField();
        ToggleButton toggle = Assert.IsType<ToggleButton>(
            comboBox.ComponentTemplateInstance!.Parts["PART_DropDownToggle"]);
        TextBlock text = Assert.IsType<TextBlock>(FindDescendant<TextBlock>(
            comboBox.ComponentTemplateInstance.Parts["PART_SelectionPresenter"]));
        Assert.Equal("one", text.Text);
        LayoutRect bounds = comboBox.ArrangedBounds;
        float y = bounds.Y + bounds.Height / 2;
        float PointX(string where) => where switch
        {
            "text" => text.ArrangedBounds.X + text.ArrangedBounds.Width / 2,
            "empty" => bounds.X + bounds.Width * 0.6f,
            _ => toggle.ArrangedBounds.X + toggle.ArrangedBounds.Width / 2
        };
        float pressX = PointX(pressAt);
        float releaseX = PointX(releaseAt);
        ElementInputBridge bridge = new();

        bridge.Dispatch(root, PointerFrame(pressX, y, currentDown: true));
        bridge.Dispatch(root, PointerFrame(releaseX, y, previousDown: true, currentDown: true));
        bridge.Dispatch(root, PointerFrame(releaseX, y, previousDown: true));
        root.ProcessFrame();

        Assert.Equal(expectedOpen, comboBox.IsDropDownOpen);
        Assert.Equal(expectedOpen, toggle.IsChecked);
        Assert.Equal(0, comboBox.SelectedIndex);
    }

    [Fact]
    public void NonEditableItemClickSelectsItemAndClosesDropDown()
    {
        (UIRoot root, ComboBox comboBox) = CreateNonEditableField();
        LayoutRect bounds = comboBox.ArrangedBounds;
        float fieldX = bounds.X + bounds.Width / 2;
        float fieldY = bounds.Y + bounds.Height / 2;
        ElementInputBridge bridge = new();
        bridge.Dispatch(root, PointerFrame(fieldX, fieldY, currentDown: true));
        bridge.Dispatch(root, PointerFrame(fieldX, fieldY, previousDown: true));
        root.ProcessFrame();
        Assert.True(comboBox.IsDropDownOpen);
        UIElement second = comboBox.ItemContainerGenerator.RealizedContainers[1];
        float itemX = second.ArrangedBounds.X + second.ArrangedBounds.Width / 2;
        float itemY = second.ArrangedBounds.Y + second.ArrangedBounds.Height / 2;

        bridge.Dispatch(root, PointerFrame(itemX, itemY, currentDown: true));
        bridge.Dispatch(root, PointerFrame(itemX, itemY, previousDown: true));
        root.ProcessFrame();

        Assert.Equal(1, comboBox.SelectedIndex);
        Assert.Equal("two", comboBox.Text);
        Assert.False(comboBox.IsDropDownOpen);
    }

    // As in WPF, where ButtonBase marks the mouse button events handled, a button in a drop-down item owns its click:
    // it neither selects the item nor closes the drop-down.
    [Fact]
    public void NonEditableItemClickOnTemplateCheckBoxChecksItWithoutSelectingItemOrClosingDropDown()
    {
        (UIRoot root, ComboBox comboBox) = CreateNonEditableField();
        comboBox.ItemTemplate = new ContentTemplate<string>(
            "check",
            key: null,
            priority: 0,
            context => new CheckBox { Content = context.Data });
        root.ProcessFrame();
        LayoutRect field = comboBox.ArrangedBounds;
        ElementInputBridge bridge = new();
        bridge.Dispatch(root, PointerFrame(field.X + field.Width / 2, field.Y + field.Height / 2, currentDown: true));
        bridge.Dispatch(root, PointerFrame(field.X + field.Width / 2, field.Y + field.Height / 2, previousDown: true));
        root.ProcessFrame();
        Assert.True(comboBox.IsDropDownOpen);
        UIElement second = comboBox.ItemContainerGenerator.RealizedContainers[1];
        CheckBox check = Assert.IsType<CheckBox>(FindDescendant<CheckBox>(second));
        int clicks = 0;
        check.Click += (_, _) => clicks++;
        LayoutRect bounds = check.ArrangedBounds;
        float x = bounds.X + 6;
        float y = bounds.Y + bounds.Height / 2;

        bridge.Dispatch(root, PointerFrame(x, y, currentDown: true));
        bridge.Dispatch(root, PointerFrame(x, y, previousDown: true));
        root.ProcessFrame();

        Assert.Equal(1, clicks);
        Assert.True(check.IsChecked);
        Assert.Equal(0, comboBox.SelectedIndex);
        Assert.True(comboBox.IsDropDownOpen);
    }

    // A release on a button in a drop-down item that the button does not turn into a click, because the press was on
    // the empty part of the item, is not the button's click: it selects the item and closes the drop-down.
    [Fact]
    public void NonEditableItemPressOnEmptyPartReleasedOnTemplateCheckBoxSelectsItemWithoutCheckingIt()
    {
        (UIRoot root, ComboBox comboBox) = CreateNonEditableField();
        comboBox.ItemTemplate = new ContentTemplate<string>(
            "check",
            key: null,
            priority: 0,
            context => new CheckBox { Content = context.Data, Width = 30 });
        root.ProcessFrame();
        LayoutRect field = comboBox.ArrangedBounds;
        ElementInputBridge bridge = new();
        bridge.Dispatch(root, PointerFrame(field.X + field.Width / 2, field.Y + field.Height / 2, currentDown: true));
        bridge.Dispatch(root, PointerFrame(field.X + field.Width / 2, field.Y + field.Height / 2, previousDown: true));
        root.ProcessFrame();
        Assert.True(comboBox.IsDropDownOpen);
        UIElement second = comboBox.ItemContainerGenerator.RealizedContainers[1];
        CheckBox check = Assert.IsType<CheckBox>(FindDescendant<CheckBox>(second));
        int clicks = 0;
        check.Click += (_, _) => clicks++;
        LayoutRect itemBounds = second.ArrangedBounds;
        LayoutRect checkBounds = check.ArrangedBounds;
        float pressX = itemBounds.X + itemBounds.Width - 4;
        Assert.True(pressX > checkBounds.X + checkBounds.Width);
        float releaseX = checkBounds.X + 6;
        float releaseY = checkBounds.Y + checkBounds.Height / 2;

        bridge.Dispatch(root, PointerFrame(pressX, itemBounds.Y + itemBounds.Height / 2, currentDown: true));
        bridge.Dispatch(root, PointerFrame(releaseX, releaseY, previousDown: true, currentDown: true));
        bridge.Dispatch(root, PointerFrame(releaseX, releaseY, previousDown: true));
        root.ProcessFrame();

        Assert.Equal(0, clicks);
        Assert.False(check.IsChecked);
        Assert.Equal(1, comboBox.SelectedIndex);
        Assert.False(comboBox.IsDropDownOpen);
    }

    // As in the WPF default template, the selection box is not hit-testable, so a click on a button that the
    // ItemTemplate shows in the closed field only toggles the drop-down.
    [Fact]
    public void NonEditableFieldClickOnTemplateButtonTogglesDropDownWithoutClickingButton()
    {
        (UIRoot root, ComboBox comboBox) = CreateNonEditableField();
        comboBox.ItemTemplate = new ContentTemplate<string>(
            "check",
            key: null,
            priority: 0,
            context => new CheckBox { Content = context.Data });
        root.ProcessFrame();
        CheckBox check = Assert.IsType<CheckBox>(FindDescendant<CheckBox>(
            comboBox.ComponentTemplateInstance!.Parts["PART_SelectionPresenter"]));
        int clicks = 0;
        check.Click += (_, _) => clicks++;
        LayoutRect bounds = check.ArrangedBounds;
        float x = bounds.X + 6;
        float y = bounds.Y + bounds.Height / 2;
        ElementInputBridge bridge = new();

        bridge.Dispatch(root, PointerFrame(x, y, currentDown: true));
        bridge.Dispatch(root, PointerFrame(x, y, previousDown: true));
        root.ProcessFrame();

        Assert.Equal(0, clicks);
        Assert.False(check.IsChecked);
        Assert.True(comboBox.IsDropDownOpen);

        bridge.Dispatch(root, PointerFrame(x, y, currentDown: true));
        bridge.Dispatch(root, PointerFrame(x, y, previousDown: true));
        root.ProcessFrame();

        Assert.Equal(0, clicks);
        Assert.False(check.IsChecked);
        Assert.False(comboBox.IsDropDownOpen);
        Assert.Equal(0, comboBox.SelectedIndex);
    }

    // A custom template can leave the selection box hit-testable. A button that the ItemTemplate shows there
    // then owns the click, as the bridge resolves the click to that command source, and the field stays closed.
    [Fact]
    public void NonEditableFieldClickOnHitTestableTemplateButtonClicksButtonWithoutTogglingDropDown()
    {
        (UIRoot root, ComboBox comboBox) = CreateNonEditableField();
        comboBox.ItemTemplate = new ContentTemplate<string>(
            "check",
            key: null,
            priority: 0,
            context => new CheckBox { Content = context.Data });
        root.ProcessFrame();
        UIElement presenter = comboBox.ComponentTemplateInstance!.Parts["PART_SelectionPresenter"];
        presenter.IsHitTestVisible = true;
        root.ProcessFrame();
        CheckBox check = Assert.IsType<CheckBox>(FindDescendant<CheckBox>(presenter));
        int clicks = 0;
        check.Click += (_, _) => clicks++;
        LayoutRect bounds = check.ArrangedBounds;
        float x = bounds.X + 6;
        float y = bounds.Y + bounds.Height / 2;
        ElementInputBridge bridge = new();

        bridge.Dispatch(root, PointerFrame(x, y, currentDown: true));
        bridge.Dispatch(root, PointerFrame(x, y, previousDown: true));
        root.ProcessFrame();

        Assert.Equal(1, clicks);
        Assert.True(check.IsChecked);
        Assert.False(comboBox.IsDropDownOpen);

        bridge.Dispatch(root, PointerFrame(x, y, currentDown: true));
        bridge.Dispatch(root, PointerFrame(x, y, previousDown: true));
        root.ProcessFrame();

        Assert.Equal(2, clicks);
        Assert.False(check.IsChecked);
        Assert.False(comboBox.IsDropDownOpen);
        Assert.Equal(0, comboBox.SelectedIndex);
    }

    // A text box that the ItemTemplate shows in a hit-testable selection box owns its press as a pointer drag
    // source, as the bridge resolves it, so the click focuses the text box and the field stays closed (#393).
    [Fact]
    public void NonEditableFieldClickOnHitTestableTemplateTextBoxFocusesTextBoxWithoutTogglingDropDown()
    {
        (UIRoot root, ComboBox comboBox) = CreateNonEditableField();
        comboBox.ItemTemplate = new ContentTemplate<string>(
            "text",
            key: null,
            priority: 0,
            context => new TextBox { Text = context.Data });
        root.ProcessFrame();
        UIElement presenter = comboBox.ComponentTemplateInstance!.Parts["PART_SelectionPresenter"];
        presenter.IsHitTestVisible = true;
        root.ProcessFrame();
        TextBox box = Assert.IsType<TextBox>(FindDescendant<TextBox>(presenter));
        LayoutRect bounds = box.ArrangedBounds;
        float x = bounds.X + 6;
        float y = bounds.Y + bounds.Height / 2;
        UIElement? hit = new HitTestService().HitTest(root, x, y)?.Element;
        Assert.NotNull(hit);
        Assert.True(ReferenceEquals(hit, box) || IsDescendantOf(hit!, box));
        ElementInputBridge bridge = new();

        bridge.Dispatch(root, PointerFrame(x, y, currentDown: true));
        bridge.Dispatch(root, PointerFrame(x, y, previousDown: true));
        root.ProcessFrame();

        Assert.Same(box, bridge.FocusManager.FocusedElement);
        Assert.False(comboBox.IsDropDownOpen);

        bridge.Dispatch(root, PointerFrame(x, y, currentDown: true));
        bridge.Dispatch(root, PointerFrame(x, y, previousDown: true));
        root.ProcessFrame();

        Assert.Same(box, bridge.FocusManager.FocusedElement);
        Assert.False(comboBox.IsDropDownOpen);
        Assert.Equal(0, comboBox.SelectedIndex);
    }

    [Fact]
    public void NonEditableFieldClickOnHitTestableTemplateTextTogglesDropDown()
    {
        (UIRoot root, ComboBox comboBox) = CreateNonEditableField();
        UIElement presenter = comboBox.ComponentTemplateInstance!.Parts["PART_SelectionPresenter"];
        presenter.IsHitTestVisible = true;
        root.ProcessFrame();
        LayoutRect bounds = comboBox.ArrangedBounds;
        float x = bounds.X + 8;
        float y = bounds.Y + bounds.Height / 2;
        UIElement? hit = new HitTestService().HitTest(root, x, y)?.Element;
        Assert.NotNull(hit);
        Assert.True(ReferenceEquals(hit, presenter) || IsDescendantOf(hit!, presenter));
        ElementInputBridge bridge = new();

        bridge.Dispatch(root, PointerFrame(x, y, currentDown: true));
        bridge.Dispatch(root, PointerFrame(x, y, previousDown: true));
        root.ProcessFrame();

        Assert.True(comboBox.IsDropDownOpen);

        bridge.Dispatch(root, PointerFrame(x, y, currentDown: true));
        bridge.Dispatch(root, PointerFrame(x, y, previousDown: true));
        root.ProcessFrame();

        Assert.False(comboBox.IsDropDownOpen);
    }

    // As in WPF, where the template's ToggleButton handles the field's click, one click on the field of a
    // non-editable ComboBox inside a Button only toggles the drop-down: the outer Button raises no Click and runs
    // no Command. A click on the outer Button outside the ComboBox still clicks it once.
    [Fact]
    public void NonEditableFieldClickInsideButtonTogglesDropDownWithoutClickingButton()
    {
        UIRoot root = new(300, 200);
        ComboBox comboBox = new() { IsEditable = false, Width = 180, Height = 32 };
        comboBox.SetItems(new[] { "one", "two", "three" });
        comboBox.SelectedIndex = 0;
        CountingCommand command = new();
        Button outer = new() { Content = comboBox, Command = command };
        LayoutCanvas canvas = new();
        LayoutCanvas.SetLeft(outer, 60);
        canvas.VisualChildren.Add(outer);
        root.VisualChildren.Add(canvas);
        root.ProcessFrame();
        int outerClicks = 0;
        outer.Click += (_, _) => outerClicks++;
        LayoutRect fieldBounds = comboBox.ArrangedBounds;
        float fieldX = fieldBounds.X + fieldBounds.Width / 2;
        float fieldY = fieldBounds.Y + fieldBounds.Height / 2;
        ElementInputBridge bridge = new();

        bridge.Dispatch(root, PointerFrame(fieldX, fieldY, currentDown: true));
        bridge.Dispatch(root, PointerFrame(fieldX, fieldY, previousDown: true));
        root.ProcessFrame();

        Assert.True(comboBox.IsDropDownOpen);
        Assert.Equal(0, outerClicks);
        Assert.Equal(0, command.Executions);

        bridge.Dispatch(root, PointerFrame(fieldX, fieldY, currentDown: true));
        bridge.Dispatch(root, PointerFrame(fieldX, fieldY, previousDown: true));
        root.ProcessFrame();

        Assert.False(comboBox.IsDropDownOpen);
        Assert.Equal(0, outerClicks);
        Assert.Equal(0, command.Executions);

        LayoutRect outerBounds = outer.ArrangedBounds;
        float outerX = outerBounds.X + 2;
        Assert.True(outerX < fieldBounds.X);
        Assert.Same(outer, new HitTestService().HitTest(root, outerX, fieldY)?.Element);

        bridge.Dispatch(root, PointerFrame(outerX, fieldY, currentDown: true));
        bridge.Dispatch(root, PointerFrame(outerX, fieldY, previousDown: true));
        root.ProcessFrame();

        Assert.False(comboBox.IsDropDownOpen);
        Assert.Equal(1, outerClicks);
        Assert.Equal(1, command.Executions);
    }

    // As in WPF, where the outer Button takes a press outside the ComboBox and the template's ToggleButton never sees
    // it, a press on the outer Button followed by a release on the field is not a field click: the drop-down stays
    // closed and the outer Button clicks once (#468). A press and a release on the field still only toggle (#392).
    [Fact]
    public void NonEditableFieldInsideButtonTogglesOnlyWhenThePressAlsoHitsTheField()
    {
        UIRoot root = new(300, 200);
        ComboBox comboBox = new() { IsEditable = false, Width = 180, Height = 32 };
        comboBox.SetItems(new[] { "one", "two", "three" });
        comboBox.SelectedIndex = 0;
        CountingCommand command = new();
        Button outer = new() { Content = comboBox, Command = command };
        LayoutCanvas canvas = new();
        LayoutCanvas.SetLeft(outer, 60);
        canvas.VisualChildren.Add(outer);
        root.VisualChildren.Add(canvas);
        root.ProcessFrame();
        int outerClicks = 0;
        outer.Click += (_, _) => outerClicks++;
        LayoutRect fieldBounds = comboBox.ArrangedBounds;
        float fieldX = fieldBounds.X + fieldBounds.Width / 2;
        float fieldY = fieldBounds.Y + fieldBounds.Height / 2;
        float outerX = outer.ArrangedBounds.X + 2;
        Assert.Same(outer, new HitTestService().HitTest(root, outerX, fieldY)?.Element);
        ElementInputBridge bridge = new();

        bridge.Dispatch(root, PointerFrame(outerX, fieldY, currentDown: true));
        bridge.Dispatch(root, PointerFrame(fieldX, fieldY, previousDown: true, currentDown: true));
        bridge.Dispatch(root, PointerFrame(fieldX, fieldY, previousDown: true));
        root.ProcessFrame();

        Assert.False(comboBox.IsDropDownOpen);
        Assert.Equal(1, outerClicks);
        Assert.Equal(1, command.Executions);

        bridge.Dispatch(root, PointerFrame(fieldX, fieldY, currentDown: true));
        bridge.Dispatch(root, PointerFrame(fieldX, fieldY, previousDown: true));
        root.ProcessFrame();

        Assert.True(comboBox.IsDropDownOpen);
        Assert.Equal(1, outerClicks);
        Assert.Equal(1, command.Executions);
    }

    // As in WPF, where the template's ToggleButton handles the field's press, a press held on the field of a
    // non-editable ComboBox inside a RepeatButton does not start the RepeatButton: it raises no Click and runs no
    // Command, and the release only opens the drop-down. The same holds for a press on PART_DropDownToggle. A press
    // held on the RepeatButton outside the ComboBox still clicks and repeats.
    [Theory]
    [InlineData("middle")]
    [InlineData("toggle")]
    public void NonEditableFieldPressInsideRepeatButtonDoesNotClickOrRepeatIt(string where)
    {
        UIRoot root = new(300, 200);
        ComboBox comboBox = new() { IsEditable = false, Width = 180, Height = 32 };
        comboBox.SetItems(new[] { "one", "two", "three" });
        comboBox.SelectedIndex = 0;
        CountingCommand command = new();
        Cerneala.UI.Controls.Primitives.RepeatButton outer = new()
        {
            Content = comboBox,
            Command = command,
            Delay = 500,
            Interval = 100
        };
        LayoutCanvas canvas = new();
        LayoutCanvas.SetLeft(outer, 60);
        canvas.VisualChildren.Add(outer);
        root.VisualChildren.Add(canvas);
        root.ProcessFrame();
        int outerClicks = 0;
        // Click bubbles, so the Click of PART_DropDownToggle also reaches this handler; count only the outer one.
        outer.Click += (_, args) =>
        {
            if (ReferenceEquals(args.OriginalSource, outer))
            {
                outerClicks++;
            }
        };
        ToggleButton toggle = Assert.IsType<ToggleButton>(
            comboBox.ComponentTemplateInstance!.Parts["PART_DropDownToggle"]);
        LayoutRect fieldBounds = comboBox.ArrangedBounds;
        float fieldX = where == "toggle"
            ? toggle.ArrangedBounds.X + toggle.ArrangedBounds.Width / 2
            : fieldBounds.X + fieldBounds.Width / 2;
        float fieldY = fieldBounds.Y + fieldBounds.Height / 2;
        Assert.Equal(where == "toggle", ReferenceEquals(new HitTestService().HitTest(root, fieldX, fieldY)?.Element, toggle));
        TimeSpan frame = TimeSpan.FromMilliseconds(100);
        ElementInputBridge bridge = new();

        bridge.Dispatch(root, PointerFrame(fieldX, fieldY, currentDown: true), frame);
        for (int index = 0; index < 10; index++)
        {
            bridge.Dispatch(root, PointerFrame(fieldX, fieldY, previousDown: true, currentDown: true), frame);
        }

        Assert.Equal(0, outerClicks);
        Assert.Equal(0, command.Executions);

        bridge.Dispatch(root, PointerFrame(fieldX, fieldY, previousDown: true), frame);
        root.ProcessFrame();

        Assert.True(comboBox.IsDropDownOpen);
        Assert.Equal(0, outerClicks);
        Assert.Equal(0, command.Executions);

        comboBox.IsDropDownOpen = false;
        root.ProcessFrame();
        LayoutRect outerBounds = outer.ArrangedBounds;
        float outerX = outerBounds.X + 2;
        Assert.True(outerX < fieldBounds.X);
        Assert.Same(outer, new HitTestService().HitTest(root, outerX, fieldY)?.Element);

        bridge.Dispatch(root, PointerFrame(outerX, fieldY, currentDown: true), frame);
        for (int index = 0; index < 10; index++)
        {
            bridge.Dispatch(root, PointerFrame(outerX, fieldY, previousDown: true, currentDown: true), frame);
        }

        bridge.Dispatch(root, PointerFrame(outerX, fieldY, previousDown: true), frame);
        root.ProcessFrame();

        Assert.False(comboBox.IsDropDownOpen);
        Assert.True(outerClicks > 1, $"The RepeatButton clicked {outerClicks} times.");
        Assert.Equal(outerClicks, command.Executions);
    }

    // As in WPF, where the template's ToggleButton takes the field's press, an outer Button is not pressed while
    // the field of a non-editable ComboBox inside it is held. A press on the Button outside the ComboBox presses it.
    [Fact]
    public void NonEditableFieldHeldInsideButtonDoesNotPressButton()
    {
        UIRoot root = new(300, 200);
        ComboBox comboBox = new() { IsEditable = false, Width = 180, Height = 32 };
        comboBox.SetItems(new[] { "one", "two", "three" });
        comboBox.SelectedIndex = 0;
        Button outer = new() { Content = comboBox };
        LayoutCanvas canvas = new();
        LayoutCanvas.SetLeft(outer, 60);
        canvas.VisualChildren.Add(outer);
        root.VisualChildren.Add(canvas);
        root.ProcessFrame();
        LayoutRect fieldBounds = comboBox.ArrangedBounds;
        float fieldX = fieldBounds.X + fieldBounds.Width / 2;
        float fieldY = fieldBounds.Y + fieldBounds.Height / 2;
        ElementInputBridge bridge = new();

        bridge.Dispatch(root, PointerFrame(fieldX, fieldY, currentDown: true));
        bridge.Dispatch(root, PointerFrame(fieldX, fieldY, previousDown: true, currentDown: true));

        Assert.False(outer.IsPressed);
        Assert.Null(bridge.PressedStateTracker.PressedElement);

        bridge.Dispatch(root, PointerFrame(fieldX, fieldY, previousDown: true));
        root.ProcessFrame();

        Assert.True(comboBox.IsDropDownOpen);
        Assert.False(outer.IsPressed);

        comboBox.IsDropDownOpen = false;
        root.ProcessFrame();
        float outerX = outer.ArrangedBounds.X + 2;
        Assert.Same(outer, new HitTestService().HitTest(root, outerX, fieldY)?.Element);

        bridge.Dispatch(root, PointerFrame(outerX, fieldY, currentDown: true));

        Assert.True(outer.IsPressed);

        bridge.Dispatch(root, PointerFrame(outerX, fieldY, previousDown: true));

        Assert.False(outer.IsPressed);
    }

    // As in WPF, where the template's ToggleButton takes the field's press and captures the mouse, a press on the
    // field of a non-editable ComboBox inside a Button followed by a release on the Button outside the ComboBox
    // clicks nothing: the Button was never pressed, and the release is not over the field.
    [Fact]
    public void NonEditableFieldPressReleasedOnOuterButtonDoesNotClickButton()
    {
        UIRoot root = new(300, 200);
        ComboBox comboBox = new() { IsEditable = false, Width = 180, Height = 32 };
        comboBox.SetItems(new[] { "one", "two", "three" });
        comboBox.SelectedIndex = 0;
        CountingCommand command = new();
        Button outer = new() { Content = comboBox, Command = command };
        LayoutCanvas canvas = new();
        LayoutCanvas.SetLeft(outer, 60);
        canvas.VisualChildren.Add(outer);
        root.VisualChildren.Add(canvas);
        root.ProcessFrame();
        int outerClicks = 0;
        outer.Click += (_, _) => outerClicks++;
        LayoutRect fieldBounds = comboBox.ArrangedBounds;
        float fieldX = fieldBounds.X + fieldBounds.Width / 2;
        float fieldY = fieldBounds.Y + fieldBounds.Height / 2;
        float outerX = outer.ArrangedBounds.X + 2;
        Assert.Same(outer, new HitTestService().HitTest(root, outerX, fieldY)?.Element);
        ElementInputBridge bridge = new();

        bridge.Dispatch(root, PointerFrame(fieldX, fieldY, currentDown: true));
        bridge.Dispatch(
            root,
            new InputFrame(
                PointerSnapshot.Empty.WithPosition(fieldX, fieldY).WithButton(InputMouseButton.Left, true),
                PointerSnapshot.Empty.WithPosition(outerX, fieldY).WithButton(InputMouseButton.Left, true),
                KeyboardSnapshot.Empty,
                KeyboardSnapshot.Empty,
                []));
        bridge.Dispatch(root, PointerFrame(outerX, fieldY, previousDown: true));
        root.ProcessFrame();

        Assert.False(comboBox.IsDropDownOpen);
        Assert.Equal(0, outerClicks);
        Assert.Equal(0, command.Executions);
    }

    // As in WPF, MouseLeftButtonUp carries the Handled value of the MouseUp it reports: after the ComboBox marks
    // the field's MouseUp handled, an ordinary MouseLeftButtonUp handler on the outer Button does not run, and a
    // handler added with handledEventsToo still runs once.
    [Fact]
    public void NonEditableFieldClickInsideButtonSkipsOrdinaryOuterMouseLeftButtonUpHandlers()
    {
        UIRoot root = new(300, 200);
        ComboBox comboBox = new() { IsEditable = false, Width = 180, Height = 32 };
        comboBox.SetItems(new[] { "one", "two", "three" });
        comboBox.SelectedIndex = 0;
        Button outer = new() { Content = comboBox };
        LayoutCanvas canvas = new();
        LayoutCanvas.SetLeft(outer, 60);
        canvas.VisualChildren.Add(outer);
        root.VisualChildren.Add(canvas);
        root.ProcessFrame();
        int outerClicks = 0;
        int ordinaryLeftUps = 0;
        int handledTooLeftUps = 0;
        outer.Click += (_, _) => outerClicks++;
        outer.MouseLeftButtonUp += (_, _) => ordinaryLeftUps++;
        outer.AddHandler(UIElement.MouseLeftButtonUpEvent, (_, _) => handledTooLeftUps++, handledEventsToo: true);
        LayoutRect fieldBounds = comboBox.ArrangedBounds;
        float fieldX = fieldBounds.X + fieldBounds.Width / 2;
        float fieldY = fieldBounds.Y + fieldBounds.Height / 2;
        ElementInputBridge bridge = new();

        bridge.Dispatch(root, PointerFrame(fieldX, fieldY, currentDown: true));
        bridge.Dispatch(root, PointerFrame(fieldX, fieldY, previousDown: true));
        root.ProcessFrame();

        Assert.True(comboBox.IsDropDownOpen);
        Assert.Equal(0, outerClicks);
        Assert.Equal(0, ordinaryLeftUps);
        Assert.Equal(1, handledTooLeftUps);
    }

    private sealed class CountingCommand : ICommand
    {
        public int Executions { get; private set; }

        public bool CanExecute(object? parameter) => true;

        public void Execute(object? parameter) => Executions++;
    }

    private static bool IsDescendantOf(UIElement element, UIElement ancestor)
    {
        for (UIElement? current = element.VisualParent; current is not null; current = current.VisualParent)
        {
            if (ReferenceEquals(current, ancestor))
            {
                return true;
            }
        }

        return false;
    }

    private static T? FindDescendant<T>(UIElement element) where T : UIElement
    {
        if (element is T match)
        {
            return match;
        }

        for (int i = 0; i < element.VisualChildren.Count; i++)
        {
            if (FindDescendant<T>(element.VisualChildren[i]) is T found)
            {
                return found;
            }
        }

        return null;
    }

    private static (UIRoot Root, ComboBox ComboBox) CreateNonEditableField()
    {
        UIRoot root = new(300, 200);
        ComboBox comboBox = new()
        {
            IsEditable = false,
            Width = 180,
            Height = 32
        };
        comboBox.SetItems(new[] { "one", "two", "three" });
        comboBox.SelectedIndex = 0;
        LayoutCanvas canvas = new();
        LayoutCanvas.SetLeft(comboBox, 60);
        canvas.VisualChildren.Add(comboBox);
        root.VisualChildren.Add(canvas);
        root.ProcessFrame();
        return (root, comboBox);
    }

    [Fact]
    public void EditableFieldRendersTextVerticallyCenteredWhileKeepingFullHeightHitArea()
    {
        UIRoot root = new(300, 120);
        ComboBox comboBox = new()
        {
            Height = 40,
            Width = 180
        };
        comboBox.SetItems(new[] { "DEFAULT" });
        comboBox.SelectedIndex = 0;
        root.VisualChildren.Add(comboBox);
        root.ProcessFrame();
        TextBox editor = Assert.IsType<TextBox>(
            comboBox.ComponentTemplateInstance!.Parts["PART_EditableTextBox"]);

        DrawCommand text = Assert.Single(
            root.RetainedRenderer.Commit(root),
            command => command.Kind == DrawCommandKind.DrawText && command.Text == "DEFAULT");

        Assert.Equal(comboBox.ArrangedBounds.Height, editor.ArrangedBounds.Height);
        float fieldCenterY = editor.ArrangedBounds.Y + (editor.ArrangedBounds.Height / 2);
        Assert.True(text.Position.Y >= fieldCenterY);
    }

    [Fact]
    public void BackspaceDoesNotRestartAutocompleteWhileDeleting()
    {
        UIRoot root = new(300, 120);
        ComboBox comboBox = new()
        {
            IsEditable = true,
            Width = 180
        };
        comboBox.SetItems(new[] { "Apricot", "Apple" });
        root.VisualChildren.Add(comboBox);
        root.ProcessFrame();
        TextBox editor = Assert.IsType<TextBox>(
            comboBox.ComponentTemplateInstance!.Parts["PART_EditableTextBox"]);
        ElementInputBridge bridge = new();
        bridge.FocusManager.Focus(editor, root.InputCache.EnsureCurrent(root));
        bridge.Dispatch(root, TextInputFrame("a"));

        for (int index = 0; index < "Apricot".Length; index++)
        {
            DispatchKey(root, bridge, InputKey.Back);
        }

        Assert.Equal(string.Empty, editor.Text);
        Assert.Equal(string.Empty, comboBox.Text);
        Assert.Equal(-1, comboBox.SelectedIndex);
    }

    [Fact]
    public void EditableTextSearchCanPreserveEnteredPrefixCasing()
    {
        ComboBox comboBox = new()
        {
            IsEditable = true,
            IsTextFilterEnabled = false,
            ShouldPreserveUserEnteredPrefix = true
        };
        comboBox.SetItems(new[] { "Alpha" });
        comboBox.ApplyTemplate();
        TextBox editor = Assert.IsType<TextBox>(
            comboBox.ComponentTemplateInstance!.Parts["PART_EditableTextBox"]);

        editor.ReceiveTextInput("aL");

        Assert.Equal(0, comboBox.SelectedIndex);
        Assert.Equal("aLpha", comboBox.Text);
        Assert.Equal("aLpha", editor.Text);
        Assert.Equal(2, editor.Selection.Start);
        Assert.Equal(5, editor.Selection.End);
    }

    [Fact]
    public void OptInTextFilterRanksPrefixesBeforeContainsAndPreservesSourceIndices()
    {
        UIRoot root = new(300, 200);
        ComboBox comboBox = new()
        {
            IsEditable = true,
            IsTextFilterEnabled = true,
            Width = 180
        };
        comboBox.SetItems(new[] { "Pine", "Apple", "Grape", "Apricot", "Banana" });
        root.VisualChildren.Add(comboBox);
        root.ProcessFrame();
        TextBox editor = Assert.IsType<TextBox>(
            comboBox.ComponentTemplateInstance!.Parts["PART_EditableTextBox"]);

        editor.ReceiveTextInput("ap");
        root.ProcessFrame();

        Assert.True(comboBox.IsDropDownOpen);
        Assert.Equal(-1, comboBox.SelectedIndex);
        Assert.Equal(string.Empty, comboBox.Text);
        Assert.Equal("Apple", editor.Text);
        Assert.Equal([1, 3, 2], RealizedSourceIndices(comboBox));
    }

    [Fact]
    public void ClickingDropDownToggleWhileFilteredShowsAllItems()
    {
        UIRoot root = new(320, 220);
        ComboBox comboBox = new() { Width = 200 };
        comboBox.SetItems(new[] { "Pine", "Apple", "Apricot", "Banana" });
        root.VisualChildren.Add(comboBox);
        root.ProcessFrame();
        TextBox editor = Assert.IsType<TextBox>(
            comboBox.ComponentTemplateInstance!.Parts["PART_EditableTextBox"]);
        ToggleButton toggle = Assert.IsType<ToggleButton>(
            comboBox.ComponentTemplateInstance.Parts["PART_DropDownToggle"]);
        ElementInputBridge bridge = new();

        editor.ReceiveTextInput("ap");
        root.ProcessFrame();
        Assert.Equal([1, 2], RealizedSourceIndices(comboBox));

        float x = toggle.ArrangedBounds.X + toggle.ArrangedBounds.Width / 2;
        float y = toggle.ArrangedBounds.Y + toggle.ArrangedBounds.Height / 2;
        Assert.Same(toggle, new HitTestService().HitTest(root, x, y)?.Element);
        bridge.Dispatch(root, PointerFrame(x, y, currentDown: true));
        bridge.Dispatch(root, PointerFrame(x, y, previousDown: true));
        root.ProcessFrame();

        Assert.True(comboBox.IsDropDownOpen);
        Assert.True(toggle.IsChecked);
        Assert.Equal([0, 1, 2, 3], RealizedSourceIndices(comboBox));
    }

    [Fact]
    public void InsertingItemWhileDropDownIsOpenKeepsPreviewEditorText()
    {
        UIRoot root = new(300, 200);
        ObservableCollection<string> items = new(["a", "b", "c", "d"]);
        ComboBox comboBox = new() { IsEditable = true, Width = 180, ItemsSource = items };
        root.VisualChildren.Add(comboBox);
        root.ProcessFrame();
        comboBox.SelectedIndex = 1;
        TextBox editor = Assert.IsType<TextBox>(
            comboBox.ComponentTemplateInstance!.Parts["PART_EditableTextBox"]);
        ElementInputBridge bridge = new();
        bridge.FocusManager.Focus(comboBox, root.InputCache.EnsureCurrent(root));
        DispatchKey(root, bridge, InputKey.Down, InputKey.LeftAlt);
        DispatchKey(root, bridge, InputKey.End);
        Assert.Equal("d", editor.Text);

        items.Insert(0, "z");
        root.ProcessFrame();

        Assert.True(comboBox.IsDropDownOpen);
        Assert.Equal(2, comboBox.SelectedIndex);
        Assert.Equal("b", comboBox.Text);
        Assert.Equal("d", editor.Text);
    }

    [Fact]
    public void FuzzyFilterPreviewsTypoMatchAndEnterCommitsIt()
    {
        UIRoot root = new(300, 200);
        ComboBox comboBox = new()
        {
            IsEditable = true,
            IsTextFilterEnabled = true,
            Width = 180
        };
        comboBox.SetItems(new[] { "Apple", "Banana" });
        root.VisualChildren.Add(comboBox);
        root.ProcessFrame();
        TextBox editor = Assert.IsType<TextBox>(
            comboBox.ComponentTemplateInstance!.Parts["PART_EditableTextBox"]);
        ElementInputBridge bridge = new();
        bridge.FocusManager.Focus(comboBox, root.InputCache.EnsureCurrent(root));

        editor.ReceiveTextInput("aplpe");
        root.ProcessFrame();

        Assert.Equal([0], RealizedSourceIndices(comboBox));
        Assert.Equal(-1, comboBox.SelectedIndex);
        Assert.Equal(string.Empty, comboBox.Text);
        Assert.Equal("aplpe", editor.Text);
        Assert.True(ItemContainerGenerator.GetIsSelected(
            comboBox.ItemsPresenter.LayoutPanelRoot!.VisualChildren[0]));

        DispatchKey(root, bridge, InputKey.Enter);

        Assert.False(comboBox.IsDropDownOpen);
        Assert.Equal(0, comboBox.SelectedIndex);
        Assert.Equal("Apple", comboBox.Text);
        Assert.Equal("Apple", editor.Text);
        Assert.True(editor.Selection.IsEmpty);
        Assert.Equal(editor.Text.Length, editor.Selection.Active);
    }

    [Fact]
    public void EnterCollapsesAutocompleteSuffixSelectionAfterCommit()
    {
        UIRoot root = new(300, 200);
        ComboBox comboBox = new() { Width = 180 };
        comboBox.SetItems(new[] { "Apple", "Apricot" });
        root.VisualChildren.Add(comboBox);
        root.ProcessFrame();
        TextBox editor = Assert.IsType<TextBox>(
            comboBox.ComponentTemplateInstance!.Parts["PART_EditableTextBox"]);
        ElementInputBridge bridge = new();
        bridge.FocusManager.Focus(editor, root.InputCache.EnsureCurrent(root));

        editor.ReceiveTextInput("ap");
        Assert.Equal("Apple", editor.Text);
        Assert.Equal(2, editor.Selection.Start);
        Assert.Equal(5, editor.Selection.End);

        DispatchKey(root, bridge, InputKey.Enter);

        Assert.False(comboBox.IsDropDownOpen);
        Assert.Equal("Apple", comboBox.Text);
        Assert.True(editor.Selection.IsEmpty);
        Assert.Equal(editor.Text.Length, editor.Selection.Active);
    }

    [Fact]
    public void FuzzyFilterRanksAdjacentTranspositionBeforeIncidentalDeletion()
    {
        UIRoot root = new(300, 200);
        ComboBox comboBox = new()
        {
            IsEditable = true,
            IsTextFilterEnabled = true,
            Width = 180
        };
        comboBox.SetItems(new[] { "Apple", "Apricot", "Banana", "Grape", "Kiwi", "Maple" });
        root.VisualChildren.Add(comboBox);
        root.ProcessFrame();
        TextBox editor = Assert.IsType<TextBox>(
            comboBox.ComponentTemplateInstance!.Parts["PART_EditableTextBox"]);

        editor.ReceiveTextInput("aplpe");
        root.ProcessFrame();

        Assert.Equal(0, RealizedSourceIndices(comboBox)[0]);
    }

    [Fact]
    public void TabCommitsEditablePreviewClosesDropDownAndLeavesCompositeControl()
    {
        UIRoot root = new(300, 180);
        LayoutStackPanel panel = new();
        ComboBox comboBox = new()
        {
            IsEditable = true,
            IsTextFilterEnabled = true,
            Width = 180
        };
        TextBox next = new() { Text = "next" };
        comboBox.SetItems(new[] { "Apricot", "Banana" });
        comboBox.SelectedIndex = 0;
        panel.VisualChildren.Add(comboBox);
        panel.VisualChildren.Add(next);
        root.VisualChildren.Add(panel);
        root.ProcessFrame();
        TextBox editor = Assert.IsType<TextBox>(
            comboBox.ComponentTemplateInstance!.Parts["PART_EditableTextBox"]);
        ElementInputBridge bridge = new();
        bridge.FocusManager.Focus(editor, root.InputCache.EnsureCurrent(root));
        editor.Select(0, editor.Text.Length);
        bridge.Dispatch(root, TextInputFrame("ban"));

        DispatchKey(root, bridge, InputKey.Tab);

        Assert.False(comboBox.IsDropDownOpen);
        Assert.Equal(1, comboBox.SelectedIndex);
        Assert.Equal("Banana", comboBox.Text);
        Assert.Same(next, bridge.FocusManager.FocusedElement);
    }

    [Fact]
    public void EscapeCancelsFilteredPreviewAndRestoresCommittedTextAndItems()
    {
        UIRoot root = new(300, 200);
        ComboBox comboBox = new()
        {
            IsEditable = true,
            IsTextFilterEnabled = true,
            Width = 180
        };
        comboBox.SetItems(new[] { "Apple", "Banana" });
        comboBox.SelectedIndex = 1;
        root.VisualChildren.Add(comboBox);
        root.ProcessFrame();
        TextBox editor = Assert.IsType<TextBox>(
            comboBox.ComponentTemplateInstance!.Parts["PART_EditableTextBox"]);
        ElementInputBridge bridge = new();
        bridge.FocusManager.Focus(comboBox, root.InputCache.EnsureCurrent(root));

        editor.Select(0, editor.Text.Length);
        editor.ReceiveTextInput("aplpe");
        root.ProcessFrame();
        Assert.Equal([0], RealizedSourceIndices(comboBox));
        Assert.Equal(1, comboBox.SelectedIndex);
        Assert.Equal("Banana", comboBox.Text);

        DispatchKey(root, bridge, InputKey.Escape);
        root.ProcessFrame();

        Assert.False(comboBox.IsDropDownOpen);
        Assert.Equal(1, comboBox.SelectedIndex);
        Assert.Equal("Banana", comboBox.Text);
        Assert.Equal("Banana", editor.Text);
        comboBox.IsDropDownOpen = true;
        root.ProcessFrame();
        Assert.Equal([0, 1], RealizedSourceIndices(comboBox));
    }

    [Fact]
    public void EnterCommitsFreeEditableTextWhenFilterHasNoMatch()
    {
        UIRoot root = new(300, 200);
        ComboBox comboBox = new()
        {
            IsEditable = true,
            IsTextFilterEnabled = true
        };
        comboBox.SetItems(new[] { "Apple", "Banana" });
        root.VisualChildren.Add(comboBox);
        root.ProcessFrame();
        TextBox editor = Assert.IsType<TextBox>(
            comboBox.ComponentTemplateInstance!.Parts["PART_EditableTextBox"]);
        ElementInputBridge bridge = new();
        bridge.FocusManager.Focus(comboBox, root.InputCache.EnsureCurrent(root));

        editor.ReceiveTextInput("zzzzzz");
        root.ProcessFrame();
        Assert.Empty(RealizedSourceIndices(comboBox));
        Assert.Equal(string.Empty, comboBox.Text);

        DispatchKey(root, bridge, InputKey.Enter);

        Assert.Equal(-1, comboBox.SelectedIndex);
        Assert.Equal("zzzzzz", comboBox.Text);
        Assert.Equal("zzzzzz", editor.Text);
    }

    [Fact]
    public void ActiveFilterRebuildsWhenItemsChange()
    {
        UIRoot root = new(300, 200);
        ComboBox comboBox = new()
        {
            IsEditable = true,
            IsTextFilterEnabled = true
        };
        comboBox.SetItems(new[] { "Apple", "Banana" });
        root.VisualChildren.Add(comboBox);
        root.ProcessFrame();
        TextBox editor = Assert.IsType<TextBox>(
            comboBox.ComponentTemplateInstance!.Parts["PART_EditableTextBox"]);

        editor.ReceiveTextInput("aplpe");
        root.ProcessFrame();
        Assert.Equal([0], RealizedSourceIndices(comboBox));

        comboBox.SetItems(new[] { "Banana", "Applet" });
        root.ProcessFrame();

        Assert.Equal([1], RealizedSourceIndices(comboBox));
        Assert.Equal(-1, comboBox.SelectedIndex);
        Assert.Equal(string.Empty, comboBox.Text);
    }

    // The selection follows its item from "b2" at 3 to 2 and re-prepares the container of "b2", which throws.
    [Theory]
    [InlineData("Items.RemoveAt")]
    [InlineData("ObservableCollection.RemoveAt")]
    [InlineData("ObservableCollection.Move")]
    [InlineData("ObservableList.RemoveAt")]
    [InlineData("ObservableList.Move")]
    public void ActiveFilterFollowsItemChangeWhoseSelectionPreparationThrows(string change)
    {
        UIRoot root = new(300, 200);
        FailingContentComboBox comboBox = new() { IsEditable = true, IsTextFilterEnabled = true, Width = 180 };
        string[] initialItems = ["x", "b1", "c", "b2"];
        ObservableCollection<string> collection = [.. initialItems];
        ObservableList<string> list = new(initialItems);
        if (change.StartsWith("Items.", StringComparison.Ordinal))
        {
            comboBox.SetItems(initialItems);
        }
        else
        {
            comboBox.ItemsSource = change.StartsWith("ObservableList.", StringComparison.Ordinal) ? list : collection;
        }

        root.VisualChildren.Add(comboBox);
        root.ProcessFrame();
        TextBox editor = Assert.IsType<TextBox>(comboBox.ComponentTemplateInstance!.Parts["PART_EditableTextBox"]);
        editor.ReceiveTextInput("b");
        root.ProcessFrame();
        comboBox.SelectedIndex = 3;
        root.ProcessFrame();
        Assert.Equal([1, 3], RealizedSourceIndices(comboBox));

        comboBox.FailFor = "b2";
        Action applyChange = change switch
        {
            "Items.RemoveAt" => () => comboBox.Items.RemoveAt(0),
            "ObservableCollection.RemoveAt" => () => collection.RemoveAt(0),
            "ObservableCollection.Move" => () => collection.Move(0, 3),
            "ObservableList.RemoveAt" => () => list.RemoveAt(0),
            _ => () => list.Move(0, 3)
        };
        InvalidOperationException failure = Assert.Throws<InvalidOperationException>(applyChange);
        comboBox.FailFor = null;
        int[] viewSourceIndices = [.. Enumerable.Range(0, comboBox.ViewItemCount).Select(comboBox.GetSourceIndexForViewIndex)];
        Exception? frame = Record.Exception(() => root.ProcessFrame());

        Assert.Equal("content preparation failed", failure.Message);
        Assert.Equal(2, comboBox.SelectedIndex);
        Assert.Equal([0, 2], viewSourceIndices);
        Assert.Null(frame);
        Assert.Equal([0, 2], RealizedSourceIndices(comboBox));
        Assert.Equal(
            ["b1", "b2"],
            RealizedSourceIndices(comboBox).Select(index => comboBox.GetItemAt(index)));
    }

    [Fact]
    public void EditableTextSearchHonorsCaseSensitivityAndEnabledSwitch()
    {
        ComboBox caseSensitive = new()
        {
            IsEditable = true,
            IsTextFilterEnabled = false,
            IsTextSearchCaseSensitive = true
        };
        caseSensitive.SetItems(new[] { "Alpha" });
        caseSensitive.ApplyTemplate();
        TextBox caseSensitiveEditor = Assert.IsType<TextBox>(
            caseSensitive.ComponentTemplateInstance!.Parts["PART_EditableTextBox"]);

        caseSensitiveEditor.ReceiveTextInput("a");

        Assert.Equal(-1, caseSensitive.SelectedIndex);
        Assert.Equal("a", caseSensitive.Text);

        ComboBox disabled = new()
        {
            IsEditable = true,
            IsTextFilterEnabled = false,
            IsTextSearchEnabled = false
        };
        disabled.SetItems(new[] { "Alpha" });
        disabled.ApplyTemplate();
        TextBox disabledEditor = Assert.IsType<TextBox>(
            disabled.ComponentTemplateInstance!.Parts["PART_EditableTextBox"]);

        disabledEditor.ReceiveTextInput("Al");

        Assert.Equal(-1, disabled.SelectedIndex);
        Assert.Equal("Al", disabled.Text);
    }

    [Fact]
    public void ProgrammaticTextUsesExactMatchWithoutPrefixCompletion()
    {
        ComboBox comboBox = new() { IsEditable = true };
        comboBox.SetItems(new[] { "Alpha", "Beta" });

        comboBox.Text = "Al";

        Assert.Equal(-1, comboBox.SelectedIndex);
        Assert.Equal("Al", comboBox.Text);

        comboBox.Text = "alpha";

        Assert.Equal(0, comboBox.SelectedIndex);
        Assert.Equal("Alpha", comboBox.Text);
    }

    [Fact]
    public void TextSearchPathOverridesDisplayMemberPathForMatching()
    {
        ComboBox comboBox = new()
        {
            IsEditable = true,
            IsTextFilterEnabled = false,
            DisplayMemberPath = nameof(SearchItem.Display)
        };
        TextSearch.SetTextPath(comboBox, nameof(SearchItem.Search));
        comboBox.SetItems(new[]
        {
            new SearchItem("Bucharest", "Romania"),
            new SearchItem("Berlin", "Germany")
        });
        comboBox.ApplyTemplate();
        TextBox editor = Assert.IsType<TextBox>(
            comboBox.ComponentTemplateInstance!.Parts["PART_EditableTextBox"]);

        editor.ReceiveTextInput("Ber");

        Assert.Equal(1, comboBox.SelectedIndex);
        Assert.Equal("Berlin", comboBox.Text);
    }

    [Fact]
    public void AttachedTextOverridesItemFallbackText()
    {
        ComboBoxItem item = new() { Content = "Visible label" };
        TextSearch.SetText(item, "Search label");
        ComboBox comboBox = new()
        {
            IsEditable = true,
            IsTextFilterEnabled = false
        };
        comboBox.SetItems(new[] { item });
        comboBox.ApplyTemplate();
        TextBox editor = Assert.IsType<TextBox>(
            comboBox.ComponentTemplateInstance!.Parts["PART_EditableTextBox"]);

        editor.ReceiveTextInput("Sea");

        Assert.Equal(0, comboBox.SelectedIndex);
        Assert.Equal("Search label", comboBox.Text);
    }

    [Fact]
    public void NonEditableTextInputSelectsFirstPrefixMatchWithoutOpeningDropDown()
    {
        ComboBox comboBox = new() { IsEditable = false };
        comboBox.SetItems(new[] { "Alpha", "Beta", "Berlin" });
        TextCompositionEventArgs input = new(InputEvents.TextInputEvent, comboBox, "Ber");

        comboBox.RaiseEvent(input);

        Assert.True(input.Handled);
        Assert.Equal(2, comboBox.SelectedIndex);
        Assert.Equal("Berlin", comboBox.Text);
        Assert.False(comboBox.IsDropDownOpen);
    }

    [Fact]
    public void ReadOnlyEditableComboBoxRejectsUserEditingButKeepsSelectionApi()
    {
        ComboBox comboBox = new()
        {
            IsEditable = true,
            IsReadOnly = true
        };
        comboBox.SetItems(new[] { "Alpha", "Beta" });
        comboBox.SelectedIndex = 0;
        comboBox.ApplyTemplate();
        TextBox editor = Assert.IsType<TextBox>(
            comboBox.ComponentTemplateInstance!.Parts["PART_EditableTextBox"]);

        editor.ReceiveTextInput("x");
        comboBox.SelectedIndex = 1;

        Assert.True(editor.IsReadOnly);
        Assert.Equal("Beta", comboBox.Text);
        Assert.Equal(1, comboBox.SelectedIndex);
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
        ContentPresenter selection = Assert.IsType<ContentPresenter>(instance.Parts["PART_SelectionPresenter"]);
        TextBox editor = Assert.IsType<TextBox>(instance.Parts["PART_EditableTextBox"]);
        Assert.Equal(HorizontalAlignment.Left, selection.HorizontalAlignment);
        Assert.Equal(VerticalAlignment.Center, selection.VerticalAlignment);
        Assert.Equal(HorizontalAlignment.Stretch, editor.HorizontalAlignment);
        Assert.Equal(VerticalAlignment.Stretch, editor.VerticalAlignment);
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
        Assert.Same(foreground, comboGlyph.Fill);
    }

    [Fact]
    public void ClosedDefaultTemplateUsesOwnerBackgroundAndForeground()
    {
        SolidColorBrush background = new(new Cerneala.Drawing.Color(20, 24, 30));
        SolidColorBrush foreground = new(Cerneala.Drawing.Color.White);
        ComboBox comboBox = new()
        {
            Background = background,
            Foreground = foreground
        };
        comboBox.SetItems(new[] { "Normal" });
        comboBox.SelectedIndex = 0;
        comboBox.ApplyTemplate();
        ComponentTemplateInstance instance = comboBox.ComponentTemplateInstance!;
        ContentPresenter selection = Assert.IsType<ContentPresenter>(instance.Parts["PART_SelectionPresenter"]);
        TextBox editor = Assert.IsType<TextBox>(instance.Parts["PART_EditableTextBox"]);
        ToggleButton toggle = Assert.IsType<ToggleButton>(instance.Parts["PART_DropDownToggle"]);
        DirectionPath glyph = Assert.IsType<DirectionPath>(toggle.Content);
        Border fieldBorder = Assert.Single(Descendants(instance.Root!).OfType<Border>(),
            border => ReferenceEquals(border.Background, background));

        Assert.Same(background, fieldBorder.Background);
        Assert.Same(foreground, selection.Foreground);
        Assert.Same(foreground, editor.CaretBrush);
        Assert.Same(foreground, glyph.Fill);
    }

    [Fact]
    public void LargeDropDownRealizesOnlyVisibleItems()
    {
        UIRoot root = new(320, 420);
        ComboBox comboBox = new()
        {
            Width = 180,
            MaxDropDownHeight = 120,
            IsDropDownOpen = true
        };
        comboBox.SetItems(Enumerable.Range(1, 200).Select(index => $"item {index}"));
        root.VisualChildren.Add(comboBox);

        root.ProcessFrame();
        root.ProcessFrame();

        Assert.InRange(comboBox.ItemContainerGenerator.RealizedContainers.Count, 1, 20);
        Assert.IsType<Cerneala.UI.Layout.Panels.VirtualizingStackPanel>(
            comboBox.ItemsPresenter.LayoutPanelRoot);

        Overlay overlay = Assert.IsType<Overlay>(
            comboBox.ComponentTemplateInstance!.Parts["PART_DropDownOverlay"]);
        ScrollViewer scrollViewer = Assert.IsType<ScrollViewer>(Assert.IsType<Border>(overlay.Content).Child);
        scrollViewer.ScrollInfo.SetVerticalOffset(1400);
        root.ProcessFrame();

        Assert.True(RealizedSourceIndices(comboBox)[0] > 0);
        Assert.InRange(comboBox.ItemContainerGenerator.RealizedContainers.Count, 1, 20);
    }

    [Fact]
    public void OwnerStackPanelRealizesEveryDropDownItemAfterScrolling()
    {
        (ComboBox comboBox, ScrollViewer viewer, UIRoot root) = CreateLargeOpenDropDown(
            template: null,
            ownerPanel: new LayoutStackPanel());
        Assert.Equal(200, comboBox.ItemContainerGenerator.RealizedContainers.Count);
        Assert.Equal(6800, viewer.ScrollInfo.ExtentHeight);

        viewer.ScrollInfo.SetVerticalOffset(1400);
        root.ProcessFrame();
        root.ProcessFrame();
        Assert.Equal(200, comboBox.ItemContainerGenerator.RealizedContainers.Count);
        Assert.Equal(6800, viewer.ScrollInfo.ExtentHeight);
        Assert.Equal(1400, viewer.ScrollInfo.VerticalOffset);
    }

    [Fact]
    public void CustomTemplatePresenterStackPanelRealizesEveryDropDownItem()
    {
        (ComboBox comboBox, ScrollViewer viewer, UIRoot root) = CreateLargeOpenDropDown(
            CreatePresenterPanelTemplate(new LayoutStackPanel()),
            ownerPanel: null);
        Assert.IsType<LayoutStackPanel>(comboBox.ItemsPresenter.LayoutPanelRoot);
        Assert.Equal(200, comboBox.ItemContainerGenerator.RealizedContainers.Count);
        Assert.Equal(6800, viewer.ScrollInfo.ExtentHeight);

        viewer.ScrollInfo.SetVerticalOffset(1400);
        root.ProcessFrame();
        root.ProcessFrame();
        Assert.Equal(200, comboBox.ItemContainerGenerator.RealizedContainers.Count);
        Assert.Equal(6800, viewer.ScrollInfo.ExtentHeight);
        Assert.Equal(1400, viewer.ScrollInfo.VerticalOffset);
    }

    [Fact]
    public void CustomTemplatePresenterVirtualizingStackPanelVirtualizesDropDown()
    {
        (ComboBox comboBox, ScrollViewer viewer, UIRoot root) = CreateLargeOpenDropDown(
            CreatePresenterPanelTemplate(new Cerneala.UI.Layout.Panels.VirtualizingStackPanel()),
            ownerPanel: null);
        Assert.IsType<Cerneala.UI.Layout.Panels.VirtualizingStackPanel>(comboBox.ItemsPresenter.LayoutPanelRoot);
        Assert.InRange(comboBox.ItemContainerGenerator.RealizedContainers.Count, 1, 20);

        viewer.ScrollInfo.SetVerticalOffset(1400);
        root.ProcessFrame();
        root.ProcessFrame();
        Assert.True(RealizedSourceIndices(comboBox)[0] > 0);
        Assert.InRange(comboBox.ItemContainerGenerator.RealizedContainers.Count, 1, 20);
    }

    [Fact]
    public void ReplacingItemsPanelReleasesDropDownContextFromPresenterVirtualizingStackPanel()
    {
        Cerneala.UI.Layout.Panels.VirtualizingStackPanel presenterPanel = new();
        (ComboBox comboBox, ScrollViewer viewer, UIRoot root) = CreateLargeOpenDropDown(
            CreatePresenterPanelTemplate(presenterPanel),
            ownerPanel: null);
        Assert.InRange(comboBox.ItemContainerGenerator.RealizedContainers.Count, 1, 20);

        comboBox.ItemsPanel = new LayoutStackPanel();
        root.ProcessFrame();
        root.ProcessFrame();
        Assert.Same(presenterPanel, comboBox.ItemsPresenter.LayoutPanelRoot);
        Assert.Null(presenterPanel.VirtualizationContext);

        viewer.ScrollInfo.SetVerticalOffset(1400);
        root.ProcessFrame();
        root.ProcessFrame();
        Assert.Equal(200, comboBox.ItemContainerGenerator.RealizedContainers.Count);
        Assert.Equal(6800, viewer.ScrollInfo.ExtentHeight);
        Assert.Equal(1400, viewer.ScrollInfo.VerticalOffset);
    }

    [Fact]
    public void SwappingPresenterPanelToStackPanelWhileOpenRealizesEveryDropDownItem()
    {
        (ComboBox comboBox, ScrollViewer viewer, UIRoot root) = CreateLargeOpenDropDown(
            template: null,
            ownerPanel: null);
        Assert.InRange(comboBox.ItemContainerGenerator.RealizedContainers.Count, 1, 20);

        ItemsPresenter presenter = Assert.IsType<ItemsPresenter>(
            comboBox.ComponentTemplateInstance!.Parts["PART_ItemsPresenter"]);
        presenter.ItemsPanel = new LayoutStackPanel();
        root.ProcessFrame();
        root.ProcessFrame();
        Assert.IsType<LayoutStackPanel>(comboBox.ItemsPresenter.LayoutPanelRoot);
        Assert.Equal(200, comboBox.ItemContainerGenerator.RealizedContainers.Count);
        Assert.Equal(6800, viewer.ScrollInfo.ExtentHeight);

        viewer.ScrollInfo.SetVerticalOffset(1400);
        root.ProcessFrame();
        root.ProcessFrame();
        Assert.Equal(200, comboBox.ItemContainerGenerator.RealizedContainers.Count);
        Assert.Equal(6800, viewer.ScrollInfo.ExtentHeight);
        Assert.Equal(1400, viewer.ScrollInfo.VerticalOffset);
    }

    [Fact]
    public void SwappingPresenterPanelToVirtualizingStackPanelWhileOpenVirtualizesDropDown()
    {
        (ComboBox comboBox, ScrollViewer viewer, UIRoot root) = CreateLargeOpenDropDown(
            CreatePresenterPanelTemplate(new LayoutStackPanel()),
            ownerPanel: null);
        Assert.Equal(200, comboBox.ItemContainerGenerator.RealizedContainers.Count);

        comboBox.ItemsPresenter.ItemsPanel = new Cerneala.UI.Layout.Panels.VirtualizingStackPanel();
        root.ProcessFrame();
        root.ProcessFrame();
        Assert.IsType<Cerneala.UI.Layout.Panels.VirtualizingStackPanel>(comboBox.ItemsPresenter.LayoutPanelRoot);
        Assert.InRange(comboBox.ItemContainerGenerator.RealizedContainers.Count, 1, 20);

        viewer.ScrollInfo.SetVerticalOffset(1400);
        root.ProcessFrame();
        root.ProcessFrame();
        Assert.True(RealizedSourceIndices(comboBox)[0] > 0);
        Assert.InRange(comboBox.ItemContainerGenerator.RealizedContainers.Count, 1, 20);
    }

    [Theory]
    [InlineData(3, false)]
    [InlineData(200, false)]
    [InlineData(3, true)]
    [InlineData(200, true)]
    public void RemovingTemplateAfterDropDownOpenedShowsEveryItemInFallbackPresenter(int itemCount, bool closeBeforeRemoval)
    {
        (ComboBox comboBox, UIRoot root) = CreateOpenedDropDown(itemCount);
        ItemsPresenter templatePresenter = comboBox.ItemsPresenter;
        Cerneala.UI.Layout.Panels.VirtualizingStackPanel panel =
            Assert.IsType<Cerneala.UI.Layout.Panels.VirtualizingStackPanel>(comboBox.ItemsPanel);
        Assert.Same(templatePresenter, panel.VisualParent);
        Assert.NotNull(panel.VirtualizationContext);
        if (closeBeforeRemoval)
        {
            comboBox.IsDropDownOpen = false;
            root.ProcessFrame();
        }

        comboBox.ComponentTemplate = null;
        Assert.Null(panel.VirtualizationContext);
        Exception? error = Record.Exception(() =>
        {
            root.ProcessFrame();
            root.ProcessFrame();
        });

        Assert.Null(error);
        Assert.NotSame(templatePresenter, comboBox.ItemsPresenter);
        Assert.Same(comboBox.ItemsPresenter, panel.VisualParent);
        Assert.Null(panel.VirtualizationContext);
        Assert.Equal(itemCount, panel.VisualChildren.Count);
        Assert.Empty(templatePresenter.VisualChildren);
    }

    [Theory]
    [InlineData(3)]
    [InlineData(200)]
    public void ReplacingTemplateAfterDropDownOpenedMovesItemsToNewPresenter(int itemCount)
    {
        (ComboBox comboBox, UIRoot root) = CreateOpenedDropDown(itemCount);
        ItemsPresenter oldPresenter = comboBox.ItemsPresenter;
        Cerneala.UI.Layout.Panels.VirtualizingStackPanel panel =
            Assert.IsType<Cerneala.UI.Layout.Panels.VirtualizingStackPanel>(comboBox.ItemsPanel);

        comboBox.ComponentTemplate = CreateComboBoxTemplate("replacement");
        Exception? error = Record.Exception(() =>
        {
            root.ProcessFrame();
            root.ProcessFrame();
        });

        Assert.Null(error);
        ItemsPresenter newPresenter = Assert.IsType<ItemsPresenter>(
            comboBox.ComponentTemplateInstance!.Parts["PART_ItemsPresenter"]);
        Assert.Same(newPresenter, comboBox.ItemsPresenter);
        Assert.Same(newPresenter, panel.VisualParent);
        Assert.Equal(newPresenter.VirtualizationContext, panel.VirtualizationContext);
        Assert.Equal(comboBox.ItemContainerGenerator.RealizedContainers.Count, panel.VisualChildren.Count);
        Assert.NotEmpty(panel.VisualChildren);
        Assert.Empty(oldPresenter.VisualChildren);
    }

    private static (ComboBox ComboBox, UIRoot Root) CreateOpenedDropDown(int itemCount)
    {
        UIRoot root = new(320, 420);
        ComboBox comboBox = new() { Width = 180, MaxDropDownHeight = 120, IsDropDownOpen = true };
        comboBox.SetItems(Enumerable.Range(0, itemCount).Select(index => $"item {index}"));
        LayoutCanvas canvas = new();
        canvas.VisualChildren.Add(comboBox);
        root.VisualChildren.Add(canvas);
        root.ProcessFrame();
        root.ProcessFrame();
        return (comboBox, root);
    }

    private static (ComboBox ComboBox, ScrollViewer Viewer, UIRoot Root) CreateLargeOpenDropDown(
        ComponentTemplate<ComboBox>? template,
        LayoutStackPanel? ownerPanel)
    {
        UIRoot root = new(320, 420);
        ComboBox comboBox = new() { Width = 180, MaxDropDownHeight = 120, IsDropDownOpen = true };
        if (ownerPanel is not null)
        {
            comboBox.ItemsPanel = ownerPanel;
        }

        if (template is not null)
        {
            comboBox.ComponentTemplate = template;
        }

        comboBox.SetItems(Enumerable.Range(0, 200).Select(index => $"item {index}"));
        LayoutCanvas canvas = new();
        canvas.VisualChildren.Add(comboBox);
        root.VisualChildren.Add(canvas);
        root.ProcessFrame();
        root.ProcessFrame();
        Overlay overlay = Assert.IsType<Overlay>(comboBox.ComponentTemplateInstance!.Parts["PART_DropDownOverlay"]);
        ScrollViewer viewer = Assert.IsType<ScrollViewer>(Assert.IsType<Border>(overlay.Content).Child);
        return (comboBox, viewer, root);
    }

    private static ComponentTemplate<ComboBox> CreatePresenterPanelTemplate(Cerneala.UI.Layout.Panels.Panel presenterPanel)
    {
        return new ComponentTemplate<ComboBox>("presenter-panel", context =>
        {
            ContentPresenter selection = new();
            TextBox editor = new() { Visibility = Visibility.Collapsed };
            ToggleButton toggle = new();
            ItemsPresenter items = new() { ItemsPanel = presenterPanel };
            ScrollViewer scrollViewer = new()
            {
                Content = items,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto
            };
            Overlay overlay = new()
            {
                Content = new Border { Child = scrollViewer },
                PlacementTarget = context.Owner,
                Placement = OverlayPlacement.Auto,
                MatchTargetWidth = true
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
    public void KeyboardNavigationPreviewsSelectionAndEscapeCancelsIt()
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
        root.ProcessFrame();
        Assert.True(comboBox.IsDropDownOpen);
        Assert.Equal(-1, comboBox.SelectedIndex);
        Assert.True(ItemContainerGenerator.GetIsSelected(
            comboBox.ItemContainerGenerator.RealizedContainers[2]));

        DispatchKey(root, bridge, InputKey.Escape);
        Assert.False(comboBox.IsDropDownOpen);
        Assert.Equal(-1, comboBox.SelectedIndex);
    }

    [Fact]
    public void AltAndEnterCommandsCommitPreviewSelection()
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
        Assert.Equal(-1, comboBox.SelectedIndex);

        DispatchKey(root, bridge, InputKey.Enter);
        Assert.False(comboBox.IsDropDownOpen);
        Assert.Equal(0, comboBox.SelectedIndex);

        DispatchKey(root, bridge, InputKey.Down, InputKey.LeftAlt);
        DispatchKey(root, bridge, InputKey.Up, InputKey.LeftAlt);
        Assert.False(comboBox.IsDropDownOpen);
    }

    [Fact]
    public void HighlightedItemFollowsItsItemWhenItemsInsertBeforeIt()
    {
        UIRoot root = new(100, 100);
        ComboBox comboBox = new();
        comboBox.SetItems(new[] { "a", "b", "c" });
        ElementInputBridge bridge = OpenAndHighlightLastItem(root, comboBox);

        comboBox.Items.Insert(0, "z");
        root.ProcessFrame();
        string highlighted = HighlightedItems(comboBox);
        DispatchKey(root, bridge, InputKey.Enter);

        Assert.Equal(
            "highlighted=c selected=c text=c",
            $"highlighted={highlighted} selected={comboBox.SelectedItem} text={comboBox.Text}");
    }

    [Fact]
    public void HighlightedItemFollowsItsItemWhenItemsRemoveBeforeIt()
    {
        UIRoot root = new(100, 100);
        ComboBox comboBox = new();
        comboBox.SetItems(new[] { "a", "b", "c" });
        ElementInputBridge bridge = OpenAndHighlightLastItem(root, comboBox);

        comboBox.Items.RemoveAt(0);
        root.ProcessFrame();
        string highlighted = HighlightedItems(comboBox);
        DispatchKey(root, bridge, InputKey.Enter);

        Assert.Equal(
            "highlighted=c selected=c text=c",
            $"highlighted={highlighted} selected={comboBox.SelectedItem} text={comboBox.Text}");
    }

    [Fact]
    public void HighlightedItemFollowsItsItemWhenItemsSourceInsertsBeforeIt()
    {
        UIRoot root = new(100, 100);
        ObservableCollection<string> items = ["a", "b", "c"];
        ComboBox comboBox = new() { ItemsSource = items };
        ElementInputBridge bridge = OpenAndHighlightLastItem(root, comboBox);

        items.Insert(0, "z");
        root.ProcessFrame();
        string highlighted = HighlightedItems(comboBox);
        DispatchKey(root, bridge, InputKey.Enter);

        Assert.Equal(
            "highlighted=c selected=c text=c",
            $"highlighted={highlighted} selected={comboBox.SelectedItem} text={comboBox.Text}");
    }

    [Fact]
    public void HighlightedItemFollowsItsItemWhenItemsSourceRemovesBeforeIt()
    {
        UIRoot root = new(100, 100);
        ObservableCollection<string> items = ["a", "b", "c"];
        ComboBox comboBox = new() { ItemsSource = items };
        ElementInputBridge bridge = OpenAndHighlightLastItem(root, comboBox);

        items.RemoveAt(0);
        root.ProcessFrame();
        string highlighted = HighlightedItems(comboBox);
        DispatchKey(root, bridge, InputKey.Enter);

        Assert.Equal(
            "highlighted=c selected=c text=c",
            $"highlighted={highlighted} selected={comboBox.SelectedItem} text={comboBox.Text}");
    }

    [Fact]
    public void HighlightedItemFollowsItsItemWhenItemsSourceMovesIt()
    {
        UIRoot root = new(100, 100);
        ObservableCollection<string> items = ["a", "b", "c"];
        ComboBox comboBox = new() { ItemsSource = items };
        ElementInputBridge bridge = OpenAndHighlightLastItem(root, comboBox);

        items.Move(2, 0);
        root.ProcessFrame();
        string highlighted = HighlightedItems(comboBox);
        DispatchKey(root, bridge, InputKey.Enter);

        Assert.Equal(
            "highlighted=c selected=c text=c",
            $"highlighted={highlighted} selected={comboBox.SelectedItem} text={comboBox.Text}");
    }

    [Fact]
    public void HighlightedItemFollowsItsItemWhenItemsSourceResets()
    {
        UIRoot root = new(100, 100);
        ResettingList items = new(["a", "b", "c"]);
        ComboBox comboBox = new() { ItemsSource = items };
        ElementInputBridge bridge = OpenAndHighlightLastItem(root, comboBox);

        items.ReverseAndReset();
        root.ProcessFrame();
        string highlighted = HighlightedItems(comboBox);
        DispatchKey(root, bridge, InputKey.Enter);

        Assert.Equal(
            "highlighted=c selected=c text=c",
            $"highlighted={highlighted} selected={comboBox.SelectedItem} text={comboBox.Text}");
    }

    [Fact]
    public void RemovingHighlightedItemHighlightsFirstItemAndEnterCommitsItsText()
    {
        UIRoot root = new(100, 100);
        ObservableCollection<string> items = ["a", "b", "c"];
        ComboBox comboBox = new() { ItemsSource = items };
        ElementInputBridge bridge = OpenAndHighlightLastItem(root, comboBox);

        items.RemoveAt(2);
        root.ProcessFrame();
        string highlighted = HighlightedItems(comboBox);
        DispatchKey(root, bridge, InputKey.Enter);

        Assert.Equal(
            "highlighted=a selected=a text=a",
            $"highlighted={highlighted} selected={comboBox.SelectedItem} text={comboBox.Text}");
    }

    [Fact]
    public void FilteredHighlightedItemFollowsItsItemWhenItemsSourceInsertsBeforeIt()
    {
        UIRoot root = new(300, 200);
        ObservableCollection<string> items = ["Apple", "Banana", "Apricot"];
        ComboBox comboBox = new() { IsTextFilterEnabled = true, Width = 180, ItemsSource = items };
        ElementInputBridge bridge = OpenFilteredAndHighlightApricot(root, comboBox);

        items.Insert(0, "Cherry");
        root.ProcessFrame();
        string highlighted = HighlightedItems(comboBox);
        DispatchKey(root, bridge, InputKey.Enter);

        Assert.Equal(
            "highlighted=Apricot selected=Apricot text=Apricot",
            $"highlighted={highlighted} selected={comboBox.SelectedItem} text={comboBox.Text}");
    }

    [Fact]
    public void FilteredHighlightedItemReplacedByHiddenItemHighlightsFirstVisibleItem()
    {
        UIRoot root = new(300, 200);
        ObservableCollection<string> items = ["Apple", "Banana", "Apricot"];
        ComboBox comboBox = new() { IsTextFilterEnabled = true, Width = 180, ItemsSource = items };
        ElementInputBridge bridge = OpenFilteredAndHighlightApricot(root, comboBox);

        items[2] = "Blueberry";
        root.ProcessFrame();
        string highlighted = HighlightedItems(comboBox);
        DispatchKey(root, bridge, InputKey.Enter);

        Assert.Equal(
            "highlighted=Apple selected=Apple text=Apple",
            $"highlighted={highlighted} selected={comboBox.SelectedItem} text={comboBox.Text}");
    }

    [Fact]
    public void HighlightedItemFollowsItsItemWhenNewItemsSourceIsAssigned()
    {
        UIRoot root = new(100, 100);
        ComboBox comboBox = new() { ItemsSource = new[] { "a", "b", "c" } };
        ElementInputBridge bridge = OpenAndHighlightLastItem(root, comboBox);

        comboBox.ItemsSource = new[] { "x", "c" };
        root.ProcessFrame();
        string highlighted = HighlightedItems(comboBox);
        DispatchKey(root, bridge, InputKey.Enter);

        Assert.Equal(
            "highlighted=c selected=c text=c",
            $"highlighted={highlighted} selected={comboBox.SelectedItem} text={comboBox.Text}");
    }

    [Fact]
    public void NewItemsSourceWithoutHighlightedItemHighlightsFirstItem()
    {
        UIRoot root = new(100, 100);
        ComboBox comboBox = new() { ItemsSource = new[] { "a", "b", "c" } };
        ElementInputBridge bridge = OpenAndHighlightLastItem(root, comboBox);

        comboBox.ItemsSource = new[] { "x", "y" };
        root.ProcessFrame();
        string highlighted = HighlightedItems(comboBox);
        DispatchKey(root, bridge, InputKey.Enter);

        Assert.Equal(
            "highlighted=x selected=x text=x",
            $"highlighted={highlighted} selected={comboBox.SelectedItem} text={comboBox.Text}");
    }

    // Clearing a realized drop-down row is not presenting an item, so the row's ItemTemplate does not run with a
    // null item (#535).
    [Theory]
    [InlineData(nameof(ComboBox.ItemsSource))]
    [InlineData(nameof(ComboBox.ItemTemplate))]
    public void ReplacingItemsSourceOrItemTemplateDoesNotRunTheRowTemplateWithoutItsItem(string change)
    {
        List<string?> rowCalls = [];
        ContentPresenter? selection = null;
        ContentTemplate<string> RowTemplate(string prefix) => new("row", key: null, priority: 0, context =>
        {
            if (!ReferenceEquals(context.Presenter, selection))
            {
                rowCalls.Add(context.Data);
            }

            return new TextBlock { Text = prefix + context.Data };
        });
        UIRoot root = new(300, 200);
        ComboBox comboBox = new() { Width = 180, ItemTemplate = RowTemplate(""), ItemsSource = new[] { "a", "b", "c" } };
        root.VisualChildren.Add(comboBox);
        root.ProcessFrame();
        selection = Assert.IsType<ContentPresenter>(comboBox.ComponentTemplateInstance!.Parts["PART_SelectionPresenter"]);
        comboBox.IsDropDownOpen = true;
        root.ProcessFrame();
        Assert.Equal(3, comboBox.RealizedItemCount);

        rowCalls.Clear();
        if (change == nameof(ComboBox.ItemsSource))
        {
            comboBox.ItemsSource = new[] { "d" };
        }
        else
        {
            comboBox.ItemTemplate = RowTemplate("x");
        }

        Assert.Empty(rowCalls);
        root.ProcessFrame();

        Assert.DoesNotContain(null, rowCalls);
        Assert.Equal(change == nameof(ComboBox.ItemsSource) ? ["d"] : ["a", "b", "c"], rowCalls.Distinct());
        Assert.Equal(
            change == nameof(ComboBox.ItemsSource) ? ["d"] : ["xa", "xb", "xc"],
            comboBox.ItemContainerGenerator.RealizedContainers.OrderBy(pair => pair.Key).Select(pair =>
                Assert.IsType<TextBlock>(Assert.IsType<ContentPresenter>(
                    Assert.IsType<ComboBoxItem>(pair.Value).Content).PresentedChild).Text));
    }

    // Preparing a drop-down row presents its item once, with the row's final index, as ListBox rows do (#574).
    [Fact]
    public void PreparingDropDownRowsRunsTheRowTemplateOnceWithTheRowIndex()
    {
        List<string> rowCalls = [];
        ContentPresenter? selection = null;
        UIRoot root = new(300, 200);
        ComboBox comboBox = new()
        {
            Width = 180,
            ItemTemplate = new ContentTemplate<string>("row", key: null, priority: 0, context =>
            {
                if (!ReferenceEquals(context.Presenter, selection))
                {
                    rowCalls.Add($"{context.Data}@{context.Index}");
                }

                return new TextBlock { Text = context.Data };
            }),
            ItemsSource = new[] { "a", "b", "c" }
        };
        root.VisualChildren.Add(comboBox);
        root.ProcessFrame();
        selection = Assert.IsType<ContentPresenter>(comboBox.ComponentTemplateInstance!.Parts["PART_SelectionPresenter"]);
        rowCalls.Clear();

        comboBox.IsDropDownOpen = true;
        root.ProcessFrame();
        string[] openCalls = [.. rowCalls];
        rowCalls.Clear();
        comboBox.ItemsSource = new[] { "d" };
        root.ProcessFrame();

        Assert.Equal(
            "open=[a@0, b@1, c@2] replace=[d@0]",
            $"open=[{string.Join(", ", openCalls)}] replace=[{string.Join(", ", rowCalls)}]");
    }

    [Fact]
    public void FilteredHighlightedItemFollowsItsItemWhenNewItemsSourceIsAssigned()
    {
        UIRoot root = new(300, 200);
        ComboBox comboBox = new() { IsTextFilterEnabled = true, Width = 180, ItemsSource = new[] { "Apple", "Banana", "Apricot" } };
        ElementInputBridge bridge = OpenFilteredAndHighlightApricot(root, comboBox);

        comboBox.ItemsSource = new[] { "Apple", "Apricot" };
        root.ProcessFrame();
        string highlighted = HighlightedItems(comboBox);
        DispatchKey(root, bridge, InputKey.Enter);

        Assert.Equal(
            "highlighted=Apricot selected=Apricot text=Apricot",
            $"highlighted={highlighted} selected={comboBox.SelectedItem} text={comboBox.Text}");
    }

    [Fact]
    public void FilteredNewItemsSourceWithoutHighlightedItemHighlightsFirstVisibleItem()
    {
        UIRoot root = new(300, 200);
        ComboBox comboBox = new() { IsTextFilterEnabled = true, Width = 180, ItemsSource = new[] { "Apple", "Banana", "Apricot" } };
        ElementInputBridge bridge = OpenFilteredAndHighlightApricot(root, comboBox);

        comboBox.ItemsSource = new[] { "Cherry", "Apple" };
        root.ProcessFrame();
        string highlighted = HighlightedItems(comboBox);
        DispatchKey(root, bridge, InputKey.Enter);

        Assert.Equal(
            "highlighted=Apple selected=Apple text=Apple",
            $"highlighted={highlighted} selected={comboBox.SelectedItem} text={comboBox.Text}");
    }

    [Fact]
    public void RemovingComboBoxItemBeforeHighlightedItemKeepsEachItemOnItsOwnContainer()
    {
        UIRoot root = new(200, 200);
        ComboBoxItem a = new() { Content = "A" }, b = new() { Content = "B" }, c = new() { Content = "C" };
        ComboBox comboBox = new();
        comboBox.SetItems(new[] { a, b, c });
        OpenAndHighlightLastContainer(root, comboBox);

        comboBox.Items.RemoveAt(0);

        Assert.Equal("A", a.Content);
        Assert.NotSame(b, ItemContainerGenerator.GetItem(a));
        Assert.False(ItemContainerGenerator.GetIsSelected(a));

        root.ProcessFrame();

        Assert.Same(b, comboBox.ItemContainerGenerator.RealizedContainers[0]);
        Assert.Same(c, comboBox.ItemContainerGenerator.RealizedContainers[1]);
        Assert.Equal([c], HighlightedContainers(comboBox));
        Assert.Equal("A,B,C", $"{a.Content},{b.Content},{c.Content}");
    }

    [Fact]
    public void RemovingStringItemBeforeHighlightedItemDoesNotPrepareRemovedItemContainer()
    {
        UIRoot root = new(200, 200);
        ObservableCollection<string> items = ["a", "b", "c"];
        ComboBox comboBox = new() { ItemsSource = items };
        OpenAndHighlightLastContainer(root, comboBox);
        UIElement containerOfA = comboBox.ItemContainerGenerator.RealizedContainers[0];

        items.RemoveAt(0);

        Assert.DoesNotContain(containerOfA, comboBox.ItemContainerGenerator.RealizedContainers.Values);
        Assert.NotEqual("b", ItemContainerGenerator.GetItem(containerOfA));
        Assert.False(ItemContainerGenerator.GetIsSelected(containerOfA));
    }

    [Fact]
    public void MovingHighlightByKeyAfterRemovingComboBoxItemPreparesOnlyItsOwnContainers()
    {
        UIRoot root = new(200, 200);
        ComboBoxItem a = new() { Content = "A" }, b = new() { Content = "B" }, c = new() { Content = "C" };
        ComboBox comboBox = new();
        comboBox.SetItems(new[] { a, b, c });
        ElementInputBridge bridge = OpenAndHighlightLastContainer(root, comboBox);

        comboBox.Items.RemoveAt(0);
        DispatchKey(root, bridge, InputKey.Up);

        Assert.Equal("A,B,C", $"{a.Content},{b.Content},{c.Content}");
        Assert.False(ItemContainerGenerator.GetIsSelected(a));

        root.ProcessFrame();

        Assert.Equal([b], HighlightedContainers(comboBox));
        Assert.Equal("A,B,C", $"{a.Content},{b.Content},{c.Content}");
    }

    [Fact]
    public void MovingHighlightByKeyAfterInsertingComboBoxItemPreparesOnlyItsOwnContainers()
    {
        UIRoot root = new(200, 200);
        ComboBoxItem a = new() { Content = "A" }, b = new() { Content = "B" }, c = new() { Content = "C" };
        ComboBoxItem z = new() { Content = "Z" };
        ComboBox comboBox = new();
        comboBox.SetItems(new[] { a, b, c });
        ElementInputBridge bridge = OpenAndHighlightLastContainer(root, comboBox);

        comboBox.Items.Insert(0, z);
        DispatchKey(root, bridge, InputKey.Up);

        Assert.Equal("Z,A,B,C", $"{z.Content},{a.Content},{b.Content},{c.Content}");
        Assert.False(ItemContainerGenerator.GetIsSelected(c));

        root.ProcessFrame();

        Assert.Equal([b], HighlightedContainers(comboBox));
        Assert.Equal("Z,A,B,C", $"{z.Content},{a.Content},{b.Content},{c.Content}");
    }

    [Fact]
    public void MovingHighlightByKeyAfterReplacingComboBoxItemDoesNotPrepareReplacedItemForNewItem()
    {
        UIRoot root = new(200, 200);
        ComboBoxItem a = new() { Content = "A" }, b = new() { Content = "B" }, c = new() { Content = "C" };
        ComboBoxItem d = new() { Content = "D" };
        ComboBox comboBox = new();
        comboBox.SetItems(new[] { a, b, c });
        ElementInputBridge bridge = OpenAndHighlightLastContainer(root, comboBox);

        comboBox.Items[0] = d;
        DispatchKey(root, bridge, InputKey.Home);

        Assert.Equal("A", a.Content);
        Assert.NotSame(d, ItemContainerGenerator.GetItem(a));
        Assert.False(ItemContainerGenerator.GetIsSelected(a));

        root.ProcessFrame();

        Assert.Same(d, comboBox.ItemContainerGenerator.RealizedContainers[0]);
        Assert.Equal([d], HighlightedContainers(comboBox));
        Assert.Equal("A,D", $"{a.Content},{d.Content}");
    }

    [Fact]
    public void MovingHighlightByKeyAfterReplacingStringItemDoesNotPrepareReplacedItemContainer()
    {
        UIRoot root = new(200, 200);
        ObservableCollection<string> items = ["a", "b", "c"];
        ComboBox comboBox = new() { ItemsSource = items };
        ElementInputBridge bridge = OpenAndHighlightLastContainer(root, comboBox);
        UIElement containerOfA = comboBox.ItemContainerGenerator.RealizedContainers[0];

        items[0] = "x";
        DispatchKey(root, bridge, InputKey.Home);

        Assert.Equal("a", ItemContainerGenerator.GetItem(containerOfA));
        Assert.False(ItemContainerGenerator.GetIsSelected(containerOfA));

        root.ProcessFrame();

        Assert.Equal("x", HighlightedItems(comboBox));
    }

    [Fact]
    public void MovingHighlightByKeyAfterReplacingItemsWithEqualValuesMovesHighlight()
    {
        UIRoot root = new(200, 200);
        ComboBox comboBox = new();
        comboBox.SetItems(new object[] { 1, 2, 3 });
        ElementInputBridge bridge = OpenAndHighlightLastContainer(root, comboBox);

        comboBox.SetItems(new object[] { 1, 2, 3 });
        DispatchKey(root, bridge, InputKey.Up);
        root.ProcessFrame();

        UIElement second = comboBox.ItemContainerGenerator.RealizedContainers[1];
        Assert.Equal([second], HighlightedContainers(comboBox));
        Assert.Same(comboBox.GetItemAt(1), ItemContainerGenerator.GetItem(second));
    }

    // Opens the drop-down with F4 and highlights the last item with End.
    private static ElementInputBridge OpenAndHighlightLastContainer(UIRoot root, ComboBox comboBox)
    {
        root.VisualChildren.Add(comboBox);
        root.ProcessFrame();
        ElementInputBridge bridge = new();
        bridge.FocusManager.Focus(comboBox, root.InputCache.EnsureCurrent(root));
        DispatchKey(root, bridge, InputKey.F4);
        DispatchKey(root, bridge, InputKey.End);
        root.ProcessFrame();
        Assert.True(comboBox.IsDropDownOpen);
        UIElement last = comboBox.ItemContainerGenerator.RealizedContainers[comboBox.ItemCount - 1];
        Assert.Same(comboBox.GetItemAt(comboBox.ItemCount - 1), ItemContainerGenerator.GetItem(last));
        Assert.Equal([last], HighlightedContainers(comboBox));
        return bridge;
    }

    private static UIElement[] HighlightedContainers(ComboBox comboBox)
    {
        return [.. comboBox.ItemContainerGenerator.RealizedContainers
            .OrderBy(pair => pair.Key)
            .Select(pair => pair.Value)
            .Where(ItemContainerGenerator.GetIsSelected)];
    }

    private static ElementInputBridge OpenAndHighlightLastItem(UIRoot root, ComboBox comboBox)
    {
        root.VisualChildren.Add(comboBox);
        root.ProcessFrame();
        ElementInputBridge bridge = new();
        bridge.FocusManager.Focus(comboBox, root.InputCache.EnsureCurrent(root));
        DispatchKey(root, bridge, InputKey.F4);
        DispatchKey(root, bridge, InputKey.End);
        root.ProcessFrame();
        Assert.True(comboBox.IsDropDownOpen);
        Assert.Equal(-1, comboBox.SelectedIndex);
        Assert.Equal("c", HighlightedItems(comboBox));
        return bridge;
    }

    private static ElementInputBridge OpenFilteredAndHighlightApricot(UIRoot root, ComboBox comboBox)
    {
        root.VisualChildren.Add(comboBox);
        root.ProcessFrame();
        TextBox editor = Assert.IsType<TextBox>(
            comboBox.ComponentTemplateInstance!.Parts["PART_EditableTextBox"]);
        ElementInputBridge bridge = new();
        bridge.FocusManager.Focus(comboBox, root.InputCache.EnsureCurrent(root));
        editor.ReceiveTextInput("ap");
        root.ProcessFrame();
        DispatchKey(root, bridge, InputKey.Down);
        root.ProcessFrame();
        Assert.True(comboBox.IsDropDownOpen);
        Assert.Equal(-1, comboBox.SelectedIndex);
        Assert.Equal("Apple,Apricot", string.Join(",", comboBox.ItemsPresenter.LayoutPanelRoot!.VisualChildren
            .Select(ItemContainerGenerator.GetItem)));
        Assert.Equal("Apricot", HighlightedItems(comboBox));
        return bridge;
    }

    private static string HighlightedItems(ComboBox comboBox)
    {
        return string.Join(",", comboBox.ItemContainerGenerator.RealizedContainers
            .OrderBy(pair => pair.Key)
            .Where(pair => ItemContainerGenerator.GetIsSelected(pair.Value))
            .Select(pair => ItemContainerGenerator.GetItem(pair.Value)));
    }

    // Reports every change as a reset, as a collection that reorders in place does.
    private sealed class ResettingList(IEnumerable<string> items) : Collection<string>([.. items]), INotifyCollectionChanged
    {
        public event NotifyCollectionChangedEventHandler? CollectionChanged;

        public void ReverseAndReset()
        {
            string[] reversed = [.. this.Reverse()];
            Items.Clear();
            foreach (string item in reversed)
            {
                Items.Add(item);
            }

            CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }
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

    // "one" is selected and highlighted. Down moves the preview to "two" while re-preparing one of the two rows
    // throws, and the other row and the selection box still show the new preview before the exception propagates.
    [Theory]
    [InlineData("one")]
    [InlineData("two")]
    public void ArrowKeyWhoseRowPreparationThrowsStillShowsNewPreview(string failingItem)
    {
        (UIRoot root, ElementInputBridge bridge, FailingContentComboBox comboBox) = OpenNonEditableWithFirstItemSelected();

        comboBox.FailFor = failingItem;
        Exception? exception = Record.Exception(() => DispatchKey(root, bridge, InputKey.Down));

        Assert.IsType<InvalidOperationException>(exception);
        ContentPresenter selection = Assert.IsType<ContentPresenter>(
            comboBox.ComponentTemplateInstance!.Parts["PART_SelectionPresenter"]);
        Assert.Equal("two", selection.Content);
        Assert.Equal(1, selection.ContentIndex);
        if (failingItem == "one")
        {
            Assert.True(ItemContainerGenerator.GetIsSelected(comboBox.ItemContainerGenerator.RealizedContainers[1]));
        }
        else
        {
            Assert.False(ItemContainerGenerator.GetIsSelected(comboBox.ItemContainerGenerator.RealizedContainers[0]));
        }
    }

    // Escape cancels the preview of "two" while re-preparing the row of "two" throws, and the row of "one", the
    // editor, and the selection box still show the committed "one" before the exception propagates.
    [Fact]
    public void EscapeWhosePreviewRowPreparationThrowsStillRestoresCommittedSelection()
    {
        (UIRoot root, ElementInputBridge bridge, FailingContentComboBox comboBox) = OpenNonEditableWithFirstItemSelected();
        DispatchKey(root, bridge, InputKey.Down);

        comboBox.FailFor = "two";
        Exception? exception = Record.Exception(() => DispatchKey(root, bridge, InputKey.Escape));

        Assert.IsType<InvalidOperationException>(exception);
        Assert.Equal(0, comboBox.SelectedIndex);
        Assert.True(ItemContainerGenerator.GetIsSelected(comboBox.ItemContainerGenerator.RealizedContainers[0]));
        ContentPresenter selection = Assert.IsType<ContentPresenter>(
            comboBox.ComponentTemplateInstance!.Parts["PART_SelectionPresenter"]);
        Assert.Equal("one", selection.Content);
        Assert.Equal(0, selection.ContentIndex);
        TextBox editor = Assert.IsType<TextBox>(comboBox.ComponentTemplateInstance.Parts["PART_EditableTextBox"]);
        Assert.Equal("one", editor.Text);
    }

    // Enter commits the highlighted "one", which is already selected, while re-preparing its row throws, and the
    // autocomplete suffix still collapses to a caret at the end before the exception propagates.
    [Fact]
    public void EnterWhosePreviewRowPreparationThrowsStillCollapsesAutocompleteSuffix()
    {
        UIRoot root = new(300, 200);
        FailingContentComboBox comboBox = new() { IsTextFilterEnabled = false, Width = 180 };
        comboBox.SetItems(new[] { "one", "two", "three" });
        root.VisualChildren.Add(comboBox);
        root.ProcessFrame();
        comboBox.SelectedIndex = 0;
        root.ProcessFrame();
        TextBox editor = Assert.IsType<TextBox>(comboBox.ComponentTemplateInstance!.Parts["PART_EditableTextBox"]);
        ElementInputBridge bridge = new();
        bridge.FocusManager.Focus(editor, root.InputCache.EnsureCurrent(root));
        DispatchKey(root, bridge, InputKey.F4);
        root.ProcessFrame();
        editor.Select(0, editor.Text.Length);
        editor.ReceiveTextInput("on");
        Assert.Equal("one", editor.Text);
        Assert.False(editor.Selection.IsEmpty);

        comboBox.FailFor = "one";
        Exception? exception = Record.Exception(() => DispatchKey(root, bridge, InputKey.Enter));

        Assert.IsType<InvalidOperationException>(exception);
        Assert.Equal(0, comboBox.SelectedIndex);
        Assert.Equal("one", editor.Text);
        Assert.True(editor.Selection.IsEmpty);
        Assert.Equal(editor.Text.Length, editor.Selection.Active);
    }

    // "two" is highlighted. A click outside light-dismisses the drop-down while re-preparing the row of "two"
    // throws, and DropDownClosed is still raised once before the exception propagates.
    [Fact]
    public void LightDismissWhoseCancelRowPreparationThrowsStillRaisesDropDownClosed()
    {
        UIRoot root = new(300, 300);
        FailingContentComboBox comboBox = new() { IsEditable = false, Width = 100 };
        comboBox.SetItems(new[] { "one", "two", "three" });
        LayoutCanvas canvas = new();
        canvas.VisualChildren.Add(comboBox);
        root.VisualChildren.Add(canvas);
        root.ProcessFrame();
        comboBox.SelectedIndex = 0;
        root.ProcessFrame();
        ElementInputBridge bridge = new();
        bridge.FocusManager.Focus(comboBox, root.InputCache.EnsureCurrent(root));
        DispatchKey(root, bridge, InputKey.F4);
        root.ProcessFrame();
        DispatchKey(root, bridge, InputKey.Down);
        root.ProcessFrame();
        Overlay overlay = Assert.IsType<Overlay>(comboBox.ComponentTemplateInstance!.Parts["PART_DropDownOverlay"]);
        int closed = 0;
        comboBox.DropDownClosed += (_, _) => closed++;

        comboBox.FailFor = "two";
        Exception? exception = Record.Exception(() =>
        {
            bridge.Dispatch(root, PointerFrame(290, 290, currentDown: true));
            bridge.Dispatch(root, PointerFrame(290, 290, previousDown: true));
        });
        comboBox.FailFor = null;
        root.ProcessFrame();

        Assert.IsType<InvalidOperationException>(exception);
        Assert.False(comboBox.IsDropDownOpen);
        Assert.False(overlay.IsOpen);
        Assert.Equal(0, comboBox.SelectedIndex);
        Assert.Equal(1, closed);
        Assert.Same(canvas, Assert.Single(root.VisualChildren));
    }

    // The overlay part opens the drop-down while re-preparing the row of the selected "one" throws, and
    // DropDownOpened is still raised once before the exception propagates.
    [Fact]
    public void OverlayOpenWhoseTransactionRowPreparationThrowsStillRaisesDropDownOpened()
    {
        (UIRoot root, ElementInputBridge bridge, FailingContentComboBox comboBox) = OpenNonEditableWithFirstItemSelected();
        DispatchKey(root, bridge, InputKey.F4);
        root.ProcessFrame();
        Overlay overlay = Assert.IsType<Overlay>(comboBox.ComponentTemplateInstance!.Parts["PART_DropDownOverlay"]);
        Assert.False(overlay.IsOpen);
        int opened = 0;
        comboBox.DropDownOpened += (_, _) => opened++;

        comboBox.FailFor = "one";
        Exception? exception = Record.Exception(() => overlay.IsOpen = true);
        comboBox.FailFor = null;
        root.ProcessFrame();

        Assert.IsType<InvalidOperationException>(exception);
        Assert.True(comboBox.IsDropDownOpen);
        Assert.True(overlay.IsOpen);
        Assert.Equal(1, opened);
    }

    // The drop-down is open with "one" selected and highlighted. SelectedIndex = 1 moves the preview to "two" while
    // re-preparing one of the two preview rows throws, and the selection change still completes and raises
    // SelectionChanged before the exception propagates out of the setter.
    [Theory]
    [InlineData("one")]
    [InlineData("two")]
    public void SelectedIndexChangeWhosePreviewRowPreparationThrowsRaisesSelectionChangedAndPropagates(string failingItem)
    {
        (_, _, FailingContentComboBox comboBox) = OpenNonEditableWithFirstItemSelected();
        List<string> raised = [];
        comboBox.SelectionChanged += (_, args) =>
            raised.Add($"[{string.Join(",", args.RemovedItems)}]->[{string.Join(",", args.AddedItems)}]");

        comboBox.FailFor = failingItem;
        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() => comboBox.SelectedIndex = 1);

        Assert.Equal("content preparation failed", exception.Message);
        Assert.Equal(1, comboBox.SelectedIndex);
        Assert.Equal(["[one]->[two]"], raised);
        ContentPresenter selection = Assert.IsType<ContentPresenter>(
            comboBox.ComponentTemplateInstance!.Parts["PART_SelectionPresenter"]);
        Assert.Equal("two", selection.Content);
        Assert.Equal(1, selection.ContentIndex);
        if (failingItem == "one")
        {
            Assert.True(ItemContainerGenerator.GetIsSelected(comboBox.ItemContainerGenerator.RealizedContainers[1]));
        }
        else
        {
            Assert.False(ItemContainerGenerator.GetIsSelected(comboBox.ItemContainerGenerator.RealizedContainers[0]));
        }
    }

    // The drop-down is open with "one" selected and highlighted, and a handler closes it when SelectedIndex changes.
    // SelectedIndex = 1 cancels the preview while re-preparing one of the rows throws, and the selection change still
    // raises SelectionChanged and the overlay still closes before the exception propagates out of the setter.
    [Theory]
    [InlineData("one")]
    [InlineData("two")]
    public void SelectedIndexChangeThatClosesDropDownWhoseRowPreparationThrowsRaisesSelectionChangedAndPropagates(
        string failingItem)
    {
        (UIRoot root, _, FailingContentComboBox comboBox) = OpenNonEditableWithFirstItemSelected();
        Overlay overlay = Assert.IsType<Overlay>(comboBox.ComponentTemplateInstance!.Parts["PART_DropDownOverlay"]);
        comboBox.PropertyChanged += (_, args) =>
        {
            if (ReferenceEquals(args.Property, ComboBox.SelectedIndexProperty))
            {
                comboBox.IsDropDownOpen = false;
            }
        };
        List<string> raised = [];
        comboBox.SelectionChanged += (_, args) =>
            raised.Add($"[{string.Join(",", args.RemovedItems)}]->[{string.Join(",", args.AddedItems)}]");

        comboBox.FailFor = failingItem;
        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() => comboBox.SelectedIndex = 1);
        comboBox.FailFor = null;
        root.ProcessFrame();

        Assert.Equal("content preparation failed", exception.Message);
        Assert.Equal(1, comboBox.SelectedIndex);
        Assert.Equal(["[one]->[two]"], raised);
        Assert.False(comboBox.IsDropDownOpen);
        Assert.False(overlay.IsOpen);
    }

    // "one" is selected and the rows are realized, with the drop-down open or closed. SetValue(SelectedIndexProperty,
    // 1), the path of a binding, an aspect, or application code, re-prepares the row of "two", which throws. As with
    // the setter, the selection change still raises SelectionChanged and the selection box shows "two" before the
    // exception propagates.
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void SelectedIndexSetValueWhoseRowPreparationThrowsShowsNewItemAndPropagates(bool dropDownOpen)
    {
        (UIRoot root, ElementInputBridge bridge, FailingContentComboBox comboBox) = OpenNonEditableWithFirstItemSelected();
        if (!dropDownOpen)
        {
            DispatchKey(root, bridge, InputKey.Escape);
            root.ProcessFrame();
            Assert.True(comboBox.ItemContainerGenerator.RealizedContainers.ContainsKey(1));
        }

        List<string> raised = [];
        comboBox.SelectionChanged += (_, args) =>
            raised.Add($"[{string.Join(",", args.RemovedItems)}]->[{string.Join(",", args.AddedItems)}]");

        comboBox.FailFor = "two";
        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
            () => comboBox.SetValue(ComboBox.SelectedIndexProperty, 1));
        comboBox.FailFor = null;
        root.ProcessFrame();

        Assert.Equal("content preparation failed", exception.Message);
        Assert.Equal(1, comboBox.SelectedIndex);
        Assert.Equal(1, comboBox.SelectionModel.SelectedIndex);
        Assert.Equal(["[one]->[two]"], raised);
        Assert.Equal("two", comboBox.Text);
        ContentPresenter selection = Assert.IsType<ContentPresenter>(
            comboBox.ComponentTemplateInstance!.Parts["PART_SelectionPresenter"]);
        Assert.Equal("two", selection.Content);
        Assert.Equal(1, selection.ContentIndex);
        Assert.Equal(dropDownOpen, comboBox.IsDropDownOpen);
    }

    // The drop-down is open with "one" selected, and a handler closes it when SelectedIndex changes.
    // SetValue(SelectedIndexProperty, 1) cancels the preview while re-preparing the row of "two" throws. As with the
    // setter, the selection model still follows, SelectionChanged is still raised, and the overlay still closes
    // before the exception propagates.
    [Fact]
    public void SelectedIndexSetValueThatClosesDropDownWhoseRowPreparationThrowsRaisesSelectionChangedAndPropagates()
    {
        (UIRoot root, _, FailingContentComboBox comboBox) = OpenNonEditableWithFirstItemSelected();
        Overlay overlay = Assert.IsType<Overlay>(comboBox.ComponentTemplateInstance!.Parts["PART_DropDownOverlay"]);
        comboBox.PropertyChanged += (_, args) =>
        {
            if (ReferenceEquals(args.Property, ComboBox.SelectedIndexProperty))
            {
                comboBox.IsDropDownOpen = false;
            }
        };
        List<string> raised = [];
        comboBox.SelectionChanged += (_, args) =>
            raised.Add($"[{string.Join(",", args.RemovedItems)}]->[{string.Join(",", args.AddedItems)}]");

        comboBox.FailFor = "two";
        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
            () => comboBox.SetValue(ComboBox.SelectedIndexProperty, 1));
        comboBox.FailFor = null;
        root.ProcessFrame();

        Assert.Equal("content preparation failed", exception.Message);
        Assert.Equal(1, comboBox.SelectedIndex);
        Assert.Equal(1, comboBox.SelectionModel.SelectedIndex);
        Assert.Equal(["[one]->[two]"], raised);
        Assert.Equal("two", comboBox.Text);
        Assert.False(comboBox.IsDropDownOpen);
        Assert.False(overlay.IsOpen);
    }

    // SetValue(SelectedIndexProperty, 1) and SelectedIndex = 1 raise the change in the same order: the selection model
    // already holds 1 when PropertyChanged is raised, and Text already shows "two" when SelectionChanged is raised.
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void SelectedIndexChangeRaisesPropertyChangedAfterModelAndSelectionChangedAfterText(bool setValue)
    {
        UIRoot root = new(100, 100);
        ComboBox comboBox = new() { IsEditable = false };
        comboBox.SetItems(new[] { "one", "two", "three" });
        root.VisualChildren.Add(comboBox);
        comboBox.SelectedIndex = 0;
        root.ProcessFrame();
        int modelIndexInPropertyChanged = -2;
        string? textInSelectionChanged = null;
        comboBox.PropertyChanged += (_, args) =>
        {
            if (ReferenceEquals(args.Property, ComboBox.SelectedIndexProperty))
            {
                modelIndexInPropertyChanged = comboBox.SelectionModel.SelectedIndex;
            }
        };
        comboBox.SelectionChanged += (_, _) => textInSelectionChanged = comboBox.Text;

        if (setValue)
        {
            comboBox.SetValue(ComboBox.SelectedIndexProperty, 1);
        }
        else
        {
            comboBox.SelectedIndex = 1;
        }

        Assert.Equal(1, modelIndexInPropertyChanged);
        Assert.Equal("two", textInSelectionChanged);
    }

    // The drop-down was opened and closed with "one" selected, so the rows stay realized, and a handler opens it
    // when SelectedIndex changes. SelectedIndex = 1 begins the preview of "two" while re-preparing its row throws,
    // and the selection change still raises SelectionChanged and the overlay still opens before the exception
    // propagates out of the setter.
    [Fact]
    public void SelectedIndexChangeThatOpensDropDownWhoseRowPreparationThrowsRaisesSelectionChangedAndPropagates()
    {
        (UIRoot root, ElementInputBridge bridge, FailingContentComboBox comboBox) = OpenNonEditableWithFirstItemSelected();
        Overlay overlay = Assert.IsType<Overlay>(comboBox.ComponentTemplateInstance!.Parts["PART_DropDownOverlay"]);
        DispatchKey(root, bridge, InputKey.Escape);
        root.ProcessFrame();
        Assert.True(comboBox.ItemContainerGenerator.RealizedContainers.ContainsKey(1));
        comboBox.PropertyChanged += (_, args) =>
        {
            if (ReferenceEquals(args.Property, ComboBox.SelectedIndexProperty))
            {
                comboBox.IsDropDownOpen = true;
            }
        };
        List<string> raised = [];
        comboBox.SelectionChanged += (_, args) =>
            raised.Add($"[{string.Join(",", args.RemovedItems)}]->[{string.Join(",", args.AddedItems)}]");

        comboBox.FailFor = "two";
        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() => comboBox.SelectedIndex = 1);
        comboBox.FailFor = null;
        root.ProcessFrame();

        Assert.Equal("content preparation failed", exception.Message);
        Assert.Equal(1, comboBox.SelectedIndex);
        Assert.Equal(["[one]->[two]"], raised);
        Assert.True(comboBox.IsDropDownOpen);
        Assert.True(overlay.IsOpen);
    }

    // The drop-down is open with "one" selected and highlighted, and a handler presses Enter when SelectedIndex
    // changes. SelectedIndex = 1 runs the Enter commit of the highlighted "one" inside the change, which selects
    // "one" again while re-preparing the row of "one" throws. Both selection changes still raise SelectionChanged and
    // the drop-down still closes before the exception propagates out of the setter.
    [Fact]
    public void SelectedIndexChangeThatCommitsDropDownWhoseRowPreparationThrowsRaisesSelectionChangedAndPropagates()
    {
        (UIRoot root, ElementInputBridge bridge, FailingContentComboBox comboBox) = OpenNonEditableWithFirstItemSelected();
        Overlay overlay = Assert.IsType<Overlay>(comboBox.ComponentTemplateInstance!.Parts["PART_DropDownOverlay"]);
        bool pressed = false;
        comboBox.PropertyChanged += (_, args) =>
        {
            if (ReferenceEquals(args.Property, ComboBox.SelectedIndexProperty) && !pressed)
            {
                pressed = true;
                DispatchKey(root, bridge, InputKey.Enter);
            }
        };
        List<string> raised = [];
        comboBox.SelectionChanged += (_, args) =>
            raised.Add($"[{string.Join(",", args.RemovedItems)}]->[{string.Join(",", args.AddedItems)}]");

        comboBox.FailFor = "one";
        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() => comboBox.SelectedIndex = 1);
        comboBox.FailFor = null;
        root.ProcessFrame();

        Assert.Equal("content preparation failed", exception.Message);
        Assert.True(pressed);
        Assert.Equal(0, comboBox.SelectedIndex);
        Assert.Equal(["[two]->[one]", "[one]->[two]"], raised);
        Assert.False(comboBox.IsDropDownOpen);
        Assert.False(overlay.IsOpen);
    }

    // "one" is selected and "tw" autocompletes to the highlighted "two". Enter or Tab commits "two" while Selector's
    // re-preparation of the old or the new row throws inside the SelectedIndex setter, and the autocomplete suffix
    // still collapses to a caret at the end before the exception propagates.
    [Theory]
    [InlineData(InputKey.Enter, "one")]
    [InlineData(InputKey.Enter, "two")]
    [InlineData(InputKey.Tab, "one")]
    [InlineData(InputKey.Tab, "two")]
    public void CommitOfAnotherItemWhoseRowPreparationThrowsStillCollapsesAutocompleteSuffix(InputKey key, string failingItem)
    {
        (UIRoot root, ElementInputBridge bridge, FailingContentComboBox comboBox, TextBox editor) =
            OpenEditableWithAutocompletedSecondItem(isTextFilterEnabled: false);

        comboBox.FailFor = failingItem;
        Exception? exception = Record.Exception(() => DispatchKey(root, bridge, key));

        Assert.IsType<InvalidOperationException>(exception);
        Assert.Equal(1, comboBox.SelectedIndex);
        Assert.Equal("two", comboBox.Text);
        Assert.Equal("two", editor.Text);
        Assert.True(editor.Selection.IsEmpty);
        Assert.Equal(editor.Text.Length, editor.Selection.Active);
    }

    // "tw" filters the drop-down to "two". Enter commits "two" while re-preparing its row throws inside the
    // SelectedIndex setter, and the filter is still cleared, so the list shows every item again.
    [Fact]
    public void CommitOfAnotherItemWhoseRowPreparationThrowsStillClearsFilter()
    {
        (UIRoot root, ElementInputBridge bridge, FailingContentComboBox comboBox, TextBox editor) =
            OpenEditableWithAutocompletedSecondItem(isTextFilterEnabled: true);
        Assert.Equal(1, comboBox.ViewItemCount);

        comboBox.FailFor = "two";
        Exception? exception = Record.Exception(() => DispatchKey(root, bridge, InputKey.Enter));

        Assert.IsType<InvalidOperationException>(exception);
        Assert.Equal(1, comboBox.SelectedIndex);
        Assert.Equal(3, comboBox.ViewItemCount);
        Assert.True(editor.Selection.IsEmpty);
        Assert.Equal(editor.Text.Length, editor.Selection.Active);
    }

    // A click on the highlighted "two" commits it while Selector's re-preparation of the old row of "one" throws
    // inside the SelectedIndex setter, and the autocomplete suffix still collapses before the exception propagates.
    [Fact]
    public void ClickCommitOfAnotherItemWhoseOldRowPreparationThrowsStillCollapsesAutocompleteSuffix()
    {
        (UIRoot root, ElementInputBridge bridge, FailingContentComboBox comboBox, TextBox editor) =
            OpenEditableWithAutocompletedSecondItem(isTextFilterEnabled: false);
        UIElement clicked = comboBox.ItemContainerGenerator.RealizedContainers[1];
        float x = clicked.ArrangedBounds.X + 2;
        float y = clicked.ArrangedBounds.Y + 2;

        comboBox.FailFor = "one";
        Exception? exception = Record.Exception(() =>
        {
            bridge.Dispatch(root, PointerFrame(x, y, currentDown: true));
            bridge.Dispatch(root, PointerFrame(x, y, previousDown: true));
        });

        Assert.IsType<InvalidOperationException>(exception);
        Assert.Equal(1, comboBox.SelectedIndex);
        Assert.Equal("two", editor.Text);
        Assert.True(editor.Selection.IsEmpty);
        Assert.Equal(editor.Text.Length, editor.Selection.Active);
    }

    // Enter and Tab commit the highlighted "one" while re-preparing its row throws, and the drop-down still closes
    // before the exception propagates. A later Down then moves the committed selection of a closed drop-down.
    [Theory]
    [InlineData(InputKey.Enter)]
    [InlineData(InputKey.Tab)]
    public void CommitKeyWhosePreviewRowPreparationThrowsStillClosesDropDown(InputKey key)
    {
        (UIRoot root, ElementInputBridge bridge, FailingContentComboBox comboBox) = OpenNonEditableWithFirstItemSelected();
        Overlay overlay = Assert.IsType<Overlay>(comboBox.ComponentTemplateInstance!.Parts["PART_DropDownOverlay"]);

        comboBox.FailFor = "one";
        Exception? exception = Record.Exception(() => DispatchKey(root, bridge, key));
        comboBox.FailFor = null;
        root.ProcessFrame();

        Assert.IsType<InvalidOperationException>(exception);
        Assert.False(comboBox.IsDropDownOpen);
        Assert.False(overlay.IsOpen);
        Assert.Equal(0, comboBox.SelectedIndex);
    }

    // Escape cancels the preview of "two" while re-preparing the row of "two" throws, and the overlay still closes
    // with IsDropDownOpen before the exception propagates.
    [Fact]
    public void EscapeWhosePreviewRowPreparationThrowsStillClosesOverlay()
    {
        (UIRoot root, ElementInputBridge bridge, FailingContentComboBox comboBox) = OpenNonEditableWithFirstItemSelected();
        Overlay overlay = Assert.IsType<Overlay>(comboBox.ComponentTemplateInstance!.Parts["PART_DropDownOverlay"]);
        DispatchKey(root, bridge, InputKey.Down);

        comboBox.FailFor = "two";
        Exception? exception = Record.Exception(() => DispatchKey(root, bridge, InputKey.Escape));
        comboBox.FailFor = null;
        root.ProcessFrame();

        Assert.IsType<InvalidOperationException>(exception);
        Assert.False(comboBox.IsDropDownOpen);
        Assert.False(overlay.IsOpen);
        Assert.Equal(0, comboBox.SelectedIndex);
    }

    // F4 reopens the drop-down while re-preparing the realized row of the selected "one" throws, and the overlay
    // still opens with IsDropDownOpen before the exception propagates. Down then moves only the preview.
    [Fact]
    public void OpenWhosePreviewRowPreparationThrowsStillOpensOverlay()
    {
        (UIRoot root, ElementInputBridge bridge, FailingContentComboBox comboBox) = OpenNonEditableWithFirstItemSelected();
        Overlay overlay = Assert.IsType<Overlay>(comboBox.ComponentTemplateInstance!.Parts["PART_DropDownOverlay"]);
        DispatchKey(root, bridge, InputKey.Escape);
        root.ProcessFrame();

        comboBox.FailFor = "one";
        Exception? exception = Record.Exception(() => DispatchKey(root, bridge, InputKey.F4));
        comboBox.FailFor = null;
        root.ProcessFrame();

        Assert.IsType<InvalidOperationException>(exception);
        Assert.True(comboBox.IsDropDownOpen);
        Assert.True(overlay.IsOpen);

        DispatchKey(root, bridge, InputKey.Down);
        root.ProcessFrame();

        Assert.Equal(0, comboBox.SelectedIndex);
    }

    // A click on "two" commits it while re-preparing the row of the highlighted "one" throws, and the selection
    // still changes and the drop-down still closes before the exception propagates.
    [Fact]
    public void ClickingItemWhilePreviewRowPreparationThrowsStillCommitsAndClosesDropDown()
    {
        UIRoot root = new(100, 100);
        FailingContentComboBox comboBox = new() { IsEditable = false, ItemsPanel = new LayoutStackPanel() };
        comboBox.SetItems(new[] { "one", "two", "three" });
        LayoutCanvas canvas = new();
        canvas.VisualChildren.Add(comboBox);
        root.VisualChildren.Add(canvas);
        root.ProcessFrame();
        comboBox.SelectedIndex = 0;
        comboBox.IsDropDownOpen = true;
        root.ProcessFrame();
        // Text rows measure 0 px tall without a font, so each row gets a fixed height.
        foreach (UIElement container in comboBox.ItemContainerGenerator.RealizedContainers.Values)
        {
            container.Height = 10;
        }

        root.ProcessFrame();
        Overlay overlay = Assert.IsType<Overlay>(comboBox.ComponentTemplateInstance!.Parts["PART_DropDownOverlay"]);
        UIElement clicked = comboBox.ItemContainerGenerator.RealizedContainers[1];
        float x = clicked.ArrangedBounds.X + 2;
        float y = clicked.ArrangedBounds.Y + 2;
        ElementInputBridge bridge = new();

        comboBox.FailFor = "one";
        Exception? exception = Record.Exception(() =>
        {
            bridge.Dispatch(root, PointerFrame(x, y, currentDown: true));
            bridge.Dispatch(root, PointerFrame(x, y, previousDown: true));
        });
        comboBox.FailFor = null;
        root.ProcessFrame();

        Assert.IsType<InvalidOperationException>(exception);
        Assert.Equal(1, comboBox.SelectedIndex);
        Assert.False(comboBox.IsDropDownOpen);
        Assert.False(overlay.IsOpen);
    }

    // An editable ComboBox with "one" selected and its drop-down open, where "tw" autocompletes to the highlighted
    // "two" with the suffix "o" selected. Every row is 10 px tall, so a click can reach it.
    private static (UIRoot Root, ElementInputBridge Bridge, FailingContentComboBox ComboBox, TextBox Editor)
        OpenEditableWithAutocompletedSecondItem(bool isTextFilterEnabled)
    {
        UIRoot root = new(300, 200);
        FailingContentComboBox comboBox = new()
        {
            IsTextFilterEnabled = isTextFilterEnabled,
            Width = 180,
            ItemsPanel = new LayoutStackPanel()
        };
        comboBox.SetItems(new[] { "one", "two", "three" });
        LayoutCanvas canvas = new();
        canvas.VisualChildren.Add(comboBox);
        root.VisualChildren.Add(canvas);
        root.ProcessFrame();
        comboBox.SelectedIndex = 0;
        root.ProcessFrame();
        TextBox editor = Assert.IsType<TextBox>(comboBox.ComponentTemplateInstance!.Parts["PART_EditableTextBox"]);
        ElementInputBridge bridge = new();
        bridge.FocusManager.Focus(editor, root.InputCache.EnsureCurrent(root));
        DispatchKey(root, bridge, InputKey.F4);
        root.ProcessFrame();
        foreach (UIElement container in comboBox.ItemContainerGenerator.RealizedContainers.Values)
        {
            container.Height = 10;
        }

        root.ProcessFrame();
        editor.Select(0, editor.Text.Length);
        editor.ReceiveTextInput("tw");
        root.ProcessFrame();
        Assert.True(comboBox.IsDropDownOpen);
        Assert.Equal("two", editor.Text);
        Assert.Equal(2, editor.Selection.Start);
        Assert.Equal(3, editor.Selection.End);
        Assert.Equal(0, comboBox.SelectedIndex);
        return (root, bridge, comboBox, editor);
    }

    // The drop-down of a non-editable ComboBox is open with "one" selected and highlighted.
    private static (UIRoot Root, ElementInputBridge Bridge, FailingContentComboBox ComboBox) OpenNonEditableWithFirstItemSelected()
    {
        UIRoot root = new(100, 100);
        FailingContentComboBox comboBox = new() { IsEditable = false };
        comboBox.SetItems(new[] { "one", "two", "three" });
        root.VisualChildren.Add(comboBox);
        root.ProcessFrame();
        comboBox.SelectedIndex = 0;
        root.ProcessFrame();
        ElementInputBridge bridge = new();
        bridge.FocusManager.Focus(comboBox, root.InputCache.EnsureCurrent(root));
        DispatchKey(root, bridge, InputKey.F4);
        root.ProcessFrame();
        Assert.True(ItemContainerGenerator.GetIsSelected(comboBox.ItemContainerGenerator.RealizedContainers[0]));
        return (root, bridge, comboBox);
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

    private static InputFrame TextInputFrame(string text)
    {
        return new InputFrame(
            PointerSnapshot.Empty,
            PointerSnapshot.Empty,
            KeyboardSnapshot.Empty,
            KeyboardSnapshot.Empty,
            [new TextInputSnapshotEvent(text)]);
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

    private static int[] RealizedSourceIndices(ComboBox comboBox)
    {
        return comboBox.ItemsPresenter.LayoutPanelRoot!.VisualChildren
            .Select(ItemContainerGenerator.GetItemIndex)
            .ToArray();
    }

    private sealed record Person(Address? Address);

    private sealed record SearchItem(string Search, string Display);

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

    // Throws while it prepares the content of a container for FailFor.
    private sealed class FailingContentComboBox : ComboBox
    {
        public object? FailFor { get; set; }

        protected override void PrepareItemContent(UIElement container, int index, object? item)
        {
            if (FailFor is not null && Equals(item, FailFor))
            {
                throw new InvalidOperationException("content preparation failed");
            }

            base.PrepareItemContent(container, index, item);
        }
    }

    private sealed class FixedElement : UIElement
    {
        protected override LayoutSize MeasureCore(MeasureContext context)
        {
            return new LayoutSize(20, 10);
        }
    }
}
