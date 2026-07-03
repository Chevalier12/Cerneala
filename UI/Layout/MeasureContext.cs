namespace Cerneala.UI.Layout;

public readonly record struct MeasureContext(LayoutSize AvailableSize, LayoutRounding Rounding)
{
    public MeasureContext(LayoutSize availableSize)
        : this(availableSize, LayoutRounding.Disabled)
    {
    }
}
