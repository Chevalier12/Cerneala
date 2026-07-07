using Cerneala.UI.Motion.Core;

namespace Cerneala.UI.Motion.Diagnostics;

public sealed class MotionDiagnostics
{
    private readonly List<MotionFramePhase> phases = [];
    private readonly List<string> warnings = [];

    public bool IsEnabled { get; set; }

    public MotionTrace Trace { get; } = new();

    public IReadOnlyList<MotionFramePhase> Phases => phases;

    public IReadOnlyList<string> Warnings => warnings;

    public int BeforeLayoutSnapshotCaptures { get; private set; }

    public int AfterLayoutSnapshotCaptures { get; private set; }

    public int ReducedMotionSkipCount { get; private set; }

    internal void BeginFrame()
    {
        phases.Clear();
        warnings.Clear();
        BeforeLayoutSnapshotCaptures = 0;
        AfterLayoutSnapshotCaptures = 0;
    }

    internal void RecordPhase(MotionFramePhase phase)
    {
        phases.Add(phase);
    }

    internal void CaptureBeforeLayoutSnapshots()
    {
        BeforeLayoutSnapshotCaptures++;
    }

    internal void CaptureAfterLayoutSnapshots()
    {
        AfterLayoutSnapshotCaptures++;
    }

    public void RecordWarning(string message)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(message);
        warnings.Add(message);
    }

    public void Record(MotionTraceEventKind kind, string? debugName = null)
    {
        if (!IsEnabled)
        {
            return;
        }

        Trace.Record(new MotionTraceEvent(kind, debugName));
    }

    internal void RecordReducedMotionSkip(string? debugName = null)
    {
        ReducedMotionSkipCount++;
        Record(MotionTraceEventKind.MotionSkippedReducedMotion, debugName);
    }

    public MotionGraphSnapshot CreateSnapshot(MotionSystem motion)
    {
        ArgumentNullException.ThrowIfNull(motion);
        return new MotionGraphSnapshot(
            motion.Graph.ActiveNodeCount,
            motion.Properties.BindingCount,
            motion.Layout.ActiveBindingCount,
            motion.Presence.ActiveExitCount,
            ValuesSampledThisFrame: 0,
            PropertiesWrittenThisFrame: 0,
            motion.HasActiveMotion);
    }
}
