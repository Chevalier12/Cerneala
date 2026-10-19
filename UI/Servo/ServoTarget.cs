using Cerneala.UI.Accessibility;

namespace Cerneala.UI.Servo;

public sealed class ServoTarget
{
    private ServoTarget(string? id, string? name, SemanticsRole? role, ServoTarget? scope)
    {
        Id = id;
        Name = name;
        Role = role;
        Scope = scope;
    }

    internal string? Id { get; }

    internal string? Name { get; }

    internal SemanticsRole? Role { get; }

    internal ServoTarget? Scope { get; }

    public static ServoTarget ById(string id)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        return new ServoTarget(id, null, null, null);
    }

    public static ServoTarget ByName(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return new ServoTarget(null, name, null, null);
    }

    public static ServoTarget ByRole(SemanticsRole role)
    {
        if (!Enum.IsDefined(role))
        {
            throw new ArgumentOutOfRangeException(nameof(role));
        }

        return new ServoTarget(null, null, role, null);
    }

    public ServoTarget WithName(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return new ServoTarget(Id, name, Role, Scope);
    }

    public ServoTarget Within(ServoTarget scope)
    {
        ArgumentNullException.ThrowIfNull(scope);
        return new ServoTarget(Id, Name, Role, scope);
    }

    // Example: ByRole(Button).WithName("Save").Within(ById("toolbar")) gives
    // Role=Button, Name="Save" within (Id="toolbar").
    public override string ToString()
    {
        string criteria = string.Join(", ", DescribeOwnCriteria());
        return Scope is null ? criteria : $"{criteria} {DescribeScope()}";
    }

    // The own criteria in display order: Id, Role, Name.
    internal IEnumerable<string> DescribeOwnCriteria()
    {
        if (Id is not null)
        {
            yield return $"Id={Quote(Id)}";
        }

        if (Role is SemanticsRole role)
        {
            yield return $"Role={role}";
        }

        if (Name is not null)
        {
            yield return $"Name={Quote(Name)}";
        }
    }

    internal string DescribeScope() => $"within ({Scope})";

    private static string Quote(string value) =>
        $"\"{value.Replace("\\", "\\\\", StringComparison.Ordinal).Replace("\"", "\\\"", StringComparison.Ordinal)}\"";
}
