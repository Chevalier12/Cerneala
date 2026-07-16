using Cerneala.UI.Controls;
using Cerneala.UI.Elements;
using Cerneala.UI.Invalidation;
using Cerneala.UI.Layout;
using Cerneala.UI.Layout.Panels;
using Cerneala.UI.Markup;
using Cerneala.UI.Motion;
using Cerneala.UI.Motion.Core;
using Cerneala.UI.Motion.Input;
using Cerneala.UI.Motion.Presence;
using Cerneala.UI.Motion.Properties;
using Cerneala.Tests.UI.Motion.Core;
using MotionFactory = Cerneala.UI.Motion.Specs.Motion;

namespace Cerneala.Tests.UI.Markup;

public sealed class MotionMarkupLifecycleStressTests
{
    [Fact]
    public void IntegratedMarkupBehaviorsReturnToBaselineAcrossOneHundredAttachCycles()
    {
        ManualMotionClock clock = new();
        UIRoot root = new(100, 100, motionClock: clock);
        Cerneala.UI.Layout.Panels.Canvas parent = new();
        ScrollViewer scroller = new() { Content = new FixedElement(new LayoutSize(100, 300)) };
        StressElement element = new()
        {
            Presence = PresenceOptions.FadeAndScale(
                MotionFactory.Tween<float>(TimeSpan.FromMilliseconds(100)),
                MotionFactory.Tween<float>(TimeSpan.FromMilliseconds(100)))
        };
        root.VisualChildren.Add(parent);
        root.VisualChildren.Add(scroller);
        root.ProcessFrame();

        ScrollTimeline timeline = scroller.Motion().ScrollTimeline();
        using IDisposable session = GeneratedMarkup.AttachMotionSession(element);
        MarkupObservation observation = GeneratedMarkup.ObserveProperty(element, UIElement.IsEnabledProperty);
        int conditionActivations = 0;
        using IDisposable conditions = GeneratedMarkup.AttachConditions(
            element,
            new[] { observation },
            new[]
            {
                new MarkupConditionRule(0, () => (bool)observation.Value!, null, null, () => conditionActivations++)
            });

        DragMotionController? drag = null;
        ScrollMotionBinding<float>? scrollBinding = null;
        EventHandler fired = (_, _) => StartPulse(session, element);
        GeneratedMarkup.AddMotionTrigger(
            session,
            () =>
            {
                element.Fired += fired;
                drag = element.Motion().Drag();
                scrollBinding = timeline.Progress.Map(1, 0);
                element.Motion().Opacity.Bind(scrollBinding);
            },
            () =>
            {
                element.Fired -= fired;
                drag?.Dispose();
                drag = null;
                scrollBinding?.Dispose();
                scrollBinding = null;
            });

        for (int cycle = 0; cycle < 100; cycle++)
        {
            element.IsEnabled = false;
            parent.VisualChildren.Add(element);
            root.ProcessFrame();
            clock.Advance(TimeSpan.FromMilliseconds(120));
            root.ProcessFrame();

            Assert.Equal(1, element.FiredSubscriberCount);
            element.IsEnabled = true;
            element.RaiseFired();
            element.RaiseFired();
            drag!.Begin(0, 0, clock.Now);
            drag.Move(8, 4, clock.Now + TimeSpan.FromMilliseconds(16));
            drag.End(MotionFactory.Tween<float>(TimeSpan.FromSeconds(1)));
            scroller.ScrollInfo.SetVerticalOffset(cycle % 2 == 0 ? 50 : 100);
            timeline.Update();
            Assert.True(root.Motion.HasActiveMotion);

            Assert.True(parent.VisualChildren.Remove(element));
            root.ProcessFrame();
            clock.Advance(TimeSpan.FromMilliseconds(120));
            root.ProcessFrame();

            Assert.False(element.IsAttached);
            Assert.Equal(0, element.FiredSubscriberCount);
            Assert.Null(drag);
            Assert.Null(scrollBinding);
            Assert.Empty(root.Motion.Presence.GetExitingVisualChildren(parent));
            Assert.Equal(0, root.Motion.Graph.ActiveNodeCount);
            Assert.False(root.Motion.HasActiveMotion);
            Assert.Equal(cycle + 1, conditionActivations);
        }

        FrameStats idle = root.ProcessFrame();
        Assert.False(idle.HasWork);
        Assert.Equal(1, idle.NoWorkFrames);
    }

