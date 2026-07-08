namespace Cerneala.UI.Aspect;

public abstract class AspectVariantKey : IEquatable<AspectVariantKey>
{
    private protected AspectVariantKey(string name, Type ownerType, Type valueType)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Aspect variant name cannot be empty.", nameof(name));
        }

        Name = name;
        OwnerType = ownerType ?? throw new ArgumentNullException(nameof(ownerType));
        ValueType = valueType ?? throw new ArgumentNullException(nameof(valueType));
    }

    public string Name { get; }

    public Type OwnerType { get; }

    public Type ValueType { get; }

    public static AspectVariantKey<TOwner, TValue> For<TOwner, TValue>(string name)
    {
        return new AspectVariantKey<TOwner, TValue>(name);
    }

    public bool Equals(AspectVariantKey? other)
    {
        return other is not null &&
            string.Equals(Name, other.Name, StringComparison.Ordinal) &&
            OwnerType == other.OwnerType &&
            ValueType == other.ValueType;
    }

    public override bool Equals(object? obj)
    {
        return obj is AspectVariantKey other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(StringComparer.Ordinal.GetHashCode(Name), OwnerType, ValueType);
    }

    public override string ToString()
    {
        return $"{OwnerType.Name}.{Name}";
    }
}
