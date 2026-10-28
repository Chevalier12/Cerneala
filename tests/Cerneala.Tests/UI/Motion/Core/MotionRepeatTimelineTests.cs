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

    [Theory]
    [InlineData(100_0000)]
    [InlineData(166700)]
    public void RepeatCycleBoundariesAreExactForCyclesThatAreNotWholeMilliseconds(long cycleTicks)
    {
        TimeSpan cycle = TimeSpan.FromTicks(cycleTicks);
        TweenSpec<float> tween = MotionFactory.Tween<float>(cycle, Easings.Linear);
        MotionSampler<float> sampler = new RepeatSpec<float>(tween, repeatCount: 5).CreateSampler(0, 10, new FloatMixer(), DefaultContext());
        List<string> wrong = [];

        for (int boundary = 1; boundary <= 4; boundary++)
        {
            sampler.Advance(cycle);
            if (Math.Abs(sampler.Current) > 1e-3f)
            {
                wrong.Add($"boundary {boundary}: {sampler.Current} (expected 0)");
            }
        }

        Assert.True(wrong.Count == 0, $"repeat, cycle {cycle.TotalMilliseconds} ms: {string.Join("; ", wrong)}.");
        sampler.Advance(cycle);
        Assert.True(sampler.IsComplete);
        Assert.Equal(10, sampler.Current);
    }

    [Theory]
    [InlineData(100_0000)]
    [InlineData(166700)]
    public void PingPongCycleBoundariesAreExactForCyclesThatAreNotWholeMilliseconds(long cycleTicks)
    {
        TimeSpan cycle = TimeSpan.FromTicks(cycleTicks);
        TweenSpec<float> tween = MotionFactory.Tween<float>(cycle, Easings.Linear);
        MotionSampler<float> sampler = new PingPongSpec<float>(tween, cycles: null).CreateSampler(0, 10, new FloatMixer(), DefaultContext());
        List<string> wrong = [];

        for (int boundary = 1; boundary <= 200; boundary++)
        {
            sampler.Advance(cycle);
            float expected = boundary % 2 == 0 ? 0 : 10;
            if (Math.Abs(sampler.Current - expected) > 1e-3f)
            {
                wrong.Add($"boundary {boundary}: {sampler.Current} (expected {expected})");
            }
        }

        Assert.True(wrong.Count == 0, $"ping-pong, cycle {cycle.TotalMilliseconds} ms: {string.Join("; ", wrong)}.");
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
    public void InfinitePingPongKeepsReversingUntilCanceledByItsOwner()
    {
        PingPongSpec<float> spec = new(
            MotionFactory.Tween<float>(TimeSpan.FromMilliseconds(100), Easings.Linear),
            cycles: null);
        MotionSampler<float> sampler = spec.CreateSampler(
            0,
            10,
            new FloatMixer(),
            DefaultContext());

        sampler.Advance(TimeSpan.FromMilliseconds(100));
        Assert.Equal(10, sampler.Current);
        sampler.Advance(TimeSpan.FromMilliseconds(100));
        Assert.Equal(0, sampler.Current);
        sampler.Advance(TimeSpan.FromMilliseconds(100));
        Assert.Equal(10, sampler.Current);
        Assert.False(sampler.IsComplete);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void FiniteRepeatAdvancesWhenItsTotalRuntimeExceedsTimeSpanMaxValue(bool pingPong)
    {
        // One hour times int.MaxValue cycles is about 245,000 years; TimeSpan holds about 29,227.
        TweenSpec<float> tween = MotionFactory.Tween<float>(TimeSpan.FromHours(1), Easings.Linear);
        MotionSpec<float> spec = pingPong
            ? new PingPongSpec<float>(tween, cycles: int.MaxValue)
            : new RepeatSpec<float>(tween, repeatCount: int.MaxValue);
        MotionSampler<float> sampler = spec.CreateSampler(0, 10, new FloatMixer(), DefaultContext());

        sampler.Advance(TimeSpan.FromMilliseconds(16));

        Assert.False(sampler.IsComplete);
        Assert.Equal(16 / 3_600_000d * 10, sampler.Current, precision: 6);
    }

    [Theory]
    [InlineData(false, 3, 10f)]
    [InlineData(true, 2, 0f)]
    [InlineData(true, 3, 10f)]
    public void FiniteRepeatCompletesExactlyAfterItsCycleCount(bool pingPong, int count, float expected)
    {
        TimeSpan cycle = TimeSpan.FromMilliseconds(100);
        TweenSpec<float> tween = MotionFactory.Tween<float>(cycle, Easings.Linear);
        MotionSpec<float> spec = pingPong
            ? new PingPongSpec<float>(tween, cycles: count)
            : new RepeatSpec<float>(tween, repeatCount: count);
        MotionSampler<float> sampler = spec.CreateSampler(0, 10, new FloatMixer(), DefaultContext());

        sampler.Advance((cycle * count) - TimeSpan.FromTicks(1));
        Assert.False(sampler.IsComplete);
        sampler.Advance(TimeSpan.FromTicks(1));

        Assert.True(sampler.IsComplete);
        Assert.Equal(expected, sampler.Current);
    }

    [Fact]
    public void RepeatRejectsNegativeDelta()
    {
        RepeatSpec<float> spec = new(
            MotionFactory.Tween<float>(TimeSpan.FromMilliseconds(100), Easings.Linear),
            repeatCount: 2);
        MotionSampler<float> sampler = spec.CreateSampler(0, 10, new FloatMixer(), DefaultContext());

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            sampler.Advance(TimeSpan.FromMilliseconds(-1)));
    }

    [Fact]
    public void PingPongRejectsNegativeDelta()
    {
        PingPongSpec<float> spec = new(
            MotionFactory.Tween<float>(TimeSpan.FromMilliseconds(100), Easings.Linear),
            cycles: 2);
        MotionSampler<float> sampler = spec.CreateSampler(0, 10, new FloatMixer(), DefaultContext());

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            sampler.Advance(TimeSpan.FromMilliseconds(-1)));
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
        Assert.True(root.Detective.Motion.ReducedMotionSkipCount > 0);
    }

    [Fact]
    public void ReducedMotionMakesInfinitePingPongStaticAtDestination()
    {
        MotionSpecContext context = new(
            new ReducedMotionPolicy(ReducedMotionMode.DisableNonEssential),
            DefaultContext().Mixers,
            null,
            TimeSpan.Zero,
            null);
        PingPongSpec<float> spec = new(
            MotionFactory.Tween<float>(TimeSpan.FromMilliseconds(100)),
            cycles: null);

        MotionSampler<float> sampler = spec.CreateSampler(
            0,
            10,
            new FloatMixer(),
            context);

        Assert.Equal(10, sampler.Current);
        Assert.True(sampler.IsComplete);
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

    [Fact]
    public void ManualTimelineKeepsProgressNormalizedForNaNInput()
    {
        ManualMotionTimeline timeline = new();

        timeline.SetProgress(float.NaN);

        Assert.True(float.IsFinite(timeline.Progress));
        Assert.InRange(timeline.Progress, 0f, 1f);
    }

    [Fact]
    public void TimelineRegistryRegistersLooksUpEnumeratesAndRemovesTimelines()
    {
        UIRoot root = new();
        ManualMotionTimeline timeline = new();

        root.Motion.Timelines.Register("primary", timeline);

        Assert.Equal(1, root.Motion.Timelines.Count);
        Assert.Equal(["primary"], root.Motion.Timelines.Names);
        Assert.Same(timeline, root.Motion.Timelines.Get("primary"));
        Assert.True(root.Motion.Timelines.TryGet("primary", out MotionTimeline? found));
        Assert.Same(timeline, found);
        Assert.Throws<InvalidOperationException>(() =>
            root.Motion.Timelines.Register("primary", new ManualMotionTimeline()));
        Assert.True(root.Motion.Timelines.Remove("primary"));
        Assert.False(root.Motion.Timelines.TryGet("primary", out _));
    }

    [Fact]
    public void RootTimelineRegistryRejectsMutationFromWrongThread()
    {
        UIRoot root = new();
        Exception? thrown = null;
        Thread thread = new(() =>
        {
            try
            {
                root.Motion.Timelines.Register("background", new ManualMotionTimeline());
            }
            catch (Exception exception)
            {
                thrown = exception;
            }
        });

        thread.Start();
        thread.Join();

        Assert.IsType<InvalidOperationException>(thrown);
    }

    [Fact]
    public void RootTimelineRegistryRejectsReadFromWrongThread()
    {
        UIRoot root = new();
        Exception? thrown = null;
        Thread thread = new(() =>
        {
            try
            {
                _ = root.Motion.Timelines.Count;
            }
            catch (Exception exception)
            {
                thrown = exception;
            }
        });

        thread.Start();
        thread.Join();

        Assert.IsType<InvalidOperationException>(thrown);
    }

    private static MotionSpecContext DefaultContext()
    {
        ValueMixerRegistry mixers = new();
        mixers.RegisterBuiltIns();
        return new MotionSpecContext(ReducedMotionPolicy.Default, mixers, null, TimeSpan.Zero, null);
    }
}
