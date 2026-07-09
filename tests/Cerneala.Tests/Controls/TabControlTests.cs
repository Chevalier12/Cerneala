using Cerneala.UI.Controls;
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
    public void TabControlClickSelectsTabItem()
    {
        UIRoot root = new(100, 100);
        TabControl tabControl = new()
        {
            ItemsPanel = new ItemsPanelTemplate(() => new StackPanel())
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
            ItemsPanel = new ItemsPanelTemplate(() => new StackPanel { Orientation = Orientation.Horizontal })
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
