namespace Cerneala.UI.Animation;

public abstract class Animation
{
    private protected Animation(TimeSpan duration, Func<float, float>? easing = null)
    {
        Clock = new AnimationClock(duration);
        Easing = easing ?? Cerneala.UI.Animation.Easing.Linear;
    }

    public AnimationClock Clock { get; }

    public Func<float, float> Easing { get; }

    public bool IsComplete => Clock.IsComplete;

    public float Progress => Clock.Progress;

    public float EasedProgress => Easing(Progress);

    public void Tick(TimeSpan elapsed)
    {
        Clock.Tick(elapsed);
        SampleCurrentValue();
    }

    private protected abstract void SampleCurrentValue();
}
