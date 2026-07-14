using Cerneala.UI.Elements;

namespace Cerneala.UI.Controls.Templates;

public sealed class TemplatePartMap
{
    private readonly Dictionary<string, UIElement> parts = new(StringComparer.Ordinal);

    public UIElement this[string name] => parts[name];

    public bool TryGetValue(string name, out UIElement? element)
    {
        return parts.TryGetValue(name, out element);
    }

    public void Register(string name, UIElement element)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Template part name cannot be empty.", nameof(name));
        }

        ArgumentNullException.ThrowIfNull(element);
        if (!parts.TryAdd(name, element))
        {
            throw new InvalidOperationException($"Template part '{name}' was registered more than once.");
        }
    }
}
