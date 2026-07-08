using Cerneala.UI.Elements;

namespace Cerneala.UI.Controls.Templates;

public sealed class TemplatePartMap
{
    private readonly Dictionary<string, UIElement> parts = new(StringComparer.Ordinal);

    public UIElement this[string name] => parts[name];

    public void Register(string name, UIElement element)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Template part name cannot be empty.", nameof(name));
        }

        parts[name] = element ?? throw new ArgumentNullException(nameof(element));
    }
}
