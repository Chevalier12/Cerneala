using Cerneala.UI.Controls;
using Cerneala.UI.Elements;
using Cerneala.UI.Input;
using Cerneala.UI.Layout;
using Cerneala.UI.Layout.Panels;

namespace Cerneala.Tests.Controls;

public sealed class SliderTests
{
    [Fact]
    public void SliderValueFollowsThumbDrag()
    {
        UIRoot root = new(200, 100);
        Slider slider = new()
        {
            Minimum = 0,
            Maximum = 100,
            Value = 0
        };
        slider.Measure(new MeasureContext(new LayoutSize(110, 20)));
        slider.Arrange(new ArrangeContext(new LayoutRect(0, 0, 110, 20)));
        root.VisualChildren.Add(slider);
        ElementInputBridge bridge = new();

        bridge.Dispatch(root, PointerFrame(5, 5, currentDown: true));
        bridge.Dispatch(root, PointerFrame(5, 5, 55, 5, previousDown: true, currentDown: true));

        Assert.Equal(50, slider.Value);
    }

    [Fact]
    public void SliderOrientationAffectsTrackLayout()
    {
        Slider slider = new()
        {
            Orientation = Orientation.Vertical,
            Minimum = 0,
            Maximum = 100,
            Value = 50
        };

        slider.Measure(new MeasureContext(new LayoutSize(20, 110)));
        slider.Arrange(new ArrangeContext(new LayoutRect(0, 0, 20, 110)));

        Assert.Equal(new LayoutRect(0, 50, 20, 10), slider.Track.Thumb.ArrangedBounds);
    }

    private static InputFrame PointerFrame(
        float previousX,
        float previousY,
        float currentX,
        float currentY,
        bool previousDown = false,
        bool currentDown = false)
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

    private static InputFrame PointerFrame(float x, float y, bool currentDown = false)
    {
        return PointerFrame(x, y, x, y, currentDown: currentDown);
    }
}
