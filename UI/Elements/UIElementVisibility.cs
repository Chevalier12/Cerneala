using Cerneala.UI.Layout;

namespace Cerneala.UI.Elements;

public static class UIElementVisibility
{
    public static bool ParticipatesInLayout(UIElement element)
    {
        ArgumentNullException.ThrowIfNull(element);
        return element.Visibility != Visibility.Collapsed;
    }

    public static bool ParticipatesInRendering(UIElement element)
    {
        ArgumentNullException.ThrowIfNull(element);
        return element.IsVisible && element.Visibility == Visibility.Visible;
    }

    public static bool ParticipatesInInput(UIElement element)
    {
        ArgumentNullException.ThrowIfNull(element);
        return !element.IsPresenceExiting && element.IsVisible && element.Visibility == Visibility.Visible;
    }

    public static bool ParticipatesInHitTest(UIElement element)
    {
        ArgumentNullException.ThrowIfNull(element);
        return !element.IsPresenceExiting && element.IsVisible && element.Visibility == Visibility.Visible;
    }

    internal static bool IsEffectivelyVisible(UIElement element)
    {
        ArgumentNullException.ThrowIfNull(element);
        for (UIElement? current = element; current is not null; current = current.VisualParent)
        {
            if (!ParticipatesInRendering(current))
            {
                return false;
            }
        }

        return true;
    }

    internal static bool IsEffectivelyParticipatingInLayout(UIElement element)
    {
        ArgumentNullException.ThrowIfNull(element);
        for (UIElement? current = element; current is not null; current = current.VisualParent)
        {
            if (!ParticipatesInLayout(current))
            {
                return false;
            }
        }

        return true;
    }
}
