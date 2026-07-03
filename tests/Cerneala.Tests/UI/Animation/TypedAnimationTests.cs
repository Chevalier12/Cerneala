using Cerneala.UI.Animation;

namespace Cerneala.Tests.UI.Animation;

public sealed class TypedAnimationTests
{
    [Fact]
    public void TypedAnimationSamplesMidpoint()
    {
        Animation<float> animation = new(0, 10, TimeSpan.FromSeconds(1), Lerp);

        Assert.Equal(5, animation.Sample(0.5f));
    }

    [Fact]
    public void TypedAnimationAppliesEasing()
    {
        Animation<float> animation = new(0, 10, TimeSpan.FromSeconds(1), Lerp, Easing.EaseInQuad);

        Assert.Equal(2.5f, animation.Sample(0.5f));
    }

    [Fact]
    public void EasingClampsProgress()
    {
        Assert.Equal(0, Easing.Linear(-1));
        Assert.Equal(1, Easing.Linear(2));
        Assert.Equal(0, Easing.Linear(float.NaN));
    }

    [Fact]
    public void TypedAnimationRejectsInvalidDuration()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Animation<float>(0, 1, TimeSpan.Zero, Lerp));
    }

    private static float Lerp(float from, float to, float progress)
    {
        return from + ((to - from) * progress);
    }
}
