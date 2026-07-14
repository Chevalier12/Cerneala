using Cerneala.UI.Controls.Primitives;
using Cerneala.UI.Controls.Templates;
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
    public void ThumbFillsTrackWhenViewportHasNoScrollableRange()
    {
        Track track = new()
        {
            Minimum = 0,
            Maximum = 0,
            ViewportSize = 100,
            Orientation = Orientation.Horizontal
        };

        track.Measure(new MeasureContext(new LayoutSize(100, 20)));
        track.Arrange(new ArrangeContext(new LayoutRect(0, 0, 100, 20)));

        Assert.Equal(new LayoutRect(0, 0, 100, 20), track.Thumb.ArrangedBounds);
    }

    [Fact]
    public void ThumbLengthIsProportionalToViewportSize()
    {
        Track track = new()
        {
            Minimum = 0,
            Maximum = 80,
            ViewportSize = 20,
            Orientation = Orientation.Horizontal
        };

        track.Measure(new MeasureContext(new LayoutSize(100, 20)));
        track.Arrange(new ArrangeContext(new LayoutRect(0, 0, 100, 20)));

        Assert.Equal(new LayoutRect(0, 0, 20, 20), track.Thumb.ArrangedBounds);
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
    public void TemplateThumbIsTheActiveDragSource()
    {
        UIRoot root = new(200, 100);
        Thumb templateThumb = new();
        Track track = new()
        {
            Minimum = 0,
            Maximum = 100,
            ComponentTemplate = new ComponentTemplate<Track>("custom", context =>
            {
                context.RequirePart("PART_Thumb", templateThumb);
                return templateThumb;
            })
        };
        track.Measure(new MeasureContext(new LayoutSize(110, 20)));
        track.Arrange(new ArrangeContext(new LayoutRect(0, 0, 110, 20)));
        root.VisualChildren.Add(track);
        ElementInputBridge bridge = new();

        bridge.Dispatch(root, PointerFrame(5, 5, currentDown: true));
        bridge.Dispatch(root, PointerFrame(5, 5, 55, 5, previousDown: true, currentDown: true));

        Assert.Same(templateThumb, track.Thumb);
        Assert.Equal(50, track.Value);
    }

    [Fact]
    public void OldThumbStopsChangingTrackAfterTemplateSwap()
    {
        Thumb oldThumb = new();
        Thumb newThumb = new();
        Track track = new()
        {
            Minimum = 0,
            Maximum = 100,
            ComponentTemplate = ThumbTemplate("old", oldThumb)
        };
        track.Measure(new MeasureContext(new LayoutSize(110, 20)));
        track.Arrange(new ArrangeContext(new LayoutRect(0, 0, 110, 20)));

        track.ComponentTemplate = ThumbTemplate("new", newThumb);
        track.Measure(new MeasureContext(new LayoutSize(110, 20)));
        track.Arrange(new ArrangeContext(new LayoutRect(0, 0, 110, 20)));
        oldThumb.RaiseEvent(new DragDeltaEventArgs(Thumb.DragDeltaEvent, oldThumb, 50, 0, 50, 0));

        Assert.Same(newThumb, track.Thumb);
        Assert.Equal(0, track.Value);
    }

    [Fact]
    public void ClearingCustomTemplateRestoresDefaultThumb()
    {
        Thumb customThumb = new();
        Track track = new()
        {
            ComponentTemplate = ThumbTemplate("custom", customThumb)
        };

        track.ComponentTemplate = null;

        Assert.NotSame(customThumb, track.Thumb);
        Assert.NotNull(track.ComponentTemplate);
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
    public void TrackClicksBeforeAndAfterThumbUseLargeChange()
    {
        UIRoot root = new(200, 100);
        Track track = new()
        {
            Minimum = 0,
            Maximum = 100,
            Value = 50,
            LargeChange = 10
        };
        track.Measure(new MeasureContext(new LayoutSize(110, 20)));
        track.Arrange(new ArrangeContext(new LayoutRect(0, 0, 110, 20)));
        root.VisualChildren.Add(track);
        ElementInputBridge bridge = new();

        bridge.Dispatch(root, PointerFrame(20, 5, currentDown: true));
        bridge.Dispatch(root, PointerFrame(20, 5));
        Assert.Equal(40, track.Value);

        bridge.Dispatch(root, PointerFrame(90, 5, currentDown: true));
        Assert.Equal(50, track.Value);
    }

    [Fact]
    public void VerticalThumbDragUsesVerticalAxis()
    {
        UIRoot root = new(100, 200);
        Track track = new()
        {
            Minimum = 0,
            Maximum = 100,
            Orientation = Orientation.Vertical
        };
        track.Measure(new MeasureContext(new LayoutSize(20, 110)));
        track.Arrange(new ArrangeContext(new LayoutRect(0, 0, 20, 110)));
        root.VisualChildren.Add(track);
        ElementInputBridge bridge = new();

        bridge.Dispatch(root, PointerFrame(5, 5, currentDown: true));
        bridge.Dispatch(root, PointerFrame(5, 5, 55, 55, previousDown: true, currentDown: true));

        Assert.Equal(50, track.Value);
    }

    [Fact]
    public void TrackShorterThanMinimumThumbLengthRemainsStable()
    {
        Track track = new()
        {
            Minimum = 0,
            Maximum = 100,
            Value = 50
        };

        track.Measure(new MeasureContext(new LayoutSize(5, 5)));
        track.Arrange(new ArrangeContext(new LayoutRect(0, 0, 5, 5)));

        Assert.Equal(new LayoutRect(0, 0, 5, 5), track.Thumb.ArrangedBounds);
        Assert.Equal(0, track.ValueFromPoint(3, 3));
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

    private static ComponentTemplate<Track> ThumbTemplate(string name, Thumb thumb)
    {
        return new ComponentTemplate<Track>(name, context =>
        {
            context.RequirePart("PART_Thumb", thumb);
            return thumb;
        });
    }
}
