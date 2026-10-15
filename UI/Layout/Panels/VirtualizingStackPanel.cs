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

    // Without a context or viewport every current child is realized, and the children represent
    // the items from FirstRealizedIndex on, so the window covers the valid item indices among them.
    public RealizationWindow RealizationWindow => VirtualizationContext?.GetRealizationWindow() ??
        (automaticViewport is null ? CreateChildrenWindow() : automaticWindow);

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

    // Without either source the window is derived from the children, not from any item count.
    internal bool IsVirtualizing => VirtualizationContext is not null || automaticViewport is not null;

    public void UpdateViewport(ItemsVirtualizationViewport viewport)
    {
        int itemCount = Math.Max(0, viewport.ItemCount);
        if (automaticViewport?.ItemCount != itemCount)
        {
            // The item count drives the desired extent, so a cached measure is stale.
            IncrementLayoutVersion();
        }

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

    // A recorded height belongs to one item, so it follows that item when the collection changes.
    internal void OnItemsReplaced(int index, int removedCount, int addedCount)
    {
        RemapMeasuredItemExtents(current => current < index
            ? current
            : current < index + removedCount
            ? -1
            : current - removedCount + addedCount);
    }

    internal void OnItemsMoved(int oldIndex, int newIndex, int count)
    {
        RemapMeasuredItemExtents(current =>
        {
            if (current >= oldIndex && current < oldIndex + count)
            {
                return newIndex + current - oldIndex;
            }

            int remaining = current < oldIndex ? current : current - count;
            return remaining < newIndex ? remaining : remaining + count;
        });
    }

    internal void OnItemsReset()
    {
        RemapMeasuredItemExtents(_ => -1);
    }

    protected override LayoutSize MeasureCore(MeasureContext context)
    {
        // With layout rounding the natural item heights add device pixels, as StackPanel adds its children, so items of
        // 57 / 1.75f and 1 / 1.75f desire 58 / 1.75f instead of a float one step above it that rounds up to 59 pixels.
        RealizationWindow window = RealizationWindow;
        LayoutRounding rounding = context.Rounding;
        float width = 0;
        float height = 0;
        for (int i = 0; i < VisualChildren.Count; i++)
        {
            int itemIndex = FirstRealizedIndex + i;
            UIElement child = VisualChildren[i];
            if (!IsRealized(window, itemIndex))
            {
                child.SetDesiredSize(LayoutSize.Zero);
                continue;
            }

            child.Measure(new MeasureContext(new LayoutSize(context.AvailableSize.Width, float.PositiveInfinity), context.Rounding));
            width = MathF.Max(width, child.DesiredSize.Width);
            height += rounding.ToDevicePixels(child.DesiredSize.Height);
            if (automaticViewport is not null && child.DesiredSize.Height >= 0 && float.IsFinite(child.DesiredSize.Height))
            {
                measuredItemExtents[itemIndex] = child.DesiredSize.Height;
            }
        }

        if (automaticViewport is not null)
        {
            UpdateEstimatedItemExtent();
            RecalculateAutomaticWindow();
        }

        float naturalHeight = rounding.FromDevicePixels(height);
        float desiredHeight = automaticViewport is not null
            ? EstimateTotalExtent(automaticViewport.Value.ItemCount)
            : UsesNaturalItemHeights(window)
            ? naturalHeight
            : VirtualizationContext?.TotalExtent ?? naturalHeight;
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
            if (!IsRealized(window, itemIndex))
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

    // Item indices start at 0 and stop at int.MaxValue, so a negative FirstRealizedIndex or an
    // overflowing FirstRealizedIndex + VisualChildren.Count only narrows the reported window.
    private RealizationWindow CreateChildrenWindow()
    {
        int end = (int)Math.Min((long)FirstRealizedIndex + VisualChildren.Count, int.MaxValue);
        return RealizationWindow.Create(end, FirstRealizedIndex, end);
    }

    // Without a context or viewport nothing is virtualized, so every child is realized even when
    // its item index lies outside the reported window.
    private bool IsRealized(RealizationWindow window, int itemIndex)
    {
        return !IsVirtualizing || window.Contains(itemIndex);
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

        // Zero is an exact measurement for its item, not evidence that unknown
        // items have no extent. Preserve the positive estimate when all samples are zero.
        estimatedItemExtent = measuredItemExtents.Values
            .Where(extent => extent > 0)
            .DefaultIfEmpty(estimatedItemExtent)
            .Average();
    }

    private void RemapMeasuredItemExtents(Func<int, int> remap)
    {
        if (measuredItemExtents.Count == 0)
        {
            return;
        }

        KeyValuePair<int, float>[] entries = [.. measuredItemExtents];
        measuredItemExtents.Clear();
        foreach ((int index, float extent) in entries)
        {
            int next = remap(index);
            if (next >= 0)
            {
                measuredItemExtents[next] = extent;
            }
        }

        // The recorded heights drive the estimate, the realization window and the desired extent.
        UpdateEstimatedItemExtent();
        RecalculateAutomaticWindow();
        IncrementLayoutVersion();
    }

    private static float SanitizeExtent(float value)
    {
        return value > 0 && float.IsFinite(value) ? value : 0;
    }
}
