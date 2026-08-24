using Cerneala.UI.Motion.Interpolation;

namespace Cerneala.UI.Motion;

public static class MotionProperty
{
    public static MotionProperty<TTarget, TValue> Create<TTarget, TValue>(
        string name,
        Func<TTarget, TValue> getValue,
        Action<TTarget, TValue> setValue,
        ValueMixer<TValue>? mixer = null)
        where TTarget : class
    {
        return new MotionProperty<TTarget, TValue>(
            name,
            getValue,
            setValue,
            mixer,
            isDiscrete: false);
    }

    public static MotionProperty<TTarget, TValue> CreateDiscrete<TTarget, TValue>(
        string name,
        Func<TTarget, TValue> getValue,
        Action<TTarget, TValue> setValue)
        where TTarget : class
    {
        return new MotionProperty<TTarget, TValue>(
            name,
            getValue,
            setValue,
            mixer: null,
            isDiscrete: true);
    }
}

public sealed class MotionProperty<TTarget, TValue>
    where TTarget : class
{
    internal MotionProperty(
        string name,
        Func<TTarget, TValue> getValue,
        Action<TTarget, TValue> setValue,
        ValueMixer<TValue>? mixer,
        bool isDiscrete)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Name = name;
        GetValue = getValue ?? throw new ArgumentNullException(nameof(getValue));
        SetValue = setValue ?? throw new ArgumentNullException(nameof(setValue));
        Mixer = mixer;
        IsDiscrete = isDiscrete;
    }

    public string Name { get; }

    public bool IsDiscrete { get; }

    internal Func<TTarget, TValue> GetValue { get; }

    internal Action<TTarget, TValue> SetValue { get; }

    internal ValueMixer<TValue>? Mixer { get; }
}
