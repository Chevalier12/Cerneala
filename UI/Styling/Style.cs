using System.Collections.ObjectModel;

namespace Cerneala.UI.Styling;

public sealed class Style
{
    private readonly List<StyleRule> rules = [];

    public Style(string? name = null)
    {
        if (name is not null && string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Style name cannot be empty.", nameof(name));
        }

        Name = name;
        Rules = new ReadOnlyCollection<StyleRule>(rules);
    }

    public string? Name { get; }

    public IReadOnlyList<StyleRule> Rules { get; }

    public Style Add(StyleRule rule)
    {
        ArgumentNullException.ThrowIfNull(rule);
        rules.Add(rule);
        return this;
    }
}
