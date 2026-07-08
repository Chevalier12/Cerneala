namespace Cerneala.UI.Aspect;

public sealed class AspectLayer : IEquatable<AspectLayer>, IComparable<AspectLayer>
{
    public static AspectLayer Reset { get; } = new("Reset", 0);

    public static AspectLayer Theme { get; } = new("Theme", 100);

    public static AspectLayer Component { get; } = new("Component", 200);

    public static AspectLayer App { get; } = new("App", 300);

    public static AspectLayer User { get; } = new("User", 400);

    public static AspectLayer Runtime { get; } = new("Runtime", 500);

    public AspectLayer(string name, int order)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Aspect layer name cannot be empty.", nameof(name));
        }

        Name = name;
        Order = order;
    }

    public string Name { get; }

    public int Order { get; }

    public int CompareTo(AspectLayer? other)
    {
        return other is null ? 1 : Order.CompareTo(other.Order);
    }

    public bool Equals(AspectLayer? other)
    {
        return other is not null && Order == other.Order && string.Equals(Name, other.Name, StringComparison.Ordinal);
    }

    public override bool Equals(object? obj)
    {
        return obj is AspectLayer other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(StringComparer.Ordinal.GetHashCode(Name), Order);
    }

    public override string ToString()
    {
        return $"{Name}:{Order}";
    }
}
