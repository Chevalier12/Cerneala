using System.Collections.ObjectModel;
using Cerneala.UI.Aspect;
using Cerneala.UI.Controls;
using Cerneala.UI.Controls.Templates;
using Cerneala.UI.Elements;
using Cerneala.UI.Layout;
using Cerneala.UI.Layout.Virtualization;

namespace Cerneala.Tests.Controls;

public sealed class ItemContainerGeneratorTests
{
    [Fact]
    public void GeneratorPreservesContainerIdentityInsideSameWindow()
    {
        ItemsControl control = new();
        control.SetItems(new[] { "a", "b", "c" });

        IReadOnlyList<Cerneala.UI.Elements.UIElement> first = control.ItemContainerGenerator.Realize(new RealizationWindow(0, 2));
        IReadOnlyList<Cerneala.UI.Elements.UIElement> second = control.ItemContainerGenerator.Realize(new RealizationWindow(0, 2));

        Assert.Same(first[0], second[0]);
        Assert.Same(first[1], second[1]);
    }

    [Fact]
    public void GeneratorPreparesOnlyNewContainersWhenRealizationWindowsOverlap()
    {
        CountingItemsControl control = new();
        control.SetItems(new[] { "a", "b", "c", "d" });

        control.ItemContainerGenerator.Realize(new RealizationWindow(0, 3));
        control.ItemContainerGenerator.Realize(new RealizationWindow(1, 4));

        Assert.Equal(4, control.PrepareCount);
    }

    [Fact]
    public void GeneratorRecyclesUnrealizedContainersAndClearsStaleState()
    {
        ListBox listBox = new();
        listBox.SetItems(new[] { "a", "b", "c" });
        listBox.SelectedIndex = 0;
        IReadOnlyList<Cerneala.UI.Elements.UIElement> first = listBox.ItemContainerGenerator.Realize(new RealizationWindow(0, 1));
        ListBoxItem oldItem = Assert.IsType<ListBoxItem>(first[0]);
        Assert.True(oldItem.IsSelected);

        IReadOnlyList<Cerneala.UI.Elements.UIElement> second = listBox.ItemContainerGenerator.Realize(new RealizationWindow(1, 2));

        ListBoxItem recycled = Assert.IsType<ListBoxItem>(second[0]);
        Assert.Same(oldItem, recycled);
        Assert.Equal(1, recycled.ItemIndex);
        Assert.Equal("b", recycled.Item);
        Assert.False(recycled.IsSelected);
        Assert.Equal(0, listBox.ItemContainerGenerator.RecyclePool.Count);
    }

    [Fact]
    public void GeneratorDoesNotRecycleUiElementItemAsDifferentItemContainer()
    {
        ItemsControl control = new();
        FixedElement first = new("first");
        FixedElement second = new("second");
        control.SetItems(new UIElement[] { first, second });

        IReadOnlyList<UIElement> firstWindow = control.ItemContainerGenerator.Realize(new RealizationWindow(0, 1));
        IReadOnlyList<UIElement> secondWindow = control.ItemContainerGenerator.Realize(new RealizationWindow(1, 2));

        Assert.Same(first, firstWindow[0]);
        Assert.Same(second, secondWindow[0]);
        Assert.Same(second, control.ItemContainerGenerator.RealizedContainers[1]);
    }

    [Fact]
    public void GeneratorReplacesRealizedUiElementContainerWhenItemChanges()
    {
        ItemsControl control = new();
        FixedElement first = new("first");
        FixedElement second = new("second");
        control.SetItems(new UIElement[] { first });
        control.ItemContainerGenerator.Realize(new RealizationWindow(0, 1));

        control.Items[0] = second;
        IReadOnlyList<UIElement> updated = control.ItemContainerGenerator.Realize(new RealizationWindow(0, 1));

        Assert.Same(second, updated[0]);
        Assert.Same(second, control.ItemContainerGenerator.RealizedContainers[0]);
    }

    [Fact]
    public void ListBoxSwapOfTwoWrappedElementItemsThroughLayoutShowsEachElementInItsNewContainer()
    {
        UIRoot root = new(100, 100);
        FixedElement e0 = new("e0");
        FixedElement e1 = new("e1");
        ListBox listBox = new();
        listBox.SetItems(new UIElement[] { e0, e1 });
        root.VisualChildren.Add(listBox);
        Layout(listBox);

        object? swapped = listBox.Items[0];
        listBox.Items[0] = listBox.Items[1];
        listBox.Items[1] = swapped;
        Layout(listBox);

        Assert.Same(e1, Assert.IsType<ListBoxItem>(listBox.ItemContainerGenerator.RealizedContainers[0]).Content);
        Assert.Same(e0, Assert.IsType<ListBoxItem>(listBox.ItemContainerGenerator.RealizedContainers[1]).Content);
    }

    [Fact]
    public void ListBoxItemsSourceSwapOfTwoWrappedElementItemsThroughLayoutShowsEachElementInItsNewContainer()
    {
        UIRoot root = new(100, 100);
        FixedElement e0 = new("e0");
        FixedElement e1 = new("e1");
        ObservableCollection<UIElement> source = [e0, e1];
        ListBox listBox = new() { ItemsSource = source };
        root.VisualChildren.Add(listBox);
        Layout(listBox);

        (source[0], source[1]) = (source[1], source[0]);
        Layout(listBox);

        Assert.Same(e1, Assert.IsType<ListBoxItem>(listBox.ItemContainerGenerator.RealizedContainers[0]).Content);
        Assert.Same(e0, Assert.IsType<ListBoxItem>(listBox.ItemContainerGenerator.RealizedContainers[1]).Content);
    }

