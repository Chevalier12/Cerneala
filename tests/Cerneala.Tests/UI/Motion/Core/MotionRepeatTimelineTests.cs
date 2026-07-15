using Cerneala.UI.Elements;
using Cerneala.UI.Motion.Core;
using Cerneala.UI.Motion.Interpolation;
using Cerneala.UI.Motion.Specs;
using Cerneala.Tests.UI.Motion.Core;
using MotionFactory = Cerneala.UI.Motion.Specs.Motion;

namespace Cerneala.Tests.UI.Motion.Core;

public sealed class MotionRepeatTimelineTests
{
    [Fact]
    public void RepeatLoopsExactCycleBoundaries()
    {
        RepeatSpec<float> spec = new(MotionFactory.Tween<float>(TimeSpan.FromMilliseconds(100), Easings.Linear), repeatCount: 2);
        MotionSampler<float> sampler = spec.CreateSampler(0, 10, new FloatMixer(), DefaultContext());

        sampler.Advance(TimeSpan.FromMilliseconds(100));
        Assert.Equal(0, sampler.Current);
        sampler.Advance(TimeSpan.FromMilliseconds(50));
        Assert.Equal(5, sampler.Current, precision: 3);
    }

    [Fact]
    public void PingPongReversesCorrectly()
    {
        PingPongSpec<float> spec = new(MotionFactory.Tween<float>(TimeSpan.FromMilliseconds(100), Easings.Linear), cycles: 2);
        MotionSampler<float> sampler = spec.CreateSampler(0, 10, new FloatMixer(), DefaultContext());

        sampler.Advance(TimeSpan.FromMilliseconds(100));
        Assert.Equal(10, sampler.Current);
        sampler.Advance(TimeSpan.FromMilliseconds(50));
        Assert.Equal(5, sampler.Current, precision: 3);
    }

    [Fact]
    public void EvenCyclePingPongMotionValueCompletesAtStart()
    {
        ManualMotionClock clock = new();
        UIRoot root = new(100, 100, motionClock: clock);
        MotionValue<float> value = root.Motion.Graph.CreateValue(0f);
        MotionHandle handle = value.AnimateTo(
            10,
            new PingPongSpec<float>(
                MotionFactory.Tween<float>(TimeSpan.FromMilliseconds(100), Easings.Linear),
                cycles: 2));

        root.Motion.Tick();
        clock.Advance(TimeSpan.FromMilliseconds(100));
        root.Motion.Tick();
        clock.Advance(TimeSpan.FromMilliseconds(100));
        root.Motion.Tick();

        Assert.True(handle.IsCompleted);
        Assert.Equal(0, value.Current);
        Assert.Equal(0, value.Target);
    }

    [Fact]
    public void InfiniteAnimationKeepsRequestingFramesUntilCanceled()
    {
        ManualMotionClock clock = new();
        UIRoot root = new(100, 100, motionClock: clock);
        MotionValue<float> value = root.Motion.Graph.CreateValue(0f);
        MotionHandle handle = value.AnimateTo(1, new RepeatSpec<float>(MotionFactory.Tween<float>(TimeSpan.FromMilliseconds(10))));

        root.Motion.Tick();
        clock.Advance(TimeSpan.FromMilliseconds(10));
        MotionFrameResult active = root.Motion.Tick();
        handle.Cancel();
        MotionFrameResult canceled = root.Motion.Tick();

        Assert.True(active.NeedsAnotherFrame);
        Assert.False(canceled.NeedsAnotherFrame);
    }

    [Fact]
    public void ReducedMotionMakesInfiniteAnimationStatic()
    {
        ManualMotionClock clock = new();
        UIRoot root = new(
            100,
            100,
            motionClock: clock,
            reducedMotion: new ReducedMotionPolicy(ReducedMotionMode.DisableNonEssential));
        MotionValue<float> value = root.Motion.Graph.CreateValue(0f);

        value.AnimateTo(1, new RepeatSpec<float>(MotionFactory.Tween<float>(TimeSpan.FromMilliseconds(10))));
        MotionFrameResult first = root.Motion.Tick();
        clock.Advance(TimeSpan.FromMilliseconds(10));
        MotionFrameResult second = root.Motion.Tick();

        Assert.Equal(1, value.Current);
        Assert.False(first.NeedsAnotherFrame);
        Assert.False(second.NeedsAnotherFrame);
        Assert.True(root.Motion.Diagnostics.ReducedMotionSkipCount > 0);
    }

    [Fact]
    public void ManualTimelineDrivesSampledValueWithoutClockDelta()
    {
        ManualMotionTimeline timeline = new();
        MotionValue<float> value = timeline.CreateValue(0f);

        timeline.SetProgress(0.75f);
        value.JumpTo(timeline.Progress);

        Assert.Equal(0.75f, value.Current);
    }

    private static MotionSpecContext DefaultContext()
    {
        ValueMixerRegistry mixers = new();
        mixers.RegisterBuiltIns();
        return new MotionSpecContext(ReducedMotionPolicy.Default, mixers, null, TimeSpan.Zero, null);
    }
}
