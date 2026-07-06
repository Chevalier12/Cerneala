namespace Cerneala.UI.Layout.Panels;

public readonly record struct GridLength(float Value, GridUnitType UnitType)
{
    public static GridLength Auto { get; } = new(1, GridUnitType.Auto);

    public static GridLength Star { get; } = new(1, GridUnitType.Star);

    public GridLength(float value)
        : this(value, GridUnitType.Pixel)
    {
    }

    public static GridLength Pixels(float value)
    {
        return new GridLength(value, GridUnitType.Pixel);
    }

    public static GridLength Stars(float value)
    {
        return new GridLength(value, GridUnitType.Star);
    }

    public bool IsAuto => UnitType == GridUnitType.Auto;

    public bool IsPixel => UnitType == GridUnitType.Pixel;

    public bool IsStar => UnitType == GridUnitType.Star;

    public void Validate()
    {
        if (UnitType is < GridUnitType.Pixel or > GridUnitType.Star)
        {
            throw new ArgumentOutOfRangeException(nameof(UnitType), "Grid unit type is not valid.");
        }

        if (Value < 0 || float.IsNaN(Value) || float.IsInfinity(Value))
        {
            throw new ArgumentOutOfRangeException(nameof(Value), "Grid length value must be finite and non-negative.");
        }
    }
}

public enum GridUnitType
{
    Pixel,
    Auto,
    Star
}
