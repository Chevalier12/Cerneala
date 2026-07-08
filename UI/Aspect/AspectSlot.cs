namespace Cerneala.UI.Aspect;

public abstract class AspectSlot : IEquatable<AspectSlot>
{
    private protected AspectSlot(string name, Type ownerType, Type targetType)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Aspect slot name cannot be empty.", nameof(name));
        }

        Name = name;
        OwnerType = ownerType ?? throw new ArgumentNullException(nameof(ownerType));
        TargetType = targetType ?? throw new ArgumentNullException(nameof(targetType));
    }

    public string Name { get; }

    public Type OwnerType { get; }

    public Type TargetType { get; }

    public static AspectSlot<TOwner, TTarget> For<TOwner, TTarget>(string name)
    {
        return new AspectSlot<TOwner, TTarget>(name);
    }

    public static AspectSlot<TOwner, TOwner> Root<TOwner>()
    {
        return new AspectSlot<TOwner, TOwner>("Root");
    }

    public bool Equals(AspectSlot? other)
    {
        return other is not null &&
            string.Equals(Name, other.Name, StringComparison.Ordinal) &&
            OwnerType == other.OwnerType &&
            TargetType == other.TargetType;
    }

    public override bool Equals(object? obj)
    {
        return obj is AspectSlot other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(StringComparer.Ordinal.GetHashCode(Name), OwnerType, TargetType);
    }

    public override string ToString()
    {
        return $"{OwnerType.Name}.{Name}";
    }
}
