namespace Cerneala.UI.Layout;

public readonly record struct LayoutRounding
{
    public LayoutRounding(bool isEnabled)
        : this(isEnabled, 1)
    {
    }

    public LayoutRounding(bool isEnabled, float scale)
    {
        if (!float.IsFinite(scale) || scale <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(scale));
        }

        IsEnabled = isEnabled;
        Scale = scale;
    }

    public static LayoutRounding Disabled { get; } = new(false);

    public static LayoutRounding Enabled { get; } = new(true);

    public bool IsEnabled { get; }

    public float Scale { get; }

    public static LayoutRounding ForScale(float scale)
    {
        return new LayoutRounding(true, scale);
    }

    public float Round(float value)
    {
        return IsEnabled ? MathF.Round(value * Scale) / Scale : value;
    }

    public LayoutPoint Round(LayoutPoint point)
    {
        return new LayoutPoint(Round(point.X), Round(point.Y));
    }

    public LayoutSize Round(LayoutSize size)
    {
        return new LayoutSize(RoundUp(size.Width), RoundUp(size.Height));
    }

    public LayoutRect Round(LayoutRect rect)
    {
        return new LayoutRect(Round(rect.X), Round(rect.Y), Round(rect.Width), Round(rect.Height));
    }

    private float RoundUp(float value)
    {
        if (!IsEnabled)
        {
            return value;
        }

        // A value that already lies on a device pixel keeps it. 57 device pixels at scale 1.75 are 57 / 1.75f, and
        // (57 / 1.75f) * 1.75f is slightly more than 57, so its ceiling alone would add a 58th device pixel.
        float scaled = value * Scale;
        return MathF.Round(scaled) / Scale == value ? value : MathF.Ceiling(scaled) / Scale;
    }
}
