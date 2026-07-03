using Cerneala.UI.Elements;

namespace Cerneala.UI.Markup;

public sealed class UiMarkupTypeRegistry
{
    private readonly Dictionary<string, UiMarkupElementRegistration> elements = new(StringComparer.Ordinal);

    public UiMarkupTypeRegistry Register(UiMarkupElementRegistration registration)
    {
        ArgumentNullException.ThrowIfNull(registration);
        if (!elements.TryAdd(registration.Name, registration))
        {
            throw new InvalidOperationException($"Markup element '{registration.Name}' is already registered.");
        }

        return this;
    }

    public bool TryGetElement(string name, out UiMarkupElementRegistration registration)
    {
        return elements.TryGetValue(name, out registration!);
    }
}

public sealed class UiMarkupElementRegistration
{
    private readonly Dictionary<string, UiMarkupPropertyRegistration> properties = new(StringComparer.Ordinal);

    public UiMarkupElementRegistration(
        string name,
        Func<UIElement> factory,
        Action<UIElement, UIElement>? addChild = null,
        string? contentPropertyName = null)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Markup element name cannot be empty.", nameof(name));
        }

        Name = name;
        Factory = factory ?? throw new ArgumentNullException(nameof(factory));
        AddChild = addChild;
        ContentPropertyName = contentPropertyName;
    }

    public string Name { get; }

    public Func<UIElement> Factory { get; }

    public Action<UIElement, UIElement>? AddChild { get; }

    public string? ContentPropertyName { get; }

    public IReadOnlyDictionary<string, UiMarkupPropertyRegistration> Properties => properties;

    public UiMarkupElementRegistration RegisterProperty(UiMarkupPropertyRegistration property)
    {
        ArgumentNullException.ThrowIfNull(property);
        if (!properties.TryAdd(property.Name, property))
        {
            throw new InvalidOperationException($"Markup property '{Name}.{property.Name}' is already registered.");
        }

        return this;
    }

    public bool TryGetProperty(string name, out UiMarkupPropertyRegistration property)
    {
        return properties.TryGetValue(name, out property!);
    }
}

public sealed class UiMarkupPropertyRegistration
{
    public UiMarkupPropertyRegistration(string name, Action<UIElement, string> setValue)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Markup property name cannot be empty.", nameof(name));
        }

        Name = name;
        SetValue = setValue ?? throw new ArgumentNullException(nameof(setValue));
    }

    public string Name { get; }

    public Action<UIElement, string> SetValue { get; }
}
