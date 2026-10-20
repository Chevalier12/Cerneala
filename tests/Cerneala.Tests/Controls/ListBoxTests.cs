using Cerneala.Drawing;
using Cerneala.UI;
using Cerneala.UI.Aspect;
using Cerneala.UI.Controls;
using Cerneala.UI.Controls.Templates;
using Cerneala.UI.Elements;
using Cerneala.UI.Input;
using Cerneala.UI.Invalidation;
using Cerneala.UI.Layout;
using Cerneala.UI.Layout.Virtualization;
using Cerneala.UI.Media;
using Cerneala.UI.Rendering;

namespace Cerneala.Tests.Controls;

public sealed class ListBoxTests
{
    [Fact]
    public void ListBoxCreatesListBoxItemContainers()
    {
        ListBox listBox = new();
        listBox.SetItems(new[] { "one" });

        IReadOnlyList<UIElement> containers = listBox.ItemContainerGenerator.Realize();

        Assert.IsType<ListBoxItem>(containers[0]);
    }

    [Fact]
    public void ListBoxClickSelectsItem()
    {
        UIRoot root = new(100, 100);
        ListBox listBox = new();
        listBox.SetItems(new UIElement[] { new FixedElement(), new FixedElement() });
        root.VisualChildren.Add(listBox);
        listBox.Measure(new MeasureContext(new LayoutSize(100, 100)));
        listBox.Arrange(new ArrangeContext(new LayoutRect(0, 0, 100, 100)));

        ElementInputBridge bridge = new();
        bridge.Dispatch(root, PointerFrame(5, 15, currentDown: true));
        bridge.Dispatch(root, PointerFrame(5, 15, previousDown: true));

        Assert.Equal(1, listBox.SelectedIndex);
        ListBoxItem item = Assert.IsType<ListBoxItem>(listBox.ItemContainerGenerator.RealizedContainers[1]);
        Assert.True(item.IsSelected);
    }

    // As in WPF, where ButtonBase marks the mouse button events handled, a button in a row owns its click and does
    // not select the row.
    [Fact]
    public void ListBoxClickOnTemplateCheckBoxChecksItWithoutSelectingRow()
    {
        (UIRoot root, ListBox listBox) = CreateListBox(context => new CheckBox { Content = context.Data });
        CheckBox check = Assert.IsType<CheckBox>(FindDescendant<CheckBox>(listBox.ItemContainerGenerator.RealizedContainers[1]));
        int clicks = 0;
        check.Click += (_, _) => clicks++;
        LayoutRect bounds = check.ArrangedBounds;
        ElementInputBridge bridge = new();

        Click(root, bridge, bounds.X + 6, bounds.Y + bounds.Height / 2);

        Assert.Equal(1, clicks);
        Assert.True(check.IsChecked);
        Assert.Equal(-1, listBox.SelectedIndex);
        Assert.False(Assert.IsType<ListBoxItem>(listBox.ItemContainerGenerator.RealizedContainers[1]).IsSelected);

        Click(root, bridge, bounds.X + 6, bounds.Y + bounds.Height / 2);

        Assert.Equal(2, clicks);
        Assert.False(check.IsChecked);
        Assert.Equal(-1, listBox.SelectedIndex);
    }

    // The click stays the CheckBox's when its Click handler removes or collapses it, or detaches the ListBox, before the
    // MouseUp bubbles to the row: the row decides with the route the input bridge resolved the click with (#550).
    [Theory]
    [InlineData("remove")]
    [InlineData("collapse")]
    [InlineData("detach")]
    public void ListBoxClickOnCheckBoxThatItsClickHandlerRemovesDoesNotSelectRow(string change)
    {
        CheckBox check = new() { Content = "check", Width = 80, Height = 24 };
        ListBoxItem one = new() { Content = "One" };
        ListBoxItem two = new() { Content = check };
        ListBox listBox = new() { Width = 160, Height = 100 };
        listBox.Items.Add(one);
        listBox.Items.Add(two);
        Cerneala.UI.Layout.Panels.Canvas canvas = new();
        int clicks = 0;
        check.Click += (_, _) =>
        {
            clicks++;
            switch (change)
            {
                case "remove":
                    two.Content = "removed";
                    break;
                case "collapse":
                    check.Visibility = Visibility.Collapsed;
                    break;
                default:
                    canvas.VisualChildren.Remove(listBox);
                    break;
            }
        };
        int selectionChanges = 0;
        listBox.SelectionChanged += (_, _) => selectionChanges++;
        UIRoot root = new(300, 200);
        canvas.VisualChildren.Add(listBox);
        root.VisualChildren.Add(canvas);
        root.ProcessFrame();
        LayoutRect bounds = check.ArrangedBounds;

        Click(root, new ElementInputBridge(), bounds.X + bounds.Width / 2, bounds.Y + bounds.Height / 2);

        Assert.Equal(1, clicks);
        Assert.Equal(-1, listBox.SelectedIndex);
        Assert.False(two.IsSelected);
        Assert.Equal(0, selectionChanges);
    }

    [Fact]
    public void ListBoxClickOnTemplateTextSelectsRow()
    {
        (UIRoot root, ListBox listBox) = CreateListBox(context => new TextBlock { Text = context.Data });
        TextBlock text = Assert.Single(FindTextBlocks(listBox.ItemContainerGenerator.RealizedContainers[1]));
        LayoutRect bounds = text.ArrangedBounds;
        ElementInputBridge bridge = new();

        Click(root, bridge, bounds.X + 2, bounds.Y + bounds.Height / 2);

        Assert.Equal(1, listBox.SelectedIndex);
        Assert.True(Assert.IsType<ListBoxItem>(listBox.ItemContainerGenerator.RealizedContainers[1]).IsSelected);
    }

    [Fact]
    public void ListBoxClickOnEmptyPartOfTemplateRowSelectsRow()
    {
        (UIRoot root, ListBox listBox) = CreateListBox(context => new CheckBox { Content = context.Data, Width = 60 });
        ListBoxItem row = Assert.IsType<ListBoxItem>(listBox.ItemContainerGenerator.RealizedContainers[1]);
        CheckBox check = Assert.IsType<CheckBox>(FindDescendant<CheckBox>(row));
        int clicks = 0;
        check.Click += (_, _) => clicks++;
        float x = row.ArrangedBounds.X + row.ArrangedBounds.Width - 10;
        Assert.True(x > check.ArrangedBounds.X + check.ArrangedBounds.Width);
        ElementInputBridge bridge = new();

        Click(root, bridge, x, row.ArrangedBounds.Y + row.ArrangedBounds.Height / 2);

        Assert.Equal(0, clicks);
        Assert.False(check.IsChecked);
        Assert.Equal(1, listBox.SelectedIndex);
        Assert.True(row.IsSelected);
    }

    // A click into a TextBox of the ItemTemplate is the text box's, as WPF's text editor handles the left button
    // down and up before ListBoxItem sees them (#586): the text box gets the focus and the row is not selected.
    [Fact]
    public void ListBoxClickInsideTemplateTextBoxFocusesItWithoutSelectingRow()
    {
        (UIRoot root, ListBox listBox) = CreateListBox(context => new TextBox { Text = context.Data, Width = 60 });
        ListBoxItem row = Assert.IsType<ListBoxItem>(listBox.ItemContainerGenerator.RealizedContainers[1]);
        TextBox text = Assert.IsType<TextBox>(FindDescendant<TextBox>(row));
        LayoutRect bounds = text.ArrangedBounds;
        ElementInputBridge bridge = new();

        Click(root, bridge, bounds.X + bounds.Width / 2, bounds.Y + bounds.Height / 2);

        Assert.True(text.IsKeyboardFocused);
        Assert.Equal(-1, listBox.SelectedIndex);
        Assert.False(row.IsSelected);
    }

    // As in WPF, where the press on the row selects it, a release on a button in the row that the button does not turn
    // into a click, because the press was on the row, is not the button's click and still selects the row.
    [Fact]
    public void ListBoxPressOnEmptyPartOfRowReleasedOnTemplateCheckBoxSelectsRowWithoutCheckingIt()
    {
        (UIRoot root, ListBox listBox) = CreateListBox(context => new CheckBox { Content = context.Data, Width = 60 });
        ListBoxItem row = Assert.IsType<ListBoxItem>(listBox.ItemContainerGenerator.RealizedContainers[1]);
        CheckBox check = Assert.IsType<CheckBox>(FindDescendant<CheckBox>(row));
        int clicks = 0;
        check.Click += (_, _) => clicks++;
        LayoutRect rowBounds = row.ArrangedBounds;
        LayoutRect checkBounds = check.ArrangedBounds;
        float pressX = rowBounds.X + rowBounds.Width - 10;
        Assert.True(pressX > checkBounds.X + checkBounds.Width);
        float releaseX = checkBounds.X + 6;
        float releaseY = checkBounds.Y + checkBounds.Height / 2;
        ElementInputBridge bridge = new();

        bridge.Dispatch(root, PointerFrame(pressX, rowBounds.Y + rowBounds.Height / 2, currentDown: true));
        bridge.Dispatch(root, PointerFrame(releaseX, releaseY, previousDown: true, currentDown: true));
        bridge.Dispatch(root, PointerFrame(releaseX, releaseY, previousDown: true));
        root.ProcessFrame();

        Assert.Equal(0, clicks);
        Assert.False(check.IsChecked);
        Assert.Equal(1, listBox.SelectedIndex);
        Assert.True(row.IsSelected);
    }

    // As a WPF ListBoxItem focuses itself when it handles MouseLeftButtonDown (#662): a click on a row takes keyboard
    // focus from the button that had it, so a Space press after the click does not click that button.
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ClickOnListBoxItemFocusesItSoSpaceDoesNotClickPreviouslyFocusedButton(bool generatedContainers)
    {
        Button other = new() { Content = "other", Width = 100, Height = 40 };
        ListBox listBox = new() { Width = 150, Height = 100 };
        if (generatedContainers)
        {
            listBox.SetItems(new[] { "a", "b" });
        }
        else
        {
            listBox.Items.Add(new ListBoxItem { Content = "a" });
            listBox.Items.Add(new ListBoxItem { Content = "b" });
        }

        UIRoot root = CreateFocusWindowRoot(other, listBox);
        ListBoxItem first = Assert.IsType<ListBoxItem>(listBox.ItemContainerGenerator.RealizedContainers[0]);
        int otherClicks = 0;
        other.Click += (_, _) => otherClicks++;
        ElementInputBridge bridge = new();
        ClickCenter(root, bridge, other);
        Assert.True(other.IsKeyboardFocused);
        int clicksBefore = otherClicks;

        ClickCenter(root, bridge, first);
        bool otherFocusedAfterItemClick = other.IsKeyboardFocused;
        bridge.Dispatch(root, KeyFrame([], [InputKey.Space]));
        bridge.Dispatch(root, KeyFrame([InputKey.Space], []));
        root.ProcessFrame();

        Assert.Equal(0, listBox.SelectedIndex);
        Assert.True(first.IsKeyboardFocused);
        Assert.True(listBox.IsKeyboardFocusWithin);
        Assert.False(otherFocusedAfterItemClick);
        Assert.Equal(0, otherClicks - clicksBefore);
    }

