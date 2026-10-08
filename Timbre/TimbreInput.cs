namespace Cerneala.Timbre;

public readonly struct TimbreInput<T>
    where T : struct
{
    public TimbreInput(T value)
    {
        Value = value;
        Parameter = null;
    }

    public TimbreInput(TimbreParameter<T> parameter)
    {
        ArgumentNullException.ThrowIfNull(parameter);
        Parameter = parameter;
        Value = parameter.DefaultValue;
    }

    public bool IsParameter => Parameter is not null;

    public TimbreParameter<T>? Parameter { get; }

    public T Value { get; }

    public static implicit operator TimbreInput<T>(T value) => new(value);

    public static implicit operator TimbreInput<T>(TimbreParameter<T> parameter) => new(parameter);
}
