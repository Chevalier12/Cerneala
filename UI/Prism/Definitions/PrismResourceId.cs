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
        Key = null;
    }

    public PrismResourceId(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException(
                "Prism resource keys cannot be empty.",
                nameof(key));
        }

        Key = key;
        Value = StableValue(key);
    }

    public int Value { get; }

    public string? Key { get; }

    public override string ToString() => Value.ToString(System.Globalization.CultureInfo.InvariantCulture);

    private static int StableValue(string key)
    {
        const uint offset = 2166136261;
        const uint prime = 16777619;
        uint hash = offset;
        foreach (char character in key)
        {
            hash ^= character;
            hash *= prime;
        }

        int value = (int)(hash & 0x7fffffff);
        return value == 0 ? 1 : value;
    }
}
