namespace Cerneala.UI.Motion.Core;

public sealed class ReducedMotionPolicy
{
    public static ReducedMotionPolicy Default { get; } = new();

    public ReducedMotionPolicy(ReducedMotionMode mode = ReducedMotionMode.NoPreference)
    {
        Mode = mode;
    }

    public ReducedMotionMode Mode { get; }
}
