using Cerneala.UI.Elements;
using Cerneala.UI.Layout.Virtualization;

namespace Cerneala.UI.Layout.Panels;

public class VirtualizingStackPanel : Panel, IItemsVirtualizingPanel
{
    private const float InitialEstimatedItemExtent = 28;
    private readonly Dictionary<int, float> measuredItemExtents = [];
    private ItemsVirtualizationViewport? automaticViewport;
    private RealizationWindow automaticWindow = RealizationWindow.Empty;
    private float estimatedItemExtent = InitialEstimatedItemExtent;

    public VirtualizationContext? VirtualizationContext { get; set; }

    public RealizationWindow RealizationWindow => VirtualizationContext?.GetRealizationWindow() ??
        (automaticViewport is null
            ? RealizationWindow.Create(VisualChildren.Count, 0, VisualChildren.Count)
            : automaticWindow);

    public float TotalExtent
    {
        get
        {
            RealizationWindow window = RealizationWindow;
            return automaticViewport is not null
                ? EstimateTotalExtent(automaticViewport.Value.ItemCount)
                : UsesNaturalItemHeights(window)
                    ? DesiredSize.Height
                    : VirtualizationContext?.TotalExtent ?? DesiredSize.Height;
        }
    }

    public int FirstRealizedIndex { get; set; }

    public void UpdateViewport(ItemsVirtualizationViewport viewport)
    {
        int itemCount = Math.Max(0, viewport.ItemCount);
        automaticViewport = viewport with
        {
            ItemCount = itemCount,
            ViewportExtent = SanitizeExtent(viewport.ViewportExtent),
            ScrollOffset = SanitizeExtent(viewport.ScrollOffset),
            CacheItems = Math.Max(0, viewport.CacheItems)
        };
        foreach (int index in measuredItemExtents.Keys.Where(index => index >= itemCount).ToArray())
        {
            measuredItemExtents.Remove(index);
        }

        RecalculateAutomaticWindow();
    }

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
            if (automaticViewport is not null && child.DesiredSize.Height > 0 && float.IsFinite(child.DesiredSize.Height))
            {
                measuredItemExtents[itemIndex] = child.DesiredSize.Height;
            }
        }

        if (automaticViewport is not null)
        {
            UpdateEstimatedItemExtent();
            RecalculateAutomaticWindow();
        }

        float desiredHeight = automaticViewport is not null
            ? EstimateTotalExtent(automaticViewport.Value.ItemCount)
            : UsesNaturalItemHeights(window)
            ? height
            : VirtualizationContext?.TotalExtent ?? height;
        return new LayoutSize(width, desiredHeight);
    }

    protected override LayoutRect ArrangeCore(ArrangeContext context)
    {
        RealizationWindow window = RealizationWindow;
        float itemExtent = automaticViewport is null && !UsesNaturalItemHeights(window) &&
            VirtualizationContext is { ItemExtent: > 0 } virtualizationContext &&
            float.IsFinite(virtualizationContext.ItemExtent)
            ? virtualizationContext.ItemExtent
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

            float childY = automaticViewport is not null
                ? context.FinalRect.Y + EstimateOffset(itemIndex)
                : itemExtent > 0
                ? context.FinalRect.Y + (itemIndex * itemExtent)
                : y;
            float height = itemExtent > 0 ? itemExtent : child.DesiredSize.Height;
            child.Arrange(new ArrangeContext(new LayoutRect(context.FinalRect.X, childY, context.FinalRect.Width, height), context.Rounding));
            y += height;
        }

        return context.FinalRect;
    }

    private bool UsesNaturalItemHeights(RealizationWindow window)
    {
        return VirtualizationContext is not { } context ||
            (FirstRealizedIndex == 0 &&
             VisualChildren.Count == context.ItemCount &&
             window.StartIndex == 0 &&
             window.EndIndexExclusive >= context.ItemCount);
    }

    private void RecalculateAutomaticWindow()
    {
        if (automaticViewport is not ItemsVirtualizationViewport viewport || viewport.ItemCount == 0)
        {
            automaticWindow = RealizationWindow.Empty;
            return;
        }

        float offset = viewport.ScrollOffset;
        float viewportEnd = offset + viewport.ViewportExtent;
        int start = 0;
        float cursor = 0;
        while (start < viewport.ItemCount)
        {
            float next = cursor + GetEstimatedItemExtent(start);
            if (next > offset)
            {
                break;
            }

            cursor = next;
            start++;
        }

        int end = start;
        while (end < viewport.ItemCount && (cursor < viewportEnd || end == start))
        {
            cursor += GetEstimatedItemExtent(end);
            end++;
        }

        start = Math.Max(0, start - viewport.CacheItems);
        end = Math.Min(viewport.ItemCount, end + viewport.CacheItems);
        automaticWindow = RealizationWindow.Create(viewport.ItemCount, start, end);
    }

    private float EstimateOffset(int index)
    {
        float offset = 0;
        for (int current = 0; current < index; current++)
        {
            offset += GetEstimatedItemExtent(current);
        }

        return offset;
    }

    private float EstimateTotalExtent(int itemCount)
    {
        double extent = 0;
        for (int index = 0; index < itemCount; index++)
        {
            extent += GetEstimatedItemExtent(index);
        }

        return extent >= float.MaxValue ? float.MaxValue : (float)extent;
    }

    private float GetEstimatedItemExtent(int index)
    {
        return measuredItemExtents.TryGetValue(index, out float extent)
            ? extent
            : estimatedItemExtent;
    }

    private void UpdateEstimatedItemExtent()
    {
        if (measuredItemExtents.Count == 0)
        {
            return;
        }

        estimatedItemExtent = measuredItemExtents.Values.Average();
    }

    private static float SanitizeExtent(float value)
    {
        return value > 0 && float.IsFinite(value) ? value : 0;
    }
}
