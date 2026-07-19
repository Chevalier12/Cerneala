namespace Cerneala.UI.Prism.Runtime;

[Flags]
public enum PrismBlendChannels
{
    None = 0,
    Red = 1,
    Green = 2,
    Blue = 4,
    Alpha = 8,
    Rgb = Red | Green | Blue,
    Rgba = Rgb | Alpha
}

public enum PrismKnockout
{
    None,
    Shallow,
    Deep
}

public enum PrismBlendIfChannel
{
    Gray,
    Red,
    Green,
    Blue
}

public readonly record struct PrismBlendRange
{
    public PrismBlendRange(
        float blackStart,
        float blackEnd,
        float whiteStart,
        float whiteEnd)
    {
        ValidateThreshold(blackStart, nameof(blackStart));
        ValidateThreshold(blackEnd, nameof(blackEnd));
        ValidateThreshold(whiteStart, nameof(whiteStart));
        ValidateThreshold(whiteEnd, nameof(whiteEnd));
        if (blackStart > blackEnd || blackEnd > whiteStart || whiteStart > whiteEnd)
        {
            throw new ArgumentException("Blend If thresholds must be ordered from black start to white end.");
        }

        BlackStart = blackStart;
        BlackEnd = blackEnd;
        WhiteStart = whiteStart;
        WhiteEnd = whiteEnd;
    }

    public float BlackStart { get; }

    public float BlackEnd { get; }

    public float WhiteStart { get; }

    public float WhiteEnd { get; }

    internal System.Numerics.Vector4 ToVector4() =>
        new(BlackStart, BlackEnd, WhiteStart, WhiteEnd);

    internal static PrismBlendRange FromVector4(System.Numerics.Vector4 value) =>
        new(value.X, value.Y, value.Z, value.W);

    private static void ValidateThreshold(float value, string parameterName)
    {
        if (!float.IsFinite(value) || value is < 0f or > 1f)
        {
            throw new ArgumentOutOfRangeException(
                parameterName,
                value,
                "Blend If thresholds must be finite values from zero through one.");
        }
    }
}
