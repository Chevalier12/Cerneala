using Cerneala.UI.Motion.Specs;

namespace Cerneala.UI.Motion.Presence;

public sealed class PresenceOptions
{
    private PresenceOptions(MotionSpec<float> enter, MotionSpec<float> exit)
    {
        Enter = enter ?? throw new ArgumentNullException(nameof(enter));
        Exit = exit ?? throw new ArgumentNullException(nameof(exit));
    }

    public MotionSpec<float> Enter { get; }

    public MotionSpec<float> Exit { get; }

    public bool ExcludeInputWhileExiting { get; init; } = true;

    public static PresenceOptions FadeAndScale(MotionSpec<float> enter, MotionSpec<float> exit)
    {
        return new PresenceOptions(enter, exit);
    }
}
