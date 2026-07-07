namespace Cerneala.UI.Motion.Core;

public sealed class ReducedMotionPolicy
{
    public static ReducedMotionPolicy Default => new();

    public ReducedMotionPolicy(ReducedMotionMode mode = ReducedMotionMode.NoPreference)
    {
        Mode = mode;
    }

    public ReducedMotionMode Mode { get; private set; }

    public void SetMode(ReducedMotionMode mode)
    {
        Mode = mode;
    }
}
