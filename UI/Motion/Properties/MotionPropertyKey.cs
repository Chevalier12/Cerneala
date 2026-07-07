using Cerneala.UI.Core;
using System.Runtime.CompilerServices;

namespace Cerneala.UI.Motion.Properties;

public sealed class MotionPropertyKey : IEquatable<MotionPropertyKey>
{
    public MotionPropertyKey(UiObject target, UiProperty property)
    {
        Target = target ?? throw new ArgumentNullException(nameof(target));
        Property = property ?? throw new ArgumentNullException(nameof(property));
    }

    public UiObject Target { get; }

    public UiProperty Property { get; }

    public bool Equals(MotionPropertyKey? other)
    {
        return other is not null &&
            ReferenceEquals(Target, other.Target) &&
            ReferenceEquals(Property, other.Property);
    }

    public override bool Equals(object? obj)
    {
        return Equals(obj as MotionPropertyKey);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(RuntimeHelpers.GetHashCode(Target), RuntimeHelpers.GetHashCode(Property));
    }
}
