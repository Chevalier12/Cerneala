using Cerneala.Drawing;
using Cerneala.UI.Controls;
using Cerneala.UI.Controls.Primitives;
using Cerneala.UI.Elements;
using Cerneala.UI.Input;
using Cerneala.UI.Layout;
using Cerneala.UI.Layout.Panels;
using Cerneala.UI.Media;

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

        bridge.Dispatch(root, PointerFrame(18, 5, currentDown: true));
        bridge.Dispatch(root, PointerFrame(18, 5, 55, 5, previousDown: true, currentDown: true));

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
    public void ScrollBarRendersItsConfiguredBorderBrush()
    {
        LinearGradientBrush brush = new(
            new DrawPoint(0, 0),
            new DrawPoint(20, 0),
            [new GradientStop(0, Color.White), new GradientStop(1, Color.Black)]);
        ScrollBar scrollBar = new() { BorderBrush = brush };
        UIRoot root = new(20, 40);
        scrollBar.Arrange(new ArrangeContext(new LayoutRect(0, 0, 20, 40)));
        root.VisualChildren.Add(scrollBar);
        root.ProcessFrame();

        DrawCommandList commands = root.RetainedRenderer.Commit(root);

        Assert.Contains(commands, command => command.Kind == DrawCommandKind.DrawRectangle && ReferenceEquals(command.Brush, brush));
    }

    [Fact]
    public void TemplatedScrollBarKeepsGeneratedRootStable()
    {
        ScrollBar scrollBar = new();
        Track root = new();
        scrollBar.ComponentTemplate = TrackTemplate("test", root);

        scrollBar.Measure(new MeasureContext(new LayoutSize(100, 100)));
        scrollBar.Measure(new MeasureContext(new LayoutSize(100, 100)));

        Assert.Same(root, scrollBar.ComponentTemplateInstance!.Root);
        Assert.Same(root, scrollBar.Track);
    }

    [Fact]
    public void TemplateTrackIsTheActiveValueSource()
    {
        Track templateTrack = new();
        ScrollBar scrollBar = new()
        {
            Maximum = 100,
            ComponentTemplate = new ComponentTemplate<ScrollBar>("custom", context =>
            {
                context.RequirePart("PART_Track", templateTrack);
                return templateTrack;
            })
        };

        templateTrack.Value = 50;

        Assert.Same(templateTrack, scrollBar.Track);
        Assert.Equal(50, scrollBar.Value);
    }

    [Fact]
    public void DefaultTemplatePlacesDirectionButtonsAroundTrackInBothOrientations()
    {
        ScrollBar scrollBar = new();
        RepeatButton decrease = Part<RepeatButton>(scrollBar, "PART_DecreaseButton");
        RepeatButton increase = Part<RepeatButton>(scrollBar, "PART_IncreaseButton");

        scrollBar.Measure(new MeasureContext(new LayoutSize(12, 100)));
        scrollBar.Arrange(new ArrangeContext(new LayoutRect(0, 0, 12, 100)));
        Assert.True(decrease.ArrangedBounds.Y < scrollBar.Track.ArrangedBounds.Y);
        Assert.True(scrollBar.Track.ArrangedBounds.Y < increase.ArrangedBounds.Y);
        Assert.Equal("^", decrease.Content);
        Assert.Equal("v", increase.Content);

        ComponentTemplateInstance instance = scrollBar.ComponentTemplateInstance!;
        scrollBar.Orientation = Orientation.Horizontal;
        scrollBar.Measure(new MeasureContext(new LayoutSize(100, 12)));
        scrollBar.Arrange(new ArrangeContext(new LayoutRect(0, 0, 100, 12)));

        Assert.Same(instance, scrollBar.ComponentTemplateInstance);
        Assert.True(decrease.ArrangedBounds.X < scrollBar.Track.ArrangedBounds.X);
        Assert.True(scrollBar.Track.ArrangedBounds.X < increase.ArrangedBounds.X);
        Assert.Equal("<", decrease.Content);
        Assert.Equal(">", increase.Content);
    }

    [Theory]
    [InlineData(1f)]
    [InlineData(1.25f)]
    [InlineData(2f)]
    public void DirectionGlyphsAndHitTargetsRemainUsableAcrossLayoutScales(float scale)
    {
        LayoutRounding rounding = LayoutRounding.ForScale(scale);
        UIRoot root = new(220, 220);
        ScrollBar horizontal = new() { Orientation = Orientation.Horizontal };
        ScrollBar vertical = new() { Orientation = Orientation.Vertical };
        horizontal.Measure(new MeasureContext(new LayoutSize(160, 12), rounding));
        horizontal.Arrange(new ArrangeContext(new LayoutRect(0, 0, 160, 12), rounding));
        vertical.Measure(new MeasureContext(new LayoutSize(12, 160), rounding));
        vertical.Arrange(new ArrangeContext(new LayoutRect(40, 24, 12, 160), rounding));
        root.VisualChildren.Add(horizontal);
        root.VisualChildren.Add(vertical);

        RepeatButton left = Part<RepeatButton>(horizontal, "PART_DecreaseButton");
        RepeatButton right = Part<RepeatButton>(horizontal, "PART_IncreaseButton");
        RepeatButton up = Part<RepeatButton>(vertical, "PART_DecreaseButton");
        RepeatButton down = Part<RepeatButton>(vertical, "PART_IncreaseButton");

        Assert.Equal("<", left.Content);
        Assert.Equal(">", right.Content);
        Assert.Equal("^", up.Content);
        Assert.Equal("v", down.Content);
        Assert.True(left.ArrangedBounds.Width > 0 && left.ArrangedBounds.Height > 0);
        Assert.True(up.ArrangedBounds.Width > 0 && up.ArrangedBounds.Height > 0);
        Assert.Same(left, HitCenter(root, left));
        Assert.Same(right, HitCenter(root, right));
        Assert.Same(up, HitCenter(root, up));
        Assert.Same(down, HitCenter(root, down));
    }

    [Fact]
    public void DirectionButtonUsesSmallChangeAndRepeatsWhileHeld()
    {
        UIRoot root = new(120, 20);
        ScrollBar scrollBar = new()
        {
            Orientation = Orientation.Horizontal,
            Maximum = 100,
            Value = 50,
            SmallChange = 5
        };
        scrollBar.Measure(new MeasureContext(new LayoutSize(110, 12)));
        scrollBar.Arrange(new ArrangeContext(new LayoutRect(0, 0, 110, 12)));
        root.VisualChildren.Add(scrollBar);
        RepeatButton decrease = Part<RepeatButton>(scrollBar, "PART_DecreaseButton");
        ElementInputBridge bridge = new();
        float x = decrease.ArrangedBounds.X + (decrease.ArrangedBounds.Width / 2);
        float y = decrease.ArrangedBounds.Y + (decrease.ArrangedBounds.Height / 2);

        bridge.Dispatch(root, PointerFrame(x, y, currentDown: true), TimeSpan.Zero);
        Assert.Equal(45, scrollBar.Value);

        bridge.Dispatch(
            root,
            PointerFrame(x, y, x, y, previousDown: true, currentDown: true),
            TimeSpan.FromMilliseconds(decrease.Delay));

        Assert.Equal(40, scrollBar.Value);
    }

    [Fact]
    public void ClampedDirectionButtonDoesNotRaiseFalseScrollEvents()
    {
        UIRoot root = new(120, 20);
        ScrollBar scrollBar = new() { Orientation = Orientation.Horizontal, Maximum = 100, Value = 0 };
        scrollBar.Measure(new MeasureContext(new LayoutSize(110, 12)));
        scrollBar.Arrange(new ArrangeContext(new LayoutRect(0, 0, 110, 12)));
        root.VisualChildren.Add(scrollBar);
        RepeatButton decrease = Part<RepeatButton>(scrollBar, "PART_DecreaseButton");
        int scrollEvents = 0;
        scrollBar.Scroll += (_, _) => scrollEvents++;
        float x = decrease.ArrangedBounds.X + (decrease.ArrangedBounds.Width / 2);
        float y = decrease.ArrangedBounds.Y + (decrease.ArrangedBounds.Height / 2);

        new ElementInputBridge().Dispatch(root, PointerFrame(x, y, currentDown: true), TimeSpan.Zero);

        Assert.Equal(0, scrollBar.Value);
        Assert.Equal(0, scrollEvents);
    }

    [Fact]
    public void MinimalTemplateWithoutButtonsRemainsPageScrollable()
    {
        Track track = new();
        ScrollBar scrollBar = new()
        {
            Maximum = 100,
            Value = 50,
            LargeChange = 10,
            ComponentTemplate = TrackTemplate("minimal", track)
        };
        List<ScrollEventType> events = [];
        scrollBar.Scroll += (_, args) => events.Add(args.ScrollEventType);

        track.DecreaseLarge();

        Assert.Equal(40, scrollBar.Value);
        Assert.Equal([ScrollEventType.LargeDecrement], events);
    }

    [Fact]
    public void OldTrackStopsChangingScrollBarAfterTemplateSwap()
    {
        Track oldTrack = new();
        Track newTrack = new();
        ScrollBar scrollBar = new()
        {
            Maximum = 100,
            ComponentTemplate = TrackTemplate("old", oldTrack)
        };

        scrollBar.ComponentTemplate = TrackTemplate("new", newTrack);
        oldTrack.Value = 50;

        Assert.Same(newTrack, scrollBar.Track);
        Assert.Equal(0, scrollBar.Value);
    }

    [Fact]
    public void ScrollEventsDescribeSmallLargeAndThumbInteractions()
    {
        ScrollBar scrollBar = new() { Maximum = 100, Value = 50, SmallChange = 5, LargeChange = 10 };
        List<ScrollEventType> events = [];
        scrollBar.Scroll += (_, args) => events.Add(args.ScrollEventType);
        RepeatButton decrease = Part<RepeatButton>(scrollBar, "PART_DecreaseButton");
        RepeatButton increase = Part<RepeatButton>(scrollBar, "PART_IncreaseButton");

        ((IInputActivatable)decrease).Activate();
        ((IInputActivatable)increase).Activate();
        scrollBar.Track.DecreaseLarge();
        scrollBar.Track.IncreaseLarge();
        scrollBar.Measure(new MeasureContext(new LayoutSize(12, 110)));
        scrollBar.Arrange(new ArrangeContext(new LayoutRect(0, 0, 12, 110)));
        scrollBar.Track.Thumb.RaiseEvent(new DragDeltaEventArgs(
            Thumb.DragDeltaEvent,
            scrollBar.Track.Thumb,
            0,
            10,
            0,
            10));

        Assert.Equal(
            [
                ScrollEventType.SmallDecrement,
                ScrollEventType.SmallIncrement,
                ScrollEventType.LargeDecrement,
                ScrollEventType.LargeIncrement,
                ScrollEventType.ThumbTrack
            ],
            events);
        Assert.DoesNotContain(ScrollEventType.EndScroll, events);
    }

    [Fact]
    public void ClearingCustomTemplateRestoresDefaultTrackAndButtons()
    {
        Track customTrack = new();
        ScrollBar scrollBar = new() { ComponentTemplate = TrackTemplate("custom", customTrack) };

        scrollBar.ComponentTemplate = null;

        Assert.NotSame(customTrack, scrollBar.Track);
        Assert.IsType<RepeatButton>(scrollBar.ComponentTemplateInstance!.Parts["PART_DecreaseButton"]);
        Assert.IsType<RepeatButton>(scrollBar.ComponentTemplateInstance.Parts["PART_IncreaseButton"]);
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

    private static ComponentTemplate<ScrollBar> TrackTemplate(string name, Track track)
    {
        return new ComponentTemplate<ScrollBar>(name, context =>
        {
            context.RequirePart("PART_Track", track);
            return track;
        });
    }

    private static UIElement? HitCenter(UIRoot root, UIElement element)
    {
        LayoutRect bounds = element.ArrangedBounds;
        return new HitTestService().HitTest(
            root,
            bounds.X + (bounds.Width / 2),
            bounds.Y + (bounds.Height / 2))?.Element;
    }

    private static TElement Part<TElement>(ScrollBar scrollBar, string name)
        where TElement : UIElement
    {
        scrollBar.ApplyTemplate();
        return Assert.IsType<TElement>(scrollBar.ComponentTemplateInstance!.Parts[name]);
    }
}