    [Fact]
    public void ItemsSourceInsertKeepsContentOfButtonItemsThatAreTheirOwnContainers()
    {
        Button one = new() { Content = "One" };
        Button two = new() { Content = "Two" };
        ObservableCollection<Button> source = [one, two];
        ItemsControl control = new() { ItemsSource = source };
        MeasureContext context = new(new LayoutSize(200, 200));
        control.Measure(context);

        source.Insert(0, new Button { Content = "Zero" });
        control.Measure(context);

        Assert.Equal(3, control.ItemsPresenter.LayoutPanelRoot!.VisualChildren.Count);
        Assert.Equal("One", one.Content);
        Assert.Equal("Two", two.Content);
    }

    [Fact]
    public void TabControlRemoveAtKeepsContentOfRemainingTabItemAndClearsItsItemState()
    {
        TabItem first = new() { Content = "First page" };
        TabItem second = new() { Content = "Second page" };
        TabControl tabControl = new();
        tabControl.SetItems(new[] { first, second });
        tabControl.ItemContainerGenerator.Realize();

        tabControl.Items.RemoveAt(0);
        tabControl.ItemContainerGenerator.Realize();

        Assert.Equal("Second page", second.Content);
        Assert.Equal("First page", first.Content);
        Assert.Equal(-1, first.ItemIndex);
        Assert.Null(first.Item);
        Assert.False(first.IsSelected);
        Assert.Equal(0, second.ItemIndex);
        Assert.Same(second, second.Item);
    }

    [Fact]
    public void ListBoxRemoveAtFirstOfThreeMovesShiftedListBoxItemsToTheirNewIndexes()
    {
        ListBoxItem a = new() { Content = "a" };
        ListBoxItem b = new() { Content = "b" };
        ListBoxItem c = new() { Content = "c" };
        ClearRecordingListBox listBox = new();
        listBox.SetItems(new[] { a, b, c });
        listBox.SelectedIndex = 1;
        listBox.ItemContainerGenerator.Realize();

        listBox.Items.RemoveAt(0);
        listBox.ItemContainerGenerator.Realize();

        Assert.Equal(new[] { 0, 1 }, listBox.ItemContainerGenerator.RealizedContainers.Keys.Order());
        Assert.Same(b, listBox.ItemContainerGenerator.RealizedContainers[0]);
        Assert.Same(c, listBox.ItemContainerGenerator.RealizedContainers[1]);
        AssertItemState(b, 0, isSelected: true);
        AssertItemState(c, 1, isSelected: false);
        Assert.Equal(new UIElement[] { a }, listBox.ClearedContainers);
    }

    [Fact]
    public void UnnotifiedGeneratorMovesShiftedListBoxItemsToTheirNewIndexesOnRealize()
    {
        ListBoxItem a = new() { Content = "a" };
        ListBoxItem b = new() { Content = "b" };
        ListBoxItem c = new() { Content = "c" };
        ClearRecordingListBox listBox = new();
        listBox.SetItems(new[] { a, b, c });
        ItemContainerGenerator generator = new(listBox);
        generator.Realize();

        listBox.Items.RemoveAt(0);
        Assert.Equal(new[] { 0, 1, 2 }, generator.RealizedContainers.Keys.Order());
        generator.Realize();

        Assert.Equal(new[] { 0, 1 }, generator.RealizedContainers.Keys.Order());
        Assert.Same(b, generator.RealizedContainers[0]);
        Assert.Same(c, generator.RealizedContainers[1]);
        AssertItemState(b, 0, isSelected: false);
        AssertItemState(c, 1, isSelected: false);
        Assert.Equal(new UIElement[] { a }, listBox.ClearedContainers);
    }

    [Fact]
    public void ListBoxRemoveAtFirstOfThreeThroughMeasureKeepsShiftedListBoxItemsInThePanel()
    {
        ListBoxItem a = new() { Content = "a" };
        ListBoxItem b = new() { Content = "b" };
        ListBoxItem c = new() { Content = "c" };
        ListBox listBox = new();
        listBox.SetItems(new[] { a, b, c });
        MeasureContext context = new(new LayoutSize(200, 200));
        listBox.Measure(context);

        listBox.Items.RemoveAt(0);
        listBox.Measure(context);

        Assert.Equal(new UIElement[] { b, c }, listBox.ItemsPresenter.LayoutPanelRoot!.VisualChildren.ToArray());
        AssertItemState(b, 0, isSelected: false);
        AssertItemState(c, 1, isSelected: false);
        Assert.Null(a.VisualParent);
        AssertItemState(a, -1, isSelected: false, expectedItem: null);
    }

    [Fact]
    public void ItemsSourceMoveOfTwoMovesBothListBoxItemsToTheirNewIndexes()
    {
        ListBoxItem a = new() { Content = "a" };
        ListBoxItem b = new() { Content = "b" };
        ObservableCollection<ListBoxItem> source = [a, b];
        ClearRecordingListBox listBox = new() { ItemsSource = source };
        listBox.ItemContainerGenerator.Realize();

        source.Move(0, 1);
        listBox.ItemContainerGenerator.Realize();

        Assert.Same(b, listBox.ItemContainerGenerator.RealizedContainers[0]);
        Assert.Same(a, listBox.ItemContainerGenerator.RealizedContainers[1]);
        AssertItemState(b, 0, isSelected: false);
        AssertItemState(a, 1, isSelected: false);
        Assert.Empty(listBox.ClearedContainers);
    }

