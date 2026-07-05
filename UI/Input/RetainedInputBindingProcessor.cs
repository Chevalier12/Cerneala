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
                TryExecuteFirstBinding(result.Target, inputFrame, commandRouter, routeMap))
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
        InputFrame inputFrame,
        CommandRouter commandRouter,
        ElementInputRouteMap routeMap)
    {
        foreach (UIElement owner in FocusedRoute(focusedElement))
        {
            if (!IsValidInputElement(owner, routeMap))
            {
                continue;
            }

            foreach (InputBinding binding in owner.InputBindings)
            {
                if (binding.TryExecute(inputFrame, commandRouter, routeMap, owner))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static IEnumerable<UIElement> FocusedRoute(UIElement focusedElement)
    {
        for (UIElement? current = focusedElement; current is not null; current = current.VisualParent)
        {
            yield return current;
        }
    }

    private static bool IsValidInputElement(UIElement element, ElementInputRouteMap routeMap)
    {
        return element.IsAttached &&
            element.IsEnabled &&
            UIElementVisibility.ParticipatesInInput(element) &&
            routeMap.TryGetId(element, out _);
    }
}
