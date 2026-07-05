using Cerneala.UI.Elements;
using Cerneala.UI.Layout;

namespace Cerneala.UI.Input;

public sealed class HitTestService
{
    public HitTestResult? HitTest(UIRoot root, float x, float y, HitTestFilter? filter = null)
    {
        ArgumentNullException.ThrowIfNull(root);
        return root.InputCache.HitTest(root, x, y, filter);
    }

    public HitTestResult? HitTest(UIElement root, ElementInputRouteMap routeMap, float x, float y, HitTestFilter? filter = null)
    {
        ArgumentNullException.ThrowIfNull(root);
        ArgumentNullException.ThrowIfNull(routeMap);

        filter ??= HitTestFilter.IncludeAll;
        return HitTestElement(root, routeMap, x, y, filter);
    }

    private static HitTestResult? HitTestElement(
        UIElement element,
        ElementInputRouteMap routeMap,
        float x,
        float y,
        HitTestFilter filter)
    {
        HitTestFilterBehavior behavior = filter.Evaluate(element);
        if (behavior == HitTestFilterBehavior.ExcludeSubtree)
        {
            return null;
        }

        if (!UIElementVisibility.ParticipatesInHitTest(element))
        {
            return null;
        }

        if (!Contains(GetHitTestBounds(element), x, y))
        {
            return null;
        }

        for (int i = element.VisualChildren.Count - 1; i >= 0; i--)
        {
            HitTestResult? childResult = HitTestElement(element.VisualChildren[i], routeMap, x, y, filter);
            if (childResult is not null)
            {
                return childResult;
            }
        }

        if (behavior == HitTestFilterBehavior.Exclude || !element.IsEnabled)
        {
            return null;
        }

        return routeMap.TryGetId(element, out UiElementId elementId)
            ? new HitTestResult(element, elementId, x, y)
            : null;
    }

    private static LayoutRect GetHitTestBounds(UIElement element)
    {
        return element is UIRoot root
            ? new LayoutRect(0, 0, root.ViewportWidth, root.ViewportHeight)
            : element.ArrangedBounds;
    }

    private static bool Contains(LayoutRect bounds, float x, float y)
    {
        return x >= bounds.X &&
            y >= bounds.Y &&
            x < bounds.X + bounds.Width &&
            y < bounds.Y + bounds.Height;
    }
}
