using Cerneala.UI.Animation;
using Cerneala.UI.Core;
using Cerneala.UI.Styling;
using RuntimeAnimation = Cerneala.UI.Animation.Animation;

namespace Cerneala.Tests.UI.Animation;

public sealed class TransitionTests
{
    [Fact]
    public void TransitionCreatesTypedAnimation()
    {
        UiProperty<float> property = RegisterFloat();
        Transition<float> transition = new(property, TimeSpan.FromSeconds(1), Lerp);

        Animation<float> animation = transition.Create(0, 10);

        Assert.Equal(5, animation.Sample(0.5f));
        Assert.Same(property, transition.Property);
    }

    [Fact]
    public void TransitionRejectsMismatchedUntypedValues()
    {
        UiProperty<float> property = RegisterFloat();
        Transition<float> transition = new(property, TimeSpan.FromSeconds(1), Lerp);

        Assert.Throws<ArgumentException>(() => transition.CreateUntyped("bad", 10f));
    }

    [Fact]
    public void TransitionRejectsNullUntypedValuesForValueProperties()
    {
        UiProperty<float> property = RegisterFloat();
        Transition<float> transition = new(property, TimeSpan.FromSeconds(1), Lerp);

        Assert.Throws<ArgumentException>(() => transition.CreateUntyped(null, 10f));
        Assert.Throws<ArgumentException>(() => transition.CreateUntyped(0f, null));
    }

    [Fact]
    public void TransitionAllowsNullUntypedValuesForReferenceProperties()
    {
        UiProperty<string?> property = UiProperty<string?>.Register(
            $"{nameof(TransitionTests)}_{Guid.NewGuid():N}",
            typeof(TransitionTests),
            new UiPropertyMetadata<string?>(null));
        Transition<string?> transition = new(
            property,
            TimeSpan.FromSeconds(1),
            (from, to, progress) => progress < 1f ? from : to);

        RuntimeAnimation animation = transition.CreateUntyped(null, "shown");

        Animation<string?> typedAnimation = Assert.IsType<Animation<string?>>(animation);
        Assert.Null(typedAnimation.From);
        Assert.Equal("shown", typedAnimation.To);
    }

    [Fact]
    public void TransitionConstructorsRejectNullArguments()
    {
        UiProperty<float> property = RegisterFloat();

        Assert.Throws<ArgumentNullException>(() => new Transition<float>(null!, TimeSpan.FromSeconds(1), Lerp));
        Assert.Throws<ArgumentNullException>(() => new Transition<float>(property, TimeSpan.FromSeconds(1), null!));
    }

    [Fact]
    public void StoryboardStopsContainedAnimations()
    {
        UiProperty<float> property = RegisterFloat();
        UiObject target = new();
        AnimationScheduler scheduler = new();
        AnimationScheduler.AnimationHandle handle = scheduler.Schedule(target, property, new Animation<float>(0, 10, TimeSpan.FromSeconds(1), Lerp));
        Storyboard storyboard = new();
        storyboard.Add(handle);

        storyboard.Stop();
        scheduler.Tick(TimeSpan.FromMilliseconds(1));

        Assert.False(scheduler.HasActiveAnimations);
    }

    [Fact]
    public void StoryboardHandlesCannotMutateContainedAnimations()
    {
        UiProperty<float> property = RegisterFloat();
        UiObject target = new();
        AnimationScheduler scheduler = new();
        AnimationScheduler.AnimationHandle handle = scheduler.Schedule(target, property, new Animation<float>(0, 10, TimeSpan.FromSeconds(1), Lerp));
        Storyboard storyboard = new();
        storyboard.Add(handle);

        Assert.Throws<NotSupportedException>(() => ((ICollection<AnimationScheduler.AnimationHandle>)storyboard.Handles).Clear());

        storyboard.Stop();
        scheduler.Tick(TimeSpan.FromMilliseconds(1));

        Assert.False(scheduler.HasActiveAnimations);
    }

    [Fact]
    public void StoryboardRejectsNullHandles()
    {
        Storyboard storyboard = new();

        Assert.Throws<ArgumentNullException>(() => storyboard.Add(null!));
    }

    [Fact]
    public void StyleTransitionCreatesRuntimeTransition()
    {
        UiProperty<float> property = RegisterFloat();
        StyleTransition<float> styleTransition = new(property, TimeSpan.FromSeconds(1), Lerp);

        Transition<float> transition = styleTransition.CreateTransition();

        Assert.Same(property, transition.Property);
        Assert.Equal(5, transition.Create(0, 10).Sample(0.5f));
    }

    [Fact]
    public void StyleTransitionConstructorRejectsNullArguments()
    {
        UiProperty<float> property = RegisterFloat();

        Assert.Throws<ArgumentNullException>(() => new StyleTransition<float>(null!, TimeSpan.FromSeconds(1), Lerp));
        Assert.Throws<ArgumentNullException>(() => new StyleTransition<float>(property, TimeSpan.FromSeconds(1), null!));
    }

    private static UiProperty<float> RegisterFloat()
    {
        return UiProperty<float>.Register(
            $"{nameof(TransitionTests)}_{Guid.NewGuid():N}",
            typeof(TransitionTests),
            new UiPropertyMetadata<float>(0));
    }

    private static float Lerp(float from, float to, float progress)
    {
        return from + ((to - from) * progress);
    }
}
