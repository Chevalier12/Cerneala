using Cerneala.UI.Motion.Specs;

namespace Cerneala.UI.Motion;

public static class MotionDefaults
{
    public static MotionSpec FastOut => Specs.Motion.Tween(TimeSpan.FromMilliseconds(120), Easings.Standard);

    public static MotionSpec Standard => Specs.Motion.Tween(TimeSpan.FromMilliseconds(180), Easings.Standard);
}
