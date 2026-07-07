using Cerneala.UI.Motion.Core;

namespace Cerneala.UI.Motion.Diagnostics;

public sealed class MotionDiagnostics
{
    private readonly List<MotionFramePhase> phases = [];
    private readonly List<string> warnings = [];

    public IReadOnlyList<MotionFramePhase> Phases => phases;

    public IReadOnlyList<string> Warnings => warnings;

    public int BeforeLayoutSnapshotCaptures { get; private set; }

    public int AfterLayoutSnapshotCaptures { get; private set; }

    internal void BeginFrame()
    {
        phases.Clear();
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
}
