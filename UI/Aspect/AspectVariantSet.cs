namespace Cerneala.UI.Aspect;

public sealed class AspectVariantSet : IEquatable<AspectVariantSet>
{
    private readonly Dictionary<AspectVariantKey, object?> values;

    public static AspectVariantSet Empty { get; } = new([]);

    private AspectVariantSet(Dictionary<AspectVariantKey, object?> values)
    {
        this.values = values;
    }

    public AspectVariantSet Set<TControl, TValue>(AspectVariantKey<TControl, TValue> key, TValue value)
    {
        ArgumentNullException.ThrowIfNull(key);
        Dictionary<AspectVariantKey, object?> next = new(values)
        {
            [key] = value
        };
        return new AspectVariantSet(next);
    }

    public AspectVariantSet Set(AspectVariantKey key, object? value)
    {
        ArgumentNullException.ThrowIfNull(key);
        if (value is not null && !key.ValueType.IsInstanceOfType(value))
        {
            throw new ArgumentException(
                $"Variant '{key.Name}' expects values of type '{key.ValueType.FullName}'.",
                nameof(value));
        }

        Dictionary<AspectVariantKey, object?> next = new(values)
        {
            [key] = value
        };
        return new AspectVariantSet(next);
    }

    public bool TryGet<TControl, TValue>(AspectVariantKey<TControl, TValue> key, out TValue value)
    {
        ArgumentNullException.ThrowIfNull(key);
        if (values.TryGetValue(key, out object? raw))
        {
            if (raw is TValue typed)
            {
                value = typed;
                return true;
            }

            if (raw is null && default(TValue) is null)
            {
                value = default!;
                return true;
            }
        }

        value = default!;
        return false;
    }

    internal bool TryGetUntyped(AspectVariantKey key, out object? value)
    {
        ArgumentNullException.ThrowIfNull(key);
        return values.TryGetValue(key, out value);
    }

    public bool Equals(AspectVariantSet? other)
    {
        if (other is null || values.Count != other.values.Count)
        {
            return false;
        }

        foreach ((AspectVariantKey key, object? value) in values)
        {
            if (!other.values.TryGetValue(key, out object? otherValue) || !Equals(value, otherValue))
            {
                return false;
            }
        }

        return true;
    }

    public override bool Equals(object? obj)
    {
        return obj is AspectVariantSet other && Equals(other);
    }

    public override int GetHashCode()
    {
        HashCode hash = new();
        foreach ((AspectVariantKey key, object? value) in values.OrderBy(pair => pair.Key.ToString(), StringComparer.Ordinal))
        {
            hash.Add(key);
            hash.Add(value);
        }

        return hash.ToHashCode();
    }
}
