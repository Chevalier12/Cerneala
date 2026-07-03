using Cerneala.UI.Controls;
using Cerneala.UI.Elements;
using Cerneala.UI.Input;
using Cerneala.UI.Layout;

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
