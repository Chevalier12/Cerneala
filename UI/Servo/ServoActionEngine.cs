using Cerneala.UI.Elements;
using Cerneala.UI.Input;
using Cerneala.UI.Layout;

namespace Cerneala.UI.Servo;

internal sealed class ServoActionEngine
{
    private readonly ServoQueryEngine queryEngine;

    internal ServoActionEngine(ServoQueryEngine queryEngine)
    {
        this.queryEngine = queryEngine ?? throw new ArgumentNullException(nameof(queryEngine));
    }

    internal ServoActionTarget ResolveActionable(UIRoot root, ServoTarget target)
    {
        ServoResolvedElement match = queryEngine.Resolve(root, target);
        UIElement element = match.Element;
        if (!ReferenceEquals(element.Root, root))
        {
            throw new ServoTargetNotActionableException(
                "The Servo target detached before input could be dispatched.");
        }

        if (!UIElementVisibility.IsEffectivelyVisible(element))
        {
            throw new ServoTargetNotActionableException("The Servo target is not effectively visible.");
        }

        if (!element.IsEnabled)
        {
            throw new ServoTargetNotActionableException("The Servo target is disabled.");
        }

        LayoutRect bounds = element.ArrangedBounds;
        if (!HasUsableBounds(bounds))
        {
            throw new ServoTargetNotActionableException("The Servo target has no usable arranged bounds.");
        }

        float x = bounds.X + (bounds.Width / 2);
        float y = bounds.Y + (bounds.Height / 2);
        HitTestResult? hit = root.InputCache.HitTest(root, x, y);
        if (hit is null || !IsSelfOrDescendant(hit.Element, element))
        {
            throw new ServoTargetNotActionableException(
                "The Servo target center is not hit-testable in the current tree.");
        }

        return new ServoActionTarget(element, x, y);
    }

    private static bool HasUsableBounds(LayoutRect bounds)
    {
        return float.IsFinite(bounds.X) &&
            float.IsFinite(bounds.Y) &&
            float.IsFinite(bounds.Width) &&
            float.IsFinite(bounds.Height) &&
            bounds.Width > 0 &&
            bounds.Height > 0;
    }

    private static bool IsSelfOrDescendant(UIElement candidate, UIElement target)
    {
        for (UIElement? current = candidate; current is not null; current = current.VisualParent)
        {
            if (ReferenceEquals(current, target))
            {
                return true;
            }
        }

        return false;
    }
}

internal readonly record struct ServoActionTarget(UIElement Element, float X, float Y);
