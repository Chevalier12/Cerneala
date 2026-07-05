using Cerneala.UI.Elements;

namespace Cerneala.UI.Input;

public static class FocusPolicy
{
    public static bool CanFocus(UIElement? element, ElementInputRouteMap routeMap)
    {
        ArgumentNullException.ThrowIfNull(routeMap);
        if (element is null)
        {
            return false;
        }

        return element.IsAttached &&
            element.Focusable &&
            element.IsEnabled &&
            UIElementVisibility.ParticipatesInInput(element) &&
            routeMap.TryGetId(element, out _);
    }
}