    [Fact]
    public void ItemsSourceMoveOfThreeThroughMeasureKeepsEveryListBoxItemRealizedAtItsNewIndex()
    {
        ListBoxItem a = new() { Content = "a" };
        ListBoxItem b = new() { Content = "b" };
        ListBoxItem c = new() { Content = "c" };
        ObservableCollection<ListBoxItem> source = [a, b, c];
        ClearRecordingListBox listBox = new() { ItemsSource = source };
        MeasureContext context = new(new LayoutSize(200, 200));
        listBox.Measure(context);

        source.Move(0, 2);
        listBox.Measure(context);

        Assert.Equal(new UIElement[] { b, c, a }, listBox.ItemsPresenter.LayoutPanelRoot!.VisualChildren.ToArray());
        AssertItemState(b, 0, isSelected: false);
        AssertItemState(c, 1, isSelected: false);
        AssertItemState(a, 2, isSelected: false);
        Assert.Empty(listBox.ClearedContainers);
    }

    [Fact]
    public void ListBoxReplacingListBoxItemWithDataItemRealizesGeneratedContainerForDataItem()
    {
        ListBoxItem a = new() { Content = "A" };
        ListBoxItem c = new() { Content = "C" };
        ListBox listBox = new();
        listBox.SetItems(new object[] { a, "str", c });
        listBox.ItemContainerGenerator.Realize();

        listBox.Items[0] = "new";
        listBox.ItemContainerGenerator.Realize();

        AssertDataItemContainer(listBox.ItemContainerGenerator.RealizedContainers[0], a, 0, "new");
        AssertRemovedListBoxItem(a);
        Assert.Equal(0, listBox.ItemContainerGenerator.RecyclePool.Count);
    }

    [Fact]
    public void ListBoxReplacingListBoxItemWithDataItemRealizesGeneratedContainerInsideWindow()
    {
        ListBoxItem a = new() { Content = "A" };
        ListBoxItem c = new() { Content = "C" };
        ListBox listBox = new();
        listBox.SetItems(new object[] { a, "str", c });
        listBox.ItemContainerGenerator.Realize(new RealizationWindow(0, 2));

        listBox.Items[0] = "new";
        IReadOnlyList<UIElement> containers = listBox.ItemContainerGenerator.Realize(new RealizationWindow(0, 2));

        AssertDataItemContainer(containers[0], a, 0, "new");
        AssertRemovedListBoxItem(a);
        Assert.Equal(0, listBox.ItemContainerGenerator.RecyclePool.Count);
    }

    [Fact]
    public void ListBoxReplacingListBoxItemWithDataItemThroughMeasureShowsGeneratedContainerInThePanel()
    {
        ListBoxItem a = new() { Content = "A" };
        ListBoxItem c = new() { Content = "C" };
        ListBox listBox = new();
        listBox.SetItems(new object[] { a, "str", c });
        MeasureContext context = new(new LayoutSize(200, 200));
        listBox.Measure(context);

        listBox.Items[0] = "new";
        listBox.Measure(context);

        IReadOnlyList<UIElement> children = listBox.ItemsPresenter.LayoutPanelRoot!.VisualChildren.ToArray();
        Assert.Equal(3, children.Count);
        AssertDataItemContainer(children[0], a, 0, "new");
        Assert.Same(c, children[2]);
        AssertRemovedListBoxItem(a);
    }

    [Fact]
    public void UnnotifiedGeneratorDoesNotPrepareRemovedListBoxItemForDataItemAtItsOldIndex()
    {
        ListBoxItem a = new() { Content = "A" };
        ListBoxItem c = new() { Content = "C" };
        ClearRecordingListBox listBox = new();
        listBox.SetItems(new object[] { a, "str", c });
        ItemContainerGenerator generator = new(listBox);
        generator.Realize();

        listBox.Items.RemoveAt(0);
        generator.Realize();

        Assert.Equal(new[] { 0, 1 }, generator.RealizedContainers.Keys.Order());
        AssertDataItemContainer(generator.RealizedContainers[0], a, 0, "str");
        Assert.Same(c, generator.RealizedContainers[1]);
        AssertRemovedListBoxItem(a);
        Assert.Contains(a, listBox.ClearedContainers);
        Assert.Equal(0, generator.RecyclePool.Count);
    }

    [Fact]
    public void ItemsSourceRemovedListBoxItemIsNotReusedForLaterDataItem()
    {
        ListBoxItem a = new() { Content = "A" };
        ListBoxItem c = new() { Content = "C" };
        ObservableCollection<object> source = [a, "str", c];
        ListBox listBox = new() { ItemsSource = source };
        listBox.ItemContainerGenerator.Realize(new RealizationWindow(0, 3));

        source.RemoveAt(0);
        listBox.ItemContainerGenerator.Realize(new RealizationWindow(0, 2));
        source.RemoveAt(0);
        listBox.ItemContainerGenerator.Realize(new RealizationWindow(0, 1));
        source.Add("x");
        listBox.ItemContainerGenerator.Realize(new RealizationWindow(0, 2));

        Assert.Same(c, listBox.ItemContainerGenerator.RealizedContainers[0]);
        AssertDataItemContainer(listBox.ItemContainerGenerator.RealizedContainers[1], a, 1, "x");
        AssertRemovedListBoxItem(a);
    }

    [Fact]
    public void ListBoxReplacedListBoxItemInsertedBackAtItsIndexIsRealizedOnlyThere()
    {
        ListBoxItem a = new() { Content = "A" };
        ClearRecordingListBox listBox = new();
        listBox.SetItems(new object[] { a, "str" });
        listBox.ItemContainerGenerator.Realize();

        listBox.Items[0] = "x";
        listBox.Items.Insert(0, a);
        listBox.ItemContainerGenerator.Realize();

        AssertReplacedListBoxItemInsertedBackAtIndexZero(listBox, a);
    }

