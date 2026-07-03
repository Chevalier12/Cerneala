using Cerneala.UI.Elements;

namespace Cerneala.UI.Styling;

public sealed class VisualStateRule
{
    private const PseudoClass AllPseudoClasses =
        PseudoClass.Hover |
        PseudoClass.Pressed |
        PseudoClass.Focus |
        PseudoClass.FocusWithin |
        PseudoClass.Disabled |
        PseudoClass.Selected;

    public VisualStateRule(PseudoClass pseudoClasses)
    {
        if ((pseudoClasses & ~AllPseudoClasses) != PseudoClass.None)
        {
            throw new ArgumentOutOfRangeException(nameof(pseudoClasses), "Unknown pseudo class value.");
        }

        PseudoClasses = pseudoClasses;
    }

    public PseudoClass PseudoClasses { get; }

    public bool Matches(UIElement element)
    {
        return PseudoClassMatcher.Matches(element, PseudoClasses);
    }
}
