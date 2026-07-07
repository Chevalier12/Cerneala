namespace Cerneala.UI.Motion.Interpolation;

public abstract class ValueMixer<T> : IValueMixer
{
    public Type ValueType => typeof(T);

    public virtual bool SupportsVectorOperations => false;

    public abstract T Mix(T from, T to, float progress);

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

    protected static InvalidOperationException CreateVectorException()
    {
        return new InvalidOperationException($"Mixer for {typeof(T).Name} does not support vector operations.");
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
