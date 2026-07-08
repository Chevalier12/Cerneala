namespace Cerneala.UI.Controls.Templates;

public sealed class ComponentTemplateDefinition
{
    public ComponentTemplateDefinition(string name, Type ownerType, object? template)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Component template definition name cannot be empty.", nameof(name));
        }

        Name = name;
        OwnerType = ownerType ?? throw new ArgumentNullException(nameof(ownerType));
        Template = template;
    }

    public string Name { get; }

    public Type OwnerType { get; }

    public object? Template { get; }
}
