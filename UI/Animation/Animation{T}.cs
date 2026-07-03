namespace Cerneala.UI.Animation;

public sealed class Animation<T> : Animation
{
    private readonly Func<T, T, float, T> interpolate;

    public Animation(
        T from,
        T to,
        TimeSpan duration,
        Func<T, T, float, T> interpolate,
        Func<float, float>? easing = null)
        : base(duration, easing)
    {
        From = from;
        To = to;
        this.interpolate = interpolate ?? throw new ArgumentNullException(nameof(interpolate));
        CurrentValue = from;
    }

    public T From { get; }

    public T To { get; }

    public T CurrentValue { get; private set; }

    public T Sample(float progress)
    {
        return interpolate(From, To, Easing(progress));
    }

    private protected override void SampleCurrentValue()
    {
        CurrentValue = interpolate(From, To, EasedProgress);
    }
}
