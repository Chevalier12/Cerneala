using Cerneala.UI.Elements;
using System.Collections.ObjectModel;

namespace Cerneala.UI.Accessibility;

public sealed class SemanticsNode
{
    public SemanticsNode(
        UIElement? element,
        SemanticsRole role,
        string? name = null,
        IReadOnlyDictionary<SemanticsProperty, object?>? properties = null,
        IReadOnlyList<SemanticsNode>? children = null)
    {
        Element = element;
        Role = role;
        Name = string.IsNullOrWhiteSpace(name) ? null : name;
        Properties = new ReadOnlyDictionary<SemanticsProperty, object?>(
            properties is null ? new Dictionary<SemanticsProperty, object?>() : new Dictionary<SemanticsProperty, object?>(properties));
        Children = new ReadOnlyCollection<SemanticsNode>(
            children is null ? [] : children.ToArray());
    }

    public UIElement? Element { get; }

    public SemanticsRole Role { get; }

    public string? Name { get; }

    public IReadOnlyDictionary<SemanticsProperty, object?> Properties { get; }

    public IReadOnlyList<SemanticsNode> Children { get; }

    public T? GetProperty<T>(SemanticsProperty property)
    {
        return Properties.TryGetValue(property, out object? value) && value is T typed
            ? typed
            : default;
    }
}
