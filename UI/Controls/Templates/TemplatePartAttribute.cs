namespace Cerneala.UI.Controls.Templates;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
public sealed class TemplatePartAttribute : Attribute
{
    private static readonly object SyncRoot = new();
    private static readonly Dictionary<Type, List<TemplatePartAttribute>> RegisteredParts = [];

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

    public static void Register<TControl>(string name, Type partType)
    {
        Register(typeof(TControl), name, partType);
    }

    public static void Register(Type controlType, string name, Type partType)
    {
        ArgumentNullException.ThrowIfNull(controlType);
        TemplatePartAttribute part = new(name, partType);
        lock (SyncRoot)
        {
            if (!RegisteredParts.TryGetValue(controlType, out List<TemplatePartAttribute>? parts))
            {
                parts = [];
                RegisteredParts[controlType] = parts;
            }

            parts.RemoveAll(existing => existing.Name == part.Name);
            parts.Add(part);
        }
    }

    public static IReadOnlyList<TemplatePartAttribute> GetParts(Type controlType)
    {
        ArgumentNullException.ThrowIfNull(controlType);
        List<TemplatePartAttribute> result = [];
        lock (SyncRoot)
        {
            for (Type? current = controlType; current is not null; current = current.BaseType)
            {
                if (RegisteredParts.TryGetValue(current, out List<TemplatePartAttribute>? parts))
                {
                    result.AddRange(parts);
                }
            }
        }

        return result;
    }
}
