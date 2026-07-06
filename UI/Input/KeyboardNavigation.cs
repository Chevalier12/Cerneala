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

        List<NavigationCandidate> candidates = CollectCandidates(routeMap);
        if (candidates.Count == 0)
        {
            return null;
        }

        if (current is null)
        {
            return reverse ? candidates[^1].Element : candidates[0].Element;
        }

        int currentIndex = candidates.FindIndex(candidate => ReferenceEquals(candidate.Element, current));
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

    private static List<NavigationCandidate> CollectCandidates(ElementInputRouteMap routeMap)
    {
        List<NavigationCandidate> candidates = [];
        for (int routeOrder = 0; routeOrder < routeMap.ElementsInRouteOrder.Count; routeOrder++)
        {
            UIElement element = routeMap.ElementsInRouteOrder[routeOrder];
            if (FocusPolicy.CanFocus(element, routeMap) && element.IsTabStop)
            {
                candidates.Add(new NavigationCandidate(element, element.TabIndex, routeOrder));
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
