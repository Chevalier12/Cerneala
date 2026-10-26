using System.Globalization;
using System.Reflection;
using Cerneala.Drawing;
using Cerneala.Tests.Controls.Primitives;
using Cerneala.UI.Controls;
using Cerneala.UI.Controls.Primitives;
using Cerneala.UI.Controls.Templates;
using Cerneala.UI.Elements;
using Cerneala.UI.Input;
using Cerneala.UI.Layout;
using Cerneala.UI.Layout.Panels;
using Cerneala.UI.Markup;
using Cerneala.UI.Media;

namespace Cerneala.Tests.Controls;

public sealed class SliderTests
{
    [Fact]
    public void SliderDeclaresRequiredTrackTemplatePart()
    {
        TemplatePartAttribute part = Assert.Single(
            typeof(Slider).GetCustomAttributes(typeof(TemplatePartAttribute), inherit: true)
                .Cast<TemplatePartAttribute>(),
            candidate => candidate.Name == "PART_Track");

        Assert.Equal(typeof(Track), part.Type);
    }

    [Fact]
    public void SliderUsesTrackProvidedByComponentTemplate()
    {
        Track templateTrack = new();
        ComponentTemplate<Slider> template = new("Slider.Custom", context =>
        {
            context.RequirePart("PART_Track", templateTrack);
            return templateTrack;
        });
        Slider slider = new()
        {
            ComponentTemplate = template,
            Minimum = 0,
            Maximum = 100,
            Value = 25
        };

        slider.Measure(new MeasureContext(new LayoutSize(110, 20)));
        slider.Arrange(new ArrangeContext(new LayoutRect(0, 0, 110, 20)));
        templateTrack.Value = 75;

        Assert.Same(templateTrack, slider.Track);
        Assert.Equal(75, slider.Value);
    }

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

    // #986: the drag change of a Slider whose Maximum - Minimum overflows float must still be finite and proportional.
    [Theory]
    [InlineData(1f)]
    [InlineData(3e38f)]
    public void SliderThumbDragReachesMiddleForLargeFiniteEndpoints(float bound)
    {
        UIRoot root = new(200, 100);
        Slider slider = new()
        {
            Minimum = -bound,
            Maximum = bound,
            Value = -bound
        };
        slider.Measure(new MeasureContext(new LayoutSize(110, 20)));
        slider.Arrange(new ArrangeContext(new LayoutRect(0, 0, 110, 20)));
        root.VisualChildren.Add(slider);
        ElementInputBridge bridge = new();

        bridge.Dispatch(root, PointerFrame(5, 5, currentDown: true));
        bridge.Dispatch(root, PointerFrame(5, 5, 55, 5, previousDown: true, currentDown: true));

        Assert.True(
            MathF.Abs(slider.Value) <= bound / 100,
            $"bound={bound}: Slider.Value after dragging the thumb across half the travel = {slider.Value} (expected about 0).");
    }

    // As WPF's Slider with IsMoveToPointEnabled true: a press on the track moves the value to the pressed point.
    [Fact]
    public void ClickingSliderTrackMovesValueToPointer()
    {
        UIRoot root = new(200, 100);
        Slider slider = new()
        {
            Minimum = 0,
            Maximum = 1,
            Value = 1,
            IsMoveToPointEnabled = true
        };
        slider.Measure(new MeasureContext(new LayoutSize(110, 20)));
        slider.Arrange(new ArrangeContext(new LayoutRect(0, 0, 110, 20)));
        root.VisualChildren.Add(slider);
        ElementInputBridge bridge = new();

        bridge.Dispatch(root, PointerFrame(30, 5, currentDown: true));

        Assert.Equal(0.25f, slider.Value);
    }