    [Fact]
    public void ItemsSourceReplacedListBoxItemInsertedBackAtItsIndexIsRealizedOnlyThere()
    {
        ListBoxItem a = new() { Content = "A" };
        ObservableCollection<object> source = [a, "str"];
        ClearRecordingListBox listBox = new() { ItemsSource = source };
        listBox.ItemContainerGenerator.Realize();

        source[0] = "x";
        source.Insert(0, a);
        listBox.ItemContainerGenerator.Realize();

        AssertReplacedListBoxItemInsertedBackAtIndexZero(listBox, a);
    }

    [Fact]
    public void ListBoxSelectedReplacedListBoxItemInsertedBackAtItsIndexIsNotSelected()
    {
        ListBoxItem a = new() { Content = "A" };
        ClearRecordingListBox listBox = new();
        listBox.SetItems(new object[] { a, "str" });
        listBox.ItemContainerGenerator.Realize();
        listBox.SelectedIndex = 0;
        Assert.True(a.IsSelected);

        listBox.Items[0] = "x";
        listBox.Items.Insert(0, a);
        listBox.ItemContainerGenerator.Realize();

        Assert.Equal(-1, listBox.SelectedIndex);
        AssertReplacedListBoxItemInsertedBackAtIndexZero(listBox, a);
    }

    [Fact]
    public void ItemsSourceSelectedReplacedListBoxItemInsertedBackAtItsIndexIsNotSelected()
    {
        ListBoxItem a = new() { Content = "A" };
        ObservableCollection<object> source = [a, "str"];
        ClearRecordingListBox listBox = new() { ItemsSource = source };
        listBox.ItemContainerGenerator.Realize();
        listBox.SelectedIndex = 0;
        Assert.True(a.IsSelected);

        source[0] = "x";
        source.Insert(0, a);
        listBox.ItemContainerGenerator.Realize();

        Assert.Equal(-1, listBox.SelectedIndex);
        AssertReplacedListBoxItemInsertedBackAtIndexZero(listBox, a);
    }

    [Fact]
    public void ListBoxSelectedReplacedListBoxItemPutBackAtItsIndexIsNotSelected()
    {
        ListBoxItem a = new() { Content = "A" };
        ListBox listBox = new();
        listBox.SetItems(new object[] { a, "str" });
        listBox.ItemContainerGenerator.Realize();
        listBox.SelectedIndex = 0;
        Assert.True(a.IsSelected);

        listBox.Items[0] = "x";
        listBox.Items[0] = a;
        listBox.ItemContainerGenerator.Realize();

        Assert.Equal(-1, listBox.SelectedIndex);
        Assert.Same(a, listBox.ItemContainerGenerator.RealizedContainers[0]);
        AssertPreparedSelection(a, 0, a, isSelected: false);
    }

    [Fact]
    public void ItemsSourceSelectedReplacedListBoxItemPutBackAtItsIndexIsNotSelected()
    {
        ListBoxItem a = new() { Content = "A" };
        ObservableCollection<object> source = [a, "str"];
        ListBox listBox = new() { ItemsSource = source };
        listBox.ItemContainerGenerator.Realize();
        listBox.SelectedIndex = 0;
        Assert.True(a.IsSelected);

        source[0] = "x";
        source[0] = a;
        listBox.ItemContainerGenerator.Realize();

        Assert.Equal(-1, listBox.SelectedIndex);
        AssertPreparedSelection(a, 0, a, isSelected: false);
    }

    [Fact]
    public void ListBoxSelectedReplacedListBoxItemPutBackThroughMeasureIsNotSelected()
    {
        ListBoxItem a = new() { Content = "A" };
        ListBox listBox = new();
        listBox.SetItems(new object[] { a, "str" });
        MeasureContext context = new(new LayoutSize(200, 200));
        listBox.Measure(context);
        listBox.SelectedIndex = 0;
        Assert.True(a.IsSelected);

        listBox.Items[0] = "x";
        listBox.Items[0] = a;
        listBox.Measure(context);

        Assert.Equal(-1, listBox.SelectedIndex);
        Assert.Same(a, listBox.ItemsPresenter.LayoutPanelRoot!.VisualChildren[0]);
        AssertPreparedSelection(a, 0, a, isSelected: false);
    }

    [Fact]
    public void ListBoxSelectedReplacedDataItemPutBackAtItsIndexIsNotSelected()
    {
        ListBox listBox = new();
        listBox.SetItems(new object[] { "b", "c" });
        listBox.ItemContainerGenerator.Realize();
        listBox.SelectedIndex = 0;
        ListBoxItem container = Assert.IsType<ListBoxItem>(listBox.ItemContainerGenerator.RealizedContainers[0]);
        Assert.True(container.IsSelected);

        listBox.Items[0] = "x";
        listBox.Items[0] = "b";
        listBox.ItemContainerGenerator.Realize();

        Assert.Equal(-1, listBox.SelectedIndex);
        Assert.Same(container, listBox.ItemContainerGenerator.RealizedContainers[0]);
        AssertPreparedSelection(container, 0, "b", isSelected: false);
    }

