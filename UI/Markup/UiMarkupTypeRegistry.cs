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
