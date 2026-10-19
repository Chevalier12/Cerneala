using System.Collections.ObjectModel;
using Cerneala.UI.Aspect;
using Cerneala.UI.Controls;
using Cerneala.UI.Controls.Items;
using Cerneala.UI.Controls.Templates;
using Cerneala.UI.Elements;
using Cerneala.UI.Input;
using Cerneala.UI.Layout;
using Orientation = Cerneala.UI.Layout.Orientation;

namespace Cerneala.Tests.Controls;

public sealed class TabControlTests
{
    [Fact]
    public void TabControlSelectedTabItemReturnsUnrealizedTabItem()
    {
        TabItem first = new() { Header = "First" };
        TabItem second = new() { Header = "Second" };
        TabControl tabControl = new();
        tabControl.SetItems(new[] { first, second });

        tabControl.SelectedIndex = 1;

        Assert.Same(second, tabControl.SelectedTabItem);
    }

    [Fact]
    public void TabControlSelectedTabItemReadsItemsSourceTabItem()
    {
        TabItem first = new() { Header = "First" };
        TabItem second = new() { Header = "Second" };
        TabControl tabControl = new() { ItemsSource = new[] { first, second } };

        tabControl.SelectedIndex = 1;

        Assert.Same(second, tabControl.SelectedItem);
        Assert.Same(second, tabControl.SelectedTabItem);
    }

    [Fact]
    public void TabControlSelectedTabItemReturnsRealizedContainerForItemsSourceDataItem()
    {
        TabControl tabControl = new() { ItemsSource = new[] { "A", "B" } };
        tabControl.ItemContainerGenerator.Realize();

        tabControl.SelectedIndex = 1;

        Assert.Same(tabControl.ItemContainerGenerator.RealizedContainers[1], tabControl.SelectedTabItem);
        Assert.Equal("B", tabControl.SelectedItem);
    }

    [Fact]
    public void TabControlClickSelectsTabItem()
    {
        UIRoot root = new(100, 100);
        TabControl tabControl = new()
        {
            ItemsPanel = new StackPanel()
        };
        TabItem second = new() { Header = "Two", Content = new FixedElement() };
        tabControl.SetItems(new[]
        {
            new TabItem { Header = "One", Content = new FixedElement() },
            second
        });
        root.VisualChildren.Add(tabControl);
        tabControl.Measure(new MeasureContext(new LayoutSize(100, 100)));
        tabControl.Arrange(new ArrangeContext(new LayoutRect(0, 0, 100, 100)));

        // The header row is as tall as its text now and the Content is shown below it, so aim at the second tab.
        float x = second.ArrangedBounds.X + second.ArrangedBounds.Width / 2;
        float y = second.ArrangedBounds.Y + second.ArrangedBounds.Height / 2;
        ElementInputBridge bridge = new();
        bridge.Dispatch(root, PointerFrame(x, y, currentDown: true));
        bridge.Dispatch(root, PointerFrame(x, y, previousDown: true));

        Assert.Equal(1, tabControl.SelectedIndex);
        Assert.True(tabControl.SelectedTabItem!.IsSelected);
    }

    [Fact]
    public void HorizontalTabControlSelectsOverflowingHeaderElement()
    {
        UIRoot root = new(200, 100);
        FixedElement wideHeader = new(new LayoutSize(120, 10));
        TabControl tabControl = new()
        {
            ItemsPanel = new StackPanel { Orientation = Orientation.Horizontal }
        };
        tabControl.SetItems(new[]
        {
            new TabItem { Header = wideHeader },
            new TabItem { Header = new FixedElement(new LayoutSize(30, 10)) }
        });
        root.VisualChildren.Add(tabControl);
        tabControl.Measure(new MeasureContext(new LayoutSize(200, 100)));
        tabControl.Arrange(new ArrangeContext(new LayoutRect(0, 0, 100, 20)));

        ElementInputBridge bridge = new();
        bridge.Dispatch(root, PointerFrame(110, 5, currentDown: true));
        bridge.Dispatch(root, PointerFrame(110, 5, previousDown: true));

        Assert.Equal(0, tabControl.SelectedIndex);
        Assert.Equal(new LayoutRect(0, 0, 120, 10), wideHeader.ArrangedBounds);
    }

    [Fact]
    public void InsertingTabBeforeSelectedTabKeepsEachTabContent()
    {
        UIRoot root = new(100, 100);
        TabItem first = new() { Header = "A", Content = "A content" };
        TabItem second = new() { Header = "B", Content = "B content" };
        TabItem third = new() { Header = "C", Content = "C content" };
        TabControl tabControl = new() { ItemsPanel = new StackPanel() };
        tabControl.SetItems(new[] { first, second, third });
        root.VisualChildren.Add(tabControl);
        Layout(tabControl);
        tabControl.SelectedIndex = 1;

        tabControl.Items.Insert(0, new TabItem { Header = "Z", Content = "Z content" });
        Layout(tabControl);

        Assert.Equal(2, tabControl.SelectedIndex);
        Assert.Same(second, tabControl.SelectedTabItem);
        Assert.Equal("A content", first.Content);
        Assert.Equal("B content", second.Content);
        Assert.Equal("C content", third.Content);
        Assert.False(first.IsSelected);
        Assert.True(second.IsSelected);
    }

    [Fact]
    public void InsertingTabBeforeSelectedTabKeepsSelectedTabItemBeforeLayout()
    {
        TabItem first = new() { Header = "A" };
        TabItem second = new() { Header = "B" };
        TabItem third = new() { Header = "C" };
        TabControl tabControl = new();
        tabControl.SetItems(new[] { first, second, third });
        tabControl.ItemContainerGenerator.Realize();
        tabControl.SelectedIndex = 0;

        tabControl.Items.Insert(0, new TabItem { Header = "Z" });

        Assert.Same(first, tabControl.SelectedItem);
        Assert.Same(first, tabControl.SelectedTabItem);
    }

    [Fact]
    public void RemovingTabBeforeSelectedTabKeepsSelectedTabItemBeforeLayout()
    {
        TabItem first = new() { Header = "A" };
        TabItem second = new() { Header = "B" };
        TabItem third = new() { Header = "C" };
        TabControl tabControl = new();
        tabControl.SetItems(new[] { first, second, third });
        tabControl.ItemContainerGenerator.Realize();
        tabControl.SelectedIndex = 2;

        tabControl.Items.RemoveAt(0);

        Assert.Same(third, tabControl.SelectedItem);
        Assert.Same(third, tabControl.SelectedTabItem);
    }

    [Fact]
    public void SelectingAfterRemovingRealizedTabNeverReturnsRemovedTab()
    {
        TabItem first = new() { Header = "A" };
        TabItem second = new() { Header = "B" };
        TabItem third = new() { Header = "C" };
        TabControl tabControl = new();
        tabControl.SetItems(new[] { first, second, third });
        tabControl.ItemContainerGenerator.Realize();

        tabControl.Items.RemoveAt(0);
        tabControl.SelectedIndex = 0;

        Assert.Same(second, tabControl.SelectedTabItem);
        tabControl.ItemContainerGenerator.Realize();
        Assert.Same(second, tabControl.SelectedTabItem);
    }

    [Fact]
    public void InsertingDataItemBeforeSelectedItemReturnsSelectedItemContainerBeforeLayout()
    {
        TabControl tabControl = new();
        tabControl.SetItems(new[] { "A", "B", "C" });
        tabControl.ItemContainerGenerator.Realize();
        tabControl.SelectedIndex = 0;

        tabControl.Items.Insert(0, "Z");

        Assert.Equal("A", tabControl.SelectedItem);
        Assert.Equal("A", ItemContainerGenerator.GetItem(tabControl.SelectedTabItem!));
        tabControl.ItemContainerGenerator.Realize();
        Assert.Same(tabControl.ItemContainerGenerator.RealizedContainers[1], tabControl.SelectedTabItem);
        Assert.Equal("A", ItemContainerGenerator.GetItem(tabControl.SelectedTabItem!));
    }

    [Fact]
    public void RemovingDataItemBeforeSelectedItemReturnsSelectedItemContainerBeforeLayout()
    {
        TabControl tabControl = new();
        tabControl.SetItems(new[] { "A", "B", "C" });
        tabControl.ItemContainerGenerator.Realize();
        tabControl.SelectedIndex = 2;

        tabControl.Items.RemoveAt(0);

        Assert.Equal("C", tabControl.SelectedItem);
        Assert.Equal("C", ItemContainerGenerator.GetItem(tabControl.SelectedTabItem!));
        tabControl.ItemContainerGenerator.Realize();
        Assert.Same(tabControl.ItemContainerGenerator.RealizedContainers[1], tabControl.SelectedTabItem);
        Assert.Equal("C", ItemContainerGenerator.GetItem(tabControl.SelectedTabItem!));
    }

