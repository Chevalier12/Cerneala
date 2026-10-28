using Cerneala.UI.Controls.Primitives;
using Cerneala.UI.Controls.Templates;
using Cerneala.UI.Core;
using Cerneala.UI.Elements;
using Cerneala.UI.Hosting;
using Cerneala.UI.Input;
using Cerneala.UI.Layout;
using Cerneala.UI.Layout.Panels;
using Cerneala.UI.Media;
using StackPanel = Cerneala.UI.Controls.StackPanel;

namespace Cerneala.Tests.Controls.Primitives;

public sealed class TrackTests
{
    [Theory]
    [InlineData(float.NaN)]
    [InlineData(float.PositiveInfinity)]
    [InlineData(float.NegativeInfinity)]
    public void NonFiniteValueIsRejectedAndKeepsPreviousValue(float nonFinite)
    {
        Track track = new() { Minimum = 10, Maximum = 20, Value = 15 };

        Assert.Throws<ArgumentException>(() => track.Value = nonFinite);

        Assert.Equal(15, track.Value);
        track.Value = 25;
        Assert.Equal(20, track.Value);
    }

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

    // As WPF's vertical Track, whose values grow upward: the thumb for Minimum is at the bottom, y 100..110, and dragging
    // it up by 50 pixels raises the value by 50; the horizontal part of the movement does nothing.
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

        bridge.Dispatch(root, PointerFrame(5, 105, currentDown: true));
        bridge.Dispatch(root, PointerFrame(5, 105, 55, 55, previousDown: true, currentDown: true));

