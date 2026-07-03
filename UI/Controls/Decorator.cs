using Cerneala.UI.Elements;
using Cerneala.UI.Layout;

namespace Cerneala.UI.Controls;

public class Decorator : Control
{
    private UIElement? child;

    public UIElement? Child
    {
        get => child;
        set
        {
            if (ReferenceEquals(child, value))
            {
                return;
            }

            ContentControl.ValidateCanOwnChild(this, value);
            UIElement? oldChild = child;
            if (oldChild is not null)
            {
                RemoveChild(oldChild);
            }

            try
            {
                AddChild(value);
                child = value;
            }
            catch
            {
                RemoveChild(value);
                AddChild(oldChild);
                child = oldChild;
                throw;
            }

            Invalidate(Cerneala.UI.Invalidation.InvalidationFlags.Measure | Cerneala.UI.Invalidation.InvalidationFlags.Render, "Decorator child changed");
        }
    }

    protected override LayoutSize MeasureCore(MeasureContext context)
    {
        Thickness insets = Insets;
        LayoutSize childSize = Child?.Measure(new MeasureContext(ContentControl.Deflate(context.AvailableSize, insets), context.Rounding)) ?? LayoutSize.Zero;
        return ContentControl.Inflate(childSize, insets);
    }

    protected override LayoutRect ArrangeCore(ArrangeContext context)
    {
        Child?.Arrange(new ArrangeContext(ContentControl.Deflate(context.FinalRect, Insets), context.Rounding));
        return context.FinalRect;
    }

    private void AddChild(UIElement? element)
    {
        if (element is null)
        {
            return;
        }

        LogicalChildren.Add(element);
        VisualChildren.Add(element);
    }

    private void RemoveChild(UIElement? element)
    {
        if (element is null)
        {
            return;
        }

        VisualChildren.Remove(element);
        LogicalChildren.Remove(element);
    }
}
