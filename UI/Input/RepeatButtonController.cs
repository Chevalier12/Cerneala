using Cerneala.UI.Elements;

namespace Cerneala.UI.Input;

internal sealed class RepeatButtonController
{
    private UIElement? source;
    private UIRoot? sourceRoot;
    private TimeSpan remainingUntilRepeat;
    private bool startedThisFrame;

    public void Begin(
        UIRoot root,
        ElementInputRouteMap routeMap,
        UIElement? hitElement,
        UIElement? routedElement,
        CommandRouter commandRouter,
        PressedStateTracker pressedStateTracker)
    {
        UIElement? hitSource = FindAncestor(hitElement);
        UIElement? routedSource = FindAncestor(routedElement);
        if (hitSource is null || !ReferenceEquals(hitSource, routedSource) || !IsValid(hitSource, root, routeMap))
        {
            Clear();
            return;
        }

        source = hitSource;
        sourceRoot = root;
        remainingUntilRepeat = hitSource.PointerRepeatDelay!.Value;
        startedThisFrame = true;

        Activate(hitSource, commandRouter, routeMap);
        if (!IsActiveAndValid(root, routeMap, pressedStateTracker))
        {
            Cancel(pressedStateTracker);
        }
    }

    public void Update(
        UIRoot root,
        ElementInputRouteMap routeMap,
        UIElement? hitElement,
        bool isLeftButtonDown,
        TimeSpan frameTime,
        CommandRouter commandRouter,
        PressedStateTracker pressedStateTracker)
    {
        if (source is null)
        {
            return;
        }

        if (!isLeftButtonDown ||
            !ReferenceEquals(FindAncestor(hitElement), source) ||
            !IsActiveAndValid(root, routeMap, pressedStateTracker))
        {
            Cancel(pressedStateTracker);
            return;
        }

        if (startedThisFrame)
        {
            startedThisFrame = false;
            return;
        }

        remainingUntilRepeat -= frameTime;
        if (remainingUntilRepeat > TimeSpan.Zero)
        {
            return;
        }

        UIElement currentSource = source;
        Activate(currentSource, commandRouter, routeMap);
        if (!IsActiveAndValid(root, routeMap, pressedStateTracker))
        {
            Cancel(pressedStateTracker);
            return;
        }

        remainingUntilRepeat = currentSource.PointerRepeatInterval;
    }

    public void Cancel(PressedStateTracker pressedStateTracker)
    {
        if (source is null)
        {
            return;
        }

        Clear();
        pressedStateTracker.Cancel();
    }

    public void Clear()
    {
        source = null;
        sourceRoot = null;
        remainingUntilRepeat = TimeSpan.Zero;
        startedThisFrame = false;
    }

    private bool IsActiveAndValid(
        UIRoot root,
        ElementInputRouteMap routeMap,
        PressedStateTracker pressedStateTracker)
    {
        return source is not null &&
            ReferenceEquals(sourceRoot, root) &&
            IsValid(source, root, routeMap) &&
            ReferenceEquals(pressedStateTracker.PressedElement, source);
    }

    private static bool IsValid(UIElement candidate, UIRoot root, ElementInputRouteMap routeMap)
    {
        return candidate.PointerRepeatDelay is not null &&
            candidate.PointerRepeatInterval > TimeSpan.Zero &&
            candidate is IInputActivatable &&
            candidate is IInputCommandSource &&
            candidate is IInputPressable &&
            candidate.IsAttached &&
            candidate.IsEnabled &&
            UIElementVisibility.ParticipatesInInput(candidate) &&
            ReferenceEquals(candidate.Root, root) &&
            routeMap.TryGetId(candidate, out _);
    }

    private static void Activate(
        UIElement candidate,
        CommandRouter commandRouter,
        ElementInputRouteMap routeMap)
    {
        ((IInputActivatable)candidate).Activate();
        ((IInputCommandSource)candidate).ExecuteCommand(commandRouter, routeMap);
    }

    private static UIElement? FindAncestor(UIElement? element)
    {
        for (UIElement? current = element; current is not null; current = current.VisualParent)
        {
            if (current.PointerRepeatDelay is not null)
            {
                return current;
            }
        }

        return null;
    }
}
