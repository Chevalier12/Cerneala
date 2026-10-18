namespace Cerneala.UI.Input;

public sealed class TextInputBridge
{
    public void Dispatch(IReadOnlyList<TextInputSnapshotEvent> textInputEvents, FocusManager focusManager, ElementInputRouteMap routeMap)
    {
        ArgumentNullException.ThrowIfNull(textInputEvents);
        ArgumentNullException.ThrowIfNull(focusManager);
        ArgumentNullException.ThrowIfNull(routeMap);

        // Handlers can move focus or detach the focused element, so every text event
        // resolves its target from the focus at the moment it is raised.
        foreach (TextInputSnapshotEvent textInputEvent in textInputEvents)
        {
            if (!focusManager.TryGetKeyboardTarget(routeMap, out _, out UiElementId focusedId))
            {
                continue;
            }

            RoutedEventRouter.RaisePair(
                routeMap.InputTree,
                focusedId,
                new TextCompositionEventArgs(InputEvents.PreviewTextInputEvent, focusedId, textInputEvent.Text),
                new TextCompositionEventArgs(InputEvents.TextInputEvent, focusedId, textInputEvent.Text));
        }
    }
}
