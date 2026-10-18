using Cerneala.UI.Elements;

namespace Cerneala.UI.Layout.Panels;

public class Panel : UIElement
{
    public Panel()
    {
        Children = new PanelChildCollection(this);
    }

    public PanelChildCollection Children { get; }

    // The panel measures, arranges and draws only its visual children, so a child that is only a
    // logical child would be ignored without any error.
    internal override void ValidateChildInsertion(UIElement child, ElementChildRole role, bool ownerManaged)
    {
        base.ValidateChildInsertion(child, role, ownerManaged);
        if (role == ElementChildRole.Logical && !ownerManaged && !ReferenceEquals(child.VisualParent, this))
        {
            throw new InvalidOperationException(
                "A panel lays out and draws only its visual children, so a child added only to LogicalChildren would never be shown. " +
                "Add it with Panel.Children.Add, which adds it to LogicalChildren and VisualChildren together.");
        }
    }

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
