using System.Collections.ObjectModel;
using Cerneala.UI.Accessibility;
using Cerneala.UI.Layout;

namespace Cerneala.UI.Servo;

public sealed class ServoElement
{
    internal ServoElement(
        string typeName,
        string? id,
        string? name,
        SemanticsRole role,
        LayoutRect bounds,
        bool isVisible,
        bool isEnabled,
        bool isFocused,
        string? value,
        IReadOnlyDictionary<SemanticsProperty, object?> properties)
    {
        TypeName = typeName;
        Id = id;
        Name = name;
        Role = role;
        Bounds = bounds;
        IsVisible = isVisible;
        IsEnabled = isEnabled;
        IsFocused = isFocused;
        Value = value;
        Properties = new ReadOnlyDictionary<SemanticsProperty, object?>(
            new Dictionary<SemanticsProperty, object?>(properties));
    }

    public string TypeName { get; }

    public string? Id { get; }

    public string? Name { get; }

    public SemanticsRole Role { get; }

    public LayoutRect Bounds { get; }

    public bool IsVisible { get; }

    public bool IsEnabled { get; }

    public bool IsFocused { get; }

    public string? Value { get; }

    public IReadOnlyDictionary<SemanticsProperty, object?> Properties { get; }
}
