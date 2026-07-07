using Cerneala.UI.Controls;
using Cerneala.UI.Elements;
using Cerneala.UI.Invalidation;
using Cerneala.UI.Layout;
using Cerneala.UI.Motion;
using Cerneala.UI.Motion.Input;
using Cerneala.Tests.UI.Motion.Core;
using MotionFactory = Cerneala.UI.Motion.Specs.Motion;

namespace Cerneala.Tests.UI.Motion.Input;

public sealed class MotionInputTimelineTests
{
    [Fact]
    public void PointerPressAndReleaseRetargetScaleMotion()
    {
        ManualMotionClock clock = new();
        UIRoot root = new(100, 100, motionClock: clock);
        UIElement element = new();
        root.VisualChildren.Add(element);
        root.ProcessFrame();

        GestureMotionController controller = element.Motion().Gestures();
        controller.PointerPressed(MotionFactory.Tween<float>(TimeSpan.FromMilliseconds(100)));
        root.ProcessFrame();
        clock.Advance(TimeSpan.FromMilliseconds(50));
        root.ProcessFrame();
        Assert.True(element.Scale < 1);

        controller.PointerReleased(MotionFactory.Tween<float>(TimeSpan.FromMilliseconds(100)));
        clock.Advance(TimeSpan.FromMilliseconds(50));
        root.ProcessFrame();
        Assert.True(element.Scale > 0.97f);
    }

    [Fact]
    public void DragUpdatesMotionValuesWithoutLayoutInvalidation()
    {
        ManualMotionClock clock = new();
        UIRoot root = new(100, 100, motionClock: clock);
        UIElement element = new();
        root.VisualChildren.Add(element);
        root.ProcessFrame();

        DragMotionController drag = element.Motion().Drag();
        drag.Begin(0, 0, clock.Now);
        drag.Move(24, 12, clock.Now + TimeSpan.FromMilliseconds(16));
        FrameStats stats = root.ProcessFrame();

        Assert.Equal(24, drag.DragX.Current);
        Assert.Equal(12, drag.DragY.Current);
        Assert.Equal(24, element.TranslateX);
        Assert.Equal(12, element.TranslateY);
        Assert.Equal(0, stats.MeasuredElements);
        Assert.Equal(0, stats.ArrangedElements);
    }

    [Fact]
    public void DragEndStartsDecayWithCapturedVelocity()
    {
        ManualMotionClock clock = new();
        UIRoot root = new(100, 100, motionClock: clock);
        UIElement element = new();
        root.VisualChildren.Add(element);
        root.ProcessFrame();

        DragMotionController drag = element.Motion().Drag();
        drag.Begin(0, 0, clock.Now);
        drag.Move(30, 0, clock.Now + TimeSpan.FromMilliseconds(30));
        drag.End(MotionFactory.Tween<float>(TimeSpan.FromMilliseconds(100)));

        Assert.True(root.Motion.HasActiveMotion);
        Assert.True(drag.DragX.IsAnimating);
    }

    [Fact]
    public void PointerCaptureLossSettlesDragBackToOriginDeterministically()
    {
        ManualMotionClock clock = new();
        UIRoot root = new(100, 100, motionClock: clock);
        UIElement element = new();
        root.VisualChildren.Add(element);
        root.ProcessFrame();

        DragMotionController drag = element.Motion().Drag();
        drag.Begin(0, 0, clock.Now);
        drag.Move(30, 12, clock.Now + TimeSpan.FromMilliseconds(16));
        drag.PointerCaptureLost(MotionFactory.Tween<float>(TimeSpan.FromMilliseconds(100)));
        root.ProcessFrame();
        clock.Advance(TimeSpan.FromMilliseconds(100));
        root.ProcessFrame();

        Assert.Equal(PointerMotionState.Settling, drag.State);
        Assert.Equal(0, drag.DragX.Current);
        Assert.Equal(0, drag.DragY.Current);
        Assert.Equal(0, element.TranslateX);
        Assert.Equal(0, element.TranslateY);
    }

    [Fact]
    public void ScrollTimelineProgressMapsToOpacityWithoutLayoutInvalidation()
    {
        UIRoot root = new(100, 100);
        ScrollViewer scrollViewer = new();
        FixedElement content = new(new LayoutSize(100, 300));
        UIElement header = new();
        scrollViewer.Content = content;
        root.VisualChildren.Add(scrollViewer);
        root.VisualChildren.Add(header);
        root.ProcessFrame();

        ScrollTimeline timeline = scrollViewer.Motion().ScrollTimeline();
        header.Motion().Opacity.Bind(timeline.Progress.Map(1, 0));
        scrollViewer.ScrollInfo.SetVerticalOffset(100);
        timeline.Update();
        FrameStats stats = root.ProcessFrame();

        Assert.Equal(0.5f, timeline.Progress.Current, precision: 3);
        Assert.Equal(0.5f, header.Opacity, precision: 3);
        Assert.Equal(0, stats.MeasuredElements);
    }

    [Fact]
    public void ScrollLinkedLayoutPropertyRequiresExplicitOptIn()
    {
        UIRoot root = new(100, 100);
        ScrollViewer scrollViewer = new();
        FixedElement content = new(new LayoutSize(100, 300));
        Button button = new();
        scrollViewer.Content = content;
        root.VisualChildren.Add(scrollViewer);
        root.VisualChildren.Add(button);
        root.ProcessFrame();

        ScrollTimeline timeline = scrollViewer.Motion().ScrollTimeline();

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() =>
            button.Motion().Animate(Control.FontSizeProperty).Bind(timeline.Progress.Map(16, 24)));
        Assert.Contains("layout", exception.Message, StringComparison.OrdinalIgnoreCase);

        button.Motion().Animate(Control.FontSizeProperty).Bind(timeline.Progress.Map(16, 24).AllowLayout());
        scrollViewer.ScrollInfo.SetVerticalOffset(100);
        timeline.Update();
        FrameStats stats = root.ProcessFrame();

        Assert.Equal(20, button.FontSize, precision: 3);
        Assert.True(stats.MeasuredElements > 0);
    }

    private sealed class FixedElement(LayoutSize desiredSize) : UIElement
    {
        protected override LayoutSize MeasureCore(MeasureContext context)
        {
            return desiredSize;
        }
    }
}
