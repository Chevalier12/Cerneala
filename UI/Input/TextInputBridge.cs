using Cerneala.UI.Elements;

namespace Cerneala.UI.Input;

public sealed class TextInputBridge
{
    public void Dispatch(IReadOnlyList<TextInputSnapshotEvent> textInputEvents, FocusManager focusManager, ElementInputRouteMap routeMap)
    {
        ArgumentNullException.ThrowIfNull(textInputEvents);
        ArgumentNullException.ThrowIfNull(focusManager);
        ArgumentNullException.ThrowIfNull(routeMap);

        // Handlers can move focus or detach the focused element, so every text event
        // resolves its target from the focus at the moment it is raised. An element that
        // a handler attached and focused after routeMap was built is routed over its
        // root's current route map, as key events are.
        foreach (TextInputSnapshotEvent textInputEvent in textInputEvents)
        {
            if (!focusManager.TryGetKeyboardTarget(routeMap, out UIElement? focused, out ElementInputRouteMap targetMap))
            {
                continue;
            }

            RoutedEventRouter.RaisePair(
                targetMap.InputTree,
                focused,
                new TextCompositionEventArgs(InputEvents.PreviewTextInputEvent, focused, textInputEvent.Text),
                new TextCompositionEventArgs(InputEvents.TextInputEvent, focused, textInputEvent.Text));
        }
    }
}
