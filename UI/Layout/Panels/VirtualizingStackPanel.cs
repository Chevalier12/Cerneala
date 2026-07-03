using Cerneala.UI.Elements;
using Cerneala.UI.Layout.Virtualization;

namespace Cerneala.UI.Layout.Panels;

public class VirtualizingStackPanel : Panel
{
    public VirtualizationContext? VirtualizationContext { get; set; }

    public RealizationWindow RealizationWindow => VirtualizationContext?.GetRealizationWindow() ?? RealizationWindow.Create(VisualChildren.Count, 0, VisualChildren.Count);

    public float TotalExtent => VirtualizationContext?.TotalExtent ?? DesiredSize.Height;

    public int FirstRealizedIndex { get; set; }

    protected override LayoutSize MeasureCore(MeasureContext context)
    {
        RealizationWindow window = RealizationWindow;
        float width = 0;
        float height = 0;
        for (int i = 0; i < VisualChildren.Count; i++)
        {
            int itemIndex = FirstRealizedIndex + i;
            UIElement child = VisualChildren[i];
            if (!window.Contains(itemIndex))
            {
                child.SetDesiredSize(LayoutSize.Zero);
                continue;
            }

            child.Measure(new MeasureContext(new LayoutSize(context.AvailableSize.Width, float.PositiveInfinity), context.Rounding));
            width = MathF.Max(width, child.DesiredSize.Width);
            height += child.DesiredSize.Height;
        }

        float desiredHeight = VirtualizationContext?.TotalExtent ?? height;
        return new LayoutSize(width, desiredHeight);
    }

    protected override LayoutRect ArrangeCore(ArrangeContext context)
    {
        RealizationWindow window = RealizationWindow;
        float itemExtent = VirtualizationContext is { ItemExtent: > 0 } virtualizationContext && float.IsFinite(virtualizationContext.ItemExtent)
            ? virtualizationContext.ItemExtent
            : 0;
        float scrollOffset = VirtualizationContext is { ScrollOffset: > 0 } scrollContext && float.IsFinite(scrollContext.ScrollOffset)
            ? scrollContext.ScrollOffset
            : 0;
        float y = context.FinalRect.Y;

        for (int i = 0; i < VisualChildren.Count; i++)
        {
            UIElement child = VisualChildren[i];
            int itemIndex = FirstRealizedIndex + i;
            if (!window.Contains(itemIndex))
            {
                child.Arrange(new ArrangeContext(new LayoutRect(context.FinalRect.X, context.FinalRect.Y, 0, 0), context.Rounding));
                continue;
            }

            float childY = itemExtent > 0
                ? context.FinalRect.Y + (itemIndex * itemExtent) - scrollOffset
                : y;
            float height = itemExtent > 0 ? itemExtent : child.DesiredSize.Height;
            child.Arrange(new ArrangeContext(new LayoutRect(context.FinalRect.X, childY, context.FinalRect.Width, height), context.Rounding));
            y += height;
        }

        return context.FinalRect;
    }
}
