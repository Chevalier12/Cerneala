using Cerneala.UI.Elements;

namespace Cerneala.UI.Input;

internal sealed class KeyboardNavigationController
{
    private readonly KeyboardNavigation navigation = new();

    public bool Process(
        IReadOnlyList<KeyboardDispatchResult> results,
        InputFrame inputFrame,
        UIRoot root,
        FocusManager focusManager,
        ElementInputRouteMap routeMap)
    {
        ArgumentNullException.ThrowIfNull(results);
        ArgumentNullException.ThrowIfNull(inputFrame);
        ArgumentNullException.ThrowIfNull(root);
        ArgumentNullException.ThrowIfNull(focusManager);
        ArgumentNullException.ThrowIfNull(routeMap);

        if (!inputFrame.Keyboard.IsPressed(InputKey.Tab))
        {
            return false;
        }

        KeyboardDispatchResult? tabResult = results.FirstOrDefault(
            result => result.Key == InputKey.Tab && result.Kind == KeyboardDispatchKind.Pressed);
        if (tabResult?.Handled == true)
        {
            return false;
        }

        if (tabResult is null && focusManager.FocusedElement is not null)
        {
            return false;
        }

        bool reverse = inputFrame.Keyboard.IsDown(InputKey.LeftShift) || inputFrame.Keyboard.IsDown(InputKey.RightShift);
        return navigation.MoveNext(root, focusManager, routeMap, reverse);
    }
}