    [Fact]
    public void ListBoxSelectedReplacedDataItemPutBackThroughMeasureIsNotSelected()
    {
        ListBox listBox = new();
        listBox.SetItems(new object[] { "b", "c" });
        MeasureContext context = new(new LayoutSize(200, 200));
        listBox.Measure(context);
        listBox.SelectedIndex = 0;
        ListBoxItem container = Assert.IsType<ListBoxItem>(listBox.ItemContainerGenerator.RealizedContainers[0]);
        Assert.True(container.IsSelected);

        listBox.Items[0] = "x";
        listBox.Items[0] = "b";
        listBox.Measure(context);

        Assert.Equal(-1, listBox.SelectedIndex);
        Assert.Same(container, listBox.ItemsPresenter.LayoutPanelRoot!.VisualChildren[0]);
        AssertPreparedSelection(container, 0, "b", isSelected: false);
    }

    [Fact]
    public void ListBoxReplacedListBoxItemPutBackAndSelectedAgainIsSelected()
    {
        ListBoxItem a = new() { Content = "A" };
        ListBox listBox = new();
        listBox.SetItems(new object[] { a, "str" });
        listBox.ItemContainerGenerator.Realize();
        listBox.SelectedIndex = 0;

        listBox.Items[0] = "x";
        listBox.Items[0] = a;
        listBox.SelectedIndex = 0;
        listBox.ItemContainerGenerator.Realize();

        Assert.Equal(0, listBox.SelectedIndex);
        AssertPreparedSelection(a, 0, a, isSelected: true);
    }

    [Fact]
    public void ListBoxItemItemIsNotWrappedInPooledGeneratedContainer()
    {
        ListBoxItem a = new() { Content = "A" };
        ListBox listBox = new();
        listBox.SetItems(new object[] { "str", a });
        UIElement generated = listBox.ItemContainerGenerator.Realize(new RealizationWindow(0, 1))[0];

        UIElement second = listBox.ItemContainerGenerator.Realize(new RealizationWindow(1, 2))[0];
        UIElement third = listBox.ItemContainerGenerator.Realize(new RealizationWindow(1, 2))[0];

        Assert.Same(a, second);
        Assert.Same(a, third);
        Assert.Equal("A", a.Content);
        Assert.Null(((ListBoxItem)generated).Content);
        Assert.Equal(1, listBox.ItemContainerGenerator.RecyclePool.Count);
    }

    [Fact]
    public void DerivedListBoxItemIsNotWrappedInPooledContainerWhenSubclassReturnsBaseContainerType()
    {
        DerivedListBoxItem a = new() { Content = "A" };
        BaseContainerTypeListBox listBox = new();
        listBox.SetItems(new object[] { "str", a });
        UIElement generated = listBox.ItemContainerGenerator.Realize(new RealizationWindow(0, 1))[0];

        UIElement second = listBox.ItemContainerGenerator.Realize(new RealizationWindow(1, 2))[0];
        UIElement third = listBox.ItemContainerGenerator.Realize(new RealizationWindow(1, 2))[0];

        Assert.Same(a, second);
        Assert.Same(a, third);
        Assert.Equal("A", a.Content);
        Assert.Equal(1, a.ItemIndex);
        Assert.Null(((ListBoxItem)generated).Content);
        Assert.Equal(1, listBox.ItemContainerGenerator.RecyclePool.Count);
    }

    [Fact]
    public void ReplacingDataItemWithDerivedListBoxItemRealizesItWhenSubclassReturnsBaseContainerType()
    {
        DerivedListBoxItem a = new() { Content = "A" };
        BaseContainerTypeListBox listBox = new();
        listBox.SetItems(new object[] { "str", "b" });
        UIElement generated = listBox.ItemContainerGenerator.Realize()[0];

        listBox.Items[0] = a;
        UIElement realized = listBox.ItemContainerGenerator.Realize()[0];

        Assert.Same(a, realized);
        Assert.Equal("A", a.Content);
        Assert.Equal(0, a.ItemIndex);
        Assert.Null(((ListBoxItem)generated).Content);
        Assert.Equal(1, listBox.ItemContainerGenerator.RecyclePool.Count);
    }

    [Fact]
    public void ListBoxReplacingDataItemWithListBoxItemThroughMeasureShowsListBoxItemInThePanel()
    {
        ListBoxItem a = new() { Content = "A" };
        ListBox listBox = new();
        listBox.SetItems(new object[] { "str", "b" });
        MeasureContext context = new(new LayoutSize(200, 200));
        listBox.Measure(context);

        listBox.Items[0] = a;
        listBox.Measure(context);

        IReadOnlyList<UIElement> children = listBox.ItemsPresenter.LayoutPanelRoot!.VisualChildren.ToArray();
        Assert.Equal(2, children.Count);
        Assert.Same(a, children[0]);
        Assert.Equal("A", a.Content);
        Assert.Same(a, listBox.ItemContainerGenerator.RealizedContainers[0]);
        Assert.Equal(0, a.ItemIndex);
    }

    [Fact]
    public void ElementItemIsNotWrappedInPooledGeneratedContainerOfItsType()
    {
        ContentPresenter element = new() { Content = "P" };
        ItemsControl control = new();
        control.SetItems(new object[] { "str", element });
        UIElement generated = control.ItemContainerGenerator.Realize(new RealizationWindow(0, 1))[0];
        Assert.IsType<ContentPresenter>(generated);

        UIElement realized = control.ItemContainerGenerator.Realize(new RealizationWindow(1, 2))[0];

        Assert.Same(element, realized);
        Assert.Equal("P", element.Content);
        Assert.Null(((ContentPresenter)generated).Content);
    }

