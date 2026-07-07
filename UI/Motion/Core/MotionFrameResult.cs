namespace Cerneala.UI.Motion.Core;

public readonly record struct MotionFrameResult(
    MotionFrame Frame,
    bool NeedsAnotherFrame,
    int MotionFrames,
    int MotionNodesSampled,
    int MotionValuesChanged,
    int MotionPropertyWrites,
    int MotionCompleted,
    int MotionRenderInvalidations,
    int MotionLayoutInvalidations,
    int MotionSkippedByReducedMotion)
{
    public bool HasWork =>
        MotionFrames > 0 ||
        MotionNodesSampled > 0 ||
        MotionValuesChanged > 0 ||
        MotionPropertyWrites > 0 ||
        MotionCompleted > 0 ||
        MotionRenderInvalidations > 0 ||
        MotionLayoutInvalidations > 0 ||
        MotionSkippedByReducedMotion > 0;

    public static MotionFrameResult Empty(MotionFrame frame)
    {
        return new MotionFrameResult(frame, false, 0, 0, 0, 0, 0, 0, 0, 0);
    }
}
