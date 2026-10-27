using Cerneala.Drawing;
using Cerneala.UI.Elements;
using Cerneala.UI.Layout;
using Cerneala.UI.Layout.Panels;
using Cerneala.UI.Media;
using Cerneala.UI.Rendering;

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
        return HitTestElement(root, routeMap, x, y, filter, Matrix3x2.Identity);
    }

    private static HitTestResult? HitTestElement(
        UIElement element,
        ElementInputRouteMap routeMap,
        float x,
        float y,
        HitTestFilter filter,
        Matrix3x2 ancestorTransform)
    {
        HitTestFilterBehavior behavior = filter.Evaluate(element);
        if (behavior == HitTestFilterBehavior.ExcludeSubtree)
        {
            return null;
        }

        if (element.IsPresenceExiting || !UIElementVisibility.ParticipatesInHitTest(element) || !element.IsEnabled)
        {
            return null;
        }

        // A transform that overflows float is non-finite and, like a non-invertible
        // transform, makes the element and its subtree un-hittable.
        if (!ElementVisualTransform.TryGetElementTransform(element, out Matrix3x2 localTransform) ||
            !Matrix3x2.TryMultiply(localTransform, ancestorTransform, out Matrix3x2 elementTransform) ||
            !ElementVisualTransform.TryInvert(elementTransform, out Matrix3x2 inverseTransform))
        {
            return null;
        }

        DrawPoint elementPoint = inverseTransform.Transform(new DrawPoint(x, y));
        if (TryGetClip(element, out LayoutRect clipBounds) &&
            !Contains(clipBounds, elementPoint.X, elementPoint.Y))
        {
            return null;
        }

        bool containsElement = Contains(GetHitTestBounds(element), elementPoint.X, elementPoint.Y);
        if (element is UIRoot && !containsElement)
        {
            return null;
        }

        for (int i = element.VisualChildren.Count - 1; i >= 0; i--)
        {
            HitTestResult? childResult = HitTestElement(
                element.GetVisualChildInZOrder(i),
                routeMap,
                x,
                y,
                filter,
                elementTransform);
            if (childResult is not null)
            {
                return childResult;
            }
        }

        if (containsElement && element is IGeometricHitTestHost geometricHost)
        {
            UIElement? geometricElement = geometricHost.HitTestGeometry(
                routeMap,
                x,
                y,
                filter);
            if (geometricElement is not null &&
                routeMap.Contains(geometricElement))
            {
                return new HitTestResult(
                    geometricElement,
                    x,
                    y);
            }
        }

        if (!containsElement ||
            behavior == HitTestFilterBehavior.Exclude ||
            !CanHitElementDirectly(element))
        {
            return null;
        }

        return routeMap.Contains(element)
            ? new HitTestResult(element, x, y)
            : null;
    }

    private static bool CanHitElementDirectly(UIElement element)
    {
        if (element is not Panel)
        {
            return true;
        }

        foreach (var _ in element.Handlers.EnumerateHandlers())
        {
            return true;
        }

        return false;
    }

    private static bool TryGetClip(UIElement element, out LayoutRect bounds)
    {
        if (ClipNode.TryGetClip(element, out ClipNode clip))
        {
            bounds = clip.Bounds;
            return true;
        }

        if (element.ClipToBounds)
        {
            bounds = element.ArrangedBounds;
            return true;
        }

        bounds = default;
        return false;
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
