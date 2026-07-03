namespace Cerneala.UI.Layout;

public readonly record struct ArrangeContext(LayoutRect FinalRect, LayoutRounding Rounding)
{
    public ArrangeContext(LayoutRect finalRect)
        : this(finalRect, LayoutRounding.Disabled)
    {
    }
}