    [Fact]
    public void RemovingSelectedTabSelectsTabThatTakesItsPlace()
    {
        TabItem first = new() { Header = "A" };
        TabItem second = new() { Header = "B" };
        TabItem third = new() { Header = "C" };
        TabControl tabControl = new();
        tabControl.SetItems(new[] { first, second, third });
        tabControl.SelectedIndex = 1;

        tabControl.Items.RemoveAt(1);

        Assert.Equal(1, tabControl.SelectedIndex);
        Assert.Same(third, tabControl.SelectedItem);
    }

    [Fact]
    public void RemovingSelectedLastTabSelectsPreviousTab()
    {
        TabItem first = new() { Header = "A" };
        TabItem second = new() { Header = "B" };
        TabItem third = new() { Header = "C" };
        TabControl tabControl = new();
        tabControl.SetItems(new[] { first, second, third });
        tabControl.SelectedIndex = 2;

        tabControl.Items.RemoveAt(2);

        Assert.Equal(1, tabControl.SelectedIndex);
        Assert.Same(second, tabControl.SelectedItem);
    }

    [Fact]
    public void RemovingSelectedTabSkipsDisabledTab()
    {
        TabItem first = new() { Header = "A" };
        TabItem second = new() { Header = "B" };
        TabItem third = new() { Header = "C", IsEnabled = false };
        TabControl tabControl = new();
        tabControl.SetItems(new[] { first, second, third });
        tabControl.SelectedIndex = 1;

        tabControl.Items.RemoveAt(1);

        Assert.Equal(0, tabControl.SelectedIndex);
        Assert.Same(first, tabControl.SelectedItem);
    }

    // Clearing the selection re-prepares the container at index 1, which already holds "C", so the removal throws.
    // The tab that took the removed tab's place is still selected, and the first failure propagates after it.
    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void RemovingSelectedTabWhosePreparationOfNextTabThrowsStillSelectsNextTab(bool useItemsSource, bool keepFailing)
    {
        UIRoot root = new(200, 100);
        ObservableCollection<string> source = ["A", "B", "C"];
        FailingContentTabControl tabControl = new();
        if (useItemsSource)
        {
            tabControl.ItemsSource = source;
        }
        else
        {
            tabControl.SetItems(new[] { "A", "B", "C" });
        }

        root.VisualChildren.Add(tabControl);
        root.ProcessFrame();
        tabControl.SelectedIndex = 1;
        root.ProcessFrame();
        List<string> events = [];
        tabControl.SelectionChanged += (_, args) => events.Add(
            $"[{string.Join(",", args.RemovedItems.Cast<object>())}] -> [{string.Join(",", args.AddedItems.Cast<object>())}]");
        tabControl.FailFor = "C";
        tabControl.RemainingFailures = keepFailing ? int.MaxValue : 1;

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() =>
        {
            if (useItemsSource)
            {
                source.RemoveAt(1);
            }
            else
            {
                tabControl.Items.RemoveAt(1);
            }
        });

        Assert.Equal("content preparation failed", exception.Message);
        Assert.Equal(1, tabControl.SelectedIndex);
        Assert.Equal("C", tabControl.SelectedItem);
        Assert.Equal(["[B] -> []", "[] -> [C]"], events);

        tabControl.FailFor = null;
        root.ProcessFrame();