    // A row whose Focusable is false cannot take the focus, and it owns the press, so no focusable element above it,
    // such as a focusable ListBox, takes the focus either: the focus stays where it was (#640, #662).
    [Fact]
    public void PressOnNonFocusableListBoxItemLeavesFocusWhereItWas()
    {
        Button other = new() { Content = "other", Width = 100, Height = 40 };
        ListBox listBox = new() { Focusable = true, Width = 150, Height = 100 };
        ListBoxItem first = new() { Content = "a", Focusable = false };
        listBox.Items.Add(first);
        listBox.Items.Add(new ListBoxItem { Content = "b" });
        UIRoot root = CreateFocusWindowRoot(other, listBox);
        ElementInputBridge bridge = new();
        ClickCenter(root, bridge, other);
        Assert.True(other.IsKeyboardFocused);

        ClickCenter(root, bridge, first);

        Assert.True(other.IsKeyboardFocused);
        Assert.False(first.IsKeyboardFocused);
        Assert.False(listBox.IsKeyboardFocusWithin);
    }

    // As WPF's ListBoxItem selects itself on a press only when its Focus() succeeds (#675): a click on a row whose
    // Focusable is false leaves the selection as it was, and a click on a focusable row selects and focuses it.
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ClickSelectsListBoxItemOnlyWhenItCanTakeFocus(bool focusable)
    {
        Button other = new() { Content = "other", Width = 100, Height = 40 };
        ListBox listBox = new() { Width = 150, Height = 100 };
        ListBoxItem first = new() { Content = "a", Focusable = focusable };
        ListBoxItem second = new() { Content = "b" };
        listBox.Items.Add(first);
        listBox.Items.Add(second);
        listBox.SelectedIndex = 1;
        int selectionChanges = 0;
        listBox.SelectionChanged += (_, _) => selectionChanges++;
        UIRoot root = CreateFocusWindowRoot(other, listBox);
        ElementInputBridge bridge = new();
        ClickCenter(root, bridge, other);
        Assert.True(other.IsKeyboardFocused);

        ClickCenter(root, bridge, first);

        Assert.Equal(focusable ? 0 : 1, listBox.SelectedIndex);
        Assert.Equal(focusable, first.IsSelected);
        Assert.Equal(!focusable, second.IsSelected);
        Assert.Equal(focusable ? 1 : 0, selectionChanges);
        Assert.Equal(focusable, first.IsKeyboardFocused);
        Assert.Equal(!focusable, other.IsKeyboardFocused);
    }

    // WPF's ListBoxItem selects itself when the Focus() of its press succeeds, so a row whose press focused it is
    // selected, also when a GotFocus handler then makes it non-focusable before the release.
    [Fact]
    public void ClickSelectsListBoxItemWhosePressFocusedItAlthoughItIsNoLongerFocusableAtTheRelease()
    {
        Button other = new() { Content = "other", Width = 100, Height = 40 };
        ListBox listBox = new() { Width = 150, Height = 100 };
        ListBoxItem first = new() { Content = "a" };
        listBox.Items.Add(first);
        listBox.Items.Add(new ListBoxItem { Content = "b" });
        bool focusedByPress = false;
        first.GotFocus += (_, _) =>
        {
            focusedByPress = true;
            first.Focusable = false;
        };
        UIRoot root = CreateFocusWindowRoot(other, listBox);
        ElementInputBridge bridge = new();
        ClickCenter(root, bridge, other);

        ClickCenter(root, bridge, first);

        Assert.True(focusedByPress);
        Assert.False(first.Focusable);
        Assert.Equal(0, listBox.SelectedIndex);
        Assert.True(first.IsSelected);
    }

    // A WPF ComboBoxItem does not focus itself on a press, and the ComboBox keeps the keyboard focus while its drop-down
    // is open, so a ComboBoxItem stays non-focusable although a ListBoxItem is focusable (#662). As a ListBox moves
    // the Tab key into its list only once in WPF, a row is no Tab stop.
    [Fact]
    public void ListBoxItemIsFocusableAndComboBoxItemIsNot()
    {
        Assert.True(new ListBoxItem().Focusable);
        Assert.False(new ListBoxItem().IsTabStop);
        Assert.False(new ComboBoxItem().Focusable);
    }

    // As in WPF, Down on the focused row of a single-selection ListBox focuses and selects the next row (#680).
    [Fact]
    public void DownKeyAfterClickSelectsNextRow()
    {
        (UIRoot root, ListBox listBox, ElementInputBridge bridge) = CreateFruitListBoxWithClickedApple();

        PressKey(root, bridge, InputKey.Down);

        Assert.Equal(1, listBox.SelectedIndex);
    }

    // As in WPF, Up on the focused row of a single-selection ListBox focuses and selects the previous row (#680).
    [Fact]
    public void UpKeyAfterDownSelectsPreviousRow()
    {
        (UIRoot root, ListBox listBox, ElementInputBridge bridge) = CreateFruitListBoxWithClickedApple();
        PressKey(root, bridge, InputKey.Down);
        Assert.Equal(1, listBox.SelectedIndex);

        PressKey(root, bridge, InputKey.Up);

        Assert.Equal(0, listBox.SelectedIndex);
    }

    // As in WPF, End selects the last row and Home the first row of a single-selection ListBox (#680).
    [Fact]
    public void EndKeySelectsLastRowAndHomeKeySelectsFirstRow()
    {
        (UIRoot root, ListBox listBox, ElementInputBridge bridge) = CreateFruitListBoxWithClickedApple();

        PressKey(root, bridge, InputKey.End);
        Assert.Equal(2, listBox.SelectedIndex);

        PressKey(root, bridge, InputKey.Home);
        Assert.Equal(0, listBox.SelectedIndex);
    }

    // As WPF's TextSearch on a ListBox, typing a letter on the focused row selects the first row whose text starts
    // with it (#680).
    [Fact]
    public void TypedLetterSelectsFirstRowStartingWithIt()
    {
        (UIRoot root, ListBox listBox, ElementInputBridge bridge) = CreateFruitListBoxWithClickedApple();

        bridge.Dispatch(root, new InputFrame(
            PointerSnapshot.Empty,
            PointerSnapshot.Empty,
            KeyboardSnapshot.Empty,
            KeyboardSnapshot.Empty,
            [new TextInputSnapshotEvent("c")]));
        root.ProcessFrame();

        Assert.Equal(2, listBox.SelectedIndex);
    }

    [Fact]
    public void ListBoxSelectionModeIsSingleByDefault()
    {
        Assert.Equal(SelectionMode.Single, new ListBox().SelectionMode);
    }

    // As WPF's ListBox in Single mode, Ctrl+click on the selected row deselects it, and Ctrl+click on another row
    // selects that row only.
    [Fact]
    public void SingleSelectionModeCtrlClickOnSelectedRowDeselectsIt()
    {
        (UIRoot root, ListBox listBox) = CreateSelectionModeListBox(SelectionMode.Single, "Apple", "Banana", "Cherry");
        ElementInputBridge bridge = new();
        ClickRow(root, bridge, listBox, 1);

        ClickRow(root, bridge, listBox, 1, InputKey.LeftCtrl);

        Assert.Equal(-1, listBox.SelectedIndex);
        Assert.Equal([false, false, false], RowSelection(listBox));

        ClickRow(root, bridge, listBox, 2, InputKey.LeftCtrl);

        Assert.Equal(["Cherry"], listBox.SelectedItems);
    }

    // SelectAll keeps a linear cost: every item of a large ListBox is selected.
    [Fact]
    public void SelectAllSelectsEveryItemOfLargeListBox()
    {
        ListBox listBox = new() { SelectionMode = SelectionMode.Extended };
        listBox.SetItems(Enumerable.Range(0, 100_000).Select(index => (object)index).ToArray());

        listBox.SelectAll();

        Assert.Equal(100_000, listBox.SelectionModel.SelectedIndices.Count);
        Assert.Equal(0, listBox.SelectedIndex);
        Assert.True(listBox.SelectionModel.IsSelected(99_999));
    }

    // As in WPF, a click in a ListBox whose SelectionMode is Multiple selects or deselects the row and keeps the other
    // selected rows.
    [Fact]
    public void MultipleSelectionModeClickTogglesRow()
    {
        (UIRoot root, ListBox listBox) = CreateSelectionModeListBox(SelectionMode.Multiple, "Apple", "Banana", "Cherry");
        ElementInputBridge bridge = new();

        ClickRow(root, bridge, listBox, 0);
        ClickRow(root, bridge, listBox, 2);

        Assert.Equal(["Apple", "Cherry"], listBox.SelectedItems);
        Assert.Equal(0, listBox.SelectedIndex);
        Assert.Equal([true, false, true], RowSelection(listBox));

        ClickRow(root, bridge, listBox, 0);

        Assert.Equal(["Cherry"], listBox.SelectedItems);
        Assert.Equal(2, listBox.SelectedIndex);
        Assert.Equal([false, false, true], RowSelection(listBox));
    }

    // As in WPF, in a ListBox whose SelectionMode is Extended, Shift+click selects the rows from the anchor to the
    // clicked row, Ctrl+click selects or deselects one row, and a plain click selects only the clicked row.
    [Fact]
    public void ExtendedSelectionModeClicksFollowShiftAndCtrl()
    {
        (UIRoot root, ListBox listBox) = CreateSelectionModeListBox(SelectionMode.Extended, "Apple", "Banana", "Cherry");
        ElementInputBridge bridge = new();

        ClickRow(root, bridge, listBox, 0);
        ClickRow(root, bridge, listBox, 2, InputKey.LeftShift);

        Assert.Equal(["Apple", "Banana", "Cherry"], listBox.SelectedItems);
        Assert.Equal([true, true, true], RowSelection(listBox));

        ClickRow(root, bridge, listBox, 1, InputKey.LeftCtrl);

        Assert.Equal(["Apple", "Cherry"], listBox.SelectedItems);
        Assert.Equal([true, false, true], RowSelection(listBox));

        ClickRow(root, bridge, listBox, 1);

        Assert.Equal(["Banana"], listBox.SelectedItems);
        Assert.Equal(1, listBox.SelectedIndex);
        Assert.Equal([false, true, false], RowSelection(listBox));
    }

    // As in WPF, Ctrl+click moves the anchor to the clicked row, and Ctrl+Shift+click adds the rows from the anchor to
    // the clicked row to the selection, where Shift+click alone would replace it.
    [Fact]
    public void ExtendedSelectionModeCtrlShiftClickAddsRangeFromAnchor()
    {
        (UIRoot root, ListBox listBox) = CreateSelectionModeListBox(SelectionMode.Extended, "A", "B", "C", "D", "E");
        ElementInputBridge bridge = new();

        ClickRow(root, bridge, listBox, 0);
        ClickRow(root, bridge, listBox, 3, InputKey.LeftCtrl);
        ClickRow(root, bridge, listBox, 4, InputKey.LeftCtrl, InputKey.LeftShift);

        Assert.Equal(["A", "D", "E"], listBox.SelectedItems);
        Assert.Equal([true, false, false, true, true], RowSelection(listBox));
    }

    // As in WPF, one click that changes several rows raises one SelectionChanged with every added and removed item.
    [Fact]
    public void ExtendedSelectionClickRaisesOneSelectionChangedWithEveryChangedItem()
    {
        (UIRoot root, ListBox listBox) = CreateSelectionModeListBox(SelectionMode.Extended, "Apple", "Banana", "Cherry");
        ElementInputBridge bridge = new();
        ClickRow(root, bridge, listBox, 0);
        List<SelectionChangedEventArgs> raised = [];
        listBox.SelectionChanged += (_, args) => raised.Add(args);

        ClickRow(root, bridge, listBox, 2, InputKey.RightShift);

        SelectionChangedEventArgs range = Assert.Single(raised);
        Assert.Equal(["Banana", "Cherry"], range.AddedItems);
        Assert.Empty(range.RemovedItems);
        raised.Clear();

        ClickRow(root, bridge, listBox, 1);

        SelectionChangedEventArgs single = Assert.Single(raised);
        Assert.Empty(single.AddedItems);
        Assert.Equal(["Apple", "Cherry"], single.RemovedItems);
    }

