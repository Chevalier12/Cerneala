namespace Cerneala.UI.Layout;

public readonly record struct LayoutRounding(bool IsEnabled)
{
    public static LayoutRounding Disabled { get; } = new(false);

    public static LayoutRounding Enabled { get; } = new(true);

    public float Round(float value)
    {
        return IsEnabled ? MathF.Round(value) : value;
    }

    public LayoutPoint Round(LayoutPoint point)
    {
        return new LayoutPoint(Round(point.X), Round(point.Y));
    }

    public LayoutSize Round(LayoutSize size)
    {
        return new LayoutSize(Round(size.Width), Round(size.Height));
    }

    public LayoutRect Round(LayoutRect rect)
    {
        return new LayoutRect(Round(rect.X), Round(rect.Y), Round(rect.Width), Round(rect.Height));
    }
}