    [Fact]
    public void EventRestartAndScrollUpdateStayWithinWarmAllocationBudget()
    {
        UIRoot root = new(100, 100);
        ScrollViewer scroller = new() { Content = new FixedElement(new LayoutSize(100, 300)) };
        StressElement element = new();
        root.VisualChildren.Add(scroller);
        root.VisualChildren.Add(element);
        root.ProcessFrame();

        using IDisposable session = GeneratedMarkup.AttachMotionSession(element);
        ScrollTimeline timeline = scroller.Motion().ScrollTimeline();
        using ScrollMotionBinding<float> binding = timeline.Progress.Map(1, 0);
        element.Motion().Opacity.Bind(binding);
        EventHandler fired = (_, _) => StartPulse(session, element);
        GeneratedMarkup.AddMotionTrigger(session, () => element.Fired += fired, () => element.Fired -= fired);

        for (int iteration = 0; iteration < 64; iteration++)
        {
            element.RaiseFired();
            scroller.ScrollInfo.SetVerticalOffset(iteration % 2 == 0 ? 25 : 75);
            timeline.Update();
        }

        long before = GC.GetAllocatedBytesForCurrentThread();
        long firstHalfAllocated = 0;
        for (int iteration = 0; iteration < 1_000; iteration++)
        {
            element.RaiseFired();
            scroller.ScrollInfo.SetVerticalOffset(iteration % 2 == 0 ? 25 : 75);
            timeline.Update();

            if (iteration == 499)
            {
                firstHalfAllocated = GC.GetAllocatedBytesForCurrentThread() - before;
            }
        }

        long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        long secondHalfAllocated = allocated - firstHalfAllocated;
        Assert.True(allocated <= 40_000_000, $"Warm event/scroll loop allocated {allocated:N0} bytes.");
        Assert.True(
            secondHalfAllocated <= firstHalfAllocated + 1_000_000,
            $"Warm allocations did not stabilize: first={firstHalfAllocated:N0}, second={secondHalfAllocated:N0} bytes.");

        Assert.True(root.VisualChildren.Remove(element));
        root.ProcessFrame();
        Assert.Equal(0, element.FiredSubscriberCount);
        Assert.Equal(0, root.Motion.Graph.ActiveNodeCount);
        Assert.False(root.Motion.HasActiveMotion);
    }

    private static MarkupMotionExecution StartPulse(IDisposable session, UIElement element)
    {
        return GeneratedMarkup.StartMotionExecution(
            session,
            "NextPreviousPulse",
            () => MarkupMotionExecution.From(
                GeneratedMarkup.StartMotionProperty(
                    session,
                    element,
                    UIElement.ScaleProperty,
                    false,
                    default,
                    false,
                    1.05f,
                    MotionFactory.Tween<float>(TimeSpan.FromSeconds(10)),
                    new MotionPropertyStartOptions())));
    }

    private sealed class StressElement : UIElement
    {
        private EventHandler? fired;

        public int FiredSubscriberCount { get; private set; }

        public event EventHandler Fired
        {
            add
            {
                fired += value;
                FiredSubscriberCount++;
            }
            remove
            {
                fired -= value;
                FiredSubscriberCount--;
            }
        }

        public void RaiseFired()
        {
            fired?.Invoke(this, EventArgs.Empty);
        }
    }

    private sealed class FixedElement(LayoutSize desiredSize) : UIElement
    {
        protected override LayoutSize MeasureCore(MeasureContext context)
        {
            return desiredSize;
        }
    }
}
