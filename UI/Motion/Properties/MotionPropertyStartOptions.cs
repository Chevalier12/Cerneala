using Cerneala.UI.Motion.Core;
using Cerneala.UI.Motion.Specs;

namespace Cerneala.UI.Motion.Properties;

public sealed class MotionPropertyStartOptions
{
    public static MotionPropertyStartOptions Default { get; } = new();

    public RetargetMode RetargetMode { get; init; } = RetargetMode.Restart;

    public MotionPriority Priority { get; init; } = MotionPriority.Normal;

    public string? DebugName { get; init; }

    public bool HoldOnComplete { get; init; }

    internal MotionStartOptions ToMotionStartOptions()
    {
        return new MotionStartOptions(RetargetMode, Priority, DebugName);
    }
}
