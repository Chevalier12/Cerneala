namespace Cerneala.UI.Motion.Core;

public readonly record struct MotionComposition(MotionChannel Channel, MotionPriority Priority)
{
    public static MotionComposition Default { get; } = new(MotionChannel.Default, MotionPriority.Normal);
}
