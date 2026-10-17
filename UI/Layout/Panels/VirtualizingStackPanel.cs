using Cerneala.UI.Elements;
using Cerneala.UI.Layout.Virtualization;

namespace Cerneala.UI.Layout.Panels;

public class VirtualizingStackPanel : Panel, IItemsVirtualizingPanel
{
    private const float InitialEstimatedItemExtent = 28;
    private readonly Dictionary<int, float> measuredItemExtents = [];
    private readonly HashSet<int> measuredChildIndices = [];
    private readonly Dictionary<UIElement, int> childPositions = new(ReferenceEqualityComparer.Instance);
    private ItemsVirtualizationViewport? automaticViewport;
    private RealizationWindow automaticWindow = RealizationWindow.Empty;
    private float estimatedItemExtent = InitialEstimatedItemExtent;
    private LayoutRounding measureRounding = LayoutRounding.Disabled;

    public VirtualizingStackPanel()
    {
        // Clearing also releases removed children; the positions are rebuilt on the next lookup.
        VisualChildren.Changed += (_, _) => childPositions.Clear();
    }

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
                ? EstimateTotalExtent(automaticViewport.Value.ItemCount, measureRounding)
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
        bool itemCountChanged = automaticViewport?.ItemCount != itemCount;
        RealizationWindow previousWindow = automaticWindow;
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

        // The item count drives the desired extent, and the window decides which children the measure measures, so a
        // cached measure is stale when either changes: a direct caller's next Measure with the same size must measure
        // the children that entered the window. A new scroll offset inside the same window changes neither, so the
        // cached measure stays.
        if (itemCountChanged || automaticWindow != previousWindow)
        {
            IncrementLayoutVersion();
        }
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
        // The estimated extent of an automatic viewport adds device pixels the same way.
        RealizationWindow window = RealizationWindow;
        LayoutRounding rounding = context.Rounding;
        measureRounding = rounding;
        MeasureContext childContext = new(new LayoutSize(context.AvailableSize.Width, float.PositiveInfinity), rounding);
        measuredChildIndices.Clear();
        MeasureRealizedChildren(window, childContext);

        // The measured heights correct the estimate, which can move the automatic window onto children that this measure
        // skipped. Arrange places the children of the corrected window, so they are measured here until a round adds
        // no child. Each further round measures at least one more child, so the rounds end.
        if (automaticViewport is not null)
        {
            do
            {
                UpdateEstimatedItemExtent();
                RecalculateAutomaticWindow();
                window = RealizationWindow;
            }
            while (MeasureRealizedChildren(window, childContext));
        }

        float width = 0;
        float height = 0;
        for (int i = 0; i < VisualChildren.Count; i++)
        {
            UIElement child = VisualChildren[i];
            if (!IsRealized(window, FirstRealizedIndex + i))
            {
                // A child that this measure measured before the window moved past it keeps that real result, so a zero
                // that no measure produced never replaces it.
                if (!measuredChildIndices.Contains(i))
                {
                    child.SetUnmeasuredDesiredSize(LayoutSize.Zero);
                }

                continue;
            }

            width = MathF.Max(width, child.DesiredSize.Width);
            height += rounding.ToDevicePixels(child.DesiredSize.Height);
        }

        float naturalHeight = rounding.FromDevicePixels(height);
        float desiredHeight = automaticViewport is not null
            ? EstimateTotalExtent(automaticViewport.Value.ItemCount, rounding)
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
        // The stacking cursor is a double for the reason given at FromDevicePixels: in a float, ten rows of 33 pixels
        // stacked at 32,999,670 would each add 32 and the last row would end 8 pixels before TotalExtent.
        double y = context.FinalRect.Y;
        bool placedFirstAutomaticChild = false;

