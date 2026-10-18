using System.Diagnostics.CodeAnalysis;
using Cerneala.UI.Elements;

namespace Cerneala.UI.Input;

public sealed class FocusManager
{
    private IReadOnlyList<UIElement> focusedPath = [];

    public UIElement? FocusedElement { get; private set; }

    // Raised after FocusedElement changes and before the focus routed events, so input
    // state tied to the old focus (a held Space press) is gone when handlers run.
    internal event Action? FocusedElementChanged;

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
        FocusedElementChanged?.Invoke();
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

        // Handlers can move focus or detach the focused element, so every key event
        // resolves its target from the focus at the moment it is raised.
        List<KeyboardDispatchResult> results = [];
        foreach (InputKey key in Enum.GetValues<InputKey>())
        {
            if (key is InputKey.None or InputKey.Unknown)
            {
                continue;
            }

            if (inputFrame.Keyboard.IsPressed(key) &&
                TryGetKeyboardTarget(routeMap, out UIElement? pressedTarget, out UiElementId pressedId))
            {
                bool handled = RaiseKeyPair(routeMap, pressedId, key, inputFrame.Keyboard, InputEvents.PreviewKeyDownEvent, InputEvents.KeyDownEvent);
                results.Add(new KeyboardDispatchResult(pressedTarget, pressedId, key, KeyboardDispatchKind.Pressed, handled));
            }

            if (inputFrame.Keyboard.IsDown(key) || inputFrame.Keyboard.IsReleased(key))
            {
                int repeatCount = inputFrame.Keyboard.GetRepeatCount(key);
                for (int i = 0; i < repeatCount; i++)
                {
                    if (TryGetKeyboardTarget(routeMap, out _, out UiElementId repeatId))
                    {
                        RaiseKeyPair(routeMap, repeatId, key, inputFrame.Keyboard, InputEvents.PreviewKeyDownEvent, InputEvents.KeyDownEvent, isRepeat: true);
                    }
                }
            }

            if (inputFrame.Keyboard.IsReleased(key) &&
                TryGetKeyboardTarget(routeMap, out UIElement? releasedTarget, out UiElementId releasedId))
            {
                bool handled = RaiseKeyPair(routeMap, releasedId, key, inputFrame.Keyboard, InputEvents.PreviewKeyUpEvent, InputEvents.KeyUpEvent);
                results.Add(new KeyboardDispatchResult(releasedTarget, releasedId, key, KeyboardDispatchKind.Released, handled));
            }
        }

        return results;
    }

    private bool TryGetKeyboardTarget(
        ElementInputRouteMap routeMap,
        [NotNullWhen(true)] out UIElement? target,
        out UiElementId targetId)
    {
        target = FocusedElement;
        if (target is not null && FocusPolicy.CanFocus(target, routeMap) && routeMap.TryGetId(target, out targetId))
        {
            return true;
        }

        target = null;
        targetId = default;
        return false;
    }

    private static bool RaiseKeyPair(
        ElementInputRouteMap routeMap,
        UiElementId targetId,
        InputKey key,
        InputFrame.KeyboardFrame keyboard,
        RoutedEvent previewEvent,
        RoutedEvent bubbleEvent,
        bool isRepeat = false)
    {
        bool isControlDown = keyboard.IsDown(InputKey.LeftCtrl) || keyboard.IsDown(InputKey.RightCtrl);
        bool isShiftDown = keyboard.IsDown(InputKey.LeftShift) || keyboard.IsDown(InputKey.RightShift);
        bool isAltDown = keyboard.IsDown(InputKey.LeftAlt) || keyboard.IsDown(InputKey.RightAlt);
        KeyEventArgs previewArgs = new(previewEvent, targetId, key, isControlDown, isShiftDown, isAltDown, isRepeat);
        KeyEventArgs bubbleArgs = new(bubbleEvent, targetId, key, isControlDown, isShiftDown, isAltDown, isRepeat);
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
