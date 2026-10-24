using Cerneala.UI.Elements;

namespace Cerneala.UI.Input;

internal sealed class KeyboardActivationController
{
    private readonly PressedStateTracker pressedStateTracker;
    private UIElement? spacePressedElement;

    public KeyboardActivationController(PressedStateTracker pressedStateTracker)
    {
        this.pressedStateTracker = pressedStateTracker;
    }

    public void Process(
        IReadOnlyList<KeyboardDispatchResult> results,
        FocusManager focusManager,
        CommandRouter commandRouter,
        ElementInputRouteMap routeMap)
    {
        ArgumentNullException.ThrowIfNull(results);
        ArgumentNullException.ThrowIfNull(focusManager);
        ArgumentNullException.ThrowIfNull(commandRouter);
        ArgumentNullException.ThrowIfNull(routeMap);

        ClearPressedIfInvalid(routeMap);

        foreach (KeyboardDispatchResult result in results)
        {
            if (result.Key == InputKey.Enter && result.Kind == KeyboardDispatchKind.Pressed)
            {
                ActivateEnter(result, commandRouter, routeMap);
                continue;
            }

            if (result.Key == InputKey.Space)
            {
                ProcessSpace(result, focusManager, commandRouter, routeMap);
            }
        }

        if (focusManager.FocusedElement is null)
        {
            ClearPressed();
        }

        ClearPressedIfInvalid(routeMap);
    }

    private static void ActivateEnter(KeyboardDispatchResult result, CommandRouter commandRouter, ElementInputRouteMap routeMap)
    {
        if (result.Handled)
        {
            return;
        }

        if (FindValidCommandSource(result.Target, routeMap) is IInputCommandSource commandSource)
        {
            (commandSource as IInputActivatable)?.Activate();
            commandSource.ExecuteCommand(commandRouter, routeMap);
        }
    }

    private void ProcessSpace(
        KeyboardDispatchResult result,
        FocusManager focusManager,
        CommandRouter commandRouter,
        ElementInputRouteMap routeMap)
    {
        if (result.Kind == KeyboardDispatchKind.Pressed)
        {
            PressSpace(result, focusManager, routeMap);
            return;
        }

        ReleaseSpace(result, commandRouter, routeMap);
    }

    private void PressSpace(KeyboardDispatchResult result, FocusManager focusManager, ElementInputRouteMap routeMap)
    {
        if (result.Handled)
        {
            return;
        }

        // The press runs after the whole frame's key dispatch and navigation. When focus left
        // the Space target in the meantime (a Tab in the same frame, or a KeyDown handler),
        // OnFocusChanged ran before there was a press to forget, so the press does not start:
        // like the OnKeyDown of WPF buttons, a Space press starts only on the focused element.
        if (!ReferenceEquals(focusManager.FocusedElement, result.Target))
        {
            return;
        }

        if (FindValidCommandSource(result.Target, routeMap) is not UIElement commandElement)
        {
            ClearPressed();
            return;
        }

        ClearPressed();

        // Like WPF ButtonBase.OnKeyDown, which presses only when the button does not have the mouse captured: Space on
        // the element that the left mouse button presses starts no press, so the mouse release clicks it once (#661).
        if (ReferenceEquals(pressedStateTracker.PressedElement, commandElement))
        {
            return;
        }

        if (commandElement is IInputPressable pressable)
        {
            pressedStateTracker.PressKeyboard(pressable);
        }

        spacePressedElement = commandElement;
    }

    private void ReleaseSpace(KeyboardDispatchResult result, CommandRouter commandRouter, ElementInputRouteMap routeMap)
    {
        UIElement? pressedElement = spacePressedElement;
        ClearPressed();

        // Like WPF ButtonBase.OnKeyUp, which clicks only when the left mouse button is released: while the mouse still
        // presses the element, the element stays pressed and the mouse release clicks it (#661).
        if (result.Handled ||
            pressedElement is null ||
            ReferenceEquals(pressedStateTracker.PressedElement, pressedElement))
        {
            return;
        }

        UIElement? commandElement = FindValidCommandSource(result.Target, routeMap);
        if (!ReferenceEquals(pressedElement, commandElement) ||
            commandElement is not IInputCommandSource commandSource)
        {
            return;
        }

        (commandSource as IInputActivatable)?.Activate();
        commandSource.ExecuteCommand(commandRouter, routeMap);
    }

    // Like WPF ButtonBase.OnLostKeyboardFocus: the Space press belongs to the focus it
    // started on, so a focus change forgets it and the later release clicks nothing.
    // The pressed look stays while the pointer still presses the same element.
    public void OnFocusChanged()
    {
        ClearPressed();
    }

    // Like WPF ButtonBase.OnMouseLeftButtonUp, where shouldClick is !IsSpaceKeyDown: a mouse release on the element
    // that Space holds pressed is not its click (#661).
    public bool IsSpacePressed(UIElement? element)
    {
        return element is not null && ReferenceEquals(spacePressedElement, element);
    }

    private void ClearPressedIfInvalid(ElementInputRouteMap routeMap)
    {
        if (spacePressedElement is not null && !IsValidInputElement(spacePressedElement, routeMap))
        {
            ClearPressed();
        }
    }

    // The pressed state stays while the left mouse button still presses the same element.
    private void ClearPressed()
    {
        spacePressedElement = null;
        pressedStateTracker.ReleaseKeyboard();
    }

    // Only the key target itself activates, as WPF buttons react to Enter and Space only when they are the key event's
    // OriginalSource: a key on a focused descendant of a command source does not activate that command source.
    private static UIElement? FindValidCommandSource(UIElement target, ElementInputRouteMap routeMap)
    {
        return target is IInputCommandSource && IsValidInputElement(target, routeMap) ? target : null;
    }

    private static bool IsValidInputElement(UIElement element, ElementInputRouteMap routeMap)
    {
        return element.IsAttached &&
            element.IsEnabled &&
            UIElementVisibility.ParticipatesInInput(element) &&
            routeMap.Contains(element);
    }
}