    // As in WPF, SelectAll selects every item of a ListBox whose SelectionMode is not Single, and UnselectAll
    // deselects them.
    [Fact]
    public void SelectAllSelectsEveryItemAndUnselectAllDeselectsThem()
    {
        (UIRoot root, ListBox listBox) = CreateSelectionModeListBox(SelectionMode.Multiple, "Apple", "Banana", "Cherry");

        listBox.SelectAll();
        root.ProcessFrame();

        Assert.Equal(["Apple", "Banana", "Cherry"], listBox.SelectedItems);
        Assert.Equal([true, true, true], RowSelection(listBox));

        listBox.UnselectAll();
        root.ProcessFrame();

        Assert.Empty(listBox.SelectedItems);
        Assert.Equal(-1, listBox.SelectedIndex);
        Assert.Equal([false, false, false], RowSelection(listBox));
    }

    // As WPF's ListBox.SelectAll, which throws NotSupportedException when SelectionMode is Single.
    [Fact]
    public void SelectAllThrowsInSingleSelectionMode()
    {
        ListBox listBox = new();
        listBox.SetItems(new[] { "Apple", "Banana" });
        listBox.SelectedIndex = 1;

        Assert.Throws<NotSupportedException>(listBox.SelectAll);
        Assert.Equal(["Banana"], listBox.SelectedItems);

        listBox.UnselectAll();

        Assert.Equal(-1, listBox.SelectedIndex);
    }

    // As in WPF, a change of SelectionMode to Single keeps only the first selected item.
    [Fact]
    public void SelectionModeChangeToSingleKeepsFirstSelectedItem()
    {
        (UIRoot root, ListBox listBox) = CreateSelectionModeListBox(SelectionMode.Multiple, "Apple", "Banana", "Cherry");
        ElementInputBridge bridge = new();
        ClickRow(root, bridge, listBox, 2);
        ClickRow(root, bridge, listBox, 0);
        Assert.Equal(["Cherry", "Apple"], listBox.SelectedItems);

        listBox.SelectionMode = SelectionMode.Single;
        root.ProcessFrame();

        Assert.Equal(["Cherry"], listBox.SelectedItems);
        Assert.Equal(2, listBox.SelectedIndex);
        Assert.Equal([false, false, true], RowSelection(listBox));
    }

    // As in WPF, removing a selected item deselects it and reports it in RemovedItems; the other selected items stay
    // selected at their new indices.
    [Fact]
    public void RemovingSelectedItemOfMultipleSelectionKeepsTheOtherSelectedItems()
    {
        (UIRoot root, ListBox listBox) = CreateSelectionModeListBox(SelectionMode.Multiple, "Apple", "Banana", "Cherry");
        ElementInputBridge bridge = new();
        ClickRow(root, bridge, listBox, 0);
        ClickRow(root, bridge, listBox, 1);
        ClickRow(root, bridge, listBox, 2);
        List<SelectionChangedEventArgs> raised = [];
        listBox.SelectionChanged += (_, args) => raised.Add(args);

        listBox.Items.RemoveAt(1);
        root.ProcessFrame();

        SelectionChangedEventArgs change = Assert.Single(raised);
        Assert.Equal(["Banana"], change.RemovedItems);
        Assert.Empty(change.AddedItems);
        Assert.Equal(["Apple", "Cherry"], listBox.SelectedItems);
        Assert.Equal([0, 1], listBox.SelectionModel.SelectedIndices);
        Assert.Equal([true, true], RowSelection(listBox));
    }

    // As the WPF ListBox template, the default template is a Border around a ScrollViewer around the items presenter,
    // and the rows are in that ScrollViewer (#680).
    [Fact]
    public void DefaultTemplateHostsTheRowsInAScrollViewerInsideABorder()
    {
        (UIRoot _, ListBox listBox) = CreateRowListBox(height: 100, rowCount: 3);

        ComponentTemplateInstance instance = Assert.IsType<ComponentTemplateInstance>(listBox.ComponentTemplateInstance);
        Border border = Assert.IsType<Border>(instance.Root);
        ScrollViewer scrollViewer = Assert.IsType<ScrollViewer>(border.Child);
        Assert.Same(scrollViewer, instance.Parts["PART_ScrollViewer"]);
        ItemsPresenter presenter = Assert.IsType<ItemsPresenter>(scrollViewer.Content);
        Assert.Same(presenter, instance.Parts["PART_ItemsPresenter"]);
        Assert.Same(presenter, listBox.ItemsPresenter);
        Assert.True(IsInside(Row(listBox, 0), scrollViewer));
        // The three rows fit, so no scroll bar shows and each row stretches over the whole width.
        Assert.False(scrollViewer.IsVerticalScrollBarVisible);
        Assert.False(scrollViewer.IsHorizontalScrollBarVisible);
        Assert.Equal(150, Row(listBox, 2).ArrangedBounds.Width);
    }

    // With ComponentTemplate set to null, the ListBox lays out its rows itself, inside BorderThickness and Padding,
    // without a ScrollViewer.
    [Fact]
    public void ListBoxWithoutComponentTemplateLaysOutItsRowsWithoutScrollViewer()
    {
        ListBoxItem row = new() { Content = "One" };
        ListBox listBox = new() { ComponentTemplate = null, BorderThickness = new Thickness(2), Padding = new Thickness(10) };
        listBox.Items.Add(row);
        UIRoot root = new();
        root.VisualChildren.Add(listBox);
        root.ProcessFrame();
        listBox.Arrange(new ArrangeContext(new LayoutRect(3, 4, 40, 30)));
        root.ProcessFrame();

        Assert.Null(listBox.ComponentTemplateInstance);
        Assert.False(HasAncestor<ScrollViewer>(row));
        Assert.Equal(new LayoutRect(15, 16, 16, row.DesiredSize.Height), row.ArrangedBounds);
    }

