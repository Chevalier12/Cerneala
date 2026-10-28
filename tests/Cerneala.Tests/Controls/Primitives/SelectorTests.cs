using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using Cerneala.UI.Controls;
using Cerneala.UI.Controls.Items;
using Cerneala.UI.Controls.Primitives;
using Cerneala.UI.Core;
using Cerneala.UI.Data;
using Cerneala.UI.Elements;
using Cerneala.UI.Input;
using Cerneala.UI.Layout;

namespace Cerneala.Tests.Controls.Primitives;

public sealed class SelectorTests
{
    [Fact]
    public void SelectorClickSelectsItemContainer()
    {
        ListBox listBox = CreateArrangedListBox();
        ElementInputBridge bridge = new();

        bridge.Dispatch(listBox.Root!, PointerFrame(5, 15, currentDown: true));
        bridge.Dispatch(listBox.Root!, PointerFrame(5, 15, previousDown: true));

        Assert.Equal(1, listBox.SelectedIndex);
    }

    [Fact]
    public void ClickAfterRemovingEarlierItemInSameInputDispatchSelectsClickedItem()
    {
        UIElement[] items = CreateFixedItems(4);
        ListBox listBox = new();
        listBox.SetItems(items);
        ArrangeInRoot(listBox);
        bool removed = false;
        listBox.Handlers.AddHandler(InputEvents.PreviewMouseUpEvent, (_, _) =>
        {
            if (!removed)
            {
                removed = true;
                listBox.Items.RemoveAt(0);
            }
        });

        Click(listBox, 5, 25);

        Assert.True(removed);
        Assert.Equal(1, listBox.SelectedIndex);
        Assert.Same(items[2], listBox.SelectedItem);
    }

    [Fact]
    public void ClickAfterRemovingEarlierItemBeforeLayoutSelectsClickedItem()
    {
        UIElement[] items = CreateFixedItems(4);
        ListBox listBox = new();
        listBox.SetItems(items);
        ArrangeInRoot(listBox);

        listBox.Items.RemoveAt(0);
        Click(listBox, 5, 25);

        Assert.Equal(1, listBox.SelectedIndex);
        Assert.Same(items[2], listBox.SelectedItem);
    }

    [Fact]
    public void ClickAfterInsertingEarlierItemBeforeLayoutSelectsClickedItem()
    {
        UIElement[] items = CreateFixedItems(4);
        ListBox listBox = new();
        listBox.SetItems(items);
        ArrangeInRoot(listBox);

        listBox.Items.Insert(0, new FixedElement(new LayoutSize(20, 10)));
        Click(listBox, 5, 25);

        Assert.Equal(3, listBox.SelectedIndex);
        Assert.Same(items[2], listBox.SelectedItem);
    }

    [Fact]
    public void ClickAfterMovingEarlierItemBeforeLayoutSelectsClickedItem()
    {
        UIElement[] items = CreateFixedItems(4);
        ObservableCollection<UIElement> source = new(items);
        ListBox listBox = new() { ItemsSource = source };
        ArrangeInRoot(listBox);

        source.Move(0, 3);
        Click(listBox, 5, 25);

        Assert.Equal(1, listBox.SelectedIndex);
        Assert.Same(items[2], listBox.SelectedItem);
    }

    [Fact]
    public void ClickAfterReplacingClickedItemInSameInputDispatchSelectsNothing()
    {
        ListBox listBox = CreateArrangedTextListBox();
        ListBoxItem clicked = (ListBoxItem)listBox.ItemContainerGenerator.RealizedContainers[1];
        int selectionChanges = 0;
        listBox.SelectionChanged += (_, _) => selectionChanges++;
        bool replaced = false;
        listBox.Handlers.AddHandler(InputEvents.PreviewMouseUpEvent, (_, _) =>
        {
            if (!replaced)
            {
                replaced = true;
                listBox.Items[1] = "x";
            }
        });

        Click(listBox, 5, 15);

        Assert.True(replaced);
        Assert.Equal("b", clicked.Content);
        Assert.Equal(-1, listBox.SelectedIndex);
        Assert.Null(listBox.SelectedItem);
        Assert.Equal(0, selectionChanges);
    }

    [Fact]
    public void ClickAfterReplacingClickedItemBeforeLayoutSelectsNothing()
    {
        ListBox listBox = CreateArrangedTextListBox();
        ListBoxItem clicked = (ListBoxItem)listBox.ItemContainerGenerator.RealizedContainers[1];

        listBox.Items[1] = "x";
        Click(listBox, 5, 15);

        Assert.Equal("b", clicked.Content);
        Assert.Equal(-1, listBox.SelectedIndex);
        Assert.Null(listBox.SelectedItem);
    }

    [Fact]
    public void ClickAfterReplacingClickedItemsSourceItemBeforeLayoutSelectsNothing()
    {
        ObservableCollection<string> source = ["a", "b", "c"];
        ListBox listBox = CreateArrangedTextListBox(source);
        ListBoxItem clicked = (ListBoxItem)listBox.ItemContainerGenerator.RealizedContainers[1];

        source[1] = "x";
        Click(listBox, 5, 15);

        Assert.Equal("b", clicked.Content);
        Assert.Equal(-1, listBox.SelectedIndex);
        Assert.Null(listBox.SelectedItem);
    }

