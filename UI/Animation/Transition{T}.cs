using Cerneala.UI.Core;

namespace Cerneala.UI.Animation;

public sealed class Transition<T> : Transition
{
    private readonly Func<T, T, float, T> interpolate;
    private readonly Func<float, float>? easing;

    public Transition(
        UiProperty<T> property,
        TimeSpan duration,
        Func<T, T, float, T> interpolate,
        Func<float, float>? easing = null)
        : base(property, duration)
    {
        Property = property;
        this.interpolate = interpolate ?? throw new ArgumentNullException(nameof(interpolate));
        this.easing = easing;
    }

    public new UiProperty<T> Property { get; }

    public Animation<T> Create(T from, T to)
    {
        return new Animation<T>(from, to, Duration, interpolate, easing);
    }

    public override Animation CreateUntyped(object? from, object? to)
    {
        T typedFrom = CastValue(from);
        T typedTo = CastValue(to);

        return Create(typedFrom, typedTo);
    }

    private static T CastValue(object? value)
    {
        if (value is T typedValue)
        {
            return typedValue;
        }

        if (value is null && default(T) is null)
        {
            return default!;
        }

        throw new ArgumentException($"Transition values must be assignable to '{typeof(T).FullName}'.");
    }
}