        Assert.Equal(1, tabControl.SelectedIndex);
        Assert.Equal("C", ItemContainerGenerator.GetItem(tabControl.SelectedTabItem!));
        Assert.True(tabControl.SelectedTabItem!.IsSelected);
    }

    // The failed refresh realizes the tab of "C" before the callback throws for "D", but the panel never presents it.
    [Fact]
    public void SelectedTabItemIsNullForATabThatAFailedRefreshDidNotPresent()
    {
        FailingPreparedTabControl tabControl = new();
        tabControl.Items.Add("A");
        tabControl.Items.Add("B");
        tabControl.Measure(new MeasureContext(new LayoutSize(200, 100)));

        tabControl.Items.Add("C");
        tabControl.Items.Add("D");
        tabControl.SelectedIndex = 2;
        tabControl.FailIndex = 3;
        Assert.Throws<InvalidOperationException>(() => tabControl.Measure(new MeasureContext(new LayoutSize(200, 101))));

        Cerneala.UI.Layout.Panels.Panel panel = tabControl.ItemsPresenter.LayoutPanelRoot!;
        Assert.Equal(2, panel.VisualChildren.Count);
        Assert.Equal(2, tabControl.RealizedItemCount);
        Assert.Null(tabControl.SelectedTabItem);

        tabControl.FailIndex = -1;
        tabControl.Measure(new MeasureContext(new LayoutSize(200, 102)));

        Assert.Equal(4, panel.VisualChildren.Count);
        Assert.Equal(4, tabControl.RealizedItemCount);
        Assert.Equal("C", tabControl.SelectedTabItem!.Content);
        Assert.Same(panel, tabControl.SelectedTabItem.VisualParent);
    }

    [Fact]
    public void DataItemTabGetsHeaderFromItemAndItemTemplateBuildsTheHeader()
    {
        UIRoot root = new(300, 200);
        int calls = 0;
        ContentTemplate<string> template = new("tab", key: null, priority: 0, context =>
        {
            calls++;
            return new FixedElement(new LayoutSize(40, 20));
        });
        TabControl tabControl = new() { ItemTemplate = template, ItemsSource = new[] { "One", "Two" } };
        root.VisualChildren.Add(tabControl);
        root.ProcessFrame();

        Assert.Equal(2, calls);
        IReadOnlyDictionary<int, UIElement> realized = tabControl.ItemContainerGenerator.RealizedContainers;
        TabItem first = Assert.IsType<TabItem>(realized[0]);
        TabItem second = Assert.IsType<TabItem>(realized[1]);
        Assert.Equal("One", first.Header);
        Assert.Equal("Two", second.Header);
        Assert.True(first.DesiredSize.Width > 0 && first.DesiredSize.Height > 0, $"first desired {first.DesiredSize}");
        Assert.True(second.DesiredSize.Width > 0 && second.DesiredSize.Height > 0, $"second desired {second.DesiredSize}");
        Assert.True(tabControl.DesiredSize.Width > 0 && tabControl.DesiredSize.Height > 0, $"tab control desired {tabControl.DesiredSize}");
    }

    [Fact]
    public void StringDataItemTabShowsItsTextAsHeaderWithoutItemTemplate()
    {
        UIRoot root = new(300, 200);
        TabControl tabControl = new() { ItemsSource = new[] { "One", "Two" } };
        root.VisualChildren.Add(tabControl);
        root.ProcessFrame();

        IReadOnlyDictionary<int, UIElement> realized = tabControl.ItemContainerGenerator.RealizedContainers;
        TabItem first = Assert.IsType<TabItem>(realized[0]);
        TabItem second = Assert.IsType<TabItem>(realized[1]);
        Assert.Equal(["One"], FindTextBlocks(first).Select(text => text.Text));
        Assert.Equal(["Two"], FindTextBlocks(second).Select(text => text.Text));
        Assert.True(first.DesiredSize.Width > 0 && first.DesiredSize.Height > 0, $"first desired {first.DesiredSize}");
        Assert.True(second.DesiredSize.Width > 0 && second.DesiredSize.Height > 0, $"second desired {second.DesiredSize}");
    }

    [Fact]
    public void ExplicitTabItemsShowTheirStringHeaderAndNeverGetTheItemTemplate()
    {
        UIRoot root = new(300, 200);
        int calls = 0;
        ContentTemplate<string> template = new("tab", key: null, priority: 0, context =>
        {
            calls++;
            return new FixedElement(new LayoutSize(40, 20));
        });
        TabItem first = new() { Header = "H1", Content = "C1" };
        TabItem second = new() { Header = "H2", Content = "C2" };
        TabControl tabControl = new() { ItemTemplate = template };
        tabControl.SetItems(new[] { first, second });
        root.VisualChildren.Add(tabControl);
        root.ProcessFrame();

        Assert.Equal(["H1"], FindTextBlocks(first).Select(text => text.Text));
        Assert.Equal(["H2"], FindTextBlocks(second).Select(text => text.Text));
        Assert.Equal(0, calls);
    }

    [Fact]
    public void ContentAreaShowsSelectedExplicitTabContentBelowTheHeaderRow()
    {
        UIRoot root = new(300, 200);
        TabItem first = new() { Header = "H1", Content = "C1" };
        TabItem second = new() { Header = "H2", Content = "C2" };
        TabControl tabControl = new();
        tabControl.SetItems(new[] { first, second });
        root.VisualChildren.Add(tabControl);
        tabControl.SelectedIndex = 0;
        root.ProcessFrame();

        AssertContentAreaShows(tabControl, new[] { first, second }, "C1", "C2");

        tabControl.SelectedIndex = 1;
        root.ProcessFrame();

        AssertContentAreaShows(tabControl, new[] { first, second }, "C2", "C1");
    }

    [Fact]
    public void ContentAreaShowsSelectedDataItemBelowTheHeaderRow()
    {
        UIRoot root = new(300, 200);
        TabControl tabControl = new() { ItemsSource = new[] { "One", "Two" } };
        root.VisualChildren.Add(tabControl);
        tabControl.SelectedIndex = 0;
        root.ProcessFrame();
        UIElement[] tabs = tabControl.ItemContainerGenerator.RealizedContainers.OrderBy(pair => pair.Key).Select(pair => pair.Value).ToArray();

        AssertContentAreaShows(tabControl, tabs, "One", "Two");

        tabControl.SelectedIndex = 1;
        root.ProcessFrame();

        AssertContentAreaShows(tabControl, tabs, "Two", "One");
    }

    [Fact]
    public void ElementContentLivesInTheContentAreaOnlyWhileItsTabIsSelected()
    {
        UIRoot root = new(300, 200);
        FixedElement a = new();
        FixedElement b = new();
        TabItem first = new() { Header = "H1", Content = a };
        TabItem second = new() { Header = "H2", Content = b };
        TabControl tabControl = new();
        tabControl.SetItems(new[] { first, second });
        root.VisualChildren.Add(tabControl);
        tabControl.SelectedIndex = 0;
        root.ProcessFrame();

        Assert.NotNull(a.VisualParent);
        Assert.True(HasVisualAncestor(a, tabControl), "a is not under the TabControl while tab 0 is selected");
        Assert.False(HasVisualAncestor(a, first), "a is still hosted inside its TabItem");
        Assert.True(a.ArrangedBounds.Width > 0 && a.ArrangedBounds.Height > 0, $"a arranged {a.ArrangedBounds}");
        Assert.Null(b.VisualParent);

        tabControl.SelectedIndex = 1;
        root.ProcessFrame();

        Assert.Null(a.VisualParent);
        Assert.NotNull(b.VisualParent);
        Assert.True(HasVisualAncestor(b, tabControl), "b is not under the TabControl while tab 1 is selected");
        Assert.False(HasVisualAncestor(b, second), "b is hosted inside its TabItem");
        Assert.True(b.ArrangedBounds.Width > 0 && b.ArrangedBounds.Height > 0, $"b arranged {b.ArrangedBounds}");
    }

    [Fact]
    public void ElementContentOfATabThatIsNotSelectedCannotBeAddedToAnotherParent()
    {
        UIRoot root = new(300, 200);
        FixedElement a = new();
        FixedElement b = new();
        FixedElement other = new();
        TabItem first = new() { Header = "H1", Content = a };
        TabItem second = new() { Header = "H2", Content = b };
        TabControl tabControl = new();
        tabControl.SetItems(new[] { first, second });
        root.VisualChildren.Add(tabControl);
        root.VisualChildren.Add(other);
        tabControl.SelectedIndex = 0;
        root.ProcessFrame();

        Assert.Same(second, b.LogicalParent);
        Assert.Null(b.VisualParent);
        InvalidOperationException rejected =Assert.Throws<InvalidOperationException>(() => other.LogicalChildren.Add(b));
        Assert.Equal("Element must be removed from its current parent before reparenting.", rejected.Message);
        Assert.Empty(other.LogicalChildren);

        tabControl.SelectedIndex = 1;
        root.ProcessFrame();

        Assert.True(HasVisualAncestor(b, tabControl), "b is not under the TabControl while tab 1 is selected");
        Assert.False(HasVisualAncestor(b, second), "b is hosted inside its TabItem");
        Assert.True(b.ArrangedBounds.Width > 0 && b.ArrangedBounds.Height > 0, $"b arranged {b.ArrangedBounds}");
        Assert.Same(first, a.LogicalParent);
        Assert.Null(a.VisualParent);
        Assert.Throws<InvalidOperationException>(() => other.LogicalChildren.Add(a));
        Assert.Empty(other.LogicalChildren);
    }

    [Fact]
    public void ElementContentOfTheTabShownBeforeIsOwnedAgainAfterPresentingTheSelectedTabThrewOnce()
    {
        UIRoot root = new(300, 200);
        FixedElement a = new();
        FixedElement b = new();
        FixedElement other = new();
        bool fail = true;
        ContentTemplate throwing = new(
            "boom",
            typeof(FixedElement),
            null,
            0,
            _ => fail ? throw new InvalidOperationException("boom") : new FixedElement());
        TabItem first = new() { Header = "H1", Content = a };
        TabItem second = new() { Header = "H2", Content = b, ContentTemplate = throwing };
        TabControl tabControl = new();
        tabControl.SetItems(new[] { first, second });
        root.VisualChildren.Add(tabControl);
        root.VisualChildren.Add(other);
        tabControl.SelectedIndex = 0;
        root.ProcessFrame();

        tabControl.SelectedIndex = 1;
        InvalidOperationException thrown = Assert.Throws<InvalidOperationException>(() => root.ProcessFrame());
        Assert.Equal("boom", thrown.Message);
        Assert.Same(first, a.LogicalParent);
        Assert.Null(a.VisualParent);

        fail = false;
        second.ContentTemplate = null;
        root.ProcessFrame();

        Assert.True(HasVisualAncestor(b, tabControl), "b is not under the TabControl while tab 1 is selected");
        Assert.Same(first, a.LogicalParent);
        Assert.Null(a.VisualParent);
        Assert.Throws<InvalidOperationException>(() => other.LogicalChildren.Add(a));
        Assert.Empty(other.LogicalChildren);
    }

    [Fact]
    public void ElementItemOfARemovedGeneratedTabCanBeAddedToAnotherParent()
    {
        UIRoot root = new(300, 200);
        FixedElement a = new();
        FixedElement b = new();
        FixedElement other = new();
        ObservableCollection<UIElement> items = [a, b];
        TabControl tabControl = new() { ItemsSource = items };
        root.VisualChildren.Add(tabControl);
        root.VisualChildren.Add(other);
        tabControl.SelectedIndex = 0;
        root.ProcessFrame();

        Assert.IsType<TabItem>(b.LogicalParent);
        Assert.Throws<InvalidOperationException>(() => other.LogicalChildren.Add(b));

        items.Remove(b);
        root.ProcessFrame();

        Assert.Null(b.LogicalParent);
        other.LogicalChildren.Add(b);
        other.VisualChildren.Add(b);
        root.ProcessFrame();
        Assert.Same(other, b.VisualParent);
    }

    [Fact]
    public void ElementContentOfATabCannotBeTheContentOfAnotherTab()
    {
        UIRoot root = new(300, 200);
        FixedElement a = new();
        FixedElement b = new();
        TabItem first = new() { Header = "H1", Content = a };
        TabItem second = new() { Header = "H2", Content = b };
        TabControl tabControl = new();
        tabControl.SetItems(new[] { first, second });
        root.VisualChildren.Add(tabControl);
        tabControl.SelectedIndex = 0;
        root.ProcessFrame();
        TabItem third = new() { Header = "H3" };

        Assert.Throws<InvalidOperationException>(() => third.Content = a);
        Assert.Throws<InvalidOperationException>(() => third.Content = b);
        Assert.Null(third.Content);

        FixedElement loose = new();
        TabItem outside = new() { Content = loose };
        Assert.Throws<InvalidOperationException>(() => third.Content = loose);

        second.Content = null;
        third.Content = b;
        Assert.Same(b, third.Content);

        tabControl.SelectedIndex = 1;
        root.ProcessFrame();

        Assert.Null(a.VisualParent);
        Assert.Throws<InvalidOperationException>(() => outside.Content = a);
        Assert.Same(loose, outside.Content);
    }

    [Fact]
    public void SetValueRejectsElementContentThatHasAnotherParentLikeTheContentSetter()
    {
        UIRoot root = new(300, 200);
        Border holder = new();
        FixedElement parented = new();
        holder.Child = parented;
        root.VisualChildren.Add(holder);
        FixedElement firstContent = new();
        FixedElement secondContent = new();
        TabItem first = new() { Header = "H1", Content = firstContent };
        TabItem second = new() { Header = "H2", Content = secondContent };
        TabControl tabControl = new();
        tabControl.SetItems(new[] { first, second });
        tabControl.SelectedIndex = 0;
        root.VisualChildren.Add(tabControl);
        root.ProcessFrame();

        InvalidOperationException rejected = Assert.Throws<InvalidOperationException>(
            () => second.SetValue(ContentControl.ContentProperty, (object?)parented));
        Assert.Equal("Element must be removed from its current parent before reparenting.", rejected.Message);
        Assert.Same(secondContent, second.Content);
        Assert.Same(holder, parented.LogicalParent);
        Assert.Same(second, secondContent.LogicalParent);

        FixedElement loose = new();
        second.SetValue(ContentControl.ContentProperty, (object?)loose);
        Assert.Same(loose, second.Content);
        Assert.Same(second, loose.LogicalParent);
        Assert.Null(secondContent.LogicalParent);

        tabControl.SelectedIndex = 1;
        root.ProcessFrame();

        Assert.True(HasVisualAncestor(loose, tabControl), "loose is not under the TabControl while tab 1 is selected");
        Assert.Same(holder, parented.VisualParent);
    }

    [Fact]
    public void ElementContentOfATabThatIsNotSelectedStaysOwnedWhenTheTabGetsAComponentTemplate()
    {
        UIRoot root = new(300, 200);
        FixedElement b = new();
        TabItem first = new() { Header = "H1", Content = new FixedElement() };
        TabItem second = new() { Header = "H2", Content = b };
        TabControl tabControl = new();
        tabControl.SetItems(new[] { first, second });
        root.VisualChildren.Add(tabControl);
        tabControl.SelectedIndex = 0;
        root.ProcessFrame();

        second.ComponentTemplate = new ComponentTemplate<TabItem>("tab", _ => new FixedElement());
        root.ProcessFrame();

        Assert.Throws<InvalidOperationException>(() => new TabItem().Content = b);

        tabControl.SelectedIndex = 1;
        root.ProcessFrame();

        Assert.True(HasVisualAncestor(b, tabControl), "b is not under the TabControl while tab 1 is selected");
    }

    [Fact]
    public void OnlyTheElementContentOfTheSelectedTabIsLoaded()
    {
        UIRoot root = new(300, 200);
        int[] loaded = new int[3];
        int[] unloaded = new int[3];
        Border[] contents = new Border[3];
        for (int index = 0; index < contents.Length; index++)
        {
            int slot = index;
            contents[index] = new Border { Width = 50, Height = 20 };
            contents[index].Loaded += (_, _) => loaded[slot]++;
            contents[index].Unloaded += (_, _) => unloaded[slot]++;
        }

        TabControl tabControl = new();
        tabControl.SetItems(new[]
        {
            new TabItem { Header = "1", Content = contents[0] },
            new TabItem { Header = "2", Content = contents[1] },
            new TabItem { Header = "3", Content = contents[2] },
        });
        tabControl.SelectedIndex = 0;
        root.VisualChildren.Add(tabControl);
        root.ProcessFrame();

        Assert.Equal([1, 0, 0], loaded);
        Assert.Equal([0, 0, 0], unloaded);
        Assert.True(contents[0].IsLoaded);
        Assert.False(contents[1].IsLoaded);
        Assert.False(contents[2].IsLoaded);

        tabControl.SelectedIndex = 1;
        root.ProcessFrame();

        Assert.Equal([1, 1, 0], loaded);
        Assert.Equal([1, 0, 0], unloaded);
        Assert.False(contents[0].IsLoaded);
        Assert.True(contents[1].IsLoaded);

        tabControl.SelectedIndex = 0;
        root.ProcessFrame();

        Assert.Equal([2, 1, 0], loaded);
        Assert.Equal([1, 1, 0], unloaded);
    }

    [Fact]
    public void ComponentTemplatePresenterCanShowTheSelectedElementContent()
    {
        UIRoot root = new(300, 200);
        FixedElement a = new();
        TabControl tabControl = new();
        tabControl.SetItems(new[] { new TabItem { Header = "1", Content = a }, new TabItem { Header = "2" } });
        tabControl.SelectedIndex = 0;
        ContentPresenter? presenter = null;

        tabControl.ComponentTemplate = new ComponentTemplate<TabControl>("tabs", context =>
            presenter = new ContentPresenter { Content = context.Owner.SelectedContent });
        root.VisualChildren.Add(tabControl);
        root.ProcessFrame();

        Assert.NotNull(presenter);
        Assert.Same(presenter, a.LogicalParent);
        Assert.Same(presenter, a.VisualParent);
    }

    [Fact]
    public void ComponentTemplatePresenterFollowsTheSelectedTabWhileOtherTabsKeepTheirElementContent()
    {
        UIRoot root = new(300, 200);
        FixedElement a = new();
        FixedElement b = new();
        FixedElement other = new();
        TabItem first = new() { Header = "1", Content = a };
        TabItem second = new() { Header = "2", Content = b };
        TabControl tabControl = new();
        tabControl.SetItems(new[] { first, second });
        tabControl.SelectedIndex = 0;
        ContentPresenter? presenter = null;
        tabControl.ComponentTemplate = new ComponentTemplate<TabControl>("tabs", context =>
            presenter = new ContentPresenter { Content = context.Owner.SelectedContent });
        tabControl.SelectionChanged += (_, _) => presenter!.Content = tabControl.SelectedContent;
        root.VisualChildren.Add(tabControl);
        root.VisualChildren.Add(other);
        root.ProcessFrame();

        Assert.Same(presenter, a.VisualParent);
        Assert.Same(second, b.LogicalParent);
        Assert.Throws<InvalidOperationException>(() => other.LogicalChildren.Add(b));

        tabControl.SelectedIndex = 1;
        root.ProcessFrame();

        Assert.Same(presenter, b.LogicalParent);
        Assert.Same(presenter, b.VisualParent);
        Assert.Same(first, a.LogicalParent);
        Assert.Null(a.VisualParent);
        Assert.Throws<InvalidOperationException>(() => other.LogicalChildren.Add(a));
        Assert.Empty(other.LogicalChildren);

        tabControl.SelectedIndex = 0;
        root.ProcessFrame();

        Assert.Same(presenter, a.VisualParent);
        Assert.Same(second, b.LogicalParent);
        Assert.Null(b.VisualParent);
    }

    [Fact]
    public void DeselectedTabOwnsTheElementContentThatASelectionChangedHandlerTookFromTheTemplateBeforeTheNextLayoutPass()
    {
        UIRoot root = new(300, 200);
        FixedElement a = new();
        FixedElement b = new();
        StackPanel other = new();
        TabItem first = new() { Header = "1", Content = a };
        TabItem second = new() { Header = "2", Content = b };
        TabControl tabControl = new();
        tabControl.SetItems(new[] { first, second });
        tabControl.SelectedIndex = 0;
        ContentPresenter? presenter = null;
        tabControl.ComponentTemplate = new ComponentTemplate<TabControl>("tabs", context =>
            presenter = new ContentPresenter { Content = context.Owner.SelectedContent });
        tabControl.SelectionChanged += (_, _) => presenter!.Content = tabControl.SelectedContent;
        root.VisualChildren.Add(tabControl);
        root.VisualChildren.Add(other);
        root.ProcessFrame();

        tabControl.SelectedIndex = 1;

        Assert.Same(presenter, b.LogicalParent);
        Assert.Same(first, a.LogicalParent);
        Assert.Null(a.VisualParent);
        Assert.Throws<InvalidOperationException>(() => other.Children.Add(a));
        Assert.Empty(other.Children);

        tabControl.SelectedIndex = 0;
        root.ProcessFrame();

        Assert.Same(presenter, a.LogicalParent);
        Assert.Same(presenter, a.VisualParent);
        Assert.Same(second, b.LogicalParent);
        Assert.Null(b.VisualParent);
    }

    [Fact]
    public void DeselectedTabOwnsTheElementContentThatASelectedHandlerTookFromTheTemplateBeforeTheNextLayoutPass()
    {
        UIRoot root = new(300, 200);
        FixedElement a = new();
        FixedElement b = new();
        StackPanel other = new();
        TabItem first = new() { Header = "1", Content = a };
        TabItem second = new() { Header = "2", Content = b };
        TabControl tabControl = new();
        tabControl.SetItems(new[] { first, second });
        tabControl.SelectedIndex = 0;
        ContentPresenter? presenter = null;
        tabControl.ComponentTemplate = new ComponentTemplate<TabControl>("tabs", context =>
            presenter = new ContentPresenter { Content = context.Owner.SelectedContent });
        second.Selected += (_, _) => presenter!.Content = second.Content;
        root.VisualChildren.Add(tabControl);
        root.VisualChildren.Add(other);
        root.ProcessFrame();

        tabControl.SelectedIndex = 1;

        Assert.Same(presenter, b.LogicalParent);
        Assert.Same(first, a.LogicalParent);
        Assert.Throws<InvalidOperationException>(() => other.Children.Add(a));
    }

    [Fact]
    public void DeselectedTabOwnsTheElementContentWhenTheSelectionChangedHandlerThatTookItThrows()
    {
        UIRoot root = new(300, 200);
        FixedElement a = new();
        FixedElement b = new();
        TabItem first = new() { Header = "1", Content = a };
        TabItem second = new() { Header = "2", Content = b };
        TabControl tabControl = new();
        tabControl.SetItems(new[] { first, second });
        tabControl.SelectedIndex = 0;
        ContentPresenter? presenter = null;
        tabControl.ComponentTemplate = new ComponentTemplate<TabControl>("tabs", context =>
            presenter = new ContentPresenter { Content = context.Owner.SelectedContent });
        tabControl.SelectionChanged += (_, _) =>
        {
            presenter!.Content = tabControl.SelectedContent;
            throw new InvalidOperationException("handler failed");
        };
        root.VisualChildren.Add(tabControl);
        root.ProcessFrame();

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() => tabControl.SelectedIndex = 1);

        Assert.Equal("handler failed", exception.Message);
        Assert.Same(presenter, b.LogicalParent);
        Assert.Same(first, a.LogicalParent);
    }

    [Fact]
    public void ElementContentOfTheSelectedTabStaysOwnedWhenTheComponentTemplateDoesNotShowIt()
    {
        UIRoot root = new(300, 200);
        FixedElement a = new();
        FixedElement b = new();
        FixedElement other = new();
        TabItem first = new() { Header = "1", Content = a };
        TabItem second = new() { Header = "2", Content = b };
        TabControl tabControl = new();
        tabControl.SetItems(new[] { first, second });
        tabControl.SelectedIndex = 0;
        tabControl.ComponentTemplate = new ComponentTemplate<TabControl>("plain", _ => new Border());
        root.VisualChildren.Add(tabControl);
        root.VisualChildren.Add(other);
        root.ProcessFrame();

        Assert.Same(first, a.LogicalParent);
        Assert.Null(a.VisualParent);
        Assert.Throws<InvalidOperationException>(() => other.LogicalChildren.Add(a));

        tabControl.SelectedIndex = 1;
        root.ProcessFrame();

        Assert.Same(second, b.LogicalParent);
        Assert.Null(b.VisualParent);
        InvalidOperationException rejected = Assert.Throws<InvalidOperationException>(() => other.LogicalChildren.Add(b));
        Assert.Equal("Element must be removed from its current parent before reparenting.", rejected.Message);
        Assert.Throws<InvalidOperationException>(() => new TabItem().Content = b);
        Assert.Same(first, a.LogicalParent);
        Assert.Empty(other.LogicalChildren);
    }

    [Fact]
    public void ElementContentKeptByTheSelectedTabGoesToTheNextAspectComponentTemplateThatShowsIt()
    {
        UIRoot root = new(300, 200);
        FixedElement a = new();
        ContentPresenter? presenter = null;
        ComponentTemplate<TabControl> plain = new("Scoped.Plain", _ => new Border());
        ComponentTemplate<TabControl> showing = new("Scoped.Showing", context =>
            presenter = new ContentPresenter { Content = context.Owner.SelectedContent });
        TabItem first = new() { Header = "1", Content = a };
        TabControl tabControl = new() { ComponentTemplateKey = "Scoped.Tabs" };
        tabControl.SetItems(new[] { first, new TabItem { Header = "2" } });
        tabControl.SelectedIndex = 0;
        Border scope = new() { Child = tabControl };
        scope.Resources["Templates"] = AspectPackage.Create("Scoped.Plain")
            .Components(components => components.AddTemplate(
                new ComponentTemplateDefinition("Scoped.Tabs", typeof(TabControl), plain)))
            .Build();
        root.VisualChildren.Add(scope);
        root.ProcessFrame();

        Assert.Same(first, a.LogicalParent);

        scope.Resources["Templates"] = AspectPackage.Create("Scoped.Showing")
            .Components(components => components.AddTemplate(
                new ComponentTemplateDefinition("Scoped.Tabs", typeof(TabControl), showing)))
            .Build();
        root.ProcessFrame();
        root.ProcessFrame();

        Assert.NotNull(presenter);
        Assert.Same(presenter, a.LogicalParent);
        Assert.Same(presenter, a.VisualParent);
    }

    [Fact]
    public void ElementContentMovesBetweenTheContentAreaAndTheComponentTemplate()
    {
        UIRoot root = new(300, 200);
        FixedElement a = new();
        TabItem first = new() { Header = "1", Content = a };
        TabControl tabControl = new();
        tabControl.SetItems(new[] { first, new TabItem { Header = "2" } });
        root.VisualChildren.Add(tabControl);
        tabControl.SelectedIndex = 0;
        root.ProcessFrame();
        Assert.True(HasVisualAncestor(a, tabControl), "a is not in the content area before the template");
        ContentPresenter? presenter = null;

        tabControl.ComponentTemplate = new ComponentTemplate<TabControl>("tabs", context =>
            presenter = new ContentPresenter { Content = context.Owner.SelectedContent });
        root.ProcessFrame();

        Assert.Same(presenter, a.VisualParent);

        tabControl.ComponentTemplate = null;
        root.ProcessFrame();

        Assert.True(HasVisualAncestor(a, tabControl), "a is not under the TabControl after the template was removed");
        Assert.NotSame(presenter, a.VisualParent);
        Assert.False(HasVisualAncestor(a, first), "a is hosted inside its TabItem");
        Assert.True(a.ArrangedBounds.Width > 0 && a.ArrangedBounds.Height > 0, $"a arranged {a.ArrangedBounds}");
    }

    [Fact]
    public void ElementContentMovesFromTheContentAreaToAComponentTemplateKeyThatResolvesLater()
    {
        UIRoot root = new(300, 200);
        FixedElement a = new();
        TabItem first = new() { Header = "1", Content = a };
        TabControl tabControl = new() { ComponentTemplateKey = "App.Tabs" };
        tabControl.SetItems(new[] { first, new TabItem { Header = "2" } });
        tabControl.SelectedIndex = 0;
        root.VisualChildren.Add(tabControl);
        root.ProcessFrame();
        Assert.Null(tabControl.ComponentTemplateInstance);
        Assert.True(HasVisualAncestor(a, tabControl), "a is not in the content area before the key resolves");
        ContentPresenter? presenter = null;
        ComponentTemplate<TabControl> template = new("App.Tabs", context =>
            presenter = new ContentPresenter { Content = context.Owner.SelectedContent });

        root.AspectRegistry.Register(AspectPackage.Create("App")
            .Components(components => components.AddTemplate(
                new ComponentTemplateDefinition("App.Tabs", typeof(TabControl), template))));
        root.AspectProcessor.Process(tabControl);
        root.ProcessFrame();

        Assert.NotNull(tabControl.ComponentTemplateInstance);
        Assert.NotNull(presenter);
        Assert.Same(presenter, a.LogicalParent);
        Assert.Same(presenter, a.VisualParent);
    }

    [Fact]
    public void ElementContentOfTheSelectedTabStaysOwnedWhenTheContentTemplateDoesNotShowIt()
    {
        UIRoot root = new(300, 200);
        FixedElement a = new();
        FixedElement other = new();
        FixedElement? made = null;
        ContentTemplate<FixedElement> template = new("content", key: null, priority: 0, _ => made = new FixedElement());
        TabItem first = new() { Header = "H1", Content = a };
        TabControl tabControl = new() { ContentTemplate = template };
        tabControl.SetItems(new[] { first, new TabItem { Header = "H2" } });
        root.VisualChildren.Add(tabControl);
        root.VisualChildren.Add(other);
        tabControl.SelectedIndex = 0;
        root.ProcessFrame();

        Assert.True(made is not null && HasVisualAncestor(made, tabControl), "the template element is not in the content area");
        Assert.Same(first, a.LogicalParent);
        Assert.Null(a.VisualParent);
        InvalidOperationException rejected = Assert.Throws<InvalidOperationException>(() => other.LogicalChildren.Add(a));
        Assert.Equal("Element must be removed from its current parent before reparenting.", rejected.Message);
        Assert.Empty(other.LogicalChildren);
        Assert.Throws<InvalidOperationException>(() => new TabItem().Content = a);

        tabControl.Invalidate(Cerneala.UI.Invalidation.InvalidationFlags.Measure, "Measure the content area again");
        root.ProcessFrame();

        Assert.Same(first, a.LogicalParent);
        Assert.Throws<InvalidOperationException>(() => other.LogicalChildren.Add(a));

        tabControl.ContentTemplate = null;
        root.ProcessFrame();

        Assert.True(HasVisualAncestor(a, tabControl), "a is not in the content area after the ContentTemplate was removed");
        Assert.False(HasVisualAncestor(a, first), "a is hosted inside its TabItem");
        Assert.True(a.ArrangedBounds.Width > 0 && a.ArrangedBounds.Height > 0, $"a arranged {a.ArrangedBounds}");
    }

    [Fact]
    public void ElementContentOfTheSelectedTabStaysOwnedWhenAnAspectContentTemplateDoesNotShowIt()
    {
        UIRoot root = new(300, 200);
        FixedElement a = new();
        FixedElement other = new();
        ContentTemplate<FixedElement> template = new("Scoped.Fixed", key: null, priority: 0, _ => new FixedElement());
        TabItem first = new() { Header = "H1", Content = a };
        TabControl tabControl = new();
        tabControl.SetItems(new[] { first, new TabItem { Header = "H2" } });
        Border scope = new() { Child = tabControl };
        scope.Resources.Add("Templates", AspectPackage.Create("Scoped")
            .Content(content => content.Add(new ContentTemplateDefinition("Scoped.Fixed", typeof(FixedElement), null, template)))
            .Build());
        root.VisualChildren.Add(scope);
        root.VisualChildren.Add(other);
        tabControl.SelectedIndex = 0;
        root.ProcessFrame();

        Assert.Same(first, a.LogicalParent);
        Assert.Null(a.VisualParent);
        Assert.Throws<InvalidOperationException>(() => other.LogicalChildren.Add(a));

        scope.Resources.Remove("Templates");
        root.ProcessFrame();

        Assert.True(HasVisualAncestor(a, tabControl), "a is not in the content area after the aspect content template was removed");
        Assert.False(HasVisualAncestor(a, first), "a is hosted inside its TabItem");
        Assert.True(a.ArrangedBounds.Width > 0 && a.ArrangedBounds.Height > 0, $"a arranged {a.ArrangedBounds}");
    }

    [Fact]
    public void DefaultTabControlLaysHeadersOutHorizontally()
    {
        UIRoot root = new(200, 100);
        TabItem first = new() { Header = new FixedElement(new LayoutSize(30, 10)) };
        TabItem second = new() { Header = new FixedElement(new LayoutSize(30, 10)) };
        TabControl tabControl = new();
        tabControl.SetItems(new[] { first, second });
        root.VisualChildren.Add(tabControl);
        root.ProcessFrame();

        Assert.True(
            second.ArrangedBounds.X > first.ArrangedBounds.X,
            $"first {first.ArrangedBounds}, second {second.ArrangedBounds}");
        Assert.Equal(first.ArrangedBounds.Y, second.ArrangedBounds.Y);
    }

    [Fact]
    public void TabControlContentTemplateBuildsTheContentAreaForTheSelectedDataItem()
    {
        UIRoot root = new(300, 200);
        int calls = 0;
        FixedElement? made = null;
        ContentTemplate<string> template = new("content", key: null, priority: 0, context =>
        {
            calls++;
            made = new FixedElement(new LayoutSize(50, 20));
            return made;
        });
        TabControl tabControl = new() { ContentTemplate = template, ItemsSource = new[] { "One", "Two" } };
        root.VisualChildren.Add(tabControl);
        tabControl.SelectedIndex = 0;
        root.ProcessFrame();

        Assert.Equal(1, calls);
        Assert.NotNull(made);
        Assert.True(HasVisualAncestor(made, tabControl), "the template element is not under the TabControl");
        Assert.False(HasTabItemAncestor(made), "the template element is inside a TabItem");
        Assert.True(made.ArrangedBounds.Width > 0 && made.ArrangedBounds.Height > 0, $"made arranged {made.ArrangedBounds}");
    }

    [Fact]
    public void TabItemContentTemplateWinsOverTabControlContentTemplate()
    {
        UIRoot root = new(300, 200);
        int tabCalls = 0;
        int controlCalls = 0;
        ContentTemplate<string> tabTemplate = new("tab", key: null, priority: 0, context =>
        {
            tabCalls++;
            return new FixedElement(new LayoutSize(50, 20));
        });
        ContentTemplate<string> controlTemplate = new("control", key: null, priority: 0, context =>
        {
            controlCalls++;
            return new FixedElement(new LayoutSize(60, 30));
        });
        TabItem first = new() { Header = "H1", Content = "C1", ContentTemplate = tabTemplate };
        TabItem second = new() { Header = "H2", Content = "C2" };
        TabControl tabControl = new() { ContentTemplate = controlTemplate };
        tabControl.SetItems(new[] { first, second });
        root.VisualChildren.Add(tabControl);
        tabControl.SelectedIndex = 0;
        root.ProcessFrame();

        Assert.Equal((1, 0), (tabCalls, controlCalls));

        tabControl.SelectedIndex = 1;
        root.ProcessFrame();

        Assert.Equal((1, 1), (tabCalls, controlCalls));
    }

    [Fact]
    public void SelectedContentAndSelectedContentTemplateFollowTheSelectedTab()
    {
        ContentTemplate<string> tabTemplate = new("tab", key: null, priority: 0, context => new FixedElement());
        ContentTemplate<string> controlTemplate = new("control", key: null, priority: 0, context => new FixedElement());
        TabItem first = new() { Header = "H1", Content = "C1", ContentTemplate = tabTemplate };
        TabItem second = new() { Header = "H2", Content = "C2" };
        TabControl tabControl = new() { ContentTemplate = controlTemplate };
        tabControl.SetItems(new[] { first, second });

        tabControl.SelectedIndex = 0;

        Assert.Equal("C1", tabControl.SelectedContent);
        Assert.Same(tabTemplate, tabControl.SelectedContentTemplate);

        tabControl.SelectedIndex = 1;

        Assert.Equal("C2", tabControl.SelectedContent);
        Assert.Same(controlTemplate, tabControl.SelectedContentTemplate);

        tabControl.SelectedIndex = -1;

        Assert.Null(tabControl.SelectedContent);
        Assert.Null(tabControl.SelectedContentTemplate);
    }

    [Fact]
    public void ItemTemplateKeyBuildsTheHeaderOfEachDataItemTab()
    {
        UIRoot root = new(300, 200);
        List<string?> built = [];
        ContentTemplate<string> keyed = new("keyed", key: "k", priority: 0, context =>
        {
            built.Add(context.Data);
            return new FixedElement(new LayoutSize(40, 20));
        });
        TabControl tabControl = new() { ItemTemplateKey = "k", ItemsSource = new[] { "One", "Two" } };
        tabControl.Templates.Add(keyed);
        root.VisualChildren.Add(tabControl);
        root.ProcessFrame();

        Assert.Equal(["One", "Two"], built);
        IReadOnlyDictionary<int, UIElement> realized = tabControl.ItemContainerGenerator.RealizedContainers;
        foreach (TabItem tab in new[] { (TabItem)realized[0], (TabItem)realized[1] })
        {
            Assert.Equal("k", tab.HeaderTemplateKey);
            Assert.Null(tab.HeaderTemplate);
            Assert.True(tab.DesiredSize.Width > 0 && tab.DesiredSize.Height > 0, $"desired {tab.DesiredSize}");
        }
    }

    // As in WPF, preparing a generated tab sets HeaderTemplate only when ItemTemplate is set, so a HeaderTemplate from
    // ItemContainerAspect applies otherwise.
    [Fact]
    public void ItemContainerAspectHeaderTemplateBuildsTheHeaderOfGeneratedTabsWithoutItemTemplate()
    {
        UIRoot root = new(300, 200);
        int calls = 0;
        ContentTemplate<string> header = new("aspect-header", key: null, priority: 0, context =>
        {
            calls++;
            return new FixedElement(new LayoutSize(20, 10));
        });
        TabControl tabControl = new()
        {
            ItemsSource = new[] { "A", "B" },
            ItemContainerAspect = new ElementAspect([
                new ElementAspectValue(TabItem.HeaderTemplateProperty, header),
                new ElementAspectValue(Control.PaddingProperty, new Thickness(6))
            ])
        };
        root.VisualChildren.Add(tabControl);
        root.ProcessFrame();

        Assert.Equal(2, calls);
        IReadOnlyDictionary<int, UIElement> realized = tabControl.ItemContainerGenerator.RealizedContainers;
        foreach (TabItem tab in new[] { (TabItem)realized[0], (TabItem)realized[1] })
        {
            Assert.Equal(new Thickness(6), tab.Padding);
            Assert.Same(header, tab.HeaderTemplate);
            Assert.Null(tab.HeaderTemplateKey);
        }
    }

    [Fact]
    public void ItemContainerAspectHeaderTemplateKeyBuildsTheHeaderOfGeneratedTabsWithoutItemTemplateKey()
    {
        UIRoot root = new(300, 200);
        List<string?> built = [];
        ContentTemplate<string> keyed = new("keyed", key: "k", priority: 0, context =>
        {
            built.Add(context.Data);
            return new FixedElement(new LayoutSize(40, 20));
        });
        TabControl tabControl = new()
        {
            ItemsSource = new[] { "One", "Two" },
            ItemContainerAspect = new ElementAspect([new ElementAspectValue(TabItem.HeaderTemplateKeyProperty, "k")])
        };
        tabControl.Templates.Add(keyed);
        root.VisualChildren.Add(tabControl);
        root.ProcessFrame();

        Assert.Equal(["One", "Two"], built);
        IReadOnlyDictionary<int, UIElement> realized = tabControl.ItemContainerGenerator.RealizedContainers;
        foreach (TabItem tab in new[] { (TabItem)realized[0], (TabItem)realized[1] })
        {
            Assert.Equal("k", tab.HeaderTemplateKey);
            Assert.Null(tab.HeaderTemplate);
        }
    }

    [Fact]
    public void ItemTemplateWinsOverItemContainerAspectHeaderTemplate()
    {
        UIRoot root = new(300, 200);
        int itemCalls = 0;
        int aspectCalls = 0;
        ContentTemplate<string> itemTemplate = new("item", key: null, priority: 0, context =>
        {
            itemCalls++;
            return new FixedElement(new LayoutSize(20, 10));
        });
        ContentTemplate<string> aspectHeader = new("aspect-header", key: null, priority: 0, context =>
        {
            aspectCalls++;
            return new FixedElement(new LayoutSize(20, 10));
        });
        TabControl tabControl = new()
        {
            ItemTemplate = itemTemplate,
            ItemsSource = new[] { "A", "B" },
            ItemContainerAspect = new ElementAspect([new ElementAspectValue(TabItem.HeaderTemplateProperty, aspectHeader)])
        };
        root.VisualChildren.Add(tabControl);
        root.ProcessFrame();

        Assert.Equal((2, 0), (itemCalls, aspectCalls));
        Assert.Same(itemTemplate, ((TabItem)tabControl.ItemContainerGenerator.RealizedContainers[0]).HeaderTemplate);
    }

    // Clearing ItemTemplate clears and recycles the generated tabs; the recycled tabs are prepared again without an
    // ItemTemplate, so the value the previous preparation set is gone and the aspect's HeaderTemplate applies.
    [Fact]
    public void RecycledGeneratedTabUsesItemContainerAspectHeaderTemplateAfterItemTemplateIsCleared()
    {
        UIRoot root = new(300, 200);
        int aspectCalls = 0;
        ContentTemplate<string> itemTemplate = new("item", key: null, priority: 0, context => new FixedElement(new LayoutSize(20, 10)));
        ContentTemplate<string> aspectHeader = new("aspect-header", key: null, priority: 0, context =>
        {
            aspectCalls++;
            return new FixedElement(new LayoutSize(20, 10));
        });
        TabControl tabControl = new()
        {
            ItemTemplate = itemTemplate,
            ItemsSource = new[] { "A", "B" },
            ItemContainerAspect = new ElementAspect([new ElementAspectValue(TabItem.HeaderTemplateProperty, aspectHeader)])
        };
        root.VisualChildren.Add(tabControl);
        root.ProcessFrame();
        UIElement[] before = [tabControl.ItemContainerGenerator.RealizedContainers[0], tabControl.ItemContainerGenerator.RealizedContainers[1]];

        tabControl.ItemTemplate = null;
        root.ProcessFrame();

        IReadOnlyDictionary<int, UIElement> realized = tabControl.ItemContainerGenerator.RealizedContainers;
        UIElement[] after = [realized[0], realized[1]];
        Assert.All(after, tab => Assert.Contains(tab, before));
        Assert.All(after, tab => Assert.Same(aspectHeader, ((TabItem)tab).HeaderTemplate));
        Assert.Equal(2, aspectCalls);
    }

    [Fact]
    public void HeaderTemplateReplacesAHeaderElementAndClearingItHostsTheElementAgain()
    {
        UIRoot root = new(300, 200);
        int calls = 0;
        FixedElement header = new(new LayoutSize(30, 10));
        ContentTemplate<UIElement> template = new("header", key: null, priority: 0, context =>
        {
            calls++;
            return new FixedElement(new LayoutSize(40, 12));
        });
        TabItem tab = new() { Header = header, HeaderTemplate = template };
        TabControl tabControl = new();
        tabControl.SetItems(new[] { tab });
        root.VisualChildren.Add(tabControl);
        root.ProcessFrame();

        Assert.Equal(1, calls);
        Assert.Null(header.VisualParent);

        tab.HeaderTemplate = null;
        root.ProcessFrame();

        Assert.Same(tab, header.VisualParent);
        Assert.True(header.ArrangedBounds.Width > 0 && header.ArrangedBounds.Height > 0, $"header arranged {header.ArrangedBounds}");
    }

    [Fact]
    public void ChangingTheSelectedTabContentUpdatesTheContentArea()
    {
        UIRoot root = new(300, 200);
        TabItem first = new() { Header = "H1", Content = "C1" };
        TabItem second = new() { Header = "H2", Content = "C2" };
        TabControl tabControl = new();
        tabControl.SetItems(new[] { first, second });
        root.VisualChildren.Add(tabControl);
        tabControl.SelectedIndex = 0;
        root.ProcessFrame();

        first.Content = "C1b";
        root.ProcessFrame();

        AssertContentAreaShows(tabControl, new[] { first, second }, "C1b", "C1");
    }

    [Fact]
    public void TabControlDeclaresItemsPresenterTemplatePart()
    {
        TemplatePartAttribute part = Assert.Single(
            typeof(TabControl).GetCustomAttributes(typeof(TemplatePartAttribute), true).Cast<TemplatePartAttribute>());

        Assert.Equal("PART_ItemsPresenter", part.Name);
        Assert.Equal(typeof(ItemsPresenter), part.Type);
    }

    [Fact]
    public void ComponentTemplateItemsPresenterPartHostsTheTabsAndAClickOnAHeaderSelectsItsTab()
    {
        UIRoot root = new(300, 200);
        TabItem first = new() { Header = "One", Content = "C1" };
        TabItem second = new() { Header = "Two", Content = "C2" };
        TabControl tabControl = new();
        tabControl.SetItems(new[] { first, second });
        tabControl.SelectedIndex = 0;
        ItemsPresenter? headers = null;
        tabControl.ComponentTemplate = HeadersAboveContentTemplate(presenter => headers = presenter);
        root.VisualChildren.Add(tabControl);
        root.ProcessFrame();

        Assert.NotNull(headers);
        Assert.Same(tabControl, headers.ItemsOwner);
        Assert.True(HasVisualAncestor(first, headers), "the first tab is not under the template's ItemsPresenter");
        Assert.True(HasVisualAncestor(second, headers), "the second tab is not under the template's ItemsPresenter");
        Assert.DoesNotContain(tabControl.VisualChildren, child => child is ItemsPresenter && !ReferenceEquals(child, headers));
        // The template puts a 30 pixel spacer above the headers, so the header row is below the spacer.
        Assert.True(second.ArrangedBounds.Y >= 30 && second.ArrangedBounds.Width > 0, $"second arranged {second.ArrangedBounds}");

        float x = second.ArrangedBounds.X + second.ArrangedBounds.Width / 2;
        float y = second.ArrangedBounds.Y + second.ArrangedBounds.Height / 2;
        ElementInputBridge bridge = new();
        bridge.Dispatch(root, PointerFrame(x, y, currentDown: true));
        bridge.Dispatch(root, PointerFrame(x, y, previousDown: true));

        Assert.Equal(1, tabControl.SelectedIndex);
        Assert.True(second.IsSelected);
    }

    [Fact]
    public void ComponentTemplateItemsPresenterPartHostsTheGeneratedTabsOfItemsSource()
    {
        UIRoot root = new(300, 200);
        TabControl tabControl = new() { ItemsSource = new[] { "A", "B" } };
        tabControl.SelectedIndex = 1;
        ItemsPresenter? headers = null;
        tabControl.ComponentTemplate = HeadersAboveContentTemplate(presenter => headers = presenter);
        root.VisualChildren.Add(tabControl);
        root.ProcessFrame();

        Assert.NotNull(headers);
        Assert.Same(tabControl, headers.ItemsOwner);
        Assert.Equal(2, tabControl.RealizedItemCount);
        foreach (UIElement container in tabControl.ItemContainerGenerator.RealizedContainers.Values)
        {
            TabItem tab = Assert.IsType<TabItem>(container);
            Assert.True(HasVisualAncestor(tab, headers), $"the tab of '{tab.Header}' is not under the template's ItemsPresenter");
            Assert.True(tab.ArrangedBounds.Y >= 30 && tab.ArrangedBounds.Width > 0, $"'{tab.Header}' arranged {tab.ArrangedBounds}");
        }

        Assert.Equal("B", tabControl.SelectedTabItem!.Header);
    }

    [Fact]
    public void RemovingTheComponentTemplateHostsTheTabsInTheBuiltInHeaderRowAgain()
    {
        UIRoot root = new(300, 200);
        TabItem first = new() { Header = "One", Content = "C1" };
        TabItem second = new() { Header = "Two", Content = "C2" };
        TabControl tabControl = new();
        tabControl.SetItems(new[] { first, second });
        tabControl.SelectedIndex = 0;
        ItemsPresenter? headers = null;
        tabControl.ComponentTemplate = HeadersAboveContentTemplate(presenter => headers = presenter);
        root.VisualChildren.Add(tabControl);
        root.ProcessFrame();
        Assert.True(HasVisualAncestor(first, headers!), "the first tab is not under the template's ItemsPresenter");

        tabControl.ComponentTemplate = null;
        root.ProcessFrame();

        Assert.Null(headers!.ItemsOwner);
        Assert.Same(tabControl, tabControl.ItemsPresenter.ItemsOwner);
        Assert.Same(tabControl, tabControl.ItemsPresenter.VisualParent);
        Assert.True(HasVisualAncestor(first, tabControl.ItemsPresenter), "the first tab is not in the built-in header row");
        Assert.True(HasVisualAncestor(second, tabControl.ItemsPresenter), "the second tab is not in the built-in header row");
        Assert.Equal(0, first.ArrangedBounds.Y);
        Assert.True(second.ArrangedBounds.Width > 0, $"second arranged {second.ArrangedBounds}");
        AssertContentAreaShows(tabControl, new[] { first, second }, "C1", "C2");
    }

    [Fact]
    public void ComponentTemplateWithoutItemsPresenterPartIsAppliedAndTheTabsStayInTheBuiltInHeaderRow()
    {
        UIRoot root = new(300, 200);
        TabItem first = new() { Header = "One", Content = "C1" };
        TabControl tabControl = new();
        tabControl.SetItems(new[] { first, new TabItem { Header = "Two", Content = "C2" } });
        tabControl.SelectedIndex = 0;
        ContentPresenter? page = null;
        tabControl.ComponentTemplate = new ComponentTemplate<TabControl>("tabs", context =>
            page = new ContentPresenter { Content = context.Owner.SelectedContent });
        root.VisualChildren.Add(tabControl);
        root.ProcessFrame();

        Assert.NotNull(tabControl.ComponentTemplateInstance);
        Assert.Same(tabControl, tabControl.ItemsPresenter.ItemsOwner);
        Assert.Same(tabControl, tabControl.ItemsPresenter.VisualParent);
        Assert.False(HasVisualAncestor(first, page!), "the first tab is inside the template");
    }

    // A 30 pixel spacer, the PART_ItemsPresenter header row, and a ContentPresenter that shows SelectedContent.
    private static ComponentTemplate<TabControl> HeadersAboveContentTemplate(Action<ItemsPresenter> created)
    {
        return new ComponentTemplate<TabControl>("tabs", context =>
        {
            ItemsPresenter headers = new();
            created(headers);
            StackPanel root = new();
            root.Children.Add(new FixedElement(new LayoutSize(10, 30)));
            root.Children.Add(headers);
            root.Children.Add(new ContentPresenter { Content = context.Owner.SelectedContent });
            context.RequirePart("PART_ItemsPresenter", headers);
            return root;
        });
    }

    private static bool HasTabItemAncestor(UIElement element)
    {
        for (UIElement? current = element.VisualParent; current is not null; current = current.VisualParent)
        {
            if (current is TabItem)
            {
                return true;
            }
        }

        return false;
    }

    // The shown content is a TextBlock outside every tab, below the tabs; the previous content is gone.
    private static void AssertContentAreaShows(TabControl tabControl, IReadOnlyList<UIElement> tabs, string expected, string absent)
    {
        List<TextBlock> outside = FindTextBlocksOutsideTabItems(tabControl);
        TextBlock shown = Assert.Single(outside, text => text.Text == expected);
        Assert.DoesNotContain(outside, text => text.Text == absent);
        Assert.True(shown.ArrangedBounds.Width > 0 && shown.ArrangedBounds.Height > 0, $"'{expected}' arranged {shown.ArrangedBounds}");
        float headerBottom = tabs.Max(tab => tab.ArrangedBounds.Y + tab.ArrangedBounds.Height);
        Assert.True(shown.ArrangedBounds.Y >= headerBottom, $"'{expected}' at {shown.ArrangedBounds} is not below the header row bottom {headerBottom}");
    }

    private static bool HasVisualAncestor(UIElement element, UIElement ancestor)
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

    private static List<TextBlock> FindTextBlocksOutsideTabItems(UIElement element)
    {
        List<TextBlock> found = [];
        foreach (UIElement child in element.VisualChildren)
        {
            if (child is TabItem)
            {
                continue;
            }

            if (child is TextBlock textBlock)
            {
                found.Add(textBlock);
            }

            found.AddRange(FindTextBlocksOutsideTabItems(child));
        }

        return found;
    }

    private static void Layout(TabControl tabControl)
    {
        tabControl.Measure(new MeasureContext(new LayoutSize(100, 100)));
        tabControl.Arrange(new ArrangeContext(new LayoutRect(0, 0, 100, 100)));
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

    private sealed class FailingPreparedTabControl : TabControl
    {
        public int FailIndex { get; set; } = -1;

        protected internal override void OnItemContainerPrepared(UIElement container, int index)
        {
            if (index == FailIndex)
            {
                throw new InvalidOperationException("container prepared callback failed");
            }

            base.OnItemContainerPrepared(container, index);
        }
    }

    private sealed class FailingContentTabControl : TabControl
    {
        public object? FailFor { get; set; }

        public int RemainingFailures { get; set; }

        protected override void PrepareItemContent(UIElement container, int index, object? item)
        {
            if (FailFor is not null && Equals(item, FailFor) && RemainingFailures > 0)
            {
                RemainingFailures--;
                throw new InvalidOperationException("content preparation failed");
            }

            base.PrepareItemContent(container, index, item);
        }
    }

    private sealed class FixedElement : UIElement
    {
        private readonly LayoutSize size;

        public FixedElement()
            : this(new LayoutSize(20, 10))
        {
        }

        public FixedElement(LayoutSize size)
        {
            this.size = size;
        }

        protected override LayoutSize MeasureCore(MeasureContext context)
        {
            return size;
        }
    }
}
