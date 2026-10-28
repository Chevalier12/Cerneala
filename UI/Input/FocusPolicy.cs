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
            routeMap.Contains(element) &&
            IsInCurrentRoute(element);
    }

    // A handler can collapse or hide an ancestor of the element after the supplied map was built, for example a KeyDown
    // handler before the Tab of the same frame moves the focus. The element of a UIRoot is therefore also checked
    // against the root's current route map, which no longer contains it, as WPF's Keyboard.Focus fails and its
    // KeyboardNavigation skips an element whose IsVisible is false (#1176).
    private static bool IsInCurrentRoute(UIElement element)
    {
        return element.Root is not UIRoot root || root.InputCache.EnsureCurrent(root).Contains(element);
    }
}
