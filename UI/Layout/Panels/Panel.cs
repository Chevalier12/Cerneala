using Cerneala.UI.Elements;

namespace Cerneala.UI.Layout.Panels;

public class Panel : UIElement
{
    protected override LayoutSize MeasureCore(MeasureContext context)
    {
        float width = 0;
        float height = 0;
        foreach (UIElement child in VisualChildren)
        {
            if (child.Visibility == Visibility.Collapsed)
            {
                child.Measure(new MeasureContext(LayoutSize.Zero, context.Rounding));
                continue;
            }

            LayoutSize childSize = child.Measure(context);
            width = MathF.Max(width, childSize.Width);
            height = MathF.Max(height, childSize.Height);
        }

        return new LayoutSize(width, height);
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

            child.Arrange(new ArrangeContext(context.FinalRect, context.Rounding));
        }

        return context.FinalRect;
    }
}
