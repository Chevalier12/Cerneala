using Cerneala.UI.Core;
using Cerneala.UI.Elements;
using Cerneala.UI.Motion.Specs;
using Cerneala.UI.Motion.Transactions;

namespace Cerneala.UI.Motion.Interpolation;

public abstract class ValueMixer<T> : IValueMixer, IValueMixerDispatcher
{
    public Type ValueType => typeof(T);

    public virtual bool SupportsVectorOperations => false;

    public abstract T Mix(T from, T to, float progress);

    public virtual bool EqualsWithinTolerance(T left, T right, float tolerance)
    {
        ThrowIfNegativeTolerance(tolerance);
        return EqualityComparer<T>.Default.Equals(left, right);
    }

    public virtual T Add(T left, T right)
    {
        throw CreateVectorException();
    }

    public virtual T Subtract(T left, T right)
    {
        throw CreateVectorException();
    }

    public virtual T Scale(T value, float scalar)
    {
        throw CreateVectorException();
    }

    public virtual float Magnitude(T value)
    {
        throw CreateVectorException();
    }

    public object? MixUntyped(object? from, object? to, float progress)
    {
        return Mix(Cast(from, nameof(from)), Cast(to, nameof(to)), progress);
    }

    public bool EqualsWithinToleranceUntyped(object? left, object? right, float tolerance)
    {
        return EqualsWithinTolerance(Cast(left, nameof(left)), Cast(right, nameof(right)), tolerance);
    }

    public object? AddUntyped(object? left, object? right)
    {
        return Add(Cast(left, nameof(left)), Cast(right, nameof(right)));
    }

    public object? SubtractUntyped(object? left, object? right)
    {
        return Subtract(Cast(left, nameof(left)), Cast(right, nameof(right)));
    }

    public object? ScaleUntyped(object? value, float scalar)
    {
        return Scale(Cast(value, nameof(value)), scalar);
    }

    public float MagnitudeUntyped(object? value)
    {
        return Magnitude(Cast(value, nameof(value)));
    }

    MotionSampler IValueMixerDispatcher.CreateTweenSampler(
        TimeSpan duration,
        IEasing? easing,
        object? from,
        object? to,
        MotionSpecContext context)
    {
        return new TweenSpec<T>(duration, easing).CreateSampler(
            Cast(from, nameof(from)),
            Cast(to, nameof(to)),
            this,
            context);
    }

    MotionSampler IValueMixerDispatcher.CreateSpringSampler(
        float stiffness,
        float damping,
        float mass,
        object? from,
        object? to,
        MotionSpecContext context)
    {
        return new SpringSpec<T>(stiffness, damping, mass).CreateSampler(
            Cast(from, nameof(from)),
            Cast(to, nameof(to)),
            this,
            context);
    }

    void IValueMixerDispatcher.AnimateMutation(
        MotionTransactionContext context,
        UIElement element,
        UiPropertyMutation mutation,
        MotionSpec spec)
    {
        context.AnimateMutation(element, mutation, this, spec);
    }

    protected static InvalidOperationException CreateVectorException()
    {
        return new InvalidOperationException($"Mixer for {typeof(T).Name} does not support vector operations.");
    }

    protected static float Lerp(float from, float to, float progress)
    {
        if (progress <= 0)
        {
            return from;
        }

        if (progress >= 1)
        {
            return to;
        }

        return from + ((to - from) * progress);
    }

    protected static double Lerp(double from, double to, float progress)
    {
        if (progress <= 0)
        {
            return from;
        }

        if (progress >= 1)
        {
            return to;
        }

        return from + ((to - from) * progress);
    }

    protected static void ThrowIfNegativeTolerance(float tolerance)
    {
        if (!float.IsFinite(tolerance) || tolerance < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(tolerance), "Tolerance must be a finite non-negative value.");
        }
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
