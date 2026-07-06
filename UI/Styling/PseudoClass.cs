using Cerneala.UI.Controls;
using Cerneala.UI.Controls.Primitives;
using Cerneala.UI.Elements;

namespace Cerneala.UI.Styling;

[Flags]
public enum PseudoClass
{
    None = 0,
    Hover = 1 << 0,
    Pressed = 1 << 1,
    Focus = 1 << 2,
    FocusWithin = 1 << 3,
    Disabled = 1 << 4,
    Selected = 1 << 5
}

public static class PseudoClassMatcher
{
    public static bool Matches(UIElement element, PseudoClass pseudoClasses)
    {
        ArgumentNullException.ThrowIfNull(element);

        if (pseudoClasses == PseudoClass.None)
        {
            return true;
        }

        if (element is IStylePseudoClassProvider provider)
        {
            return MatchesProvider(provider, pseudoClasses);
        }

        return (!pseudoClasses.HasFlag(PseudoClass.Hover) || element.IsPointerOver) &&
            (!pseudoClasses.HasFlag(PseudoClass.Pressed) || (element is ButtonBase { IsPressed: true })) &&
            (!pseudoClasses.HasFlag(PseudoClass.Focus) || element.IsKeyboardFocused) &&
            (!pseudoClasses.HasFlag(PseudoClass.FocusWithin) || element.IsKeyboardFocusWithin) &&
            (!pseudoClasses.HasFlag(PseudoClass.Disabled) || !element.IsEnabled) &&
            (!pseudoClasses.HasFlag(PseudoClass.Selected) ||
                (element is ISelectableItemContainer { IsSelected: true }));
    }

    private static bool MatchesProvider(IStylePseudoClassProvider provider, PseudoClass pseudoClasses)
    {
        foreach (PseudoClass pseudoClass in Enum.GetValues<PseudoClass>())
        {
            if (pseudoClass == PseudoClass.None || !pseudoClasses.HasFlag(pseudoClass))
            {
                continue;
            }

            if (!provider.IsPseudoClassActive(pseudoClass))
            {
                return false;
            }
        }

        return true;
    }
}

public interface IStylePseudoClassProvider
{
    bool IsPseudoClassActive(PseudoClass pseudoClass);
}