        for (int i = 0; i < VisualChildren.Count; i++)
        {
            UIElement child = VisualChildren[i];
            int itemIndex = FirstRealizedIndex + i;
            bool isRealized = IsRealized(window, itemIndex);
            // A skipped child's zero-sized bounds do not clip its content, so it must not be drawn at the panel origin.
            child.SetSkippedByVirtualization(!isRealized);
            if (!isRealized)
            {
                child.Arrange(new ArrangeContext(new LayoutRect(context.FinalRect.X, context.FinalRect.Y, 0, 0), context.Rounding));
                continue;
            }

            // Automatic mode places the first realized child at its estimated prefix extent, rounded to a device pixel,
            // and stacks the following realized children on it. An estimate is an average, so a prefix of 10.5 device
            // pixels followed by a row of 11 would round each child's own position on its own (ties to even) to 10 and 22,
            // leaving a gap after the first row's bottom at 21.
            if (automaticViewport is not null && !placedFirstAutomaticChild)
            {
                y = context.Rounding.Round(context.FinalRect.Y + EstimateOffset(itemIndex, context.Rounding));
                placedFirstAutomaticChild = true;
            }

            float childY = itemExtent > 0
                ? context.FinalRect.Y + (itemIndex * itemExtent)
                : (float)y;
            float height = itemExtent > 0 ? itemExtent : child.DesiredSize.Height;
            child.Arrange(new ArrangeContext(new LayoutRect(context.FinalRect.X, childY, context.FinalRect.Width, height), context.Rounding));
            y += height;
        }

