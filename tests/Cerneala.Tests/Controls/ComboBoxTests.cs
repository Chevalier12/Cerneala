using System.Collections.ObjectModel;
using System.Collections.Specialized;
using Cerneala.UI.Controls;
using Cerneala.UI.Controls.Items;
using Cerneala.UI.Controls.Templates;
using Cerneala.Drawing;
using Cerneala.UI.Core;
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

    private sealed class FixedElement : UIElement
    {
        protected override LayoutSize MeasureCore(MeasureContext context)
        {
            return new LayoutSize(20, 10);
        }
    }
}
