using Cerneala.UI.Elements;

namespace Cerneala.UI.Input;

// An element whose item containers are not Tab stops, but that the Tab key enters once, on one of them, as WPF's
// KeyboardNavigation.TabNavigation Once enters a ListBox or the tab strip of a TabControl on one element only.
internal interface ITabOnceScope
{
    // The container the Tab key stops on, or null when none can take the focus.
    UIElement? GetTabOnceElement(ElementInputRouteMap routeMap);
}
