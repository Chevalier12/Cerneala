namespace Cerneala.UI.Input;

public sealed class TextInputBridge
{
    public void Dispatch(IReadOnlyList<TextInputSnapshotEvent> textInputEvents, FocusManager focusManager, ElementInputRouteMap routeMap)
    {
        ArgumentNullException.ThrowIfNull(textInputEvents);
        ArgumentNullException.ThrowIfNull(focusManager);
        ArgumentNullException.ThrowIfNull(routeMap);

        if (focusManager.FocusedElement is null || !routeMap.TryGetId(focusManager.FocusedElement, out UiElementId focusedId))
        {
            return;
        }

        foreach (TextInputSnapshotEvent textInputEvent in textInputEvents)
        {
            RoutedEventRouter.RaisePair(
                routeMap.InputTree,
                focusedId,
                new TextCompositionEventArgs(InputEvents.PreviewTextInputEvent, focusedId, textInputEvent.Text),
                new TextCompositionEventArgs(InputEvents.TextInputEvent, focusedId, textInputEvent.Text));
        }
    }
}