    [Fact]
    public void ClickAfterReplacingClickedItemAndLayoutSelectsNewItem()
    {
        ListBox listBox = CreateArrangedTextListBox();

        listBox.Items[1] = "x";
        listBox.Root!.ProcessFrame();
        Click(listBox, 5, 15);

        Assert.Equal("x", ((ListBoxItem)listBox.ItemContainerGenerator.RealizedContainers[1]).Content);
        Assert.Equal(1, listBox.SelectedIndex);
        Assert.Equal("x", listBox.SelectedItem);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ClickOnReplacedRowWhosePreparationThrewSelectsNothing(bool keepFailing)
    {
        FailingContentListBox listBox = new();
        CreateArrangedTextListBox(listBox: listBox);
        ListBoxItem clicked = (ListBoxItem)listBox.ItemContainerGenerator.RealizedContainers[1];
        int selectionChanges = 0;
        listBox.SelectionChanged += (_, _) => selectionChanges++;

        listBox.Items[1] = "x";
        listBox.FailFor = "x";
        Assert.Throws<InvalidOperationException>(() => listBox.Root!.ProcessFrame());
        if (!keepFailing)
        {
            listBox.FailFor = null;
        }

        Click(listBox, 5, 15);

        Assert.Equal("b", clicked.Content);
        Assert.Equal(-1, listBox.SelectedIndex);
        Assert.Null(listBox.SelectedItem);
        Assert.Equal(0, selectionChanges);
    }

    [Fact]
    public void ClickOnReplacedRowPreparedAfterAFailedPreparationSelectsNewItem()
    {
        FailingContentListBox listBox = new();
        CreateArrangedTextListBox(listBox: listBox);

        listBox.Items[1] = "x";
        listBox.FailFor = "x";
        Assert.Throws<InvalidOperationException>(() => listBox.Root!.ProcessFrame());
        listBox.FailFor = null;
        listBox.Root!.ProcessFrame();
        Click(listBox, 5, 15);

        Assert.Equal("x", ((ListBoxItem)listBox.ItemContainerGenerator.RealizedContainers[1]).Content);
        Assert.Equal(1, listBox.SelectedIndex);
        Assert.Equal("x", listBox.SelectedItem);
    }

    [Fact]
    public void ClickWhoseOldRowPreparationThrowsRaisesSelectionChanged()
    {
        FailingContentListBox listBox = new();
        CreateArrangedTextListBox(listBox: listBox);
        listBox.SelectedIndex = 0;
        listBox.Root!.ProcessFrame();
        ListBoxItem clicked = (ListBoxItem)listBox.ItemContainerGenerator.RealizedContainers[1];
        List<SelectionChangedEventArgs> raised = [];
        listBox.SelectionChanged += (_, args) => raised.Add(args);

        listBox.FailFor = "a";
        Record.Exception(() => Click(listBox, 5, 15));

        Assert.Equal(1, listBox.SelectedIndex);
        Assert.Equal("b", listBox.SelectedItem);
        Assert.True(clicked.IsSelected);
        SelectionChangedEventArgs change = Assert.Single(raised);
        Assert.Equal(["a"], change.RemovedItems);
        Assert.Equal(["b"], change.AddedItems);
    }

    [Fact]
    public void SelectedIndexChangeWhoseNewRowPreparationThrowsRaisesSelectionChangedAndPropagates()
    {
        FailingContentListBox listBox = new();
        CreateArrangedTextListBox(listBox: listBox);
        listBox.SelectedIndex = 0;
        ListBoxItem first = (ListBoxItem)listBox.ItemContainerGenerator.RealizedContainers[0];
        List<SelectionChangedEventArgs> raised = [];
        listBox.SelectionChanged += (_, args) => raised.Add(args);

        listBox.FailFor = "b";
        Assert.Throws<InvalidOperationException>(() => listBox.SelectedIndex = 1);

        Assert.Equal(1, listBox.SelectedIndex);
        Assert.False(first.IsSelected);
        SelectionChangedEventArgs change = Assert.Single(raised);
        Assert.Equal(["a"], change.RemovedItems);
        Assert.Equal(["b"], change.AddedItems);
    }

    [Fact]
    public void RelocationWhosePreparationThrowsDoesNotRaiseSelectionChanged()
    {
        ObservableCollection<string> source = ["a", "b", "c"];
        FailingContentListBox listBox = new();
        CreateArrangedTextListBox(source, listBox);
        listBox.SelectedIndex = 1;
        int raised = 0;
        listBox.SelectionChanged += (_, _) => raised++;

        listBox.FailFor = "b";
        Assert.Throws<InvalidOperationException>(() => source.Move(1, 0));

        Assert.Equal(0, listBox.SelectedIndex);
        Assert.Equal("b", listBox.SelectedItem);
        Assert.Equal(0, raised);
    }

    [Fact]
    public void SelectionInvalidatesAffectedContainersOnly()
    {
        ListBox listBox = CreateArrangedListBox();
        ListBoxItem first = Assert.IsType<ListBoxItem>(listBox.ItemContainerGenerator.RealizedContainers[0]);
        ListBoxItem second = Assert.IsType<ListBoxItem>(listBox.ItemContainerGenerator.RealizedContainers[1]);
        int firstVersion = first.RenderVersion;
        int secondVersion = second.RenderVersion;

        listBox.SelectedIndex = 0;
        listBox.SelectedIndex = 1;

        Assert.True(first.RenderVersion > firstVersion);
        Assert.True(second.RenderVersion > secondVersion);
    }

    [Fact]
    public void SelectionReprepareDoesNotDuplicateContainerClickHandlers()
    {
        ListBox listBox = CreateArrangedListBox();
        UIElement first = listBox.ItemContainerGenerator.RealizedContainers[0];

        listBox.SelectedIndex = 0;

        Assert.Single(first.Handlers.GetHandlers(InputEvents.MouseUpEvent));
    }

    [Fact]
    public void SelectionChangeAfterRemovingSelectedLastItemBeforeLayoutDoesNotThrow()
    {
        ListBox listBox = new();
        listBox.SetItems(new[] { "a", "b", "c" });
        listBox.ItemContainerGenerator.Realize();
        listBox.SelectedIndex = 2;

        listBox.Items.RemoveAt(2);
        Exception? exception = Record.Exception(() => listBox.SelectedIndex = 0);

        Assert.Null(exception);
        Assert.Equal(0, listBox.SelectedIndex);
        Assert.Equal("a", listBox.SelectedItem);
        ListBoxItem first = Assert.IsType<ListBoxItem>(listBox.ItemContainerGenerator.RealizedContainers[0]);
        Assert.True(first.IsSelected);
    }

    [Fact]
    public void SelectionFollowsSelectedItemWhenItemIsInsertedBeforeIt()
    {
        ListBox listBox = new();
        listBox.SetItems(new[] { "a", "b", "c" });
        listBox.SelectedIndex = 1;

        listBox.Items.Insert(0, "z");

        Assert.Equal("b", listBox.SelectedItem);
        Assert.Equal(2, listBox.SelectedIndex);
    }

    [Fact]
    public void SelectionFollowsSelectedItemWhenItemIsRemovedBeforeIt()
    {
        ListBox listBox = new();
        listBox.SetItems(new[] { "a", "b", "c" });
        listBox.SelectedIndex = 2;

        listBox.Items.RemoveAt(0);

        Assert.Equal("c", listBox.SelectedItem);
        Assert.Equal(1, listBox.SelectedIndex);
    }

    [Fact]
    public void SelectionFollowsSelectedItemWhenObservableCollectionItemIsInsertedBeforeIt()
    {
        ObservableCollection<string> items = new(["a", "b", "c"]);
        ListBox listBox = new() { ItemsSource = items };
        listBox.SelectedIndex = 1;

        items.Insert(0, "z");

        Assert.Equal("b", listBox.SelectedItem);
        Assert.Equal(2, listBox.SelectedIndex);
    }

    [Fact]
    public void SelectionFollowsSelectedItemWhenObservableCollectionItemIsRemovedBeforeIt()
    {
        ObservableCollection<string> items = new(["a", "b", "c"]);
        ListBox listBox = new() { ItemsSource = items };
        listBox.SelectedIndex = 2;

        items.RemoveAt(0);

        Assert.Equal("c", listBox.SelectedItem);
        Assert.Equal(1, listBox.SelectedIndex);
    }

    [Fact]
    public void SelectionFollowsSelectedItemWhenObservableListItemIsInsertedBeforeIt()
    {
        ObservableList<string> items = new(["a", "b", "c"]);
        ListBox listBox = new() { ItemsSource = items };
        listBox.SelectedIndex = 1;

        items.Insert(0, "z");

        Assert.Equal("b", listBox.SelectedItem);
        Assert.Equal(2, listBox.SelectedIndex);
    }

    [Fact]
    public void SelectionFollowsSelectedItemWhenItIsMoved()
    {
        ObservableCollection<string> items = new(["a", "b", "c", "d"]);
        ListBox listBox = new() { ItemsSource = items };
        listBox.SelectedIndex = 1;

        items.Move(1, 3);
        Assert.Equal("b", listBox.SelectedItem);
        Assert.Equal(3, listBox.SelectedIndex);

        items.Move(0, 3);
        Assert.Equal("b", listBox.SelectedItem);
        Assert.Equal(2, listBox.SelectedIndex);
    }

    [Fact]
    public void SelectionIndexShiftDoesNotRaiseSelectionChanged()
    {
        ListBox listBox = new();
        listBox.SetItems(new[] { "a", "b", "c" });
        listBox.SelectedIndex = 1;
        int raised = 0;
        listBox.SelectionChanged += (_, _) => raised++;

        listBox.Items.Insert(0, "z");

        Assert.Equal(0, raised);
    }

    // Every selected item follows its item when items move or are reset, in selection order, without SelectionChanged.
    [Fact]
    public void EverySelectedItemFollowsItsItemWhenItemsMoveOrReset()
    {
        ObservableCollection<string> items = new(["a", "b", "c", "d"]);
        ListBox listBox = new() { SelectionMode = SelectionMode.Multiple, ItemsSource = items };
        listBox.SelectionModel.Select(1);
        listBox.SelectionModel.Add(3);
        int raised = 0;
        listBox.SelectionChanged += (_, _) => raised++;

        items.Move(0, 3);

        Assert.Equal([0, 2], listBox.SelectionModel.SelectedIndices);
        Assert.Equal(["b", "d"], listBox.SelectedItems);

        ListBox reset = new() { SelectionMode = SelectionMode.Multiple };
        reset.SetItems(new[] { "a", "b", "c", "d" });
        reset.SelectionModel.Select(1);
        reset.SelectionModel.Add(3);
        reset.SelectionChanged += (_, _) => raised++;

        reset.SetItems(new[] { "d", "x", "b" });

        Assert.Equal([2, 0], reset.SelectionModel.SelectedIndices);
        Assert.Equal(["b", "d"], reset.SelectedItems);
        Assert.Equal(0, raised);
    }

    [Fact]
    public void RemovingSelectedItemClearsSelectionAndReportsRemovedItem()
    {
        ListBox listBox = new();
        listBox.SetItems(new[] { "a", "b", "c" });
        listBox.SelectedIndex = 1;
        SelectionChangedEventArgs? raised = null;
        listBox.SelectionChanged += (_, args) => raised = args;

        listBox.Items.RemoveAt(1);

        Assert.Equal(-1, listBox.SelectedIndex);
        Assert.Null(listBox.SelectedItem);
        Assert.NotNull(raised);
        Assert.Equal(["b"], raised.RemovedItems);
        Assert.Empty(raised.AddedItems);
    }

    // A reset relocates each selected entry to its own equal item (#835): equal items that all survive stay selected
    // without SelectionChanged, and a selected entry left without an equal item is deselected and reported.
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ResetThatLeavesFewerEqualItemsThanSelectedReportsTheRemovedSelection(bool bothEqualItemsSurvive)
    {
        ListBox listBox = new() { SelectionMode = SelectionMode.Multiple };
        listBox.SetItems(new[] { "a", "a", "b" });
        listBox.SelectionModel.Select(0);
        listBox.SelectionModel.Add(1);
        List<SelectionChangedEventArgs> raised = [];
        listBox.SelectionChanged += (_, args) => raised.Add(args);

        listBox.SetItems(bothEqualItemsSurvive ? new[] { "b", "a", "a" } : new[] { "a", "b" });

        if (bothEqualItemsSurvive)
        {
            Assert.Empty(raised);
            Assert.Equal(["a", "a"], listBox.SelectedItems);
            Assert.Equal([2, 1], listBox.SelectionModel.SelectedIndices);
        }
        else
        {
            Assert.Equal(["a"], listBox.SelectedItems);
            Assert.Equal([0], listBox.SelectionModel.SelectedIndices);
            SelectionChangedEventArgs args = Assert.Single(raised);
            Assert.Equal(["a"], args.RemovedItems);
        }
    }

    // A selected index that refers to no item keeps its position across a reset, so a selected item whose only equal
    // item lands on that position is reported as removed instead of collapsing into it (#835).
    [Fact]
    public void ResetDoesNotCollapseSelectedItemIntoIndexThatReferredToNoItem()
    {
        ListBox listBox = new() { SelectionMode = SelectionMode.Multiple };
        listBox.SetItems(new[] { "a" });
        listBox.SelectionModel.Select(0);
        listBox.SelectionModel.Add(3);
        List<SelectionChangedEventArgs> raised = [];
        listBox.SelectionChanged += (_, args) => raised.Add(args);

        listBox.SetItems(new[] { "x", "x", "x", "a" });

        Assert.Equal([3], listBox.SelectionModel.SelectedIndices);
        SelectionChangedEventArgs args = Assert.Single(raised);
        Assert.Equal(["a"], args.RemovedItems);
        Assert.Empty(args.AddedItems);
    }

    [Fact]
    public void ResettingItemsKeepsSelectedItemWhenItIsStillPresent()
    {
        ListBox listBox = new();
        listBox.SetItems(new[] { "a", "b", "c" });
        listBox.SelectedIndex = 1;

        listBox.SetItems(new[] { "z", "y", "b" });

        Assert.Equal("b", listBox.SelectedItem);
        Assert.Equal(2, listBox.SelectedIndex);
    }

    [Fact]
    public void ResettingItemsClearsSelectionWhenSelectedItemIsGone()
    {
        ListBox listBox = new();
        listBox.SetItems(new[] { "a", "b", "c" });
        listBox.SelectedIndex = 1;

        listBox.Items.Clear();
        listBox.SetItems(new[] { "x", "y", "z" });

        Assert.Equal(-1, listBox.SelectedIndex);
        Assert.Null(listBox.SelectedItem);
    }

    [Fact]
    public void SelectedIndexSetBeforeItemsExistSelectsThatIndexOnceItemsArrive()
    {
        ListBox listBox = new();
        listBox.SelectedIndex = 1;

        listBox.Items.Add("a");
        listBox.Items.Add("b");

        Assert.Equal(1, listBox.SelectedIndex);
        Assert.Equal("b", listBox.SelectedItem);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void WritingTheCurrentSelectedIndexCollapsesAMultiSelectionThroughSetterAndSetValue(bool useSetValue)
    {
        ListBox listBox = new() { SelectionMode = SelectionMode.Multiple };
        listBox.SetItems(new[] { "a", "b", "c" });
        listBox.SelectionModel.Select(2);
        listBox.SelectionModel.Add(0);
        Assert.Equal([2, 0], listBox.SelectionModel.SelectedIndices);
        Assert.Equal(2, listBox.SelectedIndex);
        int raised = 0;
        SelectionChangedEventArgs? lastArgs = null;
        listBox.SelectionChanged += (_, args) =>
        {
            raised++;
            lastArgs = args;
        };

        if (useSetValue)
        {
            listBox.SetValue(Selector.SelectedIndexProperty, 2);
        }
        else
        {
            listBox.SelectedIndex = 2;
        }

        Assert.Equal([2], listBox.SelectionModel.SelectedIndices);
        Assert.Equal(1, raised);
        Assert.Equal(["a"], lastArgs!.RemovedItems);
        Assert.Empty(lastArgs.AddedItems);
    }

    [Fact]
    public void WritingTheCurrentSelectedIndexBelowTheEffectiveSourceKeepsAMultiSelection()
    {
        ListBox listBox = new() { SelectionMode = SelectionMode.Multiple };
        listBox.SetItems(new[] { "a", "b", "c" });
        listBox.SelectionModel.Select(2);
        listBox.SelectionModel.Add(0);
        Assert.Equal(UiPropertyValueSource.Local, listBox.GetValueSource(Selector.SelectedIndexProperty));
        int raised = 0;
        listBox.SelectionChanged += (_, _) => raised++;

        listBox.SetValue(Selector.SelectedIndexProperty, 2, UiPropertyValueSource.AspectBase);

        Assert.Equal([2, 0], listBox.SelectionModel.SelectedIndices);
        Assert.Equal(0, raised);
    }

    // A markup binding or a markup condition writes its value again at its own source, for example MarkupBase, each
    // time it refreshes. Such a write keeps SelectedIndex and is not a selection, so a multi-selection stays.
    [Fact]
    public void RewritingTheCurrentSelectedIndexAtAnEffectiveNonLocalSourceKeepsAMultiSelection()
    {
        ListBox listBox = new() { SelectionMode = SelectionMode.Multiple };
        listBox.SetItems(new[] { "a", "b", "c" });
        listBox.SelectionModel.Select(2);
        listBox.SelectionModel.Add(0);
        listBox.SetValue(Selector.SelectedIndexProperty, 2, UiPropertyValueSource.MarkupBase);
        listBox.ClearValue(Selector.SelectedIndexProperty);
        Assert.Equal(UiPropertyValueSource.MarkupBase, listBox.GetValueSource(Selector.SelectedIndexProperty));
        Assert.Equal([2, 0], listBox.SelectionModel.SelectedIndices);
        int raised = 0;
        listBox.SelectionChanged += (_, _) => raised++;

        listBox.SetValue(Selector.SelectedIndexProperty, 2, UiPropertyValueSource.MarkupBase);

        Assert.Equal([2, 0], listBox.SelectionModel.SelectedIndices);
        Assert.Equal(0, raised);
    }

    [Fact]
    public void TwoWayBoundSelectedIndexKeepsAMultiSelectionWhenTheListBoxIsAttachedAgain()
    {
        UIRoot root = new(100, 100);
        ListBox listBox = new() { SelectionMode = SelectionMode.Multiple };
        listBox.SetItems(new[] { "a", "b", "c" });
        root.VisualChildren.Add(listBox);
        ObservableValue<int> index = new(-1);
        using UiPropertyBinding<int> binding = BindingOperations.BindTwoWay(listBox, Selector.SelectedIndexProperty, index);
        listBox.SelectionModel.Select(2);
        listBox.SelectionModel.Add(0);
        Assert.Equal(2, index.Value);
        int raised = 0;
        listBox.SelectionChanged += (_, _) => raised++;

        root.VisualChildren.Remove(listBox);
        root.VisualChildren.Add(listBox);

        Assert.Equal([2, 0], listBox.SelectionModel.SelectedIndices);
        Assert.Equal(0, raised);
    }

    [Fact]
    public void RealizedContainerOfSelectedItemStaysSelectedAfterInsertBeforeIt()
    {
        UIRoot root = new(100, 100);
        ListBox listBox = new();
        listBox.SetItems(new[] { "a", "b", "c" });
        root.VisualChildren.Add(listBox);
        root.ProcessFrame();
        listBox.SelectedIndex = 1;

        listBox.Items.Insert(0, "z");
        root.ProcessFrame();

        Assert.Equal(4, listBox.RealizedItemCount);
        foreach (UIElement container in listBox.ItemContainerGenerator.RealizedContainers.Values)
        {
            ListBoxItem item = Assert.IsType<ListBoxItem>(container);
            Assert.Equal(Equals(item.Item, "b"), item.IsSelected);
        }
    }

    [Fact]
    public void RemovingSelectedListBoxItemKeepsEachItemContent()
    {
        UIRoot root = new(100, 100);
        ListBoxItem first = new() { Content = "a" };
        ListBoxItem second = new() { Content = "b" };
        ListBoxItem third = new() { Content = "c" };
        ListBox listBox = new();
        listBox.SetItems(new[] { first, second, third });
        root.VisualChildren.Add(listBox);
        listBox.Measure(new MeasureContext(new LayoutSize(100, 100)));
        listBox.Arrange(new ArrangeContext(new LayoutRect(0, 0, 100, 100)));
        listBox.SelectedIndex = 1;

        listBox.Items.RemoveAt(1);
        listBox.Measure(new MeasureContext(new LayoutSize(100, 100)));
        listBox.Arrange(new ArrangeContext(new LayoutRect(0, 0, 100, 100)));

        Assert.Equal(-1, listBox.SelectedIndex);
        Assert.Equal("a", first.Content);
        Assert.Equal("c", third.Content);
        Assert.False(first.IsSelected);
        Assert.False(third.IsSelected);
    }

    [Fact]
    public void SelectingAfterRemovingWrappedElementItemBeforeLayoutKeepsEachItemInItsOwnContainer()
    {
        UIElement removed = new FixedElement(new LayoutSize(20, 10));
        ListBox listBox = new();
        listBox.SetItems(new[] { removed, new FixedElement(new LayoutSize(20, 10)), new FixedElement(new LayoutSize(20, 10)) });
        listBox.ItemContainerGenerator.Realize();

        listBox.Items.RemoveAt(0);
        listBox.SelectedIndex = 0;
        listBox.ItemContainerGenerator.Realize();

        AssertRealizedContainersShowTheirItems(listBox);
        Assert.Null(removed.LogicalParent);
    }

    [Fact]
    public void SelectingAfterInsertingWrappedElementItemBeforeLayoutKeepsEachItemInItsOwnContainer()
    {
        ListBox listBox = new();
        listBox.SetItems(new[] { new FixedElement(new LayoutSize(20, 10)), new FixedElement(new LayoutSize(20, 10)) });
        listBox.ItemContainerGenerator.Realize();

        listBox.Items.Insert(0, new FixedElement(new LayoutSize(20, 10)));
        listBox.SelectedIndex = 1;
        listBox.ItemContainerGenerator.Realize();

        AssertRealizedContainersShowTheirItems(listBox);
    }

    [Fact]
    public void SelectingAfterRemovingTabItemBeforeLayoutRealizesTheTabNowAtThatIndex()
    {
        TabItem removed = new() { Content = "t0" };
        TabItem first = new() { Content = "t1" };
        TabItem second = new() { Content = "t2" };
        TabControl tabs = new();
        tabs.SetItems(new[] { removed, first, second });
        tabs.ItemContainerGenerator.Realize();

        tabs.Items.RemoveAt(0);
        tabs.SelectedIndex = 0;
        tabs.ItemContainerGenerator.Realize();

        AssertRealizedContainersShowTheirItems(tabs);
        Assert.Same(first, tabs.ItemContainerGenerator.RealizedContainers[0]);
        Assert.True(first.IsSelected);
        Assert.Equal("t0", removed.Content);
        Assert.DoesNotContain(removed, tabs.ItemContainerGenerator.RealizedContainers.Values);
    }

    [Fact]
    public void SelectingAfterRemovingTabItemOfMeasuredTabControlBeforeLayoutRealizesTheTabNowAtThatIndex()
    {
        UIRoot root = new(300, 300);
        TabItem removed = new() { Content = "t0" };
        TabItem first = new() { Content = "t1" };
        TabControl tabs = new();
        tabs.SetItems(new[] { removed, first, new TabItem { Content = "t2" } });
        root.VisualChildren.Add(tabs);
        tabs.Measure(new MeasureContext(new LayoutSize(300, 300)));

        tabs.Items.RemoveAt(0);
        tabs.SelectedIndex = 0;
        Assert.Same(first, tabs.SelectedTabItem);
        tabs.Measure(new MeasureContext(new LayoutSize(300, 300)));

        AssertRealizedContainersShowTheirItems(tabs);
        Assert.True(first.IsSelected);
        Assert.Equal("t0", removed.Content);
        Assert.Null(removed.VisualParent);
    }

    [Fact]
    public void SelectingAfterInsertingTabItemBeforeLayoutKeepsEachTabItsOwnContent()
    {
        TabItem inserted = new() { Content = "n" };
        TabItem first = new() { Content = "t0" };
        TabItem second = new() { Content = "t1" };
        TabControl tabs = new();
        tabs.SetItems(new[] { first, second });
        tabs.ItemContainerGenerator.Realize();

        tabs.Items.Insert(0, inserted);
        tabs.SelectedIndex = 0;
        tabs.ItemContainerGenerator.Realize();

        AssertRealizedContainersShowTheirItems(tabs);
        Assert.Equal("n", inserted.Content);
        Assert.Equal("t0", first.Content);
        Assert.Equal("t1", second.Content);
    }

    [Fact]
    public void SelectingAfterRemovingItemAtUnknownPositionBeforeLayoutKeepsEachItemInItsOwnContainer()
    {
        UIElement removed = new FixedElement(new LayoutSize(20, 10));
        IndexlessNotifyingList items = new([removed, new FixedElement(new LayoutSize(20, 10)), new FixedElement(new LayoutSize(20, 10))]);
        ListBox listBox = new() { ItemsSource = items };
        listBox.ItemContainerGenerator.Realize();

        items.RemoveWithoutIndex(removed);
        listBox.SelectedIndex = 0;
        listBox.ItemContainerGenerator.Realize();

        AssertRealizedContainersShowTheirItems(listBox);
    }

    [Fact]
    public void MovingSelfContainerItemsKeepsEachRealizedContainerOnItsItem()
    {
        ObservableCollection<ListBoxItem> items = [new() { Content = "a" }, new() { Content = "b" }, new() { Content = "c" }];
        ListBox listBox = new() { ItemsSource = items };
        listBox.ItemContainerGenerator.Realize();
        listBox.SelectedIndex = 0;

        items.Move(0, 2);
        listBox.ItemContainerGenerator.Realize();

        AssertRealizedContainersShowTheirItems(listBox);
        Assert.Equal(2, listBox.SelectedIndex);
    }

    [Fact]
    public void ClickOnReplacedListBoxItemInsertedBackAtItsIndexSelectsIt()
    {
        ListBoxItem a = new() { Content = new FixedElement(new LayoutSize(20, 10)) };
        ListBox listBox = new();
        listBox.SetItems(new object[] { a, "str" });
        ArrangeInRoot(listBox);

        listBox.Items[0] = "x";
        listBox.Items.Insert(0, a);
        listBox.Root!.ProcessFrame();
        Click(listBox, 50, 5);

        Assert.Equal(0, listBox.SelectedIndex);
        AssertRealizedContainersShowTheirItems(listBox);
    }

    [Fact]
    public void NestedSelectionChangeReportsItemItReplaced()
    {
        RedirectingListBox listBox = new();
        listBox.SetItems(new[] { "a", "b", "c" });
        listBox.SelectedIndex = 0;
        List<SelectionChangedEventArgs> raised = [];
        listBox.SelectionChanged += (_, args) => raised.Add(args);

        listBox.SelectedIndex = 1;

        Assert.Equal(2, listBox.SelectedIndex);
        SelectionChangedEventArgs nested = Assert.Single(raised, args => args.AddedItems.Contains("c"));
        Assert.Equal(["b"], nested.RemovedItems);
        SelectionChangedEventArgs outer = Assert.Single(raised, args => args.AddedItems.Contains("b"));
        Assert.Equal(["a"], outer.RemovedItems);
    }

    // SetValue(SelectedIndexProperty, 1), the path of a binding, an aspect, or application code, behaves like the setter.
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void NestedSelectionChangeWhosePreparationThrowsCompletesOuterChangeBeforePropagating(bool setValue)
    {
        RedirectingListBox listBox = new();
        CreateArrangedTextListBox(listBox: listBox);
        listBox.SelectedIndex = 0;
        listBox.Root!.ProcessFrame();
        ListBoxItem first = (ListBoxItem)listBox.ItemContainerGenerator.RealizedContainers[0];
        ListBoxItem third = (ListBoxItem)listBox.ItemContainerGenerator.RealizedContainers[2];
        List<string> raised = [];
        bool firstSelectedWhenOuterRaised = true;
        listBox.SelectionChanged += (_, args) =>
        {
            raised.Add($"[{string.Join(",", args.RemovedItems)}]->[{string.Join(",", args.AddedItems)}]");
            firstSelectedWhenOuterRaised = first.IsSelected;
        };

        listBox.FailFor = "b";
        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() =>
        {
            if (setValue)
            {
                listBox.SetValue(Selector.SelectedIndexProperty, 1);
            }
            else
            {
                listBox.SelectedIndex = 1;
            }
        });

        Assert.Equal("content preparation failed", exception.Message);
        Assert.Equal(2, listBox.SelectedIndex);
        Assert.False(first.IsSelected);
        Assert.True(third.IsSelected);
        Assert.Equal(["[b]->[c]", "[a]->[b]"], raised);
        Assert.False(firstSelectedWhenOuterRaised);
    }

    [Fact]
    public void ReusedContainerClickSelectsCurrentSelectorOnly()
    {
        ListBoxItem shared = new();
        ListBox first = new();
        first.SetItems(new[] { shared });
        first.ItemContainerGenerator.GetOrCreate(0);
        first.ItemContainerGenerator.Recycle(0);

        ListBox second = new();
        second.SetItems(new[] { shared });
        second.ItemContainerGenerator.GetOrCreate(0);
        // A ListBox selects only a row that can take the focus (#675), which needs a row in a root.
        UIRoot root = new(100, 100);
        root.VisualChildren.Add(second);
        root.ProcessFrame();

        MouseButtonEventArgs args = new(InputEvents.MouseUpEvent, shared, InputMouseButton.Left, 0, 0, 1);
        foreach (RoutedEventHandler handler in shared.Handlers.GetHandlers(InputEvents.MouseUpEvent))
        {
            handler(shared, args);
        }

        Assert.Equal(-1, first.SelectedIndex);
        Assert.Equal(0, second.SelectedIndex);
    }

    private static ListBox CreateArrangedListBox()
    {
        UIRoot root = new(100, 100);
        ListBox listBox = new();
        listBox.SetItems(new UIElement[]
        {
            new FixedElement(new LayoutSize(20, 10)),
            new FixedElement(new LayoutSize(20, 10))
        });
        root.VisualChildren.Add(listBox);
        listBox.Measure(new MeasureContext(new LayoutSize(100, 100)));
        listBox.Arrange(new ArrangeContext(new LayoutRect(0, 0, 100, 100)));
        return listBox;
    }

    // Shows "a", "b" and "c" in rows at y = 0, 10 and 20. Each row gets a fixed height, so the row positions do
    // not depend on the height of the text font.
    private static ListBox CreateArrangedTextListBox(IEnumerable? itemsSource = null, ListBox? listBox = null)
    {
        UIRoot root = new(100, 100);
        listBox ??= new ListBox();
        if (itemsSource is null)
        {
            listBox.SetItems(new[] { "a", "b", "c" });
        }
        else
        {
            listBox.ItemsSource = itemsSource;
        }

        root.VisualChildren.Add(listBox);
        root.ProcessFrame();
        foreach (UIElement container in listBox.ItemContainerGenerator.RealizedContainers.Values)
        {
            container.Height = 10;
        }

        root.ProcessFrame();
        return listBox;
    }

    private static UIElement[] CreateFixedItems(int count)
    {
        return [.. Enumerable.Range(0, count).Select(_ => (UIElement)new FixedElement(new LayoutSize(20, 10)))];
    }

    private static void ArrangeInRoot(ListBox listBox)
    {
        UIRoot root = new(100, 100);
        root.VisualChildren.Add(listBox);
        listBox.Measure(new MeasureContext(new LayoutSize(100, 100)));
        listBox.Arrange(new ArrangeContext(new LayoutRect(0, 0, 100, 100)));
    }

    private static void Click(ListBox listBox, float x, float y)
    {
        ElementInputBridge bridge = new();
        bridge.Dispatch(listBox.Root!, PointerFrame(x, y, currentDown: true));
        bridge.Dispatch(listBox.Root!, PointerFrame(x, y, previousDown: true));
    }

    // Every item is realized exactly once, at its current index, and its container shows that item.
    private static void AssertRealizedContainersShowTheirItems(Selector selector)
    {
        IReadOnlyDictionary<int, UIElement> realized = selector.ItemContainerGenerator.RealizedContainers;
        Assert.Equal(Enumerable.Range(0, selector.ItemCount), realized.Keys.Order());
        foreach ((int index, UIElement container) in realized)
        {
            object? item = selector.GetItemAt(index);
            ISelectableItemContainer selectable = Assert.IsAssignableFrom<ISelectableItemContainer>(container);
            Assert.Equal(index, ItemContainerGenerator.GetItemIndex(container));
            Assert.Same(item, ItemContainerGenerator.GetItem(container));
            Assert.Equal(index, selectable.ItemIndex);
            Assert.Same(item, selectable.Item);
            Assert.Equal(index == selector.SelectedIndex, selectable.IsSelected);
            if (!ReferenceEquals(container, item))
            {
                Assert.Same(item, Assert.IsAssignableFrom<ContentControl>(container).Content);
            }
        }
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

    // Reacts to SelectedIndex 1 by selecting index 2 inside the property-change notification, and throws while it
    // prepares the content of a container for FailFor.
    private sealed class RedirectingListBox : ListBox
    {
        public object? FailFor { get; set; }

        protected override void OnPropertyChanged(UiPropertyChangedEventArgs args)
        {
            base.OnPropertyChanged(args);
            if (ReferenceEquals(args.Property, SelectedIndexProperty) && SelectedIndex == 1)
            {
                SelectedIndex = 2;
            }
        }

        protected override void PrepareItemContent(UIElement container, int index, object? item)
        {
            if (FailFor is not null && Equals(item, FailFor))
            {
                throw new InvalidOperationException("content preparation failed");
            }

            base.PrepareItemContent(container, index, item);
        }
    }

    // Throws while it prepares the content of a container for FailFor.
    private sealed class FailingContentListBox : ListBox
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

    // Reports a removal without its index, as NotifyCollectionChangedEventArgs allows.
    private sealed class IndexlessNotifyingList(IEnumerable<object> items) : IEnumerable, INotifyCollectionChanged
    {
        private readonly List<object> items = [.. items];

        public event NotifyCollectionChangedEventHandler? CollectionChanged;

        public void RemoveWithoutIndex(object item)
        {
            items.Remove(item);
            CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, item));
        }

        public IEnumerator GetEnumerator() => items.GetEnumerator();
    }

    private sealed class FixedElement(LayoutSize size) : UIElement
    {
        protected override LayoutSize MeasureCore(MeasureContext context)
        {
            return size;
        }
    }
}