    // As a WPF Slider focuses itself on a MouseDown that its Thumb handled (#662): a press on the thumb of a focusable
    // slider takes keyboard focus from the button that had it. A slider that is not focusable leaves the focus there.
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void PressOnThumbOfFocusableSliderFocusesSlider(bool sliderFocusable)
    {
        Button other = new() { Content = "other", Width = 100, Height = 30 };
        Slider slider = new() { Focusable = sliderFocusable, Minimum = 0, Maximum = 100, Value = 50, Width = 200, Height = 20 };
        Cerneala.UI.Controls.StackPanel panel = new();
        panel.Children.Add(other);
        panel.Children.Add(slider);
        UIRoot root = new(360, 240);
        root.VisualChildren.Add(new Window { Content = panel, Width = 360, Height = 240 });
        root.ProcessFrame();
        ElementInputBridge bridge = new();
        LayoutRect otherBounds = other.ArrangedBounds;
        bridge.Dispatch(root, PointerFrame(otherBounds.X + 5, otherBounds.Y + 5, currentDown: true));
        bridge.Dispatch(root, PointerFrame(otherBounds.X + 5, otherBounds.Y + 5, otherBounds.X + 5, otherBounds.Y + 5, previousDown: true));
        root.ProcessFrame();
        Assert.True(other.IsKeyboardFocused);
        Thumb thumb = slider.Track.Thumb;
        LayoutRect bounds = thumb.ArrangedBounds;
        float x = bounds.X + bounds.Width / 2;
        float y = bounds.Y + bounds.Height / 2;
        Assert.Same(thumb, new HitTestService().HitTest(root, x, y)?.Element);

        bridge.Dispatch(root, PointerFrame(x, y, currentDown: true));
        root.ProcessFrame();
        bool focusedWhileHeld = slider.IsKeyboardFocused;
        bridge.Dispatch(root, PointerFrame(x, y, x, y, previousDown: true));
        root.ProcessFrame();

        Assert.Equal(sliderFocusable, focusedWhileHeld);
        Assert.Equal(sliderFocusable, slider.IsKeyboardFocused);
        Assert.Equal(!sliderFocusable, other.IsKeyboardFocused);
        Assert.False(thumb.Focusable);
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

    // As WPF's Slider, which is a tab stop: Tab from a button moves the focus to the slider, and Right then raises the
    // value by SmallChange, from 5 to 6.
    [Fact]
    public void TabFromButtonReachesSliderAndRightRaisesValueBySmallChange()
    {
        Button other = new() { Content = "other", Width = 100, Height = 30 };
        Slider slider = new() { Minimum = 0, Maximum = 10, SmallChange = 1, Value = 5, Width = 200, Height = 20 };
        (UIRoot root, ElementInputBridge bridge) = CreateWindowRoot(other, slider);
        Click(root, bridge, other);
        Assert.True(other.IsKeyboardFocused);

        PressKey(root, bridge, InputKey.Tab);
        Assert.True(slider.IsKeyboardFocused);

        PressKey(root, bridge, InputKey.Right);
        Assert.Equal(6, slider.Value);
    }

    // As WPF's Slider commands: Up and Right raise the value by SmallChange, Down and Left lower it, PageUp and PageDown
    // change it by LargeChange, Home sets Minimum and End sets Maximum. Each key that acts is handled.
    [Fact]
    public void KeysChangeValueBySmallChangeLargeChangeAndRangeEnds()
    {
        Button other = new() { Content = "other", Width = 100, Height = 30 };
        Slider slider = new() { Minimum = 0, Maximum = 10, SmallChange = 1, LargeChange = 3, Value = 5, Width = 200, Height = 20 };
        (UIRoot root, ElementInputBridge bridge) = CreateWindowRoot(other, slider);
        Click(root, bridge, other);
        PressKey(root, bridge, InputKey.Tab);
        Assert.True(slider.IsKeyboardFocused);
        List<bool> handled = [];
        slider.AddHandler(InputEvents.KeyDownEvent, (_, args) => handled.Add(args.Handled), handledEventsToo: true);

        PressKey(root, bridge, InputKey.Up);
        Assert.Equal(6, slider.Value);
        PressKey(root, bridge, InputKey.Down);
        Assert.Equal(5, slider.Value);
        PressKey(root, bridge, InputKey.Left);
        Assert.Equal(4, slider.Value);
        PressKey(root, bridge, InputKey.PageUp);
        Assert.Equal(7, slider.Value);
        PressKey(root, bridge, InputKey.PageDown);
        Assert.Equal(4, slider.Value);
        PressKey(root, bridge, InputKey.Home);
        Assert.Equal(0, slider.Value);
        PressKey(root, bridge, InputKey.End);
        Assert.Equal(10, slider.Value);

        Assert.Equal(7, handled.Count);
        Assert.All(handled, Assert.True);
        Assert.True(slider.IsKeyboardFocused);
    }

    // As WPF's vertical Slider, whose values grow upward: Minimum puts the thumb at the bottom and Maximum at the top.
    [Fact]
    public void VerticalSliderPutsMinimumAtBottom()
    {
        // The frame arranges the track again when the value changes; a second manual Arrange of the slider with the same
        // rect would stop at the template's layout panel, whose arrange is still valid.
        UIRoot root = new(200, 200);
        Slider slider = new()
        {
            Orientation = Orientation.Vertical,
            Minimum = 0,
            Maximum = 100,
            Value = 0,
            Width = 20,
            Height = 110,
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top
        };
        root.VisualChildren.Add(slider);
        root.ProcessFrame();
        LayoutRect origin = slider.ArrangedBounds;
        Assert.Equal(new LayoutRect(origin.X, origin.Y + 100, 20, 10), slider.Track.Thumb.ArrangedBounds);

        slider.Value = 100;
        root.ProcessFrame();
        Assert.Equal(new LayoutRect(origin.X, origin.Y, 20, 10), slider.Track.Thumb.ArrangedBounds);
    }

    // As WPF's Slider with IsMoveToPointEnabled false, the default: a press on the track beside the thumb moves the value
    // by LargeChange toward the press, not to the pressed point.
    [Fact]
    public void PressBesideThumbMovesByLargeChangeByDefault()
    {
        Slider slider = CreateArrangedSlider(maximum: 10, value: 5, largeChange: 3, out UIRoot root);
        ElementInputBridge bridge = new();

        // The thumb is at x 50..60; x 20 is before it and x 90 after it.
        bridge.Dispatch(root, PointerFrame(20, 5, currentDown: true));
        bridge.Dispatch(root, PointerFrame(20, 5, 20, 5, previousDown: true));
        Assert.Equal(2, slider.Value);

        bridge.Dispatch(root, PointerFrame(90, 5, currentDown: true));
        bridge.Dispatch(root, PointerFrame(90, 5, 90, 5, previousDown: true));
        Assert.Equal(5, slider.Value);
    }

    // As WPF's Slider track, whose sides are RepeatButtons: holding a press beside the thumb longer than Delay moves the
    // value again by LargeChange, and a press released before Delay moves it once.
    [Fact]
    public void HoldingPressOnTrackRepeatsLargeChangeAfterDelay()
    {
        Slider slider = CreateArrangedSlider(maximum: 100, value: 50, largeChange: 10, out UIRoot root);
        ElementInputBridge bridge = new();

        bridge.Dispatch(root, PointerFrame(20, 5, currentDown: true), TimeSpan.Zero);
        Assert.Equal(40, slider.Value);
        bridge.Dispatch(root, PointerFrame(20, 5, 20, 5, previousDown: true, currentDown: true), TimeSpan.FromMilliseconds(500));
        Assert.Equal(30, slider.Value);
        bridge.Dispatch(root, PointerFrame(20, 5, 20, 5, previousDown: true, currentDown: true), TimeSpan.FromMilliseconds(100));
        Assert.Equal(20, slider.Value);
        bridge.Dispatch(root, PointerFrame(20, 5, 20, 5, previousDown: true), TimeSpan.Zero);
        Assert.Equal(20, slider.Value);
    }

    [Fact]
    public void PressShorterThanDelayOnTrackChangesValueOnce()
    {
        Slider slider = CreateArrangedSlider(maximum: 100, value: 50, largeChange: 10, out UIRoot root);
        ElementInputBridge bridge = new();

        bridge.Dispatch(root, PointerFrame(20, 5, currentDown: true), TimeSpan.Zero);
        bridge.Dispatch(root, PointerFrame(20, 5, 20, 5, previousDown: true, currentDown: true), TimeSpan.FromMilliseconds(499));
        bridge.Dispatch(root, PointerFrame(20, 5, 20, 5, previousDown: true), TimeSpan.FromMilliseconds(1));
        bridge.Dispatch(root, PointerFrame(20, 5), TimeSpan.FromSeconds(1));

        Assert.Equal(40, slider.Value);
    }

    [Fact]
    public void DefaultsMatchWpfSlider()
    {
        Slider slider = new();

        Assert.True(slider.Focusable);
        Assert.True(slider.IsTabStop);
        Assert.False(slider.IsDirectionReversed);
        Assert.False(slider.IsMoveToPointEnabled);
        Assert.Equal(500, slider.Delay);
        Assert.Equal(100, slider.Interval);
        Assert.Equal(1, slider.TickFrequency);
        Assert.Null(slider.Ticks);
        Assert.False(slider.IsSnapToTickEnabled);
        Assert.Equal(TickPlacement.None, slider.TickPlacement);
        Assert.Equal(
            MarkupValueConstraint.NonNegative,
            typeof(Slider).GetProperty(nameof(Slider.Delay))!.GetCustomAttribute<MarkupValueConstraintAttribute>()?.Constraint);
        Assert.Equal(
            MarkupValueConstraint.Positive,
            typeof(Slider).GetProperty(nameof(Slider.Interval))!.GetCustomAttribute<MarkupValueConstraintAttribute>()?.Constraint);
        Assert.Equal(
            MarkupValueConstraint.NonNegative,
            typeof(Slider).GetProperty(nameof(Slider.TickFrequency))!.GetCustomAttribute<MarkupValueConstraintAttribute>()?.Constraint);
    }

    [Fact]
    public void InvalidDelayIntervalTickFrequencyAndTicksAreRejected()
    {
        Slider slider = new();

        Assert.Throws<ArgumentException>(() => slider.Delay = -1);
        Assert.Throws<ArgumentException>(() => slider.Interval = 0);
        Assert.Throws<ArgumentException>(() => slider.TickFrequency = -1);
        Assert.Throws<ArgumentException>(() => slider.TickFrequency = float.NaN);
        Assert.Throws<ArgumentException>(() => slider.Ticks = [0, float.PositiveInfinity]);
        Assert.Equal(500, slider.Delay);
        Assert.Equal(100, slider.Interval);
        Assert.Equal(1, slider.TickFrequency);
        Assert.Null(slider.Ticks);
    }

    // The slider gives its Delay, Interval and IsDirectionReversed to its track and the track's repeat buttons. The
    // slider itself handles IsMoveToPointEnabled, as WPF's Slider does.
    [Fact]
    public void SliderSettingsReachTrackAndItsRepeatButtons()
    {
        Slider slider = new() { Delay = 200, Interval = 50, IsDirectionReversed = true };

        Track track = slider.Track;

        Assert.Equal(200, track.DecreaseRepeatButton!.Delay);
        Assert.Equal(50, track.DecreaseRepeatButton.Interval);
        Assert.Equal(200, track.IncreaseRepeatButton!.Delay);
        Assert.Equal(50, track.IncreaseRepeatButton.Interval);
        Assert.True(track.IsDirectionReversed);

        slider.Delay = 300;

        Assert.Equal(300, track.IncreaseRepeatButton.Delay);
    }

    // As WPF's Slider.MoveToNextTick with snapping: Right from 4 with SmallChange 1 and TickFrequency 2 goes to the next
    // tick, 6, and Left goes back to 4.
    [Fact]
    public void ArrowKeysWithSnapToTickMoveToNextTick()
    {
        Button other = new() { Content = "other", Width = 100, Height = 30 };
        Slider slider = new()
        {
            Minimum = 0,
            Maximum = 10,
            SmallChange = 1,
            TickFrequency = 2,
            IsSnapToTickEnabled = true,
            Value = 4,
            Width = 200,
            Height = 20
        };
        (UIRoot root, ElementInputBridge bridge) = CreateWindowRoot(other, slider);
        Click(root, bridge, other);
        PressKey(root, bridge, InputKey.Tab);

        PressKey(root, bridge, InputKey.Right);
        Assert.Equal(6, slider.Value);
        PressKey(root, bridge, InputKey.Left);
        Assert.Equal(4, slider.Value);
    }

    // With Ticks, the ticks replace TickFrequency: Right from 3 goes to the next tick, 7, and End then Left go from 10
    // to 7.
    [Fact]
    public void ArrowKeysWithSnapToTicksCollectionMoveToNextTick()
    {
        Button other = new() { Content = "other", Width = 100, Height = 30 };
        Slider slider = new()
        {
            Minimum = 0,
            Maximum = 10,
            SmallChange = 1,
            Ticks = [10, 3, 0, 7],
            IsSnapToTickEnabled = true,
            Value = 3,
            Width = 200,
            Height = 20
        };
        (UIRoot root, ElementInputBridge bridge) = CreateWindowRoot(other, slider);
        Click(root, bridge, other);
        PressKey(root, bridge, InputKey.Tab);

        PressKey(root, bridge, InputKey.Right);
        Assert.Equal(7, slider.Value);
        PressKey(root, bridge, InputKey.End);
        Assert.Equal(10, slider.Value);
        PressKey(root, bridge, InputKey.Left);
        Assert.Equal(7, slider.Value);
    }

    // As WPF's Slider.SnapToTick during a thumb drag: the pointer's value snaps to the nearest tick. The drag moves in
    // steps of 0.5, each too small to reach a tick alone, so a thumb that snapping keeps in place still follows the
    // pointer: dragging to the point for 4.7 gives 4, and to the point for 5.2 gives 6.
    [Fact]
    public void ThumbDragWithSnapToTickSnapsToNearestTick()
    {
        UIRoot root = new(200, 100);
        Slider slider = new()
        {
            Minimum = 0,
            Maximum = 10,
            TickFrequency = 2,
            IsSnapToTickEnabled = true,
            Value = 0
        };
        slider.Measure(new MeasureContext(new LayoutSize(110, 20)));
        slider.Arrange(new ArrangeContext(new LayoutRect(0, 0, 110, 20)));
        root.VisualChildren.Add(slider);
        ElementInputBridge bridge = new();

        // The thumb is at x 0..10 and the travel is 100 pixels, so one pixel is 0.1.
        bridge.Dispatch(root, PointerFrame(5, 5, currentDown: true));
        float x = 5;
        List<float> values = [];
        foreach (float next in new float[] { 10, 15, 20, 25, 30, 35, 40, 45, 50, 52 })
        {
            bridge.Dispatch(root, PointerFrame(x, 5, next, 5, previousDown: true, currentDown: true));
            x = next;
            values.Add(slider.Value);
        }

        Assert.Equal(4, slider.Value);
        Assert.Equal([0, 2, 2, 2, 2, 4, 4, 4, 4, 4], values);
        Assert.Equal(4, slider.Track.Value);

        bridge.Dispatch(root, PointerFrame(52, 5, 57, 5, previousDown: true, currentDown: true));
        bridge.Dispatch(root, PointerFrame(57, 5, 57, 5, previousDown: true));
        Assert.Equal(6, slider.Value);
    }

    // As WPF's Slider with IsMoveToPointEnabled: the press moves the value to the pressed point once, snapped to the
    // nearest tick, and holding it longer than Delay does not page.
    [Fact]
    public void MoveToPointPressSnapsAndDoesNotRepeatWhileHeld()
    {
        UIRoot root = new(200, 100);
        Slider slider = new()
        {
            Minimum = 0,
            Maximum = 10,
            LargeChange = 1,
            TickFrequency = 2,
            IsSnapToTickEnabled = true,
            IsMoveToPointEnabled = true,
            Value = 10
        };
        slider.Measure(new MeasureContext(new LayoutSize(110, 20)));
        slider.Arrange(new ArrangeContext(new LayoutRect(0, 0, 110, 20)));
        root.VisualChildren.Add(slider);
        ElementInputBridge bridge = new();

        // x 52 is the point for 4.7.
        bridge.Dispatch(root, PointerFrame(52, 5, currentDown: true), TimeSpan.Zero);
        Assert.Equal(4, slider.Value);
        bridge.Dispatch(root, PointerFrame(52, 5, 52, 5, previousDown: true, currentDown: true), TimeSpan.FromSeconds(2));
        bridge.Dispatch(root, PointerFrame(52, 5, 52, 5, previousDown: true, currentDown: true), TimeSpan.FromSeconds(2));
        bridge.Dispatch(root, PointerFrame(52, 5, 52, 5, previousDown: true), TimeSpan.Zero);

        Assert.Equal(4, slider.Value);
        Assert.False(slider.Track.DecreaseRepeatButton!.IsPressed);
        Assert.False(bridge.HasActivePointerRepeat);
    }

    // As WPF's Slider.MoveToNextTick for the track's repeat buttons: with snapping, a press beside the thumb moves to the
    // next tick by LargeChange: from 4 with LargeChange 3 and TickFrequency 2, a press before the thumb goes to 2.
    [Fact]
    public void TrackPressWithSnapToTickMovesToNextTick()
    {
        Button other = new() { Content = "other", Width = 100, Height = 30 };
        Slider slider = new()
        {
            Minimum = 0,
            Maximum = 10,
            LargeChange = 3,
            TickFrequency = 2,
            IsSnapToTickEnabled = true,
            Value = 4,
            Width = 110,
            Height = 20
        };
        (UIRoot root, ElementInputBridge bridge) = CreateWindowRoot(other, slider);
        Click(root, bridge, other);
        PressKey(root, bridge, InputKey.Tab);
        Assert.True(slider.IsKeyboardFocused);
        LayoutRect track = slider.Track.ArrangedBounds;
        float x = track.X + 2;
        float y = track.Y + (track.Height / 2);
        Assert.Same(slider.Track.DecreaseRepeatButton, new HitTestService().HitTest(root, x, y)?.Element);

        bridge.Dispatch(root, PointerFrame(x, y, currentDown: true));
        bridge.Dispatch(root, PointerFrame(x, y, x, y, previousDown: true));

        Assert.Equal(2, slider.Value);
        Assert.Equal(2, slider.Track.Value);
    }

    // As WPF's Slider with IsDirectionReversed: Minimum is at the right of a horizontal slider, Right lowers the value and
    // Left raises it.
    [Fact]
    public void ReversedSliderPutsMinimumAtRightAndRightLowersValue()
    {
        Button other = new() { Content = "other", Width = 100, Height = 30 };
        Slider slider = new()
        {
            Minimum = 0,
            Maximum = 10,
            SmallChange = 1,
            IsDirectionReversed = true,
            Value = 5,
            Width = 110,
            Height = 20
        };
        (UIRoot root, ElementInputBridge bridge) = CreateWindowRoot(other, slider);
        Click(root, bridge, other);
        PressKey(root, bridge, InputKey.Tab);

        PressKey(root, bridge, InputKey.Right);
        Assert.Equal(4, slider.Value);
        PressKey(root, bridge, InputKey.Left);
        PressKey(root, bridge, InputKey.Left);
        Assert.Equal(6, slider.Value);
        PressKey(root, bridge, InputKey.Up);
        Assert.Equal(5, slider.Value);

        PressKey(root, bridge, InputKey.Home);
        Assert.Equal(0, slider.Value);
        LayoutRect track = slider.Track.ArrangedBounds;
        LayoutRect thumb = slider.Track.Thumb.ArrangedBounds;
        Assert.Equal(track.X + track.Width, thumb.X + thumb.Width);
    }

    // As WPF's Slider template: TickPlacement shows a TickBar above the track (TopLeft), below it (BottomRight), on both
    // sides (Both) or none (None). For 0..10 with TickFrequency 2 on 110 pixels, each TickBar draws 6 ticks.
    [Theory]
    [InlineData(TickPlacement.None, 0)]
    [InlineData(TickPlacement.TopLeft, 6)]
    [InlineData(TickPlacement.BottomRight, 6)]
    [InlineData(TickPlacement.Both, 12)]
    public void TickPlacementDrawsTickBarsOnItsSides(TickPlacement placement, int expectedLines)
    {
        Slider slider = CreateTickSlider(Orientation.Horizontal, placement);

        IReadOnlyList<(float X1, float Y1, float X2, float Y2)> lines = TickBarTests.RenderLines(slider);

        Assert.Equal(expectedLines, lines.Count);
    }

    // The bottom TickBar sits 2 pixels below the track and is 4 pixels high, and its ticks line up with the thumb's
    // center: Minimum at x 5, where the thumb's center is at value 0, and Maximum at x 105.
    [Fact]
    public void BottomTicksLineUpWithThumbCenterBelowTheTrack()
    {
        Slider slider = CreateTickSlider(Orientation.Horizontal, TickPlacement.BottomRight);

        IReadOnlyList<(float X1, float Y1, float X2, float Y2)> lines = TickBarTests.RenderLines(slider);

        LayoutRect track = slider.Track.ArrangedBounds;
        LayoutRect thumb = slider.Track.Thumb.ArrangedBounds;
        float top = track.Y + track.Height + 2 - slider.ArrangedBounds.Y;
        Assert.Equal(5, thumb.X + (thumb.Width / 2) - slider.ArrangedBounds.X);
        Assert.Equal(
            [(5, top, 5, top + 4), (105, top, 105, top + 4), (25, top, 25, top + 3), (45, top, 45, top + 3), (65, top, 65, top + 3), (85, top, 85, top + 3)],
            lines);
    }

    // Each shown TickBar adds its 4 pixels and the 2-pixel gap to the slider's height: Both makes it 12 pixels taller
    // than None, and a collapsed TickBar takes no space.
    [Fact]
    public void TickPlacementBothMakesSliderTallerThanNone()
    {
        Slider none = new() { Minimum = 0, Maximum = 10 };
        Slider both = new() { Minimum = 0, Maximum = 10, TickPlacement = TickPlacement.Both };

        none.Measure(new MeasureContext(new LayoutSize(110, float.PositiveInfinity)));
        both.Measure(new MeasureContext(new LayoutSize(110, float.PositiveInfinity)));

        Assert.Equal(none.DesiredSize.Height + 12, both.DesiredSize.Height);
        Assert.Equal(none.Track.DesiredSize.Height, none.DesiredSize.Height);
    }

    // A vertical slider shows TopLeft ticks at its left: Minimum at the bottom, drawn from x 4 leftward to x 0.
    [Fact]
    public void VerticalSliderShowsTopLeftTicksAtItsLeft()
    {
        Slider slider = CreateTickSlider(Orientation.Vertical, TickPlacement.TopLeft);

        IReadOnlyList<(float X1, float Y1, float X2, float Y2)> lines = TickBarTests.RenderLines(slider);

        Assert.Equal(6, lines.Count);
        Assert.Equal((4, 105, 0, 105), lines[0]);
        Assert.Equal((4, 5, 0, 5), lines[1]);
        Assert.True(slider.Track.ArrangedBounds.X - slider.ArrangedBounds.X >= 6);
    }

    // The slider's Ticks and IsDirectionReversed reach its TickBars: a tick at 3 on a reversed slider is drawn at x 75.
    [Fact]
    public void SliderTicksAndDirectionReachTickBars()
    {
        Slider slider = CreateTickSlider(Orientation.Horizontal, TickPlacement.BottomRight);
        slider.Ticks = [3];
        slider.IsDirectionReversed = true;

        IReadOnlyList<(float X1, float Y1, float X2, float Y2)> lines = TickBarTests.RenderLines(slider);

        Assert.Equal([105, 5, 75], lines.Select(line => line.X1));
    }

    // A custom template with only a Track keeps working with TickPlacement: there is no TickBar to show.
    [Fact]
    public void CustomTemplateWithOnlyTrackAcceptsTickPlacement()
    {
        Track templateTrack = new();
        Slider slider = new()
        {
            ComponentTemplate = new ComponentTemplate<Slider>("Slider.TrackOnly", context =>
            {
                context.RequirePart("PART_Track", templateTrack);
                return templateTrack;
            }),
            Minimum = 0,
            Maximum = 10,
            TickPlacement = TickPlacement.Both
        };

        IReadOnlyList<(float X1, float Y1, float X2, float Y2)> lines = TickBarTests.RenderLines(slider);

        Assert.Same(templateTrack, slider.Track);
        Assert.Empty(lines);
    }

    // As WPF's Slider: PART_SelectionRange is an optional template part.
    [Fact]
    public void SliderDeclaresOptionalSelectionRangeTemplatePart()
    {
        TemplatePartAttribute part = Assert.Single(
            typeof(Slider).GetCustomAttributes(typeof(TemplatePartAttribute), inherit: true)
                .Cast<TemplatePartAttribute>(),
            candidate => candidate.Name == "PART_SelectionRange");

        Assert.Equal(typeof(UIElement), part.Type);
    }

    [Fact]
    public void SelectionAndAutoToolTipDefaultsMatchWpfSlider()
    {
        Slider slider = new();

        Assert.Equal(0, slider.SelectionStart);
        Assert.Equal(0, slider.SelectionEnd);
        Assert.False(slider.IsSelectionRangeEnabled);
        Assert.Equal(AutoToolTipPlacement.None, slider.AutoToolTipPlacement);
        Assert.Equal(0, slider.AutoToolTipPrecision);
        Assert.Equal(
            MarkupValueConstraint.NonNegative,
            typeof(Slider).GetProperty(nameof(Slider.AutoToolTipPrecision))!.GetCustomAttribute<MarkupValueConstraintAttribute>()?.Constraint);
        Assert.Throws<ArgumentException>(() => slider.AutoToolTipPrecision = -1);

        // NumberFormatInfo.NumberDecimalDigits takes 0..99, so 100 is rejected when it is set, not when a drag starts.
        Assert.Throws<ArgumentException>(() => slider.AutoToolTipPrecision = 100);
        slider.AutoToolTipPrecision = 99;
        slider.AutoToolTipPrecision = 0;
        Assert.Throws<ArgumentException>(() => slider.SelectionStart = float.NaN);
        Assert.Throws<ArgumentException>(() => slider.SelectionEnd = float.PositiveInfinity);
        Assert.Throws<ArgumentException>(() => slider.AutoToolTipPlacement = (AutoToolTipPlacement)7);
        Assert.Equal(0, slider.AutoToolTipPrecision);
        Assert.Equal(0, slider.SelectionStart);
    }

    // As WPF's Slider.CoerceSelectionStart and CoerceSelectionEnd: SelectionStart stays in Minimum..Maximum, and
    // SelectionEnd in SelectionStart..Maximum; both follow a change of Minimum, Maximum or SelectionStart.
    [Fact]
    public void SelectionStartAndEndAreCoercedAsWpf()
    {
        Slider slider = new() { Minimum = 0, Maximum = 10 };

        slider.SelectionStart = -5;
        Assert.Equal(0, slider.SelectionStart);
        slider.SelectionStart = 12;
        Assert.Equal(10, slider.SelectionStart);

        slider.SelectionStart = 4;
        slider.SelectionEnd = 2;
        Assert.Equal(4, slider.SelectionEnd);
        slider.SelectionEnd = 20;
        Assert.Equal(10, slider.SelectionEnd);

        slider.Maximum = 8;
        Assert.Equal(4, slider.SelectionStart);
        Assert.Equal(8, slider.SelectionEnd);

        slider.Minimum = 6;
        Assert.Equal(6, slider.SelectionStart);
        Assert.Equal(8, slider.SelectionEnd);

        slider.SelectionEnd = 7;
        slider.SelectionStart = 7.5f;
        Assert.Equal(7.5f, slider.SelectionStart);
        Assert.Equal(7.5f, slider.SelectionEnd);
    }

    // The default template draws the track bar itself, behind the Track, with the Track's former colors: a default
    // horizontal slider with the selection range disabled draws exactly what it drew before, the bar's (225, 225, 225)
    // fill and its 1-pixel (120, 120, 120) border over the track, then the thumb.
    [Fact]
    public void DefaultSliderWithoutSelectionRangeDrawsTheSameTrackBarAsBefore()
    {
        Slider slider = CreateSelectionSlider(Orientation.Horizontal);
        slider.SelectionStart = 2;
        slider.SelectionEnd = 6;

        IReadOnlyList<(DrawCommandKind Kind, LayoutRect Rect, Color? Color, float Thickness)> commands = RenderBoxes(slider);

        Assert.Equal(
            [
                (DrawCommandKind.FillRectangle, new LayoutRect(0, 0, 110, 20), new Color(225, 225, 225), 0),
                // The track border is the default positive 1, painted outside the track: a centred pen of thickness 1 on
                // the track inflated by 0.5.
                (DrawCommandKind.DrawRectangle, new LayoutRect(-0.5f, -0.5f, 111, 21), new Color(120, 120, 120), 1),
                (DrawCommandKind.FillRectangle, new LayoutRect(0, 0, 10, 20), new Color(180, 180, 180), 0),
                // The thumb border is painted outside the thumb, as the track border is.
                (DrawCommandKind.DrawRectangle, new LayoutRect(-0.5f, -0.5f, 11, 21), new Color(80, 80, 80), 1)
            ],
            commands);
        UIElement selectionRange = Assert.IsAssignableFrom<UIElement>(slider.ComponentTemplateInstance!.Parts["PART_SelectionRange"]);
        Assert.Equal(Visibility.Collapsed, selectionRange.Visibility);
        Assert.Null(slider.Track.Background);
        Assert.Null(slider.Track.BorderBrush);
    }

    // As WPF's Slider.UpdateSelectionRangeElementPositionAndSize: the range runs along the thumb center's travel. For
    // 0..10 on 110 pixels with a 10-pixel thumb, one unit is 10 pixels, so SelectionStart 2 and SelectionEnd 6 fill
    // x 25..65 over the whole track height, drawn over the track bar and under the thumb. Reversed, it fills x 45..85.
    [Theory]
    [InlineData(false, 25)]
    [InlineData(true, 45)]
    public void SelectionRangeFillsBetweenSelectionStartAndEnd(bool reversed, float expectedX)
    {
        Slider slider = CreateSelectionSlider(Orientation.Horizontal);
        slider.IsDirectionReversed = reversed;
        slider.SelectionStart = 2;
        slider.SelectionEnd = 6;
        slider.IsSelectionRangeEnabled = true;

        IReadOnlyList<(DrawCommandKind Kind, LayoutRect Rect, Color? Color, float Thickness)> commands = RenderBoxes(slider);

        Assert.Equal(5, commands.Count);
        Assert.Equal((DrawCommandKind.FillRectangle, new LayoutRect(expectedX, 0, 40, 20), new Color(0, 120, 215), 0f), commands[2]);
        Assert.Equal(new Color(180, 180, 180), commands[3].Color);
    }

    // A vertical slider has Minimum at the bottom: SelectionStart 2 and SelectionEnd 6 fill y 45..85.
    [Fact]
    public void VerticalSelectionRangeRunsUpFromMinimum()
    {
        Slider slider = CreateSelectionSlider(Orientation.Vertical);
        slider.SelectionStart = 2;
        slider.SelectionEnd = 6;
        slider.IsSelectionRangeEnabled = true;

        IReadOnlyList<(DrawCommandKind Kind, LayoutRect Rect, Color? Color, float Thickness)> commands = RenderBoxes(slider);

        Assert.Equal((DrawCommandKind.FillRectangle, new LayoutRect(0, 45, 20, 40), new Color(0, 120, 215), 0f), commands[2]);
    }

    // The selection range takes no pointer input: a press on it, beside the thumb, still reaches the decrease repeat
    // button and lowers the value by LargeChange, from 8 to 7.
    [Fact]
    public void PressOnSelectionRangeStillPagesThroughRepeatButton()
    {
        Slider slider = CreateSelectionSlider(Orientation.Horizontal);
        slider.Value = 8;
        slider.SelectionStart = 2;
        slider.SelectionEnd = 6;
        slider.IsSelectionRangeEnabled = true;
        UIRoot root = new(200, 200);
        root.VisualChildren.Add(slider);
        root.ProcessFrame();
        LayoutRect track = slider.Track.ArrangedBounds;
        float x = track.X + 40;
        float y = track.Y + 10;
        Assert.Same(slider.Track.DecreaseRepeatButton, new HitTestService().HitTest(root, x, y)?.Element);
        ElementInputBridge bridge = new();

        bridge.Dispatch(root, PointerFrame(x, y, currentDown: true));
        bridge.Dispatch(root, PointerFrame(x, y, x, y, previousDown: true));

        Assert.Equal(7, slider.Value);
    }

    // As WPF's Slider.AutoToolTipPlacement TopLeft: a thumb drag opens a ToolTip centered above the thumb with the value
    // formatted with AutoToolTipPrecision decimals, the text and the place follow the drag, and the release closes it.
    // The track is 110 pixels for 0..10, so a 47-pixel drag gives 4.7 and 10 more pixels give 5.7.
    [Fact]
    public void AutoToolTipShowsValueAboveThumbDuringDragAndClosesOnRelease()
    {
        CultureInfo previousCulture = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        try
        {
            (UIRoot root, Slider slider) = CreateAutoToolTipSlider(Orientation.Horizontal, AutoToolTipPlacement.TopLeft);
            slider.AutoToolTipPrecision = 1;
            ElementInputBridge bridge = new();
            (float x, float y) = Center(slider.Track.Thumb);

            bridge.Dispatch(root, PointerFrame(x, y, currentDown: true));
            bridge.Dispatch(root, PointerFrame(x, y, x + 47, y, previousDown: true, currentDown: true));
            root.ProcessFrame();

            ToolTip toolTip = Assert.IsType<ToolTip>(slider.AutoToolTip);
            Assert.True(toolTip.IsOpen);
            Assert.Equal("4.7", Assert.IsType<TextBlock>(toolTip.Content).Text);
            AssertBeside(slider.Track.Thumb.ArrangedBounds, toolTip.PopupRoot.ArrangedBounds, Orientation.Horizontal, AutoToolTipPlacement.TopLeft);

            bridge.Dispatch(root, PointerFrame(x + 47, y, x + 57, y, previousDown: true, currentDown: true));
            root.ProcessFrame();
            Assert.Equal("5.7", Assert.IsType<TextBlock>(toolTip.Content).Text);
            AssertBeside(slider.Track.Thumb.ArrangedBounds, toolTip.PopupRoot.ArrangedBounds, Orientation.Horizontal, AutoToolTipPlacement.TopLeft);
            Assert.Contains(
                root.RetainedRenderer.Commit(root),
                command => command.Kind == DrawCommandKind.DrawText && command.Text == "5.7");

            bridge.Dispatch(root, PointerFrame(x + 57, y, x + 57, y, previousDown: true));
            root.ProcessFrame();
            Assert.False(toolTip.IsOpen);
            Assert.Null(toolTip.PopupRoot.VisualParent?.VisualParent);
        }
        finally
        {
            CultureInfo.CurrentCulture = previousCulture;
        }
    }

    // TopLeft puts the ToolTip centered above a horizontal thumb and vertically centered left of a vertical one;
    // BottomRight puts it centered below and right.
    [Theory]
    [InlineData(Orientation.Horizontal, AutoToolTipPlacement.TopLeft)]
    [InlineData(Orientation.Horizontal, AutoToolTipPlacement.BottomRight)]
    [InlineData(Orientation.Vertical, AutoToolTipPlacement.TopLeft)]
    [InlineData(Orientation.Vertical, AutoToolTipPlacement.BottomRight)]
    public void AutoToolTipSitsBesideTheThumb(Orientation orientation, AutoToolTipPlacement placement)
    {
        (UIRoot root, Slider slider) = CreateAutoToolTipSlider(orientation, placement);
        ElementInputBridge bridge = new();
        (float x, float y) = Center(slider.Track.Thumb);
        (float toX, float toY) = orientation == Orientation.Horizontal ? (x + 47, y) : (x, y - 47);

        bridge.Dispatch(root, PointerFrame(x, y, currentDown: true));
        bridge.Dispatch(root, PointerFrame(x, y, toX, toY, previousDown: true, currentDown: true));
        root.ProcessFrame();

        ToolTip toolTip = Assert.IsType<ToolTip>(slider.AutoToolTip);
        Assert.True(toolTip.IsOpen);
        Assert.Equal(4.7f, slider.Value, 3);
        AssertBeside(slider.Track.Thumb.ArrangedBounds, toolTip.PopupRoot.ArrangedBounds, orientation, placement);
    }

    // AutoToolTipPlacement None, the default, opens no ToolTip during a drag.
    [Fact]
    public void AutoToolTipNoneOpensNothing()
    {
        (UIRoot root, Slider slider) = CreateAutoToolTipSlider(Orientation.Horizontal, AutoToolTipPlacement.None);
        ElementInputBridge bridge = new();
        (float x, float y) = Center(slider.Track.Thumb);
        int rootChildren = root.VisualChildren.Count;

        bridge.Dispatch(root, PointerFrame(x, y, currentDown: true));
        bridge.Dispatch(root, PointerFrame(x, y, x + 47, y, previousDown: true, currentDown: true));
        root.ProcessFrame();

        Assert.Equal(4.7f, slider.Value, 3);
        Assert.Null(slider.AutoToolTip);
        Assert.Equal(rootChildren, root.VisualChildren.Count);
    }

    // The ToolTip that the slider hosts takes no pointer input after the drag that opened it: the thumb is still hit,
    // a second drag still moves the value, and a press beside the thumb still pages through the repeat button.
    [Fact]
    public void AutoToolTipHostTakesNoLaterPresses()
    {
        (UIRoot root, Slider slider) = CreateAutoToolTipSlider(Orientation.Horizontal, AutoToolTipPlacement.TopLeft);
        ElementInputBridge bridge = new();
        (float x, float y) = Center(slider.Track.Thumb);
        bridge.Dispatch(root, PointerFrame(x, y, currentDown: true));
        bridge.Dispatch(root, PointerFrame(x, y, x + 30, y, previousDown: true, currentDown: true));
        bridge.Dispatch(root, PointerFrame(x + 30, y, x + 30, y, previousDown: true));
        root.ProcessFrame();
        Assert.Equal(3, slider.Value, 3);
        Assert.False(slider.AutoToolTip!.IsOpen);

        (x, y) = Center(slider.Track.Thumb);
        Assert.Same(slider.Track.Thumb, new HitTestService().HitTest(root, x, y)?.Element);
        bridge.Dispatch(root, PointerFrame(x, y, currentDown: true));
        bridge.Dispatch(root, PointerFrame(x, y, x + 20, y, previousDown: true, currentDown: true));
        bridge.Dispatch(root, PointerFrame(x + 20, y, x + 20, y, previousDown: true));
        root.ProcessFrame();
        Assert.Equal(5, slider.Value, 3);

        float before = slider.Track.ArrangedBounds.X + 10;
        Assert.Same(slider.Track.DecreaseRepeatButton, new HitTestService().HitTest(root, before, y)?.Element);
        bridge.Dispatch(root, PointerFrame(before, y, currentDown: true));
        bridge.Dispatch(root, PointerFrame(before, y, before, y, previousDown: true));
        Assert.Equal(4, slider.Value, 3);
    }

    // A template replacement during a drag closes the drag ToolTip, and the next drag places it beside the new thumb.
    [Fact]
    public void TemplateReplacementClosesAutoToolTip()
    {
        (UIRoot root, Slider slider) = CreateAutoToolTipSlider(Orientation.Horizontal, AutoToolTipPlacement.BottomRight);
        ElementInputBridge bridge = new();
        (float x, float y) = Center(slider.Track.Thumb);
        bridge.Dispatch(root, PointerFrame(x, y, currentDown: true));
        bridge.Dispatch(root, PointerFrame(x, y, x + 30, y, previousDown: true, currentDown: true));
        root.ProcessFrame();
        ToolTip toolTip = Assert.IsType<ToolTip>(slider.AutoToolTip);
        Assert.True(toolTip.IsOpen);

        Track templateTrack = new();
        slider.ComponentTemplate = new ComponentTemplate<Slider>("Slider.TrackOnly", context =>
        {
            context.RequirePart("PART_Track", templateTrack);
            return templateTrack;
        });
        root.ProcessFrame();
        bridge.Dispatch(root, PointerFrame(x + 30, y, x + 30, y, previousDown: true));
        root.ProcessFrame();

        Assert.Same(templateTrack, slider.Track);
        Assert.False(toolTip.IsOpen);

        (x, y) = Center(templateTrack.Thumb);
        bridge.Dispatch(root, PointerFrame(x, y, currentDown: true));
        bridge.Dispatch(root, PointerFrame(x, y, x + 10, y, previousDown: true, currentDown: true));
        root.ProcessFrame();
        Assert.True(toolTip.IsOpen);
        AssertBeside(templateTrack.Thumb.ArrangedBounds, toolTip.PopupRoot.ArrangedBounds, Orientation.Horizontal, AutoToolTipPlacement.BottomRight);
    }

    // As WPF's Slider.OnPreviewMouseLeftButtonDown, move-to-point belongs to the whole slider: a press on the top TickBar
    // band, outside the Track, at the x of value 8 (x 85 of a 110-pixel slider for 0..10) sets Value to 8.
    [Fact]
    public void MoveToPointPressOnTickBarBandMovesValue()
    {
        Slider slider = CreateTickSlider(Orientation.Horizontal, TickPlacement.Both);
        slider.IsMoveToPointEnabled = true;
        UIRoot root = new(200, 200);
        root.VisualChildren.Add(slider);
        root.ProcessFrame();
        LayoutRect bounds = slider.ArrangedBounds;
        float x = bounds.X + 85;
        float y = bounds.Y + 1;
        Assert.True(y < slider.Track.ArrangedBounds.Y);
        ElementInputBridge bridge = new();

        bridge.Dispatch(root, PointerFrame(x, y, currentDown: true));
        bridge.Dispatch(root, PointerFrame(x, y, x, y, previousDown: true));

        Assert.Equal(8, slider.Value, 3);
    }

    // The move-to-point press is compared where the slider is drawn: under a 2x scaled ancestor the thumb at value 0
    // is drawn at x 0..20, so a press at x 18 leaves the value at 0, and a press at x 170 (layout x 85) gives 8.
    [Fact]
    public void MoveToPointUnderScaledAncestorUsesTheDrawnPoint()
    {
        Slider slider = CreateSelectionSlider(Orientation.Horizontal);
        slider.IsMoveToPointEnabled = true;
        Cerneala.UI.Controls.StackPanel panel = new()
        {
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
            RenderTransform = new Transform(Cerneala.UI.Media.Matrix3x2.CreateScale(2, 2)),
            RenderTransformOrigin = new LayoutPoint(0, 0)
        };
        panel.Children.Add(slider);
        UIRoot root = new(400, 400);
        root.VisualChildren.Add(panel);
        root.ProcessFrame();
        ElementInputBridge bridge = new();

        bridge.Dispatch(root, PointerFrame(18, 10, currentDown: true));
        bridge.Dispatch(root, PointerFrame(18, 10, 18, 10, previousDown: true));
        root.ProcessFrame();
        Assert.Equal(0, slider.Value);

        bridge.Dispatch(root, PointerFrame(170, 10, currentDown: true));
        bridge.Dispatch(root, PointerFrame(170, 10, 170, 10, previousDown: true));
        Assert.Equal(8, slider.Value, 3);
    }

    private static Slider CreateSelectionSlider(Orientation orientation)
    {
        bool horizontal = orientation == Orientation.Horizontal;
        return new Slider
        {
            Orientation = orientation,
            Minimum = 0,
            Maximum = 10,
            Width = horizontal ? 110 : 20,
            Height = horizontal ? 20 : 110,
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top
        };
    }

    // The rectangles that the element draws, relative to its arranged top-left corner, with their solid color.
    private static IReadOnlyList<(DrawCommandKind Kind, LayoutRect Rect, Color? Color, float Thickness)> RenderBoxes(UIElement element)
    {
        UIRoot root = new(200, 200);
        root.VisualChildren.Add(element);
        root.ProcessFrame();
        LayoutRect bounds = element.ArrangedBounds;
        return root.RetainedRenderer.Commit(root)
            .Where(command => command.Kind is DrawCommandKind.FillRectangle or DrawCommandKind.DrawRectangle)
            .Select(command => (
                command.Kind,
                new LayoutRect(command.Rect.X - bounds.X, command.Rect.Y - bounds.Y, command.Rect.Width, command.Rect.Height),
                (command.Brush as SolidColorBrush)?.Color,
                command.Thickness))
            .ToList();
    }

    private static (UIRoot Root, Slider Slider) CreateAutoToolTipSlider(Orientation orientation, AutoToolTipPlacement placement)
    {
        Slider slider = CreateSelectionSlider(orientation);
        slider.AutoToolTipPlacement = placement;
        slider.Margin = new Thickness(100, 100, 0, 0);
        UIRoot root = new(400, 400);
        root.VisualChildren.Add(slider);
        root.ProcessFrame();
        return (root, slider);
    }

    private static (float X, float Y) Center(UIElement element)
    {
        LayoutRect bounds = element.ArrangedBounds;
        return (bounds.X + (bounds.Width / 2), bounds.Y + (bounds.Height / 2));
    }

    private static void AssertBeside(LayoutRect thumb, LayoutRect popup, Orientation orientation, AutoToolTipPlacement placement)
    {
        Assert.True(popup.Width > 0 && popup.Height > 0, $"The ToolTip has no size: {popup}.");
        if (orientation == Orientation.Horizontal)
        {
            Assert.Equal(thumb.X + (thumb.Width / 2), popup.X + (popup.Width / 2), 0.5f);
            Assert.Equal(
                placement == AutoToolTipPlacement.TopLeft ? thumb.Y : thumb.Y + thumb.Height,
                placement == AutoToolTipPlacement.TopLeft ? popup.Y + popup.Height : popup.Y,
                0.5f);
        }
        else
        {
            Assert.Equal(thumb.Y + (thumb.Height / 2), popup.Y + (popup.Height / 2), 0.5f);
            Assert.Equal(
                placement == AutoToolTipPlacement.TopLeft ? thumb.X : thumb.X + thumb.Width,
                placement == AutoToolTipPlacement.TopLeft ? popup.X + popup.Width : popup.X,
                0.5f);
        }
    }

    private static Slider CreateTickSlider(Orientation orientation, TickPlacement placement)
    {
        bool horizontal = orientation == Orientation.Horizontal;
        Slider slider = new()
        {
            Orientation = orientation,
            Minimum = 0,
            Maximum = 10,
            TickFrequency = 2,
            TickPlacement = placement,
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top
        };
        if (horizontal)
        {
            slider.Width = 110;
        }
        else
        {
            slider.Height = 110;
        }

        return slider;
    }

    private static Slider CreateArrangedSlider(float maximum, float value, float largeChange, out UIRoot root)
    {
        root = new UIRoot(200, 100);
        Slider slider = new()
        {
            Minimum = 0,
            Maximum = maximum,
            LargeChange = largeChange,
            Value = value
        };
        slider.Measure(new MeasureContext(new LayoutSize(110, 20)));
        slider.Arrange(new ArrangeContext(new LayoutRect(0, 0, 110, 20)));
        root.VisualChildren.Add(slider);
        return slider;
    }

    private static (UIRoot Root, ElementInputBridge Bridge) CreateWindowRoot(params UIElement[] children)
    {
        Cerneala.UI.Controls.StackPanel panel = new();
        foreach (UIElement child in children)
        {
            panel.Children.Add(child);
        }

        UIRoot root = new(360, 240);
        root.VisualChildren.Add(new Window { Content = panel, Width = 360, Height = 240 });
        root.ProcessFrame();
        return (root, new ElementInputBridge());
    }

    private static void Click(UIRoot root, ElementInputBridge bridge, UIElement element)
    {
        LayoutRect bounds = element.ArrangedBounds;
        float x = bounds.X + (bounds.Width / 2);
        float y = bounds.Y + (bounds.Height / 2);
        bridge.Dispatch(root, PointerFrame(x, y, currentDown: true));
        root.ProcessFrame();
        bridge.Dispatch(root, PointerFrame(x, y, x, y, previousDown: true));
        root.ProcessFrame();
    }

    private static void PressKey(UIRoot root, ElementInputBridge bridge, InputKey key)
    {
        bridge.Dispatch(root, KeyFrame([], [key]));
        root.ProcessFrame();
        bridge.Dispatch(root, KeyFrame([key], []));
        root.ProcessFrame();
    }

    private static InputFrame KeyFrame(IEnumerable<InputKey> previousKeys, IEnumerable<InputKey> currentKeys)
    {
        return new InputFrame(
            PointerSnapshot.Empty,
            PointerSnapshot.Empty,
            KeyboardSnapshot.FromDownKeys(previousKeys),
            KeyboardSnapshot.FromDownKeys(currentKeys),
            []);
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