    [Fact]
    public void ItemTemplateWrapperOfContentPresenterItemIsReusedOnEveryRealization()
    {
        ContentPresenter element = new() { Content = "P" };
        ItemsControl control = new()
        {
            ItemTemplate = new ContentTemplate<UIElement>("test", key: null, priority: 0, _ => new FixedElement("templated"))
        };
        control.SetItems(new object[] { element });
        UIElement wrapper = control.ItemContainerGenerator.Realize()[0];
        Assert.NotSame(element, wrapper);

        UIElement second = control.ItemContainerGenerator.Realize()[0];
        UIElement third = control.ItemContainerGenerator.Realize()[0];

        Assert.Same(wrapper, second);
        Assert.Same(wrapper, third);
        Assert.Same(element, ((ContentPresenter)wrapper).Content);
        Assert.Equal(0, control.ItemContainerGenerator.RecyclePool.Count);
    }

    [Fact]
    public void SubclassWrapperOfElementItemOfAnotherTypeIsReusedOnEveryRealization()
    {
        ListBoxItem element = new() { Content = "A" };
        WrappingItemsControl control = new();
        control.SetItems(new object[] { element });
        UIElement wrapper = control.ItemContainerGenerator.Realize()[0];
        Assert.IsType<ContentPresenter>(wrapper);
        int preparesAfterFirstRealization = control.PrepareCount;

        UIElement second = control.ItemContainerGenerator.Realize()[0];
        UIElement third = control.ItemContainerGenerator.Realize()[0];

        Assert.Same(wrapper, second);
        Assert.Same(wrapper, third);
        Assert.Same(element, ((ContentPresenter)wrapper).Content);
        Assert.Equal(preparesAfterFirstRealization, control.PrepareCount);
        Assert.Equal(0, control.ItemContainerGenerator.RecyclePool.Count);
    }

    [Fact]
    public void SubclassWrapperOfElementItemOfTheContainerTypeDoesNotGrowRecyclePool()
    {
        ContentPresenter element = new() { Content = "P" };
        WrappingItemsControl control = new();
        control.SetItems(new object[] { element });
        UIElement wrapper = control.ItemContainerGenerator.Realize()[0];
        Assert.NotSame(element, wrapper);

        for (int pass = 0; pass < 3; pass++)
        {
            control.ItemContainerGenerator.Realize();
        }

        UIElement last = control.ItemContainerGenerator.Realize()[0];
        Assert.Same(element, ((ContentPresenter)last).Content);
        Assert.Equal("P", element.Content);
        Assert.Equal(0, control.ItemContainerGenerator.RecyclePool.Count);
    }

    [Fact]
    public void SubclassWrapperOfElementItemOfTheContainerTypeIsReusedOnEveryRealization()
    {
        ContentPresenter element = new() { Content = "P" };
        WrappingItemsControl control = new();
        control.SetItems(new object[] { element });
        UIElement wrapper = control.ItemContainerGenerator.Realize()[0];
        Assert.NotSame(element, wrapper);
        Cerneala.UI.Layout.Panels.Panel panel = new();
        panel.VisualChildren.Add(wrapper);
        Assert.Equal((1, 0, 1), (control.PrepareCount, control.ClearCount, control.CreateCount));

        for (int pass = 1; pass <= 3; pass++)
        {
            UIElement next = control.ItemContainerGenerator.Realize()[0];

            Assert.Same(wrapper, next);
            Assert.Equal((1, 0, 1), (control.PrepareCount, control.ClearCount, control.CreateCount));
            Assert.Same(panel, wrapper.VisualParent);
            Assert.Same(element, ((ContentPresenter)wrapper).Content);
            Assert.Equal(0, control.ItemContainerGenerator.RecyclePool.Count);
        }
    }

    [Fact]
    public void SubclassItemReturnedByCreateItemContainerIsNotWrappedInPooledContainerWithItemTemplate()
    {
        ListBoxItem a = new() { Content = "A" };
        OwnContainerItemsControl control = new()
        {
            ItemTemplate = new ContentTemplate<object>("test", key: null, priority: 0, _ => new FixedElement("templated"))
        };
        control.SetItems(new object[] { "str", a });
        UIElement generated = control.ItemContainerGenerator.Realize(new RealizationWindow(0, 1))[0];

        UIElement second = control.ItemContainerGenerator.Realize(new RealizationWindow(1, 2))[0];
        UIElement third = control.ItemContainerGenerator.Realize(new RealizationWindow(1, 2))[0];

        Assert.Same(a, second);
        Assert.Same(a, third);
        Assert.Equal("A", a.Content);
        Assert.Null(((ListBoxItem)generated).Content);
        Assert.Equal(1, control.ItemContainerGenerator.RecyclePool.Count);
    }

    [Fact]
    public void SubclassReplacingDataItemWithItemReturnedByCreateItemContainerRealizesItemWithItemTemplate()
    {
        ListBoxItem a = new() { Content = "A" };
        OwnContainerItemsControl control = new()
        {
            ItemTemplate = new ContentTemplate<object>("test", key: null, priority: 0, _ => new FixedElement("templated"))
        };
        control.SetItems(new object[] { "str", "b" });
        UIElement generated = control.ItemContainerGenerator.Realize()[0];

        control.Items[0] = a;
        UIElement realized = control.ItemContainerGenerator.Realize()[0];

        Assert.Same(a, realized);
        Assert.Equal("A", a.Content);
        Assert.Null(((ListBoxItem)generated).Content);
        Assert.Equal(1, control.ItemContainerGenerator.RecyclePool.Count);
    }

