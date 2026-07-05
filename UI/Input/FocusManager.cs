using Cerneala.UI.Elements;

namespace Cerneala.UI.Input;

public sealed class FocusManager
{
    public UIElement? FocusedElement { get; private set; }

    public bool Focus(UIElement? element, ElementInputRouteMap routeMap)
    {
        ArgumentNullException.ThrowIfNull(routeMap);

        if (ReferenceEquals(FocusedElement, element))
        {
            return false;
        }

        if (element is not null && !FocusPolicy.CanFocus(element, routeMap))
        {
            return false;
        }

        UIElement? oldFocus = FocusedElement;
        FocusedElement = element;

        UpdateFocusState(oldFocus, element);
        KeyboardFocusChangedEventArgs? previewLostArgs = RaisePreviewFocusLost(routeMap, oldFocus, element);
        KeyboardFocusChangedEventArgs? previewGotArgs = RaisePreviewFocusGot(routeMap, oldFocus, element);
        RaiseFocusLost(routeMap, oldFocus, element, previewLostArgs);
        RaiseFocusGot(routeMap, oldFocus, element, previewGotArgs);
        return true;
    }

    public void DispatchKeyboard(InputFrame inputFrame, ElementInputRouteMap routeMap)
    {
        ArgumentNullException.ThrowIfNull(inputFrame);
        ArgumentNullException.ThrowIfNull(routeMap);

        if (FocusedElement is null)
        {
            return;
        }

        if (!FocusPolicy.CanFocus(FocusedElement, routeMap))
        {
            Focus(null, routeMap);
            return;
        }

        if (!routeMap.TryGetId(FocusedElement, out UiElementId focusedId))
        {
            return;
        }

        foreach (InputKey key in Enum.GetValues<InputKey>())
        {
            if (key is InputKey.None or InputKey.Unknown)
            {
                continue;
            }

            if (inputFrame.Keyboard.IsPressed(key))
            {
                RaiseKeyPair(routeMap, focusedId, key, InputEvents.PreviewKeyDownEvent, InputEvents.KeyDownEvent);
            }

            if (inputFrame.Keyboard.IsReleased(key))
            {
                RaiseKeyPair(routeMap, focusedId, key, InputEvents.PreviewKeyUpEvent, InputEvents.KeyUpEvent);
            }
        }
    }

    private static void RaiseKeyPair(ElementInputRouteMap routeMap, UiElementId targetId, InputKey key, RoutedEvent previewEvent, RoutedEvent bubbleEvent)
    {
        RoutedEventRouter.RaisePair(
            routeMap.InputTree,
            targetId,
            new KeyEventArgs(previewEvent, targetId, key),
            new KeyEventArgs(bubbleEvent, targetId, key));
    }

    private static KeyboardFocusChangedEventArgs? RaisePreviewFocusLost(ElementInputRouteMap routeMap, UIElement? oldFocus, UIElement? newFocus)
    {
        if (oldFocus is null || !routeMap.TryGetId(oldFocus, out UiElementId oldId))
        {
            return null;
        }

        KeyboardFocusChangedEventArgs args = new(InputEvents.PreviewLostKeyboardFocusEvent, oldId, oldFocus, newFocus);
        RoutedEventRouter.Raise(routeMap.InputTree, oldId, args);
        return args;
    }

    private static KeyboardFocusChangedEventArgs? RaisePreviewFocusGot(ElementInputRouteMap routeMap, UIElement? oldFocus, UIElement? newFocus)
    {
        if (newFocus is null || !routeMap.TryGetId(newFocus, out UiElementId newId))
        {
            return null;
        }

        KeyboardFocusChangedEventArgs args = new(InputEvents.PreviewGotKeyboardFocusEvent, newId, oldFocus, newFocus);
        RoutedEventRouter.Raise(routeMap.InputTree, newId, args);
        return args;
    }

    private static void RaiseFocusLost(
        ElementInputRouteMap routeMap,
        UIElement? oldFocus,
        UIElement? newFocus,
        KeyboardFocusChangedEventArgs? previewArgs)
    {
        if (oldFocus is null || !routeMap.TryGetId(oldFocus, out UiElementId oldId))
        {
            return;
        }

        if (previewArgs?.Handled != true)
        {
            RoutedEventRouter.Raise(
                routeMap.InputTree,
                oldId,
                new KeyboardFocusChangedEventArgs(InputEvents.LostKeyboardFocusEvent, oldId, oldFocus, newFocus));
        }

        RoutedEventRouter.Raise(routeMap.InputTree, oldId, new RoutedEventArgs(InputEvents.LostFocusEvent, oldId));
    }

    private static void RaiseFocusGot(
        ElementInputRouteMap routeMap,
        UIElement? oldFocus,
        UIElement? newFocus,
        KeyboardFocusChangedEventArgs? previewArgs)
    {
        if (newFocus is null || !routeMap.TryGetId(newFocus, out UiElementId newId))
        {
            return;
        }

        if (previewArgs?.Handled != true)
        {
            RoutedEventRouter.Raise(
                routeMap.InputTree,
                newId,
                new KeyboardFocusChangedEventArgs(InputEvents.GotKeyboardFocusEvent, newId, oldFocus, newFocus));
        }

        RoutedEventRouter.Raise(routeMap.InputTree, newId, new RoutedEventArgs(InputEvents.GotFocusEvent, newId));
    }

    private static void UpdateFocusState(UIElement? oldFocus, UIElement? newFocus)
    {
        if (oldFocus is not null)
        {
            oldFocus.IsKeyboardFocused = false;
            foreach (UIElement element in FocusPath(oldFocus))
            {
                if (!FocusPathContains(newFocus, element))
                {
                    element.IsKeyboardFocusWithin = false;
                }
            }
        }

        if (newFocus is not null)
        {
            newFocus.IsKeyboardFocused = true;
            foreach (UIElement element in FocusPath(newFocus))
            {
                element.IsKeyboardFocusWithin = true;
            }
        }
    }

    private static bool FocusPathContains(UIElement? focusedElement, UIElement candidate)
    {
        if (focusedElement is null)
        {
            return false;
        }

        return FocusPath(focusedElement).Any(element => ReferenceEquals(element, candidate));
    }

    private static IEnumerable<UIElement> FocusPath(UIElement element)
    {
        yield return element;
        foreach (UIElement ancestor in ElementTreeWalker.Ancestors(element, ElementChildRole.Visual))
        {
            yield return ancestor;
        }
    }
}
