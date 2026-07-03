using System.Collections.ObjectModel;
using Cerneala.UI.Core;
using Cerneala.UI.Elements;

namespace Cerneala.UI.Styling;

public sealed class StyleRule
{
    private readonly List<Setter> setters = [];

    public StyleRule(StyleSelector selector, VisualStateRule? visualState = null, IEnumerable<Setter>? setters = null)
    {
        Selector = selector ?? throw new ArgumentNullException(nameof(selector));
        VisualState = visualState;
        Setters = new ReadOnlyCollection<Setter>(this.setters);
        if (setters is not null)
        {
            foreach (Setter setter in setters)
            {
                Add(setter);
            }
        }
    }

    public StyleSelector Selector { get; }

    public VisualStateRule? VisualState { get; }

    public IReadOnlyList<Setter> Setters { get; }

    public bool IsVisualStateRule => VisualState is not null;

    public UiPropertyValueSource Source => IsVisualStateRule
        ? UiPropertyValueSource.StyleVisualState
        : UiPropertyValueSource.StyleBase;

    public StyleRule Add(Setter setter)
    {
        ArgumentNullException.ThrowIfNull(setter);
        setters.Add(setter);
        return this;
    }

    public bool Matches(UIElement element)
    {
        ArgumentNullException.ThrowIfNull(element);
        return Selector.Matches(element) && (VisualState?.Matches(element) ?? true);
    }
}
