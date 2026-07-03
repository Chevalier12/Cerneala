namespace Cerneala.UI.Controls;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
public sealed class TemplatePartAttribute : Attribute
{
    public TemplatePartAttribute(string name, Type type)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Template part name cannot be empty.", nameof(name));
        }

        Name = name;
        Type = type ?? throw new ArgumentNullException(nameof(type));
    }

    public string Name { get; }

    public Type Type { get; }

    public static IReadOnlyList<TemplatePartAttribute> GetParts(Type controlType)
    {
        ArgumentNullException.ThrowIfNull(controlType);
        return Attribute.GetCustomAttributes(controlType, typeof(TemplatePartAttribute), inherit: true)
            .Cast<TemplatePartAttribute>()
            .ToArray();
    }
}
