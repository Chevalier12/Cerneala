using Cerneala.UI.Elements;
using Cerneala.UI.Motion.Core;
using Cerneala.UI.Motion.Diagnostics;
using Cerneala.Tests.UI.Motion.Core;
using MotionFactory = Cerneala.UI.Motion.Specs.Motion;

namespace Cerneala.Tests.UI.Motion.Diagnostics;

public sealed class MotionDiagnosticsTests
{
    [Fact]
    public void DiagnosticsRecordStartSampleAndComplete()
    {
        ManualMotionClock clock = new();
        UIRoot root = new(100, 100, motionClock: clock);
        root.Motion.Diagnostics.IsEnabled = true;
        MotionValue<float> value = root.Motion.Graph.CreateValue(0f);

        value.AnimateTo(1f, MotionFactory.Tween<float>(TimeSpan.FromMilliseconds(10)));
        root.Motion.Tick();
        clock.Advance(TimeSpan.FromMilliseconds(10));
        root.Motion.Tick();

        Assert.Contains(root.Motion.Diagnostics.Trace.Events, e => e.Kind == MotionTraceEventKind.MotionStarted);
        Assert.Contains(root.Motion.Diagnostics.Trace.Events, e => e.Kind == MotionTraceEventKind.MotionSampled);
        Assert.Contains(root.Motion.Diagnostics.Trace.Events, e => e.Kind == MotionTraceEventKind.MotionCompleted);
    }

    [Fact]
    public void DiagnosticsCanBeDisabledWithoutRecordingTraceEvents()
    {
        UIRoot root = new();
        MotionValue<float> value = root.Motion.Graph.CreateValue(0f);

        value.AnimateTo(1f, MotionFactory.Tween<float>(TimeSpan.FromMilliseconds(10)));
        root.Motion.Tick();

        Assert.Empty(root.Motion.Diagnostics.Trace.Events);
    }

    [Fact]
    public void SnapshotReportsActiveNodeCount()
    {
        UIRoot root = new();
        MotionValue<float> value = root.Motion.Graph.CreateValue(0f);
        value.AnimateTo(1f, MotionFactory.Tween<float>(TimeSpan.FromMilliseconds(100)));

        MotionGraphSnapshot snapshot = root.Motion.Diagnostics.CreateSnapshot(root.Motion);

        Assert.Equal(1, snapshot.ActiveNodeCount);
    }
}
