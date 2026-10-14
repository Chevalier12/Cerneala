using Cerneala.UI.Controls;
using Cerneala.UI.Controls.Items;
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
        tabControl.SetItems(new[]
        {
            new TabItem { Header = "One", Content = new FixedElement() },
            new TabItem { Header = "Two", Content = new FixedElement() }
        });
        root.VisualChildren.Add(tabControl);
        tabControl.Measure(new MeasureContext(new LayoutSize(100, 100)));
        tabControl.Arrange(new ArrangeContext(new LayoutRect(0, 0, 100, 100)));

        ElementInputBridge bridge = new();
        bridge.Dispatch(root, PointerFrame(5, 15, currentDown: true));
        bridge.Dispatch(root, PointerFrame(5, 15, previousDown: true));

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
        Assert.Equal(new LayoutRect(0, 0, 120, 20), wideHeader.ArrangedBounds);
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
