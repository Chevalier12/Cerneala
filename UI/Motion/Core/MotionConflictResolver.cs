namespace Cerneala.UI.Motion.Core;

public sealed class MotionConflictResolver
{
    public MotionComposition Resolve(MotionComposition current, MotionComposition incoming)
    {
        return incoming.Priority >= current.Priority ? incoming : current;
    }
}
