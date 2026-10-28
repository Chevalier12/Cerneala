using Cerneala.UI.Elements;

namespace Cerneala.UI.Input;

internal sealed class RetainedInputBindingProcessor
{
    public IReadOnlyList<KeyboardDispatchResult> Process(
        IReadOnlyList<KeyboardDispatchResult> results,
        InputFrame inputFrame,
        CommandRouter commandRouter,
        ElementInputRouteMap routeMap)
    {
        ArgumentNullException.ThrowIfNull(results);
        ArgumentNullException.ThrowIfNull(inputFrame);
        ArgumentNullException.ThrowIfNull(commandRouter);
        ArgumentNullException.ThrowIfNull(routeMap);

        List<KeyboardDispatchResult> activationResults = [];
        foreach (KeyboardDispatchResult result in results)
        {
            if (ShouldExecuteBinding(result) &&
                TryExecuteFirstBinding(result.Target, result.Key, inputFrame, commandRouter, routeMap))
            {
                continue;
            }

            activationResults.Add(result);
        }

        return activationResults;
    }

    private static bool ShouldExecuteBinding(KeyboardDispatchResult result)
    {
        return !result.Handled && result.Kind == KeyboardDispatchKind.Pressed;
    }

    private static bool TryExecuteFirstBinding(
        UIElement focusedElement,
        InputKey pressedKey,
        InputFrame inputFrame,
        CommandRouter commandRouter,
        ElementInputRouteMap routeMap)
    {
        foreach (UIElement owner in routeMap.GetRouteToRoot(focusedElement))
        {
            if (!IsValidInputElement(owner, routeMap))
            {
                continue;
            }

            foreach (InputBinding binding in owner.InputBindings)
            {
                // As WPF's KeyGesture.Matches, which compares its key with the key of the KeyEventArgs, a key binding
                // runs only for the press of its own key: Ctrl and S pressed in one frame run a Ctrl+S binding once,
                // for the S press, and a handled KeyDown of S keeps it from running (#771).
                if (binding.Gesture is KeyGesture keyGesture && keyGesture.Key != pressedKey)
                {
                    continue;
                }

                if (binding.TryExecute(inputFrame, commandRouter, routeMap, owner))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static bool IsValidInputElement(UIElement element, ElementInputRouteMap routeMap)
    {
        return element.IsAttached &&
            element.IsEnabled &&
            UIElementVisibility.ParticipatesInInput(element) &&
            routeMap.Contains(element);
    }
}
