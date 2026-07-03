using Cerneala.UI.Controls.Primitives;
using Cerneala.UI.Elements;
using Cerneala.UI.Input;
using Cerneala.UI.Layout;

namespace Cerneala.Tests.Controls;

public sealed class ToggleButtonTests
{
    [Fact]
    public void ToggleButtonTogglesOnCompletedLeftClick()
    {
        UIRoot root = RootWithToggle(out ToggleButton toggle);
        ElementInputBridge bridge = new();

        bridge.Dispatch(root, PointerFrame(10, 10, currentDown: true));
        bridge.Dispatch(root, PointerFrame(10, 10, previousDown: true));

        Assert.True(toggle.IsChecked);
    }

    [Fact]
    public void ToggleButtonDoesNotToggleCanceledClick()
    {
        UIRoot root = RootWithToggle(out ToggleButton toggle);
        UIElement other = new();
        other.Arrange(new ArrangeContext(new LayoutRect(50, 0, 40, 40)));
        root.VisualChildren.Add(other);
        ElementInputBridge bridge = new();

        bridge.Dispatch(root, PointerFrame(10, 10, currentDown: true));
        bridge.Dispatch(root, PointerFrame(10, 10, 60, 10, previousDown: true));

        Assert.False(toggle.IsChecked);
    }

    private static UIRoot RootWithToggle(out ToggleButton toggle)
    {
        UIRoot root = new(100, 100);
        toggle = new ToggleButton();
        toggle.Arrange(new ArrangeContext(new LayoutRect(0, 0, 40, 40)));
        root.VisualChildren.Add(toggle);
        return root;
    }

    private static InputFrame PointerFrame(float x, float y, bool previousDown = false, bool currentDown = false)
    {
        return PointerFrame(x, y, x, y, previousDown, currentDown);
    }

    private static InputFrame PointerFrame(float previousX, float previousY, float currentX, float currentY, bool previousDown = false, bool currentDown = false)
    {
        PointerSnapshot previous = PointerSnapshot.Empty.WithPosition(previousX, previousY);
        PointerSnapshot current = PointerSnapshot.Empty.WithPosition(currentX, currentY);
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
}
