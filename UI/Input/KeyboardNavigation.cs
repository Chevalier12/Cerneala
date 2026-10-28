using Cerneala.UI.Elements;

namespace Cerneala.UI.Input;

public sealed class KeyboardNavigation
{
    public bool Focus(UIElement element, FocusManager focusManager, ElementInputRouteMap routeMap)
    {
        ArgumentNullException.ThrowIfNull(element);
        ArgumentNullException.ThrowIfNull(focusManager);
        ArgumentNullException.ThrowIfNull(routeMap);

        return focusManager.Focus(element, routeMap);
    }

    public UIElement? FindNext(UIRoot root, UIElement? current, ElementInputRouteMap routeMap, bool reverse)
    {
        ArgumentNullException.ThrowIfNull(root);
        ArgumentNullException.ThrowIfNull(routeMap);

        List<NavigationCandidate> candidates = CollectCandidates(routeMap, current, out UIElement? currentGroupStop);
        if (candidates.Count == 0)
        {
            return null;
        }

        if (current is null)
        {
            return reverse ? candidates[^1].Element : candidates[0].Element;
        }

        int currentIndex = candidates.FindIndex(candidate => ReferenceEquals(candidate.Element, current));
        // An element inside a Once group, or the scope of the group itself, moves on from the group's Tab stop, so Tab
        // and Shift+Tab leave the group, as WPF's Once leaves the whole group (#1159).
        if (currentIndex < 0 && currentGroupStop is not null)
        {
            currentIndex = candidates.FindIndex(candidate => ReferenceEquals(candidate.Element, currentGroupStop));
        }

        if (currentIndex < 0)
        {
            int currentVisualOrder = FindRouteOrder(routeMap, current);
            if (currentVisualOrder < 0)
            {
                return reverse ? candidates[^1].Element : candidates[0].Element;
            }

            return FindNearestFromVisualPosition(candidates, currentVisualOrder, reverse).Element;
        }

        int nextIndex = reverse
            ? (currentIndex - 1 + candidates.Count) % candidates.Count
            : (currentIndex + 1) % candidates.Count;
        return candidates[nextIndex].Element;
    }

    public bool MoveNext(UIRoot root, FocusManager focusManager, ElementInputRouteMap routeMap, bool reverse)
    {
        ArgumentNullException.ThrowIfNull(focusManager);

        UIElement? next = FindNext(root, focusManager.FocusedElement, routeMap, reverse);
        return next is not null && focusManager.Focus(next, routeMap);
    }

    // The Tab stops in Tab order, and the Tab stop of the Once group that holds current: the element that the outermost
    // ITabOnceScope whose container holds current is entered on, or that current is entered on when it is a scope
    // outside every container; null otherwise.
    private static List<NavigationCandidate> CollectCandidates(
        ElementInputRouteMap routeMap,
        UIElement? current,
        out UIElement? currentGroupStop)
    {
        List<NavigationCandidate> candidates = [];
        currentGroupStop = null;
        // The element that each ITabOnceScope is entered on, with the TabIndex of its scope, as WPF orders a Once group
        // by its container. A scope comes before its containers in route order, so its element is known when they come.
        Dictionary<UIElement, int>? tabOnceElements = null;
        Dictionary<UIElement, UIElement>? scopeStops = null;
        // The scope of each item container of an ITabOnceScope, and for each element inside such a container the
        // outermost scope whose container holds it. As WPF's Once makes the whole group one Tab stop, an element in a
        // group is no Tab stop of its own (#1159). A parent comes before its children in route order.
        Dictionary<UIElement, UIElement>? containerScopes = null;
        Dictionary<UIElement, UIElement>? groupScopes = null;
        for (int routeOrder = 0; routeOrder < routeMap.ElementsInRouteOrder.Count; routeOrder++)
        {
            UIElement element = routeMap.ElementsInRouteOrder[routeOrder];
            UIElement? groupScope = null;
            if (containerScopes is not null && routeMap.GetVisualParent(element) is { } parent)
            {
                groupScope = groupScopes?.GetValueOrDefault(parent) ?? containerScopes.GetValueOrDefault(parent);
                if (groupScope is not null)
                {
                    (groupScopes ??= new(ReferenceEqualityComparer.Instance))[element] = groupScope;
                }
            }

            if (element is ITabOnceScope scope)
            {
                foreach (UIElement container in scope.GetTabOnceContainers())
                {
                    (containerScopes ??= new(ReferenceEqualityComparer.Instance))[container] = element;
                }

                if (scope.GetTabOnceElement(routeMap) is { } tabOnceElement)
                {
                    (tabOnceElements ??= new(ReferenceEqualityComparer.Instance))[tabOnceElement] = element.TabIndex;
                    (scopeStops ??= new(ReferenceEqualityComparer.Instance))[element] = tabOnceElement;
                }
            }

            if (ReferenceEquals(element, current) &&
                (groupScope ?? (element is ITabOnceScope ? element : null)) is { } currentScope)
            {
                currentGroupStop = scopeStops?.GetValueOrDefault(currentScope);
            }

            if (groupScope is not null || !FocusPolicy.CanFocus(element, routeMap))
            {
                continue;
            }

            if (element.IsTabStop)
            {
                candidates.Add(new NavigationCandidate(element, element.TabIndex, routeOrder));
            }
            else if (tabOnceElements is not null && tabOnceElements.TryGetValue(element, out int scopeTabIndex))
            {
                candidates.Add(new NavigationCandidate(element, scopeTabIndex, routeOrder));
            }
        }

        candidates.Sort(static (left, right) =>
        {
            int tabIndex = left.TabIndex.CompareTo(right.TabIndex);
            return tabIndex != 0 ? tabIndex : left.VisualOrder.CompareTo(right.VisualOrder);
        });
        return candidates;
    }

    private static NavigationCandidate FindNearestFromVisualPosition(
        IReadOnlyList<NavigationCandidate> candidates,
        int currentVisualOrder,
        bool reverse)
    {
        if (reverse)
        {
            NavigationCandidate? nearest = null;
            for (int i = candidates.Count - 1; i >= 0; i--)
            {
                if (candidates[i].VisualOrder < currentVisualOrder &&
                    (!nearest.HasValue || candidates[i].VisualOrder > nearest.Value.VisualOrder))
                {
                    nearest = candidates[i];
                }
            }

            return nearest ?? candidates.MaxBy(static candidate => candidate.VisualOrder);
        }

        NavigationCandidate? nearestForward = null;
        foreach (NavigationCandidate candidate in candidates)
        {
            if (candidate.VisualOrder > currentVisualOrder &&
                (!nearestForward.HasValue || candidate.VisualOrder < nearestForward.Value.VisualOrder))
            {
                nearestForward = candidate;
            }
        }

        return nearestForward ?? candidates.MinBy(static candidate => candidate.VisualOrder);
    }

    private static int FindRouteOrder(ElementInputRouteMap routeMap, UIElement current)
    {
        for (int routeOrder = 0; routeOrder < routeMap.ElementsInRouteOrder.Count; routeOrder++)
        {
            UIElement element = routeMap.ElementsInRouteOrder[routeOrder];
            if (ReferenceEquals(element, current))
            {
                return routeOrder;
            }
        }

        return -1;
    }

    private readonly record struct NavigationCandidate(UIElement Element, int TabIndex, int VisualOrder);
}
