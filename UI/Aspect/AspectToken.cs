using Cerneala.Drawing;
using Cerneala.UI.Layout;
using Cerneala.UI.Motion.Specs;

namespace Cerneala.UI.Aspect;

public abstract class AspectToken : IEquatable<AspectToken>
{
    private protected AspectToken(string name, Type valueType)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Aspect token name cannot be empty.", nameof(name));
        }

        Name = name;
        ValueType = valueType ?? throw new ArgumentNullException(nameof(valueType));
    }

    public string Name { get; }

    public Type ValueType { get; }

    public static AspectToken<T> Create<T>(string name)
    {
        return new AspectToken<T>(name);
    }

    public static AspectToken<DrawColor> Color(string name)
    {
        return Create<DrawColor>(name);
    }

    public static AspectToken<Thickness> Thickness(string name)
    {
        return Create<Thickness>(name);
    }

    public static AspectToken<float> Float(string name)
    {
        return Create<float>(name);
    }

    public static AspectToken<string> String(string name)
    {
        return Create<string>(name);
    }

    public static AspectToken<MotionSpec> Motion(string name)
    {
        return Create<MotionSpec>(name);
    }

    public bool Equals(AspectToken? other)
    {
        return other is not null &&
            string.Equals(Name, other.Name, StringComparison.Ordinal) &&
            ValueType == other.ValueType;
    }

    public override bool Equals(object? obj)
    {
        return obj is AspectToken other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(StringComparer.Ordinal.GetHashCode(Name), ValueType);
    }

    public override string ToString()
    {
        return $"{ValueType.FullName}:{Name}";
    }
}
