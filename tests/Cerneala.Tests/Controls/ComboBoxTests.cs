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
        Assert.Equal(UiPropertyValueSource.AspectBase, comboBox.GetValueSource(Control.BackgroundProperty));
        Assert.Equal(UiPropertyValueSource.AspectBase, comboBox.GetValueSource(Control.ForegroundProperty));
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
