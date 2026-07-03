using Cerneala.UI.Animation;
using Cerneala.UI.Core;

namespace Cerneala.Tests.UI.Animation;

public sealed class AnimationSchedulerTests
{
    [Fact]
    public void SchedulerAppliesTickedValueThroughAnimationSource()
    {
        UiProperty<float> property = RegisterFloat();
        UiObject target = new();
        AnimationScheduler scheduler = new();

        scheduler.Schedule(target, property, new Animation<float>(0, 10, TimeSpan.FromSeconds(1), Lerp));
        AnimationTickResult result = scheduler.Tick(TimeSpan.FromMilliseconds(500));

        Assert.Equal(5, target.GetValue(property));
        Assert.Equal(UiPropertyValueSource.Animation, target.GetValueSource(property));
        Assert.True(result.HasPendingWork);
    }

    [Fact]
    public void CompletedAnimationClearsAnimationSource()
    {
        UiProperty<float> property = RegisterFloat();
        UiObject target = new();
        target.SetValue(property, 2, UiPropertyValueSource.StyleBase);
        AnimationScheduler scheduler = new();

        scheduler.Schedule(target, property, new Animation<float>(0, 10, TimeSpan.FromSeconds(1), Lerp));
        AnimationTickResult result = scheduler.Tick(TimeSpan.FromSeconds(1));

        Assert.Equal(2, target.GetValue(property));
        Assert.Equal(UiPropertyValueSource.StyleBase, target.GetValueSource(property));
        Assert.False(result.HasPendingWork);
        Assert.Equal(1, result.Completed);
    }

    [Fact]
    public void LocalValueOverridesAnimatedValue()
    {
        UiProperty<float> property = RegisterFloat();
        UiObject target = new();
        target.SetValue(property, 42);
        AnimationScheduler scheduler = new();

        scheduler.Schedule(target, property, new Animation<float>(0, 10, TimeSpan.FromSeconds(1), Lerp));
        scheduler.Tick(TimeSpan.FromMilliseconds(500));

        Assert.Equal(42, target.GetValue(property));
        Assert.Equal(UiPropertyValueSource.Local, target.GetValueSource(property));
    }

    [Fact]
    public void AnimationValueOverridesStyleValuesUntilLocalValueMasksIt()
    {
        UiProperty<float> property = RegisterFloat();
        UiObject target = new();
        target.SetValue(property, 1, UiPropertyValueSource.StyleBase);
        target.SetValue(property, 2, UiPropertyValueSource.StyleVisualState);
        AnimationScheduler scheduler = new();

        scheduler.Schedule(target, property, new Animation<float>(0, 10, TimeSpan.FromSeconds(1), Lerp));
        scheduler.Tick(TimeSpan.FromMilliseconds(500));

        Assert.Equal(5, target.GetValue(property));
        Assert.Equal(UiPropertyValueSource.Animation, target.GetValueSource(property));

        target.SetValue(property, 42);

        Assert.Equal(42, target.GetValue(property));
        Assert.Equal(UiPropertyValueSource.Local, target.GetValueSource(property));

        target.ClearValue(property);

        Assert.Equal(5, target.GetValue(property));
        Assert.Equal(UiPropertyValueSource.Animation, target.GetValueSource(property));
    }

    [Fact]
    public void StoppedAnimationClearsAnimationSource()
    {
        UiProperty<float> property = RegisterFloat();
        UiObject target = new();
        AnimationScheduler scheduler = new();
        AnimationScheduler.AnimationHandle handle = scheduler.Schedule(target, property, new Animation<float>(0, 10, TimeSpan.FromSeconds(1), Lerp));
        scheduler.Tick(TimeSpan.FromMilliseconds(500));

        handle.Stop();
        scheduler.Tick(TimeSpan.FromMilliseconds(1));

        Assert.Equal(0, target.GetValue(property));
        Assert.Equal(UiPropertyValueSource.Default, target.GetValueSource(property));
        Assert.False(scheduler.HasActiveAnimations);
    }

    [Fact]
    public void SchedulingSamePropertyReplacesPreviousAnimation()
    {
        UiProperty<float> property = RegisterFloat();
        UiObject target = new();
        AnimationScheduler scheduler = new();

        scheduler.Schedule(target, property, new Animation<float>(0, 10, TimeSpan.FromSeconds(1), Lerp));
        scheduler.Tick(TimeSpan.FromMilliseconds(500));
        scheduler.Schedule(target, property, new Animation<float>(100, 200, TimeSpan.FromSeconds(1), Lerp));
        AnimationTickResult result = scheduler.Tick(TimeSpan.FromMilliseconds(100));

        Assert.Equal(110, target.GetValue(property));
        Assert.Equal(1, result.Ticked);
        Assert.True(result.HasPendingWork);
    }

    [Fact]
    public void TickWithoutAnimationsReportsNoWork()
    {
        AnimationScheduler scheduler = new();

        AnimationTickResult result = scheduler.Tick(TimeSpan.FromMilliseconds(16));

        Assert.Equal(0, result.Ticked);
        Assert.Equal(0, result.Completed);
        Assert.False(result.HasPendingWork);
    }

    [Fact]
    public void CompletedResultCountsAllAnimationsCompletedDuringTick()
    {
        UiProperty<float> first = RegisterFloat();
        UiProperty<float> second = RegisterFloat();
        UiObject target = new();
        AnimationScheduler scheduler = new();

        scheduler.Schedule(target, first, new Animation<float>(0, 10, TimeSpan.FromSeconds(1), Lerp));
        scheduler.Schedule(target, second, new Animation<float>(20, 30, TimeSpan.FromSeconds(1), Lerp));

        AnimationTickResult result = scheduler.Tick(TimeSpan.FromSeconds(1));

        Assert.Equal(2, result.Ticked);
        Assert.Equal(2, result.Completed);
        Assert.False(result.HasPendingWork);
        Assert.False(scheduler.HasActiveAnimations);
    }

    [Fact]
    public void SchedulerRejectsNullArguments()
    {
        UiProperty<float> property = RegisterFloat();
        UiObject target = new();
        Animation<float> animation = new(0, 10, TimeSpan.FromSeconds(1), Lerp);
        AnimationScheduler scheduler = new();

        Assert.Throws<ArgumentNullException>(() => scheduler.Schedule(null!, property, animation));
        Assert.Throws<ArgumentNullException>(() => scheduler.Schedule(target, null!, animation));
        Assert.Throws<ArgumentNullException>(() => scheduler.Schedule(target, property, null!));
    }

    private static UiProperty<float> RegisterFloat()
    {
        return UiProperty<float>.Register(
            $"{nameof(AnimationSchedulerTests)}_{Guid.NewGuid():N}",
            typeof(AnimationSchedulerTests),
            new UiPropertyMetadata<float>(0));
    }

    private static float Lerp(float from, float to, float progress)
    {
        return from + ((to - from) * progress);
    }
}
