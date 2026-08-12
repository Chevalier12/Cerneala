namespace Cerneala.UI.Markup;

public sealed class MarkupAspectResource
{
    private readonly System.Runtime.CompilerServices.ConditionalWeakTable<Cerneala.UI.Elements.UIElement, object> appliedElements = new();
    private readonly Action<Cerneala.UI.Elements.UIElement>? apply;

    public MarkupAspectResource(
        string? name,
        Type targetType,
        IReadOnlyList<string> defaultPropertyNames,
        bool isConditional)
        : this(name, targetType, defaultPropertyNames, isConditional, null)
    {
    }

    public MarkupAspectResource(
        string? name,
        Type targetType,
        IReadOnlyList<string> defaultPropertyNames,
        bool isConditional,
        Action<Cerneala.UI.Elements.UIElement>? apply)
    {
        Name = string.IsNullOrWhiteSpace(name) ? null : name;
        TargetType = targetType ?? throw new ArgumentNullException(nameof(targetType));
        DefaultPropertyNames = defaultPropertyNames?.ToArray() ?? throw new ArgumentNullException(nameof(defaultPropertyNames));
        IsConditional = isConditional;
        this.apply = apply;
    }

    public string? Name { get; }

    public Type TargetType { get; }

    public IReadOnlyList<string> DefaultPropertyNames { get; }

    public bool IsConditional { get; }

    public void ApplyTo(Cerneala.UI.Elements.UIElement element)
    {
        ArgumentNullException.ThrowIfNull(element);
        if (apply is null || !TargetType.IsInstanceOfType(element))
        {
            return;
        }

        if (appliedElements.TryGetValue(element, out _))
        {
            return;
        }

        appliedElements.Add(element, new object());
        try
        {
            apply(element);
        }
        catch
        {
            appliedElements.Remove(element);
            throw;
        }
    }
}
