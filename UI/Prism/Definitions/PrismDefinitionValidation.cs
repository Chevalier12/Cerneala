using System.Collections.Immutable;

namespace Cerneala.UI.Prism.Definitions;

internal static class PrismDefinitionValidation
{
    public static ImmutableArray<T> ToImmutableArray<T>(
        IEnumerable<T>? values,
        string parameterName)
        where T : class
    {
        if (values is null)
        {
            return ImmutableArray<T>.Empty;
        }

        ImmutableArray<T> result = values.ToImmutableArray();
        if (result.Any(value => value is null))
        {
            throw new ArgumentException("Prism definition collections cannot contain null.", parameterName);
        }

        return result;
    }

    public static string? ValidateOptionalName(string? name, string parameterName)
    {
        if (name is null)
        {
            return null;
        }
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Prism node names cannot be empty or whitespace.", parameterName);
        }

        return name;
    }

    public static float UnitInterval(float value, string parameterName)
    {
        if (!float.IsFinite(value) || value < 0f || value > 1f)
        {
            throw new ArgumentOutOfRangeException(parameterName, value, "The value must be finite and between 0 and 1.");
        }

        return value;
    }

    public static float Finite(float value, string parameterName)
    {
        if (!float.IsFinite(value))
        {
            throw new ArgumentOutOfRangeException(parameterName, value, "The value must be finite.");
        }

        return value;
    }

    public static int SequenceHash<T>(IEnumerable<T> values)
    {
        HashCode hash = new();
        foreach (T value in values)
        {
            hash.Add(value);
        }

        return hash.ToHashCode();
    }
}
