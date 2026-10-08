using Cerneala.Timbre;
using Cerneala.UI.Elements;
using Cerneala.UI.Motion.Core;
using Cerneala.UI.Motion.Properties;
using Cerneala.UI.Motion.Specs;

namespace Cerneala.UI.Timbre;

// Entry point of `playback.Motion()`: animations of the captured playback's
// Volume and declared float parameters.
public sealed class SoundMotionFacade
{
    private readonly UIRoot? root;

    internal SoundMotionFacade(SoundPlayback playback, UIRoot? root)
    {
        Playback = playback;
        this.root = root;
    }

    public SoundPlayback Playback { get; }

    public SoundMotionAnimationBuilder Animate(SoundParameter<float> parameter)
    {
        ArgumentNullException.ThrowIfNull(parameter);
        return new SoundMotionAnimationBuilder(Playback, root, parameter);
    }
}

public sealed class SoundMotionAnimationBuilder
{
    private readonly SoundPlayback playback;
    private readonly UIRoot? root;
    private readonly SoundParameter<float> parameter;
    private bool hasFrom;
    private float from;
    private float to;

    internal SoundMotionAnimationBuilder(SoundPlayback playback, UIRoot? root, SoundParameter<float> parameter)
    {
        this.playback = playback;
        this.root = root;
        this.parameter = parameter;
    }

    public SoundMotionAnimationBuilder From(float value)
    {
        from = value;
        hasFrom = true;
        return this;
    }

    public SoundMotionAnimationBuilder To(float value)
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
        return SoundPlaybackMotion.Animate(root, playback, parameter, hasFrom, from, toCurrent: false, to, spec, options);
    }
}
