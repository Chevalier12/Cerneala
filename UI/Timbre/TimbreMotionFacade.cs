using Cerneala.Timbre;
using Cerneala.UI.Elements;
using Cerneala.UI.Motion.Core;
using Cerneala.UI.Motion.Properties;
using Cerneala.UI.Motion.Specs;

namespace Cerneala.UI.Timbre;

// Entry point of `playback.Motion()`: animations of the captured playback's
// Volume and declared float parameters.
public sealed class TimbreMotionFacade
{
    private readonly UIRoot? root;

    internal TimbreMotionFacade(TimbrePlayback playback, UIRoot? root)
    {
        Playback = playback;
        this.root = root;
    }

    public TimbrePlayback Playback { get; }

    public TimbreMotionAnimationBuilder Animate(TimbreParameter<float> parameter)
    {
        ArgumentNullException.ThrowIfNull(parameter);
        return new TimbreMotionAnimationBuilder(Playback, root, parameter);
    }
}

public sealed class TimbreMotionAnimationBuilder
{
    private readonly TimbrePlayback playback;
    private readonly UIRoot? root;
    private readonly TimbreParameter<float> parameter;
    private bool hasFrom;
    private float from;
    private float to;

    internal TimbreMotionAnimationBuilder(TimbrePlayback playback, UIRoot? root, TimbreParameter<float> parameter)
    {
        this.playback = playback;
        this.root = root;
        this.parameter = parameter;
    }

    public TimbreMotionAnimationBuilder From(float value)
    {
        from = value;
        hasFrom = true;
        return this;
    }

    public TimbreMotionAnimationBuilder To(float value)
    {
        to = value;
        return this;
    }

    public MotionHandle With(MotionSpec<float> spec)
    {
        return With(spec, new MotionPropertyStartOptions { HoldOnComplete = true });
    }

    public MotionHandle With(MotionSpec<float> spec, MotionPropertyStartOptions options)
    {
        ArgumentNullException.ThrowIfNull(spec);
        ArgumentNullException.ThrowIfNull(options);
        return TimbrePlaybackMotion.Animate(root, playback, parameter, hasFrom, from, toCurrent: false, to, spec, options);
    }
}
