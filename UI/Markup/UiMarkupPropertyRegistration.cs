using Cerneala.UI.Elements;

namespace Cerneala.UI.Markup;

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
