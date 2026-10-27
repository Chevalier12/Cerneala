using System.Diagnostics.CodeAnalysis;
using Cerneala.UI.Elements;

namespace Cerneala.UI.Input;

public sealed class FocusManager
{
    private IReadOnlyList<UIElement> focusedPath = [];

    // Weak, like WPF's weak event pattern: a host can share one FocusManager across many
    // ElementInputBridge instances, and a dropped bridge must stay collectable (#509).
    private readonly List<WeakReference<KeyboardActivationController>> focusChangeListeners = [];

    public UIElement? FocusedElement { get; private set; }

    // Notified after FocusedElement changes and before the focus routed events, so input
    // state tied to the old focus (a held Space press) is gone when handlers run.
    internal void AddFocusChangeListener(KeyboardActivationController listener)
    {
        focusChangeListeners.RemoveAll(static reference => !reference.TryGetTarget(out _));
        focusChangeListeners.Add(new WeakReference<KeyboardActivationController>(listener));
    }

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
            : routeMap.GetVisualRouteToRoot(element);
        UpdateFocusState(oldFocus, element, focusedPath, nextPath);
        focusedPath = nextPath;
        NotifyFocusChanged();
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

        if (!FocusPolicy.CanFocus(FocusedElement, ResolveRouteMap(FocusedElement, routeMap)))
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
                TryGetKeyboardTarget(routeMap, out UIElement? pressedTarget, out ElementInputRouteMap pressedMap))
            {
                bool handled = RaiseKeyPair(pressedMap, pressedTarget, key, inputFrame.Keyboard, InputEvents.PreviewKeyDownEvent, InputEvents.KeyDownEvent);
                results.Add(new KeyboardDispatchResult(pressedTarget, key, KeyboardDispatchKind.Pressed, handled));
            }

            if (inputFrame.Keyboard.IsDown(key) || inputFrame.Keyboard.IsReleased(key))
            {
                int repeatCount = inputFrame.Keyboard.GetRepeatCount(key);
                for (int i = 0; i < repeatCount; i++)
                {
                    if (TryGetKeyboardTarget(routeMap, out UIElement? repeatTarget, out ElementInputRouteMap repeatMap))
                    {
                        RaiseKeyPair(repeatMap, repeatTarget, key, inputFrame.Keyboard, InputEvents.PreviewKeyDownEvent, InputEvents.KeyDownEvent, isRepeat: true);
                    }
                }
            }

            if (inputFrame.Keyboard.IsReleased(key) &&
                TryGetKeyboardTarget(routeMap, out UIElement? releasedTarget, out ElementInputRouteMap releasedMap))
            {
                bool handled = RaiseKeyPair(releasedMap, releasedTarget, key, inputFrame.Keyboard, InputEvents.PreviewKeyUpEvent, InputEvents.KeyUpEvent);
                results.Add(new KeyboardDispatchResult(releasedTarget, key, KeyboardDispatchKind.Released, handled));
            }
        }

        return results;
    }

    internal bool TryGetKeyboardTarget(
        ElementInputRouteMap routeMap,
        [NotNullWhen(true)] out UIElement? target,
        out ElementInputRouteMap targetRouteMap)
    {
        target = FocusedElement;
        targetRouteMap = target is null ? routeMap : ResolveRouteMap(target, routeMap);
        if (target is not null && FocusPolicy.CanFocus(target, targetRouteMap) && targetRouteMap.Contains(target))
        {
            return true;
        }

        target = null;
        return false;
    }

    // Handlers of the frame can change the input route after the supplied route map was
    // built. A focused element that they attached is routed over its root's current route
    // map, and one that they removed from the route, for example by hiding an ancestor
    // (#953), is checked against that map, which no longer contains it.
    private static ElementInputRouteMap ResolveRouteMap(UIElement element, ElementInputRouteMap routeMap)
    {
        if (element.Root is not UIRoot root)
        {
            return routeMap;
        }

        ElementInputRouteMap currentMap = root.InputCache.EnsureCurrent(root);
        return routeMap.Contains(element) && currentMap.Contains(element)
            ? routeMap
            : currentMap;
    }

    private void NotifyFocusChanged()
    {
        if (focusChangeListeners.Count == 0)
        {
            return;
        }

        // A listener can move the focus again or build another bridge, so the live
        // listeners are copied first, as an event invocation uses its delegate list.
        List<KeyboardActivationController> listeners = new(focusChangeListeners.Count);
        for (int i = 0; i < focusChangeListeners.Count;)
        {
            if (focusChangeListeners[i].TryGetTarget(out KeyboardActivationController? listener))
            {
                listeners.Add(listener);
                i++;
            }
            else
            {
                focusChangeListeners.RemoveAt(i);
            }
        }

        foreach (KeyboardActivationController listener in listeners)
        {
            listener.OnFocusChanged();
        }
    }

    private static bool RaiseKeyPair(
        ElementInputRouteMap routeMap,
        UIElement target,
        InputKey key,
        InputFrame.KeyboardFrame keyboard,
        RoutedEvent previewEvent,
        RoutedEvent bubbleEvent,
        bool isRepeat = false)
    {
        bool isControlDown = keyboard.IsDown(InputKey.LeftCtrl) || keyboard.IsDown(InputKey.RightCtrl);
        bool isShiftDown = keyboard.IsDown(InputKey.LeftShift) || keyboard.IsDown(InputKey.RightShift);
        bool isAltDown = keyboard.IsDown(InputKey.LeftAlt) || keyboard.IsDown(InputKey.RightAlt);
        KeyEventArgs previewArgs = new(previewEvent, target, key, isControlDown, isShiftDown, isAltDown, isRepeat);
        KeyEventArgs bubbleArgs = new(bubbleEvent, target, key, isControlDown, isShiftDown, isAltDown, isRepeat);
        RoutedEventRouter.RaisePair(
            routeMap.InputTree,
            target,
            previewArgs,
            bubbleArgs);
        return previewArgs.Handled || bubbleArgs.Handled;
    }

    private static KeyboardFocusChangedEventArgs? RaisePreviewFocusLost(ElementInputRouteMap routeMap, UIElement? oldFocus, UIElement? newFocus)
    {
        if (oldFocus is null || !routeMap.Contains(oldFocus))
        {
            return null;
        }

        KeyboardFocusChangedEventArgs args = new(InputEvents.PreviewLostKeyboardFocusEvent, oldFocus, oldFocus, newFocus);
        RoutedEventRouter.Raise(routeMap.InputTree, oldFocus, args);
        return args;
    }

    private static KeyboardFocusChangedEventArgs? RaisePreviewFocusGot(ElementInputRouteMap routeMap, UIElement? oldFocus, UIElement? newFocus)
    {
        if (newFocus is null || !routeMap.Contains(newFocus))
        {
            return null;
        }

        KeyboardFocusChangedEventArgs args = new(InputEvents.PreviewGotKeyboardFocusEvent, newFocus, oldFocus, newFocus);
        RoutedEventRouter.Raise(routeMap.InputTree, newFocus, args);
        return args;
    }

    private static void RaiseFocusLost(
        ElementInputRouteMap routeMap,
        UIElement? oldFocus,
        UIElement? newFocus,
        KeyboardFocusChangedEventArgs? previewArgs)
    {
        if (oldFocus is null || !routeMap.Contains(oldFocus))
        {
            return;
        }

        if (previewArgs?.Handled != true)
        {
            RoutedEventRouter.Raise(
                routeMap.InputTree,
                oldFocus,
                new KeyboardFocusChangedEventArgs(InputEvents.LostKeyboardFocusEvent, oldFocus, oldFocus, newFocus));
        }

        RoutedEventRouter.Raise(routeMap.InputTree, oldFocus, new RoutedEventArgs(InputEvents.LostFocusEvent, oldFocus));
    }

    private static void RaiseFocusGot(
        ElementInputRouteMap routeMap,
        UIElement? oldFocus,
        UIElement? newFocus,
        KeyboardFocusChangedEventArgs? previewArgs)
    {
        if (newFocus is null || !routeMap.Contains(newFocus))
        {
            return;
        }

        if (previewArgs?.Handled != true)
        {
            RoutedEventRouter.Raise(
                routeMap.InputTree,
                newFocus,
                new KeyboardFocusChangedEventArgs(InputEvents.GotKeyboardFocusEvent, newFocus, oldFocus, newFocus));
        }

        RoutedEventRouter.Raise(routeMap.InputTree, newFocus, new RoutedEventArgs(InputEvents.GotFocusEvent, newFocus));
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
