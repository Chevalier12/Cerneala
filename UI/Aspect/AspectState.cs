namespace Cerneala.UI.Aspect;

public sealed class AspectState : IEquatable<AspectState>
{
    private AspectState(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Aspect state name cannot be empty.", nameof(name));
        }

        Name = name;
    }

    public static AspectState Hover { get; } = new("hover");

    public static AspectState Pressed { get; } = new("pressed");

    public static AspectState Focus { get; } = new("focus");

    public static AspectState FocusWithin { get; } = new("focus-within");

    public static AspectState Disabled { get; } = new("disabled");

    public static AspectState Selected { get; } = new("selected");

    public static AspectState Checked { get; } = new("checked");

    public static AspectState Expanded { get; } = new("expanded");

    public string Name { get; }

    public static AspectState Create(string name)
    {
        return new AspectState(name);
    }

    public bool Equals(AspectState? other)
    {
        return other is not null && string.Equals(Name, other.Name, StringComparison.Ordinal);
    }

    public override bool Equals(object? obj)
    {
        return obj is AspectState other && Equals(other);
    }

    public override int GetHashCode()
    {
        return StringComparer.Ordinal.GetHashCode(Name);
    }

    public override string ToString()
    {
        return Name;
    }
}