    // A ListBox inside an outer ScrollViewer realizes only the rows in that ScrollViewer's viewport, as an ItemsControl
    // does. The rows start below the top Padding, so the viewport reaches the rows that much later (#680).
    [Theory]
    [InlineData(0, 4)]
    [InlineData(20, 2)]
    public void ListBoxInsideAnOuterScrollViewerRealizesOnlyTheVisibleRows(float topPadding, int firstVisibleRow)
    {
        ListBox listBox = new()
        {
            Padding = new Thickness(0, topPadding, 0, 0),
            ItemsPanel = new Cerneala.UI.Layout.Panels.VirtualizingStackPanel(),
            ItemContainerAspect = RowHeight(10),
            ItemsSource = Enumerable.Range(0, 100).Select(index => index.ToString()).ToArray()
        };
        ScrollViewer viewer = new() { Content = listBox, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
        UIRoot root = new(100, 30);
        root.VisualChildren.Add(viewer);
        root.ProcessFrame();

        Assert.InRange(listBox.ItemContainerGenerator.RealizedContainers.Count, 1, 8);
        Assert.Contains(0, listBox.ItemContainerGenerator.RealizedContainers.Keys);

        viewer.ScrollInfo.SetVerticalOffset(45);
        root.ProcessFrame();

        Assert.InRange(listBox.ItemContainerGenerator.RealizedContainers.Count, 1, 8);
        Assert.DoesNotContain(0, listBox.ItemContainerGenerator.RealizedContainers.Keys);
        Assert.Contains(firstVisibleRow, listBox.ItemContainerGenerator.RealizedContainers.Keys);
    }

    // As in WPF, where the panel of the ItemsPresenter in the template's ScrollViewer gets that ScrollViewer's
    // viewport, a ListBox 100 pixels high with a VirtualizingStackPanel realizes only the rows its own ScrollViewer
    // shows, before and after End scrolls to the last of its 10,000 rows (#702).
    [Fact]
    public void FixedHeightListBoxVirtualizesThroughTheScrollViewerOfItsTemplate()
    {
        (UIRoot root, ListBox listBox) = CreateVirtualizingWindowListBox();

        Assert.InRange(listBox.ItemContainerGenerator.RealizedContainers.Count, 1, 20);
        Assert.Contains(0, listBox.ItemContainerGenerator.RealizedContainers.Keys);

        ElementInputBridge bridge = new();
        ClickCenter(root, bridge, Row(listBox, 0));
        PressKey(root, bridge, InputKey.End);

        Assert.Equal(9999, listBox.SelectedIndex);
        Assert.Contains(9999, listBox.ItemContainerGenerator.RealizedContainers.Keys);
        Assert.InRange(listBox.ItemContainerGenerator.RealizedContainers.Count, 1, 20);
    }

    // As in WPF, where an unbounded viewport realizes every item, a ListBox in a StackPanel that loses its fixed height,
    // or whose ScrollViewer stops scrolling vertically, gets no viewport from its own ScrollViewer any more, and no
    // outer ScrollViewer gives one, so its VirtualizingStackPanel realizes all 100 rows, not the window of the old
    // 100-pixel viewport (#702).
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ListBoxWhoseScrollViewerStopsGivingAViewportRealizesEveryRow(bool disableVerticalScrolling)
    {
        ListBox listBox = new()
        {
            Width = 150,
            Height = 100,
            ItemsPanel = new Cerneala.UI.Layout.Panels.VirtualizingStackPanel(),
            ItemContainerAspect = RowHeight(10),
            ItemsSource = Enumerable.Range(0, 100).Select(index => index.ToString()).ToArray()
        };
        Cerneala.UI.Layout.Panels.StackPanel stack = new();
        stack.VisualChildren.Add(listBox);
        UIRoot root = new(360, 300);
        root.VisualChildren.Add(new Window { Content = stack, Width = 360, Height = 300 });
        root.ProcessFrame();
        Assert.InRange(listBox.ItemContainerGenerator.RealizedContainers.Count, 1, 20);

        if (disableVerticalScrolling)
        {
            ListBoxScrollViewer(listBox).VerticalScrollBarVisibility = ScrollBarVisibility.Disabled;
        }
        else
        {
            listBox.Height = float.NaN;
        }

        root.ProcessFrame();
        root.ProcessFrame();

        Assert.Equal(100, listBox.ItemContainerGenerator.RealizedContainers.Count);
    }

    // A panel that virtualizes only through the public IItemsVirtualizingPanel contract loses the viewport of the
    // ListBox's own ScrollViewer like a VirtualizingStackPanel when the ListBox in a StackPanel loses its fixed height:
    // all 100 rows are realized, not the rows of the old 100-pixel viewport (#702).
    [Fact]
    public void ListBoxWithACustomVirtualizingPanelRealizesEveryRowWhenItLosesItsFixedHeight()
    {
        FixedRowVirtualizingPanel panel = new();
        ListBox listBox = new()
        {
            Width = 150,
            Height = 100,
            ItemsPanel = panel,
            ItemContainerAspect = RowHeight(10),
            ItemsSource = Enumerable.Range(0, 100).Select(index => index.ToString()).ToArray()
        };
        Cerneala.UI.Layout.Panels.StackPanel stack = new();
        stack.VisualChildren.Add(listBox);
        UIRoot root = new(360, 300);
        root.VisualChildren.Add(new Window { Content = stack, Width = 360, Height = 300 });
        root.ProcessFrame();
        Assert.True(panel.ViewportUpdates > 0);
        Assert.InRange(listBox.ItemContainerGenerator.RealizedContainers.Count, 1, 20);

        listBox.Height = float.NaN;
        root.ProcessFrame();
        root.ProcessFrame();

        Assert.Equal(100, listBox.ItemContainerGenerator.RealizedContainers.Count);
    }

    // A ListBox that loses its fixed Height is as high as its realized rows, also when they differ from the panel's estimate (#705).
    [Fact]
    public void ListBoxThatLosesItsFixedHeightGrowsToItsRealizedRows()
    {
        ListBox listBox = new()
        {
            Width = 150,
            Height = 100,
            ItemsPanel = new Cerneala.UI.Layout.Panels.VirtualizingStackPanel(),
            ItemsSource = Enumerable.Range(0, 100)
                .Select(index => new ListBoxItem { Height = index < 11 ? 10 : 20, Content = index.ToString() })
                .ToArray()
        };
        Cerneala.UI.Layout.Panels.StackPanel stack = new();
        stack.VisualChildren.Add(listBox);
        UIRoot root = new(360, 300);
        root.VisualChildren.Add(new Window { Content = stack, Width = 360, Height = 300 });
        root.ProcessFrame();
        Assert.Equal(11, listBox.ItemContainerGenerator.RealizedContainers.Count);

        listBox.Height = float.NaN;
        for (int frame = 0; frame < 5 && root.Scheduler.HasWork; frame++)
        {
            root.ProcessFrame();
        }

        Assert.False(root.Scheduler.HasWork);
        Assert.Equal(100, listBox.ItemContainerGenerator.RealizedContainers.Count);
        Assert.Equal(1890, listBox.ArrangedBounds.Height);
    }

    // A ListBox 100 pixels high inside an outer ScrollViewer has a finite viewport of its own, so its own ScrollViewer,
    // not the outer one, decides which rows are realized: after End and a scroll of the outer viewer, the last row
    // stays realized and only a few rows are (#702).
    [Fact]
    public void FixedHeightListBoxInsideAnOuterScrollViewerVirtualizesThroughItsOwnScrollViewer()
    {
        ListBox listBox = CreateVirtualizingRowListBox();
        Cerneala.UI.Layout.Panels.StackPanel stack = new();
        stack.VisualChildren.Add(new Border { Height = 150 });
        stack.VisualChildren.Add(listBox);
        stack.VisualChildren.Add(new Border { Height = 200 });
        ScrollViewer outer = new() { Content = stack, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
        UIRoot root = new(360, 300);
        root.VisualChildren.Add(new Window { Content = outer, Width = 360, Height = 300 });
        root.ProcessFrame();

        Assert.InRange(listBox.ItemContainerGenerator.RealizedContainers.Count, 1, 20);

        ElementInputBridge bridge = new();
        ClickCenter(root, bridge, Row(listBox, 0));
        PressKey(root, bridge, InputKey.End);
        Assert.Contains(9999, listBox.ItemContainerGenerator.RealizedContainers.Keys);

        outer.ScrollInfo.SetVerticalOffset(100);
        root.ProcessFrame();

        Assert.Equal(100, outer.Presenter.VerticalOffset);
        Assert.Equal(9999, listBox.SelectedIndex);
        Assert.Contains(9999, listBox.ItemContainerGenerator.RealizedContainers.Keys);
        Assert.InRange(listBox.ItemContainerGenerator.RealizedContainers.Count, 1, 20);
    }

    // A ListBox without a fixed height inside an outer ScrollViewer 300 pixels high realizes the rows of that outer
    // viewport, so End scrolls the outer viewer until the last of its 10,000 rows is visible, and focuses it, as WPF's
    // BringIntoView reaches the outer ScrollViewer (#702).
    [Fact]
    public void EndInAListBoxWithoutFixedHeightScrollsTheOuterScrollViewerToTheLastRow()
    {
        (UIRoot root, ListBox listBox, ScrollViewer outer) = CreateOuterViewerListBox();
        ElementInputBridge bridge = new();
        ClickCenter(root, bridge, Row(listBox, 0));

        PressKey(root, bridge, InputKey.End);

        Assert.Equal(9999, listBox.SelectedIndex);
        Assert.Contains(9999, listBox.ItemContainerGenerator.RealizedContainers.Keys);
        Assert.True(Row(listBox, 9999).IsKeyboardFocused);
        AssertInsideViewport(outer, Row(listBox, 9999));
        Assert.InRange(listBox.ItemContainerGenerator.RealizedContainers.Count, 1, 40);
    }

    // ScrollIntoView of row 5000 of a ListBox without a fixed height inside an outer ScrollViewer 300 pixels high
    // scrolls the outer viewer until the row is visible, and realizes it (#702).
    [Fact]
    public void ScrollIntoViewInAListBoxWithoutFixedHeightScrollsTheOuterScrollViewerToTheRow()
    {
        (UIRoot root, ListBox listBox, ScrollViewer outer) = CreateOuterViewerListBox();
        Assert.DoesNotContain(5000, listBox.ItemContainerGenerator.RealizedContainers.Keys);

        listBox.ScrollIntoView("5000");
        root.ProcessFrame();

        Assert.Contains(5000, listBox.ItemContainerGenerator.RealizedContainers.Keys);
        AssertInsideViewport(outer, Row(listBox, 5000));
        Assert.InRange(listBox.ItemContainerGenerator.RealizedContainers.Count, 1, 40);
    }

    // The arrange that realizes the last row of that ListBox brings the row into view again, inside the outer
    // ScrollViewer's arrange of the ListBox, where the rows have their new place before the outer presenter has its new
    // bounds. The outer viewer scrolls once, straight to the offset that shows the row: 100,000 - 300 = 99,700 (#702).
    [Fact]
    public void ScrollIntoViewInAListBoxWithoutFixedHeightScrollsTheOuterScrollViewerOnce()
    {
        (UIRoot root, ListBox listBox, ScrollViewer outer) = CreateOuterViewerListBox();
        List<float> offsets = [];
        outer.Presenter.PropertyChanged += (_, args) =>
        {
            if (ReferenceEquals(args.Property, ScrollContentPresenter.VerticalOffsetProperty))
            {
                offsets.Add((float)args.NewValue!);
            }
        };

        listBox.ScrollIntoView("9999");
        root.ProcessFrame();
        root.ProcessFrame();

        Assert.Equal([99_700f], offsets);
    }

    // As WPF's ListBox, which brings a row without a container into view through VirtualizingStackPanel's
    // BringIndexIntoView, End and Home reach rows that are not realized: the row is realized, selected, focused and
    // inside the viewport, and only a few rows are realized (#702).
    [Fact]
    public void EndAndHomeReachRowsThatAreNotRealizedInAVirtualizedListBox()
    {
        (UIRoot root, ListBox listBox) = CreateVirtualizingWindowListBox();
        ElementInputBridge bridge = new();
        ClickCenter(root, bridge, Row(listBox, 0));

        PressKey(root, bridge, InputKey.End);

        AssertFocusedSelectedRow(listBox, 9999);
        Assert.InRange(listBox.ItemContainerGenerator.RealizedContainers.Count, 1, 20);

        PressKey(root, bridge, InputKey.Home);

        AssertFocusedSelectedRow(listBox, 0);
        Assert.InRange(listBox.ItemContainerGenerator.RealizedContainers.Count, 1, 20);
    }

    // As WPF's page navigation, the second PageDown moves one viewport further, to rows that are not realized yet: the
    // viewport shows about 10 rows of 10 pixels, so each PageDown moves the selection by about 10 rows (#702).
    [Fact]
    public void PageDownMovesByAViewportIntoRowsThatAreNotRealizedInAVirtualizedListBox()
    {
        (UIRoot root, ListBox listBox) = CreateVirtualizingWindowListBox();
        ElementInputBridge bridge = new();
        ClickCenter(root, bridge, Row(listBox, 0));

        PressKey(root, bridge, InputKey.PageDown);
        int first = listBox.SelectedIndex;
        AssertFocusedSelectedRow(listBox, first);
        Assert.InRange(first, 7, 10);

        PressKey(root, bridge, InputKey.PageDown);
        int second = listBox.SelectedIndex;
        AssertFocusedSelectedRow(listBox, second);
        Assert.InRange(second - first, 7, 11);
        Assert.InRange(listBox.ItemContainerGenerator.RealizedContainers.Count, 1, 20);
    }

    // ScrollIntoView of an item whose row is not realized scrolls the row into the viewport and realizes it (#702).
    [Fact]
    public void ScrollIntoViewReachesARowThatIsNotRealizedInAVirtualizedListBox()
    {
        (UIRoot root, ListBox listBox) = CreateVirtualizingWindowListBox();
        Assert.DoesNotContain(5000, listBox.ItemContainerGenerator.RealizedContainers.Keys);

        listBox.ScrollIntoView("5000");
        root.ProcessFrame();

        AssertRowInsideViewport(listBox, 5000);
        Assert.InRange(listBox.ItemContainerGenerator.RealizedContainers.Count, 1, 20);
    }

    // As WPF's ScrollIntoView, which is not input and works on a disabled list, ScrollIntoView scrolls the row into the
    // viewport of a ListBox 200 pixels high also when the ListBox, its parent, or the row itself is disabled, with a
    // VirtualizingStackPanel (row 5000 of 10,000) and with a StackPanel (row 90 of 100) (#702).
    [Theory]
    [InlineData(true, "ListBox")]
    [InlineData(false, "ListBox")]
    [InlineData(true, "Parent")]
    [InlineData(false, "Parent")]
    [InlineData(false, "Row")]
    public void ScrollIntoViewScrollsADisabledListBox(bool virtualizing, string disabled)
    {
        int count = virtualizing ? 10_000 : 100;
        int target = virtualizing ? 5000 : 90;
        ListBox listBox = new()
        {
            Width = 150,
            Height = 200,
            ItemsPanel = virtualizing
                ? new Cerneala.UI.Layout.Panels.VirtualizingStackPanel()
                : new Cerneala.UI.Layout.Panels.StackPanel(),
            ItemContainerAspect = RowHeight(10),
            ItemsSource = Enumerable.Range(0, count).Select(index => index.ToString()).ToArray()
        };
        Cerneala.UI.Layout.Panels.StackPanel parent = new();
        parent.VisualChildren.Add(listBox);
        UIRoot root = new(360, 300);
        root.VisualChildren.Add(new Window { Content = parent, Width = 360, Height = 300 });
        root.ProcessFrame();

        switch (disabled)
        {
            case "ListBox": listBox.IsEnabled = false; break;
            case "Parent": parent.IsEnabled = false; break;
            default: Row(listBox, target).IsEnabled = false; break;
        }

        root.ProcessFrame();
        listBox.ScrollIntoView(target.ToString());
        root.ProcessFrame();
        root.ProcessFrame();

        Assert.True(ListBoxScrollViewer(listBox).Presenter.VerticalOffset > 0);
        AssertRowInsideViewport(listBox, target);
    }

    // The same for a disabled realized row of a VirtualizingStackPanel: with the template's ScrollViewer at offset 155,
    // row 15 (150 to 160) is cut at the top, and ScrollIntoView scrolls back until its top is at the top, offset 150 (#702).
    [Fact]
    public void ScrollIntoViewScrollsToADisabledRowOfAVirtualizingListBox()
    {
        ListBox listBox = new()
        {
            Width = 150,
            Height = 200,
            ItemsPanel = new Cerneala.UI.Layout.Panels.VirtualizingStackPanel(),
            ItemContainerAspect = RowHeight(10),
            ItemsSource = Enumerable.Range(0, 10_000).Select(index => index.ToString()).ToArray()
        };
        UIRoot root = new(360, 300);
        root.VisualChildren.Add(new Window { Content = listBox, Width = 360, Height = 300 });
        root.ProcessFrame();
        ScrollContentPresenter presenter = ListBoxScrollViewer(listBox).Presenter;
        presenter.SetVerticalOffset(155);
        root.ProcessFrame();
        Row(listBox, 15).IsEnabled = false;
        root.ProcessFrame();

        listBox.ScrollIntoView("15");
        root.ProcessFrame();
        root.ProcessFrame();

        Assert.Equal(150, presenter.VerticalOffset);
        AssertRowInsideViewport(listBox, 15);
    }

    // As WPF's BringIndexIntoView, which keeps scrolling until the item is in view, End and ScrollIntoView bring a row
    // that is not realized fully into the viewport also when the rows are 10 and 40 pixels high in turn, so that the
    // panel's estimate changes with the rows it measures at the new place (#702).
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void EndAndScrollIntoViewBringAnUnrealizedRowIntoViewWhenTheRowHeightsDiffer(bool endKey)
    {
        ListBox listBox = new()
        {
            Width = 150,
            Height = 100,
            ItemsPanel = new Cerneala.UI.Layout.Panels.VirtualizingStackPanel(),
            ItemTemplate = new ContentTemplate<string>("row", key: null, priority: 0,
                context => new Border { Height = int.Parse(context.Data!) % 2 == 0 ? 10 : 40 }),
            ItemsSource = Enumerable.Range(0, 10_000).Select(index => index.ToString()).ToArray()
        };
        UIRoot root = new(360, 300);
        root.VisualChildren.Add(new Window { Content = listBox, Width = 360, Height = 300 });
        root.ProcessFrame();
        ElementInputBridge bridge = new();
        ClickCenter(root, bridge, Row(listBox, 0));

        if (endKey)
        {
            PressKey(root, bridge, InputKey.End);
            Assert.Contains(9999, listBox.ItemContainerGenerator.RealizedContainers.Keys);
            AssertFocusedSelectedRow(listBox, 9999);
        }
        else
        {
            listBox.ScrollIntoView("5001");
            root.ProcessFrame();
            Assert.Contains(5001, listBox.ItemContainerGenerator.RealizedContainers.Keys);
            AssertRowInsideViewport(listBox, 5001);
        }

        Assert.InRange(listBox.ItemContainerGenerator.RealizedContainers.Count, 1, 20);
    }

    // As WPF's ListBox.ScrollIntoView, a row below the viewport scrolls up until its bottom is at the bottom of the
    // viewport. The viewport is 100 pixels high and the rows 20, so row 15 ends at 320 and the offset becomes 220 (#680).
    [Fact]
    public void ScrollIntoViewBringsARowBelowTheViewportToTheViewportBottom()
    {
        (UIRoot root, ListBox listBox) = CreateRowListBox(height: 100, rowCount: 20);
        ScrollContentPresenter presenter = ListBoxScrollViewer(listBox).Presenter;
        Assert.Equal(0, presenter.VerticalOffset);

        listBox.ScrollIntoView("15");
        root.ProcessFrame();

        Assert.Equal(220, presenter.VerticalOffset);
        AssertRowInsideViewport(listBox, 15);
    }

    // As WPF's ListBox.ScrollIntoView, a row above the viewport scrolls down until its top is at the top of the
    // viewport (#680).
    [Fact]
    public void ScrollIntoViewBringsARowAboveTheViewportToTheViewportTop()
    {
        (UIRoot root, ListBox listBox) = CreateRowListBox(height: 100, rowCount: 20);
        listBox.ScrollIntoView("15");
        root.ProcessFrame();

        listBox.ScrollIntoView("2");
        root.ProcessFrame();

        Assert.Equal(40, ListBoxScrollViewer(listBox).Presenter.VerticalOffset);
        AssertRowInsideViewport(listBox, 2);
    }

    // A visible row and an item that the ListBox does not hold leave the scroll offset as it was (#680).
    [Fact]
    public void ScrollIntoViewOfAVisibleRowOrOfAMissingItemDoesNotScroll()
    {
        (UIRoot root, ListBox listBox) = CreateRowListBox(height: 100, rowCount: 20);

        listBox.ScrollIntoView("1");
        listBox.ScrollIntoView("4");
        listBox.ScrollIntoView("missing");
        listBox.ScrollIntoView(null);
        root.ProcessFrame();

        Assert.Equal(0, ListBoxScrollViewer(listBox).Presenter.VerticalOffset);
    }

    // Before the first layout the rows have no position yet, so the ListBox scrolls the row into view after its next
    // arrange (#680).
    [Fact]
    public void ScrollIntoViewBeforeTheFirstLayoutScrollsAfterIt()
    {
        (UIRoot root, ListBox listBox) = CreateRowListBox(height: 100, rowCount: 20, layout: false);

        listBox.ScrollIntoView("15");
        root.ProcessFrame();

        Assert.Equal(220, ListBoxScrollViewer(listBox).Presenter.VerticalOffset);
        AssertRowInsideViewport(listBox, 15);
    }

    // As in WPF, a click on the increase button of the ListBox scroll bar scrolls it and leaves the keyboard focus on
    // the focused row (#700).
    [Fact]
    public void ClickOnTheScrollBarIncreaseButtonLeavesTheFocusOnTheRow()
    {
        (UIRoot root, ListBox listBox) = CreateRowListBox(height: 100, rowCount: 20);
        ElementInputBridge bridge = new();
        ListBoxItem first = Row(listBox, 0);
        ClickCenter(root, bridge, first);
        Assert.True(first.IsKeyboardFocused);
        ScrollViewer scrollViewer = ListBoxScrollViewer(listBox);
        Assert.True(scrollViewer.IsVerticalScrollBarVisible);
        Cerneala.UI.Controls.Primitives.RepeatButton increase = Assert.IsType<Cerneala.UI.Controls.Primitives.RepeatButton>(
            scrollViewer.VerticalScrollBar.ComponentTemplateInstance!.Parts["PART_IncreaseButton"]);

        ClickCenter(root, bridge, increase);

        Assert.True(scrollViewer.Presenter.VerticalOffset > 0);
        Assert.True(first.IsKeyboardFocused);
    }

    // As in WPF, Down on the last visible row focuses and selects the next row and scrolls it into view (#680).
    [Fact]
    public void DownKeyOnTheLastVisibleRowScrollsTheNextRowIntoView()
    {
        (UIRoot root, ListBox listBox) = CreateRowListBox(height: 80, rowCount: 20);
        ElementInputBridge bridge = new();
        ClickCenter(root, bridge, Row(listBox, 3));

        PressKey(root, bridge, InputKey.Down);

        Assert.Equal(20, ListBoxScrollViewer(listBox).Presenter.VerticalOffset);
        AssertFocusedSelectedRow(listBox, 4);
    }

    // As in WPF, PageDown moves to the last fully visible row, and from there one page further; PageUp mirrors it. The
    // viewport shows exactly four rows of 20 pixels (#680).
    [Fact]
    public void PageDownAndPageUpMoveByTheRowsOfOneViewport()
    {
        (UIRoot root, ListBox listBox) = CreateRowListBox(height: 80, rowCount: 20);
        ElementInputBridge bridge = new();
        ClickCenter(root, bridge, Row(listBox, 0));

        PressKey(root, bridge, InputKey.PageDown);
        AssertFocusedSelectedRow(listBox, 3);

        PressKey(root, bridge, InputKey.PageDown);
        AssertFocusedSelectedRow(listBox, 7);

        PressKey(root, bridge, InputKey.PageUp);
        AssertFocusedSelectedRow(listBox, 4);

        PressKey(root, bridge, InputKey.PageUp);
        AssertFocusedSelectedRow(listBox, 0);
    }

    // As in WPF's Extended mode, Shift+Down and Shift+Up select the rows from the anchor to the focused row, and Down
    // without a modifier selects only the next row (#680).
    [Fact]
    public void ExtendedSelectionModeShiftArrowsSelectFromTheAnchorAndDownSelectsOnlyTheNextRow()
    {
        (UIRoot root, ListBox listBox) = CreateSelectionModeListBox(SelectionMode.Extended, "A", "B", "C", "D", "E");
        ElementInputBridge bridge = new();
        ClickRow(root, bridge, listBox, 1);

        PressKey(root, bridge, InputKey.Down, InputKey.LeftShift);
        PressKey(root, bridge, InputKey.Down, InputKey.LeftShift);
        Assert.Equal([1, 2, 3], listBox.SelectionModel.SelectedIndices);
        Assert.True(Row(listBox, 3).IsKeyboardFocused);

        PressKey(root, bridge, InputKey.Up, InputKey.LeftShift);
        Assert.Equal([1, 2], listBox.SelectionModel.SelectedIndices);

        PressKey(root, bridge, InputKey.Down);
        Assert.Equal([3], listBox.SelectionModel.SelectedIndices);
        Assert.Equal([false, false, false, true, false], RowSelection(listBox));
        Assert.True(Row(listBox, 3).IsKeyboardFocused);
    }

    // As WPF's line navigation, Down on the last row finds no next row and changes nothing: the rows that Shift+click
    // selected stay selected (#680).
    [Fact]
    public void DownKeyOnTheLastRowKeepsTheSelection()
    {
        (UIRoot root, ListBox listBox) = CreateSelectionModeListBox(SelectionMode.Extended, "A", "B", "C");
        ElementInputBridge bridge = new();
        ClickRow(root, bridge, listBox, 0);
        ClickRow(root, bridge, listBox, 2, InputKey.LeftShift);
        Assert.Equal([0, 1, 2], listBox.SelectionModel.SelectedIndices);
        Assert.True(Row(listBox, 2).IsKeyboardFocused);

        PressKey(root, bridge, InputKey.Down);

        Assert.Equal([0, 1, 2], listBox.SelectionModel.SelectedIndices);
        Assert.True(Row(listBox, 2).IsKeyboardFocused);
    }

    // As in WPF's Extended mode, Ctrl+Down moves only the focus, and Ctrl+Space toggles the focused row (#680).
    [Fact]
    public void ExtendedSelectionModeCtrlDownMovesOnlyTheFocusAndCtrlSpaceTogglesTheFocusedRow()
    {
        (UIRoot root, ListBox listBox) = CreateSelectionModeListBox(SelectionMode.Extended, "A", "B", "C");
        ElementInputBridge bridge = new();
        ClickRow(root, bridge, listBox, 0);

        PressKey(root, bridge, InputKey.Down, InputKey.LeftCtrl);
        Assert.Equal([0], listBox.SelectionModel.SelectedIndices);
        Assert.True(Row(listBox, 1).IsKeyboardFocused);

        PressKey(root, bridge, InputKey.Space, InputKey.LeftCtrl);
        Assert.Equal([0, 1], listBox.SelectionModel.SelectedIndices);

        PressKey(root, bridge, InputKey.Space, InputKey.LeftCtrl);
        Assert.Equal([0], listBox.SelectionModel.SelectedIndices);
    }

    // As in WPF's Multiple mode, Down moves only the focus, and Space toggles the focused row (#680).
    [Fact]
    public void MultipleSelectionModeDownMovesOnlyTheFocusAndSpaceTogglesTheFocusedRow()
    {
        (UIRoot root, ListBox listBox) = CreateSelectionModeListBox(SelectionMode.Multiple, "A", "B", "C");
        ElementInputBridge bridge = new();
        ClickRow(root, bridge, listBox, 0);
        Assert.Equal([0], listBox.SelectionModel.SelectedIndices);

        PressKey(root, bridge, InputKey.Down);
        Assert.Equal([0], listBox.SelectionModel.SelectedIndices);
        Assert.True(Row(listBox, 1).IsKeyboardFocused);

        PressKey(root, bridge, InputKey.Space);
        Assert.Equal([0, 1], listBox.SelectionModel.SelectedIndices);

        PressKey(root, bridge, InputKey.Space);
        Assert.Equal([0], listBox.SelectionModel.SelectedIndices);
    }

    // As in WPF, Ctrl+A selects every row in Multiple and Extended mode (#680).
    [Theory]
    [InlineData(SelectionMode.Multiple)]
    [InlineData(SelectionMode.Extended)]
    public void CtrlASelectsEveryRow(SelectionMode mode)
    {
        (UIRoot root, ListBox listBox) = CreateSelectionModeListBox(mode, "A", "B", "C");
        ElementInputBridge bridge = new();
        ClickRow(root, bridge, listBox, 0);

        PressKey(root, bridge, InputKey.A, InputKey.LeftCtrl);

        Assert.Equal([true, true, true], RowSelection(listBox));
        Assert.Equal(["A", "B", "C"], listBox.SelectedItems);
    }

    // SelectAll is not supported in Single mode, so Ctrl+A leaves the selection as it was (#680).
    [Fact]
    public void CtrlAInSingleSelectionModeLeavesTheSelection()
    {
        (UIRoot root, ListBox listBox) = CreateSelectionModeListBox(SelectionMode.Single, "A", "B", "C");
        ElementInputBridge bridge = new();
        ClickRow(root, bridge, listBox, 1);

        PressKey(root, bridge, InputKey.A, InputKey.LeftCtrl);

        Assert.Equal([1], listBox.SelectionModel.SelectedIndices);
    }

    [Fact]
    public void ListBoxTextSearchIsEnabledAndCaseInsensitiveByDefault()
    {
        ListBox listBox = new();

        Assert.True(listBox.IsTextSearchEnabled);
        Assert.False(listBox.IsTextSearchCaseSensitive);
    }

    // With IsTextSearchEnabled false, a typed letter selects nothing (#680).
    [Fact]
    public void TypedLetterSelectsNothingWhenTextSearchIsDisabled()
    {
        (UIRoot root, ListBox listBox, ElementInputBridge bridge) = CreateFruitListBoxWithClickedApple();
        listBox.IsTextSearchEnabled = false;

        TypeText(root, bridge, "c");

        Assert.Equal(0, listBox.SelectedIndex);
    }

    // As WPF's TextSearch, letters typed in quick succession search for the prefix they form: "b" then "a" keeps
    // Banana, where "a" alone would select Apple (#680).
    [Fact]
    public void TypedLettersInQuickSuccessionSearchForTheirPrefix()
    {
        (UIRoot root, ListBox listBox, ElementInputBridge bridge) = CreateFruitListBoxWithClickedApple();

        TypeText(root, bridge, "b", "a");

        Assert.Equal(1, listBox.SelectedIndex);
        Assert.True(Row(listBox, 1).IsKeyboardFocused);
    }

    [Fact]
    public void ListBoxSelectionSurvivesContainerRecycling()
    {
        ListBox listBox = new();
        listBox.SetItems(new[] { "a", "b", "c" });
        listBox.SelectedIndex = 2;

        listBox.ItemContainerGenerator.Realize(new RealizationWindow(0, 1));
        listBox.ItemContainerGenerator.Realize(new RealizationWindow(2, 3));

        ListBoxItem selected = Assert.IsType<ListBoxItem>(listBox.ItemContainerGenerator.RealizedContainers[2]);
        Assert.True(selected.IsSelected);
    }

    [Fact]
    public void ListBoxStringItemShowsItsText()
    {
        UIRoot root = new(200, 200);
        ListBox listBox = new();
        listBox.SetItems(new[] { "Apple", "Banana" });
        TextBlock reference = new() { Text = "Apple" };
        root.VisualChildren.Add(listBox);
        root.VisualChildren.Add(reference);
        root.ProcessFrame();

        ListBoxItem row = Assert.IsType<ListBoxItem>(listBox.ItemContainerGenerator.RealizedContainers[0]);

        Assert.Equal("Apple", row.Content);
        Assert.Equal("Apple", Assert.Single(FindTextBlocks(row)).Text);
        Assert.True(reference.DesiredSize.Height > 0);
        Assert.Equal(reference.DesiredSize.Height, row.DesiredSize.Height);
    }

    [Fact]
    public void ListBoxRowShowsTextOfReplacedStringItem()
    {
        UIRoot root = new(200, 200);
        ListBox listBox = new();
        listBox.SetItems(new[] { "Apple", "Banana" });
        root.VisualChildren.Add(listBox);
        root.ProcessFrame();

        listBox.Items[0] = "Cherry";
        root.ProcessFrame();

        ListBoxItem row = Assert.IsType<ListBoxItem>(listBox.ItemContainerGenerator.RealizedContainers[0]);
        Assert.Equal("Cherry", Assert.Single(FindTextBlocks(row)).Text);
    }

    [Fact]
    public void ListBoxRowTextUsesRowFontAndForeground()
    {
        UIRoot root = new(300, 200);
        SolidColorBrush foreground = new(Color.White);
        ListBox listBox = new()
        {
            ItemContainerAspect = new ElementAspect(
            [
                new ElementAspectValue(Control.FontSizeProperty, 30f),
                new ElementAspectValue(Control.ForegroundProperty, foreground)
            ])
        };
        listBox.SetItems(new[] { "Apple" });
        root.VisualChildren.Add(listBox);
        root.ProcessFrame();

        TextBlock text = Assert.Single(FindTextBlocks(listBox.ItemContainerGenerator.RealizedContainers[0]));

        Assert.Equal(30, text.FontSize);
        Assert.Same(foreground, text.Foreground);
    }

    [Fact]
    public void ListBoxItemTemplateCreatesRowContent()
    {
        UIRoot root = new(300, 200);
        List<string?> templated = [];
        ListBox listBox = new()
        {
            ItemsSource = new List<string> { "Alpha", "Beta" },
            ItemTemplate = new ContentTemplate<string>("Text", key: null, priority: 0, context =>
            {
                templated.Add(context.Data);
                return new TextBlock { Text = "[" + context.Data + "]" };
            })
        };
        root.VisualChildren.Add(listBox);
        root.ProcessFrame();

        Assert.Equal(["Alpha", "Beta"], templated);
        ListBoxItem row = Assert.IsType<ListBoxItem>(listBox.ItemContainerGenerator.RealizedContainers[1]);
        Assert.Equal("Beta", row.Content);
        Assert.Equal("[Beta]", Assert.Single(FindTextBlocks(row)).Text);
        Assert.True(row.DesiredSize.Height > 0);
    }

    [Fact]
    public void ListBoxElementItemStaysTheOnlyChildOfItsRow()
    {
        UIRoot root = new(200, 200);
        FixedElement element = new();
        ListBox listBox = new();
        listBox.SetItems(new UIElement[] { element });
        root.VisualChildren.Add(listBox);
        root.ProcessFrame();

        ListBoxItem row = Assert.IsType<ListBoxItem>(listBox.ItemContainerGenerator.RealizedContainers[0]);

        Assert.Same(element, row.Content);
        Assert.Same(element, Assert.Single(row.VisualChildren));
        Assert.Equal(new LayoutSize(20, 10), element.DesiredSize);
        Assert.Equal(10, row.DesiredSize.Height);
    }

    // WPF copies ItemTemplate to the row's ContentTemplate, and its ContentPresenter tries that template before UIElement content.
    [Fact]
    public void ListBoxItemTemplatePresentsElementItem()
    {
        UIRoot root = new(300, 200);
        Marker item = new("raw");
        List<(object? Data, int Index)> templated = [];
        ListBox listBox = new() { ItemTemplate = MarkerTemplate(templated) };
        listBox.SetItems(new object[] { "first", item });
        root.VisualChildren.Add(listBox);
        root.ProcessFrame();

        ListBoxItem row = Assert.IsType<ListBoxItem>(listBox.ItemContainerGenerator.RealizedContainers[1]);

        Assert.Same(item, row.Content);
        Assert.Equal(["T1:Marker"], MarkerLabels(row));
        Assert.Contains(templated, entry => ReferenceEquals(entry.Data, item) && entry.Index == 1);
        Assert.Null(item.VisualParent);
        Assert.Equal(new LayoutSize(40, 10), row.DesiredSize);
    }

    [Fact]
    public void ListBoxItemIsItsOwnRowWithoutItemTemplate()
    {
        UIRoot root = new(300, 200);
        ListBoxItem item = new() { Content = new Marker("own") };
        ListBox listBox = new() { ItemTemplate = MarkerTemplate([]) };
        listBox.SetItems(new object[] { item });
        root.VisualChildren.Add(listBox);
        root.ProcessFrame();

        Assert.Same(item, listBox.ItemContainerGenerator.RealizedContainers[0]);
        Assert.Equal(["own"], MarkerLabels(item));
    }

    [Fact]
    public void ListBoxRowPresentsItemTemplateWhenItsItemChangesBetweenElementAndData()
    {
        UIRoot root = new(300, 200);
        Marker element = new("raw");
        ListBox listBox = new() { ItemTemplate = MarkerTemplate([]) };
        listBox.SetItems(new object[] { "data" });
        root.VisualChildren.Add(listBox);
        root.ProcessFrame();
        Assert.Equal(["T0:String"], MarkerLabels(listBox));

        listBox.Items[0] = element;
        root.ProcessFrame();
        Assert.Equal(["T0:Marker"], MarkerLabels(listBox));
        Assert.Null(element.VisualParent);

        listBox.Items[0] = "data";
        root.ProcessFrame();
        Assert.Equal(["T0:String"], MarkerLabels(listBox));
    }

    [Fact]
    public void ListBoxRowHostsElementItemAgainWhenItemTemplateIsRemoved()
    {
        UIRoot root = new(300, 200);
        Marker element = new("raw");
        ListBox listBox = new() { ItemTemplate = MarkerTemplate([]) };
        listBox.SetItems(new object[] { element });
        root.VisualChildren.Add(listBox);
        root.ProcessFrame();
        Assert.Equal(["T0:Marker"], MarkerLabels(listBox));

        listBox.ItemTemplate = null;
        root.ProcessFrame();

        ListBoxItem row = Assert.IsType<ListBoxItem>(listBox.ItemContainerGenerator.RealizedContainers[0]);
        Assert.Same(element, Assert.Single(row.VisualChildren));
        Assert.Equal(["raw"], MarkerLabels(listBox));
    }

    [Fact]
    public void ListBoxRowHostsElementItemWhenItemTemplateThatWrappedItIsRemoved()
    {
        UIRoot root = new(300, 200);
        Marker element = new("raw");
        ListBox listBox = new() { ItemTemplate = WrapElementTemplate() };
        listBox.SetItems(new object[] { element });
        root.VisualChildren.Add(listBox);
        root.ProcessFrame();
        Border wrapper = Assert.IsType<Border>(element.VisualParent);

        listBox.ItemTemplate = null;
        root.ProcessFrame();

        ListBoxItem row = Assert.IsType<ListBoxItem>(listBox.ItemContainerGenerator.RealizedContainers[0]);
        Assert.Same(row, element.VisualParent);
        Assert.Same(element, Assert.Single(row.VisualChildren));
        Assert.Empty(wrapper.VisualChildren);
    }

    [Fact]
    public void ListBoxRowWrapsElementItemInReplacedItemTemplate()
    {
        UIRoot root = new(300, 200);
        Marker element = new("raw");
        ListBox listBox = new() { ItemTemplate = WrapElementTemplate() };
        listBox.SetItems(new object[] { element });
        root.VisualChildren.Add(listBox);
        root.ProcessFrame();
        Border first = Assert.IsType<Border>(element.VisualParent);

        listBox.ItemTemplate = WrapElementTemplate();
        root.ProcessFrame();

        Border second = Assert.IsType<Border>(element.VisualParent);
        Assert.NotSame(first, second);
        Assert.Same(element, second.Child);
        Assert.Empty(first.VisualChildren);
        ListBoxItem row = Assert.IsType<ListBoxItem>(listBox.ItemContainerGenerator.RealizedContainers[0]);
        Assert.Same(row, second.VisualParent!.VisualParent);
    }

    [Fact]
    public void ListBoxRowHostsElementItemWhenItemTemplateThatReturnedItIsRemoved()
    {
        UIRoot root = new(300, 200);
        Marker element = new("raw");
        ListBox listBox = new() { ItemTemplate = IdentityElementTemplate() };
        listBox.SetItems(new object[] { element });
        root.VisualChildren.Add(listBox);
        root.ProcessFrame();
        ContentPresenter presenter = Assert.IsType<ContentPresenter>(element.VisualParent);

        listBox.ItemTemplate = null;
        root.ProcessFrame();

        ListBoxItem row = Assert.IsType<ListBoxItem>(listBox.ItemContainerGenerator.RealizedContainers[0]);
        Assert.Same(row, element.VisualParent);
        Assert.Same(row, element.LogicalParent);
        Assert.Same(element, Assert.Single(row.VisualChildren));
        Assert.DoesNotContain(element, presenter.VisualChildren);
        Assert.DoesNotContain(element, presenter.LogicalChildren);
    }

    [Fact]
    public void ListBoxRowWrapsElementItemWhenItemTemplateThatReturnedItIsReplaced()
    {
        UIRoot root = new(300, 200);
        Marker element = new("raw");
        ListBox listBox = new() { ItemTemplate = IdentityElementTemplate() };
        listBox.SetItems(new object[] { element });
        root.VisualChildren.Add(listBox);
        root.ProcessFrame();
        ContentPresenter presenter = Assert.IsType<ContentPresenter>(element.VisualParent);

        listBox.ItemTemplate = WrapElementTemplate();
        root.ProcessFrame();

        Border wrapper = Assert.IsType<Border>(element.VisualParent);
        Assert.Same(element, wrapper.Child);
        ListBoxItem row = Assert.IsType<ListBoxItem>(listBox.ItemContainerGenerator.RealizedContainers[0]);
        Assert.Same(row, wrapper.VisualParent!.VisualParent);
        Assert.NotSame(presenter.PresentedChild, element);
        Assert.DoesNotContain(element, presenter.VisualChildren);
        Assert.DoesNotContain(element, presenter.LogicalChildren);
    }

    [Fact]
    public void ListBoxItemWithStringContentShowsItsTextInsidePadding()
    {
        UIRoot root = new(200, 200);
        ListBoxItem item = new() { Content = "Ink", Padding = new Thickness(4) };
        TextBlock reference = new() { Text = "Ink" };
        root.VisualChildren.Add(item);
        root.VisualChildren.Add(reference);
        root.ProcessFrame();

        Assert.Equal("Ink", Assert.Single(FindTextBlocks(item)).Text);
        Assert.Equal(reference.DesiredSize.Height + 8, item.DesiredSize.Height);
    }

    // As the Border of the WPF ListBox template, Background fills the arranged bounds behind the items (#590).
    [Fact]
    public void ListBoxFillsItsArrangedBoundsWithBackgroundBehindItsItems()
    {
        SolidColorBrush red = new(Color.Red);
        ListBox listBox = new() { Background = red };
        listBox.Items.Add(new ListBoxItem { Content = "One" });
        UIRoot root = new();
        root.VisualChildren.Add(listBox);
        root.ProcessFrame();
        listBox.Arrange(new ArrangeContext(new LayoutRect(3, 4, 40, 30)));
        root.Invalidate(InvalidationFlags.Render | InvalidationFlags.Subtree, "test");
        root.ProcessFrame();

        List<DrawCommand> commands = [.. root.RetainedRenderer.Commit(root)];

        DrawCommand fill = Assert.Single(commands, command => ReferenceEquals(command.Brush, red));
        Assert.Equal(DrawCommandKind.FillRectangle, fill.Kind);
        Assert.Equal(new DrawRect(3, 4, 40, 30), fill.Rect);
        Assert.Equal(0, commands.IndexOf(fill));
        Assert.True(commands.Count > 1);
    }

    // As the Border of the WPF ListBox template, BorderBrush draws the border around the rows (#609).
    [Fact]
    public void ListBoxDrawsItsBorderBrushAroundItsArrangedBounds()
    {
        SolidColorBrush blue = new(Color.Blue);
        ListBox listBox = new() { BorderBrush = blue, BorderThickness = new Thickness(2) };
        listBox.Items.Add(new ListBoxItem { Content = "One" });
        UIRoot root = new();
        root.VisualChildren.Add(listBox);
        root.ProcessFrame();
        listBox.Arrange(new ArrangeContext(new LayoutRect(3, 4, 40, 30)));
        root.Invalidate(InvalidationFlags.Render | InvalidationFlags.Subtree, "test");
        root.ProcessFrame();

        List<DrawCommand> commands = [.. root.RetainedRenderer.Commit(root)];

        DrawCommand border = Assert.Single(commands, command => ReferenceEquals(command.Brush, blue));
        Assert.Equal(DrawCommandKind.DrawRectangle, border.Kind);
        Assert.Equal(new DrawRect(3, 4, 40, 30), border.Rect);
    }

    // As the WPF ListBox template, the rows are arranged inside BorderThickness and Padding (#609). The root and the
    // arranged rectangle are large enough for the row, so the ScrollViewer of the template shows no scroll bar.
    [Fact]
    public void ListBoxArrangesItsRowsInsideBorderThicknessAndPadding()
    {
        ListBoxItem row = new() { Content = "One" };
        ListBox listBox = new() { BorderThickness = new Thickness(2), Padding = new Thickness(10) };
        listBox.Items.Add(row);
        UIRoot root = new(200, 200);
        root.VisualChildren.Add(listBox);
        root.ProcessFrame();

        Assert.Equal(new LayoutSize(row.DesiredSize.Width + 24, row.DesiredSize.Height + 24), listBox.DesiredSize);

        listBox.Arrange(new ArrangeContext(new LayoutRect(3, 4, 80, 60)));
        root.ProcessFrame();

        Assert.Equal(15, row.ArrangedBounds.X);
        Assert.Equal(16, row.ArrangedBounds.Y);
        Assert.Equal(56, row.ArrangedBounds.Width);
    }

    [Fact]
    public void ListBoxWithoutBackgroundDrawsNoFill()
    {
        UIRoot root = new(100, 100);
        ListBox listBox = new();
        listBox.SetItems(new[] { "one" });
        root.VisualChildren.Add(listBox);
        root.ProcessFrame();

        Assert.Empty(Render(listBox));
    }

    [Fact]
    public void ListBoxWithComponentTemplateLeavesItsBackgroundToTheTemplate()
    {
        UIRoot root = new(100, 100);
        ListBox listBox = new()
        {
            Background = new SolidColorBrush(Color.Red),
            ComponentTemplate = new ComponentTemplate<ListBox>("list", _ => new Border())
        };
        root.VisualChildren.Add(listBox);
        root.ProcessFrame();

        Assert.Empty(Render(listBox));
    }

    [Fact]
    public void TabControlSelectedTabItemReturnsUnrealizedTabItem()
    {
        TabControl tabControl = new();
        TabItem first = new()
        {
            Header = "First",
            Content = "First content"
        };
        TabItem second = new()
        {
            Header = "Second",
            Content = "Second content"
        };
        tabControl.SetItems(new[] { first, second });

        tabControl.SelectedIndex = 1;

        Assert.Same(second, tabControl.SelectedItem);
        Assert.Same(second, tabControl.SelectedTabItem);
        TabItem selected = tabControl.SelectedTabItem!;
        Assert.Equal("Second", selected.Header);
        Assert.Equal("Second content", selected.Content);
    }

    private static DrawCommandList Render(UIElement element)
    {
        DrawCommandList commands = new();
        element.Render(new RenderContext(element, new DrawingContext(commands), element.ArrangedBounds, RenderLayer.Default, new RenderCounters()));
        return commands;
    }

    private static InputFrame PointerFrame(
        float x,
        float y,
        bool previousDown = false,
        bool currentDown = false,
        KeyboardSnapshot? keyboard = null)
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

        return new InputFrame(previous, current, keyboard ?? KeyboardSnapshot.Empty, keyboard ?? KeyboardSnapshot.Empty, []);
    }

    private static (UIRoot Root, ListBox ListBox) CreateListBox(Func<ContentTemplateContext<string>, UIElement> build)
    {
        UIRoot root = new(200, 200);
        ListBox listBox = new() { ItemTemplate = new ContentTemplate<string>("row", key: null, priority: 0, build) };
        listBox.SetItems(new[] { "one", "two" });
        root.VisualChildren.Add(listBox);
        root.ProcessFrame();
        return (root, listBox);
    }

    private static void Click(UIRoot root, ElementInputBridge bridge, float x, float y)
    {
        bridge.Dispatch(root, PointerFrame(x, y, currentDown: true));
        bridge.Dispatch(root, PointerFrame(x, y, previousDown: true));
        root.ProcessFrame();
    }

    private static void ClickCenter(UIRoot root, ElementInputBridge bridge, UIElement element)
    {
        LayoutRect bounds = element.ArrangedBounds;
        float x = bounds.X + bounds.Width / 2;
        float y = bounds.Y + bounds.Height / 2;
        bridge.Dispatch(root, PointerFrame(x, y, currentDown: true));
        root.ProcessFrame();
        bridge.Dispatch(root, PointerFrame(x, y, previousDown: true));
        root.ProcessFrame();
    }

    // The button at the top left and the list box 120 pixels below it, in a window.
    private static UIRoot CreateFocusWindowRoot(Button other, ListBox listBox)
    {
        Cerneala.UI.Layout.Panels.Canvas canvas = new();
        canvas.VisualChildren.Add(other);
        canvas.VisualChildren.Add(listBox);
        Cerneala.UI.Layout.Panels.Canvas.SetTop(listBox, 120);
        UIRoot root = new(360, 300);
        root.VisualChildren.Add(new Window { Content = canvas, Width = 360, Height = 300 });
        root.ProcessFrame();
        return root;
    }

    // A ListBox with "Apple", "Banana", "Cherry" in a window, after the user clicked "Apple": it is selected and has
    // the keyboard focus.
    private static (UIRoot Root, ListBox ListBox, ElementInputBridge Bridge) CreateFruitListBoxWithClickedApple()
    {
        ListBox listBox = new() { Width = 150, Height = 120 };
        listBox.SetItems(new[] { "Apple", "Banana", "Cherry" });
        UIRoot root = new(360, 300);
        root.VisualChildren.Add(new Window { Content = listBox, Width = 360, Height = 300 });
        root.ProcessFrame();
        ListBoxItem apple = Assert.IsType<ListBoxItem>(listBox.ItemContainerGenerator.RealizedContainers[0]);
        ElementInputBridge bridge = new();

        ClickCenter(root, bridge, apple);

        Assert.Equal(0, listBox.SelectedIndex);
        Assert.True(apple.IsKeyboardFocused);
        return (root, listBox, bridge);
    }

    // A ListBox with the given SelectionMode and items in a window.
    private static (UIRoot Root, ListBox ListBox) CreateSelectionModeListBox(SelectionMode mode, params string[] items)
    {
        ListBox listBox = new() { SelectionMode = mode, Width = 150, Height = 200 };
        listBox.SetItems(items);
        UIRoot root = new(360, 300);
        root.VisualChildren.Add(new Window { Content = listBox, Width = 360, Height = 300 });
        root.ProcessFrame();
        return (root, listBox);
    }

    // A click on the center of the row at index, with the modifier keys held down during the press and the release.
    private static void ClickRow(UIRoot root, ElementInputBridge bridge, ListBox listBox, int index, params InputKey[] modifiers)
    {
        LayoutRect bounds = listBox.ItemContainerGenerator.RealizedContainers[index].ArrangedBounds;
        float x = bounds.X + bounds.Width / 2;
        float y = bounds.Y + bounds.Height / 2;
        KeyboardSnapshot keyboard = KeyboardSnapshot.FromDownKeys(modifiers);
        bridge.Dispatch(root, PointerFrame(x, y, currentDown: true, keyboard: keyboard));
        root.ProcessFrame();
        bridge.Dispatch(root, PointerFrame(x, y, previousDown: true, keyboard: keyboard));
        root.ProcessFrame();
    }

    private static bool[] RowSelection(ListBox listBox)
    {
        return [.. Enumerable.Range(0, listBox.ItemCount)
            .Select(index => Assert.IsType<ListBoxItem>(listBox.ItemContainerGenerator.RealizedContainers[index]).IsSelected)];
    }

    // A press and release of key, with the modifier keys pressed before it and released after it.
    private static void PressKey(UIRoot root, ElementInputBridge bridge, InputKey key, params InputKey[] modifiers)
    {
        InputKey[] withKey = [.. modifiers, key];
        if (modifiers.Length > 0)
        {
            bridge.Dispatch(root, KeyFrame([], modifiers));
        }

        bridge.Dispatch(root, KeyFrame(modifiers, withKey));
        bridge.Dispatch(root, KeyFrame(withKey, modifiers));
        if (modifiers.Length > 0)
        {
            bridge.Dispatch(root, KeyFrame(modifiers, []));
        }

        root.ProcessFrame();
    }

    // The texts typed in one input frame, as text input events in that order.
    private static void TypeText(UIRoot root, ElementInputBridge bridge, params string[] texts)
    {
        bridge.Dispatch(root, new InputFrame(
            PointerSnapshot.Empty,
            PointerSnapshot.Empty,
            KeyboardSnapshot.Empty,
            KeyboardSnapshot.Empty,
            [.. texts.Select(text => new TextInputSnapshotEvent(text))]));
        root.ProcessFrame();
    }

    // A ListBox 150 wide and height high, with rowCount rows "0", "1", ... of 20 pixels, in a window.
    private static (UIRoot Root, ListBox ListBox) CreateRowListBox(float height, int rowCount, bool layout = true)
    {
        ListBox listBox = new() { Width = 150, Height = height, ItemContainerAspect = RowHeight(20) };
        listBox.SetItems(Enumerable.Range(0, rowCount).Select(index => index.ToString()).ToArray());
        UIRoot root = new(360, 300);
        root.VisualChildren.Add(new Window { Content = listBox, Width = 360, Height = 300 });
        if (layout)
        {
            root.ProcessFrame();
        }

        return (root, listBox);
    }

    // A ListBox 150 wide and 100 high with a VirtualizingStackPanel and 10,000 rows "0", "1", ... of 10 pixels.
    private static ListBox CreateVirtualizingRowListBox()
    {
        return new ListBox
        {
            Width = 150,
            Height = 100,
            ItemsPanel = new Cerneala.UI.Layout.Panels.VirtualizingStackPanel(),
            ItemContainerAspect = RowHeight(10),
            ItemsSource = Enumerable.Range(0, 10_000).Select(index => index.ToString()).ToArray()
        };
    }

    // That ListBox alone in a window, after the first layout.
    private static (UIRoot Root, ListBox ListBox) CreateVirtualizingWindowListBox()
    {
        ListBox listBox = CreateVirtualizingRowListBox();
        UIRoot root = new(360, 300);
        root.VisualChildren.Add(new Window { Content = listBox, Width = 360, Height = 300 });
        root.ProcessFrame();
        return (root, listBox);
    }

    // A ListBox 150 wide without a fixed height, with a VirtualizingStackPanel and 10,000 rows of 10 pixels, as the
    // content of a ScrollViewer in a window 300 pixels high, after the first layout.
    private static (UIRoot Root, ListBox ListBox, ScrollViewer Outer) CreateOuterViewerListBox()
    {
        ListBox listBox = new()
        {
            Width = 150,
            ItemsPanel = new Cerneala.UI.Layout.Panels.VirtualizingStackPanel(),
            ItemContainerAspect = RowHeight(10),
            ItemsSource = Enumerable.Range(0, 10_000).Select(index => index.ToString()).ToArray()
        };
        ScrollViewer outer = new() { Content = listBox, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
        UIRoot root = new(360, 300);
        root.VisualChildren.Add(new Window { Content = outer, Width = 360, Height = 300 });
        root.ProcessFrame();
        Assert.InRange(listBox.ItemContainerGenerator.RealizedContainers.Count, 1, 40);
        return (root, listBox, outer);
    }

    private static void AssertInsideViewport(ScrollViewer viewer, UIElement element)
    {
        LayoutRect viewport = viewer.Presenter.ArrangedBounds;
        LayoutRect bounds = element.ArrangedBounds;
        Assert.InRange(bounds.Y, viewport.Y, viewport.Y + viewport.Height - bounds.Height);
    }

    private static ElementAspect RowHeight(float height)
    {
        return new ElementAspect([new ElementAspectValue(UIElement.HeightProperty, height)]);
    }

    private static ListBoxItem Row(ListBox listBox, int index)
    {
        return Assert.IsType<ListBoxItem>(listBox.ItemContainerGenerator.RealizedContainers[index]);
    }

    private static ScrollViewer ListBoxScrollViewer(ListBox listBox)
    {
        return Assert.IsType<ScrollViewer>(listBox.ComponentTemplateInstance!.Parts["PART_ScrollViewer"]);
    }

    private static void AssertRowInsideViewport(ListBox listBox, int index)
    {
        LayoutRect viewport = ListBoxScrollViewer(listBox).Presenter.ArrangedBounds;
        LayoutRect row = Row(listBox, index).ArrangedBounds;
        Assert.InRange(row.Y, viewport.Y, viewport.Y + viewport.Height - row.Height);
    }

    private static void AssertFocusedSelectedRow(ListBox listBox, int index)
    {
        Assert.Equal(index, listBox.SelectedIndex);
        Assert.True(Row(listBox, index).IsKeyboardFocused);
        AssertRowInsideViewport(listBox, index);
    }

    private static bool IsInside(UIElement element, UIElement ancestor)
    {
        for (UIElement? current = element; current is not null; current = current.VisualParent)
        {
            if (ReferenceEquals(current, ancestor))
            {
                return true;
            }
        }

        return false;
    }

    private static bool HasAncestor<T>(UIElement element) where T : UIElement
    {
        for (UIElement? current = element.VisualParent; current is not null; current = current.VisualParent)
        {
            if (current is T)
            {
                return true;
            }
        }

        return false;
    }

    private static InputFrame KeyFrame(InputKey[] previousKeys, InputKey[] currentKeys)
    {
        return new InputFrame(
            PointerSnapshot.Empty,
            PointerSnapshot.Empty,
            KeyboardSnapshot.FromDownKeys(previousKeys),
            KeyboardSnapshot.FromDownKeys(currentKeys),
            []);
    }

    private static T? FindDescendant<T>(UIElement element) where T : UIElement
    {
        if (element is T match)
        {
            return match;
        }

        foreach (UIElement child in element.VisualChildren)
        {
            if (FindDescendant<T>(child) is T found)
            {
                return found;
            }
        }

        return null;
    }

    private static List<TextBlock> FindTextBlocks(UIElement element)
    {
        List<TextBlock> found = [];
        foreach (UIElement child in element.VisualChildren)
        {
            if (child is TextBlock textBlock)
            {
                found.Add(textBlock);
            }

            found.AddRange(FindTextBlocks(child));
        }

        return found;
    }

    private static ContentTemplate MarkerTemplate(List<(object? Data, int Index)> templated)
    {
        return new ContentTemplate("Row", typeof(object), key: null, priority: 0, context =>
        {
            templated.Add((context.Data, context.Index));
            return new Marker("T" + context.Index + ":" + context.Data!.GetType().Name);
        });
    }

    private static ContentTemplate WrapElementTemplate()
    {
        return new ContentTemplate("wrap", typeof(object), key: null, priority: 0,
            context => new Border { Child = (UIElement?)context.Data });
    }

    private static ContentTemplate IdentityElementTemplate()
    {
        return new ContentTemplate("identity", typeof(object), key: null, priority: 0,
            context => (UIElement?)context.Data);
    }

    private static List<string> MarkerLabels(UIElement element)
    {
        List<string> labels = [];
        foreach (UIElement child in element.VisualChildren)
        {
            if (child is Marker marker)
            {
                labels.Add(marker.Label);
            }

            labels.AddRange(MarkerLabels(child));
        }

        return labels;
    }

    // A panel that virtualizes through the public IItemsVirtualizingPanel contract alone: rows 10 pixels high, the
    // window of the last viewport, and every item while it has no viewport.
    private sealed class FixedRowVirtualizingPanel : Cerneala.UI.Layout.Panels.Panel, IItemsVirtualizingPanel
    {
        private const float RowExtent = 10;
        private ItemsVirtualizationViewport? viewport;

        public int ViewportUpdates { get; private set; }

        public RealizationWindow RealizationWindow => viewport is { } current
            ? RealizationWindow.Create(
                current.ItemCount,
                (int)(current.ScrollOffset / RowExtent),
                (int)MathF.Ceiling((current.ScrollOffset + current.ViewportExtent) / RowExtent))
            : new RealizationWindow(0, int.MaxValue);

        public float TotalExtent => (viewport?.ItemCount ?? VisualChildren.Count) * RowExtent;

        public void UpdateViewport(ItemsVirtualizationViewport next)
        {
            viewport = next;
            ViewportUpdates++;
            Invalidate(InvalidationFlags.Measure | InvalidationFlags.Arrange, "test viewport");
        }

        public void ClearViewport()
        {
            viewport = null;
            Invalidate(InvalidationFlags.Measure | InvalidationFlags.Arrange, "test viewport cleared");
        }

        protected override LayoutSize MeasureCore(MeasureContext context)
        {
            foreach (UIElement child in VisualChildren)
            {
                child.Measure(new MeasureContext(new LayoutSize(context.AvailableSize.Width, float.PositiveInfinity), context.Rounding));
            }

            return new LayoutSize(0, TotalExtent);
        }

        protected override LayoutRect ArrangeCore(ArrangeContext context)
        {
            int start = viewport is null ? 0 : RealizationWindow.StartIndex;
            for (int index = 0; index < VisualChildren.Count; index++)
            {
                VisualChildren[index].Arrange(new ArrangeContext(
                    new LayoutRect(context.FinalRect.X, context.FinalRect.Y + ((start + index) * RowExtent), context.FinalRect.Width, RowExtent),
                    context.Rounding));
            }

            return context.FinalRect;
        }
    }

    private sealed class Marker(string label) : UIElement
    {
        public string Label { get; } = label;

        protected override LayoutSize MeasureCore(MeasureContext context)
        {
            return new LayoutSize(40, 10);
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
