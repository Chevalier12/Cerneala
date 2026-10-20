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
                DiscardRejectedChild(value);
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

        // Logical first: the child stays attached through its visual parent, so the visual removal can start a
        // presence exit. In the other order the logical removal detaches the child and cancels that exit.
        LogicalChildren.Remove(element);
        VisualChildren.Remove(element);
    }

    // Another owner takes the child: the element leaves at once, without a presence exit, so the new owner can add it,
    // and Child becomes null.
    internal void ReleaseChildElementForNewOwner()
    {
        UIElement? oldChild = child;
        if (oldChild is null)
        {
            return;
        }

        child = null;
        DiscardRejectedChild(oldChild);
        Invalidate(Cerneala.UI.Invalidation.InvalidationFlags.Measure | Cerneala.UI.Invalidation.InvalidationFlags.Render, "Decorator child changed");
    }

    // A rejected child never became the child, so it leaves at once without a presence exit: the visual removal first,
    // so the logical removal detaches it and cancels the exit that the visual removal started.
    private void DiscardRejectedChild(UIElement? element)
    {
        if (element is null)
        {
            return;
        }

        VisualChildren.Remove(element);
        LogicalChildren.Remove(element);
    }
}
