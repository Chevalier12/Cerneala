namespace Cerneala.UI.Motion.Core;

public readonly record struct MotionFrame(
    TimeSpan Now,
    TimeSpan Delta,
    int FrameIndex,
    MotionFrameReason Reason,
    MotionFramePhase Phase);

public enum MotionFrameReason
{
    Initial,
    Scheduled,
    Input,
    Layout,
    Manual
}
