using Cerneala.UI.Controls;
using Cerneala.UI.Core;
using Cerneala.UI.Elements;
using Cerneala.UI.Invalidation;
using Cerneala.UI.Layout;
using Cerneala.UI.Motion;
using Cerneala.UI.Motion.Core;
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

        controller.PointerPressed(MotionFactory.Tween<float>(TimeSpan.FromMilliseconds(100)));
        clock.Advance(TimeSpan.FromMilliseconds(50));
        root.ProcessFrame();
        Assert.True(element.Scale < 1);
        controller.Dispose();
        root.ProcessFrame();
        Assert.False(root.Motion.HasActiveMotion);
    }

    [Fact]
    public void GestureControllerDisposalStopsPressedMotionAcrossOneHundredReattachments()
    {
        ManualMotionClock clock = new();
        UIRoot root = new(100, 100, motionClock: clock);
        UIElement element = new();
        root.VisualChildren.Add(element);
        root.ProcessFrame();

        for (int cycle = 0; cycle < 100; cycle++)
        {
            GestureMotionController controller = element.Motion().Gestures();
            controller.PointerPressed(MotionFactory.Tween<float>(TimeSpan.FromSeconds(1)));
            Assert.Equal(PointerMotionState.Pressed, controller.State);
            controller.Dispose();
            root.ProcessFrame();

            Assert.Equal(PointerMotionState.Idle, controller.State);
            Assert.False(root.Motion.HasActiveMotion);
            Assert.Equal(0, root.Motion.Graph.ActiveNodeCount);
        }
    }

    [Fact]
    public void GestureControllerUsesRuntimeEndpointsUnderReducedMotion()
    {
        UIRoot root = new(
            100,
            100,
            reducedMotion: new ReducedMotionPolicy(ReducedMotionMode.Reduce));
        UIElement element = new();
        root.VisualChildren.Add(element);
        root.ProcessFrame();
        using GestureMotionController controller = element.Motion().Gestures();

        controller.PointerPressed(MotionFactory.Tween<float>(TimeSpan.FromSeconds(1)));
        root.ProcessFrame();
        root.ProcessFrame();
        Assert.Equal(0.97f, element.Scale);
        Assert.False(root.Motion.HasActiveMotion);

        controller.PointerReleased(MotionFactory.Tween<float>(TimeSpan.FromSeconds(1)));
        root.ProcessFrame();
        Assert.Equal(1, element.Scale);
        Assert.False(root.Motion.HasActiveMotion);
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
    public void DisposedDragControllerReleasesSubscriptionsAndActiveMotionAcrossReattach()
    {
        ManualMotionClock clock = new();
        UIRoot root = new(100, 100, motionClock: clock);
        UIElement element = new();
        root.VisualChildren.Add(element);
        root.ProcessFrame();

        DragMotionController oldController = element.Motion().Drag();
        oldController.Begin(0, 0, clock.Now);
        oldController.Move(20, 10, clock.Now + TimeSpan.FromMilliseconds(16));
        oldController.End(MotionFactory.Tween<float>(TimeSpan.FromSeconds(1)));
        oldController.Dispose();

        Assert.False(root.Motion.HasActiveMotion);
        oldController.DragX.JumpTo(90);
        oldController.DragY.JumpTo(80);
        Assert.Equal(20, element.TranslateX);
        Assert.Equal(10, element.TranslateY);

        DragMotionController currentController = element.Motion().Drag();
        currentController.Begin(20, 10, clock.Now);
        currentController.Move(25, 15, clock.Now + TimeSpan.FromMilliseconds(16));
        Assert.Equal(25, element.TranslateX);
        Assert.Equal(15, element.TranslateY);

        oldController.DragX.JumpTo(100);
        oldController.DragY.JumpTo(100);
        Assert.Equal(25, element.TranslateX);
        Assert.Equal(15, element.TranslateY);

        currentController.Dispose();
        for (int i = 0; i < 100; i++)
        {
            DragMotionController cycle = element.Motion().Drag();
            cycle.Begin(element.TranslateX, element.TranslateY, clock.Now);
            cycle.Move(element.TranslateX + 1, element.TranslateY + 1, clock.Now + TimeSpan.FromMilliseconds(16));
            cycle.End(MotionFactory.Tween<float>(TimeSpan.FromSeconds(1)));
            cycle.Dispose();
            Assert.Equal(0, root.Motion.Graph.ActiveNodeCount);
        }
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
    public void ScrollTimelineNormalizesHorizontalProgressAndClampsOffsets()
    {
        UIRoot root = new(100, 100);
        ScrollViewer scrollViewer = new()
        {
            Content = new FixedElement(new LayoutSize(300, 100)),
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto
        };
        root.VisualChildren.Add(scrollViewer);
        root.ProcessFrame();

        ScrollTimeline timeline = scrollViewer.Motion().ScrollTimeline();
        scrollViewer.ScrollInfo.SetHorizontalOffset(500);
        timeline.Update();

        Assert.Equal(1, timeline.HorizontalProgress.Current);
        Assert.Equal(0, timeline.Progress.Current);
        Assert.False(root.Motion.HasActiveMotion);
    }

    [Fact]
    public void ScrollTimelineUsesZeroProgressWhenExtentDoesNotExceedViewport()
    {
        UIRoot root = new(100, 100);
        ScrollViewer scrollViewer = new() { Content = new FixedElement(new LayoutSize(50, 50)) };
        root.VisualChildren.Add(scrollViewer);
        root.ProcessFrame();

        ScrollTimeline timeline = scrollViewer.Motion().ScrollTimeline();
        timeline.Update();

        Assert.Equal(0, timeline.Progress.Current);
        Assert.Equal(0, timeline.HorizontalProgress.Current);
        Assert.False(root.Motion.HasActiveMotion);
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

    [Fact]
    public void DisposedScrollBindingStopsWritesAndDoesNotAccumulateListenersOnReattach()
    {
        UIRoot root = new(100, 100);
        ScrollViewer scrollViewer = new() { Content = new FixedElement(new LayoutSize(100, 300)) };
        UIElement header = new();
        root.VisualChildren.Add(scrollViewer);
        root.VisualChildren.Add(header);
        root.ProcessFrame();

        ScrollTimeline timeline = scrollViewer.Motion().ScrollTimeline();
        for (int i = 0; i < 100; i++)
        {
            ScrollMotionBinding<float> binding = timeline.Progress.Map(1, 0);
            header.Motion().Opacity.Bind(binding);
            binding.Dispose();
            root.VisualChildren.Remove(header);
            root.VisualChildren.Remove(scrollViewer);
            root.ProcessFrame();
            root.VisualChildren.Add(scrollViewer);
            root.VisualChildren.Add(header);
            root.ProcessFrame();
        }

        int opacityWrites = 0;
        header.PropertyChanged += (_, args) =>
        {
            if (args.Property == UIElement.OpacityProperty)
            {
                opacityWrites++;
            }
        };

        ScrollMotionBinding<float> activeBinding = timeline.Progress.Map(1, 0);
        header.Motion().Opacity.Bind(activeBinding);
        scrollViewer.ScrollInfo.SetVerticalOffset(100);
        timeline.Update();

        Assert.Equal(0.5f, header.Opacity, precision: 3);
        Assert.Equal(1, opacityWrites);

        activeBinding.Dispose();
        scrollViewer.ScrollInfo.SetVerticalOffset(200);
        timeline.Update();
        Assert.Equal(0.5f, header.Opacity, precision: 3);
        Assert.Equal(1, opacityWrites);
    }

    private sealed class FixedElement(LayoutSize desiredSize) : UIElement
    {
        protected override LayoutSize MeasureCore(MeasureContext context)
        {
            return desiredSize;
        }
    }
}
