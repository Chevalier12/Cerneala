namespace Cerneala.UI.Controls.Templates;

public sealed class ContentTemplateDefinition
{
    public ContentTemplateDefinition(string name, Type? dataType, string? key, object? template)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Content template definition name cannot be empty.", nameof(name));
        }

        Name = name;
        DataType = dataType;
        Key = key;
        Template = template;
    }

    public string Name { get; }

    public Type? DataType { get; }

    public string? Key { get; }

    public object? Template { get; }
}
