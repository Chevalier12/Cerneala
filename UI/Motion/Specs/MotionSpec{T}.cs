using Cerneala.UI.Motion.Interpolation;

namespace Cerneala.UI.Motion.Specs;

public abstract class MotionSpec<T> : MotionSpec
{
    public abstract MotionSampler<T> CreateSampler(T from, T to, ValueMixer<T> mixer, MotionSpecContext context);

    public override MotionSampler CreateSamplerUntyped(
        object? from,
        object? to,
        IValueMixer mixer,
        MotionSpecContext context)
    {
        if (mixer is not ValueMixer<T> typedMixer)
        {
            throw new ArgumentException($"Expected mixer for {typeof(T).Name}.", nameof(mixer));
        }

        return CreateSampler(Cast(from, nameof(from)), Cast(to, nameof(to)), typedMixer, context);
    }

    private static T Cast(object? value, string parameterName)
    {
        if (value is T typed)
        {
            return typed;
        }

        if (value is null && default(T) is null)
        {
            return default!;
        }

        throw new ArgumentException($"Expected value of type {typeof(T).Name}.", parameterName);
    }
}
