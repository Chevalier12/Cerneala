namespace Cerneala.UI.Motion.Diagnostics;

public enum MotionTraceEventKind
{
    MotionStarted,
    MotionRetargeted,
    MotionSampled,
    MotionCompleted,
    MotionCanceled,
    MotionPropertyWritten,
    MotionInvalidatedRender,
    MotionInvalidatedLayout,
    MotionSkippedReducedMotion
}

public readonly record struct MotionTraceEvent(MotionTraceEventKind Kind, string? DebugName = null);
