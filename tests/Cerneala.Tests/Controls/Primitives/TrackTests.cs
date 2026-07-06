using Cerneala.UI.Controls.Primitives;
using Cerneala.UI.Elements;
using Cerneala.UI.Input;
using Cerneala.UI.Layout;
using Cerneala.UI.Layout.Panels;

namespace Cerneala.Tests.Controls.Primitives;

public sealed class TrackTests
{
    [Fact]
    public void TrackPositionsThumbFromHorizontalValue()
    {
        Track track = new()
        {
            Minimum = 0,
            Maximum = 100,
            Value = 50,
            Orientation = Orientation.Horizontal
        };

        track.Measure(new MeasureContext(new LayoutSize(100, 20)));
        track.Arrange(new ArrangeContext(new LayoutRect(0, 0, 110, 20)));

        Assert.Equal(new LayoutRect(50, 0, 10, 20), track.Thumb.ArrangedBounds);
    }

    [Fact]
    public void ThumbDragUpdatesTrackValue()
    {
        UIRoot root = new(200, 100);
        Track track = new()
        {
            Minimum = 0,
            Maximum = 100,
            Value = 0
        };
        track.Measure(new MeasureContext(new LayoutSize(110, 20)));
        track.Arrange(new ArrangeContext(new LayoutRect(0, 0, 110, 20)));
        root.VisualChildren.Add(track);
        ElementInputBridge bridge = new();

        bridge.Dispatch(root, PointerFrame(5, 5, currentDown: true));
        bridge.Dispatch(root, PointerFrame(5, 5, 55, 5, previousDown: true, currentDown: true));

        Assert.Equal(50, track.Value);
    }

    [Fact]
    public void ThumbDragDoesNotRaiseValueChangedWhenValueIsClampedUnchanged()
    {
        UIRoot root = new(200, 100);
        Track track = new()
        {
            Minimum = 0,
            Maximum = 100,
            Value = 100
        };
        track.Measure(new MeasureContext(new LayoutSize(110, 20)));
        track.Arrange(new ArrangeContext(new LayoutRect(0, 0, 110, 20)));
        root.VisualChildren.Add(track);
        ElementInputBridge bridge = new();
        int changes = 0;
        track.ValueChanged += (_, _) => changes++;

        bridge.Dispatch(root, PointerFrame(105, 5, currentDown: true));
        bridge.Dispatch(root, PointerFrame(105, 5, 120, 5, previousDown: true, currentDown: true));

        Assert.Equal(100, track.Value);
        Assert.Equal(0, changes);
    }

    [Fact]
    public void TrackLargeChangeRegionsMoveValue()
    {
        Track track = new()
        {
            Minimum = 0,
            Maximum = 100,
            Value = 50,
            LargeChange = 10
        };
        track.Measure(new MeasureContext(new LayoutSize(110, 20)));
        track.Arrange(new ArrangeContext(new LayoutRect(0, 0, 110, 20)));

        track.DecreaseLarge();
        Assert.Equal(40, track.Value);

        track.IncreaseLarge();
        Assert.Equal(50, track.Value);
    }

    [Fact]
    public void EndpointChangesKeepRangeOrdered()
    {
        Track track = new()
        {
            Minimum = 0,
            Maximum = 10,
            Value = 5
        };

        track.Minimum = 20;

        Assert.Equal(20, track.Maximum);
        Assert.Equal(20, track.Value);

        track.Maximum = 5;

        Assert.Equal(5, track.Minimum);
        Assert.Equal(5, track.Value);
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
