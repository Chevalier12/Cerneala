using Cerneala.UI.Controls;
using Cerneala.UI.Elements;
using Cerneala.UI.Input;
using Cerneala.UI.Layout;

namespace Cerneala.Tests.Controls;

public sealed class ComboBoxTests
{
    [Fact]
    public void ComboBoxUsesSharedSelectorSelectionState()
    {
        ComboBox comboBox = new();
        comboBox.SetItems(new[] { "one", "two" });

        comboBox.SelectedIndex = 1;

        Assert.Equal(1, comboBox.SelectedIndex);
        Assert.Equal("two", comboBox.SelectedItem);
        Assert.True(comboBox.SelectionModel.IsSelected(1));
    }

    [Fact]
    public void ComboBoxRealizedItemsParticipateInRetainedInputRouting()
    {
        UIRoot root = new(100, 100);
        ComboBox comboBox = new()
        {
            ItemsPanel = new ItemsPanelTemplate(() => new StackPanel())
        };
        comboBox.SetItems(new UIElement[] { new FixedElement(), new FixedElement() });
        root.VisualChildren.Add(comboBox);
        comboBox.Measure(new MeasureContext(new LayoutSize(100, 100)));
        comboBox.Arrange(new ArrangeContext(new LayoutRect(0, 0, 100, 100)));

        ElementInputBridge bridge = new();
        bridge.Dispatch(root, PointerFrame(5, 15, currentDown: true));
        bridge.Dispatch(root, PointerFrame(5, 15, previousDown: true));

        Assert.Equal(1, comboBox.SelectedIndex);
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
