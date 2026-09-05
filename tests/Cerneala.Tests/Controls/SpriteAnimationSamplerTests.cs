using System.Diagnostics;
using Cerneala.Drawing;
using Cerneala.UI.Controls;

namespace Cerneala.Tests.Controls;

public sealed class SpriteAnimationSamplerTests
{
    [Fact]
    [Trait("SpriteAnimationStage", "1")]
    public void DefinitionsValidateAndDefensivelyCopyTheirInputs()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Frame(new DrawRect(0, 0, 0, 1), 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => Frame(new DrawRect(float.NaN, 0, 1, 1), 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => Frame(new DrawRect(0, 0, 1, 1), 0));
        Assert.Throws<ArgumentException>(() => new SpriteAnimationClip(" ", [Frame(0, 1)], true));
        Assert.Throws<ArgumentException>(() => new SpriteAnimationClip("Idle", [], true));

        SpriteAnimationFrame[] frames = [Frame(0, 10)];
        SpriteAnimationClip clip = new("Idle", frames, true, version: 3);
        frames[0] = Frame(16, 10);
        Assert.Equal(new DrawRect(0, 0, 16, 16), clip.Frames[0].SourceRect);
        Assert.Equal(TimeSpan.FromMilliseconds(10), clip.Duration);
        Assert.Equal(3, clip.Version);

        Assert.Throws<ArgumentException>(() => new SpriteAnimationSet([clip, clip]));
        SpriteAnimationClip[] clips = [clip];
        SpriteAnimationSet set = new(clips, version: 4);
        clips[0] = new SpriteAnimationClip("Walk", [Frame(16, 10)]);
        Assert.Same(clip, set.Clips[0]);
        Assert.Equal(4, set.Version);
        Assert.True(set.TryGetClip("Idle", out SpriteAnimationClip? resolved));
        Assert.Same(clip, resolved);
        Assert.False(set.TryGetClip("idle", out _));
    }

    [Fact]
    [Trait("SpriteAnimationStage", "1")]
    public void SamplerIsDeterministicAtEveryBoundaryAndPlaybackRate()
    {
        SpriteAnimationClip loop = new(
            "Walk",
            [Frame(0, 100), Frame(16, 200), Frame(32, 300)],
            isLooping: true);
        (TimeSpan Elapsed, double Rate, int Frame, bool Completed)[] cases =
        [
            (TimeSpan.Zero, 1, 0, false),
            (TimeSpan.FromTicks(TimeSpan.FromMilliseconds(100).Ticks - 1), 1, 0, false),
            (TimeSpan.FromMilliseconds(100), 1, 1, false),
            (TimeSpan.FromMilliseconds(300), 1, 2, false),
            (TimeSpan.FromMilliseconds(600), 1, 0, false),
            (TimeSpan.FromMilliseconds(650), 1, 0, false),
            (TimeSpan.FromMilliseconds(50), 2, 1, false),
            (TimeSpan.FromDays(100), 0, 0, false)
        ];

        foreach ((TimeSpan elapsed, double rate, int frame, bool completed) in cases)
        {
            SpriteAnimationSample first = SpriteAnimationSampler.Sample(loop, elapsed, rate);
            SpriteAnimationSample second = SpriteAnimationSampler.Sample(loop, elapsed, rate);
            Assert.Equal(frame, first.FrameIndex);
            Assert.Equal(completed, first.IsCompleted);
            Assert.Equal(first, second);
        }

        SpriteAnimationClip finite = new("Attack", [Frame(0, 100), Frame(16, 100)], isLooping: false);
        Assert.Equal(1, SpriteAnimationSampler.Sample(finite, TimeSpan.FromMilliseconds(100), 1).FrameIndex);
        SpriteAnimationSample end = SpriteAnimationSampler.Sample(finite, TimeSpan.FromMilliseconds(200), 1);
        Assert.Equal(1, end.FrameIndex);
        Assert.True(end.IsCompleted);
        Assert.Throws<ArgumentOutOfRangeException>(() => SpriteAnimationSampler.Sample(loop, TimeSpan.FromTicks(-1), 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => SpriteAnimationSampler.Sample(loop, TimeSpan.Zero, -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => SpriteAnimationSampler.Sample(loop, TimeSpan.Zero, double.NaN));
    }

    [Fact]
    [Trait("SpriteAnimationStage", "1")]
    public void FixedSeedSequencesMatchAnIndependentReferenceSampler()
    {
        SpriteAnimationClip clip = new(
            "Random",
            [Frame(0, 3), Frame(16, 7), Frame(32, 11), Frame(48, 17)],
            isLooping: true);
        Random random = new(0x13A11);
        for (int iteration = 0; iteration < 10_000; iteration++)
        {
            long ticks = random.NextInt64(0, TimeSpan.FromHours(2).Ticks);
            double rate = new[] { 0d, 0.5d, 1d, 2d, 4d }[random.Next(5)];
            TimeSpan elapsed = TimeSpan.FromTicks(ticks);
            int expected = ReferenceFrame(clip, elapsed, rate);
            Assert.Equal(expected, SpriteAnimationSampler.Sample(clip, elapsed, rate).FrameIndex);
        }
    }

    [Fact]
    [Trait("SpriteAnimationStage", "1")]
    public void MillionCycleJumpsAreReducedBeforeFrameSearch()
    {
        SpriteAnimationClip clip = new("Walk", [Frame(0, 1), Frame(16, 2), Frame(32, 3)], true);
        TimeSpan jump = TimeSpan.FromMilliseconds(6_000_001);
        Stopwatch stopwatch = Stopwatch.StartNew();
        for (int iteration = 0; iteration < 10_000; iteration++)
        {
            Assert.Equal(1, SpriteAnimationSampler.Sample(clip, jump, 1).FrameIndex);
        }
        stopwatch.Stop();
        Assert.True(stopwatch.Elapsed < TimeSpan.FromSeconds(5), $"10,000 million-cycle samples took {stopwatch.Elapsed}.");

        SpriteAnimationSample saturatedA = SpriteAnimationSampler.Sample(clip, TimeSpan.MaxValue, double.MaxValue);
        SpriteAnimationSample saturatedB = SpriteAnimationSampler.Sample(clip, TimeSpan.MaxValue, double.MaxValue);
        Assert.Equal(saturatedA, saturatedB);
    }

    private static SpriteAnimationFrame Frame(float sourceX, int milliseconds) =>
        Frame(new DrawRect(sourceX, 0, 16, 16), milliseconds);

    private static SpriteAnimationFrame Frame(DrawRect sourceRect, int milliseconds) =>
        new(sourceRect, TimeSpan.FromMilliseconds(milliseconds));

    private static int ReferenceFrame(SpriteAnimationClip clip, TimeSpan elapsed, double rate)
    {
        long scaled = (long)Math.Floor(elapsed.Ticks * rate);
        long inClip = scaled % clip.Duration.Ticks;
        long cumulative = 0;
        for (int index = 0; index < clip.Frames.Count; index++)
        {
            cumulative += clip.Frames[index].Duration.Ticks;
            if (inClip < cumulative)
            {
                return index;
            }
        }
        throw new InvalidOperationException();
    }
}
