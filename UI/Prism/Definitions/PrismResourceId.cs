namespace Cerneala.UI.Prism.Definitions;

public readonly record struct PrismResourceId
{
    public PrismResourceId(int value)
    {
        if (value <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(value), value, "Prism resource identifiers must be positive.");
        }

        Value = value;
    }

    public int Value { get; }

    public override string ToString() => Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
}
