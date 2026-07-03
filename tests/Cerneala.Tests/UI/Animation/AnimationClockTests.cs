using Cerneala.UI.Animation;

namespace Cerneala.Tests.UI.Animation;

public sealed class AnimationClockTests
{
    [Fact]
    public void ClockAdvancesByElapsedTime()
    {
        AnimationClock clock = new(TimeSpan.FromSeconds(2));

        clock.Tick(TimeSpan.FromMilliseconds(500));

        Assert.Equal(TimeSpan.FromMilliseconds(500), clock.Elapsed);
        Assert.Equal(0.25f, clock.Progress);
        Assert.False(clock.IsComplete);
    }

    [Fact]
    public void ClockClampsCompletedProgress()
    {
        AnimationClock clock = new(TimeSpan.FromSeconds(1));

        clock.Tick(TimeSpan.FromSeconds(5));

        Assert.Equal(TimeSpan.FromSeconds(1), clock.Elapsed);
        Assert.Equal(1, clock.Progress);
        Assert.True(clock.IsComplete);
    }

    [Fact]
    public void ClockClampsOverflowingTickToDuration()
    {
        AnimationClock clock = new(TimeSpan.FromSeconds(1));
        clock.Tick(TimeSpan.FromMilliseconds(1));

        clock.Tick(TimeSpan.MaxValue);

        Assert.Equal(TimeSpan.FromSeconds(1), clock.Elapsed);
        Assert.Equal(1, clock.Progress);
        Assert.True(clock.IsComplete);
    }

    [Fact]
    public void ClockRejectsInvalidTime()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new AnimationClock(TimeSpan.Zero));
        Assert.Throws<ArgumentOutOfRangeException>(() => new AnimationClock(TimeSpan.FromSeconds(-1)));
        Assert.Throws<ArgumentOutOfRangeException>(() => new AnimationClock(TimeSpan.FromSeconds(1)).Tick(TimeSpan.FromSeconds(-1)));
    }
}