        return context.FinalRect;
    }

    // A child outside the window keeps the zero size MeasureCore gives it; measured by the frame on its own, it
    // would get a guessed constraint (its last one, the panel's size or the root viewport) instead of none.
    internal override bool SkipsChildMeasure(UIElement child)
    {
        if (!IsVirtualizing)
        {
            return false;
        }

        int position = GetChildPosition(child);
        return position >= 0 && !IsRealized(RealizationWindow, FirstRealizedIndex + position);
    }

    // The frame asks once per queued child, so a linear search would make adding n children quadratic.
    // A stored position is checked before use, because Move does not raise Changed.
    private int GetChildPosition(UIElement child)
    {
        if (childPositions.TryGetValue(child, out int position) &&
            position < VisualChildren.Count &&
            ReferenceEquals(VisualChildren[position], child))
        {
            return position;
        }

        childPositions.Clear();
        for (int i = 0; i < VisualChildren.Count; i++)
        {
            childPositions[VisualChildren[i]] = i;
        }

        return childPositions.TryGetValue(child, out position) ? position : -1;
    }

    // Measures the children inside the window that the current measure has not measured yet, and reports whether there
    // were any.
    private bool MeasureRealizedChildren(RealizationWindow window, MeasureContext childContext)
    {
        bool measuredAny = false;
        for (int i = 0; i < VisualChildren.Count; i++)
        {
            int itemIndex = FirstRealizedIndex + i;
            if (!IsRealized(window, itemIndex) || !measuredChildIndices.Add(i))
            {
                continue;
            }

            UIElement child = VisualChildren[i];
            child.Measure(childContext);
            measuredAny = true;
            if (automaticViewport is not null && child.DesiredSize.Height >= 0 && float.IsFinite(child.DesiredSize.Height))
            {
                measuredItemExtents[itemIndex] = child.DesiredSize.Height;
            }
        }

        return measuredAny;
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

        // The window adds the item extents as EstimateOffset and EstimateTotalExtent do, so the scroll offset
        // TotalExtent - ViewportExtent finds the last items, the ones that arrange places there. Each prefix stays in device
        // pixels and is compared with the first device pixels past the offset and the viewport end, found once, which gives
        // the window that converting every prefix to an extent gives, without a conversion per item.
        LayoutRounding rounding = measureRounding;
        float offset = viewport.ScrollOffset;
        float viewportEnd = offset + viewport.ViewportExtent;
        float offsetDevicePixels = GetFirstDevicePixelsPast(offset, inclusive: false, rounding);
        float viewportEndDevicePixels = GetFirstDevicePixelsPast(viewportEnd, inclusive: true, rounding);
        double estimatedDevicePixels = rounding.ToDevicePixels(estimatedItemExtent);
        int start = 0;
        double cursor = 0;
        while (start < viewport.ItemCount)
        {
            double next = cursor + GetItemDevicePixels(start, rounding, estimatedDevicePixels);
            if ((float)next >= offsetDevicePixels)
            {
                break;
            }

            cursor = next;
            start++;
        }

        int end = start;
        while (end < viewport.ItemCount && (!((float)cursor >= viewportEndDevicePixels) || end == start))
        {
            cursor += GetItemDevicePixels(end, rounding, estimatedDevicePixels);
            end++;
        }

        start = Math.Max(0, start - viewport.CacheItems);
        end = Math.Min(viewport.ItemCount, end + viewport.CacheItems);
        automaticWindow = RealizationWindow.Create(viewport.ItemCount, start, end);
    }

    // The extent of the items before index. The arrange offsets, the realization window and TotalExtent all add the
    // item extents this way, so they agree.
    private float EstimateOffset(int index, LayoutRounding rounding)
    {
        double devicePixels = 0;
        double estimatedDevicePixels = rounding.ToDevicePixels(estimatedItemExtent);
        for (int current = 0; current < index; current++)
        {
            devicePixels += GetItemDevicePixels(current, rounding, estimatedDevicePixels);
        }

        return FromDevicePixels(devicePixels, rounding);
    }

    private float EstimateTotalExtent(int itemCount, LayoutRounding rounding)
    {
        return EstimateOffset(itemCount, rounding);
    }

    // With layout rounding the item extents add device pixels: a measured height on a device pixel counts as its whole
    // number of device pixels, and any other extent, such as the averaged estimate, counts as extent * Scale. Every item
    // without a measured height has the estimate, so a walk converts the estimate once and passes its device pixels.
    private double GetItemDevicePixels(int index, LayoutRounding rounding, double estimatedDevicePixels)
    {
        return measuredItemExtents.TryGetValue(index, out float extent)
            ? rounding.ToDevicePixels(extent)
            : estimatedDevicePixels;
    }

    // The sum stays a double until here. Past 2^24 a float holds only even integers, so a float sum of items that are
    // 33 device pixels tall would add 32 per item: after 1,000,000 items it would end at 32,508,400 instead of 33,000,000.
    private static float FromDevicePixels(double devicePixels, LayoutRounding rounding)
    {
        float extent = rounding.FromDevicePixels((float)devicePixels);
        return float.IsFinite(extent) ? extent : float.MaxValue;
    }

    // The smallest device pixels whose FromDevicePixels extent is above extent, or reaches it when inclusive; NaN when
    // no device pixels do. That extent never decreases as the device pixels grow, so a prefix p lies past extent exactly
    // when (float)p >= the result, and never when it is NaN. Floats from 0 to infinity are ordered as their bits, so a
    // binary search over the bits finds it with at most 32 conversions. At scale 1.25 the device pixels past offset
    // 16,000,000 start at 20,000,002: 20,000,000 converts to exactly 16,000,000, and floats there are 2 apart.
    private static float GetFirstDevicePixelsPast(float extent, bool inclusive, LayoutRounding rounding)
    {
        int low = 0;
        int high = BitConverter.SingleToInt32Bits(float.PositiveInfinity);
        if (!IsPast(high))
        {
            return float.NaN;
        }

        while (low < high)
        {
            int middle = low + ((high - low) / 2);
            if (IsPast(middle))
            {
                high = middle;
            }
            else
            {
                low = middle + 1;
            }
        }

        return BitConverter.Int32BitsToSingle(low);

        bool IsPast(int bits)
        {
            float devicePixelsExtent = FromDevicePixels(BitConverter.Int32BitsToSingle(bits), rounding);
            return inclusive ? devicePixelsExtent >= extent : devicePixelsExtent > extent;
        }
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
