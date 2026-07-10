namespace Cerneala.UI.Markup;

public sealed class MarkupAspectResource
{
    public MarkupAspectResource(
        string? name,
        Type targetType,
        IReadOnlyList<string> defaultPropertyNames,
        bool isConditional)
    {
        Name = string.IsNullOrWhiteSpace(name) ? null : name;
        TargetType = targetType ?? throw new ArgumentNullException(nameof(targetType));
        DefaultPropertyNames = defaultPropertyNames?.ToArray() ?? throw new ArgumentNullException(nameof(defaultPropertyNames));
        IsConditional = isConditional;
    }

    public string? Name { get; }

    public Type TargetType { get; }

    public IReadOnlyList<string> DefaultPropertyNames { get; }

    public bool IsConditional { get; }
}
