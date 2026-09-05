using Cerneala.UI.Elements;

namespace Cerneala.UI.Input;

public sealed class FocusManager
{
    private IReadOnlyList<UIElement> focusedPath = [];

    public UIElement? FocusedElement { get; private set; }

    public bool Focus(UIElement? element, ElementInputRouteMap routeMap)
    {
        ArgumentNullException.ThrowIfNull(routeMap);

        if ((element?.Root ?? FocusedElement?.Root) is UIRoot root)
        {
            root.ActiveFocusManager = this;
        }

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

        IReadOnlyList<UIElement> nextPath = element is null
            ? []
            : routeMap.GetRouteToRoot(element);
        UpdateFocusState(oldFocus, element, focusedPath, nextPath);
        focusedPath = nextPath;
        KeyboardFocusChangedEventArgs? previewLostArgs = RaisePreviewFocusLost(routeMap, oldFocus, element);
        KeyboardFocusChangedEventArgs? previewGotArgs = RaisePreviewFocusGot(routeMap, oldFocus, element);
        RaiseFocusLost(routeMap, oldFocus, element, previewLostArgs);
        RaiseFocusGot(routeMap, oldFocus, element, previewGotArgs);
        return true;
    }

    public void DispatchKeyboard(InputFrame inputFrame, ElementInputRouteMap routeMap)
    {
        _ = DispatchKeyboardWithResults(inputFrame, routeMap);
    }

    internal IReadOnlyList<KeyboardDispatchResult> DispatchKeyboardWithResults(InputFrame inputFrame, ElementInputRouteMap routeMap)
    {
        ArgumentNullException.ThrowIfNull(inputFrame);
        ArgumentNullException.ThrowIfNull(routeMap);

        if (FocusedElement is null)
        {
            return [];
        }

        if (!FocusPolicy.CanFocus(FocusedElement, routeMap))
        {
            Focus(null, routeMap);
            return [];
        }

        if (!routeMap.TryGetId(FocusedElement, out UiElementId focusedId))
        {
            return [];
        }

        List<KeyboardDispatchResult> results = [];
        foreach (InputKey key in Enum.GetValues<InputKey>())
        {
            if (key is InputKey.None or InputKey.Unknown)
            {
                continue;
            }

            if (inputFrame.Keyboard.IsPressed(key))
            {
                bool handled = RaiseKeyPair(routeMap, focusedId, key, inputFrame.Keyboard, InputEvents.PreviewKeyDownEvent, InputEvents.KeyDownEvent);
                results.Add(new KeyboardDispatchResult(FocusedElement, focusedId, key, KeyboardDispatchKind.Pressed, handled));
            }

            if (inputFrame.Keyboard.IsReleased(key))
            {
                bool handled = RaiseKeyPair(routeMap, focusedId, key, inputFrame.Keyboard, InputEvents.PreviewKeyUpEvent, InputEvents.KeyUpEvent);
                results.Add(new KeyboardDispatchResult(FocusedElement, focusedId, key, KeyboardDispatchKind.Released, handled));
            }
        }

        return results;
    }

    private static bool RaiseKeyPair(
        ElementInputRouteMap routeMap,
        UiElementId targetId,
        InputKey key,
        InputFrame.KeyboardFrame keyboard,
        RoutedEvent previewEvent,
        RoutedEvent bubbleEvent)
    {
        bool isControlDown = keyboard.IsDown(InputKey.LeftCtrl) || keyboard.IsDown(InputKey.RightCtrl);
        bool isShiftDown = keyboard.IsDown(InputKey.LeftShift) || keyboard.IsDown(InputKey.RightShift);
        bool isAltDown = keyboard.IsDown(InputKey.LeftAlt) || keyboard.IsDown(InputKey.RightAlt);
        KeyEventArgs previewArgs = new(previewEvent, targetId, key, isControlDown, isShiftDown, isAltDown);
        KeyEventArgs bubbleArgs = new(bubbleEvent, targetId, key, isControlDown, isShiftDown, isAltDown);
        RoutedEventRouter.RaisePair(
            routeMap.InputTree,
            targetId,
            previewArgs,
            bubbleArgs);
        return previewArgs.Handled || bubbleArgs.Handled;
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

    private static void UpdateFocusState(
        UIElement? oldFocus,
        UIElement? newFocus,
        IReadOnlyList<UIElement> oldPath,
        IReadOnlyList<UIElement> newPath)
    {
        if (oldFocus is not null)
        {
            oldFocus.IsKeyboardFocused = false;
            foreach (UIElement element in oldPath)
            {
                if (!ContainsReference(newPath, element))
                {
                    element.IsKeyboardFocusWithin = false;
                }
            }
        }

        if (newFocus is not null)
        {
            newFocus.IsKeyboardFocused = true;
            foreach (UIElement element in newPath)
            {
                element.IsKeyboardFocusWithin = true;
            }
        }
    }

    private static bool ContainsReference(
        IReadOnlyList<UIElement> elements,
        UIElement candidate)
    {
        foreach (UIElement element in elements)
        {
            if (ReferenceEquals(element, candidate))
            {
                return true;
            }
        }

        return false;
    }
}
