using Cerneala.UI.Elements;
using Cerneala.UI.Motion.Core;
using Cerneala.UI.Detective;
using Cerneala.UI.Motion.Properties;
using Cerneala.Tests.UI.Motion.Core;
using MotionFactory = Cerneala.UI.Motion.Specs.Motion;

namespace Cerneala.Tests.UI.Detective;

public sealed class MotionDiagnosticsTests
{
    [Fact]
    public void DiagnosticsRecordStartSampleAndComplete()
    {
        ManualMotionClock clock = new();
        UIRoot root = new(100, 100, motionClock: clock);
        root.Detective.Motion.IsEnabled = true;
        MotionValue<float> value = root.Motion.Graph.CreateValue(0f);

        value.AnimateTo(1f, MotionFactory.Tween<float>(TimeSpan.FromMilliseconds(10)));
        root.Motion.Tick();
        clock.Advance(TimeSpan.FromMilliseconds(10));
        root.Motion.Tick();

        Assert.Contains(root.Detective.Motion.Trace.Events, e => e.Kind == MotionTraceEventKind.MotionStarted);
        Assert.Contains(root.Detective.Motion.Trace.Events, e => e.Kind == MotionTraceEventKind.MotionSampled);
        Assert.Contains(root.Detective.Motion.Trace.Events, e => e.Kind == MotionTraceEventKind.MotionCompleted);
    }

    [Fact]
    public void DiagnosticsCanBeDisabledWithoutRecordingTraceEvents()
    {
        UIRoot root = new();
        MotionValue<float> value = root.Motion.Graph.CreateValue(0f);

        value.AnimateTo(1f, MotionFactory.Tween<float>(TimeSpan.FromMilliseconds(10)));
        root.Motion.Tick();

        Assert.Empty(root.Detective.Motion.Trace.Events);
    }

    [Fact]
    public void SnapshotReportsActiveNodeCount()
    {
        UIRoot root = new();
        MotionValue<float> value = root.Motion.Graph.CreateValue(0f);
        value.AnimateTo(1f, MotionFactory.Tween<float>(TimeSpan.FromMilliseconds(100)));

        MotionGraphSnapshot snapshot = root.Detective.CaptureMotion();

        Assert.Equal(1, snapshot.ActiveNodeCount);
    }

    [Fact]
    public void SnapshotReportsLatestFrameSamplingAndPropertyWrites()
    {
        ManualMotionClock clock = new();
        UIRoot root = new(100, 100, motionClock: clock);
        UIElement element = new();
        root.VisualChildren.Add(element);
        MotionPropertyBinding<float> binding = root.Motion.Properties.GetOrCreateBinding(
            root.Motion,
            element,
            UIElement.OpacityProperty);
        binding.AnimateTo(
            0f,
            MotionFactory.Tween<float>(TimeSpan.FromMilliseconds(100)),
            new MotionPropertyStartOptions { HoldOnComplete = true });
        root.Motion.Tick();
        clock.Advance(TimeSpan.FromMilliseconds(50));

        MotionFrameResult frame = root.Motion.Tick();
        MotionGraphSnapshot snapshot = root.Detective.CaptureMotion();

        Assert.Equal(frame.MotionNodesSampled, snapshot.ValuesSampledThisFrame);
        Assert.Equal(frame.MotionPropertyWrites, snapshot.PropertiesWrittenThisFrame);
        Assert.True(snapshot.ValuesSampledThisFrame > 0);
        Assert.True(snapshot.PropertiesWrittenThisFrame > 0);
        Assert.Equal(1, snapshot.ActivePropertyBindings);
    }

    [Fact]
    public void SnapshotDoesNotCountIdleCachedPropertyBindingAsActive()
    {
        UIRoot root = new();
        UIElement element = new();
        root.VisualChildren.Add(element);
        root.Motion.Properties.GetOrCreateBinding(
            root.Motion,
            element,
            UIElement.OpacityProperty);

        MotionGraphSnapshot snapshot = root.Detective.CaptureMotion();

        Assert.Equal(0, snapshot.ActivePropertyBindings);
    }
}
