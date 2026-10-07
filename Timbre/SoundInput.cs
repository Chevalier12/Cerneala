namespace Cerneala.Timbre;

public readonly struct SoundInput<T>
    where T : struct
{
    public SoundInput(T value)
    {
        Value = value;
        Parameter = null;
    }

    public SoundInput(SoundParameter<T> parameter)
    {
        ArgumentNullException.ThrowIfNull(parameter);
        Parameter = parameter;
        Value = parameter.DefaultValue;
    }

    public bool IsParameter => Parameter is not null;

    public SoundParameter<T>? Parameter { get; }

    public T Value { get; }

    public static implicit operator SoundInput<T>(T value) => new(value);

    public static implicit operator SoundInput<T>(SoundParameter<T> parameter) => new(parameter);
}
