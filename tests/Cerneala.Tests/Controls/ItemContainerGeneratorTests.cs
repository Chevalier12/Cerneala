using System.Collections.ObjectModel;
using Cerneala.UI.Controls;
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

    private static void Layout(UIElement element)
    {
        element.Measure(new MeasureContext(new LayoutSize(100, 100)));
        element.Arrange(new ArrangeContext(new LayoutRect(0, 0, 100, 100)));
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
}
