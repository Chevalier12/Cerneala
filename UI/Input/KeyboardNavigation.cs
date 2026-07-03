using Cerneala.UI.Elements;

namespace Cerneala.UI.Input;

public sealed class KeyboardNavigation
{
    public bool Focus(UIElement element, FocusManager focusManager, ElementInputRouteMap routeMap)
    {
        ArgumentNullException.ThrowIfNull(element);
        ArgumentNullException.ThrowIfNull(focusManager);
        ArgumentNullException.ThrowIfNull(routeMap);

        return focusManager.Focus(element, routeMap);
    }
}