        Assert.Equal(50, track.Value);
    }

    // As WPF's Track template parts: the default template has a decrease and an increase RepeatButton that take no focus
    // and fill the track on each side of the thumb. For a horizontal track with value 50 of 0..100 in 110 pixels, the
    // decrease button is x 0..50, the thumb x 50..60 and the increase button x 60..110.
    [Fact]
    public void DefaultTemplateRepeatButtonsFillTrackOnEachSideOfThumb()
    {
        Track track = new() { Minimum = 0, Maximum = 100, Value = 50 };

        track.Measure(new MeasureContext(new LayoutSize(110, 20)));
        track.Arrange(new ArrangeContext(new LayoutRect(0, 0, 110, 20)));

        RepeatButton decrease = Assert.IsType<RepeatButton>(track.DecreaseRepeatButton);
        RepeatButton increase = Assert.IsType<RepeatButton>(track.IncreaseRepeatButton);
        Assert.Same(decrease, track.ComponentTemplateInstance!.Parts["PART_DecreaseRepeatButton"]);
        Assert.Same(increase, track.ComponentTemplateInstance.Parts["PART_IncreaseRepeatButton"]);
        Assert.Equal(new LayoutRect(0, 0, 50, 20), decrease.ArrangedBounds);
        Assert.Equal(new LayoutRect(50, 0, 10, 20), track.Thumb.ArrangedBounds);
        Assert.Equal(new LayoutRect(60, 0, 50, 20), increase.ArrangedBounds);
        Assert.False(decrease.Focusable);
        Assert.False(decrease.IsTabStop);
        Assert.False(increase.Focusable);
        Assert.False(increase.IsTabStop);
        Assert.Equal(
            ["PART_Thumb", "PART_DecreaseRepeatButton", "PART_IncreaseRepeatButton"],
            typeof(Track).GetCustomAttributes(typeof(TemplatePartAttribute), inherit: true)
                .Cast<TemplatePartAttribute>()
                .Select(part => part.Name));
    }

    // A vertical track has Minimum at the bottom: with value 25 of 0..100 in 110 pixels, the thumb is y 75..85, the
    // increase button above it is y 0..75 and the decrease button below it is y 85..110. IsDirectionReversed puts
    // Minimum at the top: the thumb is y 25..35, the decrease button y 0..25 and the increase button y 35..110.
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void VerticalTrackPlacesMinimumAtBottomUnlessReversed(bool reversed)
    {
        Track track = new() { Orientation = Orientation.Vertical, IsDirectionReversed = reversed, Minimum = 0, Maximum = 100, Value = 25 };

        track.Measure(new MeasureContext(new LayoutSize(20, 110)));
        track.Arrange(new ArrangeContext(new LayoutRect(0, 0, 20, 110)));

        LayoutRect decrease = track.DecreaseRepeatButton!.ArrangedBounds;
        LayoutRect increase = track.IncreaseRepeatButton!.ArrangedBounds;
        if (reversed)
        {
            Assert.Equal(new LayoutRect(0, 25, 20, 10), track.Thumb.ArrangedBounds);
            Assert.Equal(new LayoutRect(0, 0, 20, 25), decrease);
            Assert.Equal(new LayoutRect(0, 35, 20, 75), increase);
            Assert.Equal(0, track.ValueFromPoint(10, 5));
            Assert.Equal(100, track.ValueFromPoint(10, 105));
        }
        else
        {
            Assert.Equal(new LayoutRect(0, 75, 20, 10), track.Thumb.ArrangedBounds);
            Assert.Equal(new LayoutRect(0, 0, 20, 75), increase);
            Assert.Equal(new LayoutRect(0, 85, 20, 25), decrease);
            Assert.Equal(100, track.ValueFromPoint(10, 5));
            Assert.Equal(0, track.ValueFromPoint(10, 105));
        }
    }

    // A reversed horizontal track has Minimum at the right: with value 30 the thumb is x 70..80, the increase button
    // x 0..70 and the decrease button x 80..110; dragging the thumb 20 pixels to the left raises the value by 20, and a
    // press on the decrease button at the right lowers it by LargeChange.
    [Fact]
    public void ReversedHorizontalTrackPlacesMinimumAtRight()
    {
        UIRoot root = new(200, 100);
        Track track = new() { IsDirectionReversed = true, Minimum = 0, Maximum = 100, Value = 30, LargeChange = 10 };
        track.Measure(new MeasureContext(new LayoutSize(110, 20)));
        track.Arrange(new ArrangeContext(new LayoutRect(0, 0, 110, 20)));
        root.VisualChildren.Add(track);

        Assert.Equal(new LayoutRect(70, 0, 10, 20), track.Thumb.ArrangedBounds);
        Assert.Equal(new LayoutRect(0, 0, 70, 20), track.IncreaseRepeatButton!.ArrangedBounds);
        Assert.Equal(new LayoutRect(80, 0, 30, 20), track.DecreaseRepeatButton!.ArrangedBounds);
        Assert.Equal(100, track.ValueFromPoint(5, 10));
        Assert.Equal(0, track.ValueFromPoint(105, 10));

        ElementInputBridge bridge = new();
        bridge.Dispatch(root, PointerFrame(75, 5, currentDown: true));
        bridge.Dispatch(root, PointerFrame(75, 5, 55, 5, previousDown: true, currentDown: true));
        bridge.Dispatch(root, PointerFrame(55, 5, 55, 5, previousDown: true));
        Assert.Equal(50, track.Value, 3);

        bridge.Dispatch(root, PointerFrame(100, 5, currentDown: true));
        bridge.Dispatch(root, PointerFrame(100, 5, 100, 5, previousDown: true));
        Assert.Equal(40, track.Value, 3);
    }

    // The repeat buttons draw nothing in any state: a held press on the track, which presses the decrease button, adds
    // no drawing to the track's render commands.
    [Fact]
    public void PressedRepeatButtonDrawsNothing()
    {
        UIRoot root = new(200, 100);
        Track track = new() { Minimum = 0, Maximum = 100, Value = 50, LargeChange = 0, Width = 110, Height = 20 };
        root.VisualChildren.Add(track);
        root.ProcessFrame();
        int commandsBefore = root.RetainedRenderer.Commit(root).Count();
        ElementInputBridge bridge = new();
        LayoutRect bounds = track.ArrangedBounds;

        bridge.Dispatch(root, PointerFrame(bounds.X + 20, bounds.Y + 5, currentDown: true));
        root.ProcessFrame();
        int commandsWhilePressed = root.RetainedRenderer.Commit(root).Count();

        Assert.True(track.DecreaseRepeatButton!.IsPressed);
        Assert.Equal(commandsBefore, commandsWhilePressed);
        Assert.Equal(4, commandsBefore);
    }

    [Theory]
    [InlineData(Orientation.Horizontal)]
    [InlineData(Orientation.Vertical)]
    public void ThumbDragUnderScaledAncestorKeepsThumbUnderPointer(Orientation orientation)
    {
        bool horizontal = orientation == Orientation.Horizontal;
        Track track = CreateTrack(orientation, value: 0);
        UiHost host = CreateHost(track, Matrix3x2.CreateScale(2, 2));

        // The thumb is arranged at 0..10 along the track and drawn at 0..20.
        // Press its drawn center and move the pointer 60 root pixels along the track.
        (float pressX, float pressY) = horizontal ? (10f, 20f) : (20f, 10f);
        (float moveX, float moveY) = horizontal ? (70f, 20f) : (20f, 70f);
        Update(host, PointerFrame(pressX, pressY, currentDown: true));
        Update(host, PointerFrame(pressX, pressY, moveX, moveY, previousDown: true, currentDown: true));
        Update(host, PointerFrame(moveX, moveY, moveX, moveY, previousDown: true));

        Assert.Equal(30, track.Value, 3);
        LayoutRect thumb = track.Thumb.ArrangedBounds;
        Assert.Equal(30, horizontal ? thumb.X : thumb.Y, 3);
    }

    [Theory]
    [InlineData(Orientation.Horizontal)]
    [InlineData(Orientation.Vertical)]
    public void PressInsideThumbDrawnByScaledAncestorDoesNotPage(Orientation orientation)
    {
        Track track = CreateTrack(orientation, value: 0);
        UiHost host = CreateHost(track, Matrix3x2.CreateScale(2, 2));

        // Root x (or y) 18 lies inside the drawn thumb 0..20, but outside its layout bounds 0..10.
        (float x, float y) = orientation == Orientation.Horizontal ? (18f, 20f) : (20f, 18f);
        Update(host, PointerFrame(x, y, currentDown: true));
        Update(host, PointerFrame(x, y, x, y, previousDown: true));

        Assert.Equal(0, track.Value);
    }

    [Theory]
    [InlineData(Orientation.Horizontal)]
    [InlineData(Orientation.Vertical)]
    public void PressOnTrackDrawnByScaledAncestorPagesTowardThePressPoint(Orientation orientation)
    {
        bool horizontal = orientation == Orientation.Horizontal;
        Track track = CreateTrack(orientation, value: 50);
        UiHost host = CreateHost(track, Matrix3x2.CreateScale(2, 2));

        // The thumb is drawn at 100..120 along the track. Root 80 is on the drawn track before it,
        // and root 150 is on the drawn track after it.
        (float beforeX, float beforeY) = horizontal ? (80f, 20f) : (20f, 80f);
        Update(host, PointerFrame(beforeX, beforeY, currentDown: true));
        Update(host, PointerFrame(beforeX, beforeY, beforeX, beforeY, previousDown: true));
        Assert.Equal(40, track.Value);

        (float afterX, float afterY) = horizontal ? (150f, 20f) : (20f, 150f);
        Update(host, PointerFrame(afterX, afterY, currentDown: true));
        Update(host, PointerFrame(afterX, afterY, afterX, afterY, previousDown: true));
        Assert.Equal(50, track.Value);
    }

    [Fact]
    public void ThumbDragAndPressUnderRotatedAncestorFollowTheDrawnTrack()
    {
        Track track = CreateTrack(Orientation.Horizontal, value: 0);

        // A 90 degree rotation draws layout point (x, y) at root (40 - y, x):
        // the horizontal track is drawn top to bottom, and its thumb at root y 0..10.
        UiHost host = CreateHost(
            track,
            Matrix3x2.Multiply(Matrix3x2.CreateRotation(MathF.PI / 2), Matrix3x2.CreateTranslation(40, 0)));

        Update(host, PointerFrame(30, 5, currentDown: true));
        Assert.Equal(0, track.Value);
        Update(host, PointerFrame(30, 5, 30, 65, previousDown: true, currentDown: true));
        Update(host, PointerFrame(30, 65, 30, 65, previousDown: true));
        Assert.Equal(60, track.Value, 3);

        Update(host, PointerFrame(30, 20, currentDown: true));
        Update(host, PointerFrame(30, 20, 30, 20, previousDown: true));
        Assert.Equal(50, track.Value, 3);
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

    // #986: Maximum - Minimum overflows float for large finite endpoints, so the ratio, the point and the drag
    // computations must not go through that float difference.
    [Theory]
    [InlineData(1f)]
    [InlineData(3e38f)]
    public void ValueRatioAndValueFromPointStayFiniteForLargeFiniteEndpoints(float bound)
    {
        Track track = new();
        track.Minimum = -bound;
        track.Maximum = bound;
        track.Value = 0;
        track.Measure(new MeasureContext(new LayoutSize(110, 20)));
        track.Arrange(new ArrangeContext(new LayoutRect(0, 0, 110, 20)));

        float ratioAtZero = track.ValueRatio;
        LayoutRect thumbAtZero = track.Thumb.ArrangedBounds;
        float fromPoint = track.ValueFromPoint(55, 10);
        track.Value = bound;
        float ratioAtMaximum = track.ValueRatio;

        Assert.True(
            ratioAtZero == 0.5f && ratioAtMaximum == 1f && float.IsFinite(fromPoint) && MathF.Abs(fromPoint) <= bound / 100,
            $"bound={bound}: ValueRatio(0)={ratioAtZero} (expected 0.5), ValueRatio(Maximum)={ratioAtMaximum} (expected 1), " +
            $"ValueFromPoint(middle)={fromPoint} (expected about 0).");
        Assert.Equal(new LayoutRect(50, 0, 10, 20), thumbAtZero);
    }

    [Theory]
    [InlineData(1f)]
    [InlineData(3e38f)]
    public void ThumbDragAcrossHalfTheTravelReachesMiddleForLargeFiniteEndpoints(float bound)
    {
        Track track = new();
        track.Minimum = -bound;
        track.Maximum = bound;
        track.Value = -bound;
        track.Measure(new MeasureContext(new LayoutSize(110, 20)));
        track.Arrange(new ArrangeContext(new LayoutRect(0, 0, 110, 20)));

        track.Thumb.RaiseEvent(new DragDeltaEventArgs(Thumb.DragDeltaEvent, track.Thumb, 50, 0, 50, 0));

        Assert.True(
            MathF.Abs(track.Value) <= bound / 100,
            $"bound={bound}: Value after dragging the thumb across half the travel = {track.Value} (expected about 0).");
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

    // #888: ClearValue reveals the default 0, which lies below Minimum; the effective value stays in the range, as
    // RangeBase.Value does, and the clear leaves no Local value, as WPF's ClearValue.
    [Fact]
    public void ClearingValueKeepsEffectiveValueWithinRange()
    {
        Track track = new();
        track.Minimum = 10;
        track.Maximum = 20;
        track.Value = 15;

        track.ClearValue(Track.ValueProperty);

        Assert.Equal(10, track.Value);
        Assert.Equal(UiPropertyValueSource.Default, track.GetValueSource(Track.ValueProperty));
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

    // The vertical track is reversed, as a vertical ScrollBar's track, so Minimum is at the top and both orientations
    // place the same value at the same distance from the track's start.
    private static Track CreateTrack(Orientation orientation, float value)
    {
        bool horizontal = orientation == Orientation.Horizontal;
        return new Track
        {
            Orientation = orientation,
            IsDirectionReversed = !horizontal,
            Minimum = 0,
            Maximum = 100,
            Value = value,
            LargeChange = 10,
            Width = horizontal ? 110 : 20,
            Height = horizontal ? 20 : 110
        };
    }

    private static UiHost CreateHost(Track track, Matrix3x2 ancestorTransform)
    {
        StackPanel panel = new()
        {
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
            RenderTransform = new Transform(ancestorTransform),
            RenderTransformOrigin = new LayoutPoint(0, 0)
        };
        panel.VisualChildren.Add(track);
        UIRoot root = new(400, 400);
        root.VisualChildren.Add(panel);
        UiHost host = new(new UiHostOptions { Root = root, Viewport = new UiViewport(400, 400) });
        Update(host, PointerFrame(0, 0));
        return host;
    }

    private static void Update(UiHost host, InputFrame frame)
    {
        host.Update(frame, host.Viewport, TimeSpan.Zero);
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
