namespace Cerneala.UI.Layout.Virtualization;

public readonly record struct VirtualizationContext(
    int ItemCount,
    float ItemExtent,
    float ViewportExtent,
    float ScrollOffset,
    int CacheItems = 0)
{
    public float TotalExtent
    {
        get
        {
            if (ItemCount <= 0 || ItemExtent <= 0 || !float.IsFinite(ItemExtent))
            {
                return 0;
            }

            double extent = (double)ItemCount * ItemExtent;
            return extent >= float.MaxValue ? float.MaxValue : (float)extent;
        }
    }

    public RealizationWindow GetRealizationWindow()
    {
        if (ItemCount <= 0 || ItemExtent <= 0 || !float.IsFinite(ItemExtent))
        {
            return RealizationWindow.Empty;
        }

        double offset = SanitizeNonNegativeFinite(ScrollOffset);
        double viewport = SanitizeNonNegativeFinite(ViewportExtent);
        int cache = Math.Max(0, CacheItems);
        int start = ClampIndex(Math.Floor(offset / ItemExtent) - cache);
        int end = ClampIndex(Math.Ceiling((offset + viewport) / ItemExtent) + cache);
        return RealizationWindow.Create(ItemCount, start, Math.Max(start, end));
    }

    private static double SanitizeNonNegativeFinite(float value)
    {
        return float.IsFinite(value) && value > 0 ? value : 0;
    }

    private int ClampIndex(double value)
    {
        if (double.IsNaN(value) || value <= 0)
        {
            return 0;
        }

        if (value >= ItemCount)
        {
            return ItemCount;
        }

        return (int)value;
    }
}
