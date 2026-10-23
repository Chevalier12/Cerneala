using Cerneala.UI.Core;
using Cerneala.UI.Elements;

namespace Cerneala.UI.Layout.Panels;

public class Canvas : Panel
{
    // Only the parent Canvas reads Left and Top, because it is the one that places the child. AffectsParentArrange
    // queues the arrange of the child's visual parent and leaves the child's own arrange valid.
    public static readonly UiProperty<float> LeftProperty = UiProperty<float>.Register(
        "Left",
        typeof(Canvas),
        new UiPropertyMetadata<float>(0f, UiPropertyOptions.AffectsParentArrange));

    public static readonly UiProperty<float> TopProperty = UiProperty<float>.Register(
        "Top",
        typeof(Canvas),
        new UiPropertyMetadata<float>(0f, UiPropertyOptions.AffectsParentArrange));

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
}
