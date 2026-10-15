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

    // The device pixels of a length that is added to other lengths, as a desired size adds its children or its margin.
    // A length that lies on a device pixel gives the whole number of its device pixels, so a sum of such lengths stays
    // on a device pixel: 57 / 1.75f and 1 / 1.75f give 57 and 1, while their float sum is one step above 58 / 1.75f,
    // which Round(LayoutSize) would round up to a 59th device pixel. Any other length gives its scaled value.
    internal float ToDevicePixels(float value)
    {
        if (!IsEnabled)
        {
            return value;
        }

        float scaled = value * Scale;
        float whole = MathF.Round(scaled);
        return whole / Scale == value ? whole : scaled;
    }

    internal float FromDevicePixels(float devicePixels)
    {
        return IsEnabled ? devicePixels / Scale : devicePixels;
    }

    private float RoundUp(float value)
    {
        if (!IsEnabled)
        {
            return value;
        }

        // A value that already lies on a device pixel keeps it. 57 device pixels at scale 1.75 are 57 / 1.75f, and
        // (57 / 1.75f) * 1.75f is slightly more than 57, so its ceiling alone would add a 58th device pixel.
        return FromDevicePixels(MathF.Ceiling(ToDevicePixels(value)));
    }
}
