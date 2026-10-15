namespace Cerneala.UI.Layout;

public readonly record struct LayoutRounding
{
    // How far above a whole device pixel a scaled size may lie and still count as on that pixel. Float error from
    // scaling or adding sizes that lie on device pixels stays far below it, and content cut off by at most 1/256 of a
    // device pixel changes the coverage of the edge pixel by at most one 8-bit step.
    private const float DevicePixelTolerance = 1f / 256;

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

        // A value that lies on a device pixel up to float error keeps that pixel. 57 device pixels at scale 1.75 are
        // 57 / 1.75f, and (57 / 1.75f) * 1.75f is slightly more than 57, so its ceiling alone would add a 58th device
        // pixel. A panel that adds 57 / 1.75f and 1 / 1.75f gets a float sum whose scaled value is 58.000008.
        float scaled = value * Scale;
        float pixels = MathF.Floor(scaled);
        return (scaled - pixels <= DevicePixelTolerance ? pixels : pixels + 1) / Scale;
    }
}
