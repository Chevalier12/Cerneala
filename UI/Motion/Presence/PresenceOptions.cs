using Cerneala.UI.Motion.Specs;

namespace Cerneala.UI.Motion.Presence;

public sealed class PresenceOptions
{
    private PresenceOptions(MotionSpec<float> enter, MotionSpec<float> exit, bool excludeInputWhileExiting)
    {
        Enter = enter ?? throw new ArgumentNullException(nameof(enter));
        Exit = exit ?? throw new ArgumentNullException(nameof(exit));
        ExcludeInputWhileExiting = excludeInputWhileExiting;
    }

    public MotionSpec<float> Enter { get; }

    public MotionSpec<float> Exit { get; }

    public bool ExcludeInputWhileExiting { get; init; } = true;

    public static PresenceOptions FadeAndScale(MotionSpec<float> enter, MotionSpec<float> exit)
    {
        return FadeAndScale(enter, exit, excludeInputWhileExiting: true);
    }

    public static PresenceOptions FadeAndScale(
        MotionSpec<float> enter,
        MotionSpec<float> exit,
        bool excludeInputWhileExiting)
    {
        return new PresenceOptions(enter, exit, excludeInputWhileExiting);
    }
}