    [Fact]
    public void ComboBoxRemoveAtKeepsPresenterContentOfComboBoxItemThatIsItsOwnContainer()
    {
        ComboBoxItem first = new() { Content = "First" };
        ContentPresenter presenter = new() { Content = "Second" };
        ComboBoxItem second = new() { Content = presenter };
        ComboBox comboBox = new();
        comboBox.SetItems(new[] { first, second });
        comboBox.ItemContainerGenerator.Realize();

        comboBox.Items.RemoveAt(0);
        comboBox.ItemContainerGenerator.Realize();

        Assert.Same(presenter, second.Content);
        Assert.Equal("Second", presenter.Content);
    }

    [Fact]
    public void GeneratorDetachesRecycledContainerBeforeReuse()
    {
        ItemsControl control = new();
        control.SetItems(new[] { "a", "b" });
        UIElement container = control.ItemContainerGenerator.GetOrCreate(0);
        Cerneala.UI.Layout.Panels.Panel oldPanel = new();
        oldPanel.LogicalChildren.Add(container);
        oldPanel.VisualChildren.Add(container);

        IReadOnlyList<UIElement> nextWindow = control.ItemContainerGenerator.Realize(new RealizationWindow(1, 2));

        Assert.Same(container, nextWindow[0]);
        Assert.Null(container.LogicalParent);
        Assert.Null(container.VisualParent);
        Cerneala.UI.Layout.Panels.Panel nextPanel = new();
        nextPanel.LogicalChildren.Add(container);
        nextPanel.VisualChildren.Add(container);
    }

    [Fact]
    public void OwnContainerItemWhosePreparationThrowsIsClearedAfterRemoval()
    {
        ElementAspect aspect = NewMarginAspect();
        FailAfterPrepareItemsControl control = new() { ItemContainerAspect = aspect, Fail = true };
        FixedElement row = new("r");
        control.SetItems(new[] { row });
        Assert.Throws<InvalidOperationException>(() => control.Measure(new MeasureContext(new LayoutSize(100, 100))));

        control.Fail = false;
        control.SetItems(Array.Empty<object>());
        control.Measure(new MeasureContext(new LayoutSize(100, 101)));

        Assert.Null(row.Aspect);
        Assert.Equal(-1, ItemContainerGenerator.GetItemIndex(row));
        Assert.Null(ItemContainerGenerator.GetItem(row));
    }

    [Fact]
    public void PooledContainerWhosePreparationThrowsReturnsClearedToRecyclePool()
    {
        ElementAspect aspect = NewMarginAspect();
        FailAfterPrepareItemsControl control = new() { ItemContainerAspect = aspect };
        control.SetItems(new[] { "a", "b" });
        control.Measure(new MeasureContext(new LayoutSize(100, 100)));
        UIElement[] old = [.. control.ItemsPresenter.LayoutPanelRoot!.VisualChildren];
        control.SetItems(new[] { "x", "y" });
        Assert.Equal(2, control.ItemContainerGenerator.RecyclePool.Count);

        control.Fail = true;
        Assert.Throws<InvalidOperationException>(() => control.Measure(new MeasureContext(new LayoutSize(100, 101))));

        Assert.Empty(control.ItemContainerGenerator.RealizedContainers);
        Assert.Equal(2, control.ItemContainerGenerator.RecyclePool.Count);
        foreach (UIElement container in old)
        {
            Assert.Null(container.Aspect);
            Assert.Equal(-1, ItemContainerGenerator.GetItemIndex(container));
            Assert.Null(ItemContainerGenerator.GetItem(container));
        }
    }

    [Fact]
    public void RepeatedlyThrowingPreparationDoesNotDrainRecyclePool()
    {
        FailAfterPrepareItemsControl control = new();
        control.SetItems(new[] { "a", "b" });
        control.Measure(new MeasureContext(new LayoutSize(100, 100)));
        UIElement[] old = [.. control.ItemsPresenter.LayoutPanelRoot!.VisualChildren];
        control.SetItems(new[] { "x", "y" });

        control.Fail = true;
        for (int attempt = 1; attempt <= 3; attempt++)
        {
            Assert.Throws<InvalidOperationException>(() => control.Measure(new MeasureContext(new LayoutSize(100, 100 + attempt))));
            Assert.Equal(2, control.ItemContainerGenerator.RecyclePool.Count);
        }

        control.Fail = false;
        control.Measure(new MeasureContext(new LayoutSize(100, 110)));

        Assert.Equal(0, control.ItemContainerGenerator.RecyclePool.Count);
        Assert.Equal(2, control.ItemContainerGenerator.RealizedContainers.Count);
        Assert.All(old, container => Assert.Contains(container, control.ItemContainerGenerator.RealizedContainers.Values));
    }

    [Fact]
    public void PresenterPreservesUnrelatedRealizedContainersWhenDataOutsideWindowChanges()
    {
        ItemsControl control = new();
        control.SetItems(Enumerable.Range(0, 20).Cast<object>());
        control.SetVirtualizationContext(new VirtualizationContext(20, 10, 30, 0, 0));
        control.Measure(new MeasureContext(new LayoutSize(100, 30)));
        Cerneala.UI.Elements.UIElement first = control.ItemsPresenter.LayoutPanelRoot!.VisualChildren[0];

        control.Items[10] = 999;
        control.Measure(new MeasureContext(new LayoutSize(100, 30)));

        Assert.Same(first, control.ItemsPresenter.LayoutPanelRoot!.VisualChildren[0]);
    }

    private static ElementAspect NewMarginAspect()
    {
        return new ElementAspect([new ElementAspectValue(UIElement.MarginProperty, new Thickness(3))]);
    }

