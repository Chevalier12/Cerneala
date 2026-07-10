using Cerneala.UI.Controls;
using Cerneala.UI.Controls.Primitives;
using Cerneala.UI.Elements;
using Cerneala.UI.Input;
using Cerneala.UI.Layout;
using Cerneala.UI.Layout.Panels;

namespace Cerneala.Tests.Controls;

public sealed class ScrollBarTests
{
    [Fact]
    public void ScrollBarValueFollowsThumbDrag()
    {
        UIRoot root = new(200, 100);
        ScrollBar scrollBar = new()
        {
            Orientation = Orientation.Horizontal,
            Minimum = 0,
            Maximum = 100,
            Value = 0
        };
        scrollBar.Measure(new MeasureContext(new LayoutSize(110, 12)));
        scrollBar.Arrange(new ArrangeContext(new LayoutRect(0, 0, 110, 12)));
        root.VisualChildren.Add(scrollBar);
        ElementInputBridge bridge = new();

        bridge.Dispatch(root, PointerFrame(5, 5, currentDown: true));
        bridge.Dispatch(root, PointerFrame(5, 5, 55, 5, previousDown: true, currentDown: true));

        Assert.Equal(50, scrollBar.Value);
    }

    [Fact]
    public void ScrollBarOrientationAffectsMeasure()
    {
        ScrollBar scrollBar = new();

        scrollBar.Orientation = Orientation.Vertical;
        LayoutSize vertical = scrollBar.Measure(new MeasureContext(new LayoutSize(100, 100)));
        scrollBar.Orientation = Orientation.Horizontal;
        LayoutSize horizontal = scrollBar.Measure(new MeasureContext(new LayoutSize(100, 100)));

        Assert.True(vertical.Height > vertical.Width);
        Assert.True(horizontal.Width > horizontal.Height);
    }

    [Fact]
    public void ScrollBarInitializesTrackToCurrentOrientation()
    {
        ScrollBar vertical = new();
        ScrollBar horizontal = new() { Orientation = Orientation.Horizontal };

        Assert.Equal(Orientation.Vertical, vertical.Track.Orientation);
        Assert.Equal(Orientation.Horizontal, horizontal.Track.Orientation);
    }

    [Fact]
    public void TemplatedScrollBarKeepsGeneratedRootStable()
    {
        ScrollBar scrollBar = new();
        UIElement root = new();
        scrollBar.ComponentTemplate = new ComponentTemplate<ScrollBar>("test", _ => root);

        scrollBar.Measure(new MeasureContext(new LayoutSize(100, 100)));
        scrollBar.Measure(new MeasureContext(new LayoutSize(100, 100)));

        Assert.Same(root, scrollBar.ComponentTemplateInstance!.Root);
        Assert.DoesNotContain(scrollBar.Track, scrollBar.VisualChildren);
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
