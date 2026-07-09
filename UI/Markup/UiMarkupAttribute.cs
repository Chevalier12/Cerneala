namespace Cerneala.UI.Markup;

public sealed record UiMarkupAttribute
{
    public UiMarkupAttribute(string name, string value, int? line = null, int? column = null)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Markup attribute name cannot be empty.", nameof(name));
        }

        Name = name;
        Value = value;
        Line = line;
        Column = column;
    }

    public string Name { get; }

    public string Value { get; }

    public int? Line { get; }

    public int? Column { get; }
}
