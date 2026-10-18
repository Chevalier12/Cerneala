using Cerneala.UI.Elements;

namespace Cerneala.UI.Input;

internal sealed class KeyboardActivationController
{
    private UIElement? spacePressedElement;

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
                ProcessSpace(result, commandRouter, routeMap);
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

    private void ProcessSpace(KeyboardDispatchResult result, CommandRouter commandRouter, ElementInputRouteMap routeMap)
    {
        if (result.Kind == KeyboardDispatchKind.Pressed)
        {
            PressSpace(result, routeMap);
            return;
        }

        ReleaseSpace(result, commandRouter, routeMap);
    }

    private void PressSpace(KeyboardDispatchResult result, ElementInputRouteMap routeMap)
    {
        if (result.Handled)
        {
            return;
        }

        if (FindValidCommandSource(result.Target, routeMap) is not UIElement commandElement)
        {
            ClearPressed();
            return;
        }

        ClearPressed();
        if (commandElement is IInputPressable pressable)
        {
            pressable.IsPressed = true;
        }

        spacePressedElement = commandElement;
    }

    private void ReleaseSpace(KeyboardDispatchResult result, CommandRouter commandRouter, ElementInputRouteMap routeMap)
    {
        UIElement? pressedElement = spacePressedElement;
        ClearPressed();

        if (result.Handled || pressedElement is null)
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

    private void ClearPressedIfInvalid(ElementInputRouteMap routeMap)
    {
        if (spacePressedElement is not null && !IsValidInputElement(spacePressedElement, routeMap))
        {
            ClearPressed();
        }
    }

    private void ClearPressed()
    {
        if (spacePressedElement is IInputPressable pressable)
        {
            pressable.IsPressed = false;
        }

        spacePressedElement = null;
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
            routeMap.TryGetId(element, out _);
    }
}
