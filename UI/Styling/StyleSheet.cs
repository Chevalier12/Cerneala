using System.Collections.ObjectModel;

namespace Cerneala.UI.Styling;

public sealed class StyleSheet
{
    private readonly List<StyleRule> rules = [];

    public StyleSheet(IEnumerable<StyleRule>? rules = null)
    {
        Rules = new ReadOnlyCollection<StyleRule>(this.rules);
        if (rules is not null)
        {
            foreach (StyleRule rule in rules)
            {
                Add(rule);
            }
        }
    }

    public IReadOnlyList<StyleRule> Rules { get; }

    public StyleSheet Add(StyleRule rule)
    {
        ArgumentNullException.ThrowIfNull(rule);
        rules.Add(rule);
        return this;
    }
}
