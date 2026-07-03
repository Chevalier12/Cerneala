using System.Runtime.CompilerServices;
using Cerneala.UI.Elements;
using Cerneala.UI.Invalidation;

namespace Cerneala.UI.Layout.Panels;

public sealed class Canvas : Panel
{
    private static readonly ConditionalWeakTable<UIElement, CanvasPosition> positions = new();

    public static void SetLeft(UIElement element, float left)
    {
        ArgumentNullException.ThrowIfNull(element);
        CanvasPosition position = positions.GetOrCreateValue(element);
        if (position.Left == left)
        {
            return;
        }

        position.Left = left;
        InvalidateParentCanvasArrange(element);
    }

    public static float GetLeft(UIElement element)
    {
        ArgumentNullException.ThrowIfNull(element);
        return positions.TryGetValue(element, out CanvasPosition? position) ? position.Left : 0;
    }

    public static void SetTop(UIElement element, float top)
    {
        ArgumentNullException.ThrowIfNull(element);
        CanvasPosition position = positions.GetOrCreateValue(element);
        if (position.Top == top)
        {
            return;
        }

        position.Top = top;
        InvalidateParentCanvasArrange(element);
    }

    public static float GetTop(UIElement element)
    {
        ArgumentNullException.ThrowIfNull(element);
        return positions.TryGetValue(element, out CanvasPosition? position) ? position.Top : 0;
    }

    protected override LayoutSize MeasureCore(MeasureContext context)
    {
        foreach (UIElement child in VisualChildren)
        {
            if (child.Visibility == Visibility.Collapsed)
            {
                child.Measure(new MeasureContext(LayoutSize.Zero, context.Rounding));
            }
            else
            {
                child.Measure(new MeasureContext(LayoutSize.Unconstrained, context.Rounding));
            }
        }

        return LayoutSize.Zero;
    }

    protected override LayoutRect ArrangeCore(ArrangeContext context)
    {
        foreach (UIElement child in VisualChildren)
        {
            if (child.Visibility == Visibility.Collapsed)
            {
                child.Arrange(new ArrangeContext(new LayoutRect(context.FinalRect.X, context.FinalRect.Y, 0, 0), context.Rounding));
                continue;
            }

            float x = context.FinalRect.X + GetLeft(child);
            float y = context.FinalRect.Y + GetTop(child);
            child.Arrange(new ArrangeContext(new LayoutRect(x, y, child.DesiredSize.Width, child.DesiredSize.Height), context.Rounding));
        }

        return context.FinalRect;
    }

    private sealed class CanvasPosition
    {
        public float Left { get; set; }

        public float Top { get; set; }
    }

    private static void InvalidateParentCanvasArrange(UIElement element)
    {
        if (element.VisualParent is Canvas parent)
        {
            parent.IncrementLayoutVersion();
            parent.Invalidate(InvalidationFlags.Arrange, "Canvas child coordinates changed");
        }
    }
}
