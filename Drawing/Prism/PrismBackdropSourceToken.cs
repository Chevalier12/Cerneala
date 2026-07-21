namespace Cerneala.Drawing.Prism;

internal readonly record struct PrismBackdropSourceToken
{
    private static long nextValue;

    private PrismBackdropSourceToken(long value)
    {
        Value = value;
    }

    public long Value { get; }

    public static PrismBackdropSourceToken CreateUnique()
    {
        long value = Interlocked.Increment(ref nextValue);
        if (value <= 0)
        {
            throw new InvalidOperationException(
                "Prism backdrop source token space was exhausted.");
        }

        return new PrismBackdropSourceToken(value);
    }
}
