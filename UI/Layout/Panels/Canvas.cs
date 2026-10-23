using Cerneala.UI.Core;
using Cerneala.UI.Elements;
using Cerneala.UI.Invalidation;

namespace Cerneala.UI.Layout.Panels;

public class Canvas : Panel
{
    // The child options only queue the child's own arrange; UIElement.OnPropertyInvalidated also calls
    // InvalidateParentCanvasArrange, because the parent Canvas is the one that places the child.
    public static readonly UiProperty<float> LeftProperty = UiProperty<float>.Register(
        "Left",
        typeof(Canvas),
        new UiPropertyMetadata<float>(0f, UiPropertyOptions.AffectsArrange));

    public static readonly UiProperty<float> TopProperty = UiProperty<float>.Register(
        "Top",
        typeof(Canvas),
        new UiPropertyMetadata<float>(0f, UiPropertyOptions.AffectsArrange));

    public static void SetLeft(UIElement element, float left)
    {
        ArgumentNullException.ThrowIfNull(element);
        element.SetValue(LeftProperty, left);
    }

    public static float GetLeft(UIElement element)
    {
        ArgumentNullException.ThrowIfNull(element);
        return element.GetValue(LeftProperty);
    }

    public static void SetTop(UIElement element, float top)
    {
        ArgumentNullException.ThrowIfNull(element);
        element.SetValue(TopProperty, top);
    }

    public static float GetTop(UIElement element)
    {
        ArgumentNullException.ThrowIfNull(element);
        return element.GetValue(TopProperty);
    }

    internal static bool IsPositionProperty(UiProperty property) =>
        ReferenceEquals(property, LeftProperty) || ReferenceEquals(property, TopProperty);

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

    internal static void InvalidateParentCanvasArrange(UIElement element)
    {
        if (element.VisualParent is Canvas parent)
        {
            parent.IncrementLayoutVersion();
            parent.Invalidate(InvalidationFlags.Arrange, "Canvas child coordinates changed");
        }
    }
}
