using Cerneala.UI.Motion.Specs;

namespace Cerneala.UI.Motion.Core;

public sealed record MotionStartOptions(
    RetargetMode RetargetMode = RetargetMode.Restart,
    MotionPriority Priority = MotionPriority.Normal,
    string? DebugName = null)
{
    public static MotionStartOptions Default { get; } = new();
}
