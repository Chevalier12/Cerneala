using Cerneala.UI.Motion.Specs;
using Cerneala.UI.Styling;
using MotionFactory = Cerneala.UI.Motion.Specs.Motion;

namespace Cerneala.UI.Motion.Styling;

public static class ThemeMotionTokens
{
    public const string Instant = nameof(Instant);
    public const string FastOut = nameof(FastOut);
    public const string FastIn = nameof(FastIn);
    public const string Standard = nameof(Standard);
    public const string Emphasized = nameof(Emphasized);
    public const string GentleSpring = nameof(GentleSpring);
    public const string SnappySpring = nameof(SnappySpring);
    public const string LayoutSpring = nameof(LayoutSpring);
    public const string Enter = nameof(Enter);
    public const string Exit = nameof(Exit);

    public static readonly ThemeKey<MotionTokens> Key = new("MotionTokens");

    public static MotionTokens CreateDefault()
    {
        return new MotionTokens()
            .Set(Instant, MotionFactory.Tween(TimeSpan.FromMilliseconds(1), Easings.Linear))
            .Set(FastOut, MotionFactory.Tween(TimeSpan.FromMilliseconds(120), Easings.Standard))
            .Set(FastIn, MotionFactory.Tween(TimeSpan.FromMilliseconds(120), Easings.EaseIn))
            .Set(Standard, MotionFactory.Tween(TimeSpan.FromMilliseconds(180), Easings.Standard))
            .Set(Emphasized, MotionFactory.Tween(TimeSpan.FromMilliseconds(240), Easings.Emphasized))
            .Set(GentleSpring, MotionFactory.Spring(stiffness: 420, damping: 36))
            .Set(SnappySpring, MotionFactory.Spring(stiffness: 700, damping: 44))
            .Set(LayoutSpring, MotionFactory.Spring(stiffness: 520, damping: 38))
            .Set(Enter, MotionFactory.Tween(TimeSpan.FromMilliseconds(180), Easings.EaseOut))
            .Set(Exit, MotionFactory.Tween(TimeSpan.FromMilliseconds(140), Easings.EaseIn));
    }

    public static MotionSpec Resolve(ThemeProvider provider, string tokenName)
    {
        ArgumentNullException.ThrowIfNull(provider);
        return provider.Get(Key).Get(tokenName);
    }
}
