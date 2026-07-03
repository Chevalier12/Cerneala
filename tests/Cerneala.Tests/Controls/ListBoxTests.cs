using Cerneala.UI.Controls;
using Cerneala.UI.Elements;
using Cerneala.UI.Input;
using Cerneala.UI.Layout;
using Cerneala.UI.Layout.Virtualization;

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
        protected override LayoutSize MeasureCore(MeasureContext context)
        {
            return new LayoutSize(20, 10);
        }
    }
}
