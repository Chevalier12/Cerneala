using System.Runtime.CompilerServices;

namespace Cerneala.Timbre;

public abstract class TimbreParameter
{
    private protected TimbreParameter(string name, Type valueType)
    {
        ArgumentNullException.ThrowIfNull(name);
        if (!IsIdentifier(name))
        {
            throw new ArgumentException(
                $"Timbre parameter name '{name}' must be an identifier: letters, digits, or underscores, not starting with a digit.",
                nameof(name));
        }

        Name = name;
        ValueType = valueType;
    }

    public string Name { get; }

    public Type ValueType { get; }

    public override string ToString() => Name;

    internal static bool IsIdentifier(string name)
    {
        if (name.Length == 0 || char.IsDigit(name[0]))
        {
            return false;
        }

        foreach (char character in name)
        {
            if (!char.IsLetterOrDigit(character) && character != '_')
            {
                return false;
            }
        }

        return true;
    }
}

public sealed class TimbreParameter<T> : TimbreParameter
    where T : struct
{
    public TimbreParameter(string name, T defaultValue)
        : base(name, typeof(T))
    {
        if (typeof(T) != typeof(float))
        {
            throw new NotSupportedException($"Timbre parameters support float values only; '{typeof(T).Name}' is not supported.");
        }

        if (!float.IsFinite(ToFloat(defaultValue)))
        {
            throw new ArgumentOutOfRangeException(nameof(defaultValue), defaultValue, "A sound parameter default must be finite.");
        }

        DefaultValue = defaultValue;
    }

    public T DefaultValue { get; }

    // TimbreParameter<T> instances exist only for T = float.
    internal static float ToFloat(T value) => Unsafe.As<T, float>(ref value);
}