    private static void Layout(UIElement element)
    {
        element.Measure(new MeasureContext(new LayoutSize(100, 100)));
        element.Arrange(new ArrangeContext(new LayoutRect(0, 0, 100, 100)));
    }

    private static void AssertDataItemContainer(UIElement container, ListBoxItem removed, int expectedIndex, string expectedItem)
    {
        ListBoxItem generated = Assert.IsType<ListBoxItem>(container);
        Assert.NotSame(removed, generated);
        Assert.Equal(expectedItem, generated.Content);
        AssertItemState(generated, expectedIndex, isSelected: false, expectedItem);
    }

    // Items [a, "x", "str"]: a is realized only at 0 with its item state, and "x" and "str" have generated containers.
    private static void AssertReplacedListBoxItemInsertedBackAtIndexZero(ClearRecordingListBox listBox, ListBoxItem a)
    {
        IReadOnlyDictionary<int, UIElement> realized = listBox.ItemContainerGenerator.RealizedContainers;
        Assert.Equal(new[] { 0, 1, 2 }, realized.Keys.Order());
        Assert.Equal(3, realized.Values.Distinct().Count());
        Assert.Same(a, realized[0]);
        AssertItemState(a, 0, isSelected: false);
        AssertDataItemContainer(realized[1], a, 1, "x");
        AssertDataItemContainer(realized[2], a, 2, "str");
        Assert.DoesNotContain(a, listBox.ClearedContainers);
    }

    private static void AssertRemovedListBoxItem(ListBoxItem removed)
    {
        Assert.Equal("A", removed.Content);
        Assert.Null(removed.VisualParent);
        AssertItemState(removed, -1, isSelected: false, expectedItem: null);
    }

    private static void AssertItemState(ListBoxItem container, int expectedIndex, bool isSelected)
    {
        AssertItemState(container, expectedIndex, isSelected, container);
    }

    private static void AssertItemState(ListBoxItem container, int expectedIndex, bool isSelected, object? expectedItem)
    {
        Assert.Equal(expectedIndex, container.ItemIndex);
        Assert.Same(expectedItem, container.Item);
        Assert.Equal(isSelected, container.IsSelected);
        Assert.Equal(expectedIndex, ItemContainerGenerator.GetItemIndex(container));
        Assert.Same(expectedItem, ItemContainerGenerator.GetItem(container));
    }

    // Both selection records of a prepared container: the container's IsSelected and the generator's flag.
    private static void AssertPreparedSelection(ListBoxItem container, int expectedIndex, object expectedItem, bool isSelected)
    {
        AssertItemState(container, expectedIndex, isSelected, expectedItem);
        Assert.Equal(isSelected, ItemContainerGenerator.GetIsSelected(container));
    }

    private sealed class ClearRecordingListBox : ListBox
    {
        public List<UIElement> ClearedContainers { get; } = [];

        protected internal override void ClearItemContainer(UIElement container)
        {
            ClearedContainers.Add(container);
            base.ClearItemContainer(container);
        }
    }

    private sealed class FixedElement(string value) : UIElement
    {
        public string Value { get; } = value;

        protected override LayoutSize MeasureCore(MeasureContext context)
        {
            return new LayoutSize(10, 10);
        }
    }

    private sealed class CountingItemsControl : ItemsControl
    {
        public int PrepareCount { get; private set; }

        protected internal override void PrepareItemContainer(UIElement container, int index, object? item)
        {
            PrepareCount++;
            base.PrepareItemContainer(container, index, item);
        }
    }

    // Throws from PrepareItemContainer after the base preparation ran, while Fail is set.
    private sealed class FailAfterPrepareItemsControl : ItemsControl
    {
        public bool Fail { get; set; }

        protected internal override void PrepareItemContainer(UIElement container, int index, object? item)
        {
            base.PrepareItemContainer(container, index, item);
            if (Fail)
            {
                throw new InvalidOperationException("container preparation failed");
            }
        }
    }

    // Wraps every item, UIElement items included, in a ContentPresenter, through the documented overrides only.
    private sealed class WrappingItemsControl : ItemsControl
    {
        public int PrepareCount { get; private set; }

        public int ClearCount { get; private set; }

        public int CreateCount { get; private set; }

        protected internal override Type GetContainerTypeForItem(object? item)
        {
            return typeof(ContentPresenter);
        }

        protected internal override UIElement CreateItemContainer(int index, object? item)
        {
            CreateCount++;
            return new ContentPresenter();
        }

        protected internal override void PrepareItemContainer(UIElement container, int index, object? item)
        {
            PrepareCount++;
            base.PrepareItemContainer(container, index, item);
        }

        protected internal override void ClearItemContainer(UIElement container)
        {
            ClearCount++;
            base.ClearItemContainer(container);
        }
    }

    // Realizes a ListBoxItem item as itself and wraps every other item, through the documented overrides only.
    private sealed class OwnContainerItemsControl : ItemsControl
    {
        protected internal override Type GetContainerTypeForItem(object? item)
        {
            return typeof(ListBoxItem);
        }

        protected internal override UIElement CreateItemContainer(int index, object? item)
        {
            return item is ListBoxItem element ? element : new ListBoxItem();
        }
    }

    private sealed class DerivedListBoxItem : ListBoxItem;

    // Returns the base container type for every item and keeps ListBox's CreateItemContainer, which returns a
    // ListBoxItem item, a derived one too, as itself.
    private sealed class BaseContainerTypeListBox : ListBox
    {
        protected internal override Type GetContainerTypeForItem(object? item)
        {
            return typeof(ListBoxItem);
        }
    }
}
