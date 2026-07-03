using Cerneala.UI.Animation;
using Cerneala.UI.Core;

namespace Cerneala.UI.Styling;

public sealed class StyleTransition<T>
{
    public StyleTransition(
        UiProperty<T> property,
        TimeSpan duration,
        Func<T, T, float, T> interpolate,
        Func<float, float>? easing = null)
    {
        Property = property ?? throw new ArgumentNullException(nameof(property));
        if (duration <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(duration), "Style transition duration must be positive.");
        }

        Duration = duration;
        Interpolate = interpolate ?? throw new ArgumentNullException(nameof(interpolate));
        Easing = easing ?? Cerneala.UI.Animation.Easing.Linear;
    }

    public UiProperty<T> Property { get; }

    public TimeSpan Duration { get; }

    public Func<T, T, float, T> Interpolate { get; }

    public Func<float, float> Easing { get; }

    public Transition<T> CreateTransition()
    {
        return new Transition<T>(Property, Duration, Interpolate, Easing);
    }
}
